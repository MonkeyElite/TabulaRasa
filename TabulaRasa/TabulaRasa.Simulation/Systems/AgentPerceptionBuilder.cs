using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    internal static class AgentPerceptionBuilder
    {
        public static AgentPerception Build(WorldState world, AgentEntity agent, float perceptionRadius)
        {
            List<PerceivedEntity> nearbyEntities = [];
            List<InteractionOpportunity> opportunities = [];

            foreach (ISpatialEntity entity in world.SpatialEntities)
            {
                if (entity.Id == agent.Id || !CanBeSeen(entity))
                {
                    continue;
                }

                float distance = entity.Position.DistanceTo(agent.Position);

                if (distance > perceptionRadius)
                {
                    continue;
                }

                InteractionPoint? interactionPoint = entity is IInteractableEntity interactable
                    ? SpatialQueries.FindNearestAvailableInteractionPoint(
                        interactable,
                        agent.Position,
                        maxDistance: float.MaxValue)
                    : null;
                float relevance = CalculateRelevance(distance, perceptionRadius);

                nearbyEntities.Add(new PerceivedEntity(
                    entity.Id,
                    ToPerceivedEntityType(entity),
                    entity.Position,
                    IsInteractable: interactionPoint is not null,
                    Channel: PerceptionChannel.Sight,
                    Distance: distance,
                    Certainty: 1,
                    Relevance: relevance));

                if (entity is ResourceContainerEntity container
                    && SpatialQueries.ContainerHasFood(container)
                    && interactionPoint is not null)
                {
                    opportunities.Add(new InteractionOpportunity(
                        AgentActionType.Eat,
                        container.Id,
                        interactionPoint.Value.StandPosition,
                        SourceEntityId: container.Id,
                        Channel: PerceptionChannel.Sight,
                        Relevance: relevance));
                }
            }

            return new AgentPerception(nearbyEntities, opportunities);
        }

        private static bool CanBeSeen(ISpatialEntity entity)
        {
            return entity switch
            {
                ResourceContainerEntity container => !container.IsEmpty,
                _ => true
            };
        }

        private static PerceivedEntityType ToPerceivedEntityType(ISpatialEntity entity)
        {
            return entity switch
            {
                AgentEntity => PerceivedEntityType.Agent,
                ResourceContainerEntity container when container.Inventory.GetQuantity(ResourceDefinition.FoodId) > 0 => PerceivedEntityType.Food,
                ResourceContainerEntity => PerceivedEntityType.ResourceContainer,
                _ => PerceivedEntityType.Unknown
            };
        }

        private static float CalculateRelevance(float distance, float radius)
        {
            if (radius <= 0)
            {
                return distance <= 0 ? 1 : 0;
            }

            return Math.Clamp(1 - (distance / radius), 0, 1);
        }
    }
}
