using TabulaRasa.Abstractions.Spatial.Grid;

namespace TabulaRasa.World.Spatial.Navigation.Grid
{
    public sealed record PathRequest(
        GridCell Start,
        GridCell Destination,
        Func<GridCell, bool>? CanEnter = null);
}
