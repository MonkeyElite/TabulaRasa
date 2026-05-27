using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.Entities;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Api.Services
{
    public static class SimulationSnapshotMapper
    {
        public static SimulationSnapshotDto ToSnapshot(SimulationState state)
        {
            Dictionary<string, ActiveMovement> movementsByAgent = state.ActiveMovements
                .GroupBy(movement => movement.AgentId)
                .ToDictionary(group => group.Key, group => group.First());

            return new SimulationSnapshotDto(
                state.Time.Tick,
                ToGrid(state),
                state.World.Agents.Select(agent => ToAgent(agent, state, movementsByAgent)).ToList(),
                state.World.Foods.Select(ToFood).ToList(),
                state.ActiveMovements.Select(ToMovement).ToList(),
                state.ActiveJobs.Concat(state.PendingJobs).Select(ToJob).ToList(),
                state.Reservations.Reservations.Select(ToReservation).ToList(),
                state.ActionResults.TakeLast(10).Select(ToActionResult).ToList(),
                state.PendingIntents.Count,
                state.PendingActionRequests.Count);
        }

        public static SimulationDraftDto ToDraft(SimulationState state)
        {
            return new SimulationDraftDto(
                state.Time.Tick,
                new EditableGridDto(
                    state.World.Grid.Width,
                    state.World.Grid.Height,
                    state.World.Grid.BlockedCells.Select(ToGridCell).ToList()),
                state.World.Agents.Select(agent => new EditableAgentDto(
                    agent.Id,
                    ToPosition(agent.Position),
                    ToNeeds(state.GetAgentById(agent.Id)?.NeedState))).ToList(),
                state.World.Foods.Select(food => new EditableFoodDto(
                    food.Id,
                    ToPosition(food.Position),
                    food.IsConsumed)).ToList());
        }

        private static GridDto ToGrid(SimulationState state)
        {
            return new GridDto(
                state.World.Grid.Width,
                state.World.Grid.Height,
                state.World.Grid.BlockedCells.Select(ToGridCell).ToList());
        }

        private static AgentSnapshotDto ToAgent(
            AgentEntity agent,
            SimulationState state,
            IReadOnlyDictionary<string, ActiveMovement> movementsByAgent)
        {
            movementsByAgent.TryGetValue(agent.Id, out ActiveMovement? movement);

            return new AgentSnapshotDto(
                agent.Id,
                nameof(AgentEntity),
                ToPosition(agent.Position),
                ToGridCell(agent.Position.ToGridCell()),
                new FootprintDto(agent.Footprint.Width, agent.Footprint.Height),
                ToNeeds(state.GetAgentById(agent.Id)?.NeedState),
                movement is null ? null : ToMovement(movement));
        }

        private static FoodSnapshotDto ToFood(FoodEntity food)
        {
            return new FoodSnapshotDto(
                food.Id,
                nameof(FoodEntity),
                ToPosition(food.Position),
                ToGridCell(food.Position.ToGridCell()),
                new FootprintDto(food.Footprint.Width, food.Footprint.Height),
                food.IsConsumed);
        }

        private static MovementSnapshotDto ToMovement(ActiveMovement movement)
        {
            return new MovementSnapshotDto(
                movement.AgentId,
                movement.RequestedAction.ToString(),
                movement.TargetId,
                movement.Status.ToString(),
                movement.Route.Waypoints.Select(ToPosition).ToList(),
                ToPosition(movement.Route.Destination),
                movement.CurrentWaypointIndex,
                movement.SpeedPerTick,
                movement.ArrivalTolerance,
                movement.FailureReason);
        }

        private static JobSnapshotDto ToJob(JobInstance job)
        {
            return new JobSnapshotDto(
                job.Id,
                job.Definition.Id,
                job.Definition.Name,
                job.Status.ToString(),
                job.Tasks.Count,
                job.Tasks.Count(task => task.Status == TaskStatus.Pending),
                job.Tasks.Count(task => task.Status == TaskStatus.Assigned),
                job.Tasks.Count(task => task.Status == TaskStatus.InProgress),
                job.Tasks.Count(task => task.Status == TaskStatus.Completed),
                job.Tasks.Count(task => task.Status == TaskStatus.Failed),
                job.Tasks.Count(task => task.Status == TaskStatus.Cancelled));
        }

        private static ReservationSnapshotDto ToReservation(Reservation reservation)
        {
            return new ReservationSnapshotDto(
                reservation.Id,
                reservation.Target.Type.ToString(),
                reservation.Target.Id,
                reservation.OwnerId,
                reservation.CreatedAtTick,
                reservation.ExpiresAtTick);
        }

        private static ActionResultSnapshotDto ToActionResult(ActionResult result)
        {
            return new ActionResultSnapshotDto(
                result.AgentId,
                result.ActionType.ToString(),
                result.Succeeded,
                result.Reason);
        }

        private static AgentNeedsDto ToNeeds(AgentNeedState? needs)
        {
            return new AgentNeedsDto(
                needs?.Hunger ?? 0,
                needs?.Thirst ?? 0,
                needs?.Energy ?? 0);
        }

        private static PositionDto ToPosition(WorldPosition position)
        {
            return new PositionDto(position.X, position.Y);
        }

        private static GridCellDto ToGridCell(GridCell cell)
        {
            return new GridCellDto(cell.X, cell.Y);
        }
    }
}
