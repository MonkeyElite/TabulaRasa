using System.Reflection;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;

namespace TabulaRasa.Api.Services
{
    public static class SimulationDraftSchemaFactory
    {
        public static SimulationDraftSchemaDto Create()
        {
            return new SimulationDraftSchemaDto(
                StateFields:
                [
                    Field<SimulationState>("tick", "Tick", "number", nameof(SimulationState.Time))
                ],
                GridFields:
                [
                    Field<GridMap>("grid.width", "Width", "number", nameof(GridMap.Width)),
                    Field<GridMap>("grid.height", "Height", "number", nameof(GridMap.Height)),
                    Field<GridMap>("grid.blockedCells", "Blocked cells", "gridCells", nameof(GridMap.BlockedCells)),
                    Field<GridMap>("grid.terrainCells", "Terrain cells", "terrainCells", nameof(GridMap.TerrainCells))
                ],
                AgentFields:
                [
                    Field<AgentEntity>("id", "Id", "string", nameof(AgentEntity.Id), isEditable: false),
                    Field<AgentEntity>("position.x", "X", "number", nameof(AgentEntity.Position)),
                    Field<AgentEntity>("position.y", "Y", "number", nameof(AgentEntity.Position)),
                    Field<Inventory>("inventory.maxSlots", "Slots", "number", nameof(Inventory.MaxSlots)),
                    Field<Inventory>("inventory.maxWeight", "Weight", "number", nameof(Inventory.MaxWeight)),
                    Field<AgentNeedState>("needs.hunger", "Hunger", "number", nameof(AgentNeedState.Hunger)),
                    Field<AgentNeedState>("needs.thirst", "Thirst", "number", nameof(AgentNeedState.Thirst)),
                    Field<AgentNeedState>("needs.energy", "Energy", "number", nameof(AgentNeedState.Energy)),
                    Field<AgentNeedState>("needs.fatigue", "Fatigue", "number", nameof(AgentNeedState.Fatigue))
                ],
                ResourceDefinitionFields:
                [
                    Field<ResourceDefinition>("id", "Id", "string", nameof(ResourceDefinition.Id), isEditable: false),
                    Field<ResourceDefinition>("displayName", "Name", "string", nameof(ResourceDefinition.DisplayName)),
                    Field<ResourceDefinition>("iconKey", "Icon", "string", nameof(ResourceDefinition.IconKey)),
                    Field<ResourceDefinition>("unitWeight", "Unit weight", "number", nameof(ResourceDefinition.UnitWeight)),
                    Field<ResourceDefinition>("maxStackQuantity", "Stack max", "number", nameof(ResourceDefinition.MaxStackQuantity)),
                    Field<ResourceDefinition>("isConsumable", "Consumable", "boolean", nameof(ResourceDefinition.IsConsumable)),
                    Field<ResourceNeedEffects>("needEffects.hungerDelta", "Hunger effect", "number", nameof(ResourceNeedEffects.HungerDelta)),
                    Field<ResourceNeedEffects>("needEffects.thirstDelta", "Thirst effect", "number", nameof(ResourceNeedEffects.ThirstDelta)),
                    Field<ResourceNeedEffects>("needEffects.energyDelta", "Energy effect", "number", nameof(ResourceNeedEffects.EnergyDelta)),
                    Field<ResourceNeedEffects>("needEffects.fatigueDelta", "Fatigue effect", "number", nameof(ResourceNeedEffects.FatigueDelta))
                ],
                ResourceContainerFields:
                [
                    Field<ResourceContainerEntity>("id", "Id", "string", nameof(ResourceContainerEntity.Id), isEditable: false),
                    Field<ResourceContainerEntity>("position.x", "X", "number", nameof(ResourceContainerEntity.Position)),
                    Field<ResourceContainerEntity>("position.y", "Y", "number", nameof(ResourceContainerEntity.Position)),
                    Field<Inventory>("inventory.maxSlots", "Slots", "number", nameof(Inventory.MaxSlots)),
                    Field<Inventory>("inventory.maxWeight", "Weight", "number", nameof(Inventory.MaxWeight))
                ],
                PlantFields:
                [
                    Field<PlantEntity>("id", "Id", "string", nameof(PlantEntity.Id), isEditable: false),
                    Field<PlantEntity>("position.x", "X", "number", nameof(PlantEntity.Position)),
                    Field<PlantEntity>("position.y", "Y", "number", nameof(PlantEntity.Position)),
                    Field<PlantEntity>("resourceId", "Resource", "string", nameof(PlantEntity.ResourceId)),
                    Field<PlantEntity>("yield", "Yield", "number", nameof(PlantEntity.Yield)),
                    Field<PlantEntity>("maxYield", "Max yield", "number", nameof(PlantEntity.MaxYield)),
                    Field<PlantEntity>("regrowthTicks", "Regrow", "number", nameof(PlantEntity.RegrowthTicks))
                ],
                WaterSourceFields:
                [
                    Field<WaterSourceEntity>("id", "Id", "string", nameof(WaterSourceEntity.Id), isEditable: false),
                    Field<WaterSourceEntity>("position.x", "X", "number", nameof(WaterSourceEntity.Position)),
                    Field<WaterSourceEntity>("position.y", "Y", "number", nameof(WaterSourceEntity.Position)),
                    Field<WaterSourceEntity>("currentVolume", "Volume", "number", nameof(WaterSourceEntity.CurrentVolume)),
                    Field<WaterSourceEntity>("maxVolume", "Max volume", "number", nameof(WaterSourceEntity.MaxVolume))
                ],
                ResourceDepositFields:
                [
                    Field<ResourceDepositEntity>("id", "Id", "string", nameof(ResourceDepositEntity.Id), isEditable: false),
                    Field<ResourceDepositEntity>("position.x", "X", "number", nameof(ResourceDepositEntity.Position)),
                    Field<ResourceDepositEntity>("position.y", "Y", "number", nameof(ResourceDepositEntity.Position)),
                    Field<ResourceDepositEntity>("resourceId", "Resource", "string", nameof(ResourceDepositEntity.ResourceId)),
                    Field<ResourceDepositEntity>("quantity", "Quantity", "number", nameof(ResourceDepositEntity.Quantity)),
                    Field<ResourceDepositEntity>("maxQuantity", "Max", "number", nameof(ResourceDepositEntity.MaxQuantity))
                ],
                InventoryFields:
                [
                    Field<Inventory>("maxSlots", "Slots", "number", nameof(Inventory.MaxSlots)),
                    Field<Inventory>("maxWeight", "Weight", "number", nameof(Inventory.MaxWeight))
                ],
                ResourceStackFields:
                [
                    Field<ResourceStack>("stackId", "Stack", "string", nameof(ResourceStack.StackId), isEditable: false),
                    Field<ResourceStack>("resourceId", "Resource", "string", nameof(ResourceStack.ResourceId)),
                    Field<ResourceStack>("quantity", "Quantity", "number", nameof(ResourceStack.Quantity))
                ]);
        }

        private static EditableFieldDto Field<TSource>(
            string path,
            string label,
            string valueType,
            string sourceProperty,
            bool isEditable = true)
        {
            PropertyInfo? property = typeof(TSource).GetProperty(sourceProperty);

            return new EditableFieldDto(
                path,
                label,
                valueType,
                isEditable,
                typeof(TSource).FullName ?? typeof(TSource).Name,
                property?.Name ?? sourceProperty);
        }
    }
}
