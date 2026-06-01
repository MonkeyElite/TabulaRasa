using TabulaRasa.Agents.Models;

namespace TabulaRasa.Agents.Needs
{
    public class NeedSystem
    {
        public static AgentNeedState ApplyNeedDecay(
            AgentNeedState needState,
            float hungerDelta = 1,
            float thirstDelta = 1,
            float energyDelta = -1)
        {
            needState.Hunger += hungerDelta;
            needState.Thirst += thirstDelta;
            needState.Energy += energyDelta;

            return needState;
        }
    }
}
