using TabulaRasa.Abstractions.Time;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Engine
{
    public sealed class SimulationEngine
    {
        private readonly List<ISystem> _systems = [];

        public SimulationEngine(IEnumerable<ISystem> systems)
        {
            _systems = systems.OrderBy(s => s.Phase).ToList();
        }

        public void Run(SimulationState state, int maxTicks)
        {
            for (int tick = 0; tick < maxTicks; tick++)
            {
                foreach (var system in _systems)
                {
                    system.Execute(state);
                }

                state.Time = new SimulationTime(state.Time.Tick + 1);
            }
        }
    }
}
