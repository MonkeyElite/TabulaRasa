using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Services;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Learning;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Movement.Planning;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public class AgentLearningSystemTests
    {
        [Fact]
        public void SuccessfulNeedReducingAction_IncreasesLearnedPreference()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            AgentState agentState = new(
                agent.Id,
                new AgentNeedState { Hunger = 2, Thirst = 0, Energy = 10, Fatigue = 0 },
                new DefaultAgentMind());
            SimulationState state = CreateState([agent], [agentState]);

            AgentLearningService.RecordActionResult(
                state,
                new ActionResult(
                    agent.Id,
                    AgentActionType.Eat,
                    true,
                    TargetId: "food-1",
                    ContextKey: "Hunger|Food|Sight",
                    SelectedGoal: "Hunger",
                    NeedsBefore: new AgentNeedsSnapshot(Hunger: 7, Thirst: 0, Energy: 10, Fatigue: 0)),
                "Test");

            AgentLearningEntry entry = Assert.Single(agentState.Learning.Entries);
            Assert.Equal(1, entry.Attempts);
            Assert.Equal(1, entry.Successes);
            Assert.Equal(0, entry.Failures);
            Assert.Equal(1, entry.LastOutcomeScore);
            Assert.True(entry.LearnedWeight > 0);
        }

        [Fact]
        public void FailedActions_ReducePreferenceAndCanTriggerAlternative()
        {
            AgentLearningProfile learning = new();
            for (int i = 0; i < 4; i++)
            {
                learning.GetOrCreate("Hunger|Food|Sight", AgentActionType.Eat)
                    .ApplyOutcome(-1, succeeded: false, DefaultAgentMind.DefaultLearningRate);
            }

            DefaultAgentMind mind = new(explorationChance: 0);
            AgentPerception perception = new(
                [],
                [new InteractionOpportunity(AgentActionType.Eat, "food-1", new WorldPosition(1.5f, 0.5f), "food-1")]);
            AgentSnapshot self = new(
                "agent-1",
                new AgentNeedsSnapshot(Hunger: 5, Thirst: 0, Energy: 10, Fatigue: 0),
                new WorldPosition(0.5f, 0.5f));

            AgentIntent intent = mind.Decide(perception, self, learning, new Random(1));

            Assert.Equal(AgentActionType.Wander, intent.ActionType);
            Assert.True(learning.GetWeight("Hunger|Food|Sight", AgentActionType.Eat) < 0);
        }

        [Fact]
        public void Learning_IsPerAgentByDefault()
        {
            AgentEntity firstAgent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            AgentEntity secondAgent = new() { Id = "agent-2", Position = new WorldPosition(1.5f, 0.5f) };
            AgentState firstState = new(
                firstAgent.Id,
                new AgentNeedState { Hunger = 2, Energy = 10 },
                new DefaultAgentMind());
            AgentState secondState = new(
                secondAgent.Id,
                new AgentNeedState { Hunger = 2, Energy = 10 },
                new DefaultAgentMind());
            SimulationState state = CreateState([firstAgent, secondAgent], [firstState, secondState]);

            AgentLearningService.RecordActionResult(
                state,
                new ActionResult(
                    firstAgent.Id,
                    AgentActionType.Eat,
                    true,
                    TargetId: "food-1",
                    ContextKey: "Hunger|Food|Sight",
                    SelectedGoal: "Hunger",
                    NeedsBefore: new AgentNeedsSnapshot(Hunger: 7, Thirst: 0, Energy: 10, Fatigue: 0)),
                "Test");

            Assert.Single(firstState.Learning.Entries);
            Assert.Empty(secondState.Learning.Entries);
        }

        [Fact]
        public void AgentsExploreOccasionallyWhenLearningExists()
        {
            AgentLearningProfile learning = new();
            learning.GetOrCreate("Hunger|Food|Sight", AgentActionType.Eat)
                .ApplyOutcome(1, succeeded: true, DefaultAgentMind.DefaultLearningRate);
            DefaultAgentMind mind = new(explorationChance: 1);
            AgentPerception perception = new(
                [],
                [new InteractionOpportunity(AgentActionType.Eat, "food-1", new WorldPosition(1.5f, 0.5f), "food-1")]);
            AgentSnapshot self = new(
                "agent-1",
                new AgentNeedsSnapshot(Hunger: 7, Thirst: 0, Energy: 10, Fatigue: 0),
                new WorldPosition(0.5f, 0.5f));

            AgentIntent intent = mind.Decide(perception, self, learning, new Random(2));

            Assert.NotEqual(AgentActionType.Eat, intent.ActionType);
            Assert.True(learning.LatestDecision?.Explored);
        }

        [Fact]
        public void RoutePlanningFailure_IsRememberedAndLearned()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            AgentState agentState = new(
                agent.Id,
                new AgentNeedState { Hunger = 7, Energy = 10 },
                new DefaultAgentMind());
            SimulationState state = CreateState([agent], [agentState]);
            state.PendingActionRequests.Add(new ActionRequest(
                agent.Id,
                AgentActionType.Eat,
                "missing-food",
                "Hunger|Food|Memory",
                "Hunger",
                "Food",
                "Memory",
                agentState.NeedState.ToSnapshot()));

            new RoutePlanningSystem().Execute(state);

            ActionResult result = Assert.Single(state.ActionResults);
            Assert.False(result.Succeeded);
            Assert.Equal(-1, result.OutcomeScore);
            Assert.Contains(state.GetMemoryStore(agent.Id).Memories, memory => memory.Metadata["contextKey"] == "Hunger|Food|Memory");
            AgentLearningEntry entry = Assert.Single(agentState.Learning.Entries);
            Assert.Equal(AgentActionType.Eat, entry.ActionType);
            Assert.True(entry.LearnedWeight < 0);
        }

        [Fact]
        public void MovementFailure_IsRememberedAndLearned()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            AgentState agentState = new(
                agent.Id,
                new AgentNeedState { Hunger = 1, Energy = 10 },
                new DefaultAgentMind());
            SimulationState state = CreateState([agent], [agentState]);
            state.ActiveMovements.Add(new ActiveMovement(
                agent.Id,
                AgentActionType.Wander,
                targetId: null,
                new MovementRoute([new WorldPosition(1.5f, 0.5f)]),
                speedPerTick: 0,
                arrivalTolerance: 0.05f,
                contextKey: "Hunger|World|Internal",
                selectedGoal: "Hunger",
                needsBefore: agentState.NeedState.ToSnapshot()));

            MovementExecutionSystem system = new();
            system.Execute(state);
            system.Execute(state);
            system.Execute(state);

            ActionResult result = Assert.Single(state.ActionResults);
            Assert.False(result.Succeeded);
            Assert.Equal(-1, result.OutcomeScore);
            Assert.Contains(state.GetMemoryStore(agent.Id).Memories, memory => memory.Metadata["contextKey"] == "Hunger|World|Internal");
            AgentLearningEntry entry = Assert.Single(agentState.Learning.Entries);
            Assert.Equal(AgentActionType.Wander, entry.ActionType);
            Assert.True(entry.LearnedWeight < 0);
        }

        [Fact]
        public void SnapshotMapper_IncludesDecisionLearningAndOutcomeScores()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            AgentState agentState = new(
                agent.Id,
                new AgentNeedState { Hunger = 2, Energy = 10 },
                new DefaultAgentMind());
            SimulationState state = CreateState([agent], [agentState]);
            AgentLearningService.RecordActionResult(
                state,
                new ActionResult(
                    agent.Id,
                    AgentActionType.Eat,
                    true,
                    TargetId: "food-1",
                    ContextKey: "Hunger|Food|Sight",
                    SelectedGoal: "Hunger",
                    NeedsBefore: new AgentNeedsSnapshot(Hunger: 7, Thirst: 0, Energy: 10, Fatigue: 0)),
                "Test");
            agentState.Learning.LatestDecision = new AgentDecisionExplanation(
                new Dictionary<string, float> { ["Hunger"] = 0.7f },
                [
                    new AgentActionScore(
                        AgentActionType.Eat,
                        "food-1",
                        "Hunger",
                        "Hunger|Food|Sight",
                        "Food",
                        "Sight",
                        0.7f,
                        1,
                        0.25f,
                        1.125f)
                ],
                "Hunger",
                AgentActionType.Eat,
                "food-1",
                "Hunger|Food|Sight",
                Explored: false);

            SimulationSnapshotDto snapshot = SimulationSnapshotMapper.ToSnapshot(state);

            AgentSnapshotDto agentSnapshot = Assert.Single(snapshot.Agents);
            Assert.NotNull(agentSnapshot.Decision);
            Assert.Equal("Eat", agentSnapshot.Decision.SelectedAction);
            Assert.Equal("Hunger|Food|Sight", agentSnapshot.Learning.Entries.Single().ContextKey);
            ActionResultSnapshotDto actionResult = Assert.Single(snapshot.RecentActionResults);
            Assert.Equal("food-1", actionResult.TargetId);
            Assert.Equal("Hunger|Food|Sight", actionResult.ContextKey);
            Assert.Equal(1, actionResult.OutcomeScore);
        }

        private static SimulationState CreateState(
            List<AgentEntity> agents,
            List<AgentState> agentStates)
        {
            WorldState world = WorldFactory.Create(agents, []);

            return new SimulationState(
                world,
                new SimulationTime(0),
                agentStates,
                new SimulationConfig(Memory: new MemoryConfig()));
        }
    }
}
