namespace TabulaRasa.Simulation.Memory
{
    public sealed class AgentMemoryStore
    {
        private readonly List<AgentMemoryRecord> _memories = [];

        public IReadOnlyList<AgentMemoryRecord> Memories => _memories;

        public AgentMemoryRecord? FindById(string id)
        {
            return _memories.FirstOrDefault(memory => memory.Id == id);
        }

        public void Add(AgentMemoryRecord memory)
        {
            _memories.Add(memory);
        }

        public void Remove(AgentMemoryRecord memory)
        {
            _memories.Remove(memory);
        }

        public void TrimTo(int limit)
        {
            while (_memories.Count > limit)
            {
                AgentMemoryRecord weakest = _memories
                    .OrderBy(memory => memory.Strength)
                    .ThenBy(memory => memory.LastUpdatedTick)
                    .First();
                _memories.Remove(weakest);
            }
        }
    }
}
