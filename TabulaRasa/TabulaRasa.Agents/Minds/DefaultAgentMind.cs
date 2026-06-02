using TabulaRasa.Abstractions.Agents;

namespace TabulaRasa.Agents.Minds
{
    public class DefaultAgentMind : IAgentMind
    {
        public const float DefaultLearningRate = 0.25f;
        public const float DefaultExplorationChance = 0.10f;

        private readonly float _explorationChance;

        public DefaultAgentMind(float explorationChance = DefaultExplorationChance)
        {
            _explorationChance = Math.Clamp(explorationChance, 0, 1);
        }

        public AgentIntent Decide(AgentPerception perception, AgentSnapshot self)
        {
            return Decide(perception, self, new AgentLearningProfile(), new Random(0));
        }

        public AgentIntent Decide(
            AgentPerception perception,
            AgentSnapshot self,
            AgentLearningProfile learning,
            Random random)
        {
            Dictionary<string, float> needPressures = BuildNeedPressures(self.Needs);
            List<DecisionCandidate> candidates = BuildCandidates(perception, self, learning, needPressures);
            DecisionCandidate selected = SelectCandidate(candidates, learning, random, out bool explored);

            learning.LatestDecision = new AgentDecisionExplanation(
                needPressures,
                candidates
                    .OrderByDescending(candidate => candidate.Score)
                    .Select(candidate => new AgentActionScore(
                        candidate.ActionType,
                        candidate.TargetId,
                        candidate.SelectedGoal,
                        candidate.ContextKey,
                        candidate.TargetType,
                        candidate.Channel,
                        candidate.NeedPressure,
                        candidate.OpportunityRelevance,
                        candidate.LearnedWeight,
                        candidate.Score))
                    .ToList(),
                selected.SelectedGoal,
                selected.ActionType,
                selected.TargetId,
                selected.ContextKey,
                explored);

            return new AgentIntent(
                self.AgentId,
                selected.ActionType,
                selected.TargetId,
                selected.ContextKey,
                selected.SelectedGoal,
                selected.TargetType,
                selected.Channel,
                self.Needs);
        }

        private static Dictionary<string, float> BuildNeedPressures(AgentNeedsSnapshot needs)
        {
            return new Dictionary<string, float>
            {
                ["Hunger"] = ClampPressure(needs.Hunger / 10),
                ["Thirst"] = ClampPressure(needs.Thirst / 10),
                ["Fatigue"] = ClampPressure(needs.Fatigue / 10),
                ["LowEnergy"] = ClampPressure((10 - needs.Energy) / 10)
            };
        }

        private static List<DecisionCandidate> BuildCandidates(
            AgentPerception perception,
            AgentSnapshot self,
            AgentLearningProfile learning,
            IReadOnlyDictionary<string, float> needPressures)
        {
            List<DecisionCandidate> candidates = [];

            foreach (InteractionOpportunity opportunity in perception.Opportunities
                .Where(opportunity => opportunity.ActionType == AgentActionType.Eat))
            {
                string channel = opportunity.Channel.ToString();
                string contextKey = BuildContextKey("Hunger", "Food", channel);
                float learnedWeight = learning.GetWeight(contextKey, AgentActionType.Eat);
                float relevance = Math.Clamp(opportunity.Relevance, 0, 1);
                float needPressure = needPressures["Hunger"];

                candidates.Add(new DecisionCandidate(
                    AgentActionType.Eat,
                    opportunity.TargetId,
                    "Hunger",
                    contextKey,
                    "Food",
                    channel,
                    needPressure,
                    relevance,
                    learnedWeight,
                    (needPressure * 1.25f) + (relevance * 0.25f) + learnedWeight));
            }

            if ((self.Inventory?.GetValueOrDefault("food") ?? 0) > 0)
            {
                string contextKey = BuildContextKey("Hunger", "Food", "Inventory");
                float learnedWeight = learning.GetWeight(contextKey, AgentActionType.Eat);
                float needPressure = needPressures["Hunger"];
                candidates.Add(new DecisionCandidate(
                    AgentActionType.Eat,
                    null,
                    "Hunger",
                    contextKey,
                    "Food",
                    "Inventory",
                    needPressure,
                    1,
                    learnedWeight,
                    (needPressure * 1.30f) + learnedWeight));
            }

            AddSelfCandidate(candidates, learning, needPressures["Thirst"], AgentActionType.Drink, "Thirst");

            string restGoal = needPressures["LowEnergy"] > needPressures["Fatigue"] ? "LowEnergy" : "Fatigue";
            AddSelfCandidate(candidates, learning, Math.Max(needPressures["Fatigue"], needPressures["LowEnergy"]), AgentActionType.Rest, restGoal);

            string dominantNeed = needPressures
                .OrderByDescending(pressure => pressure.Value)
                .ThenBy(pressure => pressure.Key, StringComparer.Ordinal)
                .First().Key;
            string wanderContext = BuildContextKey(dominantNeed, "World", "Internal");
            float wanderWeight = learning.GetWeight(wanderContext, AgentActionType.Wander);
            float highestPressure = needPressures.Values.Max();
            candidates.Add(new DecisionCandidate(
                AgentActionType.Wander,
                null,
                dominantNeed,
                wanderContext,
                "World",
                "Internal",
                highestPressure,
                0,
                wanderWeight,
                0.10f + ((1 - highestPressure) * 0.20f) + wanderWeight));

            return candidates;
        }

        private static void AddSelfCandidate(
            List<DecisionCandidate> candidates,
            AgentLearningProfile learning,
            float needPressure,
            AgentActionType actionType,
            string selectedGoal)
        {
            string contextKey = BuildContextKey(selectedGoal, "Self", "Internal");
            float learnedWeight = learning.GetWeight(contextKey, actionType);
            candidates.Add(new DecisionCandidate(
                actionType,
                null,
                selectedGoal,
                contextKey,
                "Self",
                "Internal",
                needPressure,
                0,
                learnedWeight,
                (needPressure * 1.15f) + learnedWeight));
        }

        private DecisionCandidate SelectCandidate(
            IReadOnlyList<DecisionCandidate> candidates,
            AgentLearningProfile learning,
            Random random,
            out bool explored)
        {
            DecisionCandidate best = candidates
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.ActionType)
                .First();

            bool canExplore = learning.Entries.Count > 0 && candidates.Count > 1;
            if (!canExplore || random.NextDouble() >= _explorationChance)
            {
                explored = false;
                return best;
            }

            IReadOnlyList<DecisionCandidate> alternatives = candidates
                .Where(candidate => candidate != best)
                .ToList();
            explored = alternatives.Count > 0;

            return explored
                ? alternatives[random.Next(alternatives.Count)]
                : best;
        }

        private static string BuildContextKey(string dominantNeed, string targetType, string channel)
        {
            return $"{dominantNeed}|{targetType}|{channel}";
        }

        private static float ClampPressure(float value)
        {
            return Math.Clamp(value, 0, 1);
        }

        private sealed record DecisionCandidate(
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
}
