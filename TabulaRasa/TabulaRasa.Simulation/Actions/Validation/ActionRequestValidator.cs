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
                AgentActionType.Wander => ActionValidationResult.Valid,
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

            FoodEntity? food = SpatialQueries.FindAvailableFoodAt(
                state.World,
                agentEntity.Position,
                request.TargetId);

            if (food is null)
            {
                return ActionValidationResult.Invalid("Target food is unavailable or out of reach.");
            }

            return ActionValidationResult.Valid;
        }
    }
}
