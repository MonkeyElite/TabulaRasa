using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.Simulation.Tasks.Definitions
{
    public sealed class TaskDefinition
    {
        public TaskDefinition(
            string id,
            string name,
            int requiredProgressTicks,
            AgentActionType? atomicAction = null,
            IReadOnlyList<ITaskPrecondition>? preconditions = null,
            IReadOnlyList<TaskRequirement>? requirements = null,
            IReadOnlyList<TaskOutput>? outputs = null)
        {
            if (requiredProgressTicks < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredProgressTicks),
                    "Tasks must require at least one progress tick.");
            }

            Id = id;
            Name = name;
            RequiredProgressTicks = requiredProgressTicks;
            AtomicAction = atomicAction;
            Preconditions = preconditions ?? [];
            Requirements = requirements ?? [];
            Outputs = outputs ?? [];
        }

        public string Id { get; }
        public string Name { get; }
        public int RequiredProgressTicks { get; }
        public AgentActionType? AtomicAction { get; }
        public IReadOnlyList<ITaskPrecondition> Preconditions { get; }
        public IReadOnlyList<TaskRequirement> Requirements { get; }
        public IReadOnlyList<TaskOutput> Outputs { get; }
    }
}
