using System.Globalization;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Learning
{
    public static class AgentLearningService
    {
        public static ActionResult RecordActionResult(
            SimulationState state,
            ActionResult result,
            string sourceSystem)
        {
            AgentNeedsSnapshot? needsAfter = state.GetAgentById(result.AgentId)?.NeedState.ToSnapshot();
            float outcomeScore = result.OutcomeScore ?? ScoreOutcome(result, needsAfter);
            ActionResult scored = result with
            {
                NeedsAfter = needsAfter,
                OutcomeScore = outcomeScore
            };

            state.ActionResults.Add(scored);
            ApplyLearning(state, scored);
            AgentMemoryService.RememberActionOutcome(state, scored);
            EmitActionResultEvent(state, scored, sourceSystem);

            return scored;
        }

        private static void ApplyLearning(SimulationState state, ActionResult result)
        {
            if (result.ContextKey is null || result.OutcomeScore is null)
            {
                return;
            }

            AgentState? agent = state.GetAgentById(result.AgentId);
            if (agent is null)
            {
                return;
            }

            agent.Learning
                .GetOrCreate(result.ContextKey, result.ActionType)
                .ApplyOutcome(result.OutcomeScore.Value, result.Succeeded, DefaultAgentMind.DefaultLearningRate);
        }

        private static float ScoreOutcome(ActionResult result, AgentNeedsSnapshot? needsAfter)
        {
            if (!result.Succeeded)
            {
                return -1;
            }

            if (result.ActionType == AgentActionType.Wander)
            {
                return 0.05f;
            }

            if (result.ActionType == AgentActionType.Communicate)
            {
                return 0.15f;
            }

            if (result.NeedsBefore is null || needsAfter is null)
            {
                return 0;
            }

            float improvement = result.ActionType switch
            {
                AgentActionType.Eat => result.NeedsBefore.Hunger - needsAfter.Hunger,
                AgentActionType.Drink => result.NeedsBefore.Thirst - needsAfter.Thirst,
                AgentActionType.Rest => Math.Max(
                    needsAfter.Energy - result.NeedsBefore.Energy,
                    result.NeedsBefore.Fatigue - needsAfter.Fatigue),
                _ => 0
            };

            return Math.Clamp(improvement / 5, 0, 1);
        }

        private static void EmitActionResultEvent(
            SimulationState state,
            ActionResult result,
            string sourceSystem)
        {
            string outcome = result.Succeeded ? "succeeded" : $"failed: {result.Reason ?? "unknown"}";
            Dictionary<string, string> metadata = new()
            {
                ["actionType"] = result.ActionType.ToString(),
                ["succeeded"] = result.Succeeded.ToString(),
                ["targetId"] = result.TargetId ?? "",
                ["contextKey"] = result.ContextKey ?? "",
                ["selectedGoal"] = result.SelectedGoal ?? "",
                ["sourceTaskId"] = result.SourceTaskId ?? "",
                ["sourceGoalId"] = result.SourceGoalId ?? "",
                ["outcomeScore"] = result.OutcomeScore?.ToString("0.###", CultureInfo.InvariantCulture) ?? ""
            };

            if (result.Reason is not null)
            {
                metadata["reason"] = result.Reason;
            }

            state.EmitEvent(
                "action.result",
                sourceSystem,
                $"{result.AgentId} {result.ActionType} {outcome}.",
                result.AgentId,
                metadata);
        }
    }
}
