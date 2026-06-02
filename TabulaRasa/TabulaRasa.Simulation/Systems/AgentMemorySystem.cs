using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class AgentMemorySystem : ISystem
    {
        public string Name => "Agent Memory System";
        public SimulationPhase Phase => SimulationPhase.PreUpdate;
        public int Priority => 1;

        public void Execute(SimulationState state)
        {
            AgentMemoryService.Decay(state);
        }
    }
}
