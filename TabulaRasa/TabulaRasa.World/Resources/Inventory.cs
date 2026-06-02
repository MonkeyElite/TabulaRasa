namespace TabulaRasa.World.Resources
{
    public sealed class Inventory
    {
        public int MaxSlots { get; set; } = 8;
        public float MaxWeight { get; set; } = 10;
        public List<ResourceStack> Stacks { get; } = [];

        public int UsedSlots => Stacks.Count;

        public float GetUsedWeight(IReadOnlyDictionary<string, ResourceDefinition> definitions)
        {
            return Stacks.Sum(stack =>
                definitions.TryGetValue(stack.ResourceId, out ResourceDefinition? definition)
                    ? stack.Quantity * definition.UnitWeight
                    : 0);
        }

        public int GetQuantity(string resourceId)
        {
            return Stacks
                .Where(stack => stack.ResourceId == resourceId)
                .Sum(stack => stack.Quantity);
        }
    }
}
