using System.Diagnostics.CodeAnalysis;
using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.Agents.Models
{
    public sealed class AgentState
    {
        public required string Id { get; init; }
        public required AgentNeedState NeedState { get; init; }
        public required IAgentMind Mind { get; init; } = default!;
        public AgentLearningProfile Learning { get; } = new();

        [SetsRequiredMembers]
        public AgentState(string agentId, AgentNeedState needState, IAgentMind mind)
        {
            Id = agentId;
            NeedState = needState;
            Mind = mind;
        }
    }
}
