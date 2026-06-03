using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Goals;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Movement.Planning;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.Simulation.Tasks.Assignment;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Execution;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
using TabulaRasa.World.State;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public sealed class GoalPlanningSystemTests
    {
        [Fact]
        public void GoalGenerationSystem_CreatesFoodGoalAndJobFromVisibleFood()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            SimulationState state = CreateState(WorldFactory.Create([agent], [food]), hunger: 6);
            SimulationEngine engine = CreateEngine();

            engine.ExecuteTick(state);

            var goal = Assert.Single(state.Goals);
            Assert.Equal("agent-1", goal.AgentId);
            Assert.Equal("Hunger", goal.NeedKey);
            Assert.Equal("food-1", goal.TargetId);
            JobInstance job = Assert.Single(state.ActiveJobs);
            Assert.Equal(goal.Id, job.GoalId);
            Assert.Equal("agent-1", job.OwnerAgentId);
            Assert.Equal(["find-food", "move-to-food", "eat-food"], job.Tasks.Select(task => task.StepId).ToArray());
        }

        [Fact]
        public void GoalGenerationSystem_CanUseRememberedFood()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(4.5f, 4.5f));
            SimulationState state = CreateState(
                WorldFactory.Create([agent], [food]),
                hunger: 6,
                config: new SimulationConfig(PerceptionRadius: 1));
            state.GetMemoryStore("agent-1").Add(new AgentMemoryRecord
            {
                Id = "location:Food:food-1",
                Kind = AgentMemoryKind.Location,
                SubjectId = "food-1",
                SubjectType = "Food",
                Position = food.Position,
                CreatedTick = 0,
                LastUpdatedTick = 0,
                Strength = 1,
                Certainty = 1,
                ExpiresAtTick = 100,
                Summary = "Remembered Food location for food-1."
            });

            CreateEngine().ExecuteTick(state);

            var goal = Assert.Single(state.Goals);
            Assert.Equal("food-1", goal.TargetId);
            Assert.Equal("job:" + goal.Id + ":food", goal.JobId);
        }

        [Fact]
        public void FoodPlan_ExecutesTasksInOrderAndCompletesGoal()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            SimulationState state = CreateState(
                WorldFactory.Create([agent], [food]),
                hunger: 6,
                config: new SimulationConfig(MovementSpeedPerTick: 1));
            SimulationEngine engine = CreateEngine();

            engine.Run(state, maxTicks: 8);

            var goal = Assert.Single(state.Goals);
            Assert.Equal(GoalStatus.Completed, goal.Status);
            JobInstance job = Assert.Single(state.ActiveJobs);
            Assert.Equal(JobStatus.Completed, job.Status);
            Assert.All(job.Tasks, task => Assert.Equal(TaskStatus.Completed, task.Status));
            Assert.Equal(1, job.Tasks[1].DispatchCount);
            Assert.True(job.Tasks[2].DispatchCount >= 1);
            Assert.Equal(0, state.World.Agents[0].Inventory.GetQuantity(ResourceDefinition.FoodId) + state.World.ResourceContainers.Sum(container => container.Inventory.GetQuantity(ResourceDefinition.FoodId)));
            Assert.True(state.GetAgentById("agent-1")!.NeedState.Hunger < 6);
        }

        [Fact]
        public void FailedFoodPrecondition_ReplansWhenHungerRemainsUrgent()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            SimulationState state = CreateState(WorldFactory.Create([agent], [food]), hunger: 9);
            SimulationEngine engine = CreateEngine();

            RunUntil(
                engine,
                state,
                () => state.ActiveJobs.Any(job => job.Tasks.Any(task => task.StepId == "find-food" && task.Status == TaskStatus.Completed)));
            state.World.ResourceContainers.Clear();
            RunUntil(
                engine,
                state,
                () => state.ActiveJobs.Any(job => job.Tasks.Any(task => task.StepId == "move-to-food" && task.Status == TaskStatus.Failed)));
            Assert.Contains(state.ActiveJobs.Single().Tasks, task => task.StepId == "move-to-food" && task.Status == TaskStatus.Failed);
            engine.ExecuteTick(state);

            Assert.Contains(state.Goals, goal => goal.Status == GoalStatus.Failed);
            Assert.Contains(state.Goals, goal => goal.Status == GoalStatus.Active && goal.TargetId is null);
            Assert.Contains(state.GetRecentEvents(), simulationEvent => simulationEvent.Type == "goal.replanned");
        }

        [Fact]
        public void BusyAgents_DoNotReceiveConflictingGoalTasks()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            SimulationState state = CreateState(WorldFactory.Create([agent], [food]), hunger: 6);
            TaskDefinition longTask = new("manual", "Manual Work", requiredProgressTicks: 5);
            JobInstance manualJob = new(
                "manual-job",
                new JobDefinition("manual", "Manual Work", [new JobStepDefinition("manual", longTask)]),
                "agent-1");
            manualJob.Activate();
            manualJob.Tasks[0].AssignTo("agent-1");
            manualJob.Tasks[0].Begin();
            state.ActiveJobs.Add(manualJob);
            SimulationEngine engine = CreateEngine();

            engine.ExecuteTick(state);

            Assert.Empty(state.Goals);
            Assert.Equal(TaskStatus.InProgress, manualJob.Tasks[0].Status);
        }

        [Fact]
        public void UrgentHunger_InterruptsLowerPriorityWork()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(1.5f, 0.5f));
            SimulationState state = CreateState(WorldFactory.Create([agent], [food]), hunger: 9);
            TaskDefinition longTask = new("manual", "Manual Work", requiredProgressTicks: 5);
            JobInstance manualJob = new(
                "manual-job",
                new JobDefinition("manual", "Manual Work", [new JobStepDefinition("manual", longTask)]),
                "agent-1");
            manualJob.Activate();
            manualJob.Tasks[0].AssignTo("agent-1");
            manualJob.Tasks[0].Begin();
            state.ActiveJobs.Add(manualJob);
            SimulationEngine engine = CreateEngine();

            engine.ExecuteTick(state);

            Assert.Equal(TaskStatus.Interrupted, manualJob.Tasks[0].Status);
            Assert.Contains(state.Goals, goal => goal.AgentId == "agent-1" && goal.IsActive);
        }

        private static SimulationState CreateState(
            WorldState world,
            float hunger,
            SimulationConfig? config = null)
        {
            return new SimulationState(
                world,
                new SimulationTime(0),
                [
                    new AgentState(
                        "agent-1",
                        new AgentNeedState { Hunger = hunger, Energy = 10 },
                        new DefaultAgentMind())
                ],
                config);
        }

        private static SimulationEngine CreateEngine()
        {
            return new SimulationEngine(
            [
                new PlanningSystem(),
                new GoalGenerationSystem(),
                new ActionRequestCreationSystem(),
                new JobActivationSystem(),
                new TaskAssignmentSystem(),
                new TaskActionDispatchSystem(),
                new RoutePlanningSystem(),
                new MovementExecutionSystem(),
                new TaskExecutionSystem(),
                new ActionExecutionSystem()
            ]);
        }

        private static void RunUntil(
            SimulationEngine engine,
            SimulationState state,
            Func<bool> condition,
            int maxTicks = 8)
        {
            for (int index = 0; index < maxTicks && !condition(); index++)
            {
                engine.ExecuteTick(state);
            }
        }
    }
}
