using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Spatial;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Systems
{
    internal static class AgentPerceptionBuilder
    {
        public static AgentPerception Build(SimulationState state, AgentEntity agent, float perceptionRadius)
        {
            WorldState world = state.World;
            List<PerceivedEntity> nearbyEntities = [];
            List<InteractionOpportunity> opportunities = [];
            GridTerrainProfile agentTerrain = world.Grid.GetTerrainProfile(agent.Position.ToGridCell());
            SpeciesDefinition viewerSpecies = SpeciesRegistry.Get(agent.SpeciesId);
            agent.SpeciesId = viewerSpecies.Id;
            float effectiveRadius = perceptionRadius * agentTerrain.PerceptionMultiplier * viewerSpecies.PerceptionMultiplier;

            foreach (ISpatialEntity entity in world.SpatialEntities)
            {
                if (entity.Id == agent.Id || !CanBeSeen(entity))
                {
                    continue;
                }

                float distance = entity.Position.DistanceTo(agent.Position);

                if (distance > effectiveRadius)
                {
                    continue;
                }

                InteractionPoint? interactionPoint = entity is IInteractableEntity interactable
                    ? SpatialQueries.FindNearestAvailableInteractionPoint(
                        interactable,
                        agent.Position,
                        maxDistance: float.MaxValue)
                    : null;
                float relevance = CalculateRelevance(distance, effectiveRadius);
                GridTerrainProfile entityTerrain = world.Grid.GetTerrainProfile(entity.Position.ToGridCell());
                float certainty = Math.Clamp(agentTerrain.PerceptionMultiplier * entityTerrain.PerceptionMultiplier, 0.1f, 1f);

                nearbyEntities.Add(new PerceivedEntity(
                    entity.Id,
                    ToPerceivedEntityType(agent, entity),
                    entity.Position,
                    IsInteractable: interactionPoint is not null,
                    Channel: PerceptionChannel.Sight,
                    Distance: distance,
                    Certainty: certainty,
                    Relevance: relevance));

                if (entity is AgentEntity otherAgent
                    && !otherAgent.IsDead)
                {
                    SpeciesDefinition otherSpecies = SpeciesRegistry.Get(otherAgent.SpeciesId);
                    if (viewerSpecies.CanAttackSpecies(otherSpecies.Id))
                    {
                        opportunities.Add(new InteractionOpportunity(
                            AgentActionType.Attack,
                            otherAgent.Id,
                            otherAgent.Position,
                            SourceEntityId: otherAgent.Id,
                            Channel: PerceptionChannel.Sight,
                            Relevance: relevance));
                    }

                    if (otherSpecies.CanAttackSpecies(viewerSpecies.Id))
                    {
                        opportunities.Add(new InteractionOpportunity(
                            AgentActionType.Flee,
                            otherAgent.Id,
                            agent.Position,
                            SourceEntityId: otherAgent.Id,
                            Channel: PerceptionChannel.Sight,
                            Relevance: relevance));
                    }

                    if (LifecycleSystem.CanReproduce(state, agent, otherAgent))
                    {
                        opportunities.Add(new InteractionOpportunity(
                            AgentActionType.Reproduce,
                            otherAgent.Id,
                            otherAgent.Position,
                            SourceEntityId: otherAgent.Id,
                            Channel: PerceptionChannel.Sight,
                            Relevance: relevance));
                    }
                }

                if (entity is ResourceContainerEntity container
                    && SpatialQueries.ContainerHasFood(container)
                    && viewerSpecies.Id == SpeciesRegistry.HumanId
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

                if (entity is PlantEntity plant
                    && plant.IsHarvestable
                    && viewerSpecies.CanEatResource(plant.ResourceId)
                    && interactionPoint is not null)
                {
                    AgentActionType actionType = string.Equals(plant.ResourceId, ResourceDefinition.FoodId, StringComparison.OrdinalIgnoreCase)
                        ? AgentActionType.Eat
                        : AgentActionType.PickUpResource;
                    opportunities.Add(new InteractionOpportunity(
                        actionType,
                        plant.Id,
                        interactionPoint.Value.StandPosition,
                        SourceEntityId: plant.Id,
                        Channel: PerceptionChannel.Sight,
                        Relevance: relevance));
                }

                if (entity is WaterSourceEntity waterSource
                    && waterSource.IsAvailable
                    && interactionPoint is not null)
                {
                    opportunities.Add(new InteractionOpportunity(
                        AgentActionType.Drink,
                        waterSource.Id,
                        interactionPoint.Value.StandPosition,
                        SourceEntityId: waterSource.Id,
                        Channel: PerceptionChannel.Sight,
                        Relevance: relevance));
                }

                if (entity is ResourceDepositEntity deposit
                    && !deposit.IsEmpty
                    && interactionPoint is not null)
                {
                    opportunities.Add(new InteractionOpportunity(
                        AgentActionType.PickUpResource,
                        deposit.Id,
                        interactionPoint.Value.StandPosition,
                        SourceEntityId: deposit.Id,
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
                PlantEntity plant => !plant.IsDecayed,
                WaterSourceEntity waterSource => waterSource.IsAvailable,
                ResourceDepositEntity deposit => !deposit.IsEmpty,
                _ => true
            };
        }

        private static PerceivedEntityType ToPerceivedEntityType(AgentEntity viewer, ISpatialEntity entity)
        {
            return entity switch
            {
                AgentEntity agent when SpeciesRegistry.Get(viewer.SpeciesId).CanAttackSpecies(SpeciesRegistry.Get(agent.SpeciesId).Id) => PerceivedEntityType.Prey,
                AgentEntity agent when SpeciesRegistry.Get(agent.SpeciesId).CanAttackSpecies(SpeciesRegistry.Get(viewer.SpeciesId).Id) => PerceivedEntityType.Predator,
                AgentEntity => PerceivedEntityType.Agent,
                ResourceContainerEntity container when container.Inventory.GetQuantity(ResourceDefinition.FoodId) > 0 => PerceivedEntityType.Food,
                ResourceContainerEntity => PerceivedEntityType.ResourceContainer,
                PlantEntity plant when string.Equals(plant.ResourceId, ResourceDefinition.FoodId, StringComparison.OrdinalIgnoreCase) => PerceivedEntityType.Food,
                PlantEntity => PerceivedEntityType.Plant,
                WaterSourceEntity => PerceivedEntityType.WaterSource,
                ResourceDepositEntity => PerceivedEntityType.ResourceDeposit,
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
