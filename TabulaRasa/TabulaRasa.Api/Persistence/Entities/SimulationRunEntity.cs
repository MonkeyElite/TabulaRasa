namespace TabulaRasa.Api.Persistence.Entities
{
    public sealed class SimulationRunEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Idle";
        public long CurrentTick { get; set; }
        public int AgentCount { get; set; }
        public int AliveAgentCount { get; set; }
        public int DeadAgentCount { get; set; }
        public int FoodCount { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public string ConfigJson { get; set; } = "{}";
        public string? SourceSimulationId { get; set; }
        public long? SourceTick { get; set; }
        public long StorageBytes { get; set; }
        public long CheckpointBytes { get; set; }
        public long EventBytes { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<SimulationCheckpointEntity> Checkpoints { get; } = [];
        public List<SimulationEventEntity> Events { get; } = [];
        public List<SimulationTickSummaryEntity> TickSummaries { get; } = [];
    }
}
