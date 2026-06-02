using TabulaRasa.Abstractions.Spatial.Grid;

namespace TabulaRasa.World.Spatial.Navigation.Grid
{
    public sealed record GridPath
    {
        public GridPath(IReadOnlyList<GridCell> cells, float totalCost = 0)
        {
            if (cells.Count == 0)
            {
                throw new ArgumentException("Path must contain at least one cell.", nameof(cells));
            }

            Cells = cells;
            TotalCost = totalCost;
        }

        public IReadOnlyList<GridCell> Cells { get; }
        public float TotalCost { get; }
        public GridCell Start => Cells[0];
        public GridCell Destination => Cells[^1];
    }
}
