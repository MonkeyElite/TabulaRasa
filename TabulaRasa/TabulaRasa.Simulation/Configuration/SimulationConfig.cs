namespace TabulaRasa.Simulation.Configuration
{
    public sealed record EnvironmentConfig(
        int DayLengthTicks = 100,
        int WeatherChangeIntervalTicks = 50,
        float BaseTemperature = 20);

    public sealed record EcologyConfig(
        int InitialPlantCount = 3,
        int InitialWaterSourceCount = 1,
        int InitialResourceDepositCount = 1,
        int PlantRegrowthTicks = 5,
        int PlantDecayTicksAfterDepleted = 20,
        float WaterRefillPerRainTick = 0.5f,
        float WaterEvaporationPerHeatTick = 0.25f);

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
        MemoryConfig? Memory = null,
        EnvironmentConfig? Environment = null,
        EcologyConfig? Ecology = null)
    {
        public static readonly IReadOnlyList<string> DefaultEnabledSystems =
        [
            "environment",
            "ecology",
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
        public EnvironmentConfig EffectiveEnvironment => Environment ?? new EnvironmentConfig();
        public EcologyConfig EffectiveEcology => Ecology ?? new EcologyConfig();
        public IReadOnlyList<string> EffectiveEnabledSystems => EnabledSystems ?? DefaultEnabledSystems;
    }
}
