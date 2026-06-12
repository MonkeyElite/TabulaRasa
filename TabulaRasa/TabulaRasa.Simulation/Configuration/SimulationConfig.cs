namespace TabulaRasa.Simulation.Configuration
{
    public sealed record LifecycleConfig(
        float AgeDaysPerTick = 0,
        float DaysPerYear = 365);

    public sealed record EnvironmentConfig(
        int DayLengthTicks = 100,
        int WeatherChangeIntervalTicks = 50,
        float BaseTemperature = 20,
        float DawnEndRatio = 0.15f,
        float DayEndRatio = 0.65f,
        float DuskEndRatio = 0.80f,
        int ClearWeatherWeight = 55,
        int RainWeatherWeight = 20,
        int HeatWeatherWeight = 15,
        int ColdWeatherWeight = 10,
        float DayTemperatureDelta = 4,
        float NightTemperatureDelta = -6,
        float DawnTemperatureDelta = -2,
        float DuskTemperatureDelta = 1,
        float HeatTemperatureDelta = 8,
        float ColdTemperatureDelta = -8,
        float RainTemperatureDelta = -2,
        float MaxPlantCooling = 3,
        float PlantCoolingFactor = 30,
        float MaxWaterCooling = 2,
        float WaterCoolingPerSource = 0.5f);

    public sealed record EcologyConfig(
        int InitialPlantCount = 3,
        int InitialWaterSourceCount = 1,
        int InitialResourceDepositCount = 1,
        int PlantRegrowthTicks = 5,
        int PlantDecayTicksAfterDepleted = 20,
        float WaterRefillPerRainTick = 0.5f,
        float WaterEvaporationPerHeatTick = 0.25f,
        int CollapsePlantYieldThreshold = 0,
        float CollapseWaterVolumeThreshold = 0,
        int RecoveryPlantYieldThreshold = 1,
        float RecoveryWaterVolumeThreshold = 1);

    public sealed record SpeciesPopulationConfig(
        int Human = 1,
        int Deer = 0,
        int Wolf = 0);

    public sealed record NeedDecayConfig(
        float HungerDelta = 0.08f,
        float ThirstDelta = 0.08f,
        float EnergyDelta = -0.02f,
        float FatigueDelta = 0.04f);

    public sealed record NeedRulesConfig(
        float MaximumNeedValue = 10,
        float MaximumEnergyValue = 10,
        float EatRecoveryAmount = 5,
        float DrinkRecoveryAmount = 5,
        float RestEnergyRecoveryAmount = 4,
        float RestFatigueRecoveryAmount = 5,
        float CriticalNeedThreshold = 8,
        float HarmNeedThreshold = 10,
        float ExhaustedEnergyThreshold = 0,
        float SurvivalDamagePerTick = 1,
        float HeatWeatherThirstMultiplier = 1.25f,
        float HotTemperatureThreshold = 30,
        float HotTemperatureThirstBonus = 0.25f,
        float ColdTemperatureThreshold = 5,
        float ColdTemperatureThirstBonus = -0.15f,
        float MinTemperatureThirstMultiplier = 0.5f,
        float MaxTemperatureThirstMultiplier = 2f);

    public sealed record GoalConfig(
        float HungerThreshold = 4f,
        float UrgentHungerThreshold = 7.5f,
        int InterruptionPriorityDelta = 20,
        float InventionMaxHunger = 4f,
        float InventionMaxThirst = 4f,
        float InventionMaxFatigue = 5f);

    public sealed record PathfindingConfig(
        bool AllowDiagonalMovement = false,
        int MaxVisitedCells = 1_000,
        int MaxRepathAttempts = 3,
        float ArrivalTolerance = 0.05f,
        float InteractionTolerance = 0.1f,
        float AgentInteractionRangeBonus = 0.5f);

    public sealed record SpawnResourceConfig(
        int FoodStackQuantity = 2,
        int PlantStartingYield = 4,
        int PlantMaxYield = 5,
        float WaterStartingVolume = 16,
        float WaterMaxVolume = 20,
        int DepositQuantity = 5,
        int DepositMaxQuantity = 5);

    public sealed record MemoryConfig(
        bool Enabled = true,
        int MaxMemoriesPerAgent = 100,
        int RetentionTicks = 80,
        float DecayPerTick = 0.02f,
        float MinimumStrength = 0.2f,
        float RecallThreshold = 0.35f);

    public sealed record TraitConfig(
        float InitialVariation = 0.12f,
        float MutationChancePerTrait = 0.08f,
        float MutationDelta = 0.06f);

    public sealed record StartingNeedsConfig(
        float Hunger = 1,
        float Thirst = 0.5f,
        float Energy = 10,
        float Fatigue = 0);

    public sealed record SpeciesRuleConfig(
        float MaxHealth = 10,
        int AdultAgeDays = 20,
        int MaxAgeDays = 2_000,
        int ReproductionCooldownTicks = 80,
        float PerceptionMultiplier = 1,
        float MovementSpeedMultiplier = 1,
        float AttackDamage = 2,
        float HungerDecayMultiplier = 1,
        float ThirstDecayMultiplier = 1,
        float FatigueDecayMultiplier = 1,
        StartingNeedsConfig? StartingNeeds = null,
        IReadOnlyList<string>? EdibleResourceIds = null,
        IReadOnlyList<string>? PreySpeciesIds = null);

    public sealed record SpeciesRulesConfig(
        SpeciesRuleConfig? Human = null,
        SpeciesRuleConfig? Deer = null,
        SpeciesRuleConfig? Wolf = null)
    {
        public SpeciesRuleConfig EffectiveHuman => Human ?? new SpeciesRuleConfig();
        public SpeciesRuleConfig EffectiveDeer => Deer ?? new SpeciesRuleConfig(
            MaxHealth: 6,
            AdultAgeDays: 15,
            MaxAgeDays: 1_200,
            ReproductionCooldownTicks: 60,
            PerceptionMultiplier: 1.15f,
            MovementSpeedMultiplier: 1.25f,
            AttackDamage: 0,
            HungerDecayMultiplier: 1.1f,
            ThirstDecayMultiplier: 1,
            FatigueDecayMultiplier: 0.9f,
            StartingNeeds: new StartingNeedsConfig(Hunger: 1.5f, Thirst: 0.75f));
        public SpeciesRuleConfig EffectiveWolf => Wolf ?? new SpeciesRuleConfig(
            MaxHealth: 8,
            AdultAgeDays: 18,
            MaxAgeDays: 1_400,
            ReproductionCooldownTicks: 90,
            PerceptionMultiplier: 1.25f,
            MovementSpeedMultiplier: 1.15f,
            AttackDamage: 4,
            HungerDecayMultiplier: 1.2f,
            ThirstDecayMultiplier: 1.05f,
            FatigueDecayMultiplier: 1,
            StartingNeeds: new StartingNeedsConfig(Hunger: 2.5f, Thirst: 0.75f));
    }

    public sealed record BehaviorWeightConfig(
        float Eat = 1f,
        float Drink = 1f,
        float Rest = 1f,
        float Wander = 1f,
        float Social = 1f,
        float Reproduce = 0.85f,
        float Flee = 1f,
        float Attack = 1f,
        float Craft = 1f,
        float Experiment = 1f,
        float ExplorationChance = 0.10f,
        float PersonalityInfluence = 0.35f);

    public sealed record SocialWeightConfig(
        float PerceptionFamiliarity = 0.08f,
        float CommunicationFamiliarity = 0.12f,
        float CommunicationTrust = 0.06f,
        float CommunicationFear = -0.02f,
        float CommunicationAffinity = 0.05f,
        float AttackTrust = -0.20f,
        float AttackFear = 0.35f,
        float AttackAffinity = -0.20f,
        float ReproductionFamiliarity = 0.20f,
        float ReproductionTrust = 0.10f,
        float ReproductionFear = -0.05f,
        float ReproductionAffinity = 0.25f);

    public sealed record ReproductionConfig(
        float NeedThreshold = 4f,
        float Range = 1.25f,
        float CooldownScale = 1f,
        float PopulationPressureInfluence = 0f,
        float ParentHungerCost = 1.25f,
        float ParentThirstCost = 0.75f,
        float ParentFatigueCost = 1f);

    public sealed record RecoveryConfig(
        int FailedTargetCooldownTicks = 20,
        int MaxRepeatedActionFailures = 3,
        int MaxGoalAgeTicks = 120,
        int IdleRecoveryTicks = 8,
        int MovementStuckTicks = 3);

    public sealed record BelievabilityConfig(
        BehaviorWeightConfig? Behavior = null,
        SocialWeightConfig? Social = null,
        ReproductionConfig? Reproduction = null,
        RecoveryConfig? Recovery = null)
    {
        public BehaviorWeightConfig EffectiveBehavior => Behavior ?? new BehaviorWeightConfig();
        public SocialWeightConfig EffectiveSocial => Social ?? new SocialWeightConfig();
        public ReproductionConfig EffectiveReproduction => Reproduction ?? new ReproductionConfig();
        public RecoveryConfig EffectiveRecovery => Recovery ?? new RecoveryConfig();
    }

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
        NeedRulesConfig? NeedRules = null,
        GoalConfig? Goals = null,
        float PerceptionRadius = 20f,
        float MovementSpeedPerTick = 0.25f,
        PathfindingConfig? Pathfinding = null,
        SpawnResourceConfig? SpawnResources = null,
        IReadOnlyList<string>? EnabledSystems = null,
        MemoryConfig? Memory = null,
        LifecycleConfig? Lifecycle = null,
        EnvironmentConfig? Environment = null,
        EcologyConfig? Ecology = null,
        SpeciesPopulationConfig? SpeciesPopulation = null,
        SpeciesRulesConfig? SpeciesRules = null,
        TraitConfig? Traits = null,
        BelievabilityConfig? Believability = null)
    {
        public static readonly IReadOnlyList<string> DefaultEnabledSystems =
        [
            "environment",
            "ecology",
            "lifecycle",
            "need-decay",
            "memory",
            "social",
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
            "recovery",
            "reporting"
        ];

        public NeedDecayConfig EffectiveNeedDecay => NeedDecay ?? new NeedDecayConfig();
        public NeedRulesConfig EffectiveNeedRules => NeedRules ?? new NeedRulesConfig();
        public GoalConfig EffectiveGoals => Goals ?? new GoalConfig();
        public PathfindingConfig EffectivePathfinding => Pathfinding ?? new PathfindingConfig();
        public SpawnResourceConfig EffectiveSpawnResources => SpawnResources ?? new SpawnResourceConfig();
        public MemoryConfig EffectiveMemory => Memory ?? new MemoryConfig();
        public LifecycleConfig EffectiveLifecycle => Lifecycle ?? new LifecycleConfig();
        public EnvironmentConfig EffectiveEnvironment => Environment ?? new EnvironmentConfig();
        public EcologyConfig EffectiveEcology => Ecology ?? new EcologyConfig();
        public SpeciesRulesConfig EffectiveSpeciesRules => SpeciesRules ?? new SpeciesRulesConfig();
        public TraitConfig EffectiveTraits => Traits ?? new TraitConfig();
        public BelievabilityConfig EffectiveBelievability => Believability ?? new BelievabilityConfig();
        public SpeciesPopulationConfig EffectiveSpeciesPopulation =>
            SpeciesPopulation ?? new SpeciesPopulationConfig(Human: InitialAgentCount, Deer: 0, Wolf: 0);
        public IReadOnlyList<string> EffectiveEnabledSystems => EnabledSystems ?? DefaultEnabledSystems;
    }
}
