using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public class PlanningSystem : ISystem
    {
        public string Name => "Planning System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;
            state.LatestPerceptionsByAgentId.Clear();

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState is null || agentEntity.IsDead)
                {
                    continue;
                }

                AgentPerception perception = AgentPerceptionBuilder.Build(
                    state,
                    agentEntity,
                    state.Config.PerceptionRadius);
                AgentPerception enrichedPerception = AgentMemoryService.RememberAndEnrichPerception(
                    state,
                    agentEntity,
                    perception);
                state.LatestPerceptionsByAgentId[agentEntity.Id] = enrichedPerception;
                AgentSnapshot snapshot = new(
                    agentEntity.Id,
                    agentState.NeedState.ToSnapshot(),
                    agentEntity.Position,
                    agentEntity.Inventory.Stacks
                        .GroupBy(stack => stack.ResourceId, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.Sum(stack => stack.Quantity), StringComparer.OrdinalIgnoreCase),
                    SpeciesRegistry.NormalizeId(agentEntity.SpeciesId),
                    agentEntity.AgeTicks,
                    agentEntity.AgeTicks >= SpeciesRegistry.Get(agentEntity.SpeciesId).AdultAgeTicks,
                    agentEntity.LastReproducedTick);

                state.PendingIntents.Add(agentState.Mind.Decide(
                    enrichedPerception,
                    snapshot,
                    agentState.Learning,
                    state.Random));
            }
        }
    }
}
