using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;
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

        public WorldMutationResult TryTransferResource(
            WorldState world,
            Inventory source,
            Inventory destination,
            string resourceId,
            int quantity)
        {
            if (!TryGetDefinition(world, resourceId, out ResourceDefinition? definition, out WorldMutationResult failure))
            {
                return failure;
            }

            WorldMutationResult transferValidation = ValidateTransfer(
                source,
                destination,
                world.ResourceDefinitionsById,
                definition,
                quantity);
            if (!transferValidation.Succeeded)
            {
                return transferValidation;
            }

            RemoveResource(source, resourceId, quantity);
            AddResource(destination, definition, quantity);
            RemoveEmptyStacks(source);
            RemoveEmptyStacks(destination);
            RemoveEmptyResourceContainers(world);

            return WorldMutationResult.Success();
        }

        public WorldMutationResult TryPickUpResource(
            WorldState world,
            string agentId,
            string containerId,
            string resourceId,
            int quantity)
        {
            AgentEntity? agent = EntityQueries.GetAgentEntity(world, agentId);
            ResourceContainerEntity? container = EntityQueries.GetResourceContainer(world, containerId);

            if (agent is null || container is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.EntityNotFound,
                    "Agent or resource container does not exist.");
            }

            if (SpatialQueries.FindNearestAvailableInteractionPoint(
                    container,
                    agent.Position,
                    SpatialQueries.DefaultInteractionTolerance) is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidOperation,
                    "Resource container is out of reach.");
            }

            return TryTransferResource(world, container.Inventory, agent.Inventory, resourceId, quantity);
        }

        public WorldMutationResult TryConsumeResource(
            WorldState world,
            Inventory inventory,
            string resourceId,
            int quantity)
        {
            if (!TryGetDefinition(world, resourceId, out ResourceDefinition? definition, out WorldMutationResult failure))
            {
                return failure;
            }

            if (!definition.IsConsumable)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidOperation,
                    "Resource is not consumable.");
            }

            if (quantity <= 0)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Quantity must be greater than zero.");
            }

            if (inventory.GetQuantity(resourceId) < quantity)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Inventory does not contain enough resource quantity.");
            }

            RemoveResource(inventory, resourceId, quantity);
            RemoveEmptyStacks(inventory);
            RemoveEmptyResourceContainers(world);

            return WorldMutationResult.Success();
        }

        public WorldMutationResult TryDropResource(
            WorldState world,
            string agentId,
            string resourceId,
            int quantity)
        {
            AgentEntity? agent = EntityQueries.GetAgentEntity(world, agentId);

            if (agent is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.EntityNotFound,
                    "Agent does not exist.");
            }

            if (!TryGetDefinition(world, resourceId, out ResourceDefinition? definition, out WorldMutationResult failure))
            {
                return failure;
            }

            if (quantity <= 0)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Quantity must be greater than zero.");
            }

            if (agent.Inventory.GetQuantity(resourceId) < quantity)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Agent inventory does not contain enough resource quantity.");
            }

            ResourceContainerEntity? container = world.ResourceContainers.FirstOrDefault(candidate =>
                candidate.Position.ToGridCell() == agent.Position.ToGridCell());

            if (container is null)
            {
                container = new ResourceContainerEntity
                {
                    Id = NextContainerId(world),
                    Position = agent.Position
                };

                WorldMutationResult spawn = TrySpawnEntity(
                    world,
                    container,
                    new WorldMutationOptions(AllowOccupiedCells: true));

                if (!spawn.Succeeded)
                {
                    return spawn;
                }
            }

            WorldMutationResult transferValidation = ValidateTransfer(
                agent.Inventory,
                container.Inventory,
                world.ResourceDefinitionsById,
                definition,
                quantity);
            if (!transferValidation.Succeeded)
            {
                if (container.IsEmpty)
                {
                    world.ResourceContainers.Remove(container);
                }

                return transferValidation;
            }

            RemoveResource(agent.Inventory, resourceId, quantity);
            AddResource(container.Inventory, definition, quantity);
            RemoveEmptyStacks(agent.Inventory);
            RemoveEmptyStacks(container.Inventory);

            return WorldMutationResult.Success();
        }

        public WorldMutationResult TryTransformResources(
            WorldState world,
            Inventory inventory,
            IReadOnlyDictionary<string, int> consumed,
            IReadOnlyDictionary<string, int> produced)
        {
            Dictionary<string, int> normalizedConsumed = NormalizeQuantities(consumed);
            Dictionary<string, int> normalizedProduced = NormalizeQuantities(produced);

            if (normalizedConsumed.Count == 0 && normalizedProduced.Count == 0)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Resource transform must consume or produce resources.");
            }

            foreach ((string resourceId, int quantity) in normalizedConsumed)
            {
                if (!TryGetDefinition(world, resourceId, out _, out WorldMutationResult failure))
                {
                    return failure;
                }

                if (inventory.GetQuantity(resourceId) < quantity)
                {
                    return WorldMutationResult.Failure(
                        WorldMutationFailureKind.InvalidAmount,
                        "Inventory does not contain enough resource quantity.");
                }
            }

            Inventory simulated = new()
            {
                MaxSlots = inventory.MaxSlots,
                MaxWeight = inventory.MaxWeight
            };
            foreach (ResourceStack stack in inventory.Stacks)
            {
                simulated.Stacks.Add(new ResourceStack
                {
                    StackId = stack.StackId,
                    ResourceId = stack.ResourceId,
                    Quantity = stack.Quantity
                });
            }

            foreach ((string resourceId, int quantity) in normalizedConsumed)
            {
                RemoveResource(simulated, resourceId, quantity);
            }

            RemoveEmptyStacks(simulated);

            foreach ((string resourceId, int quantity) in normalizedProduced)
            {
                if (!TryGetDefinition(world, resourceId, out ResourceDefinition? definition, out WorldMutationResult failure))
                {
                    return failure;
                }

                if (!CanAddResource(simulated, world.ResourceDefinitionsById, definition, quantity, out string reason))
                {
                    return WorldMutationResult.Failure(WorldMutationFailureKind.CapacityExceeded, reason);
                }

                AddResource(simulated, definition, quantity);
            }

            foreach ((string resourceId, int quantity) in normalizedConsumed)
            {
                RemoveResource(inventory, resourceId, quantity);
            }

            RemoveEmptyStacks(inventory);

            foreach ((string resourceId, int quantity) in normalizedProduced)
            {
                ResourceDefinition definition = world.ResourceDefinitionsById[resourceId];
                AddResource(inventory, definition, quantity);
            }

            RemoveEmptyStacks(inventory);
            RemoveEmptyResourceContainers(world);

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
                case ResourceContainerEntity container:
                    world.ResourceContainers.Add(container);
                    return WorldMutationResult.Success();
                case PlantEntity plant:
                    world.Plants.Add(plant);
                    return WorldMutationResult.Success();
                case WaterSourceEntity waterSource:
                    world.WaterSources.Add(waterSource);
                    return WorldMutationResult.Success();
                case ResourceDepositEntity deposit:
                    world.ResourceDeposits.Add(deposit);
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

            ResourceContainerEntity? container = EntityQueries.GetResourceContainer(world, entityId);

            if (container is not null)
            {
                world.ResourceContainers.Remove(container);
                return WorldMutationResult.Success();
            }

            PlantEntity? plant = EntityQueries.GetPlant(world, entityId);
            if (plant is not null)
            {
                world.Plants.Remove(plant);
                return WorldMutationResult.Success();
            }

            WaterSourceEntity? waterSource = EntityQueries.GetWaterSource(world, entityId);
            if (waterSource is not null)
            {
                world.WaterSources.Remove(waterSource);
                return WorldMutationResult.Success();
            }

            ResourceDepositEntity? deposit = EntityQueries.GetResourceDeposit(world, entityId);
            if (deposit is not null)
            {
                world.ResourceDeposits.Remove(deposit);
                return WorldMutationResult.Success();
            }

            return WorldMutationResult.Failure(
                WorldMutationFailureKind.EntityNotFound,
                "Entity does not exist.");
        }

        public WorldMutationResult TryHarvestPlant(
            WorldState world,
            string agentId,
            string plantId,
            int quantity)
        {
            AgentEntity? agent = EntityQueries.GetAgentEntity(world, agentId);
            PlantEntity? plant = EntityQueries.GetPlant(world, plantId);

            if (agent is null || plant is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.EntityNotFound,
                    "Agent or plant does not exist.");
            }

            if (SpatialQueries.FindNearestAvailableInteractionPoint(
                    plant,
                    agent.Position,
                    SpatialQueries.DefaultInteractionTolerance) is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidOperation,
                    "Plant is out of reach.");
            }

            if (!plant.IsHarvestable || plant.Yield < quantity)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Plant does not have enough harvestable yield.");
            }

            if (!TryGetDefinition(world, plant.ResourceId, out ResourceDefinition? definition, out WorldMutationResult failure))
            {
                return failure;
            }

            if (!CanAddResource(agent.Inventory, world.ResourceDefinitionsById, definition, quantity, out string reason))
            {
                return WorldMutationResult.Failure(WorldMutationFailureKind.CapacityExceeded, reason);
            }

            plant.Yield -= quantity;
            if (plant.Yield <= 0)
            {
                plant.Yield = 0;
                plant.TicksUntilRegrowth = Math.Max(1, plant.RegrowthTicks);
                plant.DepletedTicks = 0;
            }

            AddResource(agent.Inventory, definition, quantity);
            return WorldMutationResult.Success();
        }

        public WorldMutationResult TryDrawWater(
            WorldState world,
            string agentId,
            string waterSourceId,
            float amount,
            bool addToInventory)
        {
            AgentEntity? agent = EntityQueries.GetAgentEntity(world, agentId);
            WaterSourceEntity? waterSource = EntityQueries.GetWaterSource(world, waterSourceId);

            if (agent is null || waterSource is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.EntityNotFound,
                    "Agent or water source does not exist.");
            }

            if (SpatialQueries.FindNearestAvailableInteractionPoint(
                    waterSource,
                    agent.Position,
                    SpatialQueries.DefaultInteractionTolerance) is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidOperation,
                    "Water source is out of reach.");
            }

            if (amount <= 0 || waterSource.CurrentVolume < amount)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Water source does not contain enough water.");
            }

            if (addToInventory)
            {
                if (!TryGetDefinition(world, ResourceDefinition.WaterId, out ResourceDefinition? definition, out WorldMutationResult failure))
                {
                    return failure;
                }

                if (!CanAddResource(agent.Inventory, world.ResourceDefinitionsById, definition, (int)MathF.Ceiling(amount), out string reason))
                {
                    return WorldMutationResult.Failure(WorldMutationFailureKind.CapacityExceeded, reason);
                }

                AddResource(agent.Inventory, definition, (int)MathF.Ceiling(amount));
            }

            waterSource.CurrentVolume = Math.Max(0, waterSource.CurrentVolume - amount);
            return WorldMutationResult.Success();
        }

        public WorldMutationResult TryHarvestDeposit(
            WorldState world,
            string agentId,
            string depositId,
            int quantity)
        {
            AgentEntity? agent = EntityQueries.GetAgentEntity(world, agentId);
            ResourceDepositEntity? deposit = EntityQueries.GetResourceDeposit(world, depositId);

            if (agent is null || deposit is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.EntityNotFound,
                    "Agent or resource deposit does not exist.");
            }

            if (SpatialQueries.FindNearestAvailableInteractionPoint(
                    deposit,
                    agent.Position,
                    SpatialQueries.DefaultInteractionTolerance) is null)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidOperation,
                    "Resource deposit is out of reach.");
            }

            if (quantity <= 0 || deposit.Quantity < quantity)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Resource deposit does not contain enough quantity.");
            }

            if (!TryGetDefinition(world, deposit.ResourceId, out ResourceDefinition? definition, out WorldMutationResult failure))
            {
                return failure;
            }

            if (!CanAddResource(agent.Inventory, world.ResourceDefinitionsById, definition, quantity, out string reason))
            {
                return WorldMutationResult.Failure(WorldMutationFailureKind.CapacityExceeded, reason);
            }

            deposit.Quantity -= quantity;
            AddResource(agent.Inventory, definition, quantity);
            return WorldMutationResult.Success();
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

        private static bool TryGetDefinition(
            WorldState world,
            string resourceId,
            out ResourceDefinition definition,
            out WorldMutationResult failure)
        {
            if (!world.ResourceDefinitionsById.TryGetValue(resourceId, out ResourceDefinition? found))
            {
                definition = null!;
                failure = WorldMutationResult.Failure(
                    WorldMutationFailureKind.ResourceNotFound,
                    "Resource definition does not exist.");
                return false;
            }

            definition = found;
            failure = WorldMutationResult.Success();
            return true;
        }

        private static Dictionary<string, int> NormalizeQuantities(IReadOnlyDictionary<string, int> quantities)
        {
            Dictionary<string, int> normalized = new(StringComparer.OrdinalIgnoreCase);
            foreach ((string resourceId, int quantity) in quantities)
            {
                if (string.IsNullOrWhiteSpace(resourceId) || quantity <= 0)
                {
                    continue;
                }

                normalized[resourceId] = normalized.GetValueOrDefault(resourceId) + quantity;
            }

            return normalized;
        }

        private static WorldMutationResult ValidateTransfer(
            Inventory source,
            Inventory destination,
            IReadOnlyDictionary<string, ResourceDefinition> definitions,
            ResourceDefinition definition,
            int quantity)
        {
            if (quantity <= 0)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Quantity must be greater than zero.");
            }

            if (source.GetQuantity(definition.Id) < quantity)
            {
                return WorldMutationResult.Failure(
                    WorldMutationFailureKind.InvalidAmount,
                    "Source inventory does not contain enough resource quantity.");
            }

            if (!CanAddResource(destination, definitions, definition, quantity, out string reason))
            {
                return WorldMutationResult.Failure(WorldMutationFailureKind.CapacityExceeded, reason);
            }

            return WorldMutationResult.Success();
        }

        private static bool CanAddResource(
            Inventory inventory,
            IReadOnlyDictionary<string, ResourceDefinition> definitions,
            ResourceDefinition definition,
            int quantity,
            out string reason)
        {
            int remaining = quantity;
            int simulatedSlots = inventory.Stacks.Count;

            foreach (ResourceStack stack in inventory.Stacks.Where(stack => stack.ResourceId == definition.Id))
            {
                int room = definition.MaxStackQuantity - stack.Quantity;
                if (room > 0)
                {
                    int moved = Math.Min(room, remaining);
                    remaining -= moved;
                }

                if (remaining == 0)
                {
                    break;
                }
            }

            while (remaining > 0)
            {
                simulatedSlots++;
                remaining -= Math.Min(definition.MaxStackQuantity, remaining);
            }

            if (simulatedSlots > inventory.MaxSlots)
            {
                reason = "Destination inventory does not have enough free slots.";
                return false;
            }

            float addedWeight = definition.UnitWeight * quantity;
            if (inventory.GetUsedWeight(definitions) + addedWeight > inventory.MaxWeight)
            {
                reason = "Destination inventory does not have enough weight capacity.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static void AddResource(Inventory inventory, ResourceDefinition definition, int quantity)
        {
            int remaining = quantity;

            foreach (ResourceStack stack in inventory.Stacks.Where(stack => stack.ResourceId == definition.Id))
            {
                int room = definition.MaxStackQuantity - stack.Quantity;
                if (room <= 0)
                {
                    continue;
                }

                int moved = Math.Min(room, remaining);
                stack.Quantity += moved;
                remaining -= moved;

                if (remaining == 0)
                {
                    return;
                }
            }

            while (remaining > 0)
            {
                int moved = Math.Min(definition.MaxStackQuantity, remaining);
                inventory.Stacks.Add(new ResourceStack
                {
                    StackId = NextStackId(inventory, definition.Id),
                    ResourceId = definition.Id,
                    Quantity = moved
                });
                remaining -= moved;
            }
        }

        private static void RemoveResource(Inventory inventory, string resourceId, int quantity)
        {
            int remaining = quantity;

            foreach (ResourceStack stack in inventory.Stacks.Where(stack => stack.ResourceId == resourceId).ToList())
            {
                int moved = Math.Min(stack.Quantity, remaining);
                stack.Quantity -= moved;
                remaining -= moved;

                if (remaining == 0)
                {
                    break;
                }
            }
        }

        private static void RemoveEmptyStacks(Inventory inventory)
        {
            inventory.Stacks.RemoveAll(stack => stack.Quantity <= 0);
        }

        private static void RemoveEmptyResourceContainers(WorldState world)
        {
            world.ResourceContainers.RemoveAll(container => container.IsEmpty);
        }

        private static string NextStackId(Inventory inventory, string resourceId)
        {
            string prefix = $"{resourceId}-stack";
            HashSet<string> existingIds = inventory.Stacks.Select(stack => stack.StackId).ToHashSet(StringComparer.Ordinal);
            int index = inventory.Stacks.Count + 1;
            string id = $"{prefix}-{index}";

            while (existingIds.Contains(id))
            {
                index++;
                id = $"{prefix}-{index}";
            }

            return id;
        }

        private static string NextContainerId(WorldState world)
        {
            HashSet<string> existingIds = world.ResourceContainers.Select(container => container.Id).ToHashSet(StringComparer.Ordinal);
            int index = world.ResourceContainers.Count + 1;
            string id = $"resource-container-{index}";

            while (existingIds.Contains(id))
            {
                index++;
                id = $"resource-container-{index}";
            }

            return id;
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
