using TabulaRasa.Abstractions.Execution;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public class ReportingSystem : ISystem
    {
        public string Name => "Reporting System";
        public int Order => 4;

        public void Execute(SimulationContext context)
        {
            WorldState world = (WorldState)context.WorldState;

            Console.WriteLine($"Tick {context.SimulationTime.Tick}");

            foreach (var agent in world.Agents)
            {
                Console.WriteLine(
                    $"  Agent {agent.Id} | Pos={agent.Position} | Hunger={agent.Hunger}");
            }

            var remainingFood = world.Foods.Count(f => !f.IsConsumed);
            Console.WriteLine($"  Remaining food: {remainingFood}");
            Console.WriteLine();
        }
    }
}
