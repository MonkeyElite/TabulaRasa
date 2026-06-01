namespace TabulaRasa.World.Mutation
{
    public sealed record WorldMutationResult(
        bool Succeeded,
        WorldMutationFailureKind FailureKind,
        string? Reason)
    {
        public static WorldMutationResult Success()
        {
            return new WorldMutationResult(true, WorldMutationFailureKind.None, null);
        }

        public static WorldMutationResult Failure(
            WorldMutationFailureKind failureKind,
            string reason)
        {
            return new WorldMutationResult(false, failureKind, reason);
        }
    }
}
