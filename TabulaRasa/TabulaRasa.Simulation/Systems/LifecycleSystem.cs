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
                agent.AgeTicks = CalculateAgeDays(state, agent);

                SpeciesDefinition species = SpeciesRegistry.Get(agent.SpeciesId, state.Config.EffectiveSpeciesRules);
                if (agent.AgeTicks >= species.MaxAgeDays)
                {
                    AgentLifecycleService.MarkDead(state, agent, Name, "old_age");
                }
            }
        }

        public static int CalculateAgeDays(SimulationState state, AgentEntity agent)
        {
            long elapsedTicks = Math.Max(0, state.ActiveTick - agent.BornTick + 1);
            int elapsedAgeDays = (int)Math.Floor(elapsedTicks * state.Config.EffectiveLifecycle.AgeDaysPerTick);
            return Math.Max(agent.AgeTicks, elapsedAgeDays);
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

            SpeciesDefinition species = SpeciesRegistry.Get(first.SpeciesId, state.Config.EffectiveSpeciesRules);
            if (first.AgeTicks < species.AdultAgeDays || second.AgeTicks < species.AdultAgeDays)
            {
                return false;
            }

            long tick = state.ActiveTick;
            var reproduction = state.Config.EffectiveBelievability.EffectiveReproduction;
            if (!CooldownReady(first, species, tick, reproduction.CooldownScale)
                || !CooldownReady(second, species, tick, reproduction.CooldownScale))
            {
                return false;
            }

            if (first.Position.DistanceTo(second.Position) > reproduction.Range)
            {
                return false;
            }

            return HasSafeNeeds(state, first, reproduction.NeedThreshold)
                && HasSafeNeeds(state, second, reproduction.NeedThreshold)
                && HasPopulationPressureRoom(state, first.SpeciesId);
        }

        private static bool CooldownReady(AgentEntity agent, SpeciesDefinition species, long tick, float cooldownScale)
        {
            return agent.LastReproducedTick is null
                || tick - agent.LastReproducedTick.Value >= MathF.Ceiling(species.ReproductionCooldownTicks * Math.Max(0.1f, cooldownScale));
        }

        private static bool HasSafeNeeds(SimulationState state, AgentEntity agent, float needThreshold)
        {
            var needs = state.GetAgentById(agent.Id)?.NeedState;
            return needs is not null
                && needs.Hunger <= needThreshold
                && needs.Thirst <= needThreshold
                && needs.Fatigue <= needThreshold;
        }

        private static bool HasPopulationPressureRoom(SimulationState state, string speciesId)
        {
            float influence = state.Config.EffectiveBelievability.EffectiveReproduction.PopulationPressureInfluence;
            if (influence <= 0)
            {
                return true;
            }

            int aliveAgents = Math.Max(1, state.World.Agents.Count(agent => !agent.IsDead));
            int speciesAlive = Math.Max(1, state.World.Agents.Count(agent =>
                !agent.IsDead
                && string.Equals(SpeciesRegistry.NormalizeId(agent.SpeciesId), SpeciesRegistry.NormalizeId(speciesId), StringComparison.OrdinalIgnoreCase)));
            int occupiedCells = state.World.Agents.Count(agent => !agent.IsDead);
            int totalCells = Math.Max(1, state.World.Grid.Width * state.World.Grid.Height);
            float freeSpaceRatio = Math.Clamp((totalCells - occupiedCells) / (float)totalCells, 0, 1);
            float foodUnits = state.World.ResourceContainers.Sum(container => container.Inventory.GetQuantity(TabulaRasa.World.Resources.ResourceDefinition.FoodId))
                + state.World.Plants.Sum(plant => plant.Yield);
            float waterUnits = state.World.WaterSources.Sum(water => water.CurrentVolume);
            float resourceRatio = Math.Clamp(Math.Min(foodUnits, waterUnits) / Math.Max(1, aliveAgents * 2f), 0, 1);
            float speciesCrowding = Math.Clamp(speciesAlive / Math.Max(1f, totalCells / 4f), 0, 1);
            float pressureScore = Math.Min(freeSpaceRatio, resourceRatio) * (1 - (speciesCrowding * 0.35f));

            return pressureScore >= influence * 0.35f;
        }
    }
}
