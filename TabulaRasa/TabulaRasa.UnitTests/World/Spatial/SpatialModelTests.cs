using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Spatial.Grid;

namespace TabulaRasa.UnitTests.World.Spatial
{
    public class SpatialModelTests
    {
        [Fact]
        public void WorldPosition_ToGridCell_DerivesContainingCell()
        {
            var position = new WorldPosition(2.75f, 3.1f);

            GridCell cell = position.ToGridCell();

            Assert.Equal(new GridCell(2, 3), cell);
        }

        [Fact]
        public void GridMap_ReturnsBoundedTraversableAdjacency()
        {
            var grid = new GridMap(width: 3, height: 3);
            grid.SetTraversable(new GridCell(1, 0), isTraversable: false);

            IReadOnlyList<GridCell> adjacent = grid.GetAdjacentCells(new GridCell(0, 0));
            IReadOnlyList<GridCell> traversable = grid.GetTraversableAdjacentCells(new GridCell(0, 0));

            Assert.Equal([new GridCell(1, 0), new GridCell(0, 1)], adjacent);
            Assert.Equal([new GridCell(0, 1)], traversable);
        }

        [Fact]
        public void SpatialQueries_FindAvailableFoodAtInteractionPoint_UsesExactAnchor()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 1) };
            var food = new FoodEntity { Id = "food-1", Position = new WorldPosition(1, 1) };
            var world = WorldFactory.Create([agent], [food]);

            FoodEntity? result = SpatialQueries.FindAvailableFoodAtInteractionPoint(
                world,
                agent.Position,
                food.Id);

            Assert.Equal(food, result);
        }

        [Fact]
        public void SpatialQueries_FindAvailableFoodAtInteractionPoint_RejectsTileCenterOnlyOverlap()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(1, 1) };
            var food = new FoodEntity { Id = "food-1", Position = new WorldPosition(1, 1) };
            var world = WorldFactory.Create([agent], [food]);

            FoodEntity? result = SpatialQueries.FindAvailableFoodAtInteractionPoint(
                world,
                agent.Position,
                food.Id);

            Assert.Null(result);
        }

        [Fact]
        public void SpatialQueries_GetNearbyCells_ClipsToWorldBounds()
        {
            var world = new TabulaRasa.World.State.WorldState(new GridMap(width: 3, height: 3));

            IReadOnlyList<GridCell> cells = SpatialQueries.GetNearbyCells(world, new GridCell(0, 0), radius: 1);

            Assert.Equal(
                [
                    new GridCell(0, 0),
                    new GridCell(1, 0),
                    new GridCell(0, 1),
                    new GridCell(1, 1)
                ],
                cells);
        }
    }
}
