using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Movement.Planning;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Systems
{
    public sealed class ReportingSystemTests
    {
        [Fact]
        public void Execute_ReportsCurrentSimulationSurfaces()
        {
            AgentEntity agent = new() { Id = "agent-1", Position = new WorldPosition(0.5f, 1f) };
            ResourceContainerEntity food = TestResourceFactory.FoodContainer("food-1", new WorldPosition(2f, 1f));
            WorldState world = WorldFactory.Create([agent], [food]);
            SimulationState state = new(
                world,
                new SimulationTime(3),
                [new AgentState("agent-1", new AgentNeedState { Hunger = 1.25f }, new DefaultAgentMind())]);
            MovementRoute route = new([new WorldPosition(1f, 1f), new WorldPosition(2f, 1f)]);
            state.ActiveMovements.Add(new ActiveMovement(
                "agent-1",
                AgentActionType.Eat,
                "food-1",
                route,
                speedPerTick: 1f,
                arrivalTolerance: 0.1f));
            state.ActionResults.Add(new ActionResult("agent-1", AgentActionType.Wander, true));
            state.Reservations.TryReserve(
                new ReservationTarget(ReservationTargetType.Entity, "food-1"),
                "task-1",
                currentTick: state.Time.Tick);

            TaskDefinition taskDefinition = new("collect", "Collect", requiredProgressTicks: 2);
            JobInstance job = new(
                "job-1",
                new JobDefinition("collect-job", "Collect Job", [new JobStepDefinition("collect-step", taskDefinition)]));
            job.Activate();
            job.Tasks[0].AssignTo("agent-1");
            state.ActiveJobs.Add(job);

            string report = CaptureReport(state);

            Assert.Contains("Tick 3", report);
            Assert.Contains("World: grid=10x10, agents=1, food=1/1 containers available", report);
            Assert.Contains("Agent agent-1: pos=(0.5, 1) cell=(0, 1) hunger=1.25", report);
            Assert.Contains("movement=Eat->food-1 InProgress waypoint=1/2 destination=(2, 1)", report);
            Assert.Contains("Cognition/actions: intents=0, requests=0, results=1 total", report);
            Assert.Contains("Jobs: pending=0, active=1, terminal=0, tasks=Assigned=1", report);
            Assert.Contains("Reservations: 1 active [Entity:food-1 owner=task-1 no expiry]", report);
        }

        private static string CaptureReport(SimulationState state)
        {
            TextWriter originalOut = Console.Out;
            using StringWriter writer = new();

            try
            {
                Console.SetOut(writer);
                new ReportingSystem().Execute(state);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            return writer.ToString();
        }
    }
}
