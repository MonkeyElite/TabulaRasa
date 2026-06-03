using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Actions.Resolution;
using TabulaRasa.Simulation.Actions.Validation;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Environment;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public sealed class EcologySystemTests
    {
        [Fact]
        public void EcologySystem_RegrowsAndDecaysPlantsAccordingToRules()
        {
            var regrowing = new PlantEntity
            {
                Id = "plant-regrow",
                Position = new WorldPosition(1.5f, 1.5f),
                Yield = 0,
                MaxYield = 2,
                RegrowthTicks = 1,
                TicksUntilRegrowth = 1,
                DecayTicksAfterDepleted = 5
            };
            var decaying = new PlantEntity
            {
                Id = "plant-decay",
                Position = new WorldPosition(2.5f, 1.5f),
                Yield = 0,
                MaxYield = 2,
                RegrowthTicks = 10,
                TicksUntilRegrowth = 10,
                DecayTicksAfterDepleted = 1
            };
            var state = new SimulationState(
                WorldFactory.Create([], [], new GridMap(4, 4), [regrowing, decaying]),
                new SimulationTime(0),
                []);

            new EcologySystem().Execute(state);

            Assert.Equal(2, regrowing.Yield);
            Assert.DoesNotContain(decaying, state.World.Plants);
        }

        [Fact]
        public void EcologySystem_RefillsRainAndEvaporatesHeatWaterSources()
        {
            var waterSource = new WaterSourceEntity
            {
                Id = "water-1",
                Position = new WorldPosition(1.5f, 1.5f),
                CurrentVolume = 4,
                MaxVolume = 10,
                RefillPerRainTick = 2,
                EvaporationPerHeatTick = 1
            };
            var state = new SimulationState(
                WorldFactory.Create([], [], new GridMap(3, 3), waterSources: [waterSource]),
                new SimulationTime(0),
                []);

            state.World.Environment.Weather = EnvironmentWeather.Rain;
            new EcologySystem().Execute(state);
            Assert.Equal(6, waterSource.CurrentVolume);

            state.World.Environment.Weather = EnvironmentWeather.Heat;
            new EcologySystem().Execute(state);
            Assert.Equal(5, waterSource.CurrentVolume);
        }

        [Fact]
        public void ActionResolver_AgentsCanUseEcologyResources()
        {
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.95f, 1.5f) };
            var agentState = new AgentState(
                agent.Id,
                new AgentNeedState { Hunger = 6, Thirst = 6, Energy = 10 },
                new DefaultAgentMind());
            var plant = new PlantEntity
            {
                Id = "plant-1",
                Position = new WorldPosition(1.5f, 1.5f),
                Yield = 1,
                MaxYield = 1
            };
            var waterSource = new WaterSourceEntity
            {
                Id = "water-1",
                Position = new WorldPosition(0.5f, 2.5f),
                CurrentVolume = 2,
                MaxVolume = 2
            };
            var deposit = new ResourceDepositEntity
            {
                Id = "deposit-1",
                Position = new WorldPosition(1.5f, 2.5f),
                ResourceId = ResourceDefinition.StoneId,
                Quantity = 2,
                MaxQuantity = 2
            };
            var state = new SimulationState(
                WorldFactory.Create([agent], [], new GridMap(4, 4), [plant], [waterSource], [deposit]),
                new SimulationTime(0),
                [agentState]);
            var validator = new ActionRequestValidator();
            var resolver = new ActionResolver();

            Assert.True(validator.Validate(state, new ActionRequest(agent.Id, AgentActionType.Eat, plant.Id)).IsValid);
            Assert.True(resolver.Resolve(state, new ActionRequest(agent.Id, AgentActionType.Eat, plant.Id)).Succeeded);
            Assert.Equal(0, agent.Inventory.GetQuantity(ResourceDefinition.FoodId));
            Assert.Equal(0, plant.Yield);
            agent.Position = new WorldPosition(0.5f, 1.85f);
            Assert.True(resolver.Resolve(state, new ActionRequest(agent.Id, AgentActionType.Drink, waterSource.Id)).Succeeded);
            Assert.Equal(1, waterSource.CurrentVolume);
            agent.Position = new WorldPosition(0.95f, 2.5f);
            Assert.True(resolver.Resolve(state, new ActionRequest(agent.Id, AgentActionType.PickUpResource, deposit.Id)).Succeeded);
            Assert.Equal(1, agent.Inventory.GetQuantity(ResourceDefinition.StoneId));
            Assert.Equal(1, deposit.Quantity);
            Assert.Equal(1, agentState.NeedState.Hunger);
            Assert.Equal(1, agentState.NeedState.Thirst);
        }

        [Fact]
        public void NeedDecaySystem_AppliesTerrainAndHeatSurvivalModifiers()
        {
            var grid = new GridMap(2, 1);
            grid.SetTerrain(new(0, 0), GridTerrainType.Mud);
            var agent = new AgentEntity { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            var agentState = new AgentState(
                agent.Id,
                new AgentNeedState { Hunger = 0, Thirst = 0, Energy = 10, Fatigue = 0 },
                new DefaultAgentMind());
            var state = new SimulationState(
                WorldFactory.Create([agent], [], grid),
                new SimulationTime(0),
                [agentState],
                new SimulationConfig(NeedDecay: new NeedDecayConfig(1, 1, 0, 1)));
            state.World.Environment.Weather = EnvironmentWeather.Heat;
            state.World.Environment.Temperature = 32;

            new NeedDecaySystem().Execute(state);

            Assert.Equal(1, agentState.NeedState.Hunger);
            Assert.Equal(1.5f, agentState.NeedState.Fatigue);
            Assert.Equal(1.5f, agentState.NeedState.Thirst);
        }
    }
}
