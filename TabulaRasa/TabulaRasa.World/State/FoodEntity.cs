namespace TabulaRasa.World.State
{
    public sealed class FoodEntity
    {
        public required string Id { get; init; }
        public required string Position { get; init; }
        public bool IsConsumed { get; set; }
    }
}
