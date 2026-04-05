using TabulaRasa.Abstractions.Agents.Actions;

namespace TabulaRasa.Abstractions.Agents
{
    public interface IAgentMind
    {
        ActionRequest Decide(AgentPerception perception, AgentSnapshot self);
    }
}
