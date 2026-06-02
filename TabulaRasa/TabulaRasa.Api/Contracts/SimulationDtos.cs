namespace TabulaRasa.Api.Contracts
{
    public sealed record CreateSimulationRequestDto(string? Name = null, SimulationConfigDto? Config = null);

    public sealed record CloneSimulationRequestDto(string? Name = null, long? SourceTick = null);

    public sealed record RunSimulationRequestDto(int? IntervalMilliseconds, SimulationConfigDto? Config = null);

    public sealed record ResetSimulationRequestDto(SimulationConfigDto? Config = null);

    public sealed record UpdateSimulationConfigRequestDto(SimulationConfigDto Config);

    public sealed record SimulationResourceLimitsDto(
        int MaxConcurrentRunningSimulations,
        int MaxTicksPerSecond,
        int MaxAgents,
        int MaxRetainedSnapshots);

    public sealed record NeedDecayConfigDto(
        float HungerDelta,
        float ThirstDelta,
        float EnergyDelta,
        float FatigueDelta = 1);

    public sealed record PathfindingConfigDto(
        bool AllowDiagonalMovement,
        int MaxVisitedCells,
        int MaxRepathAttempts = 3);

    public sealed record MemoryConfigDto(
        bool Enabled,
        int MaxMemoriesPerAgent,
        int RetentionTicks,
        float DecayPerTick,
        float MinimumStrength,
        float RecallThreshold);

    public sealed record SimulationConfigDto(
        int Seed,
        int WorldWidth,
        int WorldHeight,
        int TickIntervalMilliseconds,
        int InitialAgentCount,
        int InitialFoodCount,
        int EventHistoryLimit,
        int SnapshotHistoryLimit,
        NeedDecayConfigDto NeedDecay,
        float PerceptionRadius,
        float MovementSpeedPerTick,
        PathfindingConfigDto Pathfinding,
        IReadOnlyList<string> EnabledSystems,
        MemoryConfigDto? Memory = null);

    public sealed record SimulationSummaryDto(
        string SimulationId,
        string Name,
        string Status,
        long CurrentTick,
        int GridWidth,
        int GridHeight,
        int AgentCount,
        int AliveAgentCount,
        int DeadAgentCount,
        int FoodCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record SimulationTickSummaryDto(
        long Tick,
        double DurationMilliseconds,
        int EventCount);

    public sealed record SimulationStatusDto(
        long CurrentTick,
        string Status,
        long MinimumTick,
        long MaximumTick,
        int GridWidth,
        int GridHeight,
        int AgentCount,
        int AliveAgentCount,
        int DeadAgentCount,
        int FoodCount,
        SimulationConfigDto Config,
        SimulationTickSummaryDto? LatestTickSummary,
        long? EventHistoryMinimumTick,
        long? EventHistoryMaximumTick);

    public sealed record SimulationSnapshotDto(
        long Tick,
        GridDto Grid,
        IReadOnlyList<AgentSnapshotDto> Agents,
        IReadOnlyList<FoodSnapshotDto> Food,
        IReadOnlyList<MovementSnapshotDto> ActiveMovements,
        IReadOnlyList<JobSnapshotDto> Jobs,
        IReadOnlyList<ReservationSnapshotDto> Reservations,
        IReadOnlyList<ActionResultSnapshotDto> RecentActionResults,
        int PendingIntentCount,
        int PendingActionRequestCount,
        IReadOnlyList<SimulationEventDto> Events,
        IReadOnlyList<SimulationEventDto> RecentEvents,
        int PopulationCount,
        int AliveAgentCount,
        int DeadAgentCount,
        SimulationTickDiagnosticsDto? Diagnostics);

    public sealed record SimulationEventDto(
        long Tick,
        long Sequence,
        string Type,
        string SourceSystem,
        string Message,
        string? EntityId,
        IReadOnlyDictionary<string, string> Metadata);

    public sealed record SimulationTickDiagnosticsDto(
        long Tick,
        DateTimeOffset StartedAt,
        DateTimeOffset CompletedAt,
        double DurationMilliseconds,
        int EventCount,
        IReadOnlyList<SystemExecutionDiagnosticDto> Systems);

    public sealed record SystemExecutionDiagnosticDto(
        string Phase,
        string SystemName,
        int Priority,
        double DurationMilliseconds,
        int EmittedEventCount);

    public sealed record GridDto(
        int Width,
        int Height,
        IReadOnlyList<GridCellDto> BlockedCells,
        IReadOnlyList<GridTerrainCellDto> TerrainCells,
        IReadOnlyList<OccupiedCellDto> OccupiedCells);

    public sealed record GridCellDto(int X, int Y);

    public sealed record GridTerrainCellDto(
        GridCellDto Cell,
        string TerrainType,
        float TraversalCost,
        float SpeedMultiplier);

    public sealed record EditableGridTerrainCellDto(
        GridCellDto Cell,
        string TerrainType);

    public sealed record OccupiedCellDto(
        GridCellDto Cell,
        string EntityId,
        string EntityType);

    public sealed record PositionDto(float X, float Y);

    public sealed record FootprintDto(float Width, float Height);

    public sealed record EntityHealthDto(
        float Current,
        float Maximum,
        bool IsDepleted);

    public sealed record AgentSnapshotDto(
        string Id,
        string EntityType,
        PositionDto Position,
        GridCellDto Cell,
        FootprintDto Footprint,
        IReadOnlyList<GridCellDto> OccupiedCells,
        bool OccupiesSpace,
        EntityHealthDto? Health,
        bool IsDead,
        AgentNeedsDto Needs,
        MovementSnapshotDto? Movement,
        AgentPerceptionSnapshotDto Perception,
        AgentMemorySnapshotDto Memory,
        AgentDecisionSnapshotDto? Decision,
        AgentLearningSnapshotDto Learning);

    public sealed record AgentNeedsDto(float Hunger, float Thirst, float Energy, float Fatigue = 0);

    public sealed record AgentPerceptionSnapshotDto(
        IReadOnlyList<PerceivedEntitySnapshotDto> NearbyEntities,
        IReadOnlyList<InteractionOpportunitySnapshotDto> Opportunities);

    public sealed record PerceivedEntitySnapshotDto(
        string EntityId,
        string EntityType,
        PositionDto Position,
        bool IsInteractable,
        string Channel,
        float Distance,
        float Certainty,
        float Relevance);

    public sealed record InteractionOpportunitySnapshotDto(
        string ActionType,
        string? TargetId,
        PositionDto TargetPosition,
        string? SourceEntityId,
        string Channel,
        float Relevance);

    public sealed record AgentMemorySnapshotDto(
        IReadOnlyList<AgentMemoryRecordSnapshotDto> Memories);

    public sealed record AgentMemoryRecordSnapshotDto(
        string Id,
        string Kind,
        string SubjectId,
        string SubjectType,
        PositionDto Position,
        long CreatedTick,
        long LastUpdatedTick,
        float Strength,
        float Certainty,
        long? ExpiresAtTick,
        string Summary,
        IReadOnlyDictionary<string, string> Metadata);

    public sealed record AgentDecisionSnapshotDto(
        IReadOnlyDictionary<string, float> NeedPressures,
        IReadOnlyList<AgentActionScoreSnapshotDto> ActionScores,
        string SelectedGoal,
        string SelectedAction,
        string? TargetId,
        string ContextKey,
        bool Explored);

    public sealed record AgentActionScoreSnapshotDto(
        string ActionType,
        string? TargetId,
        string SelectedGoal,
        string ContextKey,
        string TargetType,
        string Channel,
        float NeedPressure,
        float OpportunityRelevance,
        float LearnedWeight,
        float Score);

    public sealed record AgentLearningSnapshotDto(
        IReadOnlyList<AgentLearningEntrySnapshotDto> Entries);

    public sealed record AgentLearningEntrySnapshotDto(
        string ContextKey,
        string ActionType,
        int Attempts,
        int Successes,
        int Failures,
        float LastOutcomeScore,
        float AverageOutcomeScore,
        float LearnedWeight);

    public sealed record FoodSnapshotDto(
        string Id,
        string EntityType,
        PositionDto Position,
        GridCellDto Cell,
        FootprintDto Footprint,
        IReadOnlyList<GridCellDto> OccupiedCells,
        bool OccupiesSpace,
        EntityHealthDto? Health,
        bool IsConsumed);

    public sealed record MovementSnapshotDto(
        string AgentId,
        string RequestedAction,
        string? TargetId,
        string Status,
        IReadOnlyList<PositionDto> Waypoints,
        PositionDto Destination,
        int CurrentWaypointIndex,
        float SpeedPerTick,
        float ArrivalTolerance,
        string? FailureReason,
        float RouteCost,
        int RepathCount,
        int MaxRepathAttempts,
        int StuckTicks,
        int MaxStuckTicks,
        string? LastRepathReason,
        float LastEffectiveSpeedPerTick);

    public sealed record JobSnapshotDto(
        string Id,
        string DefinitionId,
        string Name,
        string Status,
        int TaskCount,
        int PendingTaskCount,
        int AssignedTaskCount,
        int InProgressTaskCount,
        int CompletedTaskCount,
        int FailedTaskCount,
        int CancelledTaskCount);

    public sealed record ReservationSnapshotDto(
        string Id,
        string TargetType,
        string TargetId,
        string OwnerId,
        long ReservedAtTick,
        long? ExpiresAtTick);

    public sealed record ActionResultSnapshotDto(
        string AgentId,
        string ActionType,
        bool Succeeded,
        string? Reason,
        string? TargetId,
        string? ContextKey,
        float? OutcomeScore);

    public sealed record SimulationDraftDto(
        long Tick,
        EditableGridDto Grid,
        IReadOnlyList<EditableAgentDto> Agents,
        IReadOnlyList<EditableFoodDto> Food,
        SimulationConfigDto? Config = null);

    public sealed record SimulationDraftSchemaDto(
        IReadOnlyList<EditableFieldDto> StateFields,
        IReadOnlyList<EditableFieldDto> GridFields,
        IReadOnlyList<EditableFieldDto> AgentFields,
        IReadOnlyList<EditableFieldDto> FoodFields);

    public sealed record EditableFieldDto(
        string Path,
        string Label,
        string ValueType,
        bool IsEditable,
        string SourceType,
        string SourceProperty);

    public sealed record EditableGridDto(
        int Width,
        int Height,
        IReadOnlyList<GridCellDto> BlockedCells,
        IReadOnlyList<EditableGridTerrainCellDto> TerrainCells);

    public sealed record EditableAgentDto(
        string Id,
        PositionDto Position,
        AgentNeedsDto Needs);

    public sealed record EditableFoodDto(
        string Id,
        PositionDto Position,
        bool IsConsumed);
}
