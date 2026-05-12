using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.Agents.Minds
{
    public class DefaultAgentMind : IAgentMind
    {
        public AgentIntent Decide(AgentPerception perception, AgentSnapshot self)
        {
            InteractionOpportunity? foodOpportunity = perception.FindOpportunity(AgentActionType.Eat);

            if (self.Needs.Hunger >= 5 && foodOpportunity is not null)
            {
                return new AgentIntent(self.AgentId, AgentActionType.Eat, foodOpportunity.TargetId);
            }

            return new AgentIntent(self.AgentId, AgentActionType.Wander, null);
        }
    }
}
