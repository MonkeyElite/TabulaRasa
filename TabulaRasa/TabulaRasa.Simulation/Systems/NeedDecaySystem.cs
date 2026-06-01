using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Needs;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class NeedDecaySystem : ISystem
    {
        public string Name => "Need Decay System";
        public SimulationPhase Phase => SimulationPhase.PreUpdate;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;
            var decay = state.Config.EffectiveNeedDecay;

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState is null)
                {
                    continue;
                }

                NeedSystem.ApplyNeedDecay(
                    agentState.NeedState,
                    decay.HungerDelta,
                    decay.ThirstDelta,
                    decay.EnergyDelta);
            }
        }
    }
}
