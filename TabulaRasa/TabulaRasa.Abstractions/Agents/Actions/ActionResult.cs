namespace TabulaRasa.Abstractions.Agents.Actions
{
    public sealed record ActionResult(
        string AgentId,
        AgentActionType ActionType,
        bool Succeeded,
        string? Reason = null,
        string? TargetId = null,
        string? ContextKey = null,
        string? SelectedGoal = null,
        AgentNeedsSnapshot? NeedsBefore = null,
        AgentNeedsSnapshot? NeedsAfter = null,
        float? OutcomeScore = null);
}
