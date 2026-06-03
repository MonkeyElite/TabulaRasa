using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Resources;

namespace TabulaRasa.World.Entities
{
    public sealed class PlantEntity : ISpatialEntity, IInteractableEntity, IDamageableEntity
    {
        public required string Id { get; init; }
        public required WorldPosition Position { get; set; }
        public EntityFootprint Footprint { get; init; } = new(0.6f, 0.6f);
        public EntityHealth Health { get; init; } = new(maximum: 3);
        public string ResourceId { get; set; } = ResourceDefinition.FoodId;
        public int Yield { get; set; } = 1;
        public int MaxYield { get; set; } = 3;
        public int RegrowthTicks { get; set; } = 5;
        public int TicksUntilRegrowth { get; set; }
        public int DecayTicksAfterDepleted { get; set; } = 20;
        public int DepletedTicks { get; set; }
        public bool IsDecayed { get; set; }
        public bool IsHarvestable => !IsDecayed && Yield > 0;

        public IReadOnlyList<InteractionPoint> InteractionPoints =>
            EcologyEntityInteraction.GetInteractionPoints(Position, Footprint);
    }
}
