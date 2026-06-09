namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentTraits(
        float Perception = 0.5f,
        float Speed = 0.5f,
        float Metabolism = 0.5f,
        float RiskTolerance = 0.5f,
        float LearningRate = 0.5f)
    {
        public float Perception { get; init; } = ClampTrait(Perception);
        public float Speed { get; init; } = ClampTrait(Speed);
        public float Metabolism { get; init; } = ClampTrait(Metabolism);
        public float RiskTolerance { get; init; } = ClampTrait(RiskTolerance);
        public float LearningRate { get; init; } = ClampTrait(LearningRate);

        public static AgentTraits Default { get; } = new();

        private static float ClampTrait(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0.5f
                : Math.Clamp(value, 0, 1);
        }
    }
}
