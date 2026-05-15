namespace TabulaRasa.Simulation.Tasks.Jobs
{
    public sealed class JobDefinition
    {
        public JobDefinition(
            string id,
            string name,
            IReadOnlyList<JobStepDefinition> steps,
            int priority = 0)
        {
            if (steps.Count == 0)
            {
                throw new ArgumentException("Jobs must contain at least one step.", nameof(steps));
            }

            Id = id;
            Name = name;
            Steps = steps;
            Priority = priority;
        }

        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<JobStepDefinition> Steps { get; }
        public int Priority { get; }
    }
}
