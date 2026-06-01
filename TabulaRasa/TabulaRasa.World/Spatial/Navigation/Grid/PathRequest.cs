using TabulaRasa.Abstractions.Spatial.Grid;

namespace TabulaRasa.World.Spatial.Navigation.Grid
{
    public sealed record PathRequest(
        GridCell Start,
        GridCell Destination,
        Func<GridCell, bool>? CanEnter = null,
        bool AllowDiagonalMovement = false,
        int MaxVisitedCells = 1_000);
}
