namespace TabulaRasa.Abstractions.Agents.Actions
{
    public sealed record ActionResult(string AgentId, AgentActionType ActionType, bool Succeeded, string? Reason = null);
}
