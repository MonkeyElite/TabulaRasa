using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Evolution;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Navigation.Grid;

namespace TabulaRasa.Simulation.Movement.Planning
{
    public sealed class RoutePlanner
    {
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
                AgentActionType.Drink => PlanWaterSourceRoute(state, request),
                AgentActionType.PickUpResource => PlanResourceTargetRoute(state, request),
                AgentActionType.Attack => PlanAgentInteractionRoute(state, request, "Attack target is unavailable.", "Attack target is unreachable."),
                AgentActionType.Reproduce => PlanAgentInteractionRoute(state, request, "Mate is unavailable.", "Mate is unreachable."),
                AgentActionType.Communicate => PlanAgentInteractionRoute(state, request, "Communication target is unavailable.", "Communication target is unreachable."),
                AgentActionType.Flee => PlanFleeRoute(state, request),
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

            if (movement.RequestedAction is AgentActionType.Eat or AgentActionType.PickUpResource or AgentActionType.Drink)
            {
                if (movement.TargetId is null)
                {
                    failureReason = $"{movement.RequestedAction} action requires a target.";
                    return null;
                }

                IInteractableEntity? target = FindRouteTarget(state, movement.RequestedAction, movement.TargetId);

                if (target is null)
                {
                    failureReason = "Target is unavailable.";
                    return null;
                }

                RouteCandidate? candidate = FindBestRouteToInteractionPoint(state, agent, target);

                if (candidate is null)
                {
                    failureReason = "Target is unreachable.";
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
            PlantEntity? plant = state.World.Plants.FirstOrDefault(candidate =>
                candidate.Id == request.TargetId
                && candidate.IsHarvestable
                && string.Equals(candidate.ResourceId, ResourceDefinition.FoodId, StringComparison.OrdinalIgnoreCase));

            if (agent is null)
            {
                return RoutePlanningResult.Failure("Agent does not exist.");
            }

            IInteractableEntity? target = container ?? (IInteractableEntity?)plant;
            if (target is null)
            {
                return RoutePlanningResult.Failure("Target resource container is unavailable.");
            }

            InteractionPoint? currentAnchor = SpatialQueries.FindNearestAvailableInteractionPoint(
                target,
                agent.Position,
                state.Config.EffectivePathfinding.InteractionTolerance);

            if (currentAnchor is not null)
            {
                return RoutePlanningResult.NotNeeded();
            }

            RouteCandidate? candidate = FindBestRouteToInteractionPoint(state, agent, target);

            return candidate is null
                ? RoutePlanningResult.Failure("Target food is unreachable.")
                : RoutePlanningResult.Success(CreateMovement(
                    request,
                    candidate.Route,
                    GetMovementSpeed(state, agent),
                    state.Config.EffectivePathfinding.ArrivalTolerance,
                    state.Config.EffectivePathfinding.MaxRepathAttempts,
                    state.Config.EffectiveBelievability.EffectiveRecovery.MovementStuckTicks));
        }

        private RoutePlanningResult PlanResourceTargetRoute(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return RoutePlanningResult.Failure("Resource action requires a target.");
            }

            AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            IInteractableEntity? target = FindRouteTarget(state, request.ActionType, request.TargetId);

            if (agent is null)
            {
                return RoutePlanningResult.Failure("Agent does not exist.");
            }

            if (target is null)
            {
                return RoutePlanningResult.Failure("Target resource is unavailable.");
            }

            InteractionPoint? currentAnchor = SpatialQueries.FindNearestAvailableInteractionPoint(
                target,
                agent.Position,
                state.Config.EffectivePathfinding.InteractionTolerance);

            if (currentAnchor is not null)
            {
                return RoutePlanningResult.NotNeeded();
            }

            RouteCandidate? candidate = FindBestRouteToInteractionPoint(state, agent, target);

            return candidate is null
                ? RoutePlanningResult.Failure("Target resource container is unreachable.")
                : RoutePlanningResult.Success(CreateMovement(
                    request,
                    candidate.Route,
                    GetMovementSpeed(state, agent),
                    state.Config.EffectivePathfinding.ArrivalTolerance,
                    state.Config.EffectivePathfinding.MaxRepathAttempts,
                    state.Config.EffectiveBelievability.EffectiveRecovery.MovementStuckTicks));
        }

