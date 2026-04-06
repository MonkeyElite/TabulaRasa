using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.World.Entities
{
    public sealed class FoodEntity : IBaseEntity
    {
        public required string Id { get; init; }
        public required WorldPosition Position { get; set; }
        public bool IsConsumed { get; set; }
    }
}
