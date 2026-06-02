using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;

namespace TabulaRasa.Simulation.Actions.Requests
{
    public static class ActionRequestFactory
    {
        public static ActionRequest Create(AgentIntent intent)
        {
            return new ActionRequest(
                intent.AgentId,
                intent.ActionType,
                intent.TargetId,
                intent.ContextKey,
                intent.SelectedGoal,
                intent.TargetType,
                intent.Channel,
                intent.NeedsBefore);
        }
    }
}
