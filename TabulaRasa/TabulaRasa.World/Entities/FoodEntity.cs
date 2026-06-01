using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.World.Entities
{
    public sealed class FoodEntity : ISpatialEntity, IInteractableEntity, IDamageableEntity
    {
        public required string Id { get; init; }
        public required WorldPosition Position { get; set; }
        public EntityFootprint Footprint { get; init; } = new(0.5f, 0.5f);
        public EntityHealth Health { get; init; } = new(maximum: 1);
        public bool IsConsumed { get; set; }

        public IReadOnlyList<InteractionPoint> InteractionPoints
        {
            get
            {
                float horizontalOffset = (Footprint.Width / 2f) + 0.25f;
                float verticalOffset = (Footprint.Height / 2f) + 0.25f;

                return
                [
                    new(new WorldPosition(Position.X - horizontalOffset, Position.Y), Position),
                    new(new WorldPosition(Position.X + horizontalOffset, Position.Y), Position),
                    new(new WorldPosition(Position.X, Position.Y - verticalOffset), Position),
                    new(new WorldPosition(Position.X, Position.Y + verticalOffset), Position)
                ];
            }
        }
    }
}
