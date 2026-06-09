using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Services;
using TabulaRasa.Simulation.Actions.Resolution;
using TabulaRasa.Simulation.Actions.Validation;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Knowledge;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public sealed class InventionSystemTests
    {
        [Fact]
        public void Craft_UnknownRecipeFailsAndDoesNotCreateOutput()
        {
            SimulationState state = CreateState(seed: 1, CreateAgentWithResources("human-1", ResourceDefinition.StoneId, 2));
            state.BeginTickObservability(1);
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Craft, "stone-knapping"));

            new ActionExecutionSystem(new ActionRequestValidator(), new ActionResolver()).Execute(state);

            ActionResult result = Assert.Single(state.ActionResults);
            Assert.False(result.Succeeded);
            Assert.Equal("Recipe is unknown.", result.Reason);
            AgentEntity agent = state.World.Agents.Single();
            Assert.Equal(2, agent.Inventory.GetQuantity(ResourceDefinition.StoneId));
            Assert.Equal(0, agent.Inventory.GetQuantity(ResourceDefinition.StoneToolId));
        }

        [Fact]
        public void Experiment_OutcomesAreDeterministicBySeedAndCanSucceedOrFail()
        {
            Dictionary<int, bool> firstPass = Enumerable.Range(1, 40)
                .ToDictionary(seed => seed, seed => RunExperiment(seed).Succeeded);
            Dictionary<int, bool> secondPass = Enumerable.Range(1, 40)
                .ToDictionary(seed => seed, seed => RunExperiment(seed).Succeeded);

            Assert.Equal(firstPass, secondPass);
            Assert.Contains(true, firstPass.Values);
            Assert.Contains(false, firstPass.Values);
        }

        [Fact]
        public void SuccessfulExperimentCreatesKnowledgeMemoryAndDiscoveryEvent()
        {
            int seed = FindSeedWithOutcome(succeeded: true);
            ExperimentOutcome outcome = RunExperiment(seed);

            Assert.True(outcome.Succeeded);
            Assert.True(outcome.State.GetKnowledgeStore("human-1").KnowsRecipe("stone-knapping"));
            Assert.True(outcome.State.GetKnowledgeStore("human-1").KnowsActionUnlock("use-stone-tool"));
            Assert.Contains(
                outcome.State.GetMemoryStore("human-1").Memories,
                memory => memory.Kind == AgentMemoryKind.Knowledge && memory.SubjectId == "stone-knapping");
            Assert.Contains(outcome.State.CurrentTickEvents, simulationEvent => simulationEvent.Type == "knowledge.discovered");
        }

        [Fact]
        public void Craft_KnownRecipeConsumesInputsAndCreatesOutput()
        {
            AgentEntity agent = CreateAgentWithResources("human-1", ResourceDefinition.StoneId, 2);
            SimulationState state = CreateState(seed: 1, agent);
            state.BeginTickObservability(1);
            KnowledgeService.DiscoverRecipe(state, "human-1", RecipeRegistry.Find("stone-knapping")!, "Experiment");
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Craft, "stone-knapping"));

            new ActionExecutionSystem(new ActionRequestValidator(), new ActionResolver()).Execute(state);

            ActionResult result = state.ActionResults.Last();
            Assert.True(result.Succeeded);
            Assert.Equal(0, agent.Inventory.GetQuantity(ResourceDefinition.StoneId));
            Assert.Equal(1, agent.Inventory.GetQuantity(ResourceDefinition.StoneToolId));
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "recipe.crafted");
        }

        [Fact]
        public void DiscoveredKnowledgeChangesFuturePlanningFromExperimentToCraft()
        {
            AgentEntity agent = CreateAgentWithResources("human-1", ResourceDefinition.StoneId, 2);
            SimulationState state = CreateState(seed: 1, agent);
            state.BeginTickObservability(1);

            new PlanningSystem().Execute(state);
            new GoalGenerationSystem().Execute(state);

            JobInstance unknownJob = Assert.Single(state.PendingJobs);
            Assert.Equal(AgentActionType.Experiment, unknownJob.Tasks.Single().Definition.AtomicAction);

            state.PendingJobs.Clear();
            state.Goals.Clear();
            state.PendingIntents.Clear();
            KnowledgeService.DiscoverRecipe(state, "human-1", RecipeRegistry.Find("stone-knapping")!, "Experiment");
            state.BeginTickObservability(2);

            new PlanningSystem().Execute(state);
            new GoalGenerationSystem().Execute(state);

            JobInstance knownJob = Assert.Single(state.PendingJobs);
            Assert.Equal(AgentActionType.Craft, knownJob.Tasks.Single().Definition.AtomicAction);
        }

        [Fact]
        public void CommunicationTransfersRecipeKnowledgeThroughSocialSystem()
        {
            AgentEntity speaker = CreateAgentWithResources("human-1", ResourceDefinition.StoneId, 2);
            AgentEntity listener = new() { Id = "human-2", SpeciesId = "human", Position = new WorldPosition(1.0f, 0.5f) };
            SimulationState state = CreateState(seed: 1, speaker, listener);
            state.BeginTickObservability(1);
            KnowledgeService.DiscoverRecipe(state, "human-1", RecipeRegistry.Find("stone-knapping")!, "Experiment");
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Communicate, "human-2"));

            new ActionExecutionSystem(new ActionRequestValidator(), new ActionResolver()).Execute(state);

            Assert.True(state.GetKnowledgeStore("human-2").KnowsRecipe("stone-knapping"));
            Assert.True(state.GetKnowledgeStore("human-2").KnowsActionUnlock("use-stone-tool"));
            KnowledgeRecord record = state.GetKnowledgeStore("human-2").Find(KnowledgeKind.Recipe, "stone-knapping")!;
            Assert.Equal("Taught", record.Source);
            Assert.Equal("human-1", record.SourceAgentId);
            Assert.Contains(state.CurrentTickEvents, simulationEvent =>
                simulationEvent.Type == "knowledge.transferred"
                && simulationEvent.Metadata.GetValueOrDefault("recipeId") == "stone-knapping");
        }

        [Fact]
        public void SnapshotExposesCatalogAgentGroupKnowledgeAndDiscoveryMarkers()
        {
            AgentEntity agent = CreateAgentWithResources("human-1", ResourceDefinition.StoneId, 2);
            SimulationState state = CreateState(seed: FindSeedWithOutcome(succeeded: true), agent);
            state.BeginTickObservability(1);
            new SocialSystem().Execute(state);
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Experiment, "stone-knapping"));
            new ActionExecutionSystem(new ActionRequestValidator(), new ActionResolver()).Execute(state);
            state.CompleteTickObservability(1, new(1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, state.CurrentTickEvents.Count, []));
            state.Time = new SimulationTime(1);

            var snapshot = SimulationSnapshotMapper.ToSnapshot(state);

            Assert.Contains(snapshot.RecipeCatalog, recipe => recipe.Id == "stone-knapping");
            Assert.Contains(snapshot.Agents.Single().Knowledge.Records, record => record.SubjectId == "stone-knapping");
            Assert.Contains(snapshot.GroupKnowledge, group =>
                group.GroupId == "species:human"
                && group.KnownRecipeIds.Contains("stone-knapping"));
            Assert.Contains(snapshot.DiscoveryMarkers, marker =>
                marker.Tick == 1
                && marker.AgentId == "human-1"
                && marker.RecipeId == "stone-knapping");
        }

        private static ExperimentOutcome RunExperiment(int seed)
        {
            SimulationState state = CreateState(seed, CreateAgentWithResources("human-1", ResourceDefinition.StoneId, 2));
            state.BeginTickObservability(1);
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Experiment, "stone-knapping"));

            new ActionExecutionSystem(new ActionRequestValidator(), new ActionResolver()).Execute(state);

            return new ExperimentOutcome(state, state.ActionResults.Single().Succeeded);
        }

        private static int FindSeedWithOutcome(bool succeeded)
        {
            return Enumerable.Range(1, 100)
                .First(seed => RunExperiment(seed).Succeeded == succeeded);
        }

        private static AgentEntity CreateAgentWithResources(string id, string resourceId, int quantity)
        {
            AgentEntity agent = new()
            {
                Id = id,
                SpeciesId = "human",
                Position = new WorldPosition(0.5f, 0.5f)
            };
            agent.Inventory.Stacks.Add(new ResourceStack
            {
                StackId = $"{id}-{resourceId}",
                ResourceId = resourceId,
                Quantity = quantity
            });

            return agent;
        }

        private static SimulationState CreateState(int seed, params AgentEntity[] agents)
        {
            WorldState world = WorldFactory.Create(agents.ToList(), [], new GridMap(5, 3));
            List<AgentState> agentStates = agents
                .Select(agent => new AgentState(
                    agent.Id,
                    new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 },
                    new DefaultAgentMind()))
                .ToList();

            return new SimulationState(
                world,
                new SimulationTime(0),
                agentStates,
                new SimulationConfig(Seed: seed, PerceptionRadius: 5, MovementSpeedPerTick: 1));
        }

        private sealed record ExperimentOutcome(SimulationState State, bool Succeeded);
    }
}
