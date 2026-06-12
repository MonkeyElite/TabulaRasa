using System.Diagnostics.CodeAnalysis;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Goals;
using TabulaRasa.Simulation.Knowledge;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Observability;
using TabulaRasa.Simulation.Social;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.State
{
    public sealed class SimulationState
    {
        public required WorldState World { get; set; }
        public required SimulationTime Time { get; set; }
        public List<AgentState> Agents { get; set; } = [];
        public List<AgentIntent> PendingIntents { get; } = [];
        public List<ActionRequest> PendingActionRequests { get; } = [];
        public List<ActionResult> ActionResults { get; } = [];
        public List<ActiveMovement> ActiveMovements { get; } = [];
        public List<AgentGoal> Goals { get; } = [];
        public List<JobInstance> PendingJobs { get; } = [];
        public List<JobInstance> ActiveJobs { get; } = [];
        public ReservationRegistry Reservations { get; } = new();
        public Dictionary<string, AgentPerception> LatestPerceptionsByAgentId { get; } = [];
        public Dictionary<string, AgentMemoryStore> MemoryStoresByAgentId { get; } = [];
        public Dictionary<string, AgentKnowledgeStore> KnowledgeStoresByAgentId { get; } = [];
        public Dictionary<string, AgentSocialStore> SocialStoresByAgentId { get; } = [];
        public Dictionary<string, Dictionary<string, long>> FailedTargetCooldownsByAgentId { get; } = [];
        public Dictionary<string, int> RepeatedActionFailuresByKey { get; } = [];
        public Dictionary<string, long> AgentIdleSinceTickByAgentId { get; } = [];
        public SimulationConfig Config { get; private set; }
        public Random Random { get; private set; }
        public long ActiveTick => _activeEventTick;
        public IReadOnlyList<SimulationEvent> CurrentTickEvents => _currentTickEvents;
        public IReadOnlyDictionary<long, IReadOnlyList<SimulationEvent>> EventHistory => _eventsByTick;
        public IReadOnlyDictionary<long, SimulationTickDiagnostics> DiagnosticsHistory => _diagnosticsByTick;

        public bool IsRunning { get; set; } = true;

        private readonly List<SimulationEvent> _currentTickEvents = [];
        private readonly SortedDictionary<long, IReadOnlyList<SimulationEvent>> _eventsByTick = [];
        private readonly SortedDictionary<long, SimulationTickDiagnostics> _diagnosticsByTick = [];
        private long _eventSequence;
        private long _activeEventTick;

        [SetsRequiredMembers]
        public SimulationState(
            WorldState world,
            SimulationTime time,
            List<AgentState> agentStates,
            SimulationConfig? config = null)
        {
            World = world;
            Time = time;
            Agents = agentStates;
            Config = NormalizeConfig(config ?? new SimulationConfig());
            Random = new Random(Config.Seed);
            _activeEventTick = time.Tick;
        }

        public AgentState? GetAgentById(string id)
        {
            AgentState? agent = Agents.Find(a => a.Id == id);

            return agent;
        }

        public AgentMemoryStore GetMemoryStore(string agentId)
        {
            if (!MemoryStoresByAgentId.TryGetValue(agentId, out AgentMemoryStore? store))
            {
                store = new AgentMemoryStore();
                MemoryStoresByAgentId[agentId] = store;
            }

            return store;
        }

        public AgentSocialStore GetSocialStore(string agentId)
        {
            if (!SocialStoresByAgentId.TryGetValue(agentId, out AgentSocialStore? store))
            {
                store = new AgentSocialStore();
                SocialStoresByAgentId[agentId] = store;
            }

            return store;
        }

        public AgentKnowledgeStore GetKnowledgeStore(string agentId)
        {
            if (!KnowledgeStoresByAgentId.TryGetValue(agentId, out AgentKnowledgeStore? store))
            {
                store = new AgentKnowledgeStore();
                KnowledgeStoresByAgentId[agentId] = store;
            }

            return store;
        }

        public void BeginTickObservability(long tick)
        {
            _activeEventTick = tick;
            _eventSequence = 0;
            _currentTickEvents.Clear();
        }

        public SimulationEvent EmitEvent(
            string type,
            string sourceSystem,
            string message,
            string? entityId = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            string severity = "info",
            float importance = 0,
            IReadOnlyList<string>? tags = null)
        {
            SimulationEvent simulationEvent = new(
                _activeEventTick,
                ++_eventSequence,
                type,
                sourceSystem,
                message,
                entityId,
                metadata ?? new Dictionary<string, string>(),
                severity,
                ClampImportance(importance),
                tags ?? []);

            _currentTickEvents.Add(simulationEvent);

            return simulationEvent;
        }

        public SimulationEvent RecordEvent(
            long tick,
            string type,
            string sourceSystem,
            string message,
            string? entityId = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            string severity = "info",
            float importance = 0,
            IReadOnlyList<string>? tags = null)
        {
            List<SimulationEvent> events = _eventsByTick.TryGetValue(tick, out IReadOnlyList<SimulationEvent>? existingEvents)
                ? existingEvents.ToList()
                : [];
            long sequence = events.Count == 0 ? 1 : events.Max(simulationEvent => simulationEvent.Sequence) + 1;
            SimulationEvent simulationEvent = new(
                tick,
                sequence,
                type,
                sourceSystem,
                message,
                entityId,
                metadata ?? new Dictionary<string, string>(),
                severity,
                ClampImportance(importance),
                tags ?? []);

            events.Add(simulationEvent);
            _eventsByTick[tick] = events;
            TrimHistory(_eventsByTick, Config.EventHistoryLimit);

            return simulationEvent;
        }

        public void CompleteTickObservability(long tick, SimulationTickDiagnostics diagnostics)
        {
            _eventsByTick[tick] = _currentTickEvents.ToList();
            _diagnosticsByTick[tick] = diagnostics;
            TrimHistory(_eventsByTick, Config.EventHistoryLimit);
            TrimHistory(_diagnosticsByTick, Config.EventHistoryLimit);
        }

        public IReadOnlyList<SimulationEvent> GetEventsForTick(long tick)
        {
            return _eventsByTick.GetValueOrDefault(tick) ?? [];
        }

        public SimulationTickDiagnostics? GetDiagnosticsForTick(long tick)
        {
            return _diagnosticsByTick.GetValueOrDefault(tick);
        }

        public IReadOnlyList<SimulationEvent> GetRecentEvents()
        {
            return _eventsByTick.Values.SelectMany(events => events).ToList();
        }

        public void ApplyConfig(SimulationConfig config, bool reseedRandom = false)
        {
            Config = NormalizeConfig(config);
            if (reseedRandom)
            {
                Random = new Random(Config.Seed);
            }

            TrimHistory(_eventsByTick, Config.EventHistoryLimit);
            TrimHistory(_diagnosticsByTick, Config.EventHistoryLimit);
        }

        private static SimulationConfig NormalizeConfig(SimulationConfig config)
        {
            NeedDecayConfig needDecay = config.EffectiveNeedDecay;
            NeedRulesConfig needRules = config.EffectiveNeedRules;
            GoalConfig goals = config.EffectiveGoals;
            PathfindingConfig pathfinding = config.EffectivePathfinding;
            SpawnResourceConfig spawnResources = config.EffectiveSpawnResources;
            MemoryConfig memory = config.EffectiveMemory;
            LifecycleConfig lifecycle = config.EffectiveLifecycle;
            EnvironmentConfig environment = config.EffectiveEnvironment;
            EcologyConfig ecology = config.EffectiveEcology;
            SpeciesRulesConfig speciesRules = config.EffectiveSpeciesRules;
            TraitConfig traits = config.EffectiveTraits;
            BelievabilityConfig believability = config.EffectiveBelievability;
            BehaviorWeightConfig behavior = believability.EffectiveBehavior;
            SocialWeightConfig social = believability.EffectiveSocial;
            ReproductionConfig reproduction = believability.EffectiveReproduction;
            RecoveryConfig recovery = believability.EffectiveRecovery;
            SpeciesPopulationConfig speciesPopulation = config.EffectiveSpeciesPopulation;
            IReadOnlyList<string> enabledSystems = config.EffectiveEnabledSystems
                .Where(systemId => !string.IsNullOrWhiteSpace(systemId))
                .Select(systemId => systemId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return config with
            {
                WorldWidth = Math.Clamp(config.WorldWidth, 1, 500),
                WorldHeight = Math.Clamp(config.WorldHeight, 1, 500),
                InitialAgentCount = Math.Clamp(config.InitialAgentCount, 0, 10_000),
                InitialFoodCount = Math.Clamp(config.InitialFoodCount, 0, 10_000),
                EventHistoryLimit = Math.Clamp(config.EventHistoryLimit, 1, 10_000),
                SnapshotHistoryLimit = Math.Clamp(config.SnapshotHistoryLimit, 1, 10_000),
                TickIntervalMilliseconds = Math.Clamp(config.TickIntervalMilliseconds, 50, 60_000),
                NeedDecay = new NeedDecayConfig(
                    ClampFinite(needDecay.HungerDelta, -100, 100),
                    ClampFinite(needDecay.ThirstDelta, -100, 100),
                    ClampFinite(needDecay.EnergyDelta, -100, 100),
                    ClampFinite(needDecay.FatigueDelta, -100, 100)),
                NeedRules = new NeedRulesConfig(
                    ClampFinite(needRules.MaximumNeedValue, 1, 1_000),
                    ClampFinite(needRules.MaximumEnergyValue, 1, 1_000),
                    ClampFinite(needRules.EatRecoveryAmount, 0, 1_000),
                    ClampFinite(needRules.DrinkRecoveryAmount, 0, 1_000),
                    ClampFinite(needRules.RestEnergyRecoveryAmount, 0, 1_000),
                    ClampFinite(needRules.RestFatigueRecoveryAmount, 0, 1_000),
                    ClampFinite(needRules.CriticalNeedThreshold, 0, needRules.MaximumNeedValue),
                    ClampFinite(needRules.HarmNeedThreshold, 0, needRules.MaximumNeedValue),
                    ClampFinite(needRules.ExhaustedEnergyThreshold, 0, needRules.MaximumEnergyValue),
                    ClampFinite(needRules.SurvivalDamagePerTick, 0, 1_000),
                    ClampFinite(needRules.HeatWeatherThirstMultiplier, 0, 10),
                    ClampFinite(needRules.HotTemperatureThreshold, -100, 100),
                    ClampFinite(needRules.HotTemperatureThirstBonus, -10, 10),
                    ClampFinite(needRules.ColdTemperatureThreshold, -100, 100),
                    ClampFinite(needRules.ColdTemperatureThirstBonus, -10, 10),
                    ClampFinite(needRules.MinTemperatureThirstMultiplier, 0, 10),
                    ClampFinite(needRules.MaxTemperatureThirstMultiplier, 0, 10)),
                Goals = new GoalConfig(
                    ClampFinite(goals.HungerThreshold, 0, 1_000),
                    ClampFinite(goals.UrgentHungerThreshold, 0, 1_000),
                    Math.Clamp(goals.InterruptionPriorityDelta, 0, 1_000),
                    ClampFinite(goals.InventionMaxHunger, 0, 1_000),
                    ClampFinite(goals.InventionMaxThirst, 0, 1_000),
                    ClampFinite(goals.InventionMaxFatigue, 0, 1_000)),
                PerceptionRadius = ClampFinite(config.PerceptionRadius, 0, 1_000),
                MovementSpeedPerTick = ClampFinite(config.MovementSpeedPerTick, 0.01f, 100),
                Pathfinding = new PathfindingConfig(
                    pathfinding.AllowDiagonalMovement,
                    Math.Clamp(pathfinding.MaxVisitedCells, 1, 1_000_000),
                    Math.Clamp(pathfinding.MaxRepathAttempts, 0, 1_000),
                    ClampFinite(pathfinding.ArrivalTolerance, 0, 10),
                    ClampFinite(pathfinding.InteractionTolerance, 0, 10),
                    ClampFinite(pathfinding.AgentInteractionRangeBonus, 0, 10)),
                SpawnResources = new SpawnResourceConfig(
                    Math.Clamp(spawnResources.FoodStackQuantity, 0, 1_000),
                    Math.Clamp(spawnResources.PlantStartingYield, 0, 1_000),
                    Math.Clamp(spawnResources.PlantMaxYield, 1, 1_000),
                    ClampFinite(spawnResources.WaterStartingVolume, 0, 1_000_000),
                    ClampFinite(spawnResources.WaterMaxVolume, 0.1f, 1_000_000),
                    Math.Clamp(spawnResources.DepositQuantity, 0, 1_000_000),
                    Math.Clamp(spawnResources.DepositMaxQuantity, 1, 1_000_000)),
                Memory = new MemoryConfig(
                    memory.Enabled,
                    Math.Clamp(memory.MaxMemoriesPerAgent, 1, 10_000),
                    Math.Clamp(memory.RetentionTicks, 1, 1_000_000),
                    ClampFinite(memory.DecayPerTick, 0, 100),
                    ClampFinite(memory.MinimumStrength, 0, 1),
                    ClampFinite(memory.RecallThreshold, 0, 1)),
                Lifecycle = new LifecycleConfig(
                    ClampFinite(
                        lifecycle.AgeDaysPerTick <= 0
                            ? 1f / Math.Max(1, environment.DayLengthTicks)
                            : lifecycle.AgeDaysPerTick,
                        0.000001f,
                        1_000),
                    ClampFinite(lifecycle.DaysPerYear, 1, 10_000)),
                Environment = new EnvironmentConfig(
                    Math.Clamp(environment.DayLengthTicks, 4, 100_000),
                    Math.Clamp(environment.WeatherChangeIntervalTicks, 1, 1_000_000),
                    ClampFinite(environment.BaseTemperature, -100, 100),
                    ClampFinite(environment.DawnEndRatio, 0, 1),
                    ClampFinite(environment.DayEndRatio, 0, 1),
                    ClampFinite(environment.DuskEndRatio, 0, 1),
                    Math.Clamp(environment.ClearWeatherWeight, 0, 10_000),
                    Math.Clamp(environment.RainWeatherWeight, 0, 10_000),
                    Math.Clamp(environment.HeatWeatherWeight, 0, 10_000),
                    Math.Clamp(environment.ColdWeatherWeight, 0, 10_000),
                    ClampFinite(environment.DayTemperatureDelta, -100, 100),
                    ClampFinite(environment.NightTemperatureDelta, -100, 100),
                    ClampFinite(environment.DawnTemperatureDelta, -100, 100),
                    ClampFinite(environment.DuskTemperatureDelta, -100, 100),
                    ClampFinite(environment.HeatTemperatureDelta, -100, 100),
                    ClampFinite(environment.ColdTemperatureDelta, -100, 100),
                    ClampFinite(environment.RainTemperatureDelta, -100, 100),
                    ClampFinite(environment.MaxPlantCooling, 0, 100),
                    ClampFinite(environment.PlantCoolingFactor, 0, 1_000),
                    ClampFinite(environment.MaxWaterCooling, 0, 100),
                    ClampFinite(environment.WaterCoolingPerSource, 0, 100)),
                Ecology = new EcologyConfig(
                    Math.Clamp(ecology.InitialPlantCount, 0, 10_000),
                    Math.Clamp(ecology.InitialWaterSourceCount, 0, 10_000),
                    Math.Clamp(ecology.InitialResourceDepositCount, 0, 10_000),
                    Math.Clamp(ecology.PlantRegrowthTicks, 0, 1_000_000),
                    Math.Clamp(ecology.PlantDecayTicksAfterDepleted, 1, 1_000_000),
                    ClampFinite(ecology.WaterRefillPerRainTick, 0, 1_000),
                    ClampFinite(ecology.WaterEvaporationPerHeatTick, 0, 1_000),
                    Math.Clamp(ecology.CollapsePlantYieldThreshold, 0, 1_000_000),
                    ClampFinite(ecology.CollapseWaterVolumeThreshold, 0, 1_000_000),
                    Math.Clamp(ecology.RecoveryPlantYieldThreshold, 0, 1_000_000),
                    ClampFinite(ecology.RecoveryWaterVolumeThreshold, 0, 1_000_000)),
                SpeciesRules = new SpeciesRulesConfig(
                    NormalizeSpeciesRule(speciesRules.EffectiveHuman, 10),
                    NormalizeSpeciesRule(speciesRules.EffectiveDeer, 6),
                    NormalizeSpeciesRule(speciesRules.EffectiveWolf, 8)),
                Traits = new TraitConfig(
                    ClampFinite(traits.InitialVariation, 0, 1),
                    ClampFinite(traits.MutationChancePerTrait, 0, 1),
                    ClampFinite(traits.MutationDelta, 0, 1)),
                Believability = new BelievabilityConfig(
                    new BehaviorWeightConfig(
                        ClampFinite(behavior.Eat, 0, 5),
                        ClampFinite(behavior.Drink, 0, 5),
                        ClampFinite(behavior.Rest, 0, 5),
                        ClampFinite(behavior.Wander, 0, 5),
                        ClampFinite(behavior.Social, 0, 5),
                        ClampFinite(behavior.Reproduce, 0, 5),
                        ClampFinite(behavior.Flee, 0, 5),
                        ClampFinite(behavior.Attack, 0, 5),
                        ClampFinite(behavior.Craft, 0, 5),
                        ClampFinite(behavior.Experiment, 0, 5),
                        ClampFinite(behavior.ExplorationChance, 0, 1),
                        ClampFinite(behavior.PersonalityInfluence, 0, 1)),
                    new SocialWeightConfig(
                        ClampFinite(social.PerceptionFamiliarity, -1, 1),
                        ClampFinite(social.CommunicationFamiliarity, -1, 1),
                        ClampFinite(social.CommunicationTrust, -1, 1),
                        ClampFinite(social.CommunicationFear, -1, 1),
                        ClampFinite(social.CommunicationAffinity, -1, 1),
                        ClampFinite(social.AttackTrust, -1, 1),
                        ClampFinite(social.AttackFear, -1, 1),
                        ClampFinite(social.AttackAffinity, -1, 1),
                        ClampFinite(social.ReproductionFamiliarity, -1, 1),
                        ClampFinite(social.ReproductionTrust, -1, 1),
                        ClampFinite(social.ReproductionFear, -1, 1),
                        ClampFinite(social.ReproductionAffinity, -1, 1)),
                    new ReproductionConfig(
                        ClampFinite(reproduction.NeedThreshold, 0, 10),
                        ClampFinite(reproduction.Range, 0.1f, 20),
                        ClampFinite(reproduction.CooldownScale, 0.1f, 10),
                        ClampFinite(reproduction.PopulationPressureInfluence, 0, 1),
                        ClampFinite(reproduction.ParentHungerCost, 0, 10),
                        ClampFinite(reproduction.ParentThirstCost, 0, 10),
                        ClampFinite(reproduction.ParentFatigueCost, 0, 10)),
                    new RecoveryConfig(
                        Math.Clamp(recovery.FailedTargetCooldownTicks, 0, 1_000_000),
                        Math.Clamp(recovery.MaxRepeatedActionFailures, 1, 1_000),
                        Math.Clamp(recovery.MaxGoalAgeTicks, 1, 1_000_000),
                        Math.Clamp(recovery.IdleRecoveryTicks, 1, 1_000_000),
                        Math.Clamp(recovery.MovementStuckTicks, 1, 1_000))),
                SpeciesPopulation = new SpeciesPopulationConfig(
                    Math.Clamp(speciesPopulation.Human, 0, 10_000),
                    Math.Clamp(speciesPopulation.Deer, 0, 10_000),
                    Math.Clamp(speciesPopulation.Wolf, 0, 10_000)),
                EnabledSystems = enabledSystems.Count == 0
                    ? SimulationConfig.DefaultEnabledSystems
                    : enabledSystems
            };
        }

        private static float ClampFinite(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return min;
            }

            return Math.Clamp(value, min, max);
        }

        private static SpeciesRuleConfig NormalizeSpeciesRule(SpeciesRuleConfig rule, float fallbackMaxHealth)
        {
            StartingNeedsConfig startingNeeds = rule.StartingNeeds ?? new StartingNeedsConfig();

            return rule with
            {
                MaxHealth = ClampFinite(rule.MaxHealth <= 0 ? fallbackMaxHealth : rule.MaxHealth, 0.1f, 1_000),
                AdultAgeDays = Math.Clamp(rule.AdultAgeDays, 0, 1_000_000),
                MaxAgeDays = Math.Clamp(rule.MaxAgeDays, 1, 10_000_000),
                ReproductionCooldownTicks = Math.Clamp(rule.ReproductionCooldownTicks, 0, 1_000_000),
                PerceptionMultiplier = ClampFinite(rule.PerceptionMultiplier, 0, 100),
                MovementSpeedMultiplier = ClampFinite(rule.MovementSpeedMultiplier, 0, 100),
                AttackDamage = ClampFinite(rule.AttackDamage, 0, 1_000),
                HungerDecayMultiplier = ClampFinite(rule.HungerDecayMultiplier, 0, 100),
                ThirstDecayMultiplier = ClampFinite(rule.ThirstDecayMultiplier, 0, 100),
                FatigueDecayMultiplier = ClampFinite(rule.FatigueDecayMultiplier, 0, 100),
                StartingNeeds = new StartingNeedsConfig(
                    ClampFinite(startingNeeds.Hunger, 0, 1_000),
                    ClampFinite(startingNeeds.Thirst, 0, 1_000),
                    ClampFinite(startingNeeds.Energy, 0, 1_000),
                    ClampFinite(startingNeeds.Fatigue, 0, 1_000))
            };
        }

        private static float ClampImportance(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0;
            }

            return Math.Clamp(value, 0, 1);
        }

        private static void TrimHistory<T>(SortedDictionary<long, T> history, int limit)
        {
            while (history.Count > limit)
            {
                history.Remove(history.Keys.First());
            }
        }
    }
}
