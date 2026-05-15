using TabulaRasa.Simulation.Tasks.Reservations;

namespace TabulaRasa.Simulation.Tasks.Definitions
{
    public sealed record TaskRequirement(
        ReservationTargetType TargetType,
        string TargetId,
        bool RequiresReservation = true)
    {
        public ReservationTarget ToReservationTarget()
        {
            return new ReservationTarget(TargetType, TargetId);
        }
    }
}
