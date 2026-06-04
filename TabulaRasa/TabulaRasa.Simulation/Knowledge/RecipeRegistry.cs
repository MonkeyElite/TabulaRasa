using TabulaRasa.World.Resources;

namespace TabulaRasa.Simulation.Knowledge
{
    public static class RecipeRegistry
    {
        public static readonly IReadOnlyList<RecipeDefinition> All =
        [
            new RecipeDefinition(
                "stone-knapping",
                "Stone Knapping",
                "Shape stone into a basic cutting and striking tool.",
                Inputs:
                [
                    new RecipeIngredient(ResourceDefinition.StoneId, 2)
                ],
                Tools: [],
                Outputs:
                [
                    new RecipeOutput(ResourceDefinition.StoneToolId, 1)
                ],
                Unlocks:
                [
                    new ActionUnlockDefinition("use-stone-tool", "Use Stone Tool", "Use a shaped stone as a general-purpose tool.")
                ],
                DiscoveryChance: 0.65f),
            new RecipeDefinition(
                "wood-shaping",
                "Wood Shaping",
                "Use a stone tool to shape wood into a reusable tool.",
                Inputs:
                [
                    new RecipeIngredient(ResourceDefinition.WoodId, 1)
                ],
                Tools:
                [
                    new RecipeIngredient(ResourceDefinition.StoneToolId, 1)
                ],
                Outputs:
                [
                    new RecipeOutput(ResourceDefinition.WoodenToolId, 1)
                ],
                Unlocks:
                [
                    new ActionUnlockDefinition("tool-crafting", "Tool Crafting", "Plan and make simple tools.")
                ],
                DiscoveryChance: 0.55f)
        ];

        private static readonly IReadOnlyDictionary<string, RecipeDefinition> ById =
            All.ToDictionary(recipe => recipe.Id, StringComparer.OrdinalIgnoreCase);

        public static RecipeDefinition? Find(string? recipeId)
        {
            return recipeId is not null && ById.TryGetValue(recipeId, out RecipeDefinition? recipe)
                ? recipe
                : null;
        }

        public static IReadOnlyList<RecipeDefinition> FindExperimentCandidates(
            IReadOnlyDictionary<string, int> availableResources,
            AgentKnowledgeStore knowledge)
        {
            return All
                .Where(recipe => !knowledge.KnowsRecipe(recipe.Id))
                .Where(recipe => HasAnyRelevantResource(recipe, availableResources))
                .OrderBy(recipe => recipe.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<RecipeDefinition> FindCraftableRecipes(
            IReadOnlyDictionary<string, int> availableResources,
            AgentKnowledgeStore knowledge)
        {
            return All
                .Where(recipe => knowledge.KnowsRecipe(recipe.Id))
                .Where(recipe => HasRequirements(recipe, availableResources))
                .OrderBy(recipe => recipe.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool HasRequirements(
            RecipeDefinition recipe,
            IReadOnlyDictionary<string, int> availableResources)
        {
            return recipe.Inputs.Concat(recipe.Tools)
                .All(ingredient => availableResources.GetValueOrDefault(ingredient.ResourceId) >= ingredient.Quantity);
        }

        private static bool HasAnyRelevantResource(
            RecipeDefinition recipe,
            IReadOnlyDictionary<string, int> availableResources)
        {
            return recipe.Inputs.Concat(recipe.Tools)
                .Any(ingredient => availableResources.GetValueOrDefault(ingredient.ResourceId) > 0);
        }
    }
}
