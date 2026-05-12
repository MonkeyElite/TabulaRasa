using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Simulation.Movement.Planning
{
    public sealed record MovementRoute
    {
        public MovementRoute(IReadOnlyList<WorldPosition> waypoints)
        {
            if (waypoints.Count == 0)
            {
                throw new ArgumentException("Route must contain at least one waypoint.", nameof(waypoints));
            }

            Waypoints = waypoints;
        }

        public IReadOnlyList<WorldPosition> Waypoints { get; }
        public WorldPosition Destination => Waypoints[^1];
    }
}
