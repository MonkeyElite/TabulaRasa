using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    internal static class AgentPerceptionBuilder
    {
        private const float PerceptionRadius = 2f;

        public static AgentPerception Build(WorldState world, AgentEntity agent)
        {
            List<PerceivedEntity> nearbyEntities = [];
            List<InteractionOpportunity> opportunities = [];

            foreach (FoodEntity food in SpatialQueries.GetAvailableFoodsWithinRadius(world, agent.Position, PerceptionRadius))
            {
                InteractionPoint? interactionPoint = SpatialQueries.FindNearestAvailableInteractionPoint(
                    food,
                    agent.Position,
                    maxDistance: float.MaxValue);

                nearbyEntities.Add(new PerceivedEntity(
                    food.Id,
                    PerceivedEntityType.Food,
                    food.Position,
                    IsInteractable: interactionPoint is not null));

                if (interactionPoint is not null)
                {
                    opportunities.Add(new InteractionOpportunity(
                        AgentActionType.Eat,
                        food.Id,
                        interactionPoint.Value.StandPosition));
                }
            }

            return new AgentPerception(nearbyEntities, opportunities);
        }
    }
}
