using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentSnapshot(
        string AgentId,
        AgentNeedsSnapshot Needs,
        WorldPosition Position,
        IReadOnlyDictionary<string, int>? Inventory = null,
        string SpeciesId = "human",
        int AgeTicks = 0,
        bool IsAdult = true,
        long? LastReproducedTick = null);
}
