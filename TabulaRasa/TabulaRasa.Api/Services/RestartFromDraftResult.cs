using TabulaRasa.Api.Contracts;

namespace TabulaRasa.Api.Services
{
    public sealed class RestartFromDraftResult
    {
        private RestartFromDraftResult(
            bool succeeded,
            SimulationSnapshotDto? snapshot,
            Dictionary<string, string[]> errors)
        {
            Succeeded = succeeded;
            Snapshot = snapshot;
            Errors = errors;
        }

        public bool Succeeded { get; }
        public SimulationSnapshotDto? Snapshot { get; }
        public Dictionary<string, string[]> Errors { get; }

        public static RestartFromDraftResult Success(SimulationSnapshotDto snapshot)
        {
            return new RestartFromDraftResult(true, snapshot, []);
        }

        public static RestartFromDraftResult Failure(Dictionary<string, string[]> errors)
        {
            return new RestartFromDraftResult(false, null, errors);
        }
    }
}
