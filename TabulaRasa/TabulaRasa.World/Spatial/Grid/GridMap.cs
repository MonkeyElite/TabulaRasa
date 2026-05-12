using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.World.Spatial.Grid
{
    public sealed class GridMap
    {
        private readonly HashSet<GridCell> blockedCells = [];

        public GridMap(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            }

            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }

        public bool Contains(GridCell cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        }

        public GridCell GetCellAt(WorldPosition position)
        {
            return position.ToGridCell();
        }

        public bool IsTraversable(GridCell cell)
        {
            return Contains(cell) && !blockedCells.Contains(cell);
        }

        public void SetTraversable(GridCell cell, bool isTraversable)
        {
            if (!Contains(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), "Cell must be inside the grid bounds.");
            }

            if (isTraversable)
            {
                blockedCells.Remove(cell);
                return;
            }

            blockedCells.Add(cell);
        }

        public IReadOnlyList<GridCell> GetAdjacentCells(GridCell cell)
        {
            GridCell[] candidates =
            [
                new(cell.X, cell.Y - 1),
                new(cell.X + 1, cell.Y),
                new(cell.X, cell.Y + 1),
                new(cell.X - 1, cell.Y)
            ];

            return candidates.Where(Contains).ToList();
        }

        public IReadOnlyList<GridCell> GetTraversableAdjacentCells(GridCell cell)
        {
            return GetAdjacentCells(cell).Where(IsTraversable).ToList();
        }
    }
}
