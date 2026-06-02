namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentDecisionExplanation(
        IReadOnlyDictionary<string, float> NeedPressures,
        IReadOnlyList<AgentActionScore> ActionScores,
        string SelectedGoal,
        AgentActionType SelectedAction,
        string? TargetId,
        string ContextKey,
        bool Explored);

    public sealed record AgentActionScore(
        AgentActionType ActionType,
        string? TargetId,
        string SelectedGoal,
        string ContextKey,
        string TargetType,
        string Channel,
        float NeedPressure,
        float OpportunityRelevance,
        float LearnedWeight,
        float Score);
}
