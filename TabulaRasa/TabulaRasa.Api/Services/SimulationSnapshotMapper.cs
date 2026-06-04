using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Goals;
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
        public static SimulationSnapshotDto ToSnapshot(SimulationState state)
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
                    agent.DeathCause)).ToList(),
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
                config.Seed,
                config.WorldWidth,
                config.WorldHeight,
                config.TickIntervalMilliseconds,
                config.InitialAgentCount,
                config.InitialFoodCount,
                config.EventHistoryLimit,
                config.SnapshotHistoryLimit,
                new NeedDecayConfigDto(
                    config.EffectiveNeedDecay.HungerDelta,
                    config.EffectiveNeedDecay.ThirstDelta,
                    config.EffectiveNeedDecay.EnergyDelta,
                    config.EffectiveNeedDecay.FatigueDelta),
                config.PerceptionRadius,
                config.MovementSpeedPerTick,
                new PathfindingConfigDto(
                    config.EffectivePathfinding.AllowDiagonalMovement,
                    config.EffectivePathfinding.MaxVisitedCells,
                    config.EffectivePathfinding.MaxRepathAttempts),
                config.EffectiveEnabledSystems.ToList(),
                new MemoryConfigDto(
                    config.EffectiveMemory.Enabled,
                    config.EffectiveMemory.MaxMemoriesPerAgent,
                    config.EffectiveMemory.RetentionTicks,
                    config.EffectiveMemory.DecayPerTick,
                    config.EffectiveMemory.MinimumStrength,
                    config.EffectiveMemory.RecallThreshold),
                new EnvironmentConfigDto(
                    config.EffectiveEnvironment.DayLengthTicks,
                    config.EffectiveEnvironment.WeatherChangeIntervalTicks,
                    config.EffectiveEnvironment.BaseTemperature),
                new EcologyConfigDto(
                    config.EffectiveEcology.InitialPlantCount,
                    config.EffectiveEcology.InitialWaterSourceCount,
                    config.EffectiveEcology.InitialResourceDepositCount,
                    config.EffectiveEcology.PlantRegrowthTicks,
                    config.EffectiveEcology.PlantDecayTicksAfterDepleted,
                    config.EffectiveEcology.WaterRefillPerRainTick,
                    config.EffectiveEcology.WaterEvaporationPerHeatTick),
                new SpeciesPopulationConfigDto(
                    config.EffectiveSpeciesPopulation.Human,
                    config.EffectiveSpeciesPopulation.Deer,
                    config.EffectiveSpeciesPopulation.Wolf));
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
            EnvironmentConfig environment = dto.Environment is null
                ? fallback.EffectiveEnvironment
                : new EnvironmentConfig(
                    dto.Environment.DayLengthTicks,
                    dto.Environment.WeatherChangeIntervalTicks,
                    dto.Environment.BaseTemperature);
            EcologyConfig ecology = dto.Ecology is null
                ? new EcologyConfig(
                    InitialPlantCount: 0,
                    InitialWaterSourceCount: 0,
                    InitialResourceDepositCount: 0)
                : new EcologyConfig(
                    dto.Ecology.InitialPlantCount,
                    dto.Ecology.InitialWaterSourceCount,
                    dto.Ecology.InitialResourceDepositCount,
                    dto.Ecology.PlantRegrowthTicks,
                    dto.Ecology.PlantDecayTicksAfterDepleted,
                    dto.Ecology.WaterRefillPerRainTick,
                    dto.Ecology.WaterEvaporationPerHeatTick);
            SpeciesPopulationConfig speciesPopulation = dto.SpeciesPopulation is null
                ? new SpeciesPopulationConfig(dto.InitialAgentCount, 0, 0)
                : new SpeciesPopulationConfig(
                    dto.SpeciesPopulation.Human,
                    dto.SpeciesPopulation.Deer,
                    dto.SpeciesPopulation.Wolf);

            return new SimulationConfig(
                    dto.Seed,
                    dto.WorldWidth,
                    dto.WorldHeight,
                    dto.TickIntervalMilliseconds,
                    dto.InitialAgentCount,
                    dto.InitialFoodCount,
                    dto.EventHistoryLimit,
                    dto.SnapshotHistoryLimit,
                    new NeedDecayConfig(
                        dto.NeedDecay.HungerDelta,
                        dto.NeedDecay.ThirstDelta,
                        dto.NeedDecay.EnergyDelta,
                        dto.NeedDecay.FatigueDelta),
                    dto.PerceptionRadius,
                    dto.MovementSpeedPerTick,
                    new PathfindingConfig(
                        dto.Pathfinding.AllowDiagonalMovement,
                        dto.Pathfinding.MaxVisitedCells,
                        dto.Pathfinding.MaxRepathAttempts),
                    dto.EnabledSystems,
                    memory,
                    environment,
                    ecology,
                    speciesPopulation);
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
                    agent.DeathCause)).ToList(),
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
                movement is null ? null : ToMovement(movement),
                ToCurrentGoal(agent.Id, state),
                ToTaskQueue(agent.Id, state),
                ToPerception(state.LatestPerceptionsByAgentId.GetValueOrDefault(agent.Id)),
                ToMemory(state.MemoryStoresByAgentId.GetValueOrDefault(agent.Id)),
                ToSocial(state, agent.Id),
                ToDecision(agentState?.Learning.LatestDecision),
                ToLearning(agentState?.Learning));
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
                simulationEvent.Metadata);
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
