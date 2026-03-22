using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Execution
{
    public sealed class SimulationContext
    {
        public IWorldState WorldState { get; }
        public SimulationTime SimulationTime { get; }

        public SimulationContext(IWorldState WorldState, SimulationTime Time)
        {
            this.WorldState = WorldState;
            SimulationTime = Time;
        }
    }
}
