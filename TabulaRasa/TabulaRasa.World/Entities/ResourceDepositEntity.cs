using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Resources;

namespace TabulaRasa.World.Entities
{
    public sealed class ResourceDepositEntity : ISpatialEntity, IInteractableEntity
    {
        public required string Id { get; init; }
        public required WorldPosition Position { get; set; }
        public EntityFootprint Footprint { get; init; } = new(0.8f, 0.8f);
        public string ResourceId { get; set; } = ResourceDefinition.StoneId;
        public int Quantity { get; set; } = 5;
        public int MaxQuantity { get; set; } = 5;
        public bool IsEmpty => Quantity <= 0;

        public IReadOnlyList<InteractionPoint> InteractionPoints =>
            EcologyEntityInteraction.GetInteractionPoints(Position, Footprint);
    }
}
