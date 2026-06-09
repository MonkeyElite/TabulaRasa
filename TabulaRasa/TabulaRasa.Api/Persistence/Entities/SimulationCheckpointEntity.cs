namespace TabulaRasa.Api.Persistence.Entities
{
    public sealed class SimulationCheckpointEntity
    {
        public long Id { get; set; }
        public string RunId { get; set; } = string.Empty;
        public SimulationRunEntity? Run { get; set; }
        public long Tick { get; set; }
        public string PayloadJson { get; set; } = "{}";
        public byte[]? CompressedPayload { get; set; }
        public long PayloadBytes { get; set; }
        public bool IsCompressed { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
