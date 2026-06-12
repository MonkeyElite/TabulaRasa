using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Species;

namespace TabulaRasa.Simulation.Scenarios
{
    public static class SimulationScenarioCatalog
    {
        public static IReadOnlyList<string> Names { get; } =
        [
            "stable-mixed",
            "starvation",
            "overpopulation",
            "resource-collapse",
            "recovery"
        ];

        public static SimulationConfig Create(string? name, int? seed = null)
        {
            string scenario = string.IsNullOrWhiteSpace(name) ? "stable-mixed" : name.Trim().ToLowerInvariant();
            SimulationConfig config = scenario switch
            {
                "starvation" => Starvation(),
                "overpopulation" => Overpopulation(),
                "resource-collapse" => ResourceCollapse(),
                "recovery" => Recovery(),
                _ => StableMixed()
            };

            return seed is null ? config : config with { Seed = seed.Value };
        }

        private static SimulationConfig StableMixed()
        {
            return new SimulationConfig(
                Seed: 12345,
                WorldWidth: 18,
                WorldHeight: 18,
                InitialFoodCount: 3,
                SnapshotHistoryLimit: 500,
                EventHistoryLimit: 500,
                NeedDecay: new NeedDecayConfig(0.012f, 0.012f, -0.0015f, 0.004f),
                PerceptionRadius: 12,
                MovementSpeedPerTick: 0.70f,
                Ecology: new EcologyConfig(
                    InitialPlantCount: 18,
                    InitialWaterSourceCount: 4,
                    InitialResourceDepositCount: 2,
                    PlantRegrowthTicks: 4,
                    PlantDecayTicksAfterDepleted: 40,
                    WaterRefillPerRainTick: 0.8f,
                    WaterEvaporationPerHeatTick: 0.18f),
                SpeciesPopulation: new SpeciesPopulationConfig(Human: 2, Deer: 6, Wolf: 1),
                SpeciesRules: new SpeciesRulesConfig(
                    Human: new SpeciesRuleConfig(
                        HungerDecayMultiplier: 0.85f,
                        ThirstDecayMultiplier: 0.85f,
                        FatigueDecayMultiplier: 0.80f),
                    Deer: new SpeciesRuleConfig(
                        MaxHealth: 6,
                        AdultAgeDays: 15,
                        MaxAgeDays: 1_200,
                        ReproductionCooldownTicks: 60,
                        PerceptionMultiplier: 1.15f,
                        MovementSpeedMultiplier: 1.25f,
                        AttackDamage: 0,
                        HungerDecayMultiplier: 0.85f,
                        ThirstDecayMultiplier: 0.85f,
                        FatigueDecayMultiplier: 0.75f,
                        StartingNeeds: new StartingNeedsConfig(Hunger: 1.2f, Thirst: 0.5f)),
                    Wolf: new SpeciesRuleConfig(
                        MaxHealth: 8,
                        AdultAgeDays: 18,
                        MaxAgeDays: 1_400,
                        ReproductionCooldownTicks: 90,
                        PerceptionMultiplier: 1.25f,
                        MovementSpeedMultiplier: 1.15f,
                        AttackDamage: 4,
                        HungerDecayMultiplier: 0.35f,
                        ThirstDecayMultiplier: 0.80f,
                        FatigueDecayMultiplier: 0.75f,
                        StartingNeeds: new StartingNeedsConfig(Hunger: 1.5f, Thirst: 0.5f),
                        PreySpeciesIds: [SpeciesRegistry.DeerId])),
                Believability: StableBelievability());
        }

        private static SimulationConfig Starvation()
        {
            return StableMixed() with
            {
                InitialFoodCount = 0,
                Ecology = new EcologyConfig(0, 1, 0, 20, 20, 0, 0),
                SpeciesPopulation = new SpeciesPopulationConfig(Human: 2, Deer: 2, Wolf: 0)
            };
        }

        private static SimulationConfig Overpopulation()
        {
            return StableMixed() with
            {
                WorldWidth = 12,
                WorldHeight = 12,
                InitialFoodCount = 2,
                Ecology = new EcologyConfig(8, 2, 0, 8, 35, 0.4f, 0.2f),
                SpeciesPopulation = new SpeciesPopulationConfig(Human: 2, Deer: 14, Wolf: 0)
            };
        }

        private static SimulationConfig ResourceCollapse()
        {
            return StableMixed() with
            {
                InitialFoodCount = 0,
                Ecology = new EcologyConfig(3, 1, 0, 50, 5, 0, 1.25f),
                SpeciesPopulation = new SpeciesPopulationConfig(Human: 1, Deer: 6, Wolf: 0)
            };
        }

        private static SimulationConfig Recovery()
        {
            return StableMixed() with
            {
                InitialFoodCount = 0,
                Ecology = new EcologyConfig(6, 2, 0, 2, 60, 1.2f, 0.05f),
                SpeciesPopulation = new SpeciesPopulationConfig(Human: 1, Deer: 4, Wolf: 0)
            };
        }

        private static BelievabilityConfig StableBelievability()
        {
            return new BelievabilityConfig(
                new BehaviorWeightConfig(
                    Eat: 1.65f,
                    Drink: 1.45f,
                    Rest: 1.25f,
                    Wander: 0.65f,
                    Social: 0.85f,
                    Reproduce: 0.65f,
                    Flee: 1.25f,
                    Attack: 1.10f,
                    Craft: 0.9f,
                    Experiment: 0.75f,
                    ExplorationChance: 0.08f,
                    PersonalityInfluence: 0.30f),
                Reproduction: new ReproductionConfig(
                    NeedThreshold: 3.5f,
                    Range: 1.25f,
                    CooldownScale: 1.35f,
                    PopulationPressureInfluence: 0.75f,
                    ParentHungerCost: 1.25f,
                    ParentThirstCost: 0.75f,
                    ParentFatigueCost: 1.0f),
                Recovery: new RecoveryConfig(
                    FailedTargetCooldownTicks: 20,
                    MaxRepeatedActionFailures: 3,
                    MaxGoalAgeTicks: 100,
                    IdleRecoveryTicks: 8,
                    MovementStuckTicks: 3));
        }
    }
}
