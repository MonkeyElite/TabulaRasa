using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;

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

            return request.ActionType switch
            {
                AgentActionType.Eat => ValidateEat(state, agentEntity, request),
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
            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Eat action requires a target.");
            }

            FoodEntity? food = SpatialQueries.FindAvailableFoodAtInteractionPoint(
                state.World,
                agentEntity.Position,
                request.TargetId);

            if (food is null)
            {
                return ActionValidationResult.Invalid("Target food is unavailable or out of reach.");
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
