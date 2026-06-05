using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Services;

namespace TabulaRasa.Api.Persistence
{
    public sealed class NullSimulationPersistenceStore : ISimulationPersistenceStore
    {
        public bool IsDurable => false;
        public StorageOptions Options { get; } = new();

        public IReadOnlyList<SimulationRunBrowserDto> ListRuns(int offset, int limit, out int total)
        {
            total = 0;
            return [];
        }

        public IReadOnlyList<SimulationCheckpointSummaryDto> ListCheckpoints(string runId)
        {
            return [];
        }

        public void UpsertRun(SimulationSummaryDto summary, SimulationConfigDto config, string? sourceSimulationId = null, long? sourceTick = null)
        {
        }

        public SaveSimulationResponseDto SaveCheckpoint(string runId, SimulationStateCheckpointDto checkpoint)
        {
            return new SaveSimulationResponseDto(runId, checkpoint.Tick, checkpoint.CapturedAt, 0);
        }

        public void SaveTick(string runId, SimulationSnapshotDto snapshot)
        {
        }

        public SimulationStateCheckpointDto? GetNearestCheckpoint(string runId, long tick)
        {
            return null;
        }

        public SimulationStateCheckpointDto? GetLatestCheckpoint(string runId)
        {
            return null;
        }

        public ScenarioExportDto SaveScenario(string name, SimulationDraftDto scenario, Dictionary<string, string[]> validationErrors)
        {
            return new ScenarioExportDto(name, 1, DateTimeOffset.UtcNow, scenario);
        }

        public RetentionResultDto ApplyRetention()
        {
            return new RetentionResultDto(0, 0, 0, 0, 0);
        }

        public bool DeleteRun(string runId)
        {
            return false;
        }
    }
}
