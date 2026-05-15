using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Simulation.Tasks.Execution
{
    public sealed class TaskExecutionSystem : ISystem
    {
        public string Name => "Task Execution System";
        public SimulationPhase Phase => SimulationPhase.Execution;
        public int Priority => -5;

        public void Execute(SimulationState state)
        {
            foreach (JobInstance job in state.ActiveJobs.Where(job => job.Status == JobStatus.Active).ToList())
            {
                foreach (TaskInstance terminalTask in job.Tasks.Where(task => task.IsTerminal))
                {
                    ReleaseTaskReservations(state, terminalTask);
                }

                foreach (TaskInstance task in job.Tasks.Where(task =>
                    task.Status is TaskStatus.Assigned or TaskStatus.InProgress).ToList())
                {
                    if (!PreconditionsPass(state, task))
                    {
                        ReleaseTaskReservations(state, task);
                        continue;
                    }

                    task.Begin();
                    task.Advance();

                    if (task.IsTerminal)
                    {
                        ReleaseTaskReservations(state, task);
                    }
                }

                job.RefreshStatus();
            }
        }

        private static bool PreconditionsPass(SimulationState state, TaskInstance task)
        {
            foreach (ITaskPrecondition precondition in task.Definition.Preconditions)
            {
                TaskPreconditionResult result = precondition.Evaluate(state, task);

                if (!result.Succeeded)
                {
                    task.Fail(result.FailureReason ?? "Task precondition failed.");
                    return false;
                }
            }

            return true;
        }

        private static void ReleaseTaskReservations(SimulationState state, TaskInstance task)
        {
            state.Reservations.ReleaseByOwner(task.Id);
        }
    }
}
