namespace TabulaRasa.Simulation.Social
{
    public sealed class SocialRelationship
    {
        public required string AgentId { get; init; }
        public required string OtherAgentId { get; init; }
        public float Familiarity { get; set; }
        public float Trust { get; set; }
        public float Fear { get; set; }
        public float Affinity { get; set; }
        public int InteractionCount { get; set; }
        public long CreatedTick { get; init; }
        public long LastUpdatedTick { get; set; }
        public long? LastSeenTick { get; set; }
        public long? LastInteractionTick { get; set; }

        public void ApplyDeltas(
            float familiarity = 0,
            float trust = 0,
            float fear = 0,
            float affinity = 0,
            long? tick = null)
        {
            Familiarity = Clamp01(Familiarity + familiarity);
            Trust = Clamp01(Trust + trust);
            Fear = Clamp01(Fear + fear);
            Affinity = Clamp01(Affinity + affinity);

            if (tick is not null)
            {
                LastUpdatedTick = tick.Value;
            }
        }

        private static float Clamp01(float value)
        {
            return Math.Clamp(value, 0, 1);
        }
    }
}
