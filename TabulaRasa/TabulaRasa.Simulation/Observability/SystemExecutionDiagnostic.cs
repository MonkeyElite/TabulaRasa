using TabulaRasa.Abstractions.Execution;

namespace TabulaRasa.Simulation.Observability
{
    public sealed record SystemExecutionDiagnostic(
        SimulationPhase Phase,
        string SystemName,
        int Priority,
        double DurationMilliseconds,
        int EmittedEventCount);
}
