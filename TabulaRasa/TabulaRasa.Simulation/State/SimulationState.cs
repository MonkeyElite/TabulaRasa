using System.Diagnostics.CodeAnalysis;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Agents.Models;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.State
{
    public sealed class SimulationState
    {
        public required WorldState World { get; set; }
        public required SimulationTime Time { get; set; }
        public List<AgentState> Agents { get; set; } = [];

        public List<ActionRequest> PendingActions { get; init; } = [];
        public List<ActionResult> ActionResults { get; init; } = [];

        public bool IsRunning { get; set; } = true;

        [SetsRequiredMembers]
        public SimulationState(WorldState world, SimulationTime time, List<AgentState> agentStates)
        {
            World = world;
            Time = time;
            Agents = agentStates;
        }

        public AgentState? GetAgentById(string id)
        {
            AgentState? agent = Agents.Find(a => a.Id == id);

            return agent;
        }
    }
}
