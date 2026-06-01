using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;

namespace TabulaRasa.Simulation.Movement.Execution
{
    public sealed class MovementExecutionSystem : ISystem
    {
        private const string SourceSystem = "Movement Execution System";

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

                if (movement.CurrentWaypointIndex >= movement.Route.Waypoints.Count)
                {
                    CompleteMovement(state, movement);
                    continue;
                }

                WorldPosition waypoint = movement.Route.Waypoints[movement.CurrentWaypointIndex];

                if (!state.World.Grid.IsTraversable(waypoint.ToGridCell()))
                {
                    FailMovement(state, movement, "Route became blocked.");
                    continue;
                }

                WorldPosition previousPosition = agent.Position;
                agent.Position = MoveToward(agent.Position, waypoint, movement.SpeedPerTick);

                if (agent.Position.DistanceTo(waypoint) <= movement.ArrivalTolerance)
                {
                    agent.Position = waypoint;
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
                    true);
                state.ActionResults.Add(result);
                state.EmitEvent(
                    "action.result",
                    SourceSystem,
                    $"{result.AgentId} {result.ActionType} succeeded.",
                    result.AgentId,
                    new Dictionary<string, string>
                    {
                        ["actionType"] = result.ActionType.ToString(),
                        ["succeeded"] = result.Succeeded.ToString()
                    });
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
                reason);
            state.ActionResults.Add(result);
            state.EmitEvent(
                "action.result",
                SourceSystem,
                $"{result.AgentId} {result.ActionType} failed: {reason}",
                result.AgentId,
                new Dictionary<string, string>
                {
                    ["actionType"] = result.ActionType.ToString(),
                    ["succeeded"] = result.Succeeded.ToString(),
                    ["reason"] = reason
                });
        }
    }
}
