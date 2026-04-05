namespace TabulaRasa.Abstractions.Agents.Actions
{
    public sealed record ActionRequest(string AgentId, AgentActionType ActionType, string? TargetId);
}
