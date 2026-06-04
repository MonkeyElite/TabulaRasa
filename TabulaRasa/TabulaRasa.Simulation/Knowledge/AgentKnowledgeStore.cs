namespace TabulaRasa.Simulation.Knowledge
{
    public sealed class AgentKnowledgeStore
    {
        private readonly List<KnowledgeRecord> _records = [];

        public IReadOnlyList<KnowledgeRecord> Records => _records;

        public bool KnowsRecipe(string recipeId)
        {
            return _records.Any(record =>
                record.Kind == KnowledgeKind.Recipe
                && string.Equals(record.SubjectId, recipeId, StringComparison.OrdinalIgnoreCase));
        }

        public bool KnowsActionUnlock(string unlockId)
        {
            return _records.Any(record =>
                record.Kind == KnowledgeKind.ActionUnlock
                && string.Equals(record.SubjectId, unlockId, StringComparison.OrdinalIgnoreCase));
        }

        public KnowledgeRecord? Find(KnowledgeKind kind, string subjectId)
        {
            return _records.FirstOrDefault(record =>
                record.Kind == kind
                && string.Equals(record.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase));
        }

        public bool AddOrUpdate(KnowledgeRecord record)
        {
            KnowledgeRecord? existing = Find(record.Kind, record.SubjectId);
            if (existing is null)
            {
                _records.Add(record);
                return true;
            }

            existing.LastUpdatedTick = record.LastUpdatedTick;
            existing.Source = record.Source;
            existing.SourceAgentId = record.SourceAgentId;
            foreach ((string key, string value) in record.Metadata)
            {
                existing.Metadata[key] = value;
            }

            return false;
        }
    }
}
