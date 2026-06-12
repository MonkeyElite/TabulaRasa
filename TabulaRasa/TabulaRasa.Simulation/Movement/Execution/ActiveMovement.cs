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
            float arrivalTolerance,
            int maxRepathAttempts = 3,
            int maxStuckTicks = 3,
            string? contextKey = null,
            string? selectedGoal = null,
            AgentNeedsSnapshot? needsBefore = null,
            string? sourceTaskId = null,
            string? sourceGoalId = null)
        {
            AgentId = agentId;
            RequestedAction = requestedAction;
            TargetId = targetId;
            Route = route;
            SpeedPerTick = speedPerTick;
            ArrivalTolerance = arrivalTolerance;
            MaxRepathAttempts = maxRepathAttempts;
            MaxStuckTicks = Math.Max(1, maxStuckTicks);
            ContextKey = contextKey;
            SelectedGoal = selectedGoal;
            NeedsBefore = needsBefore;
            SourceTaskId = sourceTaskId;
            SourceGoalId = sourceGoalId;
        }

        public string AgentId { get; }
        public AgentActionType RequestedAction { get; }
        public string? TargetId { get; }
        public string? ContextKey { get; }
        public string? SelectedGoal { get; }
        public AgentNeedsSnapshot? NeedsBefore { get; }
        public string? SourceTaskId { get; }
        public string? SourceGoalId { get; }
        public MovementRoute Route { get; set; }
        public float SpeedPerTick { get; }
        public float ArrivalTolerance { get; }
        public int CurrentWaypointIndex { get; set; }
        public MovementStatus Status { get; set; } = MovementStatus.InProgress;
        public string? FailureReason { get; set; }
        public int StuckTicks { get; set; }
        public int MaxStuckTicks { get; set; } = 3;
        public int RepathCount { get; set; }
        public int MaxRepathAttempts { get; set; }
        public string? LastRepathReason { get; set; }
        public float LastEffectiveSpeedPerTick { get; set; }
        public float RouteCost => Route.TotalCost;
    }
}
