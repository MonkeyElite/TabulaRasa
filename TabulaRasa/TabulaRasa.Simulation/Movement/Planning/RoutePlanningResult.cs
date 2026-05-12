using TabulaRasa.Simulation.Movement.Execution;

namespace TabulaRasa.Simulation.Movement.Planning
{
    public sealed record RoutePlanningResult(bool Succeeded, bool RequiresMovement, ActiveMovement? Movement, string? FailureReason)
    {
        public static RoutePlanningResult Success(ActiveMovement movement)
        {
            return new RoutePlanningResult(true, true, movement, null);
        }

        public static RoutePlanningResult NotNeeded()
        {
            return new RoutePlanningResult(true, false, null, null);
        }

        public static RoutePlanningResult Failure(string reason)
        {
            return new RoutePlanningResult(false, false, null, reason);
        }
    }
}
