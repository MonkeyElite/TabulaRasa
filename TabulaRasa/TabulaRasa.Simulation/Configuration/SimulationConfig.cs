namespace TabulaRasa.Simulation.Configuration
{
    public sealed record SimulationConfig(
        int Seed = 12345,
        int EventHistoryLimit = 100,
        int TickIntervalMilliseconds = 500);
}
