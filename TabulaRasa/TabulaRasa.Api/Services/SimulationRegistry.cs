using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Persistence;
using TabulaRasa.Simulation.Configuration;

namespace TabulaRasa.Api.Services
{
    public sealed class SimulationRegistry : IDisposable
    {
        private readonly object _sync = new();
        private readonly Dictionary<string, SimulationSession> _sessions = new(StringComparer.Ordinal);
        private readonly ISimulationPersistenceStore _persistence;
        private bool _disposed;

        public SimulationRegistry()
            : this(new SimulationResourceLimitsDto(
                MaxConcurrentRunningSimulations: 4,
                MaxTicksPerSecond: 20,
                MaxAgents: 200,
                MaxRetainedSnapshots: 500))
        {
        }

        public SimulationRegistry(SimulationResourceLimitsDto limits)
            : this(limits, new NullSimulationPersistenceStore())
        {
        }

        public SimulationRegistry(ISimulationPersistenceStore persistence)
            : this(new SimulationResourceLimitsDto(
                MaxConcurrentRunningSimulations: 4,
                MaxTicksPerSecond: 20,
                MaxAgents: 200,
                MaxRetainedSnapshots: 500), persistence)
        {
        }

        public SimulationRegistry(SimulationResourceLimitsDto limits, ISimulationPersistenceStore persistence)
        {
            Limits = limits;
            _persistence = persistence;
            if (!_persistence.IsDurable || _persistence.ListRuns(0, 1, out int total).Count == 0 && total == 0)
            {
                Create("Default", new SimulationConfig());
            }
        }

        public SimulationResourceLimitsDto Limits { get; }

        public IReadOnlyList<SimulationSummaryDto> List()
        {
            lock (_sync)
            {
                return _sessions.Values
                    .Select(session => session.GetSummary())
                    .OrderBy(summary => summary.CreatedAt)
                    .ToList();
            }
        }

        public SimulationRunPageDto ListRuns(int offset, int limit)
        {
            if (!_persistence.IsDurable)
            {
                IReadOnlyList<SimulationRunBrowserDto> runs = List()
                    .Select(summary => new SimulationRunBrowserDto(
                        summary.SimulationId,
                        summary.Name,
                        summary.Status,
                        summary.CurrentTick,
                        Get(summary.SimulationId)?.GetStatus().MinimumTick ?? summary.CurrentTick,
                        Get(summary.SimulationId)?.GetStatus().MaximumTick ?? summary.CurrentTick,
                        summary.AgentCount,
                        summary.AliveAgentCount,
                        summary.DeadAgentCount,
                        0,
                        0,
                        0,
                        summary.CreatedAt,
                        summary.UpdatedAt))
                    .ToList();

                return new SimulationRunPageDto(runs, 0, runs.Count, runs.Count);
            }

            IReadOnlyList<SimulationRunBrowserDto> page = _persistence.ListRuns(offset, limit, out int total);
            return new SimulationRunPageDto(page, Math.Max(0, offset), Math.Clamp(limit, 1, 200), total);
        }

        public IReadOnlyList<SimulationCheckpointSummaryDto> ListCheckpoints(string simulationId)
        {
            return _persistence.ListCheckpoints(simulationId);
        }

        public SimulationSession? Get(string simulationId)
        {
            lock (_sync)
            {
                return _sessions.GetValueOrDefault(simulationId);
            }
        }

        public SimulationSession Create(string? name, SimulationConfigDto? configDto)
        {
            SimulationConfig config = NormalizeForLimits(
                SimulationSnapshotMapper.ToConfig(configDto, new SimulationConfig()),
                rejectTooManyAgents: true);

            return Create(name, config);
        }

        public SimulationSession Clone(string sourceSimulationId, CloneSimulationRequestDto? request)
        {
            lock (_sync)
            {
                if (!_sessions.TryGetValue(sourceSimulationId, out SimulationSession? source))
                {
                    throw new KeyNotFoundException("Simulation was not found.");
                }

                SimulationDraftDto? draft = request?.SourceTick is null
                    ? source.GetDraft()
                    : source.GetDraft(request.SourceTick.Value);

                if (draft is null)
                {
                    throw new KeyNotFoundException("Source tick was not found.");
                }

                ValidateAgentCount(draft.Agents.Count);
                SimulationConfig config = NormalizeForLimits(
                    SimulationSnapshotMapper.ToConfig(draft.Config, new SimulationConfig()),
                    rejectTooManyAgents: true);
                string name = string.IsNullOrWhiteSpace(request?.Name)
                    ? $"{source.GetSummary().Name} copy"
                    : request!.Name!;
                SimulationSession clone = CreateCore(name, config, sourceSimulationId, request?.SourceTick);
                RestartFromDraftCore(clone, draft);

                return clone;
            }
        }

