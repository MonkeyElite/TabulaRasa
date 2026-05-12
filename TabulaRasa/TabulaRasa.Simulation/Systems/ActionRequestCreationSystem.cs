using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Actions.Requests;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;

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
                state.PendingActionRequests.Add(ActionRequestFactory.Create(intent));
            }

            state.PendingIntents.Clear();
        }
    }
}
