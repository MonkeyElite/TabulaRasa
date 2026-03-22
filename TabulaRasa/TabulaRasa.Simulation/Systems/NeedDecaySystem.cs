using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class NeedDecaySystem: ISystem
    {
        public string Name => "Need Decay System";
        public int Order => 1;

        public void Execute(SimulationContext context)
        {
            WorldState world = (WorldState)context.WorldState;

            foreach (AgentEntity agent in world.Agents)
            {
                agent.Hunger += 1;
            }
        }
    }
}
