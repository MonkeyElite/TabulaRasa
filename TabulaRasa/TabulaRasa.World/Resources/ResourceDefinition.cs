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
        public ResourceRenewability Renewability { get; set; } = ResourceRenewability.Renewable;
        public string Category { get; set; } = "general";
        public ResourceNeedEffects NeedEffects { get; set; } = new();

        public const string WaterId = "water";
        public const string WoodId = "wood";
        public const string StoneId = "stone";

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
                Renewability = ResourceRenewability.Renewable,
                Category = "plant",
                NeedEffects = new ResourceNeedEffects(HungerDelta: -5)
            };
        }

        public static ResourceDefinition CreateWater()
        {
            return new ResourceDefinition
            {
                Id = WaterId,
                DisplayName = "Water",
                IconKey = "water",
                UnitWeight = 1,
                MaxStackQuantity = 10,
                IsConsumable = true,
                Renewability = ResourceRenewability.Renewable,
                Category = "water",
                NeedEffects = new ResourceNeedEffects(ThirstDelta: -5)
            };
        }

        public static ResourceDefinition CreateWood()
        {
            return new ResourceDefinition
            {
                Id = WoodId,
                DisplayName = "Wood",
                IconKey = "wood",
                UnitWeight = 1.5f,
                MaxStackQuantity = 20,
                IsConsumable = false,
                Renewability = ResourceRenewability.Renewable,
                Category = "plant"
            };
        }

        public static ResourceDefinition CreateStone()
        {
            return new ResourceDefinition
            {
                Id = StoneId,
                DisplayName = "Stone",
                IconKey = "stone",
                UnitWeight = 2,
                MaxStackQuantity = 20,
                IsConsumable = false,
                Renewability = ResourceRenewability.Nonrenewable,
                Category = "deposit"
            };
        }
    }
}
