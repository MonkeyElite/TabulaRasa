namespace TabulaRasa.Simulation.Knowledge
{
    public sealed class KnowledgeRecord
    {
        public required string Id { get; init; }
        public required KnowledgeKind Kind { get; init; }
        public required string SubjectId { get; init; }
        public required string DisplayName { get; set; }
        public required long DiscoveredTick { get; init; }
        public required long LastUpdatedTick { get; set; }
        public required string Source { get; set; }
        public string? SourceAgentId { get; set; }
        public Dictionary<string, string> Metadata { get; } = [];
    }
}