        public bool Delete(string simulationId)
        {
            SimulationSession? session;
            lock (_sync)
            {
                if (!_sessions.Remove(simulationId, out session))
                {
                    return false;
                }
            }

            session.Dispose();
            _persistence.DeleteRun(simulationId);
            return true;
        }

        public SimulationSession Load(string simulationId)
        {
            lock (_sync)
            {
                if (_sessions.TryGetValue(simulationId, out SimulationSession? existing))
                {
                    return existing;
                }

                SimulationStateCheckpointDto? checkpoint = _persistence.GetLatestCheckpoint(simulationId)
                    ?? throw new KeyNotFoundException("Simulation run was not found.");
                SimulationConfig config = SimulationSnapshotMapper.ToConfig(checkpoint.Config, new SimulationConfig());
                SimulationRunBrowserDto? run = _persistence.ListRuns(0, 200, out _)
                    .FirstOrDefault(candidate => candidate.SimulationId == simulationId);
                SimulationSession session = new(simulationId, run?.Name ?? simulationId, config, _persistence);
                RestartFromDraftResult result = session.RestartFromDraft(checkpoint.Draft);
                if (!result.Succeeded)
                {
                    session.Dispose();
                    throw new InvalidOperationException("Stored checkpoint could not be loaded.");
                }

                _sessions[simulationId] = session;
                return session;
            }
        }

        public SimulationSession ForkRun(string simulationId, ForkSimulationRunRequestDto? request)
        {
            SimulationStateCheckpointDto? checkpoint = request?.SourceTick is null
                ? _persistence.GetLatestCheckpoint(simulationId)
                : _persistence.GetNearestCheckpoint(simulationId, request.SourceTick.Value);

            if (checkpoint is null)
            {
                throw new KeyNotFoundException("Simulation run was not found.");
            }

            SimulationSession source = Load(simulationId);
            SimulationSnapshotDto? sourceSnapshot = request?.SourceTick is null
                ? checkpoint.Snapshot
                : source.GetSnapshot(request.SourceTick.Value);

            if (sourceSnapshot is null)
            {
                throw new KeyNotFoundException("Source tick was not found.");
            }

            SimulationDraftDto draft = SimulationSnapshotMapper.ToDraft(
                sourceSnapshot,
                checkpoint.Config);
            SimulationConfig config = NormalizeForLimits(
                SimulationSnapshotMapper.ToConfig(draft.Config, new SimulationConfig()),
                rejectTooManyAgents: true);
            SimulationSession fork = CreateCore(
                string.IsNullOrWhiteSpace(request?.Name) ? $"{source.GetSummary().Name} fork" : request!.Name!,
                config,
                simulationId,
                draft.Tick);
            RestartFromDraftCore(fork, draft);
            return fork;
        }

        public SimulationStatusDto Run(string simulationId, int intervalMilliseconds, SimulationConfigDto? config)
        {
            lock (_sync)
            {
                SimulationSession session = GetRequired(simulationId);
                if (!session.IsRunning && RunningCount() >= Limits.MaxConcurrentRunningSimulations)
                {
                    throw new InvalidOperationException("The maximum number of running simulations has been reached.");
                }

                int safeInterval = Math.Max(intervalMilliseconds, 1000 / Limits.MaxTicksPerSecond);
                return session.Run(safeInterval, config);
            }
        }

        public SimulationStatusDto Pause(string simulationId)
        {
            lock (_sync)
            {
                return GetRequired(simulationId).Pause();
            }
        }

        public SimulationStatusDto Stop(string simulationId)
        {
            lock (_sync)
            {
                return GetRequired(simulationId).Stop();
            }
        }

        public SimulationSnapshotDto Reset(string simulationId, SimulationConfigDto? config)
        {
            lock (_sync)
            {
                SimulationConfig normalized = NormalizeForLimits(
                    SimulationSnapshotMapper.ToConfig(config, GetRequired(simulationId).GetStatus().Config.ToSimulationConfig()),
                    rejectTooManyAgents: true);
                return GetRequired(simulationId).Reset(SimulationSnapshotMapper.ToConfig(normalized));
            }
        }

