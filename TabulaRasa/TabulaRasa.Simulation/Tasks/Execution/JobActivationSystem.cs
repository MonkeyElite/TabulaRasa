using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Jobs;

namespace TabulaRasa.Simulation.Tasks.Execution
{
    public sealed class JobActivationSystem : ISystem
    {
        public string Name => "Job Activation System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 3;

        public void Execute(SimulationState state)
        {
            foreach (JobInstance job in state.PendingJobs
                .OrderByDescending(job => job.Definition.Priority)
                .ToList())
            {
                state.PendingJobs.Remove(job);
                job.Activate();
                state.ActiveJobs.Add(job);
                state.EmitEvent(
                    "job.activated",
                    Name,
                    $"{job.Id} activated.",
                    job.Id,
                    new Dictionary<string, string>
                    {
                        ["definitionId"] = job.Definition.Id,
                        ["name"] = job.Definition.Name
                    });
            }
        }
    }
}
