namespace TabulaRasa.Api.Persistence.Entities
{
    public sealed class SimulationTickSummaryEntity
    {
        public long Id { get; set; }
        public string RunId { get; set; } = string.Empty;
        public SimulationRunEntity? Run { get; set; }
        public long Tick { get; set; }
        public double DurationMilliseconds { get; set; }
        public int EventCount { get; set; }
        public int PopulationCount { get; set; }
        public int AliveAgentCount { get; set; }
        public int DeadAgentCount { get; set; }
        public int ResourceContainerCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
