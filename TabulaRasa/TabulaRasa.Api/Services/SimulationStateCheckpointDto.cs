using TabulaRasa.Api.Contracts;

namespace TabulaRasa.Api.Services
{
    public sealed record SimulationStateCheckpointDto(
        long Tick,
        string Lifecycle,
        SimulationConfigDto Config,
        SimulationSnapshotDto Snapshot,
        SimulationDraftDto Draft,
        DateTimeOffset CapturedAt);
}
