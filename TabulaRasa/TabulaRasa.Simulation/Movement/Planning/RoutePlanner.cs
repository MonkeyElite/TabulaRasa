using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Spatial.Navigation.Grid;

namespace TabulaRasa.Simulation.Movement.Planning
{
    public sealed class RoutePlanner
    {
        public const float DefaultArrivalTolerance = 0.05f;

        private readonly GridPathfinder _pathfinder;

        public RoutePlanner()
            : this(new GridPathfinder())
        {
        }

        public RoutePlanner(GridPathfinder pathfinder)
        {
            _pathfinder = pathfinder;
        }

        public RoutePlanningResult Plan(SimulationState state, ActionRequest request)
        {
            return request.ActionType switch
            {
                AgentActionType.Eat => PlanEatRoute(state, request),
                AgentActionType.PickUpResource => PlanResourceContainerRoute(state, request),
                AgentActionType.Wander => PlanWanderRoute(state, request),
                _ => RoutePlanningResult.NotNeeded()
            };
        }

        public MovementRoute? ReplanRoute(
            SimulationState state,
            ActiveMovement movement,
            out string failureReason)
        {
            failureReason = string.Empty;
            AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == movement.AgentId);

            if (agent is null)
            {
                failureReason = "Agent does not exist.";
                return null;
            }

            if (movement.RequestedAction is AgentActionType.Eat or AgentActionType.PickUpResource)
            {
                if (movement.TargetId is null)
                {
                    failureReason = $"{movement.RequestedAction} action requires a target.";
                    return null;
                }

                ResourceContainerEntity? container = state.World.ResourceContainers.FirstOrDefault(candidate =>
                    candidate.Id == movement.TargetId
                    && (movement.RequestedAction == AgentActionType.PickUpResource
                        ? !candidate.IsEmpty
                        : SpatialQueries.ContainerHasFood(candidate)));

                if (container is null)
                {
                    failureReason = "Target resource container is unavailable.";
                    return null;
                }

                RouteCandidate? candidate = FindBestRouteToInteractionPoint(state, agent, container);

                if (candidate is null)
                {
                    failureReason = "Target food is unreachable.";
                    return null;
                }

                return candidate.Route;
            }

            MovementRoute? route = FindRouteToExactDestination(
                state,
                agent,
                movement.Route.Destination);

            if (route is null)
            {
                failureReason = "Destination is unreachable.";
            }

            return route;
        }

        private RoutePlanningResult PlanEatRoute(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return RoutePlanningResult.Failure("Eat action requires a target.");
            }

            AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            ResourceContainerEntity? container = state.World.ResourceContainers.FirstOrDefault(candidate =>
                candidate.Id == request.TargetId && SpatialQueries.ContainerHasFood(candidate));

            if (agent is null)
            {
                return RoutePlanningResult.Failure("Agent does not exist.");
            }

            if (container is null)
            {
                return RoutePlanningResult.Failure("Target resource container is unavailable.");
            }

            InteractionPoint? currentAnchor = SpatialQueries.FindNearestAvailableInteractionPoint(
                container,
                agent.Position,
                SpatialQueries.DefaultInteractionTolerance);

            if (currentAnchor is not null)
            {
                return RoutePlanningResult.NotNeeded();
            }

            RouteCandidate? candidate = FindBestRouteToInteractionPoint(state, agent, container);

