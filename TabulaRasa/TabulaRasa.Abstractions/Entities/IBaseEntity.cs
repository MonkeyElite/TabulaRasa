using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Entities
{
    public interface IBaseEntity
    {
        public string Id { get; init; }
        public WorldPosition Position { get; set; }
    }
}
