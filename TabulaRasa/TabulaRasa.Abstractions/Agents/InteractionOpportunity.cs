using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Agents
{
    public sealed record InteractionOpportunity(
        AgentActionType ActionType,
        string? TargetId,
        WorldPosition TargetPosition,
        string? SourceEntityId = null,
        PerceptionChannel Channel = PerceptionChannel.Sight,
        float Relevance = 0);
}
