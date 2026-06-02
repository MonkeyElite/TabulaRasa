using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Interfaces;
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
                    world,
                    agentEntity,
                    state.Config.PerceptionRadius);
                state.LatestPerceptionsByAgentId[agentEntity.Id] = perception;
                AgentSnapshot snapshot = new(
                    agentEntity.Id,
                    agentState.NeedState.ToSnapshot(),
                    agentEntity.Position);

                state.PendingIntents.Add(agentState.Mind.Decide(perception, snapshot));
            }
        }
    }
}
