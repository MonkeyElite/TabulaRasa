using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Services;
using TabulaRasa.Simulation.Actions.Resolution;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Evolution;
using TabulaRasa.Simulation.Learning;
using TabulaRasa.Simulation.Movement.Planning;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Spatial.Grid;

namespace TabulaRasa.UnitTests.Simulation.Evolution
{
    public sealed class EvolutionTraitSystemTests
    {
        [Fact]
        public void MinimalSimulationFactory_SeedsDeterministicClampedInitialTraits()
        {
            SimulationConfig config = new(
                Seed: 77,
                SpeciesPopulation: new SpeciesPopulationConfig(Human: 2, Deer: 1, Wolf: 0),
                Traits: new TraitConfig(InitialVariation: 0.12f, MutationChancePerTrait: 0, MutationDelta: 0));

            var (firstState, _) = MinimalSimulationFactory.Create(config);
            var (secondState, _) = MinimalSimulationFactory.Create(config);

            Assert.Equal(
                firstState.World.Agents.Select(agent => agent.Traits).ToList(),
                secondState.World.Agents.Select(agent => agent.Traits).ToList());
            Assert.All(firstState.World.Agents, agent =>
            {
                Assert.InRange(agent.Traits.Perception, 0, 1);
                Assert.InRange(agent.Traits.Speed, 0, 1);
                Assert.InRange(agent.Traits.Metabolism, 0, 1);
                Assert.InRange(agent.Traits.RiskTolerance, 0, 1);
                Assert.InRange(agent.Traits.LearningRate, 0, 1);
            });
        }

        [Fact]
        public void ActionResolver_ReproductionInheritsParentAverageWhenMutationDisabled()
        {
            AgentEntity first = WithTraits(AdultDeer("deer-1", new WorldPosition(0.5f, 0.5f)), new AgentTraits(0.2f, 0.4f, 0.6f, 0.8f, 1f));
            AgentEntity second = WithTraits(AdultDeer("deer-2", new WorldPosition(0.95f, 0.5f)), new AgentTraits(0.8f, 0.6f, 0.4f, 0.2f, 0f));
            SimulationState state = CreateReproductionState(first, second, new TraitConfig(MutationChancePerTrait: 0, MutationDelta: 0));
            state.BeginTickObservability(100);

            Assert.True(new ActionResolver().Resolve(state, new ActionRequest(first.Id, AgentActionType.Reproduce, second.Id)).Succeeded);

            AgentEntity child = Assert.Single(state.World.Agents, agent => agent.Id == "deer-3");
            Assert.Equal(new AgentTraits(0.5f, 0.5f, 0.5f, 0.5f, 0.5f), child.Traits);
        }

        [Fact]
        public void ActionResolver_MutationStaysWithinConfiguredDelta()
        {
            AgentEntity first = WithTraits(AdultDeer("deer-1", new WorldPosition(0.5f, 0.5f)), AgentTraits.Default);
            AgentEntity second = WithTraits(AdultDeer("deer-2", new WorldPosition(0.95f, 0.5f)), AgentTraits.Default);
            SimulationState state = CreateReproductionState(first, second, new TraitConfig(MutationChancePerTrait: 1, MutationDelta: 0.06f));
            state.BeginTickObservability(100);

            Assert.True(new ActionResolver().Resolve(state, new ActionRequest(first.Id, AgentActionType.Reproduce, second.Id)).Succeeded);

            AgentEntity child = Assert.Single(state.World.Agents, agent => agent.Id == "deer-3");
            float[] values = [child.Traits.Perception, child.Traits.Speed, child.Traits.Metabolism, child.Traits.RiskTolerance, child.Traits.LearningRate];
            Assert.All(values, value => Assert.InRange(Math.Abs(value - 0.5f), 0, 0.0601f));
            Assert.Contains(state.CurrentTickEvents, simulationEvent =>
                simulationEvent.Type == "agent.born"
                && simulationEvent.Metadata.GetValueOrDefault("traits.mutated") == "perception,speed,metabolism,riskTolerance,learningRate");
        }

        [Fact]
        public void Traits_ModifyPerceptionSpeedMetabolismAndLearning()
        {
            AgentEntity low = new()
            {
                Id = "deer-1",
                SpeciesId = SpeciesRegistry.DeerId,
                Position = new WorldPosition(0.5f, 0.5f),
                Traits = new AgentTraits(Perception: 0, Speed: 0, Metabolism: 0, RiskTolerance: 0.5f, LearningRate: 0)
            };
            AgentEntity high = new()
            {
                Id = "deer-2",
                SpeciesId = SpeciesRegistry.DeerId,
                Position = new WorldPosition(1.45f, 0.5f),
                Traits = new AgentTraits(Perception: 1, Speed: 1, Metabolism: 1, RiskTolerance: 0.5f, LearningRate: 1)
            };
            AgentState lowState = new(low.Id, new AgentNeedState { Hunger = 0, Thirst = 0, Energy = 10, Fatigue = 0 }, new DefaultAgentMind());
            AgentState highState = new(high.Id, new AgentNeedState { Hunger = 0, Thirst = 0, Energy = 10, Fatigue = 0 }, new DefaultAgentMind());
            SimulationState state = new(
                WorldFactory.Create([low, high], [], new GridMap(4, 4)),
                new SimulationTime(0),
                [lowState, highState],
                new SimulationConfig(
                    PerceptionRadius: 0.9f,
                    MovementSpeedPerTick: 1,
                    NeedDecay: new NeedDecayConfig(HungerDelta: 1, ThirstDelta: 1, EnergyDelta: 0, FatigueDelta: 1)));

            new PlanningSystem().Execute(state);
            Assert.Empty(state.LatestPerceptionsByAgentId[low.Id].NearbyEntities);
            Assert.Contains(state.LatestPerceptionsByAgentId[high.Id].NearbyEntities, entity => entity.EntityId == low.Id);

            RoutePlanningResult route = new RoutePlanner().Plan(state, new ActionRequest(high.Id, AgentActionType.Wander, TargetId: null));
            Assert.True(route.Succeeded);
            Assert.Equal(1.25f * SpeciesRegistry.Get(SpeciesRegistry.DeerId).MovementSpeedMultiplier, route.Movement!.SpeedPerTick);

            new NeedDecaySystem().Execute(state);
            Assert.Equal(1.32f, lowState.NeedState.Hunger, precision: 3);
            Assert.Equal(0.88f, highState.NeedState.Hunger, precision: 3);

            AgentLearningService.RecordActionResult(
                state,
                new ActionResult(high.Id, AgentActionType.Eat, true, ContextKey: "Hunger|Food|Sight", OutcomeScore: 1),
                "test");
            Assert.Equal(0.4f, highState.Learning.GetWeight("Hunger|Food|Sight", AgentActionType.Eat));
        }

