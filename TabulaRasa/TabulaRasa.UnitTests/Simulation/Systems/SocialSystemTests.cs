using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Services;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Social;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public sealed class SocialSystemTests
    {
        [Fact]
        public void Communication_RepeatedInteractionsIncreaseRelationships()
        {
            SimulationState state = CreateState(
                new AgentEntity { Id = "human-1", SpeciesId = "human", Position = new WorldPosition(0.5f, 0.5f) },
                new AgentEntity { Id = "human-2", SpeciesId = "human", Position = new WorldPosition(1.0f, 0.5f) });
            state.BeginTickObservability(1);
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Communicate, "human-2"));
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Communicate, "human-2"));

            new ActionExecutionSystem().Execute(state);

            SocialRelationship relationship = Assert.Single(state.GetSocialStore("human-1").Relationships);
            Assert.Equal("human-2", relationship.OtherAgentId);
            Assert.Equal(2, relationship.InteractionCount);
            Assert.True(relationship.Familiarity > 0);
            Assert.True(relationship.Trust > 0);
            Assert.True(relationship.Affinity > 0);
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "communication.sent");
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "relationship.changed");
        }

        [Fact]
        public void PlanningSystem_RemembersAgentsOutsideCurrentPerception()
        {
            AgentEntity first = new() { Id = "human-1", SpeciesId = "human", Position = new WorldPosition(0.5f, 0.5f) };
            AgentEntity second = new() { Id = "human-2", SpeciesId = "human", Position = new WorldPosition(1.5f, 0.5f) };
            SimulationState state = CreateState(first, second, perceptionRadius: 3);

            new PlanningSystem().Execute(state);
            Assert.Contains(
                state.LatestPerceptionsByAgentId[first.Id].NearbyEntities,
                entity => entity.EntityId == second.Id);
            Assert.Contains(
                state.GetSocialStore(first.Id).Relationships,
                relationship => relationship.OtherAgentId == second.Id);

            state.PendingIntents.Clear();
            state.ApplyConfig(new SimulationConfig(PerceptionRadius: 0.1f));
            new PlanningSystem().Execute(state);

            Assert.DoesNotContain(
                state.LatestPerceptionsByAgentId[first.Id].NearbyEntities,
                entity => entity.EntityId == second.Id);
            Assert.Contains(
                state.GetSocialStore(first.Id).Relationships,
                relationship => relationship.OtherAgentId == second.Id);
        }

        [Fact]
        public void Communication_TransfersMemoryWithSourceMetadata()
        {
            SimulationState state = CreateState(
                new AgentEntity { Id = "human-1", SpeciesId = "human", Position = new WorldPosition(0.5f, 0.5f) },
                new AgentEntity { Id = "human-2", SpeciesId = "human", Position = new WorldPosition(1.0f, 0.5f) });
            state.BeginTickObservability(1);
            state.GetMemoryStore("human-1").Add(new AgentMemoryRecord
            {
                Id = "location:Food:food-1",
                Kind = AgentMemoryKind.Location,
                SubjectId = "food-1",
                SubjectType = PerceivedEntityType.Food.ToString(),
                Position = new WorldPosition(2.5f, 0.5f),
                CreatedTick = 0,
                LastUpdatedTick = 0,
                Strength = 0.8f,
                Certainty = 0.9f,
                Summary = "Remembered food."
            });
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Communicate, "human-2"));

            new ActionExecutionSystem().Execute(state);

            AgentMemoryRecord transferred = Assert.Single(state.GetMemoryStore("human-2").Memories);
            Assert.Equal("food-1", transferred.SubjectId);
            Assert.Equal("human-1", transferred.Metadata["sourceAgentId"]);
            Assert.Equal("location:Food:food-1", transferred.Metadata["sourceMemoryId"]);
            Assert.True(transferred.Strength < 0.8f);
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "knowledge.transferred");
        }

        [Fact]
        public void Communication_TransfersLearningHintWithoutOverwritingStrongerKnowledge()
        {
            SimulationState state = CreateState(
                new AgentEntity { Id = "human-1", SpeciesId = "human", Position = new WorldPosition(0.5f, 0.5f) },
                new AgentEntity { Id = "human-2", SpeciesId = "human", Position = new WorldPosition(1.0f, 0.5f) });
            state.BeginTickObservability(1);
            AgentState speaker = state.GetAgentById("human-1")!;
            speaker.Learning.GetOrCreate("Hunger|Food|Sight", AgentActionType.Eat).ApplyOutcome(1, true, 1);

            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Communicate, "human-2"));
            new ActionExecutionSystem().Execute(state);

            AgentState listener = state.GetAgentById("human-2")!;
            AgentLearningEntry transferred = Assert.Single(listener.Learning.Entries);
            Assert.Equal("Hunger|Food|Sight", transferred.ContextKey);
            Assert.Equal(AgentActionType.Eat, transferred.ActionType);
            Assert.True(transferred.LearnedWeight > 0);
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "teaching.hook.transferred");

            AgentLearningEntry listenerEntry = listener.Learning.GetOrCreate("Hunger|Food|Sight", AgentActionType.Eat);
            listenerEntry.ApplyOutcome(1, true, 1);
            float strongerWeight = listenerEntry.LearnedWeight;
            speaker.Learning.GetOrCreate("Hunger|Food|Sight", AgentActionType.Eat).ApplyOutcome(0.2f, true, 1);
            state.PendingActionRequests.Add(new ActionRequest("human-1", AgentActionType.Communicate, "human-2"));
            new ActionExecutionSystem().Execute(state);

            Assert.Equal(strongerWeight, listener.Learning.GetOrCreate("Hunger|Food|Sight", AgentActionType.Eat).LearnedWeight);
        }

        [Fact]
        public void Attack_IncreasesTargetFearAndReducesTrust()
        {
            SimulationState state = CreateState(
                new AgentEntity { Id = "wolf-1", SpeciesId = "wolf", Position = new WorldPosition(0.5f, 0.5f) },
                new AgentEntity { Id = "deer-1", SpeciesId = "deer", Position = new WorldPosition(1.0f, 0.5f) });
            state.BeginTickObservability(1);
            state.PendingActionRequests.Add(new ActionRequest("wolf-1", AgentActionType.Attack, "deer-1"));

            new ActionExecutionSystem().Execute(state);

            SocialRelationship relationship = Assert.Single(state.GetSocialStore("deer-1").Relationships);
            Assert.Equal("wolf-1", relationship.OtherAgentId);
            Assert.True(relationship.Fear > 0);
            Assert.Equal(0, relationship.Trust);
            Assert.Equal(0, relationship.Affinity);
        }

        [Fact]
        public void SocialSystem_AddsDefaultGroupsAndSnapshotsExposeSocialGraph()
        {
            SimulationState state = CreateState(
                new AgentEntity { Id = "human-1", SpeciesId = "human", Position = new WorldPosition(0.5f, 0.5f) },
                new AgentEntity { Id = "human-2", SpeciesId = "human", Position = new WorldPosition(1.5f, 0.5f) });
            state.BeginTickObservability(1);

            new SocialSystem().Execute(state);
            SocialService.RecordCommunication(state, "human-1", "human-2");

            var snapshot = SimulationSnapshotMapper.ToSnapshot(state);

            Assert.Contains(snapshot.Agents.Single(agent => agent.Id == "human-1").Social.Groups, group => group.GroupId == "species:human");
            Assert.Contains(snapshot.SocialGraph.Nodes, node => node.AgentId == "human-1" && node.GroupIds.Contains("species:human"));
            Assert.Contains(snapshot.SocialGraph.Edges, edge => edge.FromAgentId == "human-1" && edge.ToAgentId == "human-2");
            Assert.Contains(state.CurrentTickEvents, simulationEvent => simulationEvent.Type == "group.joined");
        }

        private static SimulationState CreateState(params AgentEntity[] agents)
        {
            return CreateState(agents.ToList(), perceptionRadius: 5);
        }

        private static SimulationState CreateState(AgentEntity first, AgentEntity second, float perceptionRadius)
        {
            return CreateState([first, second], perceptionRadius);
        }

        private static SimulationState CreateState(List<AgentEntity> agents, float perceptionRadius)
        {
            WorldState world = WorldFactory.Create(agents, [], new GridMap(5, 3));
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
                new SimulationConfig(PerceptionRadius: perceptionRadius, MovementSpeedPerTick: 1));
        }
    }
}
