using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.World.State
{
    public sealed class AgentEntity
    {
        public required string Id { get; init; }
        public required string Position { get; set; }
        public int Hunger { get; set; }
        public IAgentMind Mind { get; init; } = default!;
        public AgentDecision? PendingDecision { get; set; }
    }
}
