namespace TabulaRasa.Abstractions.Agents.Actions
{
    public sealed record ActionCommand(string AgentId, AgentActionType ActionType, string? TargetId);
}
