using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Social;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class SocialSystem : ISystem
    {
        public string Name => "Social System";
        public SimulationPhase Phase => SimulationPhase.PreUpdate;
        public int Priority => 2;

        public void Execute(SimulationState state)
        {
            SocialService.EnsureDefaultGroups(state);
        }
    }
}
