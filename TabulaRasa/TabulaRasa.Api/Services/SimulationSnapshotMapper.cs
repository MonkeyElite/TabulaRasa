using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Observability;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
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
                state.World.Foods.Select(ToFood).ToList(),
                state.ActiveMovements.Select(ToMovement).ToList(),
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
                ToDiagnostics(state.GetDiagnosticsForTick(state.Time.Tick)));
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
                    ToNeeds(state.GetAgentById(agent.Id)?.NeedState))).ToList(),
                state.World.Foods.Select(food => new EditableFoodDto(
                    food.Id,
                    ToPosition(food.Position),
                    food.IsConsumed)).ToList(),
                ToConfig(state.Config));
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
                    config.EffectiveMemory.RecallThreshold));
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
                    memory);
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
                    agent.Needs)).ToList(),
                snapshot.Food.Select(food => new EditableFoodDto(
                    food.Id,
                    food.Position,
                    food.IsConsumed)).ToList(),
                config);
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
                ToNeeds(agentState?.NeedState),
                movement is null ? null : ToMovement(movement),
                ToPerception(state.LatestPerceptionsByAgentId.GetValueOrDefault(agent.Id)),
                ToMemory(state.MemoryStoresByAgentId.GetValueOrDefault(agent.Id)),
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


        private static FoodSnapshotDto ToFood(FoodEntity food)
        {
            return new FoodSnapshotDto(
                food.Id,
                nameof(FoodEntity),
                ToPosition(food.Position),
                ToGridCell(food.Position.ToGridCell()),
                new FootprintDto(food.Footprint.Width, food.Footprint.Height),
                SpatialQueries.GetOccupiedCellsForEntity(food).Select(ToGridCell).ToList(),
                SpatialQueries.OccupiesSpace(food),
                ToHealth(food),
                food.IsConsumed);
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
                job.Tasks.Count,
                job.Tasks.Count(task => task.Status == TaskStatus.Pending),
                job.Tasks.Count(task => task.Status == TaskStatus.Assigned),
                job.Tasks.Count(task => task.Status == TaskStatus.InProgress),
                job.Tasks.Count(task => task.Status == TaskStatus.Completed),
                job.Tasks.Count(task => task.Status == TaskStatus.Failed),
                job.Tasks.Count(task => task.Status == TaskStatus.Cancelled));
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
                profile.SpeedMultiplier);
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
