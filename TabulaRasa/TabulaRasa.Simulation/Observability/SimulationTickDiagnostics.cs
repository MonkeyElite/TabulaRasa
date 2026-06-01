namespace TabulaRasa.Simulation.Observability
{
    public sealed record SimulationTickDiagnostics(
        long Tick,
        DateTimeOffset StartedAt,
        DateTimeOffset CompletedAt,
        double DurationMilliseconds,
        int EventCount,
        IReadOnlyList<SystemExecutionDiagnostic> Systems);
}
