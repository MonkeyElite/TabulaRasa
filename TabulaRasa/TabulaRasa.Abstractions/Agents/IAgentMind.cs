namespace TabulaRasa.Abstractions.Agents
{
    public interface IAgentMind
    {
        AgentDecision Decide(AgentPerception perception, AgentSnapshot self);
    }
}
