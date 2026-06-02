using TabulaRasa.Abstractions.Spatial.Grid;

namespace TabulaRasa.World.Spatial.Grid
{
    public sealed record GridTerrainCell(GridCell Cell, GridTerrainType TerrainType);
}
