using TabulaRasa.Agents.Models;

namespace TabulaRasa.Agents.Needs
{
    public class NeedSystem
    {
        public static AgentNeedState ApplyNeedDecay(AgentNeedState needState)
        {
            needState.Hunger += 1;

            return needState;
        }
    }
}
