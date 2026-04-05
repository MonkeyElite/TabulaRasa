using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public class PlanningSystem : ISystem
    {
        public string Name => "Planning System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;

            foreach(AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState == null)
                {
                    continue;
                }

                Boolean foodNearby = SpatialQueries.IsFoodAt(world, agentEntity.Position);
                AgentPerception perception = new AgentPerception(foodNearby);

                AgentSnapshot snapshot = new AgentSnapshot(agentEntity.Id, agentState.NeedState.Hunger, agentEntity.Position);

                agentState.PendingDecision = agentState.Mind.Decide(perception, snapshot);
            }
        }
    }
}
