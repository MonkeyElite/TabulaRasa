using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Needs;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Mutation;
using TabulaRasa.World.Queries;

namespace TabulaRasa.Simulation.Actions.Resolution
{
    public sealed class ActionResolver
    {
        private readonly WorldMutationService _mutations;

        public ActionResolver()
            : this(new WorldMutationService())
        {
        }

        public ActionResolver(WorldMutationService mutations)
        {
            _mutations = mutations;
        }

        public ActionResult Resolve(SimulationState state, ActionRequest request)
        {
            return request.ActionType switch
            {
                AgentActionType.Eat => ResolveEat(state, request),
                AgentActionType.Drink => ResolveDrink(state, request),
                AgentActionType.Rest => ResolveRest(state, request),
                AgentActionType.Wander => ResolveWander(state, request),
                AgentActionType.None => new ActionResult(request.AgentId, request.ActionType, true),
                _ => new ActionResult(request.AgentId, request.ActionType, false, "Unsupported action type.")
            };
        }

        private ActionResult ResolveEat(SimulationState state, ActionRequest request)
        {
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            AgentState? agentState = state.GetAgentById(request.AgentId);

            if (agentEntity is null || agentState is null || request.TargetId is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Eat action could not be resolved.");
            }

            FoodEntity? food = SpatialQueries.FindAvailableFoodAtInteractionPoint(
                state.World,
                agentEntity.Position,
                request.TargetId);

            if (food is null)
            {
                return new ActionResult(
                    request.AgentId,
                    request.ActionType,
                    false,
                    "Target food became unavailable before resolution.");
            }

            WorldMutationResult mutation = _mutations.TryConsumeFood(state.World, food.Id);

            if (!mutation.Succeeded)
            {
                return new ActionResult(
                    request.AgentId,
                    request.ActionType,
                    false,
                    mutation.Reason ?? "Target food could not be consumed.");
            }

            NeedSystem.ApplyEat(agentState.NeedState);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private static ActionResult ResolveDrink(SimulationState state, ActionRequest request)
        {
            AgentState? agentState = state.GetAgentById(request.AgentId);

            if (agentState is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Drink action could not be resolved.");
            }

            NeedSystem.ApplyDrink(agentState.NeedState);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private static ActionResult ResolveRest(SimulationState state, ActionRequest request)
        {
            AgentState? agentState = state.GetAgentById(request.AgentId);

            if (agentState is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Rest action could not be resolved.");
            }

            NeedSystem.ApplyRest(agentState.NeedState);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private static ActionResult ResolveWander(SimulationState state, ActionRequest request)
        {
            return new ActionResult(
                request.AgentId,
                request.ActionType,
                false,
                "Wander requires route planning before execution.");
        }
    }
}
