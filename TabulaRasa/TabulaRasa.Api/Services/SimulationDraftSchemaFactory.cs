using System.Reflection;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
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
                    Field<AgentNeedState>("needs.hunger", "Hunger", "number", nameof(AgentNeedState.Hunger)),
                    Field<AgentNeedState>("needs.thirst", "Thirst", "number", nameof(AgentNeedState.Thirst)),
                    Field<AgentNeedState>("needs.energy", "Energy", "number", nameof(AgentNeedState.Energy))
                ],
                FoodFields:
                [
                    Field<FoodEntity>("id", "Id", "string", nameof(FoodEntity.Id), isEditable: false),
                    Field<FoodEntity>("position.x", "X", "number", nameof(FoodEntity.Position)),
                    Field<FoodEntity>("position.y", "Y", "number", nameof(FoodEntity.Position)),
                    Field<FoodEntity>("isConsumed", "Consumed", "boolean", nameof(FoodEntity.IsConsumed))
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
