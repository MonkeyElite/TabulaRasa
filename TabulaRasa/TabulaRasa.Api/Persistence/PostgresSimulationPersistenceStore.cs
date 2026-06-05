using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Persistence.Entities;
using TabulaRasa.Api.Services;

namespace TabulaRasa.Api.Persistence
{
    public sealed class PostgresSimulationPersistenceStore : ISimulationPersistenceStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IDbContextFactory<SimulationDbContext> _dbContextFactory;

        public PostgresSimulationPersistenceStore(
            IDbContextFactory<SimulationDbContext> dbContextFactory,
            IOptions<StorageOptions> options)
        {
            _dbContextFactory = dbContextFactory;
            Options = options.Value;
        }

        public bool IsDurable => true;
        public StorageOptions Options { get; }

        public IReadOnlyList<SimulationRunBrowserDto> ListRuns(int offset, int limit, out int total)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            total = db.Runs.Count();
            int safeOffset = Math.Max(0, offset);
            int safeLimit = Math.Clamp(limit, 1, 200);
            List<SimulationRunEntity> runs = db.Runs
                .AsNoTracking()
                .OrderByDescending(run => run.UpdatedAt)
                .Skip(safeOffset)
                .Take(safeLimit)
                .ToList();

            return runs.Select(run =>
            {
                long? minTick = db.TickSummaries
                    .Where(summary => summary.RunId == run.Id)
                    .Select(summary => (long?)summary.Tick)
                    .Min();
                long? maxTick = db.TickSummaries
                    .Where(summary => summary.RunId == run.Id)
                    .Select(summary => (long?)summary.Tick)
                    .Max();

                if (minTick is null)
                {
                    minTick = db.Checkpoints
                        .Where(checkpoint => checkpoint.RunId == run.Id)
                        .Select(checkpoint => (long?)checkpoint.Tick)
                        .Min();
                }

                if (maxTick is null)
                {
                    maxTick = db.Checkpoints
                        .Where(checkpoint => checkpoint.RunId == run.Id)
                        .Select(checkpoint => (long?)checkpoint.Tick)
                        .Max();
                }

                return new SimulationRunBrowserDto(
                    run.Id,
                    run.Name,
                    run.Status,
                    run.CurrentTick,
                    minTick ?? run.CurrentTick,
                    maxTick ?? run.CurrentTick,
                    run.AgentCount,
                    run.AliveAgentCount,
                    run.DeadAgentCount,
                    run.StorageBytes,
                    run.CheckpointBytes,
                    run.EventBytes,
                    run.CreatedAt,
                    run.UpdatedAt,
                    run.SourceSimulationId,
                    run.SourceTick);
            }).ToList();
        }

        public IReadOnlyList<SimulationCheckpointSummaryDto> ListCheckpoints(string runId)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            return db.Checkpoints
                .AsNoTracking()
                .Where(checkpoint => checkpoint.RunId == runId)
                .OrderBy(checkpoint => checkpoint.Tick)
                .Select(checkpoint => new SimulationCheckpointSummaryDto(
                    checkpoint.RunId,
                    checkpoint.Tick,
                    checkpoint.PayloadBytes,
                    checkpoint.IsCompressed,
                    checkpoint.CreatedAt))
                .ToList();
        }

        public void UpsertRun(
            SimulationSummaryDto summary,
            SimulationConfigDto config,
            string? sourceSimulationId = null,
            long? sourceTick = null)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            string configJson = Serialize(config);
            SimulationRunEntity? run = db.Runs.Find(summary.SimulationId);
            if (run is null)
            {
                run = new SimulationRunEntity
                {
                    Id = summary.SimulationId,
                    CreatedAt = summary.CreatedAt,
                    SourceSimulationId = sourceSimulationId,
                    SourceTick = sourceTick
                };
                db.Runs.Add(run);
            }

            run.Name = summary.Name;
            run.Status = summary.Status;
            run.CurrentTick = summary.CurrentTick;
            run.AgentCount = summary.AgentCount;
            run.AliveAgentCount = summary.AliveAgentCount;
            run.DeadAgentCount = summary.DeadAgentCount;
            run.FoodCount = summary.FoodCount;
            run.GridWidth = summary.GridWidth;
            run.GridHeight = summary.GridHeight;
            run.ConfigJson = configJson;
            run.UpdatedAt = summary.UpdatedAt;
            if (!string.IsNullOrWhiteSpace(sourceSimulationId))
            {
                run.SourceSimulationId = sourceSimulationId;
                run.SourceTick = sourceTick;
            }

            db.SaveChanges();
            RefreshStorageTotals(db, run.Id);
        }

        public SaveSimulationResponseDto SaveCheckpoint(string runId, SimulationStateCheckpointDto checkpoint)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            string payloadJson = Serialize(checkpoint);
            long bytes = Encoding.UTF8.GetByteCount(payloadJson);
            SimulationCheckpointEntity? entity = db.Checkpoints
                .SingleOrDefault(candidate => candidate.RunId == runId && candidate.Tick == checkpoint.Tick);

            if (entity is null)
            {
                entity = new SimulationCheckpointEntity
                {
                    RunId = runId,
                    Tick = checkpoint.Tick,
                    CreatedAt = checkpoint.CapturedAt
                };
                db.Checkpoints.Add(entity);
            }

            entity.PayloadJson = payloadJson;
            entity.PayloadBytes = bytes;
            entity.IsCompressed = false;
            entity.CompressedPayload = null;
            entity.CreatedAt = checkpoint.CapturedAt;

            SimulationRunEntity? run = db.Runs.Find(runId);
            if (run is not null)
            {
                run.CurrentTick = Math.Max(run.CurrentTick, checkpoint.Tick);
                run.Status = checkpoint.Lifecycle;
                run.UpdatedAt = checkpoint.CapturedAt;
            }

            db.SaveChanges();
            RefreshStorageTotals(db, runId);
            return new SaveSimulationResponseDto(runId, checkpoint.Tick, checkpoint.CapturedAt, bytes);
        }

        public void SaveTick(string runId, SimulationSnapshotDto snapshot)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            db.Events
                .Where(simulationEvent => simulationEvent.RunId == runId && simulationEvent.Tick == snapshot.Tick)
                .ExecuteDelete();
            db.TickSummaries
                .Where(summary => summary.RunId == runId && summary.Tick == snapshot.Tick)
                .ExecuteDelete();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (SimulationEventDto simulationEvent in snapshot.Events)
            {
                string metadataJson = Serialize(simulationEvent.Metadata);
                db.Events.Add(new SimulationEventEntity
                {
                    RunId = runId,
                    Tick = simulationEvent.Tick,
                    Sequence = simulationEvent.Sequence,
                    Type = simulationEvent.Type,
                    SourceSystem = simulationEvent.SourceSystem,
                    Message = simulationEvent.Message,
                    EntityId = simulationEvent.EntityId,
                    MetadataJson = metadataJson,
                    PayloadBytes = Encoding.UTF8.GetByteCount(metadataJson)
                        + Encoding.UTF8.GetByteCount(simulationEvent.Type)
                        + Encoding.UTF8.GetByteCount(simulationEvent.SourceSystem)
                        + Encoding.UTF8.GetByteCount(simulationEvent.Message),
                    CreatedAt = now
                });
            }

            db.TickSummaries.Add(new SimulationTickSummaryEntity
            {
                RunId = runId,
                Tick = snapshot.Tick,
                DurationMilliseconds = snapshot.Diagnostics?.DurationMilliseconds ?? 0,
                EventCount = snapshot.Events.Count,
                PopulationCount = snapshot.PopulationCount,
                AliveAgentCount = snapshot.AliveAgentCount,
                DeadAgentCount = snapshot.DeadAgentCount,
                ResourceContainerCount = snapshot.ResourceContainers.Count,
                CreatedAt = now
            });

            SimulationRunEntity? run = db.Runs.Find(runId);
            if (run is not null)
            {
                run.CurrentTick = Math.Max(run.CurrentTick, snapshot.Tick);
                run.AgentCount = snapshot.PopulationCount;
                run.AliveAgentCount = snapshot.AliveAgentCount;
                run.DeadAgentCount = snapshot.DeadAgentCount;
                run.FoodCount = snapshot.ResourceContainers.Count;
                run.GridWidth = snapshot.Grid.Width;
                run.GridHeight = snapshot.Grid.Height;
                run.UpdatedAt = now;
            }

            db.SaveChanges();
            RefreshStorageTotals(db, runId);
        }

        public SimulationStateCheckpointDto? GetNearestCheckpoint(string runId, long tick)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            SimulationCheckpointEntity? entity = db.Checkpoints
                .AsNoTracking()
                .Where(checkpoint => checkpoint.RunId == runId && checkpoint.Tick <= tick)
                .OrderByDescending(checkpoint => checkpoint.Tick)
                .FirstOrDefault();

            return entity is null ? null : Deserialize<SimulationStateCheckpointDto>(entity.PayloadJson);
        }

        public SimulationStateCheckpointDto? GetLatestCheckpoint(string runId)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            SimulationCheckpointEntity? entity = db.Checkpoints
                .AsNoTracking()
                .Where(checkpoint => checkpoint.RunId == runId)
                .OrderByDescending(checkpoint => checkpoint.Tick)
                .FirstOrDefault();

            return entity is null ? null : Deserialize<SimulationStateCheckpointDto>(entity.PayloadJson);
        }

        public ScenarioExportDto SaveScenario(
            string name,
            SimulationDraftDto scenario,
            Dictionary<string, string[]> validationErrors)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            ScenarioExportDto export = new(name, 1, now, scenario);
            db.Scenarios.Add(new SimulationScenarioEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Version = export.Version,
                ScenarioJson = Serialize(export),
                IsValid = validationErrors.Count == 0,
                ValidationErrorsJson = Serialize(validationErrors),
                CreatedAt = now,
                UpdatedAt = now
            });

            db.SaveChanges();
            return export;
        }

        public RetentionResultDto ApplyRetention()
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            DateTimeOffset? olderThan = Options.RetainRunsForDays is null
                ? null
                : DateTimeOffset.UtcNow.AddDays(-Options.RetainRunsForDays.Value);
            HashSet<string> protectedLatestRunIds = Options.RetainLatestRuns is null
                ? []
                : db.Runs
                    .AsNoTracking()
                    .OrderByDescending(run => run.UpdatedAt)
                    .Take(Math.Max(0, Options.RetainLatestRuns.Value))
                    .Select(run => run.Id)
                    .ToHashSet(StringComparer.Ordinal);

            List<SimulationRunEntity> runsToDelete = db.Runs
                .Where(run => (olderThan == null || run.UpdatedAt < olderThan) && !protectedLatestRunIds.Contains(run.Id))
                .ToList();
            int deletedRuns = runsToDelete.Count;
            int deletedCheckpoints = 0;
            int deletedEvents = 0;
            int deletedTickSummaries = 0;
            long removedBytes = 0;

            foreach (SimulationRunEntity run in runsToDelete)
            {
                deletedCheckpoints += db.Checkpoints.Count(checkpoint => checkpoint.RunId == run.Id);
                deletedEvents += db.Events.Count(simulationEvent => simulationEvent.RunId == run.Id);
                deletedTickSummaries += db.TickSummaries.Count(summary => summary.RunId == run.Id);
                removedBytes += run.StorageBytes;
            }

            db.Runs.RemoveRange(runsToDelete);
            db.SaveChanges();
            return new RetentionResultDto(deletedRuns, deletedCheckpoints, deletedEvents, deletedTickSummaries, removedBytes);
        }

        public bool DeleteRun(string runId)
        {
            using SimulationDbContext db = _dbContextFactory.CreateDbContext();
            SimulationRunEntity? run = db.Runs.Find(runId);
            if (run is null)
            {
                return false;
            }

            db.Runs.Remove(run);
            db.SaveChanges();
            return true;
        }

        private static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }

        private static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new InvalidOperationException("Stored simulation payload could not be deserialized.");
        }

        private static void RefreshStorageTotals(SimulationDbContext db, string runId)
        {
            SimulationRunEntity? run = db.Runs.Find(runId);
            if (run is null)
            {
                return;
            }

            long checkpointBytes = db.Checkpoints
                .Where(checkpoint => checkpoint.RunId == runId)
                .Sum(checkpoint => (long?)checkpoint.PayloadBytes) ?? 0;
            long eventBytes = db.Events
                .Where(simulationEvent => simulationEvent.RunId == runId)
                .Sum(simulationEvent => (long?)simulationEvent.PayloadBytes) ?? 0;
            run.CheckpointBytes = checkpointBytes;
            run.EventBytes = eventBytes;
            run.StorageBytes = checkpointBytes + eventBytes;
            db.SaveChanges();
        }
    }
}
