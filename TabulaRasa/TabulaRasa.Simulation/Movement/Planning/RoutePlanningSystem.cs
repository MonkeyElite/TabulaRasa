using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Movement.Planning
{
    public sealed class RoutePlanningSystem : ISystem
    {
        private readonly RoutePlanner _planner;

        public RoutePlanningSystem()
            : this(new RoutePlanner())
        {
        }

        public RoutePlanningSystem(RoutePlanner planner)
        {
            _planner = planner;
        }

        public string Name => "Route Planning System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 2;

        public void Execute(SimulationState state)
        {
            foreach (ActionRequest request in state.PendingActionRequests.ToList())
            {
                if (state.ActiveMovements.Any(m => m.AgentId == request.AgentId))
                {
                    state.PendingActionRequests.Remove(request);
                    continue;
                }

                RoutePlanningResult result = _planner.Plan(state, request);

                if (!result.Succeeded)
                {
                    state.PendingActionRequests.Remove(request);
                    ActionResult actionResult = new(
                        request.AgentId,
                        request.ActionType,
                        false,
                        result.FailureReason);
                    state.ActionResults.Add(actionResult);
                    state.EmitEvent(
                        "action.result",
                        Name,
                        $"{request.AgentId} {request.ActionType} failed during route planning: {result.FailureReason ?? "unknown"}",
                        request.AgentId,
                        new Dictionary<string, string>
                        {
                            ["actionType"] = request.ActionType.ToString(),
                            ["succeeded"] = "False",
                            ["reason"] = result.FailureReason ?? ""
                        });
                    continue;
                }

                if (!result.RequiresMovement)
                {
                    continue;
                }

                state.PendingActionRequests.Remove(request);

                if (result.Movement is not null)
                {
                    state.ActiveMovements.Add(result.Movement);
                    state.EmitEvent(
                        "movement.planned",
                        Name,
                        $"{request.AgentId} planned movement for {request.ActionType}.",
                        request.AgentId,
                        new Dictionary<string, string>
                        {
                            ["actionType"] = request.ActionType.ToString(),
                            ["targetId"] = request.TargetId ?? ""
                        });
                }
            }
        }
    }
}
