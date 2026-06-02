using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Needs;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class NeedDecaySystem : ISystem
    {
        private const float CriticalNeedThreshold = 80;
        private const float HarmNeedThreshold = 100;
        private const float ExhaustedEnergyThreshold = 0;
        private const float SurvivalDamagePerTick = 1;

        public string Name => "Need Decay System";
        public SimulationPhase Phase => SimulationPhase.PreUpdate;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;
            var decay = state.Config.EffectiveNeedDecay;

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState is null || agentEntity.IsDead)
                {
                    continue;
                }

                NeedSystem.ApplyNeedDecay(
                    agentState.NeedState,
                    decay.HungerDelta,
                    decay.ThirstDelta,
                    decay.EnergyDelta,
                    decay.FatigueDelta);

                EmitCriticalNeedEvents(state, agentEntity, agentState.NeedState);
                ApplySurvivalDamage(state, agentEntity, agentState.NeedState);
            }
        }

        private void EmitCriticalNeedEvents(
            SimulationState state,
            AgentEntity agentEntity,
            AgentNeedState needs)
        {
            if (needs.Hunger >= CriticalNeedThreshold)
            {
                EmitCriticalNeedEvent(state, agentEntity, "hunger", "is starving", needs.Hunger);
            }

            if (needs.Thirst >= CriticalNeedThreshold)
            {
                EmitCriticalNeedEvent(state, agentEntity, "thirst", "is dehydrated", needs.Thirst);
            }

            if (needs.Fatigue >= CriticalNeedThreshold || needs.Energy <= ExhaustedEnergyThreshold)
            {
                EmitCriticalNeedEvent(state, agentEntity, "fatigue", "is exhausted", needs.Fatigue);
            }
        }

        private void ApplySurvivalDamage(
            SimulationState state,
            AgentEntity agentEntity,
            AgentNeedState needs)
        {
            float damage = 0;

            if (needs.Hunger >= HarmNeedThreshold)
            {
                damage += SurvivalDamagePerTick;
            }

            if (needs.Thirst >= HarmNeedThreshold)
            {
                damage += SurvivalDamagePerTick;
            }

            if (needs.Fatigue >= HarmNeedThreshold || needs.Energy <= ExhaustedEnergyThreshold)
            {
                damage += SurvivalDamagePerTick;
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
                });

            if (agentEntity.Health.IsDepleted)
            {
                agentEntity.IsDead = true;
                state.ActiveMovements.RemoveAll(movement => movement.AgentId == agentEntity.Id);
                state.PendingIntents.RemoveAll(intent => intent.AgentId == agentEntity.Id);
                state.PendingActionRequests.RemoveAll(request => request.AgentId == agentEntity.Id);
                state.EmitEvent(
                    "agent.died",
                    Name,
                    $"{agentEntity.Id} died.",
                    agentEntity.Id,
                    new Dictionary<string, string>
                    {
                        ["isDead"] = "True"
                    });
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
                });
        }
    }
}
