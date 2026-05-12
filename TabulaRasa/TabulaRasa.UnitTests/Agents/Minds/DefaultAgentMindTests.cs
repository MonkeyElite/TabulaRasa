using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;

namespace TabulaRasa.UnitTests.Agents.Minds
{
    public class DefaultAgentMindTests
    {
        [Fact]
        public void Decide_WhenHungryAndFoodIsInteractable_ReturnsEatIntentWithTarget()
        {
            var mind = new DefaultAgentMind();
            var foodPosition = new WorldPosition(1, 1);
            var perception = new AgentPerception(
                [
                    new PerceivedEntity("food-1", PerceivedEntityType.Food, foodPosition, IsInteractable: true)
                ],
                [
                    new InteractionOpportunity(AgentActionType.Eat, "food-1", foodPosition)
                ]);
            var self = new AgentSnapshot(
                "agent-1",
                new AgentNeedsSnapshot(Hunger: 5, Thirst: 0, Energy: 10),
                foodPosition);

            AgentIntent intent = mind.Decide(perception, self);

            Assert.Equal("agent-1", intent.AgentId);
            Assert.Equal(AgentActionType.Eat, intent.ActionType);
            Assert.Equal("food-1", intent.TargetId);
        }

        [Fact]
        public void Decide_WhenHungryWithoutFoodOpportunity_ReturnsWanderIntent()
        {
            var mind = new DefaultAgentMind();
            var self = new AgentSnapshot(
                "agent-1",
                new AgentNeedsSnapshot(Hunger: 5, Thirst: 0, Energy: 10),
                new WorldPosition(1, 1));

            AgentIntent intent = mind.Decide(AgentPerception.Empty, self);

            Assert.Equal("agent-1", intent.AgentId);
            Assert.Equal(AgentActionType.Wander, intent.ActionType);
            Assert.Null(intent.TargetId);
        }
    }
}
