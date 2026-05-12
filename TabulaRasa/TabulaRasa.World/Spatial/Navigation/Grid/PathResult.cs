namespace TabulaRasa.World.Spatial.Navigation.Grid
{
    public sealed record PathResult(bool Succeeded, GridPath? Path, string? FailureReason)
    {
        public static PathResult Success(GridPath path)
        {
            return new PathResult(true, path, null);
        }

        public static PathResult Failure(string reason)
        {
            return new PathResult(false, null, reason);
        }
    }
}
