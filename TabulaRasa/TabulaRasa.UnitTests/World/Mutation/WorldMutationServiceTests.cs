using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Services;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Mutation;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.World.Mutation
{
    public class WorldMutationServiceTests
    {
        [Fact]
        public void TryMoveEntity_RejectsMissingBlockedOutOfBoundsAndOccupiedDestinations()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var blocker = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            WorldState world = WorldFactory.Create([agent], [blocker], new GridMap(width: 3, height: 3));
            world.Grid.SetTraversable(new GridCell(0, 1), isTraversable: false);
            var mutations = new WorldMutationService();

            WorldMutationResult missing = mutations.TryMoveEntity(world, "missing", new WorldPosition(2.5f, 2.5f));
            WorldMutationResult blocked = mutations.TryMoveEntity(world, agent.Id, new WorldPosition(0.5f, 1.5f));
            WorldMutationResult outOfBounds = mutations.TryMoveEntity(world, agent.Id, new WorldPosition(3.5f, 0.5f));
            WorldMutationResult occupied = mutations.TryMoveEntity(world, agent.Id, blocker.Position);

            Assert.Equal(WorldMutationFailureKind.EntityNotFound, missing.FailureKind);
            Assert.Equal(WorldMutationFailureKind.BlockedCell, blocked.FailureKind);
            Assert.Equal(WorldMutationFailureKind.OutOfBounds, outOfBounds.FailureKind);
            Assert.Equal(WorldMutationFailureKind.OccupiedCell, occupied.FailureKind);
            Assert.Equal(new WorldPosition(0.5f, 0.5f), agent.Position);
        }

        [Fact]
        public void TryMoveEntity_AllowsOccupiedDestinationWhenExplicitlyAllowed()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            WorldState world = WorldFactory.Create([agent], [food], new GridMap(width: 3, height: 3));
            var mutations = new WorldMutationService();

            WorldMutationResult result = mutations.TryMoveEntity(
                world,
                agent.Id,
                food.Position,
                new WorldMutationOptions(AllowOccupiedCells: true));

            Assert.True(result.Succeeded);
            Assert.Equal(food.Position, agent.Position);
        }

        [Fact]
        public void TrySpawnEntity_RejectsDuplicateIdsAndDeleteKeepsLookupStable()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            WorldState world = WorldFactory.Create([agent], [], new GridMap(width: 3, height: 3));
            var mutations = new WorldMutationService();

            WorldMutationResult duplicate = mutations.TrySpawnEntity(
                world,
                TestResourceFactory.FoodContainer(agent.Id, new WorldPosition(1.5f, 0.5f)));
            WorldMutationResult spawn = mutations.TrySpawnEntity(
                world,
                TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f)));
            WorldMutationResult delete = mutations.TryDeleteEntity(world, agent.Id);

            Assert.Equal(WorldMutationFailureKind.DuplicateEntityId, duplicate.FailureKind);
            Assert.True(spawn.Succeeded);
            Assert.True(delete.Succeeded);
            Assert.Null(EntityQueries.GetSpatialEntity(world, agent.Id));
            Assert.NotNull(EntityQueries.GetResourceContainer(world, "food-1"));
        }

        [Fact]
        public void TryDamageEntity_RejectsInvalidAmountsAndClampsHealthToZero()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            WorldState world = WorldFactory.Create([agent], [], new GridMap(width: 3, height: 3));
            var mutations = new WorldMutationService();

            WorldMutationResult invalid = mutations.TryDamageEntity(world, agent.Id, 0);
            WorldMutationResult damage = mutations.TryDamageEntity(world, agent.Id, 99);

            Assert.Equal(WorldMutationFailureKind.InvalidAmount, invalid.FailureKind);
            Assert.True(damage.Succeeded);
            Assert.Equal(0, agent.Health.Current);
            Assert.True(agent.Health.IsDepleted);
        }

        [Fact]
        public void TransferAndConsume_RemoveResourcesAndEmptyWorldContainers()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 1.5f) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1, 1.5f));
            WorldState world = WorldFactory.Create([agent], [food], new GridMap(width: 3, height: 3));
            var mutations = new WorldMutationService();

            WorldMutationResult pickup = mutations.TryPickUpResource(world, agent.Id, food.Id, ResourceDefinition.FoodId, 1);
            WorldMutationResult consume = mutations.TryConsumeResource(world, agent.Inventory, ResourceDefinition.FoodId, 1);

            Assert.True(pickup.Succeeded);
            Assert.True(consume.Succeeded);
            Assert.Equal(0, agent.Inventory.GetQuantity(ResourceDefinition.FoodId));
            Assert.DoesNotContain(food, world.ResourceContainers);
        }

        [Fact]
        public void PickUp_EnforcesWeightCapacity()
        {
            var agent = new AgentEntity
            {
                Id = "agent-1",
                Position = new WorldPosition(0.5f, 1.5f),
                Inventory = new Inventory { MaxSlots = 8, MaxWeight = 0.5f }
            };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1, 1.5f));
            WorldState world = WorldFactory.Create([agent], [food], new GridMap(width: 3, height: 3));
            var mutations = new WorldMutationService();

            WorldMutationResult result = mutations.TryPickUpResource(world, agent.Id, food.Id, ResourceDefinition.FoodId, 1);

            Assert.Equal(WorldMutationFailureKind.CapacityExceeded, result.FailureKind);
            Assert.Equal(0, agent.Inventory.GetQuantity(ResourceDefinition.FoodId));
            Assert.Contains(food, world.ResourceContainers);
        }

        [Fact]
        public void Drop_CreatesWorldContainerAtAgentPosition()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            agent.Inventory.Stacks.Add(new ResourceStack
            {
                StackId = "agent-food",
                ResourceId = ResourceDefinition.FoodId,
                Quantity = 1
            });
            WorldState world = WorldFactory.Create([agent], [], new GridMap(width: 3, height: 3));
            var mutations = new WorldMutationService();

            WorldMutationResult result = mutations.TryDropResource(world, agent.Id, ResourceDefinition.FoodId, 1);

            Assert.True(result.Succeeded);
            ResourceContainerEntity container = Assert.Single(world.ResourceContainers);
            Assert.Equal(agent.Position.ToGridCell(), container.Position.ToGridCell());
            Assert.Equal(1, container.Inventory.GetQuantity(ResourceDefinition.FoodId));
            Assert.Equal(0, agent.Inventory.GetQuantity(ResourceDefinition.FoodId));
        }

        [Fact]
        public void SimulationSnapshotMapper_IncludesOccupancyAndHealth()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            agent.Inventory.Stacks.Add(new ResourceStack
            {
                StackId = "agent-food",
                ResourceId = ResourceDefinition.FoodId,
                Quantity = 1
            });
            var agentState = new AgentState(
                agent.Id,
                new AgentNeedState(),
                new DefaultAgentMind());
            WorldState world = WorldFactory.Create([agent], [], new GridMap(width: 3, height: 3));
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);

            var snapshot = SimulationSnapshotMapper.ToSnapshot(state);

            Assert.Single(snapshot.Grid.OccupiedCells);
            Assert.Equal(agent.Id, snapshot.Grid.OccupiedCells[0].EntityId);
            Assert.Single(snapshot.Agents[0].OccupiedCells);
            Assert.True(snapshot.Agents[0].OccupiesSpace);
            Assert.NotNull(snapshot.Agents[0].Health);
            Assert.Contains(snapshot.ResourceDefinitions, definition => definition.Id == ResourceDefinition.FoodId);
            Assert.Contains(snapshot.ResourceDefinitions, definition => definition.Id == ResourceDefinition.WaterId);
            Assert.Contains(snapshot.ResourceDefinitions, definition => definition.Id == ResourceDefinition.WoodId);
            Assert.Contains(snapshot.ResourceDefinitions, definition => definition.Id == ResourceDefinition.StoneId);
            Assert.Equal(1, snapshot.Agents[0].Inventory.Stacks.Single().Quantity);
            Assert.Equal(1, snapshot.Agents[0].Inventory.UsedSlots);
        }
    }
}
