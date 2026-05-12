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
    }
}
