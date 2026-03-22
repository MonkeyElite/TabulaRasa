namespace TabulaRasa.World.State
{
    public static class WorldQueries
    {
        public static FoodEntity? FindAvailableFoodAt(WorldState world, string position)
        {
            return world.Foods.FirstOrDefault(f => !f.IsConsumed && f.Position == position);
        }

        public static bool IsFoodAt(WorldState world, string position)
        {
            return world.Foods.Any(f => !f.IsConsumed && f.Position == position);
        }
    }
}
