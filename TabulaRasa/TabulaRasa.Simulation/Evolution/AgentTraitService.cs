using System.Globalization;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Simulation.Configuration;

namespace TabulaRasa.Simulation.Evolution
{
    public static class AgentTraitService
    {
        public static AgentTraits CreateInitialTraits(Random random, TraitConfig config)
        {
            return new AgentTraits(
                Vary(random, config.InitialVariation),
                Vary(random, config.InitialVariation),
                Vary(random, config.InitialVariation),
                Vary(random, config.InitialVariation),
                Vary(random, config.InitialVariation));
        }

        public static AgentTraits Inherit(
            AgentTraits first,
            AgentTraits second,
            TraitConfig config,
            Random random,
            out IReadOnlyList<string> mutatedTraits)
        {
            List<string> mutations = [];
            AgentTraits traits = new(
                Mutate("perception", Average(first.Perception, second.Perception), config, random, mutations),
                Mutate("speed", Average(first.Speed, second.Speed), config, random, mutations),
                Mutate("metabolism", Average(first.Metabolism, second.Metabolism), config, random, mutations),
                Mutate("riskTolerance", Average(first.RiskTolerance, second.RiskTolerance), config, random, mutations),
                Mutate("learningRate", Average(first.LearningRate, second.LearningRate), config, random, mutations));

            mutatedTraits = mutations;
            return traits;
        }

        public static float TraitMultiplier(float trait)
        {
            return 0.75f + (ClampTrait(trait) * 0.5f);
        }

        public static float MetabolismMultiplier(float trait)
        {
            return 1.2f - (ClampTrait(trait) * 0.4f);
        }

        public static float LearningRate(float trait)
        {
            return 0.1f + (ClampTrait(trait) * 0.3f);
        }

        public static float RiskAdjustment(float trait)
        {
            return (ClampTrait(trait) - 0.5f) * 0.7f;
        }

        public static IReadOnlyDictionary<string, string> ToMetadata(AgentTraits traits, IReadOnlyList<string>? mutatedTraits = null)
        {
            Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase)
            {
                ["traits.perception"] = Format(traits.Perception),
                ["traits.speed"] = Format(traits.Speed),
                ["traits.metabolism"] = Format(traits.Metabolism),
                ["traits.riskTolerance"] = Format(traits.RiskTolerance),
                ["traits.learningRate"] = Format(traits.LearningRate)
            };

            if (mutatedTraits is not null && mutatedTraits.Count > 0)
            {
                metadata["traits.mutated"] = string.Join(",", mutatedTraits);
            }

            return metadata;
        }

        private static float Vary(Random random, float variation)
        {
            return ClampTrait(0.5f + (((float)random.NextDouble() * 2f - 1f) * Math.Clamp(variation, 0, 1)));
        }

        private static float Mutate(
            string traitName,
            float value,
            TraitConfig config,
            Random random,
            List<string> mutatedTraits)
        {
            if (random.NextDouble() > Math.Clamp(config.MutationChancePerTrait, 0, 1))
            {
                return ClampTrait(value);
            }

            mutatedTraits.Add(traitName);
            float delta = ((float)random.NextDouble() * 2f - 1f) * Math.Clamp(config.MutationDelta, 0, 1);

            return ClampTrait(value + delta);
        }

        private static float Average(float first, float second)
        {
            return (ClampTrait(first) + ClampTrait(second)) / 2f;
        }

        private static float ClampTrait(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0.5f
                : Math.Clamp(value, 0, 1);
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
