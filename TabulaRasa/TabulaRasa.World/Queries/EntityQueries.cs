using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.World.Queries
{
    public static class EntityQueries
    {
        public static IReadOnlyList<ISpatialEntity> GetSpatialEntities(WorldState worldState)
        {
            return worldState.SpatialEntities.ToList();
        }

        public static ISpatialEntity? GetSpatialEntity(WorldState worldState, string entityId)
        {
            return worldState.GetSpatialEntityById(entityId);
        }

        public static AgentEntity? GetAgentEntity(WorldState worldState, string agentId)
        {
            return worldState.Agents.FirstOrDefault(a => a.Id == agentId);
        }

        public static ResourceContainerEntity? GetResourceContainer(WorldState worldState, string containerId)
        {
            return worldState.ResourceContainers.FirstOrDefault(container => container.Id == containerId);
        }

        public static IDamageableEntity? GetDamageableEntity(WorldState worldState, string entityId)
        {
            return GetSpatialEntity(worldState, entityId) as IDamageableEntity;
        }
    }
}
