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
                Energy = 10
            };

            AgentNeedState result = NeedSystem.ApplyNeedDecay(needs);

            Assert.Same(needs, result);
            Assert.Equal(2, needs.Hunger);
            Assert.Equal(3, needs.Thirst);
            Assert.Equal(9, needs.Energy);
        }
    }
}
