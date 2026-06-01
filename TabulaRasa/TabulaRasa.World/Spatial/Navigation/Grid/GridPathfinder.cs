using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.World.Spatial.Grid;

namespace TabulaRasa.World.Spatial.Navigation.Grid
{
    public sealed class GridPathfinder
    {
        public PathResult FindPath(GridMap grid, PathRequest request)
        {
            if (!IsPassable(grid, request, request.Start))
            {
                return PathResult.Failure("Start cell is not traversable.");
            }

            if (!IsPassable(grid, request, request.Destination))
            {
                return PathResult.Failure("Destination cell is not traversable.");
            }

            if (request.Start == request.Destination)
            {
                return PathResult.Success(new GridPath([request.Start]));
            }

            Queue<GridCell> frontier = [];
            Dictionary<GridCell, GridCell?> cameFrom = [];

            frontier.Enqueue(request.Start);
            cameFrom[request.Start] = null;

            while (frontier.Count > 0)
            {
                GridCell current = frontier.Dequeue();

                if (current == request.Destination)
                {
                    return PathResult.Success(BuildPath(request.Start, request.Destination, cameFrom));
                }

                foreach (GridCell next in grid.GetAdjacentCells(current).Where(cell => IsPassable(grid, request, cell)))
                {
                    if (cameFrom.ContainsKey(next))
                    {
                        continue;
                    }

                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }

            return PathResult.Failure("Destination is unreachable.");
        }

        private static bool IsPassable(GridMap grid, PathRequest request, GridCell cell)
        {
            return grid.IsTraversable(cell) && (request.CanEnter?.Invoke(cell) ?? true);
        }

        private static GridPath BuildPath(
            GridCell start,
            GridCell destination,
            IReadOnlyDictionary<GridCell, GridCell?> cameFrom)
        {
            List<GridCell> cells = [];
            GridCell? current = destination;

            while (current is not null)
            {
                cells.Add(current.Value);
                current = cameFrom[current.Value];
            }

            cells.Reverse();

            if (cells[0] != start)
            {
                throw new InvalidOperationException("Path reconstruction did not reach the start cell.");
            }

            return new GridPath(cells);
        }
    }
}
