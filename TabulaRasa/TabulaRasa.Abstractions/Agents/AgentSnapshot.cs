using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentSnapshot(
        string AgentId,
        AgentNeedsSnapshot Needs,
        WorldPosition Position,
        IReadOnlyDictionary<string, int>? Inventory = null);
}
