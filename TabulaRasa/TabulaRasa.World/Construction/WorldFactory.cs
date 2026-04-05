using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.World.Construction
{
    public static class WorldFactory
    {
        public static WorldState Create(List<AgentEntity> agents, List<FoodEntity> foods)
        {
            WorldState worldState = new WorldState();
            worldState.Agents.AddRange(agents);
            worldState.Foods.AddRange(foods);

            return worldState;
        }
    }
}
