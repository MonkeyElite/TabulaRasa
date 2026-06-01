namespace TabulaRasa.Simulation.Observability
{
    public sealed record SimulationEvent(
        long Tick,
        long Sequence,
        string Type,
        string SourceSystem,
        string Message,
        string? EntityId,
        IReadOnlyDictionary<string, string> Metadata);
}
