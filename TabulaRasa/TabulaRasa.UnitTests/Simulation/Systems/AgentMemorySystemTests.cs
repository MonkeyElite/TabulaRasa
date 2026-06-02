using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Actions.Requests;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Movement.Planning;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public class AgentMemorySystemTests
    {
        [Fact]
        public void PlanningSystem_RemembersSeenFoodLocations()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 1.5f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(2.5f, 1.5f));
            SimulationState state = CreateState(agent, [food], hunger: 1, perceptionRadius: 5);

            new PlanningSystem().Execute(state);

            AgentMemoryStore store = state.GetMemoryStore(agent.Id);
            Assert.Contains(store.Memories, memory => memory.Kind == AgentMemoryKind.Entity && memory.SubjectId == food.Id);
            Assert.Contains(store.Memories, memory => memory.Kind == AgentMemoryKind.Location && memory.SubjectId == food.Id);
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "memory.created");
        }

        [Fact]
        public void AgentMemorySystem_DecaysAndExpiresWeakMemories()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            SimulationState state = CreateState(
                agent,
                [],
                hunger: 1,
                perceptionRadius: 0,
                memory: new MemoryConfig(DecayPerTick: 0.02f, MinimumStrength: 0.2f));
            state.GetMemoryStore(agent.Id).Add(new AgentMemoryRecord
            {
                Id = "location:Food:food-1",
                Kind = AgentMemoryKind.Location,
                SubjectId = "food-1",
                SubjectType = PerceivedEntityType.Food.ToString(),
                Position = new WorldPosition(2.5f, 0.5f),
                CreatedTick = 0,
                LastUpdatedTick = 0,
                Strength = 0.21f,
                Certainty = 1,
                Summary = "Remembered food."
            });

            new AgentMemorySystem().Execute(state);

            Assert.Empty(state.GetMemoryStore(agent.Id).Memories);
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "memory.expired");
        }

        [Fact]
        public void PlanningSystem_UsesRememberedFoodWhenCurrentPerceptionIsInsufficient()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 1.5f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(3.5f, 1.5f));
            SimulationState state = CreateState(agent, [food], hunger: 1, perceptionRadius: 5);
            new PlanningSystem().Execute(state);
            state.PendingIntents.Clear();
            state.ApplyConfig(new SimulationConfig(
                PerceptionRadius: 0.1f,
                MovementSpeedPerTick: 1,
                Memory: new MemoryConfig()));
            state.GetAgentById(agent.Id)!.NeedState.Hunger = 5;

            new PlanningSystem().Execute(state);
            new ActionRequestCreationSystem().Execute(state);
            new RoutePlanningSystem().Execute(state);

            Assert.Contains(
                state.LatestPerceptionsByAgentId[agent.Id].Opportunities,
                opportunity => opportunity.Channel == PerceptionChannel.Memory && opportunity.TargetId == food.Id);
            ActiveMovement movement = Assert.Single(state.ActiveMovements);
            Assert.Equal(AgentActionType.Eat, movement.RequestedAction);
            Assert.Equal(food.Id, movement.TargetId);
        }

        [Fact]
        public void RoutePlanningSystem_ExpiresStaleRememberedTargetsGracefully()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 1.5f) };
            SimulationState state = CreateState(agent, [], hunger: 5, perceptionRadius: 0.1f);
            state.GetMemoryStore(agent.Id).Add(new AgentMemoryRecord
            {
                Id = "location:Food:food-missing",
                Kind = AgentMemoryKind.Location,
                SubjectId = "food-missing",
                SubjectType = PerceivedEntityType.Food.ToString(),
                Position = new WorldPosition(3.5f, 1.5f),
                CreatedTick = 0,
                LastUpdatedTick = 0,
                Strength = 1,
                Certainty = 1,
                Summary = "Remembered food."
            });

            new PlanningSystem().Execute(state);
            new ActionRequestCreationSystem().Execute(state);
            new RoutePlanningSystem().Execute(state);

            Assert.Empty(state.ActiveMovements);
            ActionResult result = Assert.Single(state.ActionResults);
            Assert.False(result.Succeeded);
            Assert.Equal("Target resource container is unavailable.", result.Reason);
            Assert.DoesNotContain(
                state.GetMemoryStore(agent.Id).Memories,
                memory => memory.SubjectId == "food-missing" && memory.Kind is AgentMemoryKind.Entity or AgentMemoryKind.Location);
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "memory.stale");
        }

        private static SimulationState CreateState(
            AgentEntity agent,
            List<ResourceContainerEntity> foods,
            float hunger,
            float perceptionRadius,
            MemoryConfig? memory = null)
        {
            WorldState world = WorldFactory.Create([agent], foods, new GridMap(5, 3));
            AgentState agentState = new(
                agent.Id,
                new AgentNeedState { Hunger = hunger, Energy = 10 },
                new DefaultAgentMind());

            return new SimulationState(
                world,
                new SimulationTime(0),
                [agentState],
                new SimulationConfig(
                    PerceptionRadius: perceptionRadius,
                    MovementSpeedPerTick: 1,
                    Memory: memory ?? new MemoryConfig()));
        }
    }
}
