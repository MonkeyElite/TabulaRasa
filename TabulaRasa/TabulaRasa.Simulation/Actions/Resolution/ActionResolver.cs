using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;

namespace TabulaRasa.Simulation.Actions.Resolution
{
    public sealed class ActionResolver
    {
        public ActionResult Resolve(SimulationState state, ActionRequest request)
        {
            return request.ActionType switch
            {
                AgentActionType.Eat => ResolveEat(state, request),
                AgentActionType.Wander => ResolveWander(state, request),
                AgentActionType.None => new ActionResult(request.AgentId, request.ActionType, true),
                _ => new ActionResult(request.AgentId, request.ActionType, false, "Unsupported action type.")
            };
        }

        private static ActionResult ResolveEat(SimulationState state, ActionRequest request)
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

            food.IsConsumed = true;
            agentState.NeedState.Hunger = Math.Max(0, agentState.NeedState.Hunger - 5);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private static ActionResult ResolveWander(SimulationState state, ActionRequest request)
        {
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);

            if (agentEntity is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Agent does not exist.");
            }

            GridCell currentCell = SpatialQueries.GetCurrentCell(state.World, agentEntity.Position);
            IReadOnlyList<GridCell> destinations = state.World.Grid.GetTraversableAdjacentCells(currentCell);

            if (destinations.Count == 0)
            {
                return new ActionResult(
                    request.AgentId,
                    request.ActionType,
                    false,
                    "Agent has no traversable adjacent cell to wander to.");
            }

            GridCell destination = destinations[0];
            agentEntity.Position = new WorldPosition(destination.X + 0.5f, destination.Y + 0.5f);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }
    }
}
