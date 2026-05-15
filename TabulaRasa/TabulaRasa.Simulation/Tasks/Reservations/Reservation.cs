namespace TabulaRasa.Simulation.Tasks.Reservations
{
    public sealed record Reservation(
        string Id,
        ReservationTarget Target,
        string OwnerId,
        long CreatedAtTick,
        long? ExpiresAtTick = null);
}
