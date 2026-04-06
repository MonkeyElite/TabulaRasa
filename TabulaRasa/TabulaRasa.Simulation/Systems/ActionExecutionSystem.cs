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

                if (agentState is null)
                {
                    // Log the fact that we have an agent in the world that doesn't have a corresponding agent state in the simulation state. This shouldn't happen, but if it does, we want to know about it.
                    continue;
                }

                if (agentState.PendingDecision is null)
                {
                    // Log the fact that the agent doesn't have a pending decision. This could be due to a variety of reasons, such as the agent not having had enough time to think through its next action, or the agent being in a state where it can't make a decision (e.g., it's asleep or incapacitated). For now, we'll just skip executing an action for this agent if it doesn't have a pending decision.
                    continue;
                }

                if (!ValidateAction())
                {
                    // Log the fact that the agent's pending action is invalid. This could be due to a variety of reasons, such as the agent not having the necessary resources to perform the action, or the action being outside of the agent's capabilities. For now, we'll just skip executing the action if it's invalid.
                    continue;
                }

                switch (agentState.PendingDecision.ActionType)
                {
                    case AgentActionType.Eat:
                        ExecuteEat(world, agentEntity, agentState);
                        break;

                    case AgentActionType.Wander:
                        ExecuteWander(agentEntity);
                        break;
                }

                agentState.PendingDecision = null;
            }
        }

        private static bool ValidateAction()
        {
            // TODO: Add validation to check whether or not agent is allowed and capable of performing action.
            return true;
        }

        private static void ExecuteEat(WorldState world, AgentEntity agentEntity, AgentState agentState)
        {
            var food = SpatialQueries.FindAvailableFoodAt(world, agentEntity.Position);

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
