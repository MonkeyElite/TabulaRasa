using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Actions.Resolution;
using TabulaRasa.Simulation.Actions.Validation;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Learning;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;

namespace TabulaRasa.Simulation.Systems
{
    public class ActionExecutionSystem : ISystem
    {
        private readonly ActionRequestValidator _validator;
        private readonly ActionResolver _resolver;

        public string Name => "Action Execution System";
        public SimulationPhase Phase => SimulationPhase.Execution;
        public int Priority => 0;

        public ActionExecutionSystem()
            : this(new ActionRequestValidator(), new ActionResolver())
        {
        }

        public ActionExecutionSystem(ActionRequestValidator validator, ActionResolver resolver)
        {
            _validator = validator;
            _resolver = resolver;
        }

        public void Execute(SimulationState state)
        {
            List<ActionRequest> requests = [.. state.PendingActionRequests];
            state.PendingActionRequests.Clear();

            foreach (ActionRequest request in requests)
            {
                if (request.IsMovementOnly)
                {
                    continue;
                }

                AgentEntity? agent = state.World.Agents.FirstOrDefault(agent => agent.Id == request.AgentId);
                if (agent is not null && agent.IsDead)
                {
                    continue;
                }

                ActionValidationResult validation = _validator.Validate(state, request);

                if (!validation.IsValid)
                {
                    ActionResult result = new(
                        request.AgentId,
                        request.ActionType,
                        false,
                        validation.FailureReason,
                        request.TargetId,
                        request.ContextKey,
                        request.SelectedGoal,
                        request.NeedsBefore,
                        SourceTaskId: request.SourceTaskId,
                        SourceGoalId: request.SourceGoalId);
                    AgentLearningService.RecordActionResult(state, result, Name);

                    continue;
                }

                ActionResult resolved = _resolver.Resolve(state, request) with
                {
                    TargetId = request.TargetId,
                    ContextKey = request.ContextKey,
                    SelectedGoal = request.SelectedGoal,
                    NeedsBefore = request.NeedsBefore,
                    SourceTaskId = request.SourceTaskId,
                    SourceGoalId = request.SourceGoalId
                };
                AgentLearningService.RecordActionResult(state, resolved, Name);
            }
        }
    }
}