        private RoutePlanningResult PlanWaterSourceRoute(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return RoutePlanningResult.NotNeeded();
            }

            AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            WaterSourceEntity? waterSource = state.World.WaterSources.FirstOrDefault(candidate =>
                candidate.Id == request.TargetId && candidate.IsAvailable);

            if (agent is null)
            {
                return RoutePlanningResult.Failure("Agent does not exist.");
            }

            if (waterSource is null)
            {
                return RoutePlanningResult.Failure("Target water source is unavailable.");
            }

            InteractionPoint? currentAnchor = SpatialQueries.FindNearestAvailableInteractionPoint(
                waterSource,
                agent.Position,
                state.Config.EffectivePathfinding.InteractionTolerance);

            if (currentAnchor is not null)
            {
                return RoutePlanningResult.NotNeeded();
            }

            RouteCandidate? candidate = FindBestRouteToInteractionPoint(state, agent, waterSource);

            return candidate is null
                ? RoutePlanningResult.Failure("Target water source is unreachable.")
                : RoutePlanningResult.Success(CreateMovement(
                    request,
                    candidate.Route,
                    GetMovementSpeed(state, agent),
                    state.Config.EffectivePathfinding.ArrivalTolerance,
                    state.Config.EffectivePathfinding.MaxRepathAttempts,
                    state.Config.EffectiveBelievability.EffectiveRecovery.MovementStuckTicks));
        }

        private RoutePlanningResult PlanAgentInteractionRoute(
            SimulationState state,
            ActionRequest request,
            string unavailableReason,
            string unreachableReason)
        {
            if (request.TargetId is null)
            {
                return RoutePlanningResult.Failure(unavailableReason);
            }

            AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            AgentEntity? target = state.World.Agents.FirstOrDefault(candidate => candidate.Id == request.TargetId && !candidate.IsDead);
            if (agent is null)
            {
                return RoutePlanningResult.Failure("Agent does not exist.");
            }

            if (target is null)
            {
                return RoutePlanningResult.Failure(unavailableReason);
            }

            if (agent.Position.DistanceTo(target.Position) <= state.Config.EffectivePathfinding.InteractionTolerance + state.Config.EffectivePathfinding.AgentInteractionRangeBonus)
            {
                return RoutePlanningResult.NotNeeded();
            }

            RouteCandidate? candidate = FindBestRouteAdjacentToAgent(state, agent, target);

            return candidate is null
                ? RoutePlanningResult.Failure(unreachableReason)
                : RoutePlanningResult.Success(CreateMovement(
                    request,
                    candidate.Route,
                    GetMovementSpeed(state, agent),
                    state.Config.EffectivePathfinding.ArrivalTolerance,
                    state.Config.EffectivePathfinding.MaxRepathAttempts,
                    state.Config.EffectiveBelievability.EffectiveRecovery.MovementStuckTicks));
        }

        private RoutePlanningResult PlanFleeRoute(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return RoutePlanningResult.Failure("Flee action requires a target.");
            }

            AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            AgentEntity? predator = state.World.Agents.FirstOrDefault(candidate => candidate.Id == request.TargetId && !candidate.IsDead);
            if (agent is null)
            {
                return RoutePlanningResult.Failure("Agent does not exist.");
            }

            if (predator is null)
            {
                return RoutePlanningResult.Failure("Flee target is unavailable.");
            }

            GridCell currentCell = SpatialQueries.GetCurrentCell(state.World, agent.Position);
            IReadOnlyList<GridCell> fleeDestinations = state.World.Grid.GetAdjacentCells(
                    currentCell,
                    state.Config.EffectivePathfinding.AllowDiagonalMovement)
                .Where(cell => CanAgentEnterCell(state, agent, cell))
                .OrderByDescending(cell => GetCellCenter(cell).DistanceTo(predator.Position))
                .ToList();

            if (fleeDestinations.Count == 0)
            {
                return RoutePlanningResult.Failure("Agent has no available adjacent cell to flee to.");
            }

            GridCell destination = fleeDestinations[0];
            MovementRoute route = CreateRoute(
                new GridPath([currentCell, destination], state.World.Grid.GetTraversalCost(destination)),
                GetCellCenter(destination));

