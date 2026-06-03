using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.World.Entities
{
    public sealed class WaterSourceEntity : ISpatialEntity, IInteractableEntity
    {
        public required string Id { get; init; }
        public required WorldPosition Position { get; set; }
        public EntityFootprint Footprint { get; init; } = new(0.8f, 0.8f);
        public float CurrentVolume { get; set; } = 5;
        public float MaxVolume { get; set; } = 10;
        public float RefillPerRainTick { get; set; } = 0.5f;
        public float EvaporationPerHeatTick { get; set; } = 0.25f;
        public bool IsAvailable => CurrentVolume > 0;

        public IReadOnlyList<InteractionPoint> InteractionPoints =>
            EcologyEntityInteraction.GetInteractionPoints(Position, Footprint);
    }
}
