using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.Spatial.Navigation.Grid;

namespace TabulaRasa.UnitTests.World.Spatial
{
    public class GridPathfinderTests
    {
        [Fact]
        public void FindPath_FindsRouteAroundBlockedCells()
        {
            var grid = new GridMap(width: 3, height: 3);
            grid.SetTraversable(new GridCell(1, 0), isTraversable: false);
            var pathfinder = new GridPathfinder();

            PathResult result = pathfinder.FindPath(
                grid,
                new PathRequest(new GridCell(0, 0), new GridCell(2, 0)));

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    new GridCell(0, 0),
                    new GridCell(0, 1),
                    new GridCell(1, 1),
                    new GridCell(2, 1),
                    new GridCell(2, 0)
                ],
                result.Path?.Cells);
        }

        [Fact]
        public void FindPath_ChoosesLowestCostPath()
        {
            var grid = new GridMap(width: 5, height: 3);
            grid.SetTerrain(new GridCell(1, 1), GridTerrainType.Mud);
            grid.SetTerrain(new GridCell(2, 1), GridTerrainType.Mud);
            grid.SetTerrain(new GridCell(3, 1), GridTerrainType.Mud);

            for (int x = 0; x < 5; x++)
            {
                grid.SetTerrain(new GridCell(x, 0), GridTerrainType.Road);
            }

            var pathfinder = new GridPathfinder();

            PathResult result = pathfinder.FindPath(
                grid,
                new PathRequest(new GridCell(0, 1), new GridCell(4, 1)));

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Path);
            Assert.DoesNotContain(new GridCell(1, 1), result.Path.Cells);
            Assert.True(result.Path.TotalCost < 5);
        }

        [Fact]
        public void FindPath_AvoidsDynamicObstaclesFromRequestPredicate()
        {
            var grid = new GridMap(width: 3, height: 3);
            var pathfinder = new GridPathfinder();

            PathResult result = pathfinder.FindPath(
                grid,
                new PathRequest(
                    new GridCell(0, 0),
                    new GridCell(2, 0),
                    cell => cell != new GridCell(1, 0)));

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    new GridCell(0, 0),
                    new GridCell(0, 1),
                    new GridCell(1, 1),
                    new GridCell(2, 1),
                    new GridCell(2, 0)
                ],
                result.Path?.Cells);
        }

        [Fact]
        public void FindPath_WhenDestinationIsUnreachable_FailsCleanly()
        {
            var grid = new GridMap(width: 3, height: 3);
            grid.SetTraversable(new GridCell(1, 0), isTraversable: false);
            grid.SetTraversable(new GridCell(0, 1), isTraversable: false);
            var pathfinder = new GridPathfinder();

            PathResult result = pathfinder.FindPath(
                grid,
                new PathRequest(new GridCell(0, 0), new GridCell(2, 2)));

            Assert.False(result.Succeeded);
            Assert.Null(result.Path);
            Assert.Equal("Destination is unreachable.", result.FailureReason);
        }
    }
}
