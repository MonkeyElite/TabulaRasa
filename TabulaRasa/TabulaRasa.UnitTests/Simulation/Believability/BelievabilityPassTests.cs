using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Services;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Scenarios;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;

namespace TabulaRasa.UnitTests.Simulation.Believability
{
    public sealed class BelievabilityPassTests
    {
        [Fact]
        public void LifecycleSystem_PopulationPressureBlocksReproductionWhenResourcesAreCollapsed()
        {
            AgentEntity first = AdultDeer("deer-1", new WorldPosition(0.5f, 0.5f));
            AgentEntity second = AdultDeer("deer-2", new WorldPosition(0.95f, 0.5f));
            SimulationState state = new(
                WorldFactory.Create([first, second], [], new GridMap(3, 3)),
                new SimulationTime(0),
                [
                    new AgentState(first.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind()),
                    new AgentState(second.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 }, new DefaultAgentMind())
                ],
                new SimulationConfig(Believability: new BelievabilityConfig(
                    Reproduction: new ReproductionConfig(PopulationPressureInfluence: 1f))));

            Assert.False(LifecycleSystem.CanReproduce(state, first, second));
        }

        [Fact]
        public void RecoverySystem_CoolsRepeatedFailedTargetsAndEmitsImportantEvent()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            SimulationState state = new(
                WorldFactory.Create([agent], [], new GridMap(3, 3)),
                new SimulationTime(0),
                [new AgentState(agent.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10 }, new DefaultAgentMind())],
                new SimulationConfig(Believability: new BelievabilityConfig(
                    Recovery: new RecoveryConfig(FailedTargetCooldownTicks: 12, MaxRepeatedActionFailures: 2, MaxGoalAgeTicks: 40, IdleRecoveryTicks: 5, MovementStuckTicks: 3))));
            state.BeginTickObservability(3);
            state.ActionResults.Add(new ActionResult(agent.Id, AgentActionType.Eat, false, "unavailable", "food-1"));
            state.ActionResults.Add(new ActionResult(agent.Id, AgentActionType.Eat, false, "unavailable", "food-1"));

            new RecoverySystem().Execute(state);

            Assert.Equal(15, state.FailedTargetCooldownsByAgentId[agent.Id]["food-1"]);
            Assert.Contains(state.CurrentTickEvents, simulationEvent =>
                simulationEvent.Type == "recovery.target_cooled_down"
                && simulationEvent.Importance >= 0.5f
                && simulationEvent.Tags?.Contains("anti-stuck") == true);
        }

        [Fact]
        public void SimulationSession_TimelineReturnsSampledBelievabilityMetrics()
        {
            using SimulationSession session = new(
                "timeline-test",
                "Timeline Test",
                SimulationScenarioCatalog.Create("stable-mixed", seed: 77) with
                {
                    SnapshotHistoryLimit = 20,
                    EventHistoryLimit = 20
                });

            session.Step();
            session.Step();
            session.Step();

            var timeline = session.GetTimeline(from: 0, to: 3, sampleEvery: 1);

            Assert.Equal(4, timeline.Count);
            Assert.Equal(3, timeline[^1].Tick);
            Assert.True(timeline[^1].SpeciesPopulation.Sum(species => species.Total) > 0);
            Assert.True(timeline[^1].PlantCount >= 0);
        }

        [Fact]
        public void LifecycleSystem_AgeAdvancesByConfiguredDayScale()
        {
            AgentEntity agent = new()
            {
                Id = "human-1",
                Position = new WorldPosition(0.5f, 0.5f),
                BornTick = 0,
                AgeTicks = 0
            };
            SimulationState state = new(
                WorldFactory.Create([agent], [], new GridMap(2, 2)),
                new SimulationTime(9),
                [new AgentState(agent.Id, new AgentNeedState(), new DefaultAgentMind())],
                new SimulationConfig(Environment: new EnvironmentConfig(DayLengthTicks: 10)));

            new LifecycleSystem().Execute(state);

            Assert.Equal(1, agent.AgeTicks);
            Assert.Equal(0.1f, state.Config.EffectiveLifecycle.AgeDaysPerTick, precision: 3);
        }

        [Fact]
        public void DefaultSimulation_AgentSurvivesLongEnoughToUseReachableResources()
        {
            SimulationConfig config = new()
            {
                EnabledSystems = SimulationConfig.DefaultEnabledSystems
                    .Where(systemId => !string.Equals(systemId, "reporting", StringComparison.OrdinalIgnoreCase))
                    .ToList()
            };
            var (state, systems) = MinimalSimulationFactory.Create(config);

            new SimulationEngine(systems).Run(state, maxTicks: 120);

            Assert.Contains(state.World.Agents, agent => !agent.IsDead);
            Assert.DoesNotContain(state.World.Agents, agent => agent.DeathCause == "survival");
        }

        [Fact]
        public void Factory_UsesConfiguredSpawnResourcesAndSpeciesRules()
        {
            SimulationConfig config = new(
                InitialFoodCount: 1,
                Ecology: new EcologyConfig(InitialPlantCount: 1, InitialWaterSourceCount: 1, InitialResourceDepositCount: 1),
                SpawnResources: new SpawnResourceConfig(
                    FoodStackQuantity: 7,
                    PlantStartingYield: 6,
                    PlantMaxYield: 9,
                    WaterStartingVolume: 22,
                    WaterMaxVolume: 25,
                    DepositQuantity: 11,
                    DepositMaxQuantity: 13),
                SpeciesRules: new SpeciesRulesConfig(
                    Human: new SpeciesRuleConfig(MaxHealth: 12, StartingNeeds: new StartingNeedsConfig(Hunger: 2, Thirst: 3, Energy: 8, Fatigue: 1))));

            var (state, _) = MinimalSimulationFactory.Create(config);

            Assert.Equal(7, state.World.ResourceContainers.Single().Inventory.GetQuantity(ResourceDefinition.FoodId));
            Assert.Equal(6, state.World.Plants.Single().Yield);
            Assert.Equal(9, state.World.Plants.Single().MaxYield);
            Assert.Equal(22, state.World.WaterSources.Single().CurrentVolume);
            Assert.Equal(25, state.World.WaterSources.Single().MaxVolume);
            Assert.Equal(11, state.World.ResourceDeposits.Single().Quantity);
            Assert.Equal(13, state.World.ResourceDeposits.Single().MaxQuantity);
            Assert.Equal(12, state.World.Agents.Single().Health.Maximum);
            Assert.Equal(2, state.Agents.Single().NeedState.Hunger);
            Assert.Equal(3, state.Agents.Single().NeedState.Thirst);
        }

        [Fact]
        public void ConfigNormalization_KeepsMovementGoalAndEcologyKnobs()
        {
            SimulationState state = new(
                WorldFactory.Create([], [], new GridMap(2, 2)),
                new SimulationTime(0),
                [],
                new SimulationConfig(
                    Goals: new GoalConfig(HungerThreshold: 2.5f, UrgentHungerThreshold: 6.5f, InterruptionPriorityDelta: 7),
                    Pathfinding: new PathfindingConfig(
                        AllowDiagonalMovement: true,
                        MaxVisitedCells: 42,
                        MaxRepathAttempts: 5,
                        ArrivalTolerance: 0.2f,
                        InteractionTolerance: 0.35f,
                        AgentInteractionRangeBonus: 0.75f),
                    Ecology: new EcologyConfig(
                        CollapsePlantYieldThreshold: 2,
                        CollapseWaterVolumeThreshold: 3,
                        RecoveryPlantYieldThreshold: 4,
                        RecoveryWaterVolumeThreshold: 5)));

            Assert.Equal(2.5f, state.Config.EffectiveGoals.HungerThreshold);
            Assert.Equal(0.2f, state.Config.EffectivePathfinding.ArrivalTolerance);
            Assert.Equal(0.35f, state.Config.EffectivePathfinding.InteractionTolerance);
            Assert.Equal(2, state.Config.EffectiveEcology.CollapsePlantYieldThreshold);
            Assert.Equal(5, state.Config.EffectiveEcology.RecoveryWaterVolumeThreshold);
        }

        [Fact]
        public void CriticalLifecycleEventsCarrySeverityAndImportance()
        {
            AgentEntity agent = new()
            {
                Id = "agent-1",
                Position = new WorldPosition(0.5f, 0.5f),
                Health = new EntityHealth(1)
            };
            AgentState agentState = new(agent.Id, new AgentNeedState { Hunger = 10, Thirst = 10, Energy = 0, Fatigue = 10 }, new DefaultAgentMind());
            SimulationState state = new(WorldFactory.Create([agent], [], new GridMap(2, 2)), new SimulationTime(0), [agentState]);
            state.BeginTickObservability(1);

            new NeedDecaySystem().Execute(state);

            Assert.Contains(state.CurrentTickEvents, simulationEvent =>
                simulationEvent.Type == "agent.died"
                && simulationEvent.Severity == "critical"
                && simulationEvent.Importance >= 0.9f);
        }

        [Fact]
        public void SeededScenarioRunsAreDeterministicAndDifferentSeedsDiverge()
        {
            string first = RunSignature(SimulationScenarioCatalog.Create("stable-mixed", seed: 55), ticks: 40);
            string second = RunSignature(SimulationScenarioCatalog.Create("stable-mixed", seed: 55), ticks: 40);
            string differentSeed = RunSignature(SimulationScenarioCatalog.Create("stable-mixed", seed: 56), ticks: 40);

            Assert.Equal(first, second);
            Assert.NotEqual(first, differentSeed);
        }

        [Fact]
        public void GoalGenerationSystem_HungerGoalCanTargetHarvestablePlants()
        {
            AgentEntity agent = new()
            {
                Id = "deer-1",
                SpeciesId = SpeciesRegistry.DeerId,
                Position = new WorldPosition(0.5f, 0.5f)
            };
            PlantEntity plant = new()
            {
                Id = "plant-1",
                Position = new WorldPosition(1.5f, 0.5f),
                Yield = 2,
                MaxYield = 3
            };
            SimulationState state = new(
                WorldFactory.Create([agent], [], new GridMap(4, 4), [plant]),
                new SimulationTime(0),
                [new AgentState(agent.Id, new AgentNeedState { Hunger = 5, Thirst = 1, Energy = 10 }, new DefaultAgentMind())],
                new SimulationConfig(PerceptionRadius: 5));
            state.BeginTickObservability(1);

            new PlanningSystem().Execute(state);
            new GoalGenerationSystem().Execute(state);

            JobInstance job = Assert.Single(state.PendingJobs);
            Assert.Contains(job.Tasks, task => task.Definition.TargetId == plant.Id && task.Definition.AtomicAction == AgentActionType.Eat);
            Assert.DoesNotContain(state.PendingIntents, intent => intent.AgentId == agent.Id);
        }

        [Fact]
        public void GoalGenerationSystem_WolfHungerGoalCanTargetPrey()
        {
            AgentEntity wolf = new()
            {
                Id = "wolf-1",
                SpeciesId = SpeciesRegistry.WolfId,
                Position = new WorldPosition(0.5f, 0.5f)
            };
            AgentEntity deer = new()
            {
                Id = "deer-1",
                SpeciesId = SpeciesRegistry.DeerId,
                Position = new WorldPosition(1.5f, 0.5f),
                Health = new EntityHealth(6)
            };
            SimulationState state = new(
                WorldFactory.Create([wolf, deer], [], new GridMap(4, 4)),
                new SimulationTime(0),
                [
                    new AgentState(wolf.Id, new AgentNeedState { Hunger = 8, Thirst = 1, Energy = 10 }, new DefaultAgentMind()),
                    new AgentState(deer.Id, new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10 }, new DefaultAgentMind())
                ],
                new SimulationConfig(PerceptionRadius: 5));
            state.BeginTickObservability(1);

            new PlanningSystem().Execute(state);
            new GoalGenerationSystem().Execute(state);

            JobInstance job = Assert.Single(state.PendingJobs);
            Assert.Contains(job.Tasks, task => task.Definition.TargetId == deer.Id && task.Definition.AtomicAction == AgentActionType.Attack);
            Assert.DoesNotContain(state.PendingIntents, intent => intent.AgentId == wolf.Id);
        }

        private static string RunSignature(SimulationConfig config, int ticks)
        {
            SimulationConfig quiet = config with
            {
                EnabledSystems = config.EffectiveEnabledSystems
                    .Where(systemId => !string.Equals(systemId, "reporting", StringComparison.OrdinalIgnoreCase))
                    .ToList()
            };
            var (state, systems) = MinimalSimulationFactory.Create(quiet);
            new SimulationEngine(systems).Run(state, ticks);

            return string.Join(
                "|",
                state.World.Agents
                    .OrderBy(agent => agent.Id, StringComparer.Ordinal)
                    .Select(agent =>
                    {
                        AgentNeedState? needs = state.GetAgentById(agent.Id)?.NeedState;
                        return $"{agent.Id}:{agent.IsDead}:{agent.Position.X:0.###}:{agent.Position.Y:0.###}:{needs?.Hunger:0.###}:{needs?.Thirst:0.###}";
                    }));
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
    }
}
