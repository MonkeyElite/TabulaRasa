using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentSnapshot(string AgentId, float Hunger, WorldPosition Position);
}
