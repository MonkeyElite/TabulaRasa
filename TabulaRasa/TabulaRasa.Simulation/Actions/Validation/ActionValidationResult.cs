namespace TabulaRasa.Simulation.Actions.Validation
{
    public sealed record ActionValidationResult(bool IsValid, string? FailureReason = null)
    {
        public static ActionValidationResult Valid { get; } = new(true);

        public static ActionValidationResult Invalid(string failureReason)
        {
            return new ActionValidationResult(false, failureReason);
        }
    }
}
