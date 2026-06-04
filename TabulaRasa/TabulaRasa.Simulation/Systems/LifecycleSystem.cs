using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Lifecycle;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class LifecycleSystem : ISystem
    {
        public const float ReproductionNeedThreshold = 4f;
        public const float ReproductionRange = 1.25f;

        public string Name => "Lifecycle System";
        public SimulationPhase Phase => SimulationPhase.PostUpdate;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            foreach (AgentEntity agent in state.World.Agents)
            {
                if (agent.IsDead)
                {
                    continue;
                }

                agent.SpeciesId = SpeciesRegistry.NormalizeId(agent.SpeciesId);
                agent.AgeTicks++;

                SpeciesDefinition species = SpeciesRegistry.Get(agent.SpeciesId);
                if (agent.AgeTicks >= species.MaxAgeTicks)
                {
                    AgentLifecycleService.MarkDead(state, agent, Name, "old_age");
                }
            }
        }

        public static bool CanReproduce(SimulationState state, AgentEntity first, AgentEntity second)
        {
            if (first.Id == second.Id
                || first.IsDead
                || second.IsDead
                || !string.Equals(SpeciesRegistry.NormalizeId(first.SpeciesId), SpeciesRegistry.NormalizeId(second.SpeciesId), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            SpeciesDefinition species = SpeciesRegistry.Get(first.SpeciesId);
            if (first.AgeTicks < species.AdultAgeTicks || second.AgeTicks < species.AdultAgeTicks)
            {
                return false;
            }

            long tick = state.ActiveTick;
            if (!CooldownReady(first, species, tick) || !CooldownReady(second, species, tick))
            {
                return false;
            }

            if (first.Position.DistanceTo(second.Position) > ReproductionRange)
            {
                return false;
            }

            return HasSafeNeeds(state, first) && HasSafeNeeds(state, second);
        }

        private static bool CooldownReady(AgentEntity agent, SpeciesDefinition species, long tick)
        {
            return agent.LastReproducedTick is null
                || tick - agent.LastReproducedTick.Value >= species.ReproductionCooldownTicks;
        }

        private static bool HasSafeNeeds(SimulationState state, AgentEntity agent)
        {
            var needs = state.GetAgentById(agent.Id)?.NeedState;
            return needs is not null
                && needs.Hunger <= ReproductionNeedThreshold
                && needs.Thirst <= ReproductionNeedThreshold
                && needs.Fatigue <= ReproductionNeedThreshold;
        }
    }
}
