namespace TabulaRasa.World.Resources
{
    public sealed class ResourceDefinition
    {
        public const string FoodId = "food";

        public required string Id { get; init; }
        public required string DisplayName { get; set; }
        public required string IconKey { get; set; }
        public float UnitWeight { get; set; }
        public int MaxStackQuantity { get; set; }
        public bool IsConsumable { get; set; }
        public ResourceNeedEffects NeedEffects { get; set; } = new();

        public static ResourceDefinition CreateFood()
        {
            return new ResourceDefinition
            {
                Id = FoodId,
                DisplayName = "Food",
                IconKey = "food",
                UnitWeight = 1,
                MaxStackQuantity = 10,
                IsConsumable = true,
                NeedEffects = new ResourceNeedEffects(HungerDelta: -5)
            };
        }
    }
}
