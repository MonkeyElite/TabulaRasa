using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Agents
{
    public sealed record PerceivedEntity(
        string EntityId,
        PerceivedEntityType EntityType,
        WorldPosition Position,
        bool IsInteractable);
}
