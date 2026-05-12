using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial.Footprints;

namespace TabulaRasa.Abstractions.Spatial
{
    public interface ISpatialEntity : IBaseEntity
    {
        public EntityFootprint Footprint { get; }
    }
}
