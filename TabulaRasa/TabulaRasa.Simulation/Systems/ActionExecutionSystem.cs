using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public class ActionExecutionSystem : ISystem
    {
        public string Name => "Action Execution System";
        public SimulationPhase Phase => SimulationPhase.Execution;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState?.PendingIntent is null)
                {
                    continue;
                }

                AgentIntent intent = agentState.PendingIntent;

                if (!ValidateIntent(world, agentEntity, intent))
                {
                    agentState.PendingIntent = null;
                    continue;
                }

                switch (intent.ActionType)
                {
                    case AgentActionType.Eat:
                        ExecuteEat(world, agentEntity, agentState, intent.TargetId);
                        break;

                    case AgentActionType.Wander:
                        ExecuteWander(agentEntity);
                        break;
                }

                agentState.PendingIntent = null;
            }
        }

        private static bool ValidateIntent(WorldState world, AgentEntity agentEntity, AgentIntent intent)
        {
            if (intent.AgentId != agentEntity.Id)
            {
                return false;
            }

            return intent.ActionType switch
            {
                AgentActionType.Eat => intent.TargetId is not null
                    && SpatialQueries.FindAvailableFoodAt(world, agentEntity.Position, intent.TargetId) is not null,
                AgentActionType.Wander => true,
                AgentActionType.None => true,
                _ => false
            };
        }

        private static void ExecuteEat(
            WorldState world,
            AgentEntity agentEntity,
            AgentState agentState,
            string? targetId)
        {
            if (targetId is null)
            {
                return;
            }

            FoodEntity? food = SpatialQueries.FindAvailableFoodAt(world, agentEntity.Position, targetId);

            if (food is null)
            {
                return;
            }

            food.IsConsumed = true;
            agentState.NeedState.Hunger = Math.Max(0, agentState.NeedState.Hunger - 5);
        }

        private static void ExecuteWander(AgentEntity agent)
        {
            agent.Position = agent.Position == new WorldPosition(1, 1) ? new WorldPosition(1, 2) : new WorldPosition(1, 1);
        }
    }
}
