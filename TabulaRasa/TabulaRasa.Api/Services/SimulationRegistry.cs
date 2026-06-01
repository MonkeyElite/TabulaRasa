using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.Configuration;

namespace TabulaRasa.Api.Services
{
    public sealed class SimulationRegistry : IDisposable
    {
        private readonly object _sync = new();
        private readonly Dictionary<string, SimulationSession> _sessions = new(StringComparer.Ordinal);
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
        {
            Limits = limits;
            Create("Default", new SimulationConfig());
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
                SimulationSession clone = CreateCore(name, config);
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
            return true;
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

        private SimulationSession CreateCore(string? name, SimulationConfig config)
        {
            string id = Guid.NewGuid().ToString("N");
            SimulationSession session = new(id, name ?? $"Simulation {_sessions.Count + 1}", config);
            _sessions[id] = session;

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
