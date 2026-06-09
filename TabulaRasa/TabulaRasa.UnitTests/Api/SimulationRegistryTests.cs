using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Services;

namespace TabulaRasa.UnitTests.Api
{
    public sealed class SimulationRegistryTests
    {
        [Fact]
        public void Registry_StartsWithDefaultSimulation()
        {
            using SimulationRegistry registry = new();

            SimulationSummaryDto summary = registry.List().Single();

            Assert.False(string.IsNullOrWhiteSpace(summary.SimulationId));
            Assert.Equal("Default", summary.Name);
            Assert.Equal("Idle", summary.Status);
        }

        [Fact]
        public void TwoSimulations_StepIndependently()
        {
            using SimulationRegistry registry = new();
            SimulationSession first = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();
            SimulationSession second = registry.Create("Second", Config(seed: 2));

            first.Step();
            first.Step();
            second.Step();

            Assert.Equal(2, first.GetStatus().CurrentTick);
            Assert.Equal(1, second.GetStatus().CurrentTick);
        }

        [Fact]
        public void PauseOneSimulation_DoesNotPauseAnother()
        {
            using SimulationRegistry registry = new();
            SimulationSession first = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();
            SimulationSession second = registry.Create("Second", Config(seed: 2));

            registry.Run(first.SimulationId, 60_000, null);
            registry.Run(second.SimulationId, 60_000, null);
            registry.Pause(first.SimulationId);

            Assert.Equal("Paused", first.GetStatus().Status);
            Assert.Equal("Running", second.GetStatus().Status);
        }

        [Fact]
        public void ResetAndDelete_AffectOnlySelectedSimulation()
        {
            using SimulationRegistry registry = new();
            SimulationSession first = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();
            SimulationSession second = registry.Create("Second", Config(seed: 2));

            first.Step();
            second.Step();
            second.Step();
            registry.Reset(first.SimulationId, null);

            Assert.Equal(0, first.GetStatus().CurrentTick);
            Assert.Equal(2, second.GetStatus().CurrentTick);

            Assert.True(registry.Delete(first.SimulationId));
            Assert.Null(registry.Get(first.SimulationId));
            Assert.NotNull(registry.Get(second.SimulationId));
        }

        [Fact]
        public void SameSeedAndConfig_ProduceDeterministicSnapshots()
        {
            using SimulationRegistry registry = new();
            SimulationConfigDto config = Config(seed: 42, agents: 3, food: 4);
            SimulationSession first = registry.Create("First", config);
            SimulationSession second = registry.Create("Second", config);

            first.Step();
            second.Step();
            SimulationSnapshotDto firstSnapshot = first.GetCurrentSnapshot();
            SimulationSnapshotDto secondSnapshot = second.GetCurrentSnapshot();

            Assert.Equal(
                firstSnapshot.Agents.Select(agent => (agent.Id, agent.Position.X, agent.Position.Y, agent.Needs.Hunger)).ToList(),
                secondSnapshot.Agents.Select(agent => (agent.Id, agent.Position.X, agent.Position.Y, agent.Needs.Hunger)).ToList());
            Assert.Equal(
                firstSnapshot.ResourceContainers.Select(container => (container.Id, container.Position.X, container.Position.Y)).ToList(),
                secondSnapshot.ResourceContainers.Select(container => (container.Id, container.Position.X, container.Position.Y)).ToList());
            Assert.Equal(
                firstSnapshot.Agents.SelectMany(agent => agent.Perception.NearbyEntities)
                    .Select(entity => (entity.EntityId, entity.EntityType, entity.Channel, entity.Distance, entity.Certainty, entity.Relevance))
                    .ToList(),
                secondSnapshot.Agents.SelectMany(agent => agent.Perception.NearbyEntities)
                    .Select(entity => (entity.EntityId, entity.EntityType, entity.Channel, entity.Distance, entity.Certainty, entity.Relevance))
                    .ToList());
        }

        [Fact]
        public void Snapshot_IncludesLatestAgentPerceptionAfterPlanning()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.Create("Perception", Config(seed: 4, agents: 1, food: 1));

            session.Step();

            AgentSnapshotDto agent = Assert.Single(session.GetCurrentSnapshot().Agents);
            PerceivedEntitySnapshotDto perceivedFood = Assert.Single(agent.Perception.NearbyEntities);
            InteractionOpportunitySnapshotDto opportunity = Assert.Single(agent.Perception.Opportunities);
            Assert.Equal("Food", perceivedFood.EntityType);
            Assert.Equal("Sight", perceivedFood.Channel);
            Assert.True(perceivedFood.Distance <= session.GetStatus().Config.PerceptionRadius);
            Assert.Equal(perceivedFood.EntityId, opportunity.TargetId);
            Assert.Equal("Eat", opportunity.ActionType);
            Assert.Equal("Sight", opportunity.Channel);
        }

