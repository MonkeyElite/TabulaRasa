using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Needs;

namespace TabulaRasa.UnitTests.Agents.Needs
{
    public class NeedSystemTests
    {
        [Fact]
        public void ApplyNeedDecay_UpdatesAllTrackedNeeds()
        {
            var needs = new AgentNeedState
            {
                Hunger = 1,
                Thirst = 2,
                Energy = 10,
                Fatigue = 3
            };

            AgentNeedState result = NeedSystem.ApplyNeedDecay(needs);

            Assert.Same(needs, result);
            Assert.Equal(2, needs.Hunger);
            Assert.Equal(3, needs.Thirst);
            Assert.Equal(9, needs.Energy);
            Assert.Equal(4, needs.Fatigue);
        }

        [Fact]
        public void RecoveryActions_AffectOnlyTheirTargetNeeds()
        {
            var needs = new AgentNeedState
            {
                Hunger = 7,
                Thirst = 8,
                Energy = 2,
                Fatigue = 9
            };

            NeedSystem.ApplyEat(needs);

            Assert.Equal(2, needs.Hunger);
            Assert.Equal(8, needs.Thirst);
            Assert.Equal(2, needs.Energy);
            Assert.Equal(9, needs.Fatigue);

            NeedSystem.ApplyDrink(needs);

            Assert.Equal(2, needs.Hunger);
            Assert.Equal(3, needs.Thirst);
            Assert.Equal(2, needs.Energy);
            Assert.Equal(9, needs.Fatigue);

            NeedSystem.ApplyRest(needs);

            Assert.Equal(2, needs.Hunger);
            Assert.Equal(3, needs.Thirst);
            Assert.Equal(6, needs.Energy);
            Assert.Equal(4, needs.Fatigue);
        }
    }
}
