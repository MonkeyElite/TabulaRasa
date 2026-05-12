using TabulaRasa.Abstractions.Agents;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    internal static class AgentPerceptionBuilder
    {
        public static AgentPerception Build(WorldState world, AgentEntity agent)
        {
            List<PerceivedEntity> nearbyEntities = [];
            List<InteractionOpportunity> opportunities = [];

            foreach (FoodEntity food in world.Foods.Where(f => !f.IsConsumed && f.Position == agent.Position))
            {
                nearbyEntities.Add(new PerceivedEntity(
                    food.Id,
                    PerceivedEntityType.Food,
                    food.Position,
                    IsInteractable: true));

                opportunities.Add(new InteractionOpportunity(
                    AgentActionType.Eat,
                    food.Id,
                    food.Position));
            }

            return new AgentPerception(nearbyEntities, opportunities);
        }
    }
}
