using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.World.Queries
{
    public static class SpatialQueries
    {
        public static FoodEntity? FindAvailableFoodAt(WorldState world, WorldPosition position)
        {
            return world.Foods.FirstOrDefault(f => !f.IsConsumed && f.Position == position);
        }

        public static bool IsFoodAt(WorldState world, WorldPosition position)
        {
            return world.Foods.Any(f => !f.IsConsumed && f.Position == position);
        }
    }
}
