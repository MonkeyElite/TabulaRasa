using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.State;
using System.Globalization;

namespace TabulaRasa.Simulation.Systems
{
    public class ReportingSystem : ISystem
    {
        public string Name => "Reporting System";
        public SimulationPhase Phase => SimulationPhase.Logging;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;
            int availableFood = world.ResourceContainers.Count(SpatialQueries.ContainerHasFood);

            Console.WriteLine($"Tick {state.Time.Tick}");
            Console.WriteLine(
                $"  World: grid={world.Grid.Width}x{world.Grid.Height}, agents={world.Agents.Count}, food={availableFood}/{world.ResourceContainers.Count} containers available");

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState == null)
                {
                    continue;
                }

                ActiveMovement? movement = state.ActiveMovements.FirstOrDefault(m => m.AgentId == agentEntity.Id);

                Console.WriteLine(
                    $"  Agent {agentEntity.Id}: pos={FormatPosition(agentEntity.Position)} cell={FormatCell(agentEntity.Position)} hunger={FormatFloat(agentState.NeedState.Hunger)} | movement={FormatMovement(movement)}");
            }

            Console.WriteLine(
                $"  Cognition/actions: intents={state.PendingIntents.Count}, requests={state.PendingActionRequests.Count}, results={FormatActionResults(state)}");
            Console.WriteLine($"  Jobs: {FormatJobs(state)}");
            Console.WriteLine($"  Reservations: {FormatReservations(state.Reservations)}");
            Console.WriteLine();
        }

        private static string FormatPosition(WorldPosition position)
        {
            return $"({FormatFloat(position.X)}, {FormatFloat(position.Y)})";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatCell(WorldPosition position)
        {
            var cell = position.ToGridCell();

            return $"({cell.X}, {cell.Y})";
        }

        private static string FormatMovement(ActiveMovement? movement)
        {
            if (movement is null)
            {
                return "idle";
            }

            int currentWaypoint = Math.Min(
                movement.CurrentWaypointIndex + 1,
                movement.Route.Waypoints.Count);

            return $"{movement.RequestedAction}->{movement.TargetId ?? "none"} {movement.Status} waypoint={currentWaypoint}/{movement.Route.Waypoints.Count} destination={FormatPosition(movement.Route.Destination)}";
        }

        private static string FormatActionResults(SimulationState state)
        {
            if (state.ActionResults.Count == 0)
            {
                return "0";
            }

            var latestResults = state.ActionResults
                .TakeLast(3)
                .Select(result =>
                {
                    string outcome = result.Succeeded ? "ok" : $"failed:{result.Reason ?? "unknown"}";

                    return $"{result.AgentId} {result.ActionType} {outcome}";
                });

            return $"{state.ActionResults.Count} total, latest=[{string.Join("; ", latestResults)}]";
        }

        private static string FormatJobs(SimulationState state)
        {
            int pendingJobs = state.PendingJobs.Count;
            IReadOnlyList<JobInstance> activeJobs = state.ActiveJobs;
            int terminalJobs = activeJobs.Count(job => job.Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled);

            if (pendingJobs == 0 && activeJobs.Count == 0)
            {
                return "pending=0, active=0, terminal=0, tasks=none";
            }

            string taskStatusSummary = string.Join(
                ", ",
                activeJobs
                    .SelectMany(job => job.Tasks)
                    .GroupBy(task => task.Status)
                    .OrderBy(group => group.Key)
                    .Select(group => $"{group.Key}={group.Count()}"));

            string activeJobSummary = string.Join(
                "; ",
                activeJobs.Select(job => $"{job.Id}:{job.Definition.Name}:{job.Status}"));

            return $"pending={pendingJobs}, active={activeJobs.Count(job => job.Status == JobStatus.Active)}, terminal={terminalJobs}, tasks={taskStatusSummary}, jobs=[{activeJobSummary}]";
        }

        private static string FormatReservations(ReservationRegistry registry)
        {
            if (registry.Reservations.Count == 0)
            {
                return "none";
            }

            IEnumerable<string> reservations = registry.Reservations.Select(FormatReservation);

            return $"{registry.Reservations.Count} active [{string.Join("; ", reservations)}]";
        }

        private static string FormatReservation(Reservation reservation)
        {
            string expiry = reservation.ExpiresAtTick is null
                ? "no expiry"
                : $"expires={reservation.ExpiresAtTick}";

            return $"{reservation.Target.Type}:{reservation.Target.Id} owner={reservation.OwnerId} {expiry}";
        }
    }
}
