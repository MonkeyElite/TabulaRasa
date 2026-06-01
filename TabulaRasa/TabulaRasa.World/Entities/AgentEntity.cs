using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.World.Entities
{
    public sealed class AgentEntity : ISpatialEntity, IDamageableEntity
    {
        public required string Id { get; init; }
        public required WorldPosition Position { get; set; }
        public EntityFootprint Footprint { get; init; } = new(0.8f, 0.8f);
        public EntityHealth Health { get; init; } = new(maximum: 10);
    }
}
