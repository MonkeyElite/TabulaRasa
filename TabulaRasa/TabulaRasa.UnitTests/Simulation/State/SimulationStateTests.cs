using TabulaRasa.Abstractions.Time;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.State
{
    public class SimulationStateTests
    {
        [Fact]
        public void TestSimulationStateInitialization()
        {
            // Arrange
            var worldState = new WorldState();
            var simulationTime = new SimulationTime(0);
            var agentStates = new List<AgentState>();

            // Act
            var simulationState = new SimulationState(worldState, simulationTime, agentStates);

            // Assert
            Assert.NotNull(simulationState);
            Assert.Equal(worldState, simulationState.World);
            Assert.Equal(simulationTime, simulationState.Time);
            Assert.Equal(agentStates, simulationState.Agents);
        }

        [Fact]
        public void TestGetAgentById()
        {
            // Arrange
            WorldState worldState = new WorldState();
            SimulationTime simulationTime = new SimulationTime(0);

            AgentState agent1 = new AgentState("agent1", new AgentNeedState(), null! );
            List<AgentState> agentStates = new List<AgentState>();
            agentStates.Add(agent1);

            // Act
            SimulationState simulationState = new SimulationState(worldState, simulationTime, agentStates);

            // Assert
            AgentState? retrievedAgent = simulationState.GetAgentById("agent1");
            Assert.Equal(agent1, retrievedAgent);
        }
    }
}
