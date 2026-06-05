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

    public sealed record TraitConfigDto(
        float InitialVariation,
        float MutationChancePerTrait,
        float MutationDelta);

    public sealed record EnvironmentConfigDto(
        int DayLengthTicks,
        int WeatherChangeIntervalTicks,
        float BaseTemperature);

    public sealed record EcologyConfigDto(
        int InitialPlantCount,
        int InitialWaterSourceCount,
        int InitialResourceDepositCount,
        int PlantRegrowthTicks,
        int PlantDecayTicksAfterDepleted,
        float WaterRefillPerRainTick,
        float WaterEvaporationPerHeatTick);

    public sealed record SpeciesPopulationConfigDto(
        int Human,
        int Deer,
        int Wolf);

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
        MemoryConfigDto? Memory = null,
        EnvironmentConfigDto? Environment = null,
        EcologyConfigDto? Ecology = null,
        SpeciesPopulationConfigDto? SpeciesPopulation = null,
        TraitConfigDto? Traits = null);

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
        IReadOnlyList<ResourceDefinitionDto> ResourceDefinitions,
        IReadOnlyList<ResourceContainerSnapshotDto> ResourceContainers,
        IReadOnlyList<MovementSnapshotDto> ActiveMovements,
        IReadOnlyList<GoalSnapshotDto> Goals,
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
        IReadOnlyList<SpeciesPopulationCountDto> SpeciesPopulation,
        SocialGraphSnapshotDto SocialGraph,
        EvolutionSummaryDto Evolution,
        IReadOnlyList<RecipeDefinitionSnapshotDto> RecipeCatalog,
        IReadOnlyList<GroupKnowledgeSnapshotDto> GroupKnowledge,
        IReadOnlyList<DiscoveryMarkerSnapshotDto> DiscoveryMarkers,
        SimulationTickDiagnosticsDto? Diagnostics,
        EnvironmentStateDto? Environment = null,
        EcologyStatsDto? EcologyStats = null,
        IReadOnlyList<PlantSnapshotDto>? Plants = null,
        IReadOnlyList<WaterSourceSnapshotDto>? WaterSources = null,
        IReadOnlyList<ResourceDepositSnapshotDto>? ResourceDeposits = null);

    public sealed record EnvironmentStateDto(
        int DayLengthTicks,
        int TickOfDay,
        int Day,
        string Phase,
        string Weather,
        float Temperature);

    public sealed record EcologyStatsDto(
        int PlantCount,
        int HarvestablePlantCount,
        int TotalPlantYield,
        int WaterSourceCount,
        float TotalWaterVolume,
        int ResourceDepositCount,
        int TotalDepositQuantity);

    public sealed record SpeciesPopulationCountDto(
        string SpeciesId,
        string DisplayName,
        int Total,
        int Alive,
        int Dead);

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
        float SpeedMultiplier,
        float PerceptionMultiplier = 1,
        float HungerDeltaMultiplier = 1,
        float ThirstDeltaMultiplier = 1,
        float FatigueDeltaMultiplier = 1,
        bool IsWater = false);

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
        string SpeciesId,
        int AgeTicks,
        long BornTick,
        IReadOnlyList<string> ParentIds,
        IReadOnlyList<string> OffspringIds,
        long? LastReproducedTick,
        long? DeathTick,
        string? DeathCause,
        InventoryDto Inventory,
        AgentNeedsDto Needs,
        AgentTraitsDto Traits,
        MovementSnapshotDto? Movement,
        GoalSnapshotDto? CurrentGoal,
        IReadOnlyList<TaskSnapshotDto> TaskQueue,
        AgentPerceptionSnapshotDto Perception,
        AgentMemorySnapshotDto Memory,
        AgentSocialSnapshotDto Social,
        AgentKnowledgeSnapshotDto Knowledge,
        AgentDecisionSnapshotDto? Decision,
        AgentLearningSnapshotDto Learning);

    public sealed record AgentNeedsDto(float Hunger, float Thirst, float Energy, float Fatigue = 0);

    public sealed record AgentTraitsDto(
        float Perception,
        float Speed,
        float Metabolism,
        float RiskTolerance,
        float LearningRate);

    public sealed record EvolutionSummaryDto(
        IReadOnlyList<PopulationTraitMetricDto> CurrentTraits,
        IReadOnlyList<TraitHistoryPointDto> TraitHistory);

    public sealed record PopulationTraitMetricDto(
        string Trait,
        float Average,
        float Minimum,
        float Maximum,
        float AliveAverage,
        float DeadAverage);

    public sealed record TraitHistoryPointDto(
        long Tick,
        string Trait,
        float Average,
        float Minimum,
        float Maximum,
        float AliveAverage,
        float DeadAverage);

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

    public sealed record AgentSocialSnapshotDto(
        IReadOnlyList<SocialRelationshipSnapshotDto> Relationships,
        IReadOnlyList<SocialGroupMembershipSnapshotDto> Groups);

    public sealed record AgentKnowledgeSnapshotDto(
        IReadOnlyList<KnowledgeRecordSnapshotDto> Records);

    public sealed record KnowledgeRecordSnapshotDto(
        string Id,
        string Kind,
        string SubjectId,
        string DisplayName,
        long DiscoveredTick,
        long LastUpdatedTick,
        string Source,
        string? SourceAgentId,
        IReadOnlyDictionary<string, string> Metadata);

    public sealed record SocialRelationshipSnapshotDto(
        string AgentId,
        string OtherAgentId,
        float Familiarity,
        float Trust,
        float Fear,
        float Affinity,
        int InteractionCount,
        long CreatedTick,
        long LastUpdatedTick,
        long? LastSeenTick,
        long? LastInteractionTick,
        IReadOnlyList<string> SharedGroupIds);

    public sealed record SocialGroupMembershipSnapshotDto(
        string GroupId,
        string DisplayName,
        string Kind,
        long JoinedTick);

    public sealed record SocialGraphSnapshotDto(
        IReadOnlyList<SocialGraphNodeDto> Nodes,
        IReadOnlyList<SocialGraphEdgeDto> Edges);

    public sealed record GroupKnowledgeSnapshotDto(
        string GroupId,
        string DisplayName,
        IReadOnlyList<string> MemberAgentIds,
        IReadOnlyList<string> KnownRecipeIds,
        IReadOnlyList<string> KnownActionUnlockIds);

    public sealed record DiscoveryMarkerSnapshotDto(
        long Tick,
        string AgentId,
        string RecipeId,
        string DisplayName,
        string Source);

    public sealed record RecipeDefinitionSnapshotDto(
        string Id,
        string DisplayName,
        string Description,
        IReadOnlyList<RecipeIngredientSnapshotDto> Inputs,
        IReadOnlyList<RecipeIngredientSnapshotDto> Tools,
        IReadOnlyList<RecipeOutputSnapshotDto> Outputs,
        IReadOnlyList<ActionUnlockSnapshotDto> Unlocks,
        float DiscoveryChance);

    public sealed record RecipeIngredientSnapshotDto(
        string ResourceId,
        int Quantity);

    public sealed record RecipeOutputSnapshotDto(
        string ResourceId,
        int Quantity);

    public sealed record ActionUnlockSnapshotDto(
        string Id,
        string DisplayName,
        string Description);

    public sealed record SocialGraphNodeDto(
        string AgentId,
        string SpeciesId,
        bool IsDead,
        PositionDto Position,
        IReadOnlyList<string> GroupIds);

    public sealed record SocialGraphEdgeDto(
        string FromAgentId,
        string ToAgentId,
        float Familiarity,
        float Trust,
        float Fear,
        float Affinity,
        int InteractionCount,
        long? LastInteractionTick,
        IReadOnlyList<string> SharedGroupIds);

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

    public sealed record ResourceDefinitionDto(
        string Id,
        string DisplayName,
        string IconKey,
        float UnitWeight,
        int MaxStackQuantity,
        bool IsConsumable,
        ResourceNeedEffectsDto NeedEffects,
        string Renewability = "Renewable",
        string Category = "general");

    public sealed record ResourceNeedEffectsDto(
        float HungerDelta,
        float ThirstDelta,
        float EnergyDelta,
        float FatigueDelta);

    public sealed record ResourceStackDto(
        string StackId,
        string ResourceId,
        int Quantity);

    public sealed record InventoryDto(
        int MaxSlots,
        float MaxWeight,
        int UsedSlots,
        float UsedWeight,
        IReadOnlyList<ResourceStackDto> Stacks);

    public sealed record ResourceContainerSnapshotDto(
        string Id,
        string EntityType,
        PositionDto Position,
        GridCellDto Cell,
        FootprintDto Footprint,
        IReadOnlyList<GridCellDto> OccupiedCells,
        bool OccupiesSpace,
        EntityHealthDto? Health,
        InventoryDto Inventory);

    public sealed record PlantSnapshotDto(
        string Id,
        string EntityType,
        PositionDto Position,
        GridCellDto Cell,
        FootprintDto Footprint,
        IReadOnlyList<GridCellDto> OccupiedCells,
        bool OccupiesSpace,
        EntityHealthDto? Health,
        string ResourceId,
        int Yield,
        int MaxYield,
        int RegrowthTicks,
        int TicksUntilRegrowth,
        int DecayTicksAfterDepleted,
        int DepletedTicks,
        bool IsDecayed);

    public sealed record WaterSourceSnapshotDto(
        string Id,
        string EntityType,
        PositionDto Position,
        GridCellDto Cell,
        FootprintDto Footprint,
        IReadOnlyList<GridCellDto> OccupiedCells,
        bool OccupiesSpace,
        float CurrentVolume,
        float MaxVolume,
        float RefillPerRainTick,
        float EvaporationPerHeatTick);

    public sealed record ResourceDepositSnapshotDto(
        string Id,
        string EntityType,
        PositionDto Position,
        GridCellDto Cell,
        FootprintDto Footprint,
        IReadOnlyList<GridCellDto> OccupiedCells,
        bool OccupiesSpace,
        string ResourceId,
        int Quantity,
        int MaxQuantity);

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
        string? OwnerAgentId,
        string? GoalId,
        int TaskCount,
        int PendingTaskCount,
        int AssignedTaskCount,
        int InProgressTaskCount,
        int CompletedTaskCount,
        int FailedTaskCount,
        int CancelledTaskCount,
        int InterruptedTaskCount,
        IReadOnlyList<TaskSnapshotDto> Tasks);

    public sealed record GoalSnapshotDto(
        string Id,
        string AgentId,
        string NeedKey,
        string Reason,
        int Priority,
        string? TargetId,
        string? TargetType,
        string? JobId,
        string Status,
        long CreatedTick,
        long LastUpdatedTick,
        string? FailureReason);

    public sealed record TaskSnapshotDto(
        string Id,
        string JobId,
        string StepId,
        string DefinitionId,
        string Name,
        string Status,
        string ExecutionKind,
        string? AssignedAgentId,
        int ProgressTicks,
        int RequiredProgressTicks,
        int DispatchCount,
        string? TargetId,
        string? TargetType,
        string? AtomicAction,
        string? SelectedGoal,
        string? ContextKey,
        string? FailureReason);

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
        IReadOnlyList<EditableResourceDefinitionDto> ResourceDefinitions,
        IReadOnlyList<EditableResourceContainerDto> ResourceContainers,
        SimulationConfigDto? Config = null,
        IReadOnlyList<EditablePlantDto>? Plants = null,
        IReadOnlyList<EditableWaterSourceDto>? WaterSources = null,
        IReadOnlyList<EditableResourceDepositDto>? ResourceDeposits = null);

    public sealed record SimulationDraftSchemaDto(
        IReadOnlyList<EditableFieldDto> StateFields,
        IReadOnlyList<EditableFieldDto> GridFields,
        IReadOnlyList<EditableFieldDto> AgentFields,
        IReadOnlyList<EditableFieldDto> ResourceDefinitionFields,
        IReadOnlyList<EditableFieldDto> ResourceContainerFields,
        IReadOnlyList<EditableFieldDto> PlantFields,
        IReadOnlyList<EditableFieldDto> WaterSourceFields,
        IReadOnlyList<EditableFieldDto> ResourceDepositFields,
        IReadOnlyList<EditableFieldDto> InventoryFields,
        IReadOnlyList<EditableFieldDto> ResourceStackFields);

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
        EditableInventoryDto Inventory,
        AgentNeedsDto Needs,
        string SpeciesId = "human",
        int AgeTicks = 0,
        long BornTick = 0,
        IReadOnlyList<string>? ParentIds = null,
        IReadOnlyList<string>? OffspringIds = null,
        long? LastReproducedTick = null,
        long? DeathTick = null,
        string? DeathCause = null,
        AgentTraitsDto? Traits = null);

    public sealed record EditableResourceDefinitionDto(
        string Id,
        string DisplayName,
        string IconKey,
        float UnitWeight,
        int MaxStackQuantity,
        bool IsConsumable,
        ResourceNeedEffectsDto NeedEffects,
        string Renewability = "Renewable",
        string Category = "general");

    public sealed record EditableInventoryDto(
        int MaxSlots,
        float MaxWeight,
        IReadOnlyList<EditableResourceStackDto> Stacks);

    public sealed record EditableResourceStackDto(
        string StackId,
        string ResourceId,
        int Quantity);

    public sealed record EditableResourceContainerDto(
        string Id,
        PositionDto Position,
        EditableInventoryDto Inventory);

    public sealed record EditablePlantDto(
        string Id,
        PositionDto Position,
        string ResourceId,
        int Yield,
        int MaxYield,
        int RegrowthTicks,
        int TicksUntilRegrowth,
        int DecayTicksAfterDepleted,
        int DepletedTicks,
        bool IsDecayed);

    public sealed record EditableWaterSourceDto(
        string Id,
        PositionDto Position,
        float CurrentVolume,
        float MaxVolume,
        float RefillPerRainTick,
        float EvaporationPerHeatTick);

    public sealed record EditableResourceDepositDto(
        string Id,
        PositionDto Position,
        string ResourceId,
        int Quantity,
        int MaxQuantity);
}