            return RoutePlanningResult.Success(CreateMovement(
                request,
                route,
                GetMovementSpeed(state, agent),
                state.Config.EffectivePathfinding.ArrivalTolerance,
                state.Config.EffectivePathfinding.MaxRepathAttempts,
                state.Config.EffectiveBelievability.EffectiveRecovery.MovementStuckTicks));
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
                GetMovementSpeed(state, agent),
                state.Config.EffectivePathfinding.ArrivalTolerance,
                state.Config.EffectivePathfinding.MaxRepathAttempts,
                state.Config.EffectiveBelievability.EffectiveRecovery.MovementStuckTicks));
        }

        private RouteCandidate? FindBestRouteToInteractionPoint(
            SimulationState state,
            AgentEntity agent,
            IInteractableEntity target)
        {
            GridCell startCell = SpatialQueries.GetCurrentCell(state.World, agent.Position);
            List<RouteCandidate> candidates = [];

            foreach (InteractionPoint point in target.InteractionPoints.Where(point => !point.IsReserved))
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

        private RouteCandidate? FindBestRouteAdjacentToAgent(
            SimulationState state,
            AgentEntity agent,
            AgentEntity target)
        {
            GridCell startCell = SpatialQueries.GetCurrentCell(state.World, agent.Position);
            GridCell targetCell = SpatialQueries.GetCurrentCell(state.World, target.Position);
            List<RouteCandidate> candidates = [];

            foreach (GridCell destinationCell in state.World.Grid.GetAdjacentCells(
                targetCell,
                state.Config.EffectivePathfinding.AllowDiagonalMovement))
            {
                if (!CanAgentEnterCell(state, agent, destinationCell))
                {
                    continue;
                }

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

                WorldPosition exactDestination = GetCellCenter(destinationCell);
                candidates.Add(new RouteCandidate(
                    CreateRoute(result.Path, exactDestination),
                    result.Path.TotalCost,
                    exactDestination.DistanceTo(target.Position)));
            }

            return candidates
                .OrderBy(candidate => candidate.TotalCost)
                .ThenBy(candidate => candidate.Distance)
                .FirstOrDefault();
        }

        private static ActiveMovement CreateMovement(
            ActionRequest request,
            MovementRoute route,
            float speedPerTick,
            float arrivalTolerance,
            int maxRepathAttempts,
            int maxStuckTicks)
        {
            return new ActiveMovement(
                request.AgentId,
                request.ActionType,
                request.TargetId,
                route,
                speedPerTick,
                arrivalTolerance,
                maxRepathAttempts,
                maxStuckTicks,
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

        private static IInteractableEntity? FindRouteTarget(
            SimulationState state,
            AgentActionType actionType,
            string targetId)
        {
            if (actionType == AgentActionType.Drink)
            {
                return state.World.WaterSources.FirstOrDefault(candidate =>
                    candidate.Id == targetId && candidate.IsAvailable);
            }

            ResourceContainerEntity? container = state.World.ResourceContainers.FirstOrDefault(candidate =>
                candidate.Id == targetId
                && (actionType == AgentActionType.Eat
                    ? SpatialQueries.ContainerHasFood(candidate)
                    : !candidate.IsEmpty));

            if (container is not null)
            {
                return container;
            }

            PlantEntity? plant = state.World.Plants.FirstOrDefault(candidate =>
                candidate.Id == targetId
                && candidate.IsHarvestable
                && (actionType != AgentActionType.Eat
                    || string.Equals(candidate.ResourceId, ResourceDefinition.FoodId, StringComparison.OrdinalIgnoreCase)));
            if (plant is not null)
            {
                return plant;
            }

            return state.World.ResourceDeposits.FirstOrDefault(candidate =>
                candidate.Id == targetId && !candidate.IsEmpty);
        }

        private static float GetMovementSpeed(SimulationState state, AgentEntity agent)
        {
            return state.Config.MovementSpeedPerTick
                * SpeciesRegistry.Get(agent.SpeciesId, state.Config.EffectiveSpeciesRules).MovementSpeedMultiplier
                * AgentTraitService.TraitMultiplier(agent.Traits.Speed);
        }

        private sealed record RouteCandidate(MovementRoute Route, float TotalCost, float Distance);
    }
}
