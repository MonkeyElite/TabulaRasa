using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.World.Entities
{
    internal static class EcologyEntityInteraction
    {
        public static IReadOnlyList<InteractionPoint> GetInteractionPoints(
            WorldPosition position,
            EntityFootprint footprint)
        {
            float horizontalOffset = (footprint.Width / 2f) + 0.25f;
            float verticalOffset = (footprint.Height / 2f) + 0.25f;

            return
            [
                new(new WorldPosition(position.X - horizontalOffset, position.Y), position),
                new(new WorldPosition(position.X + horizontalOffset, position.Y), position),
                new(new WorldPosition(position.X, position.Y - verticalOffset), position),
                new(new WorldPosition(position.X, position.Y + verticalOffset), position)
            ];
        }
    }
}
