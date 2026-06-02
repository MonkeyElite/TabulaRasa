using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Actions.Requests;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class ActionRequestCreationSystem : ISystem
    {
        public string Name => "Action Request Creation System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 1;

        public void Execute(SimulationState state)
        {
            foreach (AgentIntent intent in state.PendingIntents)
            {
                AgentEntity? agent = state.World.Agents.FirstOrDefault(agent => agent.Id == intent.AgentId);
                if (agent?.IsDead == true)
                {
                    continue;
                }

                state.PendingActionRequests.Add(ActionRequestFactory.Create(intent));
            }

            state.PendingIntents.Clear();
        }
    }
}
