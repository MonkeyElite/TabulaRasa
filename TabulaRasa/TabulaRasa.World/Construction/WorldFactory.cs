using TabulaRasa.World.Entities;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.World.Construction
{
    public static class WorldFactory
    {
        public static WorldState Create(
            List<AgentEntity> agents,
            List<ResourceContainerEntity> resourceContainers,
            GridMap? grid = null)
        {
            WorldState worldState = grid is null
                ? new WorldState()
                : new WorldState(grid);

            worldState.Agents.AddRange(agents);
            worldState.ResourceContainers.AddRange(resourceContainers);

            return worldState;
        }
    }
}