        public SimulationStatusDto UpdateConfig(string simulationId, SimulationConfigDto config)
        {
            lock (_sync)
            {
                SimulationConfig normalized = NormalizeForLimits(
                    SimulationSnapshotMapper.ToConfig(config, new SimulationConfig()),
                    rejectTooManyAgents: true);
                return GetRequired(simulationId).UpdateConfig(SimulationSnapshotMapper.ToConfig(normalized));
            }
        }

        public RestartFromDraftResult RestartFromDraft(string simulationId, SimulationDraftDto draft)
        {
            lock (_sync)
            {
                ValidateAgentCount(draft.Agents.Count);
                return RestartFromDraftCore(GetRequired(simulationId), draft);
            }
        }

        public SaveSimulationResponseDto Save(string simulationId)
        {
            lock (_sync)
            {
                return GetRequired(simulationId).Save();
            }
        }

        public ScenarioExportDto ExportScenario(string simulationId)
        {
            lock (_sync)
            {
                return GetRequired(simulationId).ExportScenario();
            }
        }

        public RestartFromDraftResult ImportScenario(ImportScenarioRequestDto request, out SimulationSession? session)
        {
            session = null;
            Dictionary<string, string[]> errors = SimulationSession.ValidateDraft(request.Scenario);
            _persistence.SaveScenario(
                string.IsNullOrWhiteSpace(request.Name) ? "Imported scenario" : request.Name!,
                request.Scenario,
                errors);

            if (errors.Count > 0)
            {
                return RestartFromDraftResult.Failure(errors);
            }

            ValidateAgentCount(request.Scenario.Agents.Count);
            SimulationConfig config = NormalizeForLimits(
                SimulationSnapshotMapper.ToConfig(request.Scenario.Config, new SimulationConfig()),
                rejectTooManyAgents: true);
            session = CreateCore(
                string.IsNullOrWhiteSpace(request.Name) ? "Imported scenario" : request.Name!,
                config);
            return RestartFromDraftCore(session, request.Scenario);
        }

        public RetentionResultDto ApplyRetention()
        {
            return _persistence.ApplyRetention();
        }

        public void Dispose()
        {
            List<SimulationSession> sessions;
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                sessions = _sessions.Values.ToList();
                _sessions.Clear();
            }

            foreach (SimulationSession session in sessions)
            {
                session.Dispose();
            }
        }

        private SimulationSession Create(string? name, SimulationConfig config)
        {
            lock (_sync)
            {
                return CreateCore(name, NormalizeForLimits(config, rejectTooManyAgents: true));
            }
        }

        private SimulationSession CreateCore(
            string? name,
            SimulationConfig config,
            string? sourceSimulationId = null,
            long? sourceTick = null)
        {
            string id = Guid.NewGuid().ToString("N");
            SimulationSession session = new(id, name ?? $"Simulation {_sessions.Count + 1}", config, _persistence);
            _sessions[id] = session;
            _persistence.UpsertRun(session.GetSummary(), SimulationSnapshotMapper.ToConfig(config), sourceSimulationId, sourceTick);

            return session;
        }

        private RestartFromDraftResult RestartFromDraftCore(SimulationSession session, SimulationDraftDto draft)
        {
            RestartFromDraftResult result = session.RestartFromDraft(draft);
            return result;
        }

        private SimulationSession GetRequired(string simulationId)
        {
            if (!_sessions.TryGetValue(simulationId, out SimulationSession? session))
            {
                throw new KeyNotFoundException("Simulation was not found.");
            }

            return session;
        }

        private int RunningCount()
        {
            return _sessions.Values.Count(session => session.IsRunning);
        }

        private SimulationConfig NormalizeForLimits(SimulationConfig config, bool rejectTooManyAgents)
        {
            if (rejectTooManyAgents)
            {
                ValidateAgentCount(config.InitialAgentCount);
            }

            int minimumInterval = 1000 / Limits.MaxTicksPerSecond;
            return config with
            {
                TickIntervalMilliseconds = Math.Max(config.TickIntervalMilliseconds, minimumInterval),
                SnapshotHistoryLimit = Math.Min(config.SnapshotHistoryLimit, Limits.MaxRetainedSnapshots)
            };
        }

        private void ValidateAgentCount(int agentCount)
        {
            if (agentCount > Limits.MaxAgents)
            {
                throw new InvalidOperationException($"Agent count cannot exceed {Limits.MaxAgents}.");
            }
        }
    }

    internal static class SimulationConfigDtoExtensions
    {
        public static SimulationConfig ToSimulationConfig(this SimulationConfigDto dto)
        {
            return SimulationSnapshotMapper.ToConfig(dto, new SimulationConfig());
        }
    }
}
