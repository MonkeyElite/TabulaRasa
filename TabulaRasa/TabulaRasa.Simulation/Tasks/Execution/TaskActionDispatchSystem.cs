using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.World.Entities;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Simulation.Tasks.Execution
{
    public sealed class TaskActionDispatchSystem : ISystem
    {
        public string Name => "Task Action Dispatch System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 5;

        public void Execute(SimulationState state)
        {
            foreach (JobInstance job in state.ActiveJobs.Where(job => job.Status == JobStatus.Active))
            {
                foreach (TaskInstance task in job.Tasks.Where(task =>
                    task.Status is TaskStatus.Assigned or TaskStatus.InProgress))
                {
                    if (task.Definition.ExecutionKind == TaskExecutionKind.Progress)
                    {
                        continue;
                    }

                    if (task.AssignedAgentId is null || task.Definition.AtomicAction is null)
                    {
                        continue;
                    }

                    if (HasFinishedActionResult(state, task)
                        || HasPendingRequest(state, task)
                        || HasActiveMovement(state, task))
                    {
                        continue;
                    }

                    task.Begin();

                    AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(agent => agent.Id == task.AssignedAgentId);
                    if (agentEntity?.IsDead == true)
                    {
                        continue;
                    }

                    AgentNeedsSnapshot? needsBefore = state.GetAgentById(task.AssignedAgentId)?.NeedState.ToSnapshot();
                    ActionRequest request = new(
                        task.AssignedAgentId,
                        task.Definition.AtomicAction.Value,
                        task.Definition.TargetId,
                        task.Definition.ContextKey,
                        task.Definition.SelectedGoal,
                        task.Definition.TargetType,
                        "Task",
                        needsBefore,
                        task.Id,
                        job.GoalId,
                        task.Definition.ExecutionKind == TaskExecutionKind.Movement);

                    state.PendingActionRequests.Add(request);
                    task.MarkActionDispatched(state.ActiveTick);
                    state.EmitEvent(
                        "task.action_dispatched",
                        Name,
                        $"{task.Id} dispatched {request.ActionType}.",
                        task.Id,
                        new Dictionary<string, string>
                        {
                            ["agentId"] = task.AssignedAgentId,
                            ["actionType"] = request.ActionType.ToString(),
                            ["targetId"] = request.TargetId ?? "",
                            ["sourceGoalId"] = job.GoalId ?? ""
                        });
                }
            }
        }

        private static bool HasFinishedActionResult(SimulationState state, TaskInstance task)
        {
            return state.ActionResults.Any(result => result.SourceTaskId == task.Id);
        }

        private static bool HasPendingRequest(SimulationState state, TaskInstance task)
        {
            return state.PendingActionRequests.Any(request => request.SourceTaskId == task.Id);
        }

        private static bool HasActiveMovement(SimulationState state, TaskInstance task)
        {
            return state.ActiveMovements.Any(movement => movement.SourceTaskId == task.Id);
        }
    }
}
