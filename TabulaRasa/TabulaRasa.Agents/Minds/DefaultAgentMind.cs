using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.Agents.Minds
{
    public class DefaultAgentMind : IAgentMind
    {
        public ActionRequest Decide(AgentPerception perception, AgentSnapshot self)
        {
            if (self.Hunger >= 5 && perception.FoodNearby)
            {
                return new ActionRequest(self.AgentId, AgentActionType.Eat, null);
            }

            return new ActionRequest(self.AgentId, AgentActionType.Wander, null);
        }
    }
}
