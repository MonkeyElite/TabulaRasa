namespace TabulaRasa.Simulation.Knowledge
{
    public sealed record RecipeDefinition(
        string Id,
        string DisplayName,
        string Description,
        IReadOnlyList<RecipeIngredient> Inputs,
        IReadOnlyList<RecipeIngredient> Tools,
        IReadOnlyList<RecipeOutput> Outputs,
        IReadOnlyList<ActionUnlockDefinition> Unlocks,
        float DiscoveryChance = 0.65f);
}
