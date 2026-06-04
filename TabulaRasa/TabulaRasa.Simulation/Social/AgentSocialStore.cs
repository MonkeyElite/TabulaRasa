namespace TabulaRasa.Simulation.Social
{
    public sealed class AgentSocialStore
    {
        private readonly Dictionary<string, SocialRelationship> _relationshipsByAgentId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SocialGroupMembership> _groupsById = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<SocialRelationship> Relationships => _relationshipsByAgentId.Values
            .OrderByDescending(relationship => relationship.Familiarity)
            .ThenByDescending(relationship => relationship.LastUpdatedTick)
            .ThenBy(relationship => relationship.OtherAgentId, StringComparer.Ordinal)
            .ToList();

        public IReadOnlyList<SocialGroupMembership> Groups => _groupsById.Values
            .OrderBy(group => group.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.GroupId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        public SocialRelationship GetOrCreateRelationship(string agentId, string otherAgentId, long tick)
        {
            if (!_relationshipsByAgentId.TryGetValue(otherAgentId, out SocialRelationship? relationship))
            {
                relationship = new SocialRelationship
                {
                    AgentId = agentId,
                    OtherAgentId = otherAgentId,
                    CreatedTick = tick,
                    LastUpdatedTick = tick
                };
                _relationshipsByAgentId[otherAgentId] = relationship;
            }

            return relationship;
        }

        public bool TryAddGroup(SocialGroupMembership membership)
        {
            return _groupsById.TryAdd(membership.GroupId, membership);
        }

        public bool HasGroup(string groupId)
        {
            return _groupsById.ContainsKey(groupId);
        }
    }
}
