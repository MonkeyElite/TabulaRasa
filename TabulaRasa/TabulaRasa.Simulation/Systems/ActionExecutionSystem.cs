using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public class ActionExecutionSystem : ISystem
    {
        public string Name => "Action Execution System";
        public int Order => 3;

        public void Execute(SimulationContext context)
        {
            WorldState world = (WorldState)context.WorldState;

            foreach (var agent in world.Agents)
            {
                if (agent.PendingDecision is null)
                {
                    continue;
                }

                switch (agent.PendingDecision.ActionType)
                {
                    case AgentActionType.Eat:
                        ExecuteEat(world, agent);
                        break;

                    case AgentActionType.Wander:
                        ExecuteWander(agent);
                        break;
                }

                agent.PendingDecision = null;
            }
        }

        private static void ExecuteEat(WorldState world, AgentEntity agent)
        {
            var food = WorldQueries.FindAvailableFoodAt(world, agent.Position);

            if (food is null)
            {
                return;
            }

            food.IsConsumed = true;
            agent.Hunger = Math.Max(0, agent.Hunger - 5);
        }

        private static void ExecuteWander(AgentEntity agent)
        {
            agent.Position = agent.Position == "A" ? "B" : "A";
        }
    }
}
