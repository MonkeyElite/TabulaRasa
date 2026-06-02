using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Needs;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Mutation;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;

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
                AgentActionType.PickUpResource => ResolvePickUpResource(state, request),
                AgentActionType.DropResource => ResolveDropResource(state, request),
                AgentActionType.ConsumeResource => ResolveConsumeResource(state, request),
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

            if (agentEntity is null || agentState is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Eat action could not be resolved.");
            }

            if (agentEntity.Inventory.GetQuantity(ResourceDefinition.FoodId) == 0)
            {
                if (request.TargetId is null)
                {
                    return new ActionResult(request.AgentId, request.ActionType, false, "No food is available to eat.");
                }

                ResourceContainerEntity? container = SpatialQueries.FindAvailableFoodContainerAtInteractionPoint(
                    state.World,
                    agentEntity.Position,
                    request.TargetId);

                if (container is null)
                {
                    return new ActionResult(
                        request.AgentId,
                        request.ActionType,
                        false,
                        "Target resource container became unavailable before resolution.");
                }

                WorldMutationResult pickup = _mutations.TryPickUpResource(
                    state.World,
                    agentEntity.Id,
                    container.Id,
                    ResourceDefinition.FoodId,
                    quantity: 1);

                if (!pickup.Succeeded)
                {
                    return new ActionResult(
                        request.AgentId,
                        request.ActionType,
                        false,
                        pickup.Reason ?? "Food could not be picked up.");
                }
            }

            WorldMutationResult mutation = _mutations.TryConsumeResource(
                state.World,
                agentEntity.Inventory,
                ResourceDefinition.FoodId,
                quantity: 1);

            if (!mutation.Succeeded)
            {
                return new ActionResult(
                    request.AgentId,
                    request.ActionType,
                    false,
                    mutation.Reason ?? "Food could not be consumed.");
            }

            ApplyNeedEffects(
                agentState.NeedState,
                state.World.ResourceDefinitionsById[ResourceDefinition.FoodId].NeedEffects);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private ActionResult ResolvePickUpResource(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Pick up action requires a target.");
            }

            string resourceId = request.TargetType ?? ResourceDefinition.FoodId;
            WorldMutationResult mutation = _mutations.TryPickUpResource(
                state.World,
                request.AgentId,
                request.TargetId,
                resourceId,
                quantity: 1);

            return mutation.Succeeded
                ? new ActionResult(request.AgentId, request.ActionType, true)
                : new ActionResult(request.AgentId, request.ActionType, false, mutation.Reason);
        }

        private ActionResult ResolveDropResource(SimulationState state, ActionRequest request)
        {
            string resourceId = request.TargetType ?? ResourceDefinition.FoodId;
            WorldMutationResult mutation = _mutations.TryDropResource(
                state.World,
                request.AgentId,
                resourceId,
                quantity: 1);

            return mutation.Succeeded
                ? new ActionResult(request.AgentId, request.ActionType, true)
                : new ActionResult(request.AgentId, request.ActionType, false, mutation.Reason);
        }

        private ActionResult ResolveConsumeResource(SimulationState state, ActionRequest request)
        {
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            AgentState? agentState = state.GetAgentById(request.AgentId);
            string resourceId = request.TargetType ?? ResourceDefinition.FoodId;

            if (agentEntity is null || agentState is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Consume action could not be resolved.");
            }

            WorldMutationResult mutation = _mutations.TryConsumeResource(
                state.World,
                agentEntity.Inventory,
                resourceId,
                quantity: 1);

            if (!mutation.Succeeded)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, mutation.Reason);
            }

            if (state.World.ResourceDefinitionsById.TryGetValue(resourceId, out ResourceDefinition? definition))
            {
                ApplyNeedEffects(agentState.NeedState, definition.NeedEffects);
            }

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

        private static void ApplyNeedEffects(AgentNeedState needState, ResourceNeedEffects effects)
        {
            needState.Hunger = NeedSystem.ClampNeed(needState.Hunger + effects.HungerDelta);
            needState.Thirst = NeedSystem.ClampNeed(needState.Thirst + effects.ThirstDelta);
            needState.Energy = NeedSystem.ClampEnergy(needState.Energy + effects.EnergyDelta);
            needState.Fatigue = NeedSystem.ClampNeed(needState.Fatigue + effects.FatigueDelta);
        }
    }
}
