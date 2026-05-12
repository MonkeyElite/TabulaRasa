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

            if (movement.RequestedAction == AgentActionType.Wander)
            {
                state.ActionResults.Add(new ActionResult(
                    movement.AgentId,
                    movement.RequestedAction,
                    true));
            }
        }

        private static void FailMovement(SimulationState state, ActiveMovement movement, string reason)
        {
            movement.Status = MovementStatus.Failed;
            movement.FailureReason = reason;
            state.ActiveMovements.Remove(movement);
            state.ActionResults.Add(new ActionResult(
                movement.AgentId,
                movement.RequestedAction,
                false,
                reason));
        }
    }
}
