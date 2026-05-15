namespace TabulaRasa.Simulation.Tasks.Definitions
{
    public sealed record TaskPreconditionResult(bool Succeeded, string? FailureReason = null)
    {
        public static TaskPreconditionResult Success { get; } = new(true);

        public static TaskPreconditionResult Failure(string reason)
        {
            return new TaskPreconditionResult(false, reason);
        }
    }
}
