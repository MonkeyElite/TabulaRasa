using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;

namespace TabulaRasa.Simulation.Lifecycle
{
    public static class AgentLifecycleService
    {
        public static void MarkDead(
            SimulationState state,
            AgentEntity agent,
            string sourceSystem,
            string cause,
            long? deathTick = null)
        {
            if (agent.IsDead)
            {
                return;
            }

            agent.Health.Current = 0;
            agent.IsDead = true;
            agent.DeathTick = deathTick ?? state.ActiveTick;
            agent.DeathCause = cause;
            state.ActiveMovements.RemoveAll(movement => movement.AgentId == agent.Id);
            state.PendingIntents.RemoveAll(intent => intent.AgentId == agent.Id);
            state.PendingActionRequests.RemoveAll(request => request.AgentId == agent.Id);
            state.EmitEvent(
                "agent.died",
                sourceSystem,
                $"{agent.Id} died.",
                agent.Id,
                new Dictionary<string, string>
                {
                    ["isDead"] = "True",
                    ["cause"] = cause
                });
        }
    }
}
