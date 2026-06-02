namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentIntent(
        string AgentId,
        AgentActionType ActionType,
        string? TargetId,
        string? ContextKey = null,
        string? SelectedGoal = null,
        string? TargetType = null,
        string? Channel = null,
        AgentNeedsSnapshot? NeedsBefore = null);
}
