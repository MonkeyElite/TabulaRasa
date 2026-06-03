namespace TabulaRasa.Abstractions.Agents.Actions
{
    public sealed record ActionRequest(
        string AgentId,
        AgentActionType ActionType,
        string? TargetId,
        string? ContextKey = null,
        string? SelectedGoal = null,
        string? TargetType = null,
        string? Channel = null,
        AgentNeedsSnapshot? NeedsBefore = null,
        string? SourceTaskId = null,
        string? SourceGoalId = null,
        bool IsMovementOnly = false);
}
