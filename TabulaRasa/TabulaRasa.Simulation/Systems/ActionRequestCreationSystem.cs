using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Actions.Requests;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class ActionRequestCreationSystem : ISystem
    {
        public string Name => "Action Request Creation System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 2;

        public void Execute(SimulationState state)
        {
            foreach (AgentIntent intent in state.PendingIntents)
            {
                if (IsAgentBusyWithGoalOrTask(state, intent.AgentId))
                {
                    continue;
                }

                AgentEntity? agent = state.World.Agents.FirstOrDefault(agent => agent.Id == intent.AgentId);
                if (agent?.IsDead == true)
                {
                    continue;
                }

                state.PendingActionRequests.Add(ActionRequestFactory.Create(intent));
            }

            state.PendingIntents.Clear();
        }

        private static bool IsAgentBusyWithGoalOrTask(SimulationState state, string agentId)
        {
            return state.Goals.Any(goal => goal.AgentId == agentId && goal.IsActive)
                || state.ActiveJobs
                    .SelectMany(job => job.Tasks)
                    .Any(task =>
                        task.AssignedAgentId == agentId
                        && task.Status is TaskStatus.Assigned or TaskStatus.InProgress);
        }
    }
}
