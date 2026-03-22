using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.Agents.Minds;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Composition
{
    public static class MinimalSimulationFactory
    {
        public static (WorldState World, IReadOnlyList<ISystem> Systems) Create()
        {
            var world = new WorldState();

            world.Agents.Add(new AgentEntity
            {
                Id = "agent-1",
                Position = "A",
                Hunger = 0,
                Mind = new DefaultAgentMind()
            });

            world.Foods.Add(new FoodEntity
            {
                Id = "food-1",
                Position = "A",
                IsConsumed = false
            });

            ISystem[] systems =
            [
                new NeedDecaySystem(),
            new PlanningSystem(),
            new ActionExecutionSystem(),
            new ReportingSystem()
            ];

            return (world, systems);
        }
    }
}
