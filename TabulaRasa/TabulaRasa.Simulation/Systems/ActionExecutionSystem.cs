using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Actions.Resolution;
using TabulaRasa.Simulation.Actions.Validation;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;

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
                ActionValidationResult validation = _validator.Validate(state, request);

                if (!validation.IsValid)
                {
                    state.ActionResults.Add(new ActionResult(
                        request.AgentId,
                        request.ActionType,
                        false,
                        validation.FailureReason));

                    continue;
                }

                state.ActionResults.Add(_resolver.Resolve(state, request));
            }
        }
    }
}
