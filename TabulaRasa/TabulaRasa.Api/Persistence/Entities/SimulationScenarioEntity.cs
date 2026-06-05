namespace TabulaRasa.Api.Persistence.Entities
{
    public sealed class SimulationScenarioEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public string ScenarioJson { get; set; } = "{}";
        public bool IsValid { get; set; }
        public string ValidationErrorsJson { get; set; } = "{}";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
