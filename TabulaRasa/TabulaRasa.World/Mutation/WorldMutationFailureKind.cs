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
        ResourceNotFound,
        CapacityExceeded,
        InvalidOperation,
        UnsupportedEntityType
    }
}
