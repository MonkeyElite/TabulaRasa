using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Actions.Resolution;
using TabulaRasa.Simulation.Actions.Validation;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Movement.Planning;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Movement
{
    public class MovementSystemTests
    {
        [Fact]
        public void MovementExecutionSystem_AdvancesAgentTowardDestinationWithoutTeleporting()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            WorldState world = WorldFactory.Create([agent], []);
            var state = new SimulationState(world, new SimulationTime(0), []);
            state.ActiveMovements.Add(new ActiveMovement(
                agent.Id,
                AgentActionType.Wander,
                targetId: null,
                new MovementRoute([new WorldPosition(1.5f, 0.5f)]),
                speedPerTick: 0.25f,
                arrivalTolerance: 0.05f));

            new MovementExecutionSystem().Execute(state);

            Assert.Equal(new WorldPosition(0.75f, 0.5f), agent.Position);
            Assert.Single(state.ActiveMovements);
            Assert.Empty(state.ActionResults);
        }

        [Fact]
        public void MovementExecutionSystem_WhenRouteBecomesBlocked_ReplansRoute()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            WorldState world = WorldFactory.Create([agent], [], new TabulaRasa.World.Spatial.Grid.GridMap(3, 2));
            world.Grid.SetTraversable(new GridCell(1, 0), isTraversable: false);
            var state = new SimulationState(world, new SimulationTime(0), []);
            state.ActiveMovements.Add(new ActiveMovement(
                agent.Id,
                AgentActionType.Wander,
                targetId: null,
                new MovementRoute([new WorldPosition(1.5f, 0.5f), new WorldPosition(2.5f, 0.5f)]),
                speedPerTick: 0.25f,
                arrivalTolerance: 0.05f));

            new MovementExecutionSystem().Execute(state);

            ActiveMovement movement = Assert.Single(state.ActiveMovements);
            Assert.Equal(MovementStatus.Repathing, movement.Status);
            Assert.Equal(1, movement.RepathCount);
            Assert.Equal("Route became blocked.", movement.LastRepathReason);
            Assert.Equal(new WorldPosition(2.5f, 0.5f), movement.Route.Destination);
            Assert.Empty(state.ActionResults);
        }

        [Fact]
        public void MovementExecutionSystem_WhenRouteBecomesOccupied_ReplansRoute()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            WorldState world = WorldFactory.Create([agent], [food], new TabulaRasa.World.Spatial.Grid.GridMap(3, 2));
            var state = new SimulationState(world, new SimulationTime(0), []);
            state.ActiveMovements.Add(new ActiveMovement(
                agent.Id,
                AgentActionType.Wander,
                targetId: null,
                new MovementRoute([food.Position, new WorldPosition(2.5f, 0.5f)]),
                speedPerTick: 1f,
                arrivalTolerance: 0.05f));

            new MovementExecutionSystem().Execute(state);

            Assert.Equal(new WorldPosition(0.5f, 0.5f), agent.Position);
            ActiveMovement movement = Assert.Single(state.ActiveMovements);
            Assert.Equal(MovementStatus.Repathing, movement.Status);
            Assert.Equal(1, movement.RepathCount);
            Assert.Equal("Route became occupied.", movement.LastRepathReason);
            Assert.Empty(state.ActionResults);
        }

        [Fact]
        public void MovementExecutionSystem_WhenRepathDestinationIsUnreachable_ReportsFailure()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            WorldState world = WorldFactory.Create([agent], [food], new TabulaRasa.World.Spatial.Grid.GridMap(2, 1));
            var state = new SimulationState(world, new SimulationTime(0), []);
            state.ActiveMovements.Add(new ActiveMovement(
                agent.Id,
                AgentActionType.Wander,
                targetId: null,
                new MovementRoute([food.Position]),
                speedPerTick: 1f,
                arrivalTolerance: 0.05f));

            new MovementExecutionSystem().Execute(state);

            Assert.Empty(state.ActiveMovements);
            ActionResult result = Assert.Single(state.ActionResults);
            Assert.False(result.Succeeded);
            Assert.Equal("Route became occupied. Could not replan route: Destination is unreachable.", result.Reason);
        }

        [Fact]
        public void RoutePlanner_WanderAvoidsOccupiedAdjacentCells()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            WorldState world = WorldFactory.Create([agent], [food]);
            var state = new SimulationState(world, new SimulationTime(0), []);
            var request = new ActionRequest(agent.Id, AgentActionType.Wander, TargetId: null);

            RoutePlanningResult result = new RoutePlanner().Plan(state, request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Movement);
            Assert.Equal(new WorldPosition(0.5f, 1.5f), result.Movement.Route.Destination);
        }

        [Fact]
        public void MovementExecutionSystem_WhenAgentCannotProgress_ReportsStuckFailure()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            WorldState world = WorldFactory.Create([agent], []);
            var state = new SimulationState(world, new SimulationTime(0), []);
            state.ActiveMovements.Add(new ActiveMovement(
                agent.Id,
                AgentActionType.Wander,
                targetId: null,
                new MovementRoute([new WorldPosition(1.5f, 0.5f)]),
                speedPerTick: 0f,
                arrivalTolerance: 0.05f));

            var system = new MovementExecutionSystem();
            system.Execute(state);
            system.Execute(state);
            system.Execute(state);

            Assert.Empty(state.ActiveMovements);
            ActionResult result = Assert.Single(state.ActionResults);
            Assert.False(result.Succeeded);
            Assert.Equal("Movement is stuck.", result.Reason);
        }

        [Fact]
        public void MovementExecutionSystem_AppliesTerrainAndEnergySpeedModifiers()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var grid = new TabulaRasa.World.Spatial.Grid.GridMap(2, 1);
            grid.SetTerrain(new GridCell(1, 0), TabulaRasa.World.Spatial.Grid.GridTerrainType.Forest);
            WorldState world = WorldFactory.Create([agent], [], grid);
            var agentState = new AgentState(
                agent.Id,
                new AgentNeedState { Energy = 0.5f },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);
            state.ActiveMovements.Add(new ActiveMovement(
                agent.Id,
                AgentActionType.Wander,
                targetId: null,
                new MovementRoute([new WorldPosition(1.5f, 0.5f)]),
                speedPerTick: 1f,
                arrivalTolerance: 0.05f));

            new MovementExecutionSystem().Execute(state);

            Assert.Equal(new WorldPosition(1.0625f, 0.5f), agent.Position);
            ActiveMovement movement = Assert.Single(state.ActiveMovements);
            Assert.Equal(0.5625f, movement.LastEffectiveSpeedPerTick);
        }

        [Fact]
        public void SimulationEngine_RoutesAgentToExactInteractionPointBeforeEating()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 1f) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(2f, 1f));
            WorldState world = WorldFactory.Create([agent], [food]);
            var agentState = new AgentState(
                agent.Id,
                new AgentNeedState { Hunger = 5, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);
            ISystem[] systems =
            [
                new PlanningSystem(),
                new ActionRequestCreationSystem(),
                new RoutePlanningSystem(),
                new MovementExecutionSystem(),
                new ActionExecutionSystem(new ActionRequestValidator(), new ActionResolver())
            ];
            var engine = new SimulationEngine(systems);

            engine.Run(state, maxTicks: 4);

            Assert.Equal(1, food.Inventory.GetQuantity(ResourceDefinition.FoodId));
            Assert.Equal(new WorldPosition(1.5f, 1f), agent.Position);

            engine.Run(state, maxTicks: 1);

            Assert.DoesNotContain(food, state.World.ResourceContainers);
            Assert.Empty(state.ActiveMovements);
            Assert.Contains(
                state.ActionResults,
                result => result.ActionType == AgentActionType.Eat && result.Succeeded);
        }
    }
}
