using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.State;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Minds;

namespace TabulaRasa.Simulation.Composition
{
    public static class MinimalSimulationFactory
    {
        public static (SimulationState State, IReadOnlyList<ISystem> Systems) Create()
        {
            List<AgentEntity> agentEntities = new List<AgentEntity>();
            List<AgentState> agentStates = new List<AgentState>();
            List<FoodEntity> foods = new List<FoodEntity>();

            agentEntities.Add(new AgentEntity
            {
                Id = "agent-1",
                Position = new WorldPosition(1, 1),
            });

            agentStates.Add(new AgentState("agent-1", new AgentNeedState
            {
                Hunger = 1
            }, new DefaultAgentMind()));

            foods.Add(new FoodEntity
            {
                Id = "food-1",
                Position = new WorldPosition(1, 1),
                IsConsumed = false
            });

            WorldState world = WorldFactory.Create(agentEntities, foods);

            ISystem[] systems =
            [
                new NeedDecaySystem(),
                new PlanningSystem(),
                new ActionExecutionSystem(),
                new ReportingSystem()
            ];

            return (new SimulationState(world, new SimulationTime(Tick: 0), agentStates), systems);
        }
    }
}
