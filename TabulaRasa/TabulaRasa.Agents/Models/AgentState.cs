using System.Diagnostics.CodeAnalysis;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;

namespace TabulaRasa.Agents.Models
{
    public sealed class AgentState
    {
        public required string Id { get; init; }
        public required AgentNeedState NeedState { get; init; }
        public required IAgentMind Mind { get; init; } = default!;
        public ActionRequest? PendingDecision { get; set; }

        [SetsRequiredMembers]
        public AgentState(string AgentId, AgentNeedState needState, IAgentMind mind)
        {
            Id = AgentId;
            NeedState = needState;
            Mind = mind;
        }
    }
}
