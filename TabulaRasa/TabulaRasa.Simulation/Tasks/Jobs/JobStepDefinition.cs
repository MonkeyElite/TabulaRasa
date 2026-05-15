using TabulaRasa.Simulation.Tasks.Definitions;

namespace TabulaRasa.Simulation.Tasks.Jobs
{
    public sealed class JobStepDefinition
    {
        public JobStepDefinition(
            string id,
            TaskDefinition taskDefinition,
            IReadOnlyList<string>? dependsOnStepIds = null)
        {
            Id = id;
            TaskDefinition = taskDefinition;
            DependsOnStepIds = dependsOnStepIds ?? [];
        }

        public string Id { get; }
        public TaskDefinition TaskDefinition { get; }
        public IReadOnlyList<string> DependsOnStepIds { get; }
    }
}
