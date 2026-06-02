using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Actions.Resolution;
using TabulaRasa.Simulation.Actions.Validation;
using TabulaRasa.Simulation.Interfaces;
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
                        validation.FailureReason);
                    state.ActionResults.Add(result);
                    EmitActionResultEvent(state, result);

                    continue;
                }

                ActionResult resolved = _resolver.Resolve(state, request);
                state.ActionResults.Add(resolved);
                EmitActionResultEvent(state, resolved);
            }
        }

        private void EmitActionResultEvent(SimulationState state, ActionResult result)
        {
            string outcome = result.Succeeded ? "succeeded" : $"failed: {result.Reason ?? "unknown"}";
            state.EmitEvent(
                "action.result",
                Name,
                $"{result.AgentId} {result.ActionType} {outcome}.",
                result.AgentId,
                new Dictionary<string, string>
                {
                    ["actionType"] = result.ActionType.ToString(),
                    ["succeeded"] = result.Succeeded.ToString()
                });
        }
    }
}
