namespace TabulaRasa.Abstractions.Spatial.Grid
{
    public sealed record OccupiedCell(
        GridCell Cell,
        string EntityId,
        string EntityType);
}
