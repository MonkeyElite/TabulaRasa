using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Spatial.Grid;

namespace TabulaRasa.World.State
{
    public sealed class WorldState : IWorldState
    {
        public WorldState()
            : this(new GridMap(width: 10, height: 10))
        {
        }

        public WorldState(GridMap grid)
        {
            Grid = grid;
        }

        public GridMap Grid { get; }
        public List<AgentEntity> Agents { get; } = [];
        public List<FoodEntity> Foods { get; } = [];

        public IEnumerable<ISpatialEntity> SpatialEntities => Agents
            .Cast<ISpatialEntity>()
            .Concat(Foods);

        public ISpatialEntity? GetSpatialEntityById(string entityId)
        {
            return SpatialEntities.FirstOrDefault(entity => entity.Id == entityId);
        }
    }
}
