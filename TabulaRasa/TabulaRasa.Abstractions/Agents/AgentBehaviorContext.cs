namespace TabulaRasa.Abstractions.Agents
{
    public sealed record AgentBehaviorContext(
        float EatWeight = 1,
        float DrinkWeight = 1,
        float RestWeight = 1,
        float WanderWeight = 1,
        float SocialWeight = 1,
        float ReproduceWeight = 1,
        float FleeWeight = 1,
        float AttackWeight = 1,
        float CraftWeight = 1,
        float ExperimentWeight = 1,
        float ExplorationChance = 0.10f,
        float PersonalityInfluence = 0.35f,
        long ActiveTick = 0,
        IReadOnlyDictionary<string, long>? FailedTargetCooldowns = null)
    {
        public static AgentBehaviorContext Default { get; } = new();

        public bool IsTargetOnCooldown(string? targetId, long activeTick)
        {
            return targetId is not null
                && FailedTargetCooldowns is not null
                && FailedTargetCooldowns.TryGetValue(targetId, out long expiresAtTick)
                && expiresAtTick > activeTick;
        }

        public bool IsTargetOnCooldown(string? targetId)
        {
            return IsTargetOnCooldown(targetId, ActiveTick);
        }
    }
}
