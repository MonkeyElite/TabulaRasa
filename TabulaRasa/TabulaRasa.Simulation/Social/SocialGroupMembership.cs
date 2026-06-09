namespace TabulaRasa.Simulation.Social
{
    public sealed class SocialGroupMembership
    {
        public required string GroupId { get; init; }
        public required string DisplayName { get; init; }
        public required string Kind { get; init; }
        public long JoinedTick { get; init; }
    }
}
