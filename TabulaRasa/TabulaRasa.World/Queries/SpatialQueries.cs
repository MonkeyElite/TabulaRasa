using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Footprints;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
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
                .Concat(world.ResourceContainers)
                .Concat(world.Plants)
                .Concat(world.WaterSources)
                .Concat(world.ResourceDeposits)
                .Where(entity => entity.Position.DistanceTo(origin) <= radius)
                .ToList();
        }

        public static bool OccupiesSpace(ISpatialEntity entity)
        {
            return entity switch
            {
                ResourceContainerEntity container => !container.IsEmpty,
                PlantEntity plant => !plant.IsDecayed,
                ResourceDepositEntity deposit => !deposit.IsEmpty,
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

        public static IReadOnlyList<ResourceContainerEntity> GetAvailableFoodContainersWithinRadius(
            WorldState world,
            WorldPosition origin,
            float radius)
        {
            return world.ResourceContainers
                .Where(container => ContainerHasFood(container) && container.Position.DistanceTo(origin) <= radius)
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

        public static ResourceContainerEntity? FindAvailableFoodContainerAtInteractionPoint(
            WorldState world,
            WorldPosition agentPosition,
            string containerId,
            float tolerance = DefaultInteractionTolerance)
        {
            ResourceContainerEntity? container = world.ResourceContainers.FirstOrDefault(candidate =>
                candidate.Id == containerId && ContainerHasFood(candidate));

            if (container is null)
            {
                return null;
            }

            InteractionPoint? interactionPoint = FindNearestAvailableInteractionPoint(
                container,
                agentPosition,
                tolerance);

            return interactionPoint is null ? null : container;
        }

        public static bool ContainerHasFood(ResourceContainerEntity container)
        {
            return container.Inventory.GetQuantity(ResourceDefinition.FoodId) > 0;
        }

        public static PlantEntity? FindAvailablePlantAtInteractionPoint(
            WorldState world,
            WorldPosition agentPosition,
            string plantId,
            float tolerance = DefaultInteractionTolerance)
        {
            PlantEntity? plant = world.Plants.FirstOrDefault(candidate =>
                candidate.Id == plantId && candidate.IsHarvestable);

            if (plant is null)
            {
                return null;
            }

            InteractionPoint? interactionPoint = FindNearestAvailableInteractionPoint(
                plant,
                agentPosition,
                tolerance);

            return interactionPoint is null ? null : plant;
        }

        public static WaterSourceEntity? FindAvailableWaterSourceAtInteractionPoint(
            WorldState world,
            WorldPosition agentPosition,
            string waterSourceId,
            float tolerance = DefaultInteractionTolerance)
        {
            WaterSourceEntity? waterSource = world.WaterSources.FirstOrDefault(candidate =>
                candidate.Id == waterSourceId && candidate.IsAvailable);

            if (waterSource is null)
            {
                return null;
            }

            InteractionPoint? interactionPoint = FindNearestAvailableInteractionPoint(
                waterSource,
                agentPosition,
                tolerance);

            return interactionPoint is null ? null : waterSource;
        }
    }
}
