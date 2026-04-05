namespace TabulaRasa.Abstractions.Execution
{
    public enum SimulationPhase
    {
        PreUpdate = 0,
        Sensing = 1,
        Evaluation = 2,
        Execution = 3,
        PostUpdate = 4,
        Logging = 5
    }
}
