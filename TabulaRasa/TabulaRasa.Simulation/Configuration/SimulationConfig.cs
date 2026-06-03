namespace TabulaRasa.Simulation.Configuration
{
    public sealed record NeedDecayConfig(
        float HungerDelta = 1,
        float ThirstDelta = 1,
        float EnergyDelta = -1,
        float FatigueDelta = 1);

    public sealed record PathfindingConfig(
        bool AllowDiagonalMovement = false,
        int MaxVisitedCells = 1_000,
        int MaxRepathAttempts = 3);

    public sealed record MemoryConfig(
        bool Enabled = true,
        int MaxMemoriesPerAgent = 100,
        int RetentionTicks = 80,
        float DecayPerTick = 0.02f,
        float MinimumStrength = 0.2f,
        float RecallThreshold = 0.35f);

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
        IReadOnlyList<string>? EnabledSystems = null,
        MemoryConfig? Memory = null)
    {
        public static readonly IReadOnlyList<string> DefaultEnabledSystems =
        [
            "need-decay",
            "memory",
            "planning",
            "goal-generation",
            "action-request-creation",
            "route-planning",
            "job-activation",
            "task-assignment",
            "task-action-dispatch",
            "movement-execution",
            "task-execution",
            "action-execution",
            "reporting"
        ];

        public NeedDecayConfig EffectiveNeedDecay => NeedDecay ?? new NeedDecayConfig();
        public PathfindingConfig EffectivePathfinding => Pathfinding ?? new PathfindingConfig();
        public MemoryConfig EffectiveMemory => Memory ?? new MemoryConfig();
        public IReadOnlyList<string> EffectiveEnabledSystems => EnabledSystems ?? DefaultEnabledSystems;
    }
}
