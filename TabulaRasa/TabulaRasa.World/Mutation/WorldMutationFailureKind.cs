namespace TabulaRasa.World.Mutation
{
    public enum WorldMutationFailureKind
    {
        None,
        EntityNotFound,
        DuplicateEntityId,
        OutOfBounds,
        BlockedCell,
        OccupiedCell,
        InvalidAmount,
        InvalidOperation,
        UnsupportedEntityType
    }
}
