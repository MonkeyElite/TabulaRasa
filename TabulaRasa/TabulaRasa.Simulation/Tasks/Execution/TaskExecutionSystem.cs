using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Simulation.Tasks.Execution
{
    public sealed class TaskExecutionSystem : ISystem
    {
        private const string SourceSystem = "Task Execution System";

        public string Name => "Task Execution System";
        public SimulationPhase Phase => SimulationPhase.Execution;
        public int Priority => -5;

        public void Execute(SimulationState state)
        {
            foreach (JobInstance job in state.ActiveJobs.Where(job => job.Status == JobStatus.Active).ToList())
            {
                JobStatus previousJobStatus = job.Status;

                foreach (TaskInstance terminalTask in job.Tasks.Where(task => task.IsTerminal))
                {
                    ReleaseTaskReservations(state, terminalTask);
                }

                foreach (TaskInstance task in job.Tasks.Where(task =>
                    task.Status is TaskStatus.Assigned or TaskStatus.InProgress).ToList())
                {
                    if (task.AssignedAgentId is not null
                        && state.World.Agents.FirstOrDefault(agent => agent.Id == task.AssignedAgentId)?.IsDead == true)
                    {
                        task.Interrupt("Assigned agent is dead.");
                        ReleaseTaskReservations(state, task);
                        state.EmitEvent(
                            "task.interrupted",
                            SourceSystem,
                            $"{task.Id} interrupted: assigned agent is dead.",
                            task.Id,
                            new Dictionary<string, string>
                            {
                                ["assignedAgentId"] = task.AssignedAgentId
                            });
                        continue;
                    }

                    bool hasActionResult = task.Definition.ExecutionKind != TaskExecutionKind.Progress
                        && FindTaskActionResult(state, task) is not null;
                    if (!hasActionResult && !PreconditionsPass(state, task))
                    {
                        ReleaseTaskReservations(state, task);
                        state.EmitEvent(
                            "task.failed",
                            SourceSystem,
                            $"{task.Id} failed preconditions: {task.FailureReason}",
                            task.Id,
                            new Dictionary<string, string>
                            {
                                ["reason"] = task.FailureReason ?? ""
                            });
                        continue;
                    }

                    TaskStatus previousTaskStatus = task.Status;
                    task.Begin();
                    if (previousTaskStatus == TaskStatus.Assigned && task.Status == TaskStatus.InProgress)
                    {
                        state.EmitEvent(
                            "task.started",
                            SourceSystem,
                            $"{task.Id} started.",
                            task.Id,
                            new Dictionary<string, string>
                            {
                                ["assignedAgentId"] = task.AssignedAgentId ?? ""
                        });
                    }

                    AdvanceTask(state, task);

                    if (task.IsTerminal)
                    {
                        state.EmitEvent(
                            task.Status == TaskStatus.Completed ? "task.completed" : "task.terminal",
                            SourceSystem,
                            $"{task.Id} is {task.Status}.",
                            task.Id,
                            new Dictionary<string, string>
                            {
                                ["status"] = task.Status.ToString()
                            });
                        ReleaseTaskReservations(state, task);
                    }
                }

                job.RefreshStatus();
                if (job.Status != previousJobStatus)
                {
                    string type = job.Status switch
                    {
                        JobStatus.Completed => "job.completed",
                        JobStatus.Failed => "job.failed",
                        JobStatus.Cancelled => "job.cancelled",
                        JobStatus.Interrupted => "job.interrupted",
                        _ => "job.status_changed"
                    };
                    state.EmitEvent(
                        type,
                        SourceSystem,
                        $"{job.Id} is {job.Status}.",
                        job.Id,
                        new Dictionary<string, string>
                        {
                            ["status"] = job.Status.ToString(),
                            ["definitionId"] = job.Definition.Id
                        });
                }
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
                    MarkTaskTargetUnavailable(state, task, task.FailureReason ?? "Task precondition failed.");
                    return false;
                }
            }

            return true;
        }

        private static void ReleaseTaskReservations(SimulationState state, TaskInstance task)
        {
            bool hadReservations = state.Reservations.Reservations.Any(reservation => reservation.OwnerId == task.Id);
            state.Reservations.ReleaseByOwner(task.Id);
            if (hadReservations)
            {
                state.EmitEvent(
                    "reservation.released",
                    SourceSystem,
                    $"{task.Id} released reservations.",
                    task.Id);
            }
        }

        private static void AdvanceTask(SimulationState state, TaskInstance task)
        {
            switch (task.Definition.ExecutionKind)
            {
                case TaskExecutionKind.Progress:
                    task.Advance();
                    break;
                case TaskExecutionKind.Movement:
                    AdvanceMovementTask(state, task);
                    break;
                case TaskExecutionKind.Action:
                    AdvanceActionTask(state, task);
                    break;
            }
        }

        private static void AdvanceMovementTask(SimulationState state, TaskInstance task)
        {
            ActionResult? result = FindTaskActionResult(state, task);
            if (result is not null)
            {
                if (result.Succeeded)
                {
                    task.Complete();
                    return;
                }

                task.Fail(result.Reason ?? "Movement task failed.");
                MarkTaskTargetUnavailable(state, task, task.FailureReason ?? "Movement task failed.");
                return;
            }

            bool hasPendingWork = state.ActiveMovements.Any(movement => movement.SourceTaskId == task.Id)
                || state.PendingActionRequests.Any(request => request.SourceTaskId == task.Id);

            if (task.DispatchCount > 0 && !hasPendingWork)
            {
                task.Complete();
            }
        }

        private static void AdvanceActionTask(SimulationState state, TaskInstance task)
        {
            ActionResult? result = FindTaskActionResult(state, task);
            if (result is null)
            {
                return;
            }

            if (result.Succeeded)
            {
                task.Complete();
                return;
            }

            task.Fail(result.Reason ?? "Action task failed.");
            MarkTaskTargetUnavailable(state, task, task.FailureReason ?? "Action task failed.");
        }

        private static ActionResult? FindTaskActionResult(SimulationState state, TaskInstance task)
        {
            return state.ActionResults
                .LastOrDefault(result => result.SourceTaskId == task.Id);
        }

        private static void MarkTaskTargetUnavailable(SimulationState state, TaskInstance task, string reason)
        {
            if (!string.Equals(task.Definition.SelectedGoal, "Hunger", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AgentMemoryService.MarkTargetUnavailable(
                state,
                task.AssignedAgentId ?? "",
                task.Definition.TargetId,
                reason);
        }
    }
}
