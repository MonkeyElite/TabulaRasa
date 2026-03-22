using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public class PlanningSystem : ISystem
    {
        public string Name => "Planning System";
        public int Order => 2;

        public void Execute(SimulationContext context)
        {
            WorldState world = (WorldState)context.WorldState;

            foreach(AgentEntity agent in world.Agents)
            {
                Boolean foodNearby = WorldQueries.IsFoodAt(world, agent.Position);
                AgentPerception perception = new AgentPerception(foodNearby);

                AgentSnapshot snapshot = new AgentSnapshot(agent.Id, agent.Hunger, agent.Position);

                agent.PendingDecision = agent.Mind.Decide(perception, snapshot);
            }
        }
    }
}
