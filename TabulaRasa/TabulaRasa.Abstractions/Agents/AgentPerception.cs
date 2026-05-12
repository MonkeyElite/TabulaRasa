namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentPerception(
        IReadOnlyList<PerceivedEntity> NearbyEntities,
        IReadOnlyList<InteractionOpportunity> Opportunities)
    {
        public static AgentPerception Empty { get; } = new([], []);

        public bool HasOpportunity(AgentActionType actionType)
        {
            return Opportunities.Any(o => o.ActionType == actionType);
        }

        public InteractionOpportunity? FindOpportunity(AgentActionType actionType)
        {
            return Opportunities.FirstOrDefault(o => o.ActionType == actionType);
        }
    }
}
