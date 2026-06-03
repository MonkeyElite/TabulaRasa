namespace TabulaRasa.Simulation.Tasks.Definitions
{
    public sealed class TaskInstance
    {
        public TaskInstance(
            string id,
            string jobId,
            string stepId,
            TaskDefinition definition,
            IReadOnlyList<string>? dependsOnStepIds = null)
        {
            Id = id;
            JobId = jobId;
            StepId = stepId;
            Definition = definition;
            DependsOnStepIds = dependsOnStepIds ?? [];
        }

        public string Id { get; }
        public string JobId { get; }
        public string StepId { get; }
        public TaskDefinition Definition { get; }
        public IReadOnlyList<string> DependsOnStepIds { get; }
        public string? AssignedAgentId { get; private set; }
        public int ProgressTicks { get; private set; }
        public int DispatchCount { get; private set; }
        public long? LastDispatchTick { get; private set; }
        public TaskStatus Status { get; private set; } = TaskStatus.Pending;
        public string? FailureReason { get; private set; }

        public void AssignTo(string agentId)
        {
            AssignedAgentId = agentId;
            Status = TaskStatus.Assigned;
        }

        public void Begin()
        {
            if (Status == TaskStatus.Assigned)
            {
                Status = TaskStatus.InProgress;
            }
        }

        public void Advance()
        {
            if (Status != TaskStatus.InProgress)
            {
                return;
            }

            ProgressTicks++;

            if (ProgressTicks >= Definition.RequiredProgressTicks)
            {
                Status = TaskStatus.Completed;
            }
        }

        public void MarkActionDispatched(long tick)
        {
            DispatchCount++;
            LastDispatchTick = tick;
        }

        public void Complete()
        {
            Status = TaskStatus.Completed;
        }

        public void Fail(string reason)
        {
            FailureReason = reason;
            Status = TaskStatus.Failed;
        }

        public void Cancel(string reason)
        {
            FailureReason = reason;
            Status = TaskStatus.Cancelled;
        }

        public void Interrupt(string reason)
        {
            FailureReason = reason;
            Status = TaskStatus.Interrupted;
        }

        public bool IsTerminal =>
            Status is TaskStatus.Completed
                or TaskStatus.Failed
                or TaskStatus.Cancelled
                or TaskStatus.Interrupted;
    }
}
