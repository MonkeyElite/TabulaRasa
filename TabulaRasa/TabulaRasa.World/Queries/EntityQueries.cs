using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.World.Queries
{
    public static class EntityQueries
    {
        public static AgentEntity? GetAgentEntity(WorldState worldState, string agentId)
        {
            return worldState.Agents.FirstOrDefault(a => a.Id == agentId);
        }
    }
}
