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

            PriorityQueue<GridCell, (float Priority, int Sequence)> frontier = new();
            Dictionary<GridCell, GridCell?> cameFrom = [];
            Dictionary<GridCell, float> costSoFar = [];
            HashSet<GridCell> visited = [];
            int sequence = 0;

            frontier.Enqueue(request.Start, (0, sequence++));
            cameFrom[request.Start] = null;
            costSoFar[request.Start] = 0;

            while (frontier.Count > 0)
            {
                GridCell current = frontier.Dequeue();

                if (!visited.Add(current))
                {
                    continue;
                }

                if (current == request.Destination)
                {
                    return PathResult.Success(BuildPath(
                        request.Start,
                        request.Destination,
                        cameFrom,
                        costSoFar[request.Destination]));
                }

                if (visited.Count >= request.MaxVisitedCells)
                {
                    return PathResult.Failure("Path search visited the configured maximum number of cells.");
                }

                foreach (GridCell next in grid
                    .GetAdjacentCells(current, request.AllowDiagonalMovement)
                    .Where(cell => IsPassable(grid, request, cell)))
                {
                    float newCost = costSoFar[current] + GetStepCost(grid, current, next);

                    if (costSoFar.TryGetValue(next, out float existingCost) && newCost >= existingCost)
                    {
                        continue;
                    }

                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    float priority = newCost + Heuristic(
                        next,
                        request.Destination,
                        request.AllowDiagonalMovement);
                    frontier.Enqueue(next, (priority, sequence++));
                }
            }

            return PathResult.Failure("Destination is unreachable.");
        }

        private static bool IsPassable(GridMap grid, PathRequest request, GridCell cell)
        {
            return grid.IsTraversable(cell) && (request.CanEnter?.Invoke(cell) ?? true);
        }

        private static float GetStepCost(GridMap grid, GridCell current, GridCell next)
        {
            bool isDiagonal = current.X != next.X && current.Y != next.Y;
            float distanceMultiplier = isDiagonal ? MathF.Sqrt(2f) : 1f;

            return grid.GetTraversalCost(next) * distanceMultiplier;
        }

        private static float Heuristic(GridCell current, GridCell destination, bool allowDiagonalMovement)
        {
            int dx = Math.Abs(current.X - destination.X);
            int dy = Math.Abs(current.Y - destination.Y);

            if (!allowDiagonalMovement)
            {
                return (dx + dy) * GridTerrainProfile.MinimumTraversalCost;
            }

            int diagonalSteps = Math.Min(dx, dy);
            int straightSteps = Math.Max(dx, dy) - diagonalSteps;

            return ((diagonalSteps * MathF.Sqrt(2f)) + straightSteps)
                * GridTerrainProfile.MinimumTraversalCost;
        }

        private static GridPath BuildPath(
            GridCell start,
            GridCell destination,
            IReadOnlyDictionary<GridCell, GridCell?> cameFrom,
            float totalCost)
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

            return new GridPath(cells, totalCost);
        }
    }
}
