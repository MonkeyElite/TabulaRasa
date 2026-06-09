using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Actions.Resolution;
using TabulaRasa.Simulation.Actions.Validation;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public sealed class SpeciesLifecycleSystemTests
    {
        [Fact]
        public void SpeciesRegistry_ReturnsBuiltInRules()
        {
            SpeciesDefinition human = SpeciesRegistry.Get("human");
            SpeciesDefinition deer = SpeciesRegistry.Get("deer");
            SpeciesDefinition wolf = SpeciesRegistry.Get("wolf");

            Assert.True(human.CanEatResource(ResourceDefinition.FoodId));
            Assert.True(deer.CanEatResource(ResourceDefinition.FoodId));
            Assert.False(wolf.CanEatResource(ResourceDefinition.FoodId));
            Assert.True(wolf.CanAttackSpecies(deer.Id));
            Assert.False(deer.CanAttackSpecies(wolf.Id));
            Assert.True(deer.MovementSpeedMultiplier > human.MovementSpeedMultiplier);
            Assert.True(wolf.PerceptionMultiplier > human.PerceptionMultiplier);
        }

        [Fact]
        public void PlanningSystem_DeerFleesVisibleWolf()
        {
            AgentEntity deer = new()
            {
                Id = "deer-1",
                SpeciesId = SpeciesRegistry.DeerId,
                Position = new WorldPosition(0.5f, 0.5f)
            };
            AgentEntity wolf = new()
            {
                Id = "wolf-1",
                SpeciesId = SpeciesRegistry.WolfId,
                Position = new WorldPosition(0.95f, 0.5f)
            };
            SimulationState state = CreateState(
                [deer, wolf],
                [
                    new AgentState(deer.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind()),
                    new AgentState(wolf.Id, new AgentNeedState { Hunger = 8, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind())
                ]);

            new PlanningSystem().Execute(state);

            AgentIntent intent = Assert.Single(state.PendingIntents, intent => intent.AgentId == deer.Id);
            Assert.Equal(AgentActionType.Flee, intent.ActionType);
            Assert.Equal(wolf.Id, intent.TargetId);
            Assert.Contains(state.LatestPerceptionsByAgentId[deer.Id].NearbyEntities, entity => entity.EntityType == PerceivedEntityType.Predator);
        }

        [Fact]
        public void ActionExecutionSystem_WolfAttacksDeerAndReducesHunger()
        {
            AgentEntity wolf = new()
            {
                Id = "wolf-1",
                SpeciesId = SpeciesRegistry.WolfId,
                Position = new WorldPosition(0.5f, 0.5f),
                Health = new EntityHealth(8)
            };
            AgentEntity deer = new()
            {
                Id = "deer-1",
                SpeciesId = SpeciesRegistry.DeerId,
                Position = new WorldPosition(0.95f, 0.5f),
                Health = new EntityHealth(6)
            };
            AgentState wolfState = new(wolf.Id, new AgentNeedState { Hunger = 8, Thirst = 1, Energy = 10 }, new DefaultAgentMind());
            SimulationState state = CreateState(
                [wolf, deer],
                [
                    wolfState,
                    new AgentState(deer.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10 }, new DefaultAgentMind())
                ]);
            state.BeginTickObservability(1);
            state.PendingActionRequests.Add(new ActionRequest(wolf.Id, AgentActionType.Attack, deer.Id));

            new ActionExecutionSystem().Execute(state);

            Assert.Equal(2, deer.Health.Current);
            Assert.Equal(3, wolfState.NeedState.Hunger);
            Assert.Contains(state.ActionResults, result => result.ActionType == AgentActionType.Attack && result.Succeeded);
        }

        [Fact]
        public void ActionResolver_ReproductionCreatesOffspringGenealogy()
        {
            AgentEntity first = AdultDeer("deer-1", new WorldPosition(0.5f, 0.5f));
            AgentEntity second = AdultDeer("deer-2", new WorldPosition(0.95f, 0.5f));
            SimulationState state = CreateState(
                [first, second],
                [
                    new AgentState(first.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind()),
                    new AgentState(second.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind())
                ]);
            state.BeginTickObservability(100);

            Assert.True(new ActionRequestValidator().Validate(state, new ActionRequest(first.Id, AgentActionType.Reproduce, second.Id)).IsValid);
            Assert.True(new ActionResolver().Resolve(state, new ActionRequest(first.Id, AgentActionType.Reproduce, second.Id)).Succeeded);

            AgentEntity child = Assert.Single(state.World.Agents, agent => agent.Id == "deer-3");
            Assert.Equal(SpeciesRegistry.DeerId, child.SpeciesId);
            Assert.Contains(first.Id, child.ParentIds);
            Assert.Contains(second.Id, child.ParentIds);
            Assert.Contains(child.Id, first.OffspringIds);
            Assert.Contains(child.Id, second.OffspringIds);
            Assert.Equal(100, child.BornTick);
        }

        [Fact]
        public void LifecycleSystem_MaxAgeKillsAgentAndRecordsCause()
        {
            AgentEntity deer = AdultDeer("deer-1", new WorldPosition(0.5f, 0.5f));
            deer.AgeTicks = SpeciesRegistry.Get(SpeciesRegistry.DeerId).MaxAgeTicks - 1;
            SimulationState state = CreateState(
                [deer],
                [new AgentState(deer.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind())]);
            state.BeginTickObservability(10);

            new LifecycleSystem().Execute(state);

            Assert.True(deer.IsDead);
            Assert.Equal("old_age", deer.DeathCause);
            Assert.Equal(10, deer.DeathTick);
        }

        [Fact]
        public void SimulationEngine_MixedPopulationRunsLongWithoutCrashing()
        {
            var (state, systems) = MinimalSimulationFactory.Create(new SimulationConfig(
                WorldWidth: 12,
                WorldHeight: 12,
                InitialFoodCount: 2,
                Ecology: new EcologyConfig(InitialPlantCount: 8, InitialWaterSourceCount: 2, InitialResourceDepositCount: 0),
                SpeciesPopulation: new SpeciesPopulationConfig(Human: 1, Deer: 3, Wolf: 1)));
            SimulationEngine engine = new(systems);

            engine.Run(state, 1_000);

            Assert.Equal(1_000, state.Time.Tick);
            Assert.NotEmpty(state.World.Agents);
        }

        private static AgentEntity AdultDeer(string id, WorldPosition position)
        {
            return new AgentEntity
            {
                Id = id,
                SpeciesId = SpeciesRegistry.DeerId,
                Position = position,
                AgeTicks = SpeciesRegistry.Get(SpeciesRegistry.DeerId).AdultAgeTicks,
                Health = new EntityHealth(6)
            };
        }

        private static SimulationState CreateState(List<AgentEntity> agents, List<AgentState> agentStates)
        {
            return new SimulationState(
                WorldFactory.Create(agents, [], new GridMap(4, 4)),
                new SimulationTime(0),
                agentStates,
                new SimulationConfig(PerceptionRadius: 5));
        }
    }
}
