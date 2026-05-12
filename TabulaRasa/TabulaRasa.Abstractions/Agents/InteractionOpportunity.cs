using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Agents
{
    public sealed record InteractionOpportunity(
        AgentActionType ActionType,
        string? TargetId,
        WorldPosition TargetPosition);
}
