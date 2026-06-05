namespace TabulaRasa.Api.Persistence.Entities
{
    public sealed class SimulationEventEntity
    {
        public long Id { get; set; }
        public string RunId { get; set; } = string.Empty;
        public SimulationRunEntity? Run { get; set; }
        public long Tick { get; set; }
        public long Sequence { get; set; }
        public string Type { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string MetadataJson { get; set; } = "{}";
        public long PayloadBytes { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
