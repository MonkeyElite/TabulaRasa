namespace TabulaRasa.Api.Contracts
{
    public sealed record RunSimulationRequestDto(int? IntervalMilliseconds);

    public sealed record SimulationStatusDto(
        long CurrentTick,
        string Status,
        long MinimumTick,
        long MaximumTick,
        int GridWidth,
        int GridHeight,
        int AgentCount,
        int FoodCount);

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
        int PendingActionRequestCount);

    public sealed record GridDto(
        int Width,
        int Height,
        IReadOnlyList<GridCellDto> BlockedCells);

    public sealed record GridCellDto(int X, int Y);

    public sealed record PositionDto(float X, float Y);

    public sealed record FootprintDto(float Width, float Height);

    public sealed record AgentSnapshotDto(
        string Id,
        string EntityType,
        PositionDto Position,
        GridCellDto Cell,
        FootprintDto Footprint,
        AgentNeedsDto Needs,
        MovementSnapshotDto? Movement);

    public sealed record AgentNeedsDto(float Hunger, float Thirst, float Energy);

    public sealed record FoodSnapshotDto(
        string Id,
        string EntityType,
        PositionDto Position,
        GridCellDto Cell,
        FootprintDto Footprint,
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
        string? FailureReason);

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
        string? Reason);

    public sealed record SimulationDraftDto(
        long Tick,
        EditableGridDto Grid,
        IReadOnlyList<EditableAgentDto> Agents,
        IReadOnlyList<EditableFoodDto> Food);

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
        IReadOnlyList<GridCellDto> BlockedCells);

    public sealed record EditableAgentDto(
        string Id,
        PositionDto Position,
        AgentNeedsDto Needs);

    public sealed record EditableFoodDto(
        string Id,
        PositionDto Position,
        bool IsConsumed);
}
