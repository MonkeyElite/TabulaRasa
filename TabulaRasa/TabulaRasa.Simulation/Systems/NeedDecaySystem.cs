using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Needs;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Lifecycle;
using TabulaRasa.Simulation.Evolution;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Environment;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class NeedDecaySystem : ISystem
    {
        public string Name => "Need Decay System";
        public SimulationPhase Phase => SimulationPhase.PreUpdate;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;
            var decay = state.Config.EffectiveNeedDecay;
            var rules = state.Config.EffectiveNeedRules;

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState is null || agentEntity.IsDead)
                {
                    continue;
                }

                GridTerrainProfile terrain = world.Grid.GetTerrainProfile(agentEntity.Position.ToGridCell());
                float temperatureThirstMultiplier = GetTemperatureThirstMultiplier(world.Environment, rules);
                SpeciesDefinition species = SpeciesRegistry.Get(agentEntity.SpeciesId, state.Config.EffectiveSpeciesRules);
                agentEntity.SpeciesId = species.Id;
                float metabolismMultiplier = AgentTraitService.MetabolismMultiplier(agentEntity.Traits.Metabolism);

                NeedSystem.ApplyNeedDecay(
                    agentState.NeedState,
                    rules.MaximumNeedValue,
                    rules.MaximumEnergyValue,
                    decay.HungerDelta * terrain.HungerDeltaMultiplier * species.HungerDecayMultiplier * metabolismMultiplier,
                    decay.ThirstDelta * terrain.ThirstDeltaMultiplier * temperatureThirstMultiplier * species.ThirstDecayMultiplier * metabolismMultiplier,
                    decay.EnergyDelta,
                    decay.FatigueDelta * terrain.FatigueDeltaMultiplier * species.FatigueDecayMultiplier * metabolismMultiplier);

                EmitCriticalNeedEvents(state, agentEntity, agentState.NeedState, rules);
                ApplySurvivalDamage(state, agentEntity, agentState.NeedState, rules);
            }
        }

        private static float GetTemperatureThirstMultiplier(EnvironmentState environment, Configuration.NeedRulesConfig rules)
        {
            float multiplier = environment.Weather == EnvironmentWeather.Heat
                ? rules.HeatWeatherThirstMultiplier
                : 1f;

            if (environment.Temperature >= rules.HotTemperatureThreshold)
            {
                multiplier += rules.HotTemperatureThirstBonus;
            }
            else if (environment.Temperature <= rules.ColdTemperatureThreshold)
            {
                multiplier += rules.ColdTemperatureThirstBonus;
            }

            return Math.Clamp(multiplier, rules.MinTemperatureThirstMultiplier, rules.MaxTemperatureThirstMultiplier);
        }

        private void EmitCriticalNeedEvents(
            SimulationState state,
            AgentEntity agentEntity,
            AgentNeedState needs,
            Configuration.NeedRulesConfig rules)
        {
            if (needs.Hunger >= rules.CriticalNeedThreshold)
            {
                EmitCriticalNeedEvent(state, agentEntity, "hunger", "is starving", needs.Hunger);
            }

            if (needs.Thirst >= rules.CriticalNeedThreshold)
            {
                EmitCriticalNeedEvent(state, agentEntity, "thirst", "is dehydrated", needs.Thirst);
            }

            if (needs.Fatigue >= rules.CriticalNeedThreshold || needs.Energy <= rules.ExhaustedEnergyThreshold)
            {
                EmitCriticalNeedEvent(state, agentEntity, "fatigue", "is exhausted", needs.Fatigue);
            }
        }

        private void ApplySurvivalDamage(
            SimulationState state,
            AgentEntity agentEntity,
            AgentNeedState needs,
            Configuration.NeedRulesConfig rules)
        {
            float damage = 0;

            if (needs.Hunger >= rules.HarmNeedThreshold)
            {
                damage += rules.SurvivalDamagePerTick;
            }

            if (needs.Thirst >= rules.HarmNeedThreshold)
            {
                damage += rules.SurvivalDamagePerTick;
            }

            if (needs.Fatigue >= rules.HarmNeedThreshold || needs.Energy <= rules.ExhaustedEnergyThreshold)
            {
                damage += rules.SurvivalDamagePerTick;
            }

            if (damage <= 0)
            {
                return;
            }

            agentEntity.Health.Current = Math.Max(0, agentEntity.Health.Current - damage);
            state.EmitEvent(
                "agent.health_damaged",
                Name,
                $"{agentEntity.Id} took {damage:0.##} survival damage.",
                agentEntity.Id,
                new Dictionary<string, string>
                {
                    ["damage"] = damage.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                    ["health"] = agentEntity.Health.Current.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                },
                severity: "warning",
                importance: 0.74f,
                tags: ["survival", "need"]);

            if (agentEntity.Health.IsDepleted)
            {
                AgentLifecycleService.MarkDead(state, agentEntity, Name, "survival");
            }
        }

        private void EmitCriticalNeedEvent(
            SimulationState state,
            AgentEntity agentEntity,
            string needName,
            string condition,
            float value)
        {
            state.EmitEvent(
                "agent.need_critical",
                Name,
                $"{agentEntity.Id} {condition}.",
                agentEntity.Id,
                new Dictionary<string, string>
                {
                    ["need"] = needName,
                    ["value"] = value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                },
                severity: "warning",
                importance: 0.62f,
                tags: ["need", needName]);
        }
    }
}
