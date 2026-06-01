using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.World.Queries
{
    public static class SpatialQueries
    {
        public const float DefaultInteractionTolerance = 0.1f;

        public static GridCell GetCurrentCell(WorldState world, WorldPosition position)
        {
            return world.Grid.GetCellAt(position);
        }

        public static IReadOnlyList<GridCell> GetNearbyCells(WorldState world, GridCell origin, int radius)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be zero or greater.");
            }

            List<GridCell> cells = [];

            for (int y = origin.Y - radius; y <= origin.Y + radius; y++)
            {
                for (int x = origin.X - radius; x <= origin.X + radius; x++)
                {
                    GridCell cell = new(x, y);

                    if (world.Grid.Contains(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }

            return cells;
        }

        public static IReadOnlyList<ISpatialEntity> GetEntitiesWithinRadius(
            WorldState world,
            WorldPosition origin,
            float radius)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be zero or greater.");
            }

            return world.Agents
                .Cast<ISpatialEntity>()
                .Concat(world.Foods)
                .Where(entity => entity.Position.DistanceTo(origin) <= radius)
                .ToList();
        }

        public static bool OccupiesSpace(ISpatialEntity entity)
        {
            return entity switch
            {
                FoodEntity food => !food.IsConsumed,
                _ => true
            };
        }

        public static IReadOnlyList<GridCell> GetOccupiedCellsForFootprint(
            WorldPosition position,
            EntityFootprint footprint)
        {
            GridCell origin = position.ToGridCell();
            int width = Math.Max(1, (int)MathF.Ceiling(footprint.Width));
            int height = Math.Max(1, (int)MathF.Ceiling(footprint.Height));
            List<GridCell> cells = [];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells.Add(new GridCell(origin.X + x, origin.Y + y));
                }
            }

            return cells;
        }

        public static IReadOnlyList<GridCell> GetOccupiedCellsForEntity(ISpatialEntity entity)
        {
            if (!OccupiesSpace(entity))
            {
                return [];
            }

            return GetOccupiedCellsForFootprint(entity.Position, entity.Footprint);
        }

        public static IReadOnlyList<OccupiedCell> GetOccupiedCells(
            WorldState world,
            string? ignoredEntityId = null)
        {
            return world.SpatialEntities
                .Where(entity => ignoredEntityId is null || entity.Id != ignoredEntityId)
                .SelectMany(entity => GetOccupiedCellsForEntity(entity)
                    .Select(cell => new OccupiedCell(cell, entity.Id, entity.GetType().Name)))
                .ToList();
        }

        public static bool IsCellOccupied(
            WorldState world,
            GridCell cell,
            string? ignoredEntityId = null)
        {
            return GetOccupiedCells(world, ignoredEntityId)
                .Any(occupied => occupied.Cell == cell);
        }

        public static bool IsAnyCellOccupied(
            WorldState world,
            IEnumerable<GridCell> cells,
            string? ignoredEntityId = null)
        {
            HashSet<GridCell> targetCells = cells.ToHashSet();

            return GetOccupiedCells(world, ignoredEntityId)
                .Any(occupied => targetCells.Contains(occupied.Cell));
        }

        public static IReadOnlyList<FoodEntity> GetAvailableFoodsWithinRadius(
            WorldState world,
            WorldPosition origin,
            float radius)
        {
            return world.Foods
                .Where(food => !food.IsConsumed && food.Position.DistanceTo(origin) <= radius)
                .ToList();
        }

        public static IReadOnlyList<InteractionPoint> GetInteractionPoints(IInteractableEntity entity)
        {
            return entity.InteractionPoints;
        }

        public static InteractionPoint? FindNearestAvailableInteractionPoint(
            IInteractableEntity entity,
            WorldPosition origin,
            float maxDistance)
        {
            return entity.InteractionPoints
                .Where(point => !point.IsReserved)
                .Where(point => point.StandPosition.DistanceTo(origin) <= maxDistance)
                .OrderBy(point => point.StandPosition.DistanceTo(origin))
                .Cast<InteractionPoint?>()
                .FirstOrDefault();
        }

        public static FoodEntity? FindAvailableFoodAtInteractionPoint(
            WorldState world,
            WorldPosition agentPosition,
            string foodId,
            float tolerance = DefaultInteractionTolerance)
        {
            FoodEntity? food = world.Foods.FirstOrDefault(f => f.Id == foodId && !f.IsConsumed);

            if (food is null)
            {
                return null;
            }

            InteractionPoint? interactionPoint = FindNearestAvailableInteractionPoint(
                food,
                agentPosition,
                tolerance);

            return interactionPoint is null ? null : food;
        }

    }
}
