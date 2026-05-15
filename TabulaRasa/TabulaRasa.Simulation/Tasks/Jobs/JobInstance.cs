using TabulaRasa.Simulation.Tasks.Definitions;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Simulation.Tasks.Jobs
{
    public sealed class JobInstance
    {
        public JobInstance(string id, JobDefinition definition)
        {
            Id = id;
            Definition = definition;
            Tasks = definition.Steps
                .Select(step => new TaskInstance(
                    $"{id}:{step.Id}",
                    id,
                    step.Id,
                    step.TaskDefinition,
                    step.DependsOnStepIds))
                .ToList();
        }

        public string Id { get; }
        public JobDefinition Definition { get; }
        public JobStatus Status { get; private set; } = JobStatus.Pending;
        public IReadOnlyList<TaskInstance> Tasks { get; }

        public void Activate()
        {
            if (Status == JobStatus.Pending)
            {
                Status = JobStatus.Active;
            }
        }

        public bool AreDependenciesComplete(TaskInstance task)
        {
            return task.DependsOnStepIds.All(dependencyStepId =>
                Tasks.Any(candidate =>
                    candidate.StepId == dependencyStepId
                    && candidate.Status == TaskStatus.Completed));
        }

        public void RefreshStatus()
        {
            if (Tasks.Any(t => t.Status == TaskStatus.Failed))
            {
                Status = JobStatus.Failed;
                return;
            }

            if (Tasks.Any(t => t.Status == TaskStatus.Cancelled))
            {
                Status = JobStatus.Cancelled;
                return;
            }

            if (Tasks.All(t => t.Status == TaskStatus.Completed))
            {
                Status = JobStatus.Completed;
            }
        }
    }
}
