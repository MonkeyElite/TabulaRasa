using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Resources;

namespace TabulaRasa.World.Entities
{
    public sealed class AgentEntity : ISpatialEntity, IDamageableEntity
    {
        public required string Id { get; init; }
        public required WorldPosition Position { get; set; }
        public EntityFootprint Footprint { get; init; } = new(0.8f, 0.8f);
        public EntityHealth Health { get; init; } = new(maximum: 10);
        public Inventory Inventory { get; init; } = new()
        {
            MaxSlots = 8,
            MaxWeight = 10
        };
        public bool IsDead { get; set; }
        public string SpeciesId { get; set; } = "human";
        public int AgeTicks { get; set; }
        public long BornTick { get; set; }
        public List<string> ParentIds { get; init; } = [];
        public List<string> OffspringIds { get; init; } = [];
        public long? LastReproducedTick { get; set; }
        public long? DeathTick { get; set; }
        public string? DeathCause { get; set; }
    }
}
