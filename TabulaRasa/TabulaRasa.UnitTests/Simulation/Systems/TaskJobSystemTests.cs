using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Assignment;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Execution;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public sealed class TaskJobSystemTests
    {
        [Fact]
        public void TaskExecutionSystem_CompletesMultiStepJobInDependencyOrderAcrossTicks()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 0.5f) };
            WorldState world = WorldFactory.Create([agent], []);
            SimulationState state = CreateState(world, ["agent-1"]);
            TaskDefinition gather = new("gather", "Gather", requiredProgressTicks: 2);
            TaskDefinition deliver = new("deliver", "Deliver", requiredProgressTicks: 1);
            JobDefinition definition = new(
                "haul",
                "Haul Resource",
                [
                    new JobStepDefinition("gather-step", gather),
                    new JobStepDefinition("deliver-step", deliver, ["gather-step"])
                ]);
            JobInstance job = new("job-1", definition);
            state.PendingJobs.Add(job);
            SimulationEngine engine = CreateTaskEngine();

            engine.Run(state, maxTicks: 1);

            Assert.Equal(JobStatus.Active, job.Status);
            Assert.Equal(TaskStatus.InProgress, job.Tasks[0].Status);
            Assert.Equal(TaskStatus.Pending, job.Tasks[1].Status);

            engine.Run(state, maxTicks: 1);

            Assert.Equal(TaskStatus.Completed, job.Tasks[0].Status);
            Assert.Equal(TaskStatus.Pending, job.Tasks[1].Status);

            engine.Run(state, maxTicks: 1);

            Assert.Equal(TaskStatus.Completed, job.Tasks[1].Status);
            Assert.Equal(JobStatus.Completed, job.Status);
        }

        [Fact]
        public void TaskAssignmentSystem_ReservationsPreventTwoAgentsTakingSameTarget()
        {
            WorldState world = WorldFactory.Create([], []);
            SimulationState state = CreateState(world, ["agent-1", "agent-2"]);
            TaskRequirement requirement = new(
                ReservationTargetType.InteractionPoint,
                "food-1:left");
            TaskDefinition useTarget = new(
                "use-target",
                "Use Target",
                requiredProgressTicks: 3,
                requirements: [requirement]);
            JobDefinition definition = new(
                "use-shared-target",
                "Use Shared Target",
                [new JobStepDefinition("use", useTarget)]);
            JobInstance firstJob = new("job-1", definition);
            JobInstance secondJob = new("job-2", definition);
            state.PendingJobs.Add(firstJob);
            state.PendingJobs.Add(secondJob);

            new JobActivationSystem().Execute(state);
            new TaskAssignmentSystem().Execute(state);

            TaskInstance firstTask = firstJob.Tasks[0];
            TaskInstance secondTask = secondJob.Tasks[0];
            Assert.Equal(TaskStatus.Assigned, firstTask.Status);
            Assert.Equal(TaskStatus.Pending, secondTask.Status);
            Assert.Single(state.Reservations.Reservations);
            Assert.Equal(firstTask.Id, state.Reservations.Reservations[0].OwnerId);
        }

        [Fact]
        public void TaskExecutionSystem_ReleasesReservationWhenTaskCompletes()
        {
            WorldState world = WorldFactory.Create([], []);
            SimulationState state = CreateState(world, ["agent-1", "agent-2"]);
            TaskRequirement requirement = new(
                ReservationTargetType.Resource,
                "wood-pile-1");
            TaskDefinition taskDefinition = new(
                "collect",
                "Collect",
                requiredProgressTicks: 1,
                requirements: [requirement]);
            JobInstance job = new(
                "job-1",
                new JobDefinition("collect-job", "Collect Job", [new JobStepDefinition("collect", taskDefinition)]));
            state.PendingJobs.Add(job);
            SimulationEngine engine = CreateTaskEngine();

            engine.Run(state, maxTicks: 1);

            Assert.Equal(JobStatus.Completed, job.Status);
            Assert.Empty(state.Reservations.Reservations);
        }

        [Fact]
        public void TaskAssignmentSystem_FailsTaskWhenPreconditionFails()
        {
            WorldState world = WorldFactory.Create([], []);
            SimulationState state = CreateState(world, ["agent-1"]);
            TaskDefinition taskDefinition = new(
                "eat-food",
                "Eat Food",
                requiredProgressTicks: 1,
                preconditions: [new EntityExistsPrecondition("missing-food")]);
            JobInstance job = new(
                "job-1",
                new JobDefinition("eat-job", "Eat Job", [new JobStepDefinition("eat", taskDefinition)]));
            state.PendingJobs.Add(job);

            new JobActivationSystem().Execute(state);
            new TaskAssignmentSystem().Execute(state);

            Assert.Equal(TaskStatus.Failed, job.Tasks[0].Status);
            Assert.Equal(JobStatus.Failed, job.Status);
            Assert.Equal("Required entity 'missing-food' does not exist.", job.Tasks[0].FailureReason);
        }

        [Fact]
        public void TaskExecutionSystem_CanRepresentInterruptionAndReleaseReservations()
        {
            WorldState world = WorldFactory.Create([], []);
            SimulationState state = CreateState(world, ["agent-1"]);
            TaskRequirement requirement = new(ReservationTargetType.Resource, "tool-1");
            TaskDefinition taskDefinition = new(
                "repair",
                "Repair",
                requiredProgressTicks: 3,
                requirements: [requirement]);
            JobInstance job = new(
                "job-1",
                new JobDefinition("repair-job", "Repair Job", [new JobStepDefinition("repair", taskDefinition)]));
            state.PendingJobs.Add(job);

            new JobActivationSystem().Execute(state);
            new TaskAssignmentSystem().Execute(state);
            TaskInstance task = job.Tasks[0];
            task.Interrupt("Higher priority work arrived.");

            new TaskExecutionSystem().Execute(state);

            Assert.Equal(TaskStatus.Interrupted, task.Status);
            Assert.Empty(state.Reservations.Reservations);
        }

        private static SimulationEngine CreateTaskEngine()
        {
            ISystem[] systems =
            [
                new JobActivationSystem(),
                new TaskAssignmentSystem(),
                new TaskExecutionSystem()
            ];

            return new SimulationEngine(systems);
        }

        private static SimulationState CreateState(WorldState world, IReadOnlyList<string> agentIds)
        {
            List<AgentState> agents = agentIds
                .Select(id => new AgentState(
                    id,
                    new AgentNeedState(),
                    new DefaultAgentMind()))
                .ToList();

            return new SimulationState(world, new SimulationTime(0), agents);
        }
    }
}
