using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;

namespace TabulaRasa.Simulation.Actions.Validation
{
    public sealed class ActionRequestValidator
    {
        public ActionValidationResult Validate(SimulationState state, ActionRequest request)
        {
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);

            if (agentEntity is null)
            {
                return ActionValidationResult.Invalid("Agent does not exist.");
            }

            if (state.GetAgentById(request.AgentId) is null)
            {
                return ActionValidationResult.Invalid("Agent state does not exist.");
            }

            if (agentEntity.IsDead)
            {
                return ActionValidationResult.Invalid("Agent is dead.");
            }

            return request.ActionType switch
            {
                AgentActionType.Eat => ValidateEat(state, agentEntity, request),
                AgentActionType.PickUpResource => ValidatePickUpResource(state, agentEntity, request),
                AgentActionType.DropResource => ActionValidationResult.Valid,
                AgentActionType.ConsumeResource => ActionValidationResult.Valid,
                AgentActionType.TransferResource => ActionValidationResult.Valid,
                AgentActionType.Drink => ActionValidationResult.Valid,
                AgentActionType.Rest => ActionValidationResult.Valid,
                AgentActionType.Wander => ValidateWander(state, agentEntity),
                AgentActionType.None => ActionValidationResult.Valid,
                _ => ActionValidationResult.Invalid("Unsupported action type.")
            };
        }

        private static ActionValidationResult ValidateEat(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (agentEntity.Inventory.GetQuantity(ResourceDefinition.FoodId) > 0)
            {
                return ActionValidationResult.Valid;
            }

            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Eat action requires carried food or a target.");
            }

            ResourceContainerEntity? container = SpatialQueries.FindAvailableFoodContainerAtInteractionPoint(
                state.World,
                agentEntity.Position,
                request.TargetId);

            if (container is null)
            {
                AgentMemoryService.MarkTargetUnavailable(
                    state,
                    request.AgentId,
                    request.TargetId,
                    "Target resource container is unavailable or out of reach.");
                return ActionValidationResult.Invalid("Target resource container is unavailable or out of reach.");
            }

            return ActionValidationResult.Valid;
        }

        private static ActionValidationResult ValidatePickUpResource(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Pick up action requires a target.");
            }

            ResourceContainerEntity? container = state.World.ResourceContainers.FirstOrDefault(candidate =>
                candidate.Id == request.TargetId && !candidate.IsEmpty);

            if (container is null)
            {
                return ActionValidationResult.Invalid("Target resource container is unavailable.");
            }

            if (SpatialQueries.FindNearestAvailableInteractionPoint(
                    container,
                    agentEntity.Position,
                    SpatialQueries.DefaultInteractionTolerance) is null)
            {
                return ActionValidationResult.Invalid("Target resource container is out of reach.");
            }

            return ActionValidationResult.Valid;
        }

        private static ActionValidationResult ValidateWander(SimulationState state, AgentEntity agentEntity)
        {
            bool hasDestination = state.World.Grid
                .GetAdjacentCells(SpatialQueries.GetCurrentCell(state.World, agentEntity.Position))
                .Where(cell => state.World.Grid.IsTraversable(cell))
                .Where(cell => !SpatialQueries.IsCellOccupied(state.World, cell, agentEntity.Id))
                .Any();

            return hasDestination
                ? ActionValidationResult.Valid
                : ActionValidationResult.Invalid("Agent has no available adjacent cell to wander to.");
        }
    }
}
