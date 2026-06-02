namespace TabulaRasa.Abstractions.Agents
{
    public interface IAgentMind
    {
        AgentIntent Decide(
            AgentPerception perception,
            AgentSnapshot self,
            AgentLearningProfile learning,
            Random random);
    }
}
