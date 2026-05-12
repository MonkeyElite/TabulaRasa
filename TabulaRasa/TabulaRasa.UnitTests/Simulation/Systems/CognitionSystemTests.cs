using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
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
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(1, 1) };
            var food = new FoodEntity { Id = "food-1", Position = new WorldPosition(1, 1) };
            WorldState world = WorldFactory.Create([agent], [food]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 5, Energy = 10 },
                new DefaultAgentMind());
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);

            new PlanningSystem().Execute(state);

            Assert.NotNull(agentState.PendingIntent);
            Assert.Equal(AgentActionType.Eat, agentState.PendingIntent.ActionType);
            Assert.Equal("food-1", agentState.PendingIntent.TargetId);
            Assert.False(food.IsConsumed);
        }

        [Fact]
        public void ActionExecutionSystem_ConsumesIntentAndAppliesWorldMutation()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(1, 1) };
            var food = new FoodEntity { Id = "food-1", Position = new WorldPosition(1, 1) };
            WorldState world = WorldFactory.Create([agent], [food]);
            var agentState = new AgentState(
                "agent-1",
                new AgentNeedState { Hunger = 7, Energy = 10 },
                new DefaultAgentMind())
            {
                PendingIntent = new AgentIntent("agent-1", AgentActionType.Eat, "food-1")
            };
            var state = new SimulationState(world, new SimulationTime(0), [agentState]);

            new ActionExecutionSystem().Execute(state);

            Assert.True(food.IsConsumed);
            Assert.Equal(2, agentState.NeedState.Hunger);
            Assert.Null(agentState.PendingIntent);
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