            return candidate is null
                ? RoutePlanningResult.Failure("Target food is unreachable.")
                : RoutePlanningResult.Success(CreateMovement(
                    request,
                    candidate.Route,
                    state.Config.MovementSpeedPerTick,
                    DefaultArrivalTolerance,
                    state.Config.EffectivePathfinding.MaxRepathAttempts));
        }

        private RoutePlanningResult PlanResourceContainerRoute(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return RoutePlanningResult.Failure("Resource action requires a target.");
            }

            AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            ResourceContainerEntity? container = state.World.ResourceContainers.FirstOrDefault(candidate =>
                candidate.Id == request.TargetId && !candidate.IsEmpty);

            if (agent is null)
            {
                return RoutePlanningResult.Failure("Agent does not exist.");
            }

            if (container is null)
            {
                return RoutePlanningResult.Failure("Target resource container is unavailable.");
            }

            InteractionPoint? currentAnchor = SpatialQueries.FindNearestAvailableInteractionPoint(
                container,
                agent.Position,
                SpatialQueries.DefaultInteractionTolerance);

            if (currentAnchor is not null)
            {
                return RoutePlanningResult.NotNeeded();
            }

            RouteCandidate? candidate = FindBestRouteToInteractionPoint(state, agent, container);

            return candidate is null
                ? RoutePlanningResult.Failure("Target resource container is unreachable.")
                : RoutePlanningResult.Success(CreateMovement(
                    request,
                    candidate.Route,
                    state.Config.MovementSpeedPerTick,
                    DefaultArrivalTolerance,
                    state.Config.EffectivePathfinding.MaxRepathAttempts));
        }

        private RoutePlanningResult PlanWanderRoute(SimulationState state, ActionRequest request)
        {
            AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);

            if (agent is null)
            {
                return RoutePlanningResult.Failure("Agent does not exist.");
            }

            GridCell currentCell = SpatialQueries.GetCurrentCell(state.World, agent.Position);
            IReadOnlyList<GridCell> destinations = state.World.Grid.GetAdjacentCells(
                    currentCell,
                    state.Config.EffectivePathfinding.AllowDiagonalMovement)
                .Where(cell => CanAgentEnterCell(state, agent, cell))
                .ToList();

            if (destinations.Count == 0)
            {
                return RoutePlanningResult.Failure("Agent has no available adjacent cell to wander to.");
            }

            GridCell destination = destinations[0];
            MovementRoute route = CreateRoute(
                new GridPath([currentCell, destination], state.World.Grid.GetTraversalCost(destination)),
                GetCellCenter(destination));

            return RoutePlanningResult.Success(CreateMovement(
                request,
                route,
                state.Config.MovementSpeedPerTick,
                DefaultArrivalTolerance,
                state.Config.EffectivePathfinding.MaxRepathAttempts));
        }

        private RouteCandidate? FindBestRouteToInteractionPoint(
            SimulationState state,
            AgentEntity agent,
            ResourceContainerEntity container)
        {
            GridCell startCell = SpatialQueries.GetCurrentCell(state.World, agent.Position);
            List<RouteCandidate> candidates = [];

            foreach (InteractionPoint point in container.InteractionPoints.Where(point => !point.IsReserved))
            {
                GridCell destinationCell = point.StandPosition.ToGridCell();
                PathResult result = _pathfinder.FindPath(
                    state.World.Grid,
                    new PathRequest(
                        startCell,
                        destinationCell,
                        cell => CanAgentEnterCell(state, agent, cell),
                        state.Config.EffectivePathfinding.AllowDiagonalMovement,
                        state.Config.EffectivePathfinding.MaxVisitedCells));

                if (!result.Succeeded || result.Path is null)
                {
                    continue;
                }

                candidates.Add(new RouteCandidate(
                    CreateRoute(result.Path, point.StandPosition),
                    result.Path.TotalCost,
                    agent.Position.DistanceTo(point.StandPosition)));
            }

            return candidates
                .OrderBy(candidate => candidate.TotalCost)
                .ThenBy(candidate => candidate.Distance)
                .FirstOrDefault();
        }

        private MovementRoute? FindRouteToExactDestination(
            SimulationState state,
            AgentEntity agent,
            WorldPosition exactDestination)
        {
            GridCell startCell = SpatialQueries.GetCurrentCell(state.World, agent.Position);
            GridCell destinationCell = exactDestination.ToGridCell();
            PathResult result = _pathfinder.FindPath(
                state.World.Grid,
                new PathRequest(
                    startCell,
                    destinationCell,
                    cell => CanAgentEnterCell(state, agent, cell),
                    state.Config.EffectivePathfinding.AllowDiagonalMovement,
                    state.Config.EffectivePathfinding.MaxVisitedCells));

            return result.Succeeded && result.Path is not null
                ? CreateRoute(result.Path, exactDestination)
                : null;
        }

        private static ActiveMovement CreateMovement(
            ActionRequest request,
            MovementRoute route,
            float speedPerTick,
            float arrivalTolerance,
            int maxRepathAttempts)
        {
            return new ActiveMovement(
                request.AgentId,
                request.ActionType,
                request.TargetId,
                route,
                speedPerTick,
                arrivalTolerance,
                maxRepathAttempts,
                request.ContextKey,
                request.SelectedGoal,
                request.NeedsBefore,
                request.SourceTaskId,
                request.SourceGoalId);
        }

        private static MovementRoute CreateRoute(GridPath path, WorldPosition exactDestination)
        {
            List<WorldPosition> waypoints = [];

            foreach (GridCell cell in path.Cells.Skip(1).SkipLast(1))
            {
                waypoints.Add(GetCellCenter(cell));
            }

            waypoints.Add(exactDestination);

            return new MovementRoute(waypoints, path.TotalCost);
        }

        private static WorldPosition GetCellCenter(GridCell cell)
        {
            return new WorldPosition(cell.X + 0.5f, cell.Y + 0.5f);
        }

        private static bool CanAgentEnterCell(
            SimulationState state,
            AgentEntity agent,
            GridCell cell)
        {
            return state.World.Grid.IsTraversable(cell)
                && !SpatialQueries.IsCellOccupied(state.World, cell, agent.Id);
        }

        private sealed record RouteCandidate(MovementRoute Route, float TotalCost, float Distance);
    }
}
