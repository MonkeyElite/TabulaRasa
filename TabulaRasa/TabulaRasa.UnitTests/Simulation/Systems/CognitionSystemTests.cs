using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public class CognitionSystemTests
    {
        [Fact]
        public void PlanningSystem_StoresAgentIntentWithoutMutatingWorld()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 1) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1, 1));
            WorldState world = WorldFactory.Create([agent], [food]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 5, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);

            new PlanningSystem().Execute(state);

            AgentIntent intent = Assert.Single(state.PendingIntents);
            Assert.Equal("agent-1", intent.AgentId);
            Assert.Equal(AgentActionType.Eat, intent.ActionType);
            Assert.Equal("food-1", intent.TargetId);
            Assert.Empty(state.PendingActionRequests);
            Assert.Equal(1, food.Inventory.GetQuantity(ResourceDefinition.FoodId));
        }

        [Fact]
        public void PlanningSystem_CachesPerceptionForEntitiesWithinSightRadius()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var otherAgent = new AgentEntity { Id = "agent-2", Position = new WorldPosition(1.5f, 0.5f) };
            var nearbyFood = TestResourceFactory.FoodContainer("food-near", new WorldPosition(0.5f, 1.0f));
            var farFood = TestResourceFactory.FoodContainer("food-far", new WorldPosition(5.5f, 5.5f));
            WorldState world = WorldFactory.Create([agent, otherAgent], [nearbyFood, farFood]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 5, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(
                world,
                new SimulationTime(0),
                [agentState],
                new SimulationConfig(PerceptionRadius: 1.25f));

            new PlanningSystem().Execute(state);

            AgentPerception perception = state.LatestPerceptionsByAgentId["agent-1"];
            Assert.DoesNotContain(perception.NearbyEntities, entity => entity.EntityId == "agent-1");
            Assert.Contains(perception.NearbyEntities, entity => entity.EntityId == "agent-2" && entity.EntityType == PerceivedEntityType.Agent);
            Assert.Contains(perception.NearbyEntities, entity => entity.EntityId == "food-near" && entity.EntityType == PerceivedEntityType.Food);
            Assert.DoesNotContain(perception.NearbyEntities, entity => entity.EntityId == "food-far");
            Assert.All(perception.NearbyEntities, entity =>
            {
                Assert.Equal(PerceptionChannel.Sight, entity.Channel);
                Assert.Equal(1, entity.Certainty);
                Assert.InRange(entity.Relevance, 0, 1);
            });
        }

        [Fact]
        public void PlanningSystem_IgnoresEmptyResourceContainersAndDoesNotCreateOpportunity()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var emptyContainer = TestResourceFactory.FoodContainer("food-1", new WorldPosition(0.5f, 1.0f), quantity: 0);
            WorldState world = WorldFactory.Create([agent], [emptyContainer]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 8, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(
                world,
                new SimulationTime(0),
                [agentState],
                new SimulationConfig(PerceptionRadius: 5));

            new PlanningSystem().Execute(state);

            AgentPerception perception = state.LatestPerceptionsByAgentId["agent-1"];
            Assert.Empty(perception.NearbyEntities);
            Assert.Empty(perception.Opportunities);
            AgentIntent intent = Assert.Single(state.PendingIntents);
            Assert.Equal(AgentActionType.Wander, intent.ActionType);
            Assert.True(emptyContainer.IsEmpty);
        }

        [Fact]
        public void PlanningSystem_DoesNotMutateWorldWhileBuildingPerception()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(0.5f, 1.0f));
            WorldState world = WorldFactory.Create([agent], [food]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 5, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(
                world,
                new SimulationTime(0),
                [agentState],
                new SimulationConfig(PerceptionRadius: 5));

            new PlanningSystem().Execute(state);

            Assert.Equal(new WorldPosition(0.5f, 0.5f), agent.Position);
            Assert.Equal(new WorldPosition(0.5f, 1.0f), food.Position);
            Assert.Equal(1, food.Inventory.GetQuantity(ResourceDefinition.FoodId));
            Assert.Single(world.Agents);
            Assert.Single(world.ResourceContainers);
        }

        [Fact]
        public void PlanningSystem_HungryAgentIgnoresFoodOutsidePerception()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(4.5f, 4.5f));
            WorldState world = WorldFactory.Create([agent], [food]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 9, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(
                world,
                new SimulationTime(0),
                [agentState],
                new SimulationConfig(PerceptionRadius: 1));

            new PlanningSystem().Execute(state);

            AgentIntent intent = Assert.Single(state.PendingIntents);
            Assert.Equal(AgentActionType.Wander, intent.ActionType);
            Assert.Null(intent.TargetId);
            Assert.Empty(state.LatestPerceptionsByAgentId["agent-1"].Opportunities);
        }

        [Fact]
        public void ActionRequestCreationSystem_ConvertsIntentToBufferedRequest()
        {
            var state = new SimulationState(new WorldState(), new SimulationTime(0), []);
            state.PendingIntents.Add(new AgentIntent("agent-1", AgentActionType.Eat, "food-1"));

            new ActionRequestCreationSystem().Execute(state);

            Assert.Empty(state.PendingIntents);
            ActionRequest request = Assert.Single(state.PendingActionRequests);
            Assert.Equal("agent-1", request.AgentId);
            Assert.Equal(AgentActionType.Eat, request.ActionType);
            Assert.Equal("food-1", request.TargetId);
        }

        [Fact]
        public void ActionExecutionSystem_ConsumesRequestAndAppliesWorldMutation()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 1) };
            var food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1, 1));
            WorldState world = WorldFactory.Create([agent], [food]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 7, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);
            state.PendingActionRequests.Add(new ActionRequest("agent-1", AgentActionType.Eat, "food-1"));

            new ActionExecutionSystem().Execute(state);

            Assert.DoesNotContain(food, world.ResourceContainers);
            Assert.Equal(0, agent.Inventory.GetQuantity(ResourceDefinition.FoodId));
            Assert.Equal(2, agentState.NeedState.Hunger);
            Assert.Empty(state.PendingActionRequests);
            ActionResult result = Assert.Single(state.ActionResults);
            Assert.True(result.Succeeded);
            Assert.Null(result.Reason);
        }

        [Fact]
        public void ActionExecutionSystem_DrinkingAndRestingRecoverCorrectNeeds()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(1, 1) };
            WorldState world = WorldFactory.Create([agent], []);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 6, Thirst = 8, Energy = 2, Fatigue = 9 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);
            state.PendingActionRequests.Add(new ActionRequest("agent-1", AgentActionType.Drink, null));
            state.PendingActionRequests.Add(new ActionRequest("agent-1", AgentActionType.Rest, null));

            new ActionExecutionSystem().Execute(state);

            Assert.Equal(6, agentState.NeedState.Hunger);
            Assert.Equal(3, agentState.NeedState.Thirst);
            Assert.Equal(6, agentState.NeedState.Energy);
            Assert.Equal(4, agentState.NeedState.Fatigue);
            Assert.All(state.ActionResults, result => Assert.True(result.Succeeded));
        }

        [Fact]
        public void ActionExecutionSystem_InvalidRequestProducesFailureResult()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(1, 1) };
            WorldState world = WorldFactory.Create([agent], []);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 7, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);
            state.PendingActionRequests.Add(new ActionRequest("agent-1", AgentActionType.Eat, "missing-food"));

            new ActionExecutionSystem().Execute(state);

            Assert.Empty(state.PendingActionRequests);
            ActionResult result = Assert.Single(state.ActionResults);
            Assert.False(result.Succeeded);
            Assert.Equal(AgentActionType.Eat, result.ActionType);
            Assert.Equal("Target resource container is unavailable or out of reach.", result.Reason);
        }

        [Fact]
        public void NeedDecaySystem_UsesSharedNeedEvaluation()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(1, 1) };
            WorldState world = WorldFactory.Create([agent], []);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 1, Thirst = 2, Energy = 10, Fatigue = 3 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);

            new NeedDecaySystem().Execute(state);

            Assert.Equal(1.08f, agentState.NeedState.Hunger, precision: 3);
            Assert.Equal(2.08f, agentState.NeedState.Thirst, precision: 3);
            Assert.Equal(9.98f, agentState.NeedState.Energy, precision: 3);
            Assert.Equal(3.04f, agentState.NeedState.Fatigue, precision: 3);
        }

        [Fact]
        public void NeedDecaySystem_ExtremeNeedsDamageHealthAndEmitCriticalEvents()
        {
            var agent = new AgentEntity
            {
                Id = "agent-1",
                Position = new WorldPosition(1, 1),
                Health = new EntityHealth(maximum: 10, current: 10)
            };
            WorldState world = WorldFactory.Create([agent], []);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 10, Thirst = 10, Energy = 0, Fatigue = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);
            state.BeginTickObservability(1);

            new NeedDecaySystem().Execute(state);

            Assert.Equal(7, agent.Health.Current);
            Assert.False(agent.IsDead);
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "agent.need_critical");
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "agent.health_damaged");
        }

        [Fact]
        public void NeedDecaySystem_HealthDepletionMarksAgentDeadAndStopsBufferedWork()
        {
            var agent = new AgentEntity
            {
                Id = "agent-1",
                Position = new WorldPosition(1, 1),
                Health = new EntityHealth(maximum: 10, current: 1)
            };
            WorldState world = WorldFactory.Create([agent], []);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 10, Thirst = 10, Energy = 0, Fatigue = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);
            state.PendingIntents.Add(new AgentIntent("agent-1", AgentActionType.Wander, null));
            state.PendingActionRequests.Add(new ActionRequest("agent-1", AgentActionType.Wander, null));
            state.ActiveMovements.Add(new TabulaRasa.Simulation.Movement.Execution.ActiveMovement(
                "agent-1",
                AgentActionType.Wander,
                null,
                new TabulaRasa.Simulation.Movement.Planning.MovementRoute([new WorldPosition(2, 1)]),
                speedPerTick: 1,
                arrivalTolerance: 0.1f));
            state.BeginTickObservability(1);

            new NeedDecaySystem().Execute(state);

            Assert.True(agent.IsDead);
            Assert.True(agent.Health.IsDepleted);
            Assert.Empty(state.PendingIntents);
            Assert.Empty(state.PendingActionRequests);
            Assert.Empty(state.ActiveMovements);
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "agent.died");
        }

        [Fact]
        public void PlanningSystem_DeadAgentsDoNotCreateIntents()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(1, 1), IsDead = true };
            WorldState world = WorldFactory.Create([agent], []);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);

            new PlanningSystem().Execute(state);

            Assert.Empty(state.PendingIntents);
            Assert.Empty(state.LatestPerceptionsByAgentId);
        }
    }
}
