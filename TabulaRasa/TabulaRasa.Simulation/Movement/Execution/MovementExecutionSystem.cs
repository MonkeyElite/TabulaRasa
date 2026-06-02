using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Learning;
using TabulaRasa.Simulation.Movement.Planning;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Mutation;
using TabulaRasa.World.Queries;

namespace TabulaRasa.Simulation.Movement.Execution
{
    public sealed class MovementExecutionSystem : ISystem
    {
        private const string SourceSystem = "Movement Execution System";
        private readonly WorldMutationService _mutations;
        private readonly RoutePlanner _routePlanner;

        public MovementExecutionSystem()
            : this(new WorldMutationService(), new RoutePlanner())
        {
        }

        public MovementExecutionSystem(WorldMutationService mutations)
            : this(mutations, new RoutePlanner())
        {
        }

        public MovementExecutionSystem(WorldMutationService mutations, RoutePlanner routePlanner)
        {
            _mutations = mutations;
            _routePlanner = routePlanner;
        }

        public string Name => "Movement Execution System";
        public SimulationPhase Phase => SimulationPhase.Execution;
        public int Priority => -10;

        public void Execute(SimulationState state)
        {
            foreach (ActiveMovement movement in state.ActiveMovements.ToList())
            {
                AgentEntity? agent = state.World.Agents.FirstOrDefault(a => a.Id == movement.AgentId);

                if (agent is null)
                {
                    FailMovement(state, movement, "Agent does not exist.");
                    continue;
                }

                if (agent.IsDead)
                {
                    CancelMovementForDeadAgent(state, movement);
                    continue;
                }

                if (movement.Status == MovementStatus.Repathing)
                {
                    movement.Status = MovementStatus.InProgress;
                }

                if (movement.CurrentWaypointIndex >= movement.Route.Waypoints.Count)
                {
                    CompleteMovement(state, movement);
                    continue;
                }

                WorldPosition waypoint = movement.Route.Waypoints[movement.CurrentWaypointIndex];

                if (IsRouteCellInvalid(state, agent, waypoint.ToGridCell(), out string invalidReason))
                {
                    if (TryRepath(state, movement, invalidReason, out string repathFailureReason))
                    {
                        continue;
                    }

                    FailMovement(state, movement, repathFailureReason);
                    continue;
                }

                WorldPosition previousPosition = agent.Position;
                float effectiveSpeed = GetEffectiveSpeedPerTick(state, agent, movement, waypoint.ToGridCell());
                movement.LastEffectiveSpeedPerTick = effectiveSpeed;
                WorldPosition nextPosition = MoveToward(agent.Position, waypoint, effectiveSpeed);
                WorldMutationResult moveResult = _mutations.TryMoveEntity(
                    state.World,
                    agent.Id,
                    nextPosition);

                if (!moveResult.Succeeded)
                {
                    string reason = ToMovementFailureReason(moveResult);
                    if (moveResult.FailureKind is WorldMutationFailureKind.BlockedCell or WorldMutationFailureKind.OccupiedCell
                        && TryRepath(state, movement, reason, out string repathFailureReason))
                    {
                        continue;
                    }

                    FailMovement(state, movement, reason);
                    continue;
                }

                if (agent.Position.DistanceTo(waypoint) <= movement.ArrivalTolerance)
                {
                    WorldMutationResult snapResult = _mutations.TryMoveEntity(
                        state.World,
                        agent.Id,
                        waypoint);

                    if (!snapResult.Succeeded)
                    {
                        string reason = ToMovementFailureReason(snapResult);
                        if (snapResult.FailureKind is WorldMutationFailureKind.BlockedCell or WorldMutationFailureKind.OccupiedCell
                            && TryRepath(state, movement, reason, out string repathFailureReason))
                        {
                            continue;
                        }

                        FailMovement(state, movement, reason);
                        continue;
                    }

                    movement.CurrentWaypointIndex++;
                    movement.StuckTicks = 0;
                }
                else if (agent.Position.DistanceTo(previousPosition) <= 0.0001f)
                {
                    movement.StuckTicks++;
                }
                else
                {
                    movement.StuckTicks = 0;
                }

                if (movement.StuckTicks >= movement.MaxStuckTicks)
                {
                    FailMovement(state, movement, "Movement is stuck.");
                    continue;
                }

                if (movement.CurrentWaypointIndex >= movement.Route.Waypoints.Count)
                {
                    CompleteMovement(state, movement);
                }
            }
        }

