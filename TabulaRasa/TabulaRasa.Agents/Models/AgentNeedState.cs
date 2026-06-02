using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.Agents.Models
{
    public sealed class AgentNeedState
    {
        public float Hunger { get; set; }
        public float Thirst { get; set; }
        public float Energy { get; set; }
        public float Fatigue { get; set; }

        public AgentNeedsSnapshot ToSnapshot()
        {
            return new AgentNeedsSnapshot(Hunger, Thirst, Energy, Fatigue);
        }
    }
}
