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
        public const string StoneToolId = "stone-tool";
        public const string WoodenToolId = "wooden-tool";

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

        public static ResourceDefinition CreateStoneTool()
        {
            return new ResourceDefinition
            {
                Id = StoneToolId,
                DisplayName = "Stone Tool",
                IconKey = "stone-tool",
                UnitWeight = 1.5f,
                MaxStackQuantity = 5,
                IsConsumable = false,
                Renewability = ResourceRenewability.Nonrenewable,
                Category = "tool"
            };
        }

        public static ResourceDefinition CreateWoodenTool()
        {
            return new ResourceDefinition
            {
                Id = WoodenToolId,
                DisplayName = "Wooden Tool",
                IconKey = "wooden-tool",
                UnitWeight = 1,
                MaxStackQuantity = 5,
                IsConsumable = false,
                Renewability = ResourceRenewability.Renewable,
                Category = "tool"
            };
        }
    }
}
