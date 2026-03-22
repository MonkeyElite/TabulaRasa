namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentDecision(AgentActionType ActionType, string? TargetId);
}
