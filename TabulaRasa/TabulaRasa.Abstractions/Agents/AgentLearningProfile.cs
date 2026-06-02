namespace TabulaRasa.Abstractions.Agents
{
    public sealed class AgentLearningProfile
    {
        private readonly Dictionary<string, AgentLearningEntry> _entries = [];

        public IReadOnlyList<AgentLearningEntry> Entries => _entries.Values
            .OrderBy(entry => entry.ContextKey, StringComparer.Ordinal)
            .ThenBy(entry => entry.ActionType)
            .ToList();

        public AgentDecisionExplanation? LatestDecision { get; set; }

        public AgentLearningEntry GetOrCreate(string contextKey, AgentActionType actionType)
        {
            string key = BuildEntryKey(contextKey, actionType);
            if (!_entries.TryGetValue(key, out AgentLearningEntry? entry))
            {
                entry = new AgentLearningEntry
                {
                    ContextKey = contextKey,
                    ActionType = actionType
                };
                _entries[key] = entry;
            }

            return entry;
        }

        public float GetWeight(string contextKey, AgentActionType actionType)
        {
            return _entries.TryGetValue(BuildEntryKey(contextKey, actionType), out AgentLearningEntry? entry)
                ? entry.LearnedWeight
                : 0;
        }

        private static string BuildEntryKey(string contextKey, AgentActionType actionType)
        {
            return $"{contextKey}|{actionType}";
        }
    }
}
