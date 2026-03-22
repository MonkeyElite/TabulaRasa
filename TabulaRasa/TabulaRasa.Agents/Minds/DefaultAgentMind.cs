using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.Agents.Minds
{
    public class DefaultAgentMind : IAgentMind
    {
        public AgentDecision Decide(AgentPerception perception, AgentSnapshot self)
        {
            if (self.Hunger >= 5 && perception.FoodNearby)
            {
                return new AgentDecision(AgentActionType.Eat, null);
            }

            return new AgentDecision(AgentActionType.Wander, null);
        }
    }
}
