using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Simulation.Tasks.Assignment
{
    public sealed class TaskAssignmentSystem : ISystem
    {
        private const string SourceSystem = "Task Assignment System";

        public string Name => "Task Assignment System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 4;

        public void Execute(SimulationState state)
        {
            state.Reservations.ReleaseExpired(state.Time.Tick);

            foreach (JobInstance job in state.ActiveJobs
                .Where(job => job.Status == JobStatus.Active)
                .OrderByDescending(job => job.Definition.Priority))
            {
                foreach (TaskInstance task in job.Tasks.Where(task =>
                    task.Status == TaskStatus.Pending
                    && job.AreDependenciesComplete(task)))
                {
                    string? agentId = FindAvailableAgentId(state);

                    if (agentId is null)
                    {
                        return;
                    }

                    if (!PreconditionsPass(state, task))
                    {
                        job.RefreshStatus();
                        continue;
                    }

                    if (!TryReserveRequirements(state, task))
                    {
                        continue;
                    }

                    task.AssignTo(agentId);
                    state.EmitEvent(
                        "task.assigned",
                        SourceSystem,
                        $"{task.Id} assigned to {agentId}.",
                        task.Id,
                        new Dictionary<string, string>
                        {
                            ["agentId"] = agentId,
                            ["taskDefinitionId"] = task.Definition.Id
                        });
                }
            }
        }

        private static string? FindAvailableAgentId(SimulationState state)
        {
            HashSet<string> busyAgentIds = state.ActiveMovements
                .Select(movement => movement.AgentId)
                .Concat(state.ActiveJobs
                    .SelectMany(job => job.Tasks)
                    .Where(task => task.Status is TaskStatus.Assigned or TaskStatus.InProgress)
                    .Select(task => task.AssignedAgentId)
                    .OfType<string>())
                .ToHashSet();

            return state.Agents
                .Where(agent => !busyAgentIds.Contains(agent.Id))
                .Where(agent => state.World.Agents.FirstOrDefault(entity => entity.Id == agent.Id)?.IsDead != true)
                .Select(agent => agent.Id)
                .FirstOrDefault();
        }

        private static bool PreconditionsPass(SimulationState state, TaskInstance task)
        {
            foreach (ITaskPrecondition precondition in task.Definition.Preconditions)
            {
                TaskPreconditionResult result = precondition.Evaluate(state, task);

                if (!result.Succeeded)
                {
                    task.Fail(result.FailureReason ?? "Task precondition failed.");
                    state.Reservations.ReleaseByOwner(task.Id);
                    state.EmitEvent(
                        "task.failed",
                        SourceSystem,
                        $"{task.Id} failed preconditions: {task.FailureReason}",
                        task.Id,
                        new Dictionary<string, string>
                        {
                            ["reason"] = task.FailureReason ?? ""
                        });
                    return false;
                }
            }

            return true;
        }

        private static bool TryReserveRequirements(SimulationState state, TaskInstance task)
        {
            foreach (TaskRequirement requirement in task.Definition.Requirements.Where(r => r.RequiresReservation))
            {
                if (!state.Reservations.TryReserve(
                    requirement.ToReservationTarget(),
                    task.Id,
                    state.Time.Tick))
                {
                    state.Reservations.ReleaseByOwner(task.Id);
                    return false;
                }

                state.EmitEvent(
                    "reservation.created",
                    SourceSystem,
                    $"{task.Id} reserved {requirement.TargetType}:{requirement.TargetId}.",
                    task.Id,
                    new Dictionary<string, string>
                    {
                        ["targetType"] = requirement.TargetType.ToString(),
                        ["targetId"] = requirement.TargetId
                    });
            }

            return true;
        }
    }
}
