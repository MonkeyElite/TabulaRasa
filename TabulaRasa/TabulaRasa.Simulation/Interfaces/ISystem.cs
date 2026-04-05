using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Interfaces
{
    public interface ISystem
    {
        public string Name { get; }
        public SimulationPhase Phase { get; }
        public int Priority { get; }

        public void Execute(SimulationState state);
    }
}
