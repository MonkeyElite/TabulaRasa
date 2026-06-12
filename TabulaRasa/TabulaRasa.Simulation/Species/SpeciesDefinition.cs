using TabulaRasa.World.Resources;
using TabulaRasa.Simulation.Configuration;

namespace TabulaRasa.Simulation.Species
{
    public sealed record SpeciesDefinition(
        string Id,
        string DisplayName,
        float MaxHealth,
        int AdultAgeDays,
        int MaxAgeDays,
        int ReproductionCooldownTicks,
        float PerceptionMultiplier,
        float MovementSpeedMultiplier,
        float AttackDamage,
        float HungerDecayMultiplier,
        float ThirstDecayMultiplier,
        float FatigueDecayMultiplier,
        IReadOnlySet<string> EdibleResourceIds,
        IReadOnlySet<string> PreySpeciesIds)
    {
        public bool CanEatResource(string resourceId)
        {
            return EdibleResourceIds.Contains(resourceId);
        }

        public bool CanAttackSpecies(string speciesId)
        {
            return PreySpeciesIds.Contains(speciesId);
        }
    }

    public static class SpeciesRegistry
    {
        public const string HumanId = "human";
        public const string DeerId = "deer";
        public const string WolfId = "wolf";

        private static readonly IReadOnlyDictionary<string, SpeciesDefinition> DefinitionsById =
            new Dictionary<string, SpeciesDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                [HumanId] = new SpeciesDefinition(
                    HumanId,
                    "Human",
                    MaxHealth: 10,
                    AdultAgeDays: 20,
                    MaxAgeDays: 2_000,
                    ReproductionCooldownTicks: 80,
                    PerceptionMultiplier: 1,
                    MovementSpeedMultiplier: 1,
                    AttackDamage: 2,
                    HungerDecayMultiplier: 1,
                    ThirstDecayMultiplier: 1,
                    FatigueDecayMultiplier: 1,
                    new HashSet<string>([ResourceDefinition.FoodId], StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                [DeerId] = new SpeciesDefinition(
                    DeerId,
                    "Deer",
                    MaxHealth: 6,
                    AdultAgeDays: 15,
                    MaxAgeDays: 1_200,
                    ReproductionCooldownTicks: 60,
                    PerceptionMultiplier: 1.15f,
                    MovementSpeedMultiplier: 1.25f,
                    AttackDamage: 0,
                    HungerDecayMultiplier: 1.1f,
                    ThirstDecayMultiplier: 1,
                    FatigueDecayMultiplier: 0.9f,
                    new HashSet<string>([ResourceDefinition.FoodId], StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                [WolfId] = new SpeciesDefinition(
                    WolfId,
                    "Wolf",
                    MaxHealth: 8,
                    AdultAgeDays: 18,
                    MaxAgeDays: 1_400,
                    ReproductionCooldownTicks: 90,
                    PerceptionMultiplier: 1.25f,
                    MovementSpeedMultiplier: 1.15f,
                    AttackDamage: 4,
                    HungerDecayMultiplier: 1.2f,
                    ThirstDecayMultiplier: 1.05f,
                    FatigueDecayMultiplier: 1,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>([DeerId], StringComparer.OrdinalIgnoreCase))
            };

        public static IReadOnlyList<SpeciesDefinition> All => DefinitionsById.Values.ToList();

        public static SpeciesDefinition Get(string? speciesId)
        {
            if (!string.IsNullOrWhiteSpace(speciesId)
                && DefinitionsById.TryGetValue(speciesId.Trim(), out SpeciesDefinition? definition))
            {
                return definition;
            }

            return DefinitionsById[HumanId];
        }

        public static SpeciesDefinition Get(string? speciesId, SpeciesRulesConfig rules)
        {
            SpeciesDefinition fallback = Get(speciesId);
            SpeciesRuleConfig rule = fallback.Id switch
            {
                HumanId => rules.EffectiveHuman,
                DeerId => rules.EffectiveDeer,
                WolfId => rules.EffectiveWolf,
                _ => rules.EffectiveHuman
            };

            IReadOnlySet<string> edible = rule.EdibleResourceIds is null
                ? fallback.EdibleResourceIds
                : new HashSet<string>(rule.EdibleResourceIds, StringComparer.OrdinalIgnoreCase);
            IReadOnlySet<string> prey = rule.PreySpeciesIds is null
                ? fallback.PreySpeciesIds
                : new HashSet<string>(rule.PreySpeciesIds, StringComparer.OrdinalIgnoreCase);

            return fallback with
            {
                MaxHealth = rule.MaxHealth,
                AdultAgeDays = rule.AdultAgeDays,
                MaxAgeDays = rule.MaxAgeDays,
                ReproductionCooldownTicks = rule.ReproductionCooldownTicks,
                PerceptionMultiplier = rule.PerceptionMultiplier,
                MovementSpeedMultiplier = rule.MovementSpeedMultiplier,
                AttackDamage = rule.AttackDamage,
                HungerDecayMultiplier = rule.HungerDecayMultiplier,
                ThirstDecayMultiplier = rule.ThirstDecayMultiplier,
                FatigueDecayMultiplier = rule.FatigueDecayMultiplier,
                EdibleResourceIds = edible,
                PreySpeciesIds = prey
            };
        }

        public static string NormalizeId(string? speciesId)
        {
            return Get(speciesId).Id;
        }

        public static bool IsKnown(string? speciesId)
        {
            return !string.IsNullOrWhiteSpace(speciesId)
                && DefinitionsById.ContainsKey(speciesId.Trim());
        }
    }
}
