using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public class CognitionSystemTests
    {
        [Fact]
        public void PlanningSystem_StoresAgentIntentWithoutMutatingWorld()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 1) };
            var food = new FoodEntity { Id = "food-1", Position = new WorldPosition(1, 1) };
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
            Assert.False(food.IsConsumed);
        }

        [Fact]
        public void PlanningSystem_CachesPerceptionForEntitiesWithinSightRadius()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var otherAgent = new AgentEntity { Id = "agent-2", Position = new WorldPosition(1.5f, 0.5f) };
            var nearbyFood = new FoodEntity { Id = "food-near", Position = new WorldPosition(0.5f, 1.0f) };
            var farFood = new FoodEntity { Id = "food-far", Position = new WorldPosition(5.5f, 5.5f) };
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
        public void PlanningSystem_IgnoresConsumedFoodAndDoesNotCreateOpportunity()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var consumedFood = new FoodEntity
            {
                Id = "food-1",
                Position = new WorldPosition(0.5f, 1.0f),
                IsConsumed = true
            };
            WorldState world = WorldFactory.Create([agent], [consumedFood]);
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
            Assert.True(consumedFood.IsConsumed);
        }

        [Fact]
        public void PlanningSystem_DoesNotMutateWorldWhileBuildingPerception()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var food = new FoodEntity { Id = "food-1", Position = new WorldPosition(0.5f, 1.0f) };
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
            Assert.False(food.IsConsumed);
            Assert.Single(world.Agents);
            Assert.Single(world.Foods);
        }

        [Fact]
        public void PlanningSystem_HungryAgentIgnoresFoodOutsidePerception()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var food = new FoodEntity { Id = "food-1", Position = new WorldPosition(4.5f, 4.5f) };
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
            var food = new FoodEntity { Id = "food-1", Position = new WorldPosition(1, 1) };
            WorldState world = WorldFactory.Create([agent], [food]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 7, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);
            state.PendingActionRequests.Add(new ActionRequest("agent-1", AgentActionType.Eat, "food-1"));

            new ActionExecutionSystem().Execute(state);

            Assert.True(food.IsConsumed);
            Assert.Equal(2, agentState.NeedState.Hunger);
            Assert.Empty(state.PendingActionRequests);
            ActionResult result = Assert.Single(state.ActionResults);
            Assert.True(result.Succeeded);
            Assert.Null(result.Reason);
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
            Assert.Equal("Target food is unavailable or out of reach.", result.Reason);
        }

        [Fact]
        public void NeedDecaySystem_UsesSharedNeedEvaluation()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(1, 1) };
            WorldState world = WorldFactory.Create([agent], []);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 1, Thirst = 2, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);

            new NeedDecaySystem().Execute(state);

            Assert.Equal(2, agentState.NeedState.Hunger);
            Assert.Equal(3, agentState.NeedState.Thirst);
            Assert.Equal(9, agentState.NeedState.Energy);
        }
    }
}
