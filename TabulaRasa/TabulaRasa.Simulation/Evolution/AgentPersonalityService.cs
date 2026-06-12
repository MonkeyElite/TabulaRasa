using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.Simulation.Evolution
{
    public sealed record AgentPersonality(
        string Label,
        string DominantTrait,
        IReadOnlyDictionary<string, float> BehaviorBiases,
        float ExplorationChance);

    public static class AgentPersonalityService
    {
        public static AgentPersonality Derive(
            AgentTraits traits,
            float baseExplorationChance = 0.10f,
            float personalityInfluence = 0.35f)
        {
            Dictionary<string, float> values = new(StringComparer.OrdinalIgnoreCase)
            {
                ["perception"] = Clamp(traits.Perception),
                ["speed"] = Clamp(traits.Speed),
                ["metabolism"] = Clamp(traits.Metabolism),
                ["riskTolerance"] = Clamp(traits.RiskTolerance),
                ["learningRate"] = Clamp(traits.LearningRate)
            };
            KeyValuePair<string, float> dominant = values
                .OrderByDescending(pair => Math.Abs(pair.Value - 0.5f))
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .First();

            float influence = Clamp(personalityInfluence);
            Dictionary<string, float> biases = new(StringComparer.OrdinalIgnoreCase)
            {
                ["eat"] = BiasFromLow(values["metabolism"], influence),
                ["drink"] = BiasFromLow(values["metabolism"], influence * 0.75f),
                ["rest"] = BiasFromLow(values["speed"], influence),
                ["wander"] = BiasFromHigh(values["speed"], influence),
                ["social"] = BiasFromHigh(values["learningRate"], influence),
                ["reproduce"] = BiasFromHigh(values["riskTolerance"], influence * 0.5f),
                ["flee"] = BiasFromLow(values["riskTolerance"], influence),
                ["attack"] = BiasFromHigh(values["riskTolerance"], influence),
                ["craft"] = BiasFromHigh(values["learningRate"], influence),
                ["experiment"] = BiasFromHigh(values["learningRate"], influence)
            };

            return new AgentPersonality(
                LabelFor(dominant.Key, dominant.Value),
                dominant.Key,
                biases,
                Math.Clamp(baseExplorationChance + ((values["learningRate"] - 0.5f) * influence), 0, 1));
        }

        private static string LabelFor(string trait, float value)
        {
            bool high = value >= 0.5f;
            return trait switch
            {
                "perception" => high ? "Observant" : "Inattentive",
                "speed" => high ? "Restless" : "Deliberate",
                "metabolism" => high ? "Efficient" : "Needy",
                "riskTolerance" => high ? "Bold" : "Cautious",
                "learningRate" => high ? "Curious" : "Habitual",
                _ => "Balanced"
            };
        }

        private static float BiasFromHigh(float value, float influence)
        {
            return 1 + ((Clamp(value) - 0.5f) * 2 * influence);
        }

        private static float BiasFromLow(float value, float influence)
        {
            return 1 + ((0.5f - Clamp(value)) * 2 * influence);
        }

        private static float Clamp(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0.5f : Math.Clamp(value, 0, 1);
        }
    }
}
