using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Kernel.Engine
{
    public sealed class SimulationEngine
    {
        private readonly List<ISystem> _systems = [];

        public SimulationEngine(IEnumerable<ISystem> systems)
        {
            _systems = systems.OrderBy(s => s.Order).ToList();
        }

        public void Run(IWorldState world, int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                SimulationContext context = new SimulationContext(world, new SimulationTime(tick));

                foreach (var system in _systems)
                {
                    system.Execute(context);
                }
            }
        }
    }
}
