namespace TabulaRasa.Simulation.Goals
{
    public sealed class AgentGoal
    {
        public AgentGoal(
            string id,
            string agentId,
            string needKey,
            string reason,
            int priority,
            long createdTick,
            string? targetId = null,
            string? targetType = null,
            string? jobId = null)
        {
            Id = id;
            AgentId = agentId;
            NeedKey = needKey;
            Reason = reason;
            Priority = priority;
            CreatedTick = createdTick;
            LastUpdatedTick = createdTick;
            TargetId = targetId;
            TargetType = targetType;
            JobId = jobId;
        }

        public string Id { get; }
        public string AgentId { get; }
        public string NeedKey { get; }
        public string Reason { get; }
        public int Priority { get; }
        public long CreatedTick { get; }
        public long LastUpdatedTick { get; private set; }
        public string? TargetId { get; }
        public string? TargetType { get; }
        public string? JobId { get; private set; }
        public GoalStatus Status { get; private set; } = GoalStatus.Active;
        public string? FailureReason { get; private set; }

        public void LinkJob(string jobId, long tick)
        {
            JobId = jobId;
            LastUpdatedTick = tick;
        }

        public void Complete(long tick)
        {
            Status = GoalStatus.Completed;
            LastUpdatedTick = tick;
        }

        public void Fail(string reason, long tick)
        {
            FailureReason = reason;
            Status = GoalStatus.Failed;
            LastUpdatedTick = tick;
        }

        public void Interrupt(string reason, long tick)
        {
            FailureReason = reason;
            Status = GoalStatus.Interrupted;
            LastUpdatedTick = tick;
        }

        public bool IsActive => Status == GoalStatus.Active;
    }
}
