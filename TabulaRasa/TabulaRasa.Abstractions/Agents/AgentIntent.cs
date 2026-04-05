namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentIntent(string AgentId, AgentActionType ActionType, string? TargetId);
}
