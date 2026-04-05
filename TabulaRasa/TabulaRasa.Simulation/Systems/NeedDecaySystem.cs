using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class NeedDecaySystem: ISystem
    {
        public string Name => "Need Decay System";
        public SimulationPhase Phase => SimulationPhase.PreUpdate;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState == null)
                {
                    continue;
                }

                agentState.NeedState.Hunger += 1;
            }
        }
    }
}