        [Fact]
        public void RiskTolerance_AdjustsDangerDecisionScores()
        {
            AgentPerception perception = new(
                [],
                [
                    new InteractionOpportunity(AgentActionType.Flee, "wolf-1", new WorldPosition(1, 1), Relevance: 1)
                ]);
            AgentLearningProfile lowRiskLearning = new();
            AgentLearningProfile highRiskLearning = new();
            AgentSnapshot lowRisk = new(
                "deer-1",
                new AgentNeedsSnapshot(1, 1, 10),
                new WorldPosition(0, 0),
                Traits: new AgentTraits(RiskTolerance: 0));
            AgentSnapshot highRisk = lowRisk with
            {
                AgentId = "deer-2",
                Traits = new AgentTraits(RiskTolerance: 1)
            };
            DefaultAgentMind mind = new();

            mind.Decide(perception, lowRisk, lowRiskLearning, new Random(1));
            mind.Decide(perception, highRisk, highRiskLearning, new Random(1));

            float lowRiskFleeScore = lowRiskLearning.LatestDecision!.ActionScores.Single(score => score.ActionType == AgentActionType.Flee).Score;
            float highRiskFleeScore = highRiskLearning.LatestDecision!.ActionScores.Single(score => score.ActionType == AgentActionType.Flee).Score;
            Assert.True(lowRiskFleeScore > highRiskFleeScore);
        }

        [Fact]
        public void SimulationSnapshotMapper_ReportsEvolutionTraitMetrics()
        {
            AgentEntity alive = new()
            {
                Id = "agent-1",
                Position = new WorldPosition(0.5f, 0.5f),
                Traits = new AgentTraits(Perception: 0.25f)
            };
            AgentEntity dead = new()
            {
                Id = "agent-2",
                Position = new WorldPosition(1.5f, 0.5f),
                IsDead = true,
                Traits = new AgentTraits(Perception: 0.75f)
            };
            SimulationState state = new(
                WorldFactory.Create([alive, dead], [], new GridMap(3, 3)),
                new SimulationTime(5),
                [
                    new AgentState(alive.Id, new AgentNeedState(), new DefaultAgentMind()),
                    new AgentState(dead.Id, new AgentNeedState(), new DefaultAgentMind())
                ]);

            var snapshot = SimulationSnapshotMapper.ToSnapshot(state);

            var perception = Assert.Single(snapshot.Evolution.CurrentTraits, metric => metric.Trait == "perception");
            Assert.Equal(0.5f, perception.Average);
            Assert.Equal(0.25f, perception.AliveAverage);
            Assert.Equal(0.75f, perception.DeadAverage);
            Assert.Contains(snapshot.Evolution.TraitHistory, point => point.Tick == 5 && point.Trait == "perception");
        }

        [Fact]
        public void SimulationEngine_LongMixedPopulationKeepsGenealogyAndTraitStateStable()
        {
            var (state, systems) = MinimalSimulationFactory.Create(new SimulationConfig(
                WorldWidth: 12,
                WorldHeight: 12,
                InitialFoodCount: 2,
                Ecology: new EcologyConfig(InitialPlantCount: 8, InitialWaterSourceCount: 2, InitialResourceDepositCount: 0),
                SpeciesPopulation: new SpeciesPopulationConfig(Human: 1, Deer: 3, Wolf: 1)));
            var engine = new SimulationEngine(systems);

            engine.Run(state, 1_000);

            Assert.Equal(1_000, state.Time.Tick);
            Assert.NotEmpty(state.World.Agents);
            Assert.All(state.World.Agents, agent => Assert.InRange(agent.Traits.Perception, 0, 1));
        }

        private static AgentEntity AdultDeer(string id, WorldPosition position)
        {
            return new AgentEntity
            {
                Id = id,
                SpeciesId = SpeciesRegistry.DeerId,
                Position = position,
                AgeTicks = SpeciesRegistry.Get(SpeciesRegistry.DeerId).AdultAgeDays,
                Health = new EntityHealth(6)
            };
        }

        private static SimulationState CreateReproductionState(AgentEntity first, AgentEntity second, TraitConfig traitConfig)
        {
            return new SimulationState(
                WorldFactory.Create([first, second], [], new GridMap(4, 4)),
                new SimulationTime(0),
                [
                    new AgentState(first.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind()),
                    new AgentState(second.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind())
                ],
                new SimulationConfig(Seed: 13, Traits: traitConfig));
        }

        private static AgentEntity WithTraits(AgentEntity agent, AgentTraits traits)
        {
            agent.Traits = traits;
            return agent;
        }
    }
}