        [Fact]
        public void Snapshot_IncludesAgentGoalsAndTaskQueues()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.Create("Goals", Config(seed: 4, agents: 1, food: 1, hungerDelta: 5));

            session.Step();

            SimulationSnapshotDto snapshot = session.GetCurrentSnapshot();
            AgentSnapshotDto agent = Assert.Single(snapshot.Agents);
            GoalSnapshotDto goal = Assert.Single(snapshot.Goals);
            JobSnapshotDto job = Assert.Single(snapshot.Jobs);
            Assert.NotNull(agent.CurrentGoal);
            Assert.Equal(goal.Id, agent.CurrentGoal.Id);
            Assert.Equal(goal.Id, job.GoalId);
            Assert.Equal(agent.Id, job.OwnerAgentId);
            Assert.Equal(job.Tasks.Count, agent.TaskQueue.Count);
            Assert.Contains(agent.TaskQueue, task => task.StepId == "find-food");
        }

        [Fact]
        public void SnapshotHistory_PreservesSocialGraphPerTick()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.Create("Social", Config(seed: 4, agents: 2, food: 0));

            session.Step();
            SimulationSnapshotDto tickOne = session.GetCurrentSnapshot();
            session.Step();

            SimulationSnapshotDto? historical = session.GetSnapshot(tickOne.Tick);

            Assert.NotNull(historical);
            Assert.Contains(tickOne.SocialGraph.Edges, edge => edge.FromAgentId == "human-1" && edge.ToAgentId == "human-2");
            Assert.Contains(historical.SocialGraph.Edges, edge => edge.FromAgentId == "human-1" && edge.ToAgentId == "human-2");
            Assert.Contains(session.GetCurrentSnapshot().SocialGraph.Edges, edge => edge.FromAgentId == "human-1" && edge.ToAgentId == "human-2");
        }


        [Fact]
        public void ConcurrentSteps_DoNotCorruptTickProgression()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.Create("Concurrent", Config(seed: 9));

            Parallel.For(0, 20, _ => session.Step());

            Assert.Equal(20, session.GetStatus().CurrentTick);
            Assert.Equal(20, session.GetCurrentSnapshot().Tick);
        }

        [Fact]
        public void ResourceLimits_AreEnforced()
        {
            using SimulationRegistry registry = new(new SimulationResourceLimitsDto(2, 20, 2, 3));
            SimulationSession first = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();
            SimulationSession second = registry.Create("Second", Config(seed: 2));
            SimulationSession third = registry.Create("Third", Config(seed: 3));

            registry.Run(first.SimulationId, 1, null);
            registry.Run(second.SimulationId, 1, null);

            Assert.Throws<InvalidOperationException>(() => registry.Run(third.SimulationId, 1, null));
            Assert.Throws<InvalidOperationException>(() => registry.Create("Too many agents", Config(agents: 3)));

            registry.Pause(first.SimulationId);
            Assert.True(first.GetStatus().Config.TickIntervalMilliseconds >= 50);
        }

        [Fact]
        public void SnapshotRetention_TrimsOldTicks()
        {
            using SimulationRegistry registry = new(new SimulationResourceLimitsDto(4, 20, 200, 3));
            SimulationSession session = registry.Create("Trimmed", Config(snapshotHistoryLimit: 10));

            session.Step();
            session.Step();
            session.Step();
            session.Step();

            Assert.Null(session.GetSnapshot(0));
            Assert.Equal(2, session.GetStatus().MinimumTick);
            Assert.Equal(4, session.GetStatus().MaximumTick);
            Assert.Equal(3, session.GetStatus().Config.SnapshotHistoryLimit);
        }

        [Fact]
        public void ConfigUpdate_IsRejectedWhileRunning()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();

            registry.Run(session.SimulationId, 60_000, null);

            Assert.Throws<InvalidOperationException>(() => registry.UpdateConfig(session.SimulationId, Config(seed: 1)));
        }

        [Fact]
        public void RestartFromDraft_RebuildsEditableStateAndClearsHistory()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();
            session.Step();

            SimulationDraftDto draft = session.GetDraft() with
            {
                Tick = 12,
                Grid = new EditableGridDto(
                    8,
                    8,
                    [new GridCellDto(4, 4)],
                    [new EditableGridTerrainCellDto(new GridCellDto(2, 2), "Forest")]),
                Agents =
                [
                    new EditableAgentDto(
                        "agent-custom",
                        new PositionDto(2.5f, 3.25f),
                        new EditableInventoryDto(8, 10, []),
                        new AgentNeedsDto(4, 5, 6))
                ],
                ResourceDefinitions =
                [
                    new EditableResourceDefinitionDto(
                        "food",
                        "Food",
                        "food",
                        1,
                        10,
                        true,
                        new ResourceNeedEffectsDto(-5, 0, 0, 0))
                ],
                ResourceContainers =
                [
                    new EditableResourceContainerDto(
                        "resource-custom",
                        new PositionDto(1, 1),
                        new EditableInventoryDto(
                            4,
                            100,
                            [new EditableResourceStackDto("resource-custom-food", "food", 1)]))
                ],
                Config = Config(seed: 7, agents: 1, food: 1)
            };

            RestartFromDraftResult result = registry.RestartFromDraft(session.SimulationId, draft);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Snapshot);
            Assert.Equal(12, result.Snapshot.Tick);
            Assert.Equal("agent-custom", result.Snapshot.Agents.Single().Id);
            Assert.Equal("resource-custom", result.Snapshot.ResourceContainers.Single().Id);
            Assert.Equal("Forest", result.Snapshot.Grid.TerrainCells.Single().TerrainType);
            Assert.Null(session.GetSnapshot(0));
            Assert.Equal(7, session.GetStatus().Config.Seed);
        }

        [Fact]
        public void DraftSchema_ExposesEditableFieldsFromSimulationTypes()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();

            SimulationDraftSchemaDto schema = session.GetDraftSchema();

            Assert.Contains(schema.AgentFields, field => field.Path == "position.x" && field.SourceType.EndsWith("AgentEntity"));
            Assert.Contains(schema.AgentFields, field => field.Path == "needs.hunger" && field.SourceType.EndsWith("AgentNeedState"));
            Assert.Contains(schema.ResourceContainerFields, field => field.Path == "position.x" && field.SourceType.EndsWith("ResourceContainerEntity"));
            Assert.Contains(schema.ResourceDefinitionFields, field => field.Path == "unitWeight" && field.SourceType.EndsWith("ResourceDefinition"));
            Assert.Contains(schema.GridFields, field => field.Path == "grid.blockedCells" && field.SourceType.EndsWith("GridMap"));
            Assert.Contains(schema.GridFields, field => field.Path == "grid.terrainCells" && field.SourceType.EndsWith("GridMap"));
        }

        [Fact]
        public void RestartFromDraft_RejectsInvalidTerrainCells()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();
            SimulationDraftDto draft = session.GetDraft() with
            {
                Grid = new EditableGridDto(
                    3,
                    3,
                    [],
                    [
                        new EditableGridTerrainCellDto(new GridCellDto(1, 1), "Forest"),
                        new EditableGridTerrainCellDto(new GridCellDto(1, 1), "Mud"),
                        new EditableGridTerrainCellDto(new GridCellDto(4, 1), "Road"),
                        new EditableGridTerrainCellDto(new GridCellDto(2, 2), "Bog")
                    ])
            };

            RestartFromDraftResult result = registry.RestartFromDraft(session.SimulationId, draft);

            Assert.False(result.Succeeded);
            Assert.Contains("grid.terrainCells[1]", result.Errors.Keys);
            Assert.Contains("grid.terrainCells[2]", result.Errors.Keys);
            Assert.Contains("grid.terrainCells[3].terrainType", result.Errors.Keys);
        }

        [Fact]
        public void RestartFromDraft_RejectsOverCapacityInventories()
        {
            using SimulationRegistry registry = new();
            SimulationSession session = registry.List().Select(summary => registry.Get(summary.SimulationId)!).Single();
            SimulationDraftDto draft = session.GetDraft() with
            {
                Agents =
                [
                    new EditableAgentDto(
                        "agent-1",
                        new PositionDto(0.5f, 0.5f),
                        new EditableInventoryDto(
                            0,
                            0,
                            [new EditableResourceStackDto("agent-food", "food", 1)]),
                        new AgentNeedsDto(1, 0, 10))
                ]
            };

            RestartFromDraftResult result = registry.RestartFromDraft(session.SimulationId, draft);

            Assert.False(result.Succeeded);
            Assert.Contains("agents[0].inventory.maxSlots", result.Errors.Keys);
            Assert.Contains("agents[0].inventory.maxWeight", result.Errors.Keys);
        }

        private static SimulationConfigDto Config(
            int seed = 12345,
            int width = 10,
            int height = 10,
            int interval = 500,
            int agents = 1,
            int food = 1,
            int eventHistoryLimit = 100,
            int snapshotHistoryLimit = 100,
            float hungerDelta = 1)
        {
            return new SimulationConfigDto(
                seed,
                width,
                height,
                interval,
                agents,
                food,
                eventHistoryLimit,
                snapshotHistoryLimit,
                new NeedDecayConfigDto(hungerDelta, 1, -1),
                20,
                0.25f,
                new PathfindingConfigDto(false, 1_000, 3),
                [
                    "need-decay",
                    "planning",
                    "goal-generation",
                    "action-request-creation",
                    "route-planning",
                    "job-activation",
                    "task-assignment",
                    "task-action-dispatch",
                    "movement-execution",
                    "task-execution",
                    "action-execution",
                    "reporting"
                ]);
        }
    }
}
