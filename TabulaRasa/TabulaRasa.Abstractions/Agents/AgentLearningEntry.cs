namespace TabulaRasa.Abstractions.Agents
{
    public sealed class AgentLearningEntry
    {
        public required string ContextKey { get; init; }
        public required AgentActionType ActionType { get; init; }
        public int Attempts { get; private set; }
        public int Successes { get; private set; }
        public int Failures { get; private set; }
        public float LastOutcomeScore { get; private set; }
        public float AverageOutcomeScore { get; private set; }
        public float LearnedWeight { get; private set; }

        public void ApplyOutcome(float outcomeScore, bool succeeded, float learningRate)
        {
            float clampedScore = Math.Clamp(outcomeScore, -1, 1);
            Attempts++;
            if (succeeded)
            {
                Successes++;
            }
            else
            {
                Failures++;
            }

            LastOutcomeScore = clampedScore;
            AverageOutcomeScore += (clampedScore - AverageOutcomeScore) / Attempts;
            LearnedWeight = Math.Clamp(
                LearnedWeight + ((clampedScore - LearnedWeight) * learningRate),
                -1,
                1);
        }
    }
}
