using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Agents
{
    public sealed record PerceivedEntity(
        string EntityId,
        PerceivedEntityType EntityType,
        WorldPosition Position,
        bool IsInteractable,
        PerceptionChannel Channel = PerceptionChannel.Sight,
        float Distance = 0,
        float Certainty = 1,
        float Relevance = 0);
}
