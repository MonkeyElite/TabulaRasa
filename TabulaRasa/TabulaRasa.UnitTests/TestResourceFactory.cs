using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;

namespace TabulaRasa.UnitTests
{
    internal static class TestResourceFactory
    {
        public static ResourceContainerEntity FoodContainer(
            string id,
            WorldPosition position,
            int quantity = 1)
        {
            ResourceContainerEntity container = new()
            {
                Id = id,
                Position = position
            };

            if (quantity > 0)
            {
                container.Inventory.Stacks.Add(new ResourceStack
                {
                    StackId = $"{id}-food",
                    ResourceId = ResourceDefinition.FoodId,
                    Quantity = quantity
                });
            }

            return container;
        }
    }
}
