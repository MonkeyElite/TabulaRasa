using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.State;

namespace TabulaRasa.World.Mutation
{
    public sealed class WorldMutationService
    {
        public WorldMutationResult TryMoveEntity(
            WorldState world,
            string entityId,
            WorldPosition destination,
            WorldMutationOptions? options = null)
        {
            ISpatialEntity? entity = EntityQueries.GetSpatialEntity(world, entityId);

            if (entity is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.EntityNotFound,
                    "Entity does not exist.");
            }

            WorldMutationResult placement = ValidatePlacement(world, entity, destination, entityId, options);

            if (!placement.Succeeded)
            {
                return placement;
            }

            entity.Position = destination;
            return WorldMutationResult.Success();
        }

        public WorldMutationResult TryConsumeFood(WorldState world, string foodId)
        {
            FoodEntity? food = EntityQueries.GetFoodEntity(world, foodId);

            if (food is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.EntityNotFound,
                    "Food does not exist.");
            }

            if (food.IsConsumed)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidOperation,
                    "Food is already consumed.");
            }

            food.IsConsumed = true;
            return WorldMutationResult.Success();
        }

        public WorldMutationResult TrySpawnEntity(
            WorldState world,
            ISpatialEntity entity,
            WorldMutationOptions? options = null)
        {
            if (EntityQueries.GetSpatialEntity(world, entity.Id) is not null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.DuplicateEntityId,
                    "Entity id already exists.");
            }

            WorldMutationResult placement = ValidatePlacement(world, entity, entity.Position, null, options);

            if (!placement.Succeeded)
            {
                return placement;
            }

            switch (entity)
            {
                case AgentEntity agent:
                    world.Agents.Add(agent);
                    return WorldMutationResult.Success();
                case FoodEntity food:
                    world.Foods.Add(food);
                    return WorldMutationResult.Success();
                default:
                    return WorldMutationResult.Failure(
                        WorldMutationFailureKind.UnsupportedEntityType,
                        "Entity type is not supported by this world state.");
            }
        }

        public WorldMutationResult TryDeleteEntity(WorldState world, string entityId)
        {
            AgentEntity? agent = EntityQueries.GetAgentEntity(world, entityId);

            if (agent is not null)
            {
                world.Agents.Remove(agent);
                return WorldMutationResult.Success();
            }

            FoodEntity? food = EntityQueries.GetFoodEntity(world, entityId);

            if (food is not null)
            {
                world.Foods.Remove(food);
                return WorldMutationResult.Success();
            }

            return WorldMutationResult.Failure(
                WorldMutationFailureKind.EntityNotFound,
                "Entity does not exist.");
        }

        public WorldMutationResult TryDamageEntity(WorldState world, string entityId, float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Damage amount must be finite and greater than zero.");
            }

            IDamageableEntity? entity = EntityQueries.GetDamageableEntity(world, entityId);

            if (entity is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.EntityNotFound,
                    "Damageable entity does not exist.");
            }

            entity.Health.Current = Math.Max(0, entity.Health.Current - amount);
            return WorldMutationResult.Success();
        }

        private static WorldMutationResult ValidatePlacement(
            WorldState world,
            ISpatialEntity entity,
            WorldPosition position,
            string? ignoredEntityId,
            WorldMutationOptions? options)
        {
            if (float.IsNaN(position.X)
                || float.IsNaN(position.Y)
                || float.IsInfinity(position.X)
                || float.IsInfinity(position.Y))
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.OutOfBounds,
                    "Position must be finite.");
            }

            WorldMutationOptions resolvedOptions = options ?? new WorldMutationOptions();
            IReadOnlyList<GridCell> footprintCells = SpatialQueries.GetOccupiedCellsForFootprint(
                position,
                entity.Footprint);

            if (footprintCells.Any(cell => !world.Grid.Contains(cell)))
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.OutOfBounds,
                    "Entity footprint must remain inside the grid.");
            }

            if (!resolvedOptions.AllowBlockedCells
                && footprintCells.Any(cell => !world.Grid.IsTraversable(cell)))
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.BlockedCell,
                    "Entity footprint overlaps a blocked cell.");
            }

            if (!resolvedOptions.AllowOccupiedCells
                && SpatialQueries.IsAnyCellOccupied(world, footprintCells, ignoredEntityId))
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.OccupiedCell,
                    "Entity footprint overlaps an occupied cell.");
            }

            return WorldMutationResult.Success();
        }
    }
}
