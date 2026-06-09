using TabulaRasa.Abstractions.World;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Knowledge
{
    public static class KnowledgeService
    {
        private const string SourceSystem = "Knowledge System";

        public static bool DiscoverRecipe(
            SimulationState state,
            string agentId,
            RecipeDefinition recipe,
            string source,
            string? sourceAgentId = null)
        {
            AgentKnowledgeStore store = state.GetKnowledgeStore(agentId);
            KnowledgeRecord recipeRecord = new()
            {
                Id = $"knowledge:{KnowledgeKind.Recipe}:{recipe.Id}",
                Kind = KnowledgeKind.Recipe,
                SubjectId = recipe.Id,
                DisplayName = recipe.DisplayName,
                DiscoveredTick = state.ActiveTick,
                LastUpdatedTick = state.ActiveTick,
                Source = source,
                SourceAgentId = sourceAgentId
            };
            recipeRecord.Metadata["description"] = recipe.Description;
            bool added = store.AddOrUpdate(recipeRecord);

            foreach (ActionUnlockDefinition unlock in recipe.Unlocks)
            {
                KnowledgeRecord unlockRecord = new()
                {
                    Id = $"knowledge:{KnowledgeKind.ActionUnlock}:{unlock.Id}",
                    Kind = KnowledgeKind.ActionUnlock,
                    SubjectId = unlock.Id,
                    DisplayName = unlock.DisplayName,
                    DiscoveredTick = state.ActiveTick,
                    LastUpdatedTick = state.ActiveTick,
                    Source = source,
                    SourceAgentId = sourceAgentId
                };
                unlockRecord.Metadata["description"] = unlock.Description;
                unlockRecord.Metadata["recipeId"] = recipe.Id;
                store.AddOrUpdate(unlockRecord);
            }

            if (added)
            {
                RememberKnowledge(state, agentId, recipe, source, sourceAgentId);
                EmitKnowledgeEvent(
                    state,
                    source == "Taught" ? "knowledge.transferred" : "knowledge.discovered",
                    agentId,
                    recipe,
                    source,
                    sourceAgentId);
            }

            return added;
        }

        private static void RememberKnowledge(
            SimulationState state,
            string agentId,
            RecipeDefinition recipe,
            string source,
            string? sourceAgentId)
        {
            if (!state.Config.EffectiveMemory.Enabled)
            {
                return;
            }

            AgentMemoryStore store = state.GetMemoryStore(agentId);
            AgentMemoryRecord memory = new()
            {
                Id = $"knowledge:{state.ActiveTick}:{recipe.Id}",
                Kind = AgentMemoryKind.Knowledge,
                SubjectId = recipe.Id,
                SubjectType = "Recipe",
                Position = state.World.Agents.FirstOrDefault(agent => agent.Id == agentId)?.Position ?? new WorldPosition(0, 0),
                CreatedTick = state.ActiveTick,
                LastUpdatedTick = state.ActiveTick,
                Strength = 1,
                Certainty = source == "Taught" ? 0.85f : 1,
                ExpiresAtTick = state.ActiveTick + state.Config.EffectiveMemory.RetentionTicks,
                Summary = $"{source} recipe: {recipe.DisplayName}."
            };
            memory.Metadata["recipeId"] = recipe.Id;
            memory.Metadata["source"] = source;
            memory.Metadata["sourceAgentId"] = sourceAgentId ?? "";
            store.Add(memory);
            store.TrimTo(state.Config.EffectiveMemory.MaxMemoriesPerAgent);
        }

        private static void EmitKnowledgeEvent(
            SimulationState state,
            string eventType,
            string agentId,
            RecipeDefinition recipe,
            string source,
            string? sourceAgentId)
        {
            state.EmitEvent(
                eventType,
                SourceSystem,
                $"{agentId} learned {recipe.DisplayName}.",
                agentId,
                new Dictionary<string, string>
                {
                    ["agentId"] = agentId,
                    ["recipeId"] = recipe.Id,
                    ["displayName"] = recipe.DisplayName,
                    ["source"] = source,
                    ["sourceAgentId"] = sourceAgentId ?? "",
                    ["unlockIds"] = string.Join(",", recipe.Unlocks.Select(unlock => unlock.Id))
                });
        }
    }
}
