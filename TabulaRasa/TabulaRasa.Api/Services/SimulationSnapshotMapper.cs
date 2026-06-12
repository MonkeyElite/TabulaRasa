using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Evolution;
using TabulaRasa.Simulation.Goals;
using TabulaRasa.Simulation.Knowledge;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Observability;
using TabulaRasa.Simulation.Social;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Environment;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Api.Services
{
    public static class SimulationSnapshotMapper
    {
        public static SimulationSnapshotDto ToSnapshot(
            SimulationState state,
            IReadOnlyList<SimulationSnapshotDto>? retainedSnapshots = null)
        {
            Dictionary<string, ActiveMovement> movementsByAgent = state.ActiveMovements
                .GroupBy(movement => movement.AgentId)
                .ToDictionary(group => group.Key, group => group.First());
            int populationCount = state.World.Agents.Count;
            int deadAgentCount = state.World.Agents.Count(agent => agent.IsDead);
            int aliveAgentCount = populationCount - deadAgentCount;

            return new SimulationSnapshotDto(
                state.Time.Tick,
                ToGrid(state),
                state.World.Agents.Select(agent => ToAgent(agent, state, movementsByAgent)).ToList(),
                state.World.ResourceDefinitions.Select(ToResourceDefinition).ToList(),
                state.World.ResourceContainers.Select(container => ToResourceContainer(container, state.World.ResourceDefinitionsById)).ToList(),
                state.ActiveMovements.Select(ToMovement).ToList(),
                state.Goals.Select(ToGoal).ToList(),
                state.ActiveJobs.Concat(state.PendingJobs).Select(ToJob).ToList(),
                state.Reservations.Reservations.Select(ToReservation).ToList(),
                state.ActionResults.TakeLast(10).Select(ToActionResult).ToList(),
                state.PendingIntents.Count,
                state.PendingActionRequests.Count,
                state.GetEventsForTick(state.Time.Tick).Select(ToEvent).ToList(),
                state.GetRecentEvents().Select(ToEvent).ToList(),
                populationCount,
                aliveAgentCount,
                deadAgentCount,
                ToSpeciesPopulation(state),
                ToSocialGraph(state),
                ToEvolutionSummary(state, retainedSnapshots ?? []),
                RecipeRegistry.All.Select(ToRecipeDefinition).ToList(),
                ToGroupKnowledge(state),
                ToDiscoveryMarkers(state),
                ToDiagnostics(state.GetDiagnosticsForTick(state.Time.Tick)),
                ToEnvironment(state.World.Environment),
                ToEcologyStats(state),
                state.World.Plants.Select(ToPlant).ToList(),
                state.World.WaterSources.Select(ToWaterSource).ToList(),
                state.World.ResourceDeposits.Select(ToResourceDeposit).ToList());
        }

        public static SimulationDraftDto ToDraft(SimulationState state)
        {
            return new SimulationDraftDto(
                state.Time.Tick,
                new EditableGridDto(
                    state.World.Grid.Width,
                    state.World.Grid.Height,
                    state.World.Grid.BlockedCells.Select(ToGridCell).ToList(),
                    state.World.Grid.TerrainCells.Select(ToEditableTerrainCell).ToList()),
                state.World.Agents.Select(agent => new EditableAgentDto(
                    agent.Id,
                    ToPosition(agent.Position),
                    ToEditableInventory(agent.Inventory),
                    ToNeeds(state.GetAgentById(agent.Id)?.NeedState),
                    SpeciesRegistry.NormalizeId(agent.SpeciesId),
                    agent.AgeTicks,
                    agent.BornTick,
                    agent.ParentIds.ToList(),
                    agent.OffspringIds.ToList(),
                    agent.LastReproducedTick,
                    agent.DeathTick,
                    agent.DeathCause,
                    ToTraits(agent.Traits))).ToList(),
                state.World.ResourceDefinitions.Select(ToEditableResourceDefinition).ToList(),
                state.World.ResourceContainers.Select(container => new EditableResourceContainerDto(
                    container.Id,
                    ToPosition(container.Position),
                    ToEditableInventory(container.Inventory))).ToList(),
                ToConfig(state.Config),
                state.World.Plants.Select(ToEditablePlant).ToList(),
                state.World.WaterSources.Select(ToEditableWaterSource).ToList(),
                state.World.ResourceDeposits.Select(ToEditableResourceDeposit).ToList());
        }

        public static SimulationConfigDto ToConfig(SimulationConfig config)
        {
            return new SimulationConfigDto(
                Seed: config.Seed,
                WorldWidth: config.WorldWidth,
                WorldHeight: config.WorldHeight,
                TickIntervalMilliseconds: config.TickIntervalMilliseconds,
                InitialAgentCount: config.InitialAgentCount,
                InitialFoodCount: config.InitialFoodCount,
                EventHistoryLimit: config.EventHistoryLimit,
                SnapshotHistoryLimit: config.SnapshotHistoryLimit,
                NeedDecay: ToNeedDecayConfig(config.EffectiveNeedDecay),
                NeedRules: ToNeedRulesConfig(config.EffectiveNeedRules),
                Goals: ToGoalConfig(config.EffectiveGoals),
                PerceptionRadius: config.PerceptionRadius,
                MovementSpeedPerTick: config.MovementSpeedPerTick,
                Pathfinding: ToPathfindingConfig(config.EffectivePathfinding),
                SpawnResources: ToSpawnResourceConfig(config.EffectiveSpawnResources),
                EnabledSystems: config.EffectiveEnabledSystems.ToList(),
                Memory: ToMemoryConfig(config.EffectiveMemory),
                Lifecycle: ToLifecycleConfig(config.EffectiveLifecycle),
                Environment: ToEnvironmentConfig(config.EffectiveEnvironment),
                Ecology: ToEcologyConfig(config.EffectiveEcology),
                SpeciesPopulation: new SpeciesPopulationConfigDto(
                    config.EffectiveSpeciesPopulation.Human,
                    config.EffectiveSpeciesPopulation.Deer,
                    config.EffectiveSpeciesPopulation.Wolf),
                SpeciesRules: ToSpeciesRulesConfig(config.EffectiveSpeciesRules),
                Traits: new TraitConfigDto(
                    config.EffectiveTraits.InitialVariation,
                    config.EffectiveTraits.MutationChancePerTrait,
                    config.EffectiveTraits.MutationDelta),
                Believability: ToBelievabilityConfig(config.EffectiveBelievability));
        }

        public static SimulationConfig ToConfig(SimulationConfigDto? dto, SimulationConfig fallback)
        {
            if (dto is null)
            {
                return fallback;
            }

            MemoryConfig memory = dto.Memory is null
                ? fallback.EffectiveMemory
                : new MemoryConfig(
                    dto.Memory.Enabled,
                    dto.Memory.MaxMemoriesPerAgent,
                    dto.Memory.RetentionTicks,
                    dto.Memory.DecayPerTick,
                    dto.Memory.MinimumStrength,
                    dto.Memory.RecallThreshold);
            LifecycleConfig lifecycle = dto.Lifecycle is null
                ? fallback.EffectiveLifecycle
                : new LifecycleConfig(dto.Lifecycle.AgeDaysPerTick, dto.Lifecycle.DaysPerYear);
            EnvironmentConfig environment = dto.Environment is null
                ? fallback.EffectiveEnvironment
                : ToEnvironmentConfig(dto.Environment);
            EcologyConfig ecology = dto.Ecology is null
                ? new EcologyConfig(
                    InitialPlantCount: 0,
                    InitialWaterSourceCount: 0,
                    InitialResourceDepositCount: 0)
                : ToEcologyConfig(dto.Ecology);
            SpeciesPopulationConfig speciesPopulation = dto.SpeciesPopulation is null
                ? new SpeciesPopulationConfig(dto.InitialAgentCount, 0, 0)
                : new SpeciesPopulationConfig(
                    dto.SpeciesPopulation.Human,
                    dto.SpeciesPopulation.Deer,
                    dto.SpeciesPopulation.Wolf);
            TraitConfig traits = dto.Traits is null
                ? fallback.EffectiveTraits
                : new TraitConfig(
                    dto.Traits.InitialVariation,
                    dto.Traits.MutationChancePerTrait,
                    dto.Traits.MutationDelta);
            BelievabilityConfig believability = ToBelievabilityConfig(dto.Believability, fallback.EffectiveBelievability);

            return new SimulationConfig(
                Seed: dto.Seed,
                WorldWidth: dto.WorldWidth,
                WorldHeight: dto.WorldHeight,
                TickIntervalMilliseconds: dto.TickIntervalMilliseconds,
                InitialAgentCount: dto.InitialAgentCount,
                InitialFoodCount: dto.InitialFoodCount,
                EventHistoryLimit: dto.EventHistoryLimit,
                SnapshotHistoryLimit: dto.SnapshotHistoryLimit,
                NeedDecay: ToNeedDecayConfig(dto.NeedDecay),
                NeedRules: dto.NeedRules is null ? fallback.EffectiveNeedRules : ToNeedRulesConfig(dto.NeedRules),
                Goals: dto.Goals is null ? fallback.EffectiveGoals : ToGoalConfig(dto.Goals),
                PerceptionRadius: dto.PerceptionRadius,
                MovementSpeedPerTick: dto.MovementSpeedPerTick,
                Pathfinding: dto.Pathfinding is null ? fallback.EffectivePathfinding : ToPathfindingConfig(dto.Pathfinding),
                SpawnResources: dto.SpawnResources is null ? fallback.EffectiveSpawnResources : ToSpawnResourceConfig(dto.SpawnResources),
                EnabledSystems: dto.EnabledSystems,
                Memory: memory,
                Lifecycle: lifecycle,
                Environment: environment,
                Ecology: ecology,
                SpeciesPopulation: speciesPopulation,
                SpeciesRules: dto.SpeciesRules is null ? fallback.EffectiveSpeciesRules : ToSpeciesRulesConfig(dto.SpeciesRules, fallback.EffectiveSpeciesRules),
                Traits: traits,
                Believability: believability);
        }

        private static NeedDecayConfigDto ToNeedDecayConfig(NeedDecayConfig config)
        {
            return new NeedDecayConfigDto(config.HungerDelta, config.ThirstDelta, config.EnergyDelta, config.FatigueDelta);
        }

        private static NeedDecayConfig ToNeedDecayConfig(NeedDecayConfigDto dto)
        {
            return new NeedDecayConfig(dto.HungerDelta, dto.ThirstDelta, dto.EnergyDelta, dto.FatigueDelta);
        }

        private static NeedRulesConfigDto ToNeedRulesConfig(NeedRulesConfig config)
        {
            return new NeedRulesConfigDto(
                config.MaximumNeedValue,
                config.MaximumEnergyValue,
                config.EatRecoveryAmount,
                config.DrinkRecoveryAmount,
                config.RestEnergyRecoveryAmount,
                config.RestFatigueRecoveryAmount,
                config.CriticalNeedThreshold,
                config.HarmNeedThreshold,
                config.ExhaustedEnergyThreshold,
                config.SurvivalDamagePerTick,
                config.HeatWeatherThirstMultiplier,
                config.HotTemperatureThreshold,
                config.HotTemperatureThirstBonus,
                config.ColdTemperatureThreshold,
                config.ColdTemperatureThirstBonus,
                config.MinTemperatureThirstMultiplier,
                config.MaxTemperatureThirstMultiplier);
        }

        private static NeedRulesConfig ToNeedRulesConfig(NeedRulesConfigDto dto)
        {
            return new NeedRulesConfig(
                dto.MaximumNeedValue,
                dto.MaximumEnergyValue,
                dto.EatRecoveryAmount,
                dto.DrinkRecoveryAmount,
                dto.RestEnergyRecoveryAmount,
                dto.RestFatigueRecoveryAmount,
                dto.CriticalNeedThreshold,
                dto.HarmNeedThreshold,
                dto.ExhaustedEnergyThreshold,
                dto.SurvivalDamagePerTick,
                dto.HeatWeatherThirstMultiplier,
                dto.HotTemperatureThreshold,
                dto.HotTemperatureThirstBonus,
                dto.ColdTemperatureThreshold,
                dto.ColdTemperatureThirstBonus,
                dto.MinTemperatureThirstMultiplier,
                dto.MaxTemperatureThirstMultiplier);
        }

        private static GoalConfigDto ToGoalConfig(GoalConfig config)
        {
            return new GoalConfigDto(
                config.HungerThreshold,
                config.UrgentHungerThreshold,
                config.InterruptionPriorityDelta,
                config.InventionMaxHunger,
                config.InventionMaxThirst,
                config.InventionMaxFatigue);
        }

        private static GoalConfig ToGoalConfig(GoalConfigDto dto)
        {
            return new GoalConfig(
                dto.HungerThreshold,
                dto.UrgentHungerThreshold,
                dto.InterruptionPriorityDelta,
                dto.InventionMaxHunger,
                dto.InventionMaxThirst,
                dto.InventionMaxFatigue);
        }

        private static PathfindingConfigDto ToPathfindingConfig(PathfindingConfig config)
        {
            return new PathfindingConfigDto(
                config.AllowDiagonalMovement,
                config.MaxVisitedCells,
                config.MaxRepathAttempts,
                config.ArrivalTolerance,
                config.InteractionTolerance,
                config.AgentInteractionRangeBonus);
        }

        private static PathfindingConfig ToPathfindingConfig(PathfindingConfigDto dto)
        {
            return new PathfindingConfig(
                dto.AllowDiagonalMovement,
                dto.MaxVisitedCells,
                dto.MaxRepathAttempts,
                dto.ArrivalTolerance,
                dto.InteractionTolerance,
                dto.AgentInteractionRangeBonus);
        }

        private static SpawnResourceConfigDto ToSpawnResourceConfig(SpawnResourceConfig config)
        {
            return new SpawnResourceConfigDto(
                config.FoodStackQuantity,
                config.PlantStartingYield,
                config.PlantMaxYield,
                config.WaterStartingVolume,
                config.WaterMaxVolume,
                config.DepositQuantity,
                config.DepositMaxQuantity);
        }

        private static SpawnResourceConfig ToSpawnResourceConfig(SpawnResourceConfigDto dto)
        {
            return new SpawnResourceConfig(
                dto.FoodStackQuantity,
                dto.PlantStartingYield,
                dto.PlantMaxYield,
                dto.WaterStartingVolume,
                dto.WaterMaxVolume,
                dto.DepositQuantity,
                dto.DepositMaxQuantity);
        }

        private static MemoryConfigDto ToMemoryConfig(MemoryConfig config)
        {
            return new MemoryConfigDto(
                config.Enabled,
                config.MaxMemoriesPerAgent,
                config.RetentionTicks,
                config.DecayPerTick,
                config.MinimumStrength,
                config.RecallThreshold);
        }

        private static LifecycleConfigDto ToLifecycleConfig(LifecycleConfig config)
        {
            return new LifecycleConfigDto(config.AgeDaysPerTick, config.DaysPerYear);
        }

        private static EnvironmentConfigDto ToEnvironmentConfig(EnvironmentConfig config)
        {
            return new EnvironmentConfigDto(
                config.DayLengthTicks,
                config.WeatherChangeIntervalTicks,
                config.BaseTemperature,
                config.DawnEndRatio,
                config.DayEndRatio,
                config.DuskEndRatio,
                config.ClearWeatherWeight,
                config.RainWeatherWeight,
                config.HeatWeatherWeight,
                config.ColdWeatherWeight,
                config.DayTemperatureDelta,
                config.NightTemperatureDelta,
                config.DawnTemperatureDelta,
                config.DuskTemperatureDelta,
                config.HeatTemperatureDelta,
                config.ColdTemperatureDelta,
                config.RainTemperatureDelta,
                config.MaxPlantCooling,
                config.PlantCoolingFactor,
                config.MaxWaterCooling,
                config.WaterCoolingPerSource);
        }

        private static EnvironmentConfig ToEnvironmentConfig(EnvironmentConfigDto dto)
        {
            return new EnvironmentConfig(
                dto.DayLengthTicks,
                dto.WeatherChangeIntervalTicks,
                dto.BaseTemperature,
                dto.DawnEndRatio,
                dto.DayEndRatio,
                dto.DuskEndRatio,
                dto.ClearWeatherWeight,
                dto.RainWeatherWeight,
                dto.HeatWeatherWeight,
                dto.ColdWeatherWeight,
                dto.DayTemperatureDelta,
                dto.NightTemperatureDelta,
                dto.DawnTemperatureDelta,
                dto.DuskTemperatureDelta,
                dto.HeatTemperatureDelta,
                dto.ColdTemperatureDelta,
                dto.RainTemperatureDelta,
                dto.MaxPlantCooling,
                dto.PlantCoolingFactor,
                dto.MaxWaterCooling,
                dto.WaterCoolingPerSource);
        }

        private static EcologyConfigDto ToEcologyConfig(EcologyConfig config)
        {
            return new EcologyConfigDto(
                config.InitialPlantCount,
                config.InitialWaterSourceCount,
                config.InitialResourceDepositCount,
                config.PlantRegrowthTicks,
                config.PlantDecayTicksAfterDepleted,
                config.WaterRefillPerRainTick,
                config.WaterEvaporationPerHeatTick,
                config.CollapsePlantYieldThreshold,
                config.CollapseWaterVolumeThreshold,
                config.RecoveryPlantYieldThreshold,
                config.RecoveryWaterVolumeThreshold);
        }

        private static EcologyConfig ToEcologyConfig(EcologyConfigDto dto)
        {
            return new EcologyConfig(
                dto.InitialPlantCount,
                dto.InitialWaterSourceCount,
                dto.InitialResourceDepositCount,
                dto.PlantRegrowthTicks,
                dto.PlantDecayTicksAfterDepleted,
                dto.WaterRefillPerRainTick,
                dto.WaterEvaporationPerHeatTick,
                dto.CollapsePlantYieldThreshold,
                dto.CollapseWaterVolumeThreshold,
                dto.RecoveryPlantYieldThreshold,
                dto.RecoveryWaterVolumeThreshold);
        }

        private static SpeciesRulesConfigDto ToSpeciesRulesConfig(SpeciesRulesConfig config)
        {
            return new SpeciesRulesConfigDto(
                ToSpeciesRuleConfig(config.EffectiveHuman),
                ToSpeciesRuleConfig(config.EffectiveDeer),
                ToSpeciesRuleConfig(config.EffectiveWolf));
        }

        private static SpeciesRuleConfigDto ToSpeciesRuleConfig(SpeciesRuleConfig config)
        {
            return new SpeciesRuleConfigDto(
                config.MaxHealth,
                config.AdultAgeDays,
                config.MaxAgeDays,
                config.ReproductionCooldownTicks,
                config.PerceptionMultiplier,
                config.MovementSpeedMultiplier,
                config.AttackDamage,
                config.HungerDecayMultiplier,
                config.ThirstDecayMultiplier,
                config.FatigueDecayMultiplier,
                config.StartingNeeds is null
                    ? null
                    : new StartingNeedsConfigDto(
                        config.StartingNeeds.Hunger,
                        config.StartingNeeds.Thirst,
                        config.StartingNeeds.Energy,
                        config.StartingNeeds.Fatigue),
                config.EdibleResourceIds,
                config.PreySpeciesIds);
        }

        private static SpeciesRulesConfig ToSpeciesRulesConfig(SpeciesRulesConfigDto dto, SpeciesRulesConfig fallback)
        {
            return new SpeciesRulesConfig(
                dto.Human is null ? fallback.EffectiveHuman : ToSpeciesRuleConfig(dto.Human),
                dto.Deer is null ? fallback.EffectiveDeer : ToSpeciesRuleConfig(dto.Deer),
                dto.Wolf is null ? fallback.EffectiveWolf : ToSpeciesRuleConfig(dto.Wolf));
        }

        private static SpeciesRuleConfig ToSpeciesRuleConfig(SpeciesRuleConfigDto dto)
        {
            return new SpeciesRuleConfig(
                dto.MaxHealth,
                dto.AdultAgeDays,
                dto.MaxAgeDays,
                dto.ReproductionCooldownTicks,
                dto.PerceptionMultiplier,
                dto.MovementSpeedMultiplier,
                dto.AttackDamage,
                dto.HungerDecayMultiplier,
                dto.ThirstDecayMultiplier,
                dto.FatigueDecayMultiplier,
                dto.StartingNeeds is null
                    ? null
                    : new StartingNeedsConfig(
                        dto.StartingNeeds.Hunger,
                        dto.StartingNeeds.Thirst,
                        dto.StartingNeeds.Energy,
                        dto.StartingNeeds.Fatigue),
                dto.EdibleResourceIds,
                dto.PreySpeciesIds);
        }

        private static BelievabilityConfigDto ToBelievabilityConfig(BelievabilityConfig config)
        {
            BehaviorWeightConfig behavior = config.EffectiveBehavior;
            SocialWeightConfig social = config.EffectiveSocial;
            ReproductionConfig reproduction = config.EffectiveReproduction;
            RecoveryConfig recovery = config.EffectiveRecovery;

            return new BelievabilityConfigDto(
                new BehaviorWeightConfigDto(
                    behavior.Eat,
                    behavior.Drink,
                    behavior.Rest,
                    behavior.Wander,
                    behavior.Social,
                    behavior.Reproduce,
                    behavior.Flee,
                    behavior.Attack,
                    behavior.Craft,
                    behavior.Experiment,
                    behavior.ExplorationChance,
                    behavior.PersonalityInfluence),
                new SocialWeightConfigDto(
                    social.PerceptionFamiliarity,
                    social.CommunicationFamiliarity,
                    social.CommunicationTrust,
                    social.CommunicationFear,
                    social.CommunicationAffinity,
                    social.AttackTrust,
                    social.AttackFear,
                    social.AttackAffinity,
                    social.ReproductionFamiliarity,
                    social.ReproductionTrust,
                    social.ReproductionFear,
                    social.ReproductionAffinity),
                new ReproductionConfigDto(
                    reproduction.NeedThreshold,
                    reproduction.Range,
                    reproduction.CooldownScale,
                    reproduction.PopulationPressureInfluence,
                    reproduction.ParentHungerCost,
                    reproduction.ParentThirstCost,
                    reproduction.ParentFatigueCost),
                new RecoveryConfigDto(
                    recovery.FailedTargetCooldownTicks,
                    recovery.MaxRepeatedActionFailures,
                    recovery.MaxGoalAgeTicks,
                    recovery.IdleRecoveryTicks,
                    recovery.MovementStuckTicks));
        }

        private static BelievabilityConfig ToBelievabilityConfig(BelievabilityConfigDto? dto, BelievabilityConfig fallback)
        {
            BehaviorWeightConfig fallbackBehavior = fallback.EffectiveBehavior;
            SocialWeightConfig fallbackSocial = fallback.EffectiveSocial;
            ReproductionConfig fallbackReproduction = fallback.EffectiveReproduction;
            RecoveryConfig fallbackRecovery = fallback.EffectiveRecovery;

            BehaviorWeightConfig behavior = dto?.Behavior is null
                ? fallbackBehavior
                : new BehaviorWeightConfig(
                    dto.Behavior.Eat,
                    dto.Behavior.Drink,
                    dto.Behavior.Rest,
                    dto.Behavior.Wander,
                    dto.Behavior.Social,
                    dto.Behavior.Reproduce,
                    dto.Behavior.Flee,
                    dto.Behavior.Attack,
                    dto.Behavior.Craft,
                    dto.Behavior.Experiment,
                    dto.Behavior.ExplorationChance,
                    dto.Behavior.PersonalityInfluence);
            SocialWeightConfig social = dto?.Social is null
                ? fallbackSocial
                : new SocialWeightConfig(
                    dto.Social.PerceptionFamiliarity,
                    dto.Social.CommunicationFamiliarity,
                    dto.Social.CommunicationTrust,
                    dto.Social.CommunicationFear,
                    dto.Social.CommunicationAffinity,
                    dto.Social.AttackTrust,
                    dto.Social.AttackFear,
                    dto.Social.AttackAffinity,
                    dto.Social.ReproductionFamiliarity,
                    dto.Social.ReproductionTrust,
                    dto.Social.ReproductionFear,
                    dto.Social.ReproductionAffinity);
            ReproductionConfig reproduction = dto?.Reproduction is null
                ? fallbackReproduction
                : new ReproductionConfig(
                    dto.Reproduction.NeedThreshold,
                    dto.Reproduction.Range,
                    dto.Reproduction.CooldownScale,
                    dto.Reproduction.PopulationPressureInfluence,
                    dto.Reproduction.ParentHungerCost,
                    dto.Reproduction.ParentThirstCost,
                    dto.Reproduction.ParentFatigueCost);
            RecoveryConfig recovery = dto?.Recovery is null
                ? fallbackRecovery
                : new RecoveryConfig(
                    dto.Recovery.FailedTargetCooldownTicks,
                    dto.Recovery.MaxRepeatedActionFailures,
                    dto.Recovery.MaxGoalAgeTicks,
                    dto.Recovery.IdleRecoveryTicks,
                    dto.Recovery.MovementStuckTicks);

            return new BelievabilityConfig(behavior, social, reproduction, recovery);
        }

        public static SimulationDraftDto ToDraft(SimulationSnapshotDto snapshot, SimulationConfigDto config)
        {
            return new SimulationDraftDto(
                snapshot.Tick,
                new EditableGridDto(
                    snapshot.Grid.Width,
                    snapshot.Grid.Height,
                    snapshot.Grid.BlockedCells,
                    snapshot.Grid.TerrainCells.Select(cell => new EditableGridTerrainCellDto(
                        cell.Cell,
                        cell.TerrainType)).ToList()),
                snapshot.Agents.Select(agent => new EditableAgentDto(
                    agent.Id,
                    agent.Position,
                    ToEditableInventory(agent.Inventory),
                    agent.Needs,
                    agent.SpeciesId,
                    agent.AgeTicks,
                    agent.BornTick,
                    agent.ParentIds,
                    agent.OffspringIds,
                    agent.LastReproducedTick,
                    agent.DeathTick,
                    agent.DeathCause,
                    agent.Traits)).ToList(),
                snapshot.ResourceDefinitions.Select(ToEditableResourceDefinition).ToList(),
                snapshot.ResourceContainers.Select(container => new EditableResourceContainerDto(
                    container.Id,
                    container.Position,
                    ToEditableInventory(container.Inventory))).ToList(),
                config,
                (snapshot.Plants ?? []).Select(ToEditablePlant).ToList(),
                (snapshot.WaterSources ?? []).Select(ToEditableWaterSource).ToList(),
                (snapshot.ResourceDeposits ?? []).Select(ToEditableResourceDeposit).ToList());
        }

        private static GridDto ToGrid(SimulationState state)
        {
            return new GridDto(
                state.World.Grid.Width,
                state.World.Grid.Height,
                state.World.Grid.BlockedCells.Select(ToGridCell).ToList(),
                state.World.Grid.TerrainCells.Select(ToTerrainCell).ToList(),
                SpatialQueries.GetOccupiedCells(state.World).Select(ToOccupiedCell).ToList());
        }

        private static AgentSnapshotDto ToAgent(
            AgentEntity agent,
            SimulationState state,
            IReadOnlyDictionary<string, ActiveMovement> movementsByAgent)
        {
            movementsByAgent.TryGetValue(agent.Id, out ActiveMovement? movement);
            AgentState? agentState = state.GetAgentById(agent.Id);

            return new AgentSnapshotDto(
                agent.Id,
                nameof(AgentEntity),
                ToPosition(agent.Position),
                ToGridCell(agent.Position.ToGridCell()),
                new FootprintDto(agent.Footprint.Width, agent.Footprint.Height),
                SpatialQueries.GetOccupiedCellsForEntity(agent).Select(ToGridCell).ToList(),
                SpatialQueries.OccupiesSpace(agent),
                ToHealth(agent),
                agent.IsDead,
                SpeciesRegistry.NormalizeId(agent.SpeciesId),
                agent.AgeTicks,
                agent.BornTick,
                agent.ParentIds.ToList(),
                agent.OffspringIds.ToList(),
                agent.LastReproducedTick,
                agent.DeathTick,
                agent.DeathCause,
                ToInventory(agent.Inventory, state.World.ResourceDefinitionsById),
                ToNeeds(agentState?.NeedState),
                ToTraits(agent.Traits),
                movement is null ? null : ToMovement(movement),
                ToCurrentGoal(agent.Id, state),
                ToTaskQueue(agent.Id, state),
                ToPerception(state.LatestPerceptionsByAgentId.GetValueOrDefault(agent.Id)),
                ToMemory(state.MemoryStoresByAgentId.GetValueOrDefault(agent.Id)),
                ToSocial(state, agent.Id),
                ToKnowledge(state.KnowledgeStoresByAgentId.GetValueOrDefault(agent.Id)),
                ToDecision(agentState?.Learning.LatestDecision),
                ToLearning(agentState?.Learning),
                ToPersonality(agent, state),
                agent.AgeTicks,
                agent.AgeTicks / Math.Max(1, state.Config.EffectiveLifecycle.DaysPerYear));
        }

        private static AgentPerceptionSnapshotDto ToPerception(AgentPerception? perception)
        {
            AgentPerception source = perception ?? AgentPerception.Empty;

            return new AgentPerceptionSnapshotDto(
                source.NearbyEntities.Select(ToPerceivedEntity).ToList(),
                source.Opportunities.Select(ToInteractionOpportunity).ToList());
        }

        private static PerceivedEntitySnapshotDto ToPerceivedEntity(PerceivedEntity entity)
        {
            return new PerceivedEntitySnapshotDto(
                entity.EntityId,
                entity.EntityType.ToString(),
                ToPosition(entity.Position),
                entity.IsInteractable,
                entity.Channel.ToString(),
                entity.Distance,
                entity.Certainty,
                entity.Relevance);
        }

        private static InteractionOpportunitySnapshotDto ToInteractionOpportunity(InteractionOpportunity opportunity)
        {
            return new InteractionOpportunitySnapshotDto(
                opportunity.ActionType.ToString(),
                opportunity.TargetId,
                ToPosition(opportunity.TargetPosition),
                opportunity.SourceEntityId,
                opportunity.Channel.ToString(),
                opportunity.Relevance);
        }

        private static AgentMemorySnapshotDto ToMemory(AgentMemoryStore? store)
        {
            return new AgentMemorySnapshotDto(
                (store?.Memories ?? [])
                    .OrderByDescending(memory => memory.Strength)
                    .ThenByDescending(memory => memory.LastUpdatedTick)
                    .Select(ToMemoryRecord)
                    .ToList());
        }

        private static AgentMemoryRecordSnapshotDto ToMemoryRecord(AgentMemoryRecord memory)
        {
            return new AgentMemoryRecordSnapshotDto(
                memory.Id,
                memory.Kind.ToString(),
                memory.SubjectId,
                memory.SubjectType,
                ToPosition(memory.Position),
                memory.CreatedTick,
                memory.LastUpdatedTick,
                memory.Strength,
                memory.Certainty,
                memory.ExpiresAtTick,
                memory.Summary,
                memory.Metadata);
        }

        private static AgentSocialSnapshotDto ToSocial(SimulationState state, string agentId)
        {
            AgentSocialStore? store = state.SocialStoresByAgentId.GetValueOrDefault(agentId);

            return new AgentSocialSnapshotDto(
                (store?.Relationships ?? [])
                    .Select(relationship => ToRelationship(state, relationship))
                    .ToList(),
                (store?.Groups ?? [])
                    .Select(ToGroupMembership)
                    .ToList());
        }

        private static AgentKnowledgeSnapshotDto ToKnowledge(AgentKnowledgeStore? store)
        {
            return new AgentKnowledgeSnapshotDto(
                (store?.Records ?? [])
                    .OrderBy(record => record.Kind)
                    .ThenBy(record => record.SubjectId, StringComparer.OrdinalIgnoreCase)
                    .Select(ToKnowledgeRecord)
                    .ToList());
        }

        private static KnowledgeRecordSnapshotDto ToKnowledgeRecord(KnowledgeRecord record)
        {
            return new KnowledgeRecordSnapshotDto(
                record.Id,
                record.Kind.ToString(),
                record.SubjectId,
                record.DisplayName,
                record.DiscoveredTick,
                record.LastUpdatedTick,
                record.Source,
                record.SourceAgentId,
                record.Metadata);
        }

        private static SocialRelationshipSnapshotDto ToRelationship(
            SimulationState state,
            SocialRelationship relationship)
        {
            return new SocialRelationshipSnapshotDto(
                relationship.AgentId,
                relationship.OtherAgentId,
                relationship.Familiarity,
                relationship.Trust,
                relationship.Fear,
                relationship.Affinity,
                relationship.InteractionCount,
                relationship.CreatedTick,
                relationship.LastUpdatedTick,
                relationship.LastSeenTick,
                relationship.LastInteractionTick,
                SharedGroups(state, relationship.AgentId, relationship.OtherAgentId));
        }

        private static SocialGroupMembershipSnapshotDto ToGroupMembership(SocialGroupMembership membership)
        {
            return new SocialGroupMembershipSnapshotDto(
                membership.GroupId,
                membership.DisplayName,
                membership.Kind,
                membership.JoinedTick);
        }

        private static SocialGraphSnapshotDto ToSocialGraph(SimulationState state)
        {
            IReadOnlyList<SocialGraphNodeDto> nodes = state.World.Agents
                .Select(agent =>
                {
                    AgentSocialStore? store = state.SocialStoresByAgentId.GetValueOrDefault(agent.Id);

                    return new SocialGraphNodeDto(
                        agent.Id,
                        SpeciesRegistry.NormalizeId(agent.SpeciesId),
                        agent.IsDead,
                        ToPosition(agent.Position),
                        (store?.Groups ?? [])
                            .Select(group => group.GroupId)
                            .ToList());
                })
                .ToList();

            IReadOnlyList<SocialGraphEdgeDto> edges = state.SocialStoresByAgentId.Values
                .SelectMany(store => store.Relationships)
                .Select(relationship => new SocialGraphEdgeDto(
                    relationship.AgentId,
                    relationship.OtherAgentId,
                    relationship.Familiarity,
                    relationship.Trust,
                    relationship.Fear,
                    relationship.Affinity,
                    relationship.InteractionCount,
                    relationship.LastInteractionTick,
                    SharedGroups(state, relationship.AgentId, relationship.OtherAgentId)))
                .ToList();

            return new SocialGraphSnapshotDto(nodes, edges);
        }

        private static EvolutionSummaryDto ToEvolutionSummary(
            SimulationState state,
            IReadOnlyList<SimulationSnapshotDto> retainedSnapshots)
        {
            IReadOnlyList<PopulationTraitMetricDto> currentTraits = ToPopulationTraitMetrics(
                state.World.Agents.Select(agent => new TraitMetricAgent(agent.Traits, agent.IsDead)).ToList());

            List<TraitHistoryPointDto> history = retainedSnapshots
                .SelectMany(snapshot => ToTraitHistoryPoints(snapshot.Tick, snapshot.Agents))
                .Concat(ToTraitHistoryPoints(
                    state.Time.Tick,
                    state.World.Agents.Select(agent => new TraitMetricAgent(agent.Traits, agent.IsDead)).ToList()))
                .GroupBy(point => $"{point.Tick}:{point.Trait}", StringComparer.Ordinal)
                .Select(group => group.Last())
                .OrderBy(point => point.Tick)
                .ThenBy(point => point.Trait, StringComparer.Ordinal)
                .ToList();

            return new EvolutionSummaryDto(currentTraits, history);
        }

        private static IReadOnlyList<TraitHistoryPointDto> ToTraitHistoryPoints(
            long tick,
            IReadOnlyList<AgentSnapshotDto> agents)
        {
            return ToPopulationTraitMetrics(
                    agents.Select(agent => new TraitMetricAgent(ToAgentTraits(agent.Traits), agent.IsDead)).ToList())
                .Select(metric => new TraitHistoryPointDto(
                    tick,
                    metric.Trait,
                    metric.Average,
                    metric.Minimum,
                    metric.Maximum,
                    metric.AliveAverage,
                    metric.DeadAverage))
                .ToList();
        }

        private static IReadOnlyList<TraitHistoryPointDto> ToTraitHistoryPoints(
            long tick,
            IReadOnlyList<TraitMetricAgent> agents)
        {
            return ToPopulationTraitMetrics(agents)
                .Select(metric => new TraitHistoryPointDto(
                    tick,
                    metric.Trait,
                    metric.Average,
                    metric.Minimum,
                    metric.Maximum,
                    metric.AliveAverage,
                    metric.DeadAverage))
                .ToList();
        }

        private static IReadOnlyList<PopulationTraitMetricDto> ToPopulationTraitMetrics(
            IReadOnlyList<TraitMetricAgent> agents)
        {
            return new[]
                {
                    "perception",
                    "speed",
                    "metabolism",
                    "riskTolerance",
                    "learningRate"
                }
                .Select(trait =>
                {
                    List<float> values = agents.Select(agent => GetTraitValue(agent.Traits, trait)).ToList();
                    List<float> aliveValues = agents.Where(agent => !agent.IsDead).Select(agent => GetTraitValue(agent.Traits, trait)).ToList();
                    List<float> deadValues = agents.Where(agent => agent.IsDead).Select(agent => GetTraitValue(agent.Traits, trait)).ToList();

                    return new PopulationTraitMetricDto(
                        trait,
                        AverageOrZero(values),
                        values.Count == 0 ? 0 : values.Min(),
                        values.Count == 0 ? 0 : values.Max(),
                        AverageOrZero(aliveValues),
                        AverageOrZero(deadValues));
                })
                .ToList();
        }

        private static float GetTraitValue(AgentTraits traits, string trait)
        {
            return trait switch
            {
                "perception" => traits.Perception,
                "speed" => traits.Speed,
                "metabolism" => traits.Metabolism,
                "riskTolerance" => traits.RiskTolerance,
                "learningRate" => traits.LearningRate,
                _ => 0
            };
        }

        private static float AverageOrZero(IReadOnlyList<float> values)
        {
            return values.Count == 0 ? 0 : values.Average();
        }

        private sealed record TraitMetricAgent(AgentTraits Traits, bool IsDead);

        private static IReadOnlyList<GroupKnowledgeSnapshotDto> ToGroupKnowledge(SimulationState state)
        {
            return state.SocialStoresByAgentId
                .SelectMany(pair => pair.Value.Groups.Select(group => new { AgentId = pair.Key, Group = group }))
                .GroupBy(item => item.Group.GroupId, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    IReadOnlyList<string> memberAgentIds = group
                        .Select(item => item.AgentId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(agentId => agentId, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    IReadOnlyList<KnowledgeRecord> records = memberAgentIds
                        .Select(agentId => state.KnowledgeStoresByAgentId.GetValueOrDefault(agentId))
                        .OfType<AgentKnowledgeStore>()
                        .SelectMany(store => store.Records)
                        .ToList();

                    return new GroupKnowledgeSnapshotDto(
                        group.Key,
                        group.First().Group.DisplayName,
                        memberAgentIds,
                        records
                            .Where(record => record.Kind == KnowledgeKind.Recipe)
                            .Select(record => record.SubjectId)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                            .ToList(),
                        records
                            .Where(record => record.Kind == KnowledgeKind.ActionUnlock)
                            .Select(record => record.SubjectId)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                            .ToList());
                })
                .OrderBy(group => group.GroupId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<DiscoveryMarkerSnapshotDto> ToDiscoveryMarkers(SimulationState state)
        {
            return state.GetRecentEvents()
                .Where(simulationEvent => simulationEvent.Type == "knowledge.discovered")
                .Select(simulationEvent => new DiscoveryMarkerSnapshotDto(
                    simulationEvent.Tick,
                    simulationEvent.Metadata.GetValueOrDefault("agentId", simulationEvent.EntityId ?? ""),
                    simulationEvent.Metadata.GetValueOrDefault("recipeId", ""),
                    simulationEvent.Metadata.GetValueOrDefault("displayName", "Discovery"),
                    simulationEvent.Metadata.GetValueOrDefault("source", "")))
                .ToList();
        }

        private static RecipeDefinitionSnapshotDto ToRecipeDefinition(RecipeDefinition recipe)
        {
            return new RecipeDefinitionSnapshotDto(
                recipe.Id,
                recipe.DisplayName,
                recipe.Description,
                recipe.Inputs.Select(input => new RecipeIngredientSnapshotDto(input.ResourceId, input.Quantity)).ToList(),
                recipe.Tools.Select(tool => new RecipeIngredientSnapshotDto(tool.ResourceId, tool.Quantity)).ToList(),
                recipe.Outputs.Select(output => new RecipeOutputSnapshotDto(output.ResourceId, output.Quantity)).ToList(),
                recipe.Unlocks.Select(unlock => new ActionUnlockSnapshotDto(unlock.Id, unlock.DisplayName, unlock.Description)).ToList(),
                recipe.DiscoveryChance);
        }

        private static IReadOnlyList<string> SharedGroups(
            SimulationState state,
            string firstAgentId,
            string secondAgentId)
        {
            if (!state.SocialStoresByAgentId.TryGetValue(firstAgentId, out AgentSocialStore? firstStore)
                || !state.SocialStoresByAgentId.TryGetValue(secondAgentId, out AgentSocialStore? secondStore))
            {
                return [];
            }

            HashSet<string> firstGroups = firstStore.Groups
                .Select(group => group.GroupId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return secondStore.Groups
                .Where(group => firstGroups.Contains(group.GroupId))
                .Select(group => group.GroupId)
                .OrderBy(groupId => groupId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static AgentDecisionSnapshotDto? ToDecision(AgentDecisionExplanation? decision)
        {
            if (decision is null)
            {
                return null;
            }

            return new AgentDecisionSnapshotDto(
                decision.NeedPressures,
                decision.ActionScores.Select(ToActionScore).ToList(),
                decision.SelectedGoal,
                decision.SelectedAction.ToString(),
                decision.TargetId,
                decision.ContextKey,
                decision.Explored);
        }

        private static AgentActionScoreSnapshotDto ToActionScore(AgentActionScore score)
        {
            return new AgentActionScoreSnapshotDto(
                score.ActionType.ToString(),
                score.TargetId,
                score.SelectedGoal,
                score.ContextKey,
                score.TargetType,
                score.Channel,
                score.NeedPressure,
                score.OpportunityRelevance,
                score.LearnedWeight,
                score.Score);
        }

        private static AgentLearningSnapshotDto ToLearning(AgentLearningProfile? learning)
        {
            return new AgentLearningSnapshotDto(
                (learning?.Entries ?? [])
                    .Select(ToLearningEntry)
                    .ToList());
        }

        private static AgentLearningEntrySnapshotDto ToLearningEntry(AgentLearningEntry entry)
        {
            return new AgentLearningEntrySnapshotDto(
                entry.ContextKey,
                entry.ActionType.ToString(),
                entry.Attempts,
                entry.Successes,
                entry.Failures,
                entry.LastOutcomeScore,
                entry.AverageOutcomeScore,
                entry.LearnedWeight);
        }

        private static ResourceDefinitionDto ToResourceDefinition(ResourceDefinition definition)
        {
            return new ResourceDefinitionDto(
                definition.Id,
                definition.DisplayName,
                definition.IconKey,
                definition.UnitWeight,
                definition.MaxStackQuantity,
                definition.IsConsumable,
                ToNeedEffects(definition.NeedEffects),
                definition.Renewability.ToString(),
                definition.Category);
        }

        private static EditableResourceDefinitionDto ToEditableResourceDefinition(ResourceDefinition definition)
        {
            return new EditableResourceDefinitionDto(
                definition.Id,
                definition.DisplayName,
                definition.IconKey,
                definition.UnitWeight,
                definition.MaxStackQuantity,
                definition.IsConsumable,
                ToNeedEffects(definition.NeedEffects),
                definition.Renewability.ToString(),
                definition.Category);
        }

        private static EditableResourceDefinitionDto ToEditableResourceDefinition(ResourceDefinitionDto definition)
        {
            return new EditableResourceDefinitionDto(
                definition.Id,
                definition.DisplayName,
                definition.IconKey,
                definition.UnitWeight,
                definition.MaxStackQuantity,
                definition.IsConsumable,
                definition.NeedEffects,
                definition.Renewability,
                definition.Category);
        }

        private static ResourceNeedEffectsDto ToNeedEffects(ResourceNeedEffects effects)
        {
            return new ResourceNeedEffectsDto(
                effects.HungerDelta,
                effects.ThirstDelta,
                effects.EnergyDelta,
                effects.FatigueDelta);
        }

        private static InventoryDto ToInventory(
            Inventory inventory,
            IReadOnlyDictionary<string, ResourceDefinition> definitions)
        {
            return new InventoryDto(
                inventory.MaxSlots,
                inventory.MaxWeight,
                inventory.UsedSlots,
                inventory.GetUsedWeight(definitions),
                inventory.Stacks.Select(ToResourceStack).ToList());
        }

        private static EditableInventoryDto ToEditableInventory(Inventory inventory)
        {
            return new EditableInventoryDto(
                inventory.MaxSlots,
                inventory.MaxWeight,
                inventory.Stacks.Select(stack => new EditableResourceStackDto(
                    stack.StackId,
                    stack.ResourceId,
                    stack.Quantity)).ToList());
        }

        private static EditableInventoryDto ToEditableInventory(InventoryDto inventory)
        {
            return new EditableInventoryDto(
                inventory.MaxSlots,
                inventory.MaxWeight,
                inventory.Stacks.Select(stack => new EditableResourceStackDto(
                    stack.StackId,
                    stack.ResourceId,
                    stack.Quantity)).ToList());
        }

        private static ResourceStackDto ToResourceStack(ResourceStack stack)
        {
            return new ResourceStackDto(stack.StackId, stack.ResourceId, stack.Quantity);
        }

        private static ResourceContainerSnapshotDto ToResourceContainer(
            ResourceContainerEntity container,
            IReadOnlyDictionary<string, ResourceDefinition> definitions)
        {
            return new ResourceContainerSnapshotDto(
                container.Id,
                nameof(ResourceContainerEntity),
                ToPosition(container.Position),
                ToGridCell(container.Position.ToGridCell()),
                new FootprintDto(container.Footprint.Width, container.Footprint.Height),
                SpatialQueries.GetOccupiedCellsForEntity(container).Select(ToGridCell).ToList(),
                SpatialQueries.OccupiesSpace(container),
                ToHealth(container),
                ToInventory(container.Inventory, definitions));
        }

        private static PlantSnapshotDto ToPlant(PlantEntity plant)
        {
            return new PlantSnapshotDto(
                plant.Id,
                nameof(PlantEntity),
                ToPosition(plant.Position),
                ToGridCell(plant.Position.ToGridCell()),
                new FootprintDto(plant.Footprint.Width, plant.Footprint.Height),
                SpatialQueries.GetOccupiedCellsForEntity(plant).Select(ToGridCell).ToList(),
                SpatialQueries.OccupiesSpace(plant),
                ToHealth(plant),
                plant.ResourceId,
                plant.Yield,
                plant.MaxYield,
                plant.RegrowthTicks,
                plant.TicksUntilRegrowth,
                plant.DecayTicksAfterDepleted,
                plant.DepletedTicks,
                plant.IsDecayed);
        }

        private static WaterSourceSnapshotDto ToWaterSource(WaterSourceEntity waterSource)
        {
            return new WaterSourceSnapshotDto(
                waterSource.Id,
                nameof(WaterSourceEntity),
                ToPosition(waterSource.Position),
                ToGridCell(waterSource.Position.ToGridCell()),
                new FootprintDto(waterSource.Footprint.Width, waterSource.Footprint.Height),
                SpatialQueries.GetOccupiedCellsForEntity(waterSource).Select(ToGridCell).ToList(),
                SpatialQueries.OccupiesSpace(waterSource),
                waterSource.CurrentVolume,
                waterSource.MaxVolume,
                waterSource.RefillPerRainTick,
                waterSource.EvaporationPerHeatTick);
        }

        private static ResourceDepositSnapshotDto ToResourceDeposit(ResourceDepositEntity deposit)
        {
            return new ResourceDepositSnapshotDto(
                deposit.Id,
                nameof(ResourceDepositEntity),
                ToPosition(deposit.Position),
                ToGridCell(deposit.Position.ToGridCell()),
                new FootprintDto(deposit.Footprint.Width, deposit.Footprint.Height),
                SpatialQueries.GetOccupiedCellsForEntity(deposit).Select(ToGridCell).ToList(),
                SpatialQueries.OccupiesSpace(deposit),
                deposit.ResourceId,
                deposit.Quantity,
                deposit.MaxQuantity);
        }

        private static EditablePlantDto ToEditablePlant(PlantEntity plant)
        {
            return new EditablePlantDto(
                plant.Id,
                ToPosition(plant.Position),
                plant.ResourceId,
                plant.Yield,
                plant.MaxYield,
                plant.RegrowthTicks,
                plant.TicksUntilRegrowth,
                plant.DecayTicksAfterDepleted,
                plant.DepletedTicks,
                plant.IsDecayed);
        }

        private static EditablePlantDto ToEditablePlant(PlantSnapshotDto plant)
        {
            return new EditablePlantDto(
                plant.Id,
                plant.Position,
                plant.ResourceId,
                plant.Yield,
                plant.MaxYield,
                plant.RegrowthTicks,
                plant.TicksUntilRegrowth,
                plant.DecayTicksAfterDepleted,
                plant.DepletedTicks,
                plant.IsDecayed);
        }

        private static EditableWaterSourceDto ToEditableWaterSource(WaterSourceEntity waterSource)
        {
            return new EditableWaterSourceDto(
                waterSource.Id,
                ToPosition(waterSource.Position),
                waterSource.CurrentVolume,
                waterSource.MaxVolume,
                waterSource.RefillPerRainTick,
                waterSource.EvaporationPerHeatTick);
        }

        private static EditableWaterSourceDto ToEditableWaterSource(WaterSourceSnapshotDto waterSource)
        {
            return new EditableWaterSourceDto(
                waterSource.Id,
                waterSource.Position,
                waterSource.CurrentVolume,
                waterSource.MaxVolume,
                waterSource.RefillPerRainTick,
                waterSource.EvaporationPerHeatTick);
        }

        private static EditableResourceDepositDto ToEditableResourceDeposit(ResourceDepositEntity deposit)
        {
            return new EditableResourceDepositDto(
                deposit.Id,
                ToPosition(deposit.Position),
                deposit.ResourceId,
                deposit.Quantity,
                deposit.MaxQuantity);
        }

        private static EditableResourceDepositDto ToEditableResourceDeposit(ResourceDepositSnapshotDto deposit)
        {
            return new EditableResourceDepositDto(
                deposit.Id,
                deposit.Position,
                deposit.ResourceId,
                deposit.Quantity,
                deposit.MaxQuantity);
        }

        private static EnvironmentStateDto ToEnvironment(EnvironmentState environment)
        {
            return new EnvironmentStateDto(
                environment.DayLengthTicks,
                environment.TickOfDay,
                environment.Day,
                environment.Phase.ToString(),
                environment.Weather.ToString(),
                environment.Temperature);
        }

        private static EcologyStatsDto ToEcologyStats(SimulationState state)
        {
            return new EcologyStatsDto(
                state.World.Plants.Count,
                state.World.Plants.Count(plant => plant.IsHarvestable),
                state.World.Plants.Sum(plant => plant.Yield),
                state.World.WaterSources.Count,
                state.World.WaterSources.Sum(water => water.CurrentVolume),
                state.World.ResourceDeposits.Count,
                state.World.ResourceDeposits.Sum(deposit => deposit.Quantity));
        }

        private static IReadOnlyList<SpeciesPopulationCountDto> ToSpeciesPopulation(SimulationState state)
        {
            return SpeciesRegistry.All
                .Select(species =>
                {
                    List<AgentEntity> agents = state.World.Agents
                        .Where(agent => string.Equals(SpeciesRegistry.NormalizeId(agent.SpeciesId), species.Id, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    return new SpeciesPopulationCountDto(
                        species.Id,
                        species.DisplayName,
                        agents.Count,
                        agents.Count(agent => !agent.IsDead),
                        agents.Count(agent => agent.IsDead));
                })
                .ToList();
        }

        private static MovementSnapshotDto ToMovement(ActiveMovement movement)
        {
            return new MovementSnapshotDto(
                movement.AgentId,
                movement.RequestedAction.ToString(),
                movement.TargetId,
                movement.Status.ToString(),
                movement.Route.Waypoints.Select(ToPosition).ToList(),
                ToPosition(movement.Route.Destination),
                movement.CurrentWaypointIndex,
                movement.SpeedPerTick,
                movement.ArrivalTolerance,
                movement.FailureReason,
                movement.RouteCost,
                movement.RepathCount,
                movement.MaxRepathAttempts,
                movement.StuckTicks,
                movement.MaxStuckTicks,
                movement.LastRepathReason,
                movement.LastEffectiveSpeedPerTick);
        }

        private static JobSnapshotDto ToJob(JobInstance job)
        {
            return new JobSnapshotDto(
                job.Id,
                job.Definition.Id,
                job.Definition.Name,
                job.Status.ToString(),
                job.OwnerAgentId,
                job.GoalId,
                job.Tasks.Count,
                job.Tasks.Count(task => task.Status == TaskStatus.Pending),
                job.Tasks.Count(task => task.Status == TaskStatus.Assigned),
                job.Tasks.Count(task => task.Status == TaskStatus.InProgress),
                job.Tasks.Count(task => task.Status == TaskStatus.Completed),
                job.Tasks.Count(task => task.Status == TaskStatus.Failed),
                job.Tasks.Count(task => task.Status == TaskStatus.Cancelled),
                job.Tasks.Count(task => task.Status == TaskStatus.Interrupted),
                job.Tasks.Select(ToTask).ToList());
        }

        private static GoalSnapshotDto? ToCurrentGoal(string agentId, SimulationState state)
        {
            AgentGoal? goal = state.Goals
                .LastOrDefault(goal => goal.AgentId == agentId && goal.IsActive);

            return goal is null ? null : ToGoal(goal);
        }

        private static IReadOnlyList<TaskSnapshotDto> ToTaskQueue(string agentId, SimulationState state)
        {
            HashSet<string> goalJobIds = state.Goals
                .Where(goal => goal.AgentId == agentId && goal.IsActive && goal.JobId is not null)
                .Select(goal => goal.JobId!)
                .ToHashSet(StringComparer.Ordinal);

            return state.ActiveJobs.Concat(state.PendingJobs)
                .Where(job => job.OwnerAgentId == agentId || goalJobIds.Contains(job.Id))
                .SelectMany(job => job.Tasks)
                .Select(ToTask)
                .ToList();
        }

        private static GoalSnapshotDto ToGoal(AgentGoal goal)
        {
            return new GoalSnapshotDto(
                goal.Id,
                goal.AgentId,
                goal.NeedKey,
                goal.Reason,
                goal.Priority,
                goal.TargetId,
                goal.TargetType,
                goal.JobId,
                goal.Status.ToString(),
                goal.CreatedTick,
                goal.LastUpdatedTick,
                goal.FailureReason);
        }

        private static TaskSnapshotDto ToTask(TaskInstance task)
        {
            return new TaskSnapshotDto(
                task.Id,
                task.JobId,
                task.StepId,
                task.Definition.Id,
                task.Definition.Name,
                task.Status.ToString(),
                task.Definition.ExecutionKind.ToString(),
                task.AssignedAgentId,
                task.ProgressTicks,
                task.Definition.RequiredProgressTicks,
                task.DispatchCount,
                task.Definition.TargetId,
                task.Definition.TargetType,
                task.Definition.AtomicAction?.ToString(),
                task.Definition.SelectedGoal,
                task.Definition.ContextKey,
                task.FailureReason);
        }

        private static ReservationSnapshotDto ToReservation(Reservation reservation)
        {
            return new ReservationSnapshotDto(
                reservation.Id,
                reservation.Target.Type.ToString(),
                reservation.Target.Id,
                reservation.OwnerId,
                reservation.CreatedAtTick,
                reservation.ExpiresAtTick);
        }

        private static ActionResultSnapshotDto ToActionResult(ActionResult result)
        {
            return new ActionResultSnapshotDto(
                result.AgentId,
                result.ActionType.ToString(),
                result.Succeeded,
                result.Reason,
                result.TargetId,
                result.ContextKey,
                result.OutcomeScore);
        }

        private static SimulationEventDto ToEvent(SimulationEvent simulationEvent)
        {
            return new SimulationEventDto(
                simulationEvent.Tick,
                simulationEvent.Sequence,
                simulationEvent.Type,
                simulationEvent.SourceSystem,
                simulationEvent.Message,
                simulationEvent.EntityId,
                simulationEvent.Metadata,
                simulationEvent.Severity,
                simulationEvent.Importance,
                simulationEvent.Tags ?? []);
        }

        private static SimulationTickDiagnosticsDto? ToDiagnostics(SimulationTickDiagnostics? diagnostics)
        {
            if (diagnostics is null)
            {
                return null;
            }

            return new SimulationTickDiagnosticsDto(
                diagnostics.Tick,
                diagnostics.StartedAt,
                diagnostics.CompletedAt,
                diagnostics.DurationMilliseconds,
                diagnostics.EventCount,
                diagnostics.Systems.Select(ToSystemDiagnostic).ToList());
        }

        private static SystemExecutionDiagnosticDto ToSystemDiagnostic(SystemExecutionDiagnostic diagnostic)
        {
            return new SystemExecutionDiagnosticDto(
                diagnostic.Phase.ToString(),
                diagnostic.SystemName,
                diagnostic.Priority,
                diagnostic.DurationMilliseconds,
                diagnostic.EmittedEventCount);
        }

        private static AgentNeedsDto ToNeeds(AgentNeedState? needs)
        {
            return new AgentNeedsDto(
                needs?.Hunger ?? 0,
                needs?.Thirst ?? 0,
                needs?.Energy ?? 0,
                needs?.Fatigue ?? 0);
        }

        private static AgentTraitsDto ToTraits(AgentTraits traits)
        {
            return new AgentTraitsDto(
                traits.Perception,
                traits.Speed,
                traits.Metabolism,
                traits.RiskTolerance,
                traits.LearningRate);
        }

        private static AgentPersonalityDto ToPersonality(AgentEntity agent, SimulationState state)
        {
            AgentPersonality personality = AgentPersonalityService.Derive(
                agent.Traits,
                state.Config.EffectiveBelievability.EffectiveBehavior.ExplorationChance,
                state.Config.EffectiveBelievability.EffectiveBehavior.PersonalityInfluence);

            return new AgentPersonalityDto(
                personality.Label,
                personality.DominantTrait,
                personality.BehaviorBiases,
                personality.ExplorationChance);
        }

        private static AgentTraits ToAgentTraits(AgentTraitsDto? traits)
        {
            return traits is null
                ? AgentTraits.Default
                : new AgentTraits(
                    traits.Perception,
                    traits.Speed,
                    traits.Metabolism,
                    traits.RiskTolerance,
                    traits.LearningRate);
        }

        private static PositionDto ToPosition(WorldPosition position)
        {
            return new PositionDto(position.X, position.Y);
        }

        private static GridCellDto ToGridCell(GridCell cell)
        {
            return new GridCellDto(cell.X, cell.Y);
        }

        private static GridTerrainCellDto ToTerrainCell(GridTerrainCell terrainCell)
        {
            GridTerrainProfile profile = GridTerrainProfile.For(terrainCell.TerrainType);

            return new GridTerrainCellDto(
                ToGridCell(terrainCell.Cell),
                terrainCell.TerrainType.ToString(),
                profile.TraversalCost,
                profile.SpeedMultiplier,
                profile.PerceptionMultiplier,
                profile.HungerDeltaMultiplier,
                profile.ThirstDeltaMultiplier,
                profile.FatigueDeltaMultiplier,
                profile.IsWater);
        }

        private static EditableGridTerrainCellDto ToEditableTerrainCell(GridTerrainCell terrainCell)
        {
            return new EditableGridTerrainCellDto(
                ToGridCell(terrainCell.Cell),
                terrainCell.TerrainType.ToString());
        }

        private static OccupiedCellDto ToOccupiedCell(OccupiedCell occupiedCell)
        {
            return new OccupiedCellDto(
                ToGridCell(occupiedCell.Cell),
                occupiedCell.EntityId,
                occupiedCell.EntityType);
        }

        private static EntityHealthDto? ToHealth(IDamageableEntity? entity)
        {
            return entity is null
                ? null
                : new EntityHealthDto(
                    entity.Health.Current,
                    entity.Health.Maximum,
                    entity.Health.IsDepleted);
        }
    }
}
