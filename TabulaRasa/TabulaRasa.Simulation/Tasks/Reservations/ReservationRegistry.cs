namespace TabulaRasa.Simulation.Tasks.Reservations
{
    public sealed class ReservationRegistry
    {
        private readonly List<Reservation> _reservations = [];

        public IReadOnlyList<Reservation> Reservations => _reservations;

        public bool TryReserve(
            ReservationTarget target,
            string ownerId,
            long currentTick,
            long? expiresAtTick = null)
        {
            if (IsReservedByAnotherOwner(target, ownerId, currentTick))
            {
                return false;
            }

            if (_reservations.Any(r => r.Target == target && r.OwnerId == ownerId))
            {
                return true;
            }

            _reservations.Add(new Reservation(
                $"{target.Type}:{target.Id}:{ownerId}",
                target,
                ownerId,
                currentTick,
                expiresAtTick));

            return true;
        }

        public bool IsReserved(ReservationTarget target, long currentTick)
        {
            ReleaseExpired(currentTick);
            return _reservations.Any(r => r.Target == target);
        }

        public bool IsReservedByAnotherOwner(ReservationTarget target, string ownerId, long currentTick)
        {
            ReleaseExpired(currentTick);
            return _reservations.Any(r => r.Target == target && r.OwnerId != ownerId);
        }

        public void ReleaseByOwner(string ownerId)
        {
            _reservations.RemoveAll(r => r.OwnerId == ownerId);
        }

        public void Release(ReservationTarget target, string ownerId)
        {
            _reservations.RemoveAll(r => r.Target == target && r.OwnerId == ownerId);
        }

        public void ReleaseExpired(long currentTick)
        {
            _reservations.RemoveAll(r => r.ExpiresAtTick is not null && r.ExpiresAtTick <= currentTick);
        }
    }
}
