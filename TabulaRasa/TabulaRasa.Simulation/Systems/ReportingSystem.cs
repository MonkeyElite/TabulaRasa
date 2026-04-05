using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public class ReportingSystem : ISystem
    {
        public string Name => "Reporting System";
        public SimulationPhase Phase => SimulationPhase.Logging;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;

            Console.WriteLine($"Tick {state.Time.Tick}");

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState == null)
                {
                    continue;
                }

                Console.WriteLine(
                    $"  Agent {agentEntity.Id} | Pos={agentEntity.Position} | Hunger={agentState.NeedState.Hunger}");
            }

            var remainingFood = world.Foods.Count(f => !f.IsConsumed);
            Console.WriteLine($"  Remaining food: {remainingFood}");
            Console.WriteLine();
        }
    }
}
