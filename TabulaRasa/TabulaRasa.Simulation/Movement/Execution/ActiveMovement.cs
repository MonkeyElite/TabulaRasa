using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Simulation.Movement.Planning;

namespace TabulaRasa.Simulation.Movement.Execution
{
    public sealed class ActiveMovement
    {
        public ActiveMovement(
            string agentId,
            AgentActionType requestedAction,
            string? targetId,
            MovementRoute route,
            float speedPerTick,
            float arrivalTolerance)
        {
            AgentId = agentId;
            RequestedAction = requestedAction;
            TargetId = targetId;
            Route = route;
            SpeedPerTick = speedPerTick;
            ArrivalTolerance = arrivalTolerance;
        }

        public string AgentId { get; }
        public AgentActionType RequestedAction { get; }
        public string? TargetId { get; }
        public MovementRoute Route { get; }
        public float SpeedPerTick { get; }
        public float ArrivalTolerance { get; }
        public int CurrentWaypointIndex { get; set; }
        public MovementStatus Status { get; set; } = MovementStatus.InProgress;
        public string? FailureReason { get; set; }
        public int StuckTicks { get; set; }
        public int MaxStuckTicks { get; set; } = 3;
    }
}
