using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Services;

namespace TabulaRasa.Api.Persistence
{
    public interface ISimulationPersistenceStore
    {
        bool IsDurable { get; }
        StorageOptions Options { get; }
        IReadOnlyList<SimulationRunBrowserDto> ListRuns(int offset, int limit, out int total);
        IReadOnlyList<SimulationCheckpointSummaryDto> ListCheckpoints(string runId);
        void UpsertRun(SimulationSummaryDto summary, SimulationConfigDto config, string? sourceSimulationId = null, long? sourceTick = null);
        SaveSimulationResponseDto SaveCheckpoint(string runId, SimulationStateCheckpointDto checkpoint);
        void SaveTick(string runId, SimulationSnapshotDto snapshot);
        SimulationStateCheckpointDto? GetNearestCheckpoint(string runId, long tick);
        SimulationStateCheckpointDto? GetLatestCheckpoint(string runId);
        ScenarioExportDto SaveScenario(string name, SimulationDraftDto scenario, Dictionary<string, string[]> validationErrors);
        RetentionResultDto ApplyRetention();
        bool DeleteRun(string runId);
    }
}