        private static bool IsRouteCellInvalid(
            SimulationState state,
            AgentEntity agent,
            GridCell cell,
            out string reason)
        {
            if (!state.World.Grid.IsTraversable(cell))
            {
                reason = "Route became blocked.";
                return true;
            }

            if (SpatialQueries.IsCellOccupied(state.World, cell, agent.Id))
            {
                reason = "Route became occupied.";
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private bool TryRepath(
            SimulationState state,
            ActiveMovement movement,
            string reason,
            out string failureReason)
        {
            failureReason = reason;

            if (movement.RepathCount >= movement.MaxRepathAttempts)
            {
                failureReason = $"{reason} Maximum repath attempts reached.";
                return false;
            }

            MovementRoute? route = _routePlanner.ReplanRoute(state, movement, out string routeFailureReason);

            if (route is null)
            {
                failureReason = $"{reason} Could not replan route: {routeFailureReason}";
                return false;
            }

            movement.Route = route;
            movement.CurrentWaypointIndex = 0;
            movement.StuckTicks = 0;
            movement.RepathCount++;
            movement.LastRepathReason = reason;
            movement.LastEffectiveSpeedPerTick = 0;
            movement.Status = MovementStatus.Repathing;

            state.EmitEvent(
                "movement.replanned",
                SourceSystem,
                $"{movement.AgentId} replanned movement for {movement.RequestedAction}: {reason}",
                movement.AgentId,
                new Dictionary<string, string>
                {
                    ["actionType"] = movement.RequestedAction.ToString(),
                    ["targetId"] = movement.TargetId ?? "",
                    ["reason"] = reason,
                    ["routeCost"] = movement.RouteCost.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                    ["repathCount"] = movement.RepathCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });

            return true;
        }

        private static float GetEffectiveSpeedPerTick(
            SimulationState state,
            AgentEntity agent,
            ActiveMovement movement,
            GridCell nextCell)
        {
            float terrainMultiplier = state.World.Grid.GetSpeedMultiplier(nextCell);
            float energyMultiplier = GetEnergySpeedMultiplier(state.GetAgentById(agent.Id));

            return movement.SpeedPerTick * terrainMultiplier * energyMultiplier;
        }

        private static float GetEnergySpeedMultiplier(AgentState? agentState)
        {
            if (agentState is null || agentState.NeedState.Energy >= 1)
            {
                return 1f;
            }

            return agentState.NeedState.Energy > 0 ? 0.75f : 0.5f;
        }

        private static string ToMovementFailureReason(WorldMutationResult result)
        {
            return result.FailureKind switch
            {
                WorldMutationFailureKind.BlockedCell => "Route became blocked.",
                WorldMutationFailureKind.OccupiedCell => "Route became occupied.",
                WorldMutationFailureKind.OutOfBounds => "Route left the world bounds.",
                _ => result.Reason ?? "Movement mutation failed."
            };
        }

        private static WorldPosition MoveToward(WorldPosition current, WorldPosition destination, float maxDistance)
        {
            float distance = current.DistanceTo(destination);

            if (distance <= maxDistance || distance == 0)
            {
                return destination;
            }

            float ratio = maxDistance / distance;
            return new WorldPosition(
                current.X + ((destination.X - current.X) * ratio),
                current.Y + ((destination.Y - current.Y) * ratio));
        }

        private static void CompleteMovement(SimulationState state, ActiveMovement movement)
        {
            movement.Status = MovementStatus.Arrived;
            state.ActiveMovements.Remove(movement);
            state.EmitEvent(
                "movement.completed",
                SourceSystem,
                $"{movement.AgentId} arrived for {movement.RequestedAction}.",
                movement.AgentId,
                new Dictionary<string, string>
                {
                    ["actionType"] = movement.RequestedAction.ToString(),
                    ["targetId"] = movement.TargetId ?? ""
                });

            if (movement.RequestedAction == AgentActionType.Wander)
            {
                ActionResult result = new(
                    movement.AgentId,
                    movement.RequestedAction,
                    true,
                    TargetId: movement.TargetId,
                    ContextKey: movement.ContextKey,
                    SelectedGoal: movement.SelectedGoal,
                    NeedsBefore: movement.NeedsBefore);
                AgentLearningService.RecordActionResult(state, result, SourceSystem);
            }
        }

        private static void FailMovement(SimulationState state, ActiveMovement movement, string reason)
        {
            movement.Status = MovementStatus.Failed;
            movement.FailureReason = reason;
            state.ActiveMovements.Remove(movement);
            state.EmitEvent(
                "movement.failed",
                SourceSystem,
                $"{movement.AgentId} movement failed: {reason}",
                movement.AgentId,
                new Dictionary<string, string>
                {
                    ["actionType"] = movement.RequestedAction.ToString(),
                    ["targetId"] = movement.TargetId ?? "",
                    ["reason"] = reason
                });
            ActionResult result = new(
                movement.AgentId,
                movement.RequestedAction,
                false,
                reason,
                movement.TargetId,
                movement.ContextKey,
                movement.SelectedGoal,
                movement.NeedsBefore);
            AgentLearningService.RecordActionResult(state, result, SourceSystem);
        }

        private static void CancelMovementForDeadAgent(SimulationState state, ActiveMovement movement)
        {
            movement.Status = MovementStatus.Failed;
            movement.FailureReason = "Agent is dead.";
            state.ActiveMovements.Remove(movement);
            state.EmitEvent(
                "movement.cancelled",
                SourceSystem,
                $"{movement.AgentId} movement cancelled: agent is dead.",
                movement.AgentId,
                new Dictionary<string, string>
                {
                    ["actionType"] = movement.RequestedAction.ToString(),
                    ["targetId"] = movement.TargetId ?? "",
                    ["reason"] = "Agent is dead."
                });
        }
    }
}
