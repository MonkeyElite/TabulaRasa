namespace TabulaRasa.Simulation.Configuration
{
    public sealed record NeedDecayConfig(
        float HungerDelta = 1,
        float ThirstDelta = 1,
        float EnergyDelta = -1);

    public sealed record PathfindingConfig(
        bool AllowDiagonalMovement = false,
        int MaxVisitedCells = 1_000);

    public sealed record SimulationConfig(
        int Seed = 12345,
        int WorldWidth = 10,
        int WorldHeight = 10,
        int TickIntervalMilliseconds = 500,
        int InitialAgentCount = 1,
        int InitialFoodCount = 1,
        int EventHistoryLimit = 100,
        int SnapshotHistoryLimit = 100,
        NeedDecayConfig? NeedDecay = null,
        float PerceptionRadius = 20f,
        float MovementSpeedPerTick = 0.25f,
        PathfindingConfig? Pathfinding = null,
        IReadOnlyList<string>? EnabledSystems = null)
    {
        public static readonly IReadOnlyList<string> DefaultEnabledSystems =
        [
            "need-decay",
            "planning",
            "action-request-creation",
            "route-planning",
            "job-activation",
            "task-assignment",
            "movement-execution",
            "task-execution",
            "action-execution",
            "reporting"
        ];

        public NeedDecayConfig EffectiveNeedDecay => NeedDecay ?? new NeedDecayConfig();
        public PathfindingConfig EffectivePathfinding => Pathfinding ?? new PathfindingConfig();
        public IReadOnlyList<string> EffectiveEnabledSystems => EnabledSystems ?? DefaultEnabledSystems;
    }
}
