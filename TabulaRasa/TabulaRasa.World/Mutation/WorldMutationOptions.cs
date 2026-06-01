namespace TabulaRasa.World.Mutation
{
    public sealed record WorldMutationOptions(
        bool AllowBlockedCells = false,
        bool AllowOccupiedCells = false);
}
