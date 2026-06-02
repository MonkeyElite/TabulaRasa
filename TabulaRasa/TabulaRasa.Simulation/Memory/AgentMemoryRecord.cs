using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Simulation.Memory
{
    public sealed class AgentMemoryRecord
    {
        public required string Id { get; init; }
        public required AgentMemoryKind Kind { get; init; }
        public required string SubjectId { get; set; }
        public required string SubjectType { get; set; }
        public required WorldPosition Position { get; set; }
        public required long CreatedTick { get; init; }
        public required long LastUpdatedTick { get; set; }
        public required float Strength { get; set; }
        public required float Certainty { get; set; }
        public long? ExpiresAtTick { get; set; }
        public required string Summary { get; set; }
        public Dictionary<string, string> Metadata { get; } = [];

        public bool IsExpired(long tick, float minimumStrength)
        {
            return Strength < minimumStrength || ExpiresAtTick is { } expiresAtTick && expiresAtTick <= tick;
        }
    }
}
