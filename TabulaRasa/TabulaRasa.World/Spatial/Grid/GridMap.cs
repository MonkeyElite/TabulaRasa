using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;

namespace TabulaRasa.World.Spatial.Grid
{
    public sealed class GridMap
    {
        private readonly HashSet<GridCell> blockedCells = [];
        private readonly Dictionary<GridCell, GridTerrainType> terrainByCell = [];

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
        public IReadOnlyCollection<GridCell> BlockedCells => blockedCells;
        public IReadOnlyCollection<GridTerrainCell> TerrainCells => terrainByCell
            .Select(pair => new GridTerrainCell(pair.Key, pair.Value))
            .ToList();

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

        public GridTerrainType GetTerrain(GridCell cell)
        {
            if (!Contains(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), "Cell must be inside the grid bounds.");
            }

            return terrainByCell.GetValueOrDefault(cell, GridTerrainType.Plain);
        }

        public GridTerrainProfile GetTerrainProfile(GridCell cell)
        {
            return GridTerrainProfile.For(GetTerrain(cell));
        }

        public float GetTraversalCost(GridCell cell)
        {
            return GetTerrainProfile(cell).TraversalCost;
        }

        public float GetSpeedMultiplier(GridCell cell)
        {
            return GetTerrainProfile(cell).SpeedMultiplier;
        }

        public void SetTerrain(GridCell cell, GridTerrainType terrainType)
        {
            if (!Contains(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), "Cell must be inside the grid bounds.");
            }

            if (terrainType == GridTerrainType.Plain)
            {
                terrainByCell.Remove(cell);
                return;
            }

            terrainByCell[cell] = terrainType;
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

        public IReadOnlyList<GridCell> GetAdjacentCells(GridCell cell, bool includeDiagonals = false)
        {
            List<GridCell> candidates =
            [
                new(cell.X, cell.Y - 1),
                new(cell.X + 1, cell.Y),
                new(cell.X, cell.Y + 1),
                new(cell.X - 1, cell.Y)
            ];

            if (includeDiagonals)
            {
                candidates.AddRange(
                [
                    new(cell.X + 1, cell.Y - 1),
                    new(cell.X + 1, cell.Y + 1),
                    new(cell.X - 1, cell.Y + 1),
                    new(cell.X - 1, cell.Y - 1)
                ]);
            }

            return candidates.Where(Contains).ToList();
        }

        public IReadOnlyList<GridCell> GetTraversableAdjacentCells(GridCell cell, bool includeDiagonals = false)
        {
            return GetAdjacentCells(cell, includeDiagonals).Where(IsTraversable).ToList();
        }
    }
}
