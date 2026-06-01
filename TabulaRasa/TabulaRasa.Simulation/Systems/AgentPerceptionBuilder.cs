using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Spatial.Interaction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    internal static class AgentPerceptionBuilder
    {
        public static AgentPerception Build(WorldState world, AgentEntity agent, float perceptionRadius)
        {
            List<PerceivedEntity> nearbyEntities = [];
            List<InteractionOpportunity> opportunities = [];

            foreach (FoodEntity food in SpatialQueries.GetAvailableFoodsWithinRadius(world, agent.Position, perceptionRadius))
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
