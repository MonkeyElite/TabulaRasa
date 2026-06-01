using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.State;

namespace TabulaRasa.UnitTests.Simulation.Engine
{
    public sealed class SimulationEngineObservabilityTests
    {
        [Fact]
        public void ExecuteTick_RunsSystemsInDeterministicOrder()
        {
            List<string> order = [];
            SimulationEngine engine = new(
            [
                new RecordingSystem("z", SimulationPhase.Evaluation, 0, order),
                new RecordingSystem("execution", SimulationPhase.Execution, 0, order),
                new RecordingSystem("a", SimulationPhase.Evaluation, 0, order),
                new RecordingSystem("first-priority", SimulationPhase.Evaluation, -1, order)
            ]);
            SimulationState state = CreateState();

            engine.ExecuteTick(state);

            Assert.Equal(["first-priority", "a", "z", "execution"], order);
            Assert.Equal(["first-priority", "a", "z", "execution"], state.GetDiagnosticsForTick(1)!.Systems.Select(system => system.SystemName));
        }

        [Fact]
        public void ExecuteTick_RecordsSystemDiagnosticsAndEmittedEventCounts()
        {
            SimulationEngine engine = new([new EmittingSystem()]);
            SimulationState state = CreateState();

            engine.ExecuteTick(state);

            var diagnostics = state.GetDiagnosticsForTick(1);
            Assert.NotNull(diagnostics);
            Assert.Equal(1, diagnostics.Tick);
            Assert.True(diagnostics.DurationMilliseconds >= 0);
            Assert.Contains(state.GetEventsForTick(1), simulationEvent => simulationEvent.Type == "test.event");

            var system = Assert.Single(diagnostics.Systems);
            Assert.Equal("Emitter", system.SystemName);
            Assert.Equal(1, system.EmittedEventCount);
        }

        [Fact]
        public void ExecuteTick_StoresEventsPerTickAndClearsCurrentBufferAtNextTickStart()
        {
            SimulationEngine engine = new([new EmittingSystem()]);
            SimulationState state = CreateState();

            engine.ExecuteTick(state);
            IReadOnlyList<string> tickOneMessages = state.CurrentTickEvents.Select(simulationEvent => simulationEvent.Message).ToList();

            engine.ExecuteTick(state);

            Assert.Contains("Emitted at 1.", tickOneMessages);
            Assert.Contains(state.GetEventsForTick(1), simulationEvent => simulationEvent.Message == "Emitted at 1.");
            Assert.Contains(state.GetEventsForTick(2), simulationEvent => simulationEvent.Message == "Emitted at 2.");
            Assert.DoesNotContain(state.CurrentTickEvents, simulationEvent => simulationEvent.Message == "Emitted at 1.");
        }

        [Fact]
        public void ExecuteTick_TrimsEventAndDiagnosticHistoryToConfiguredLimit()
        {
            SimulationEngine engine = new([new EmittingSystem()]);
            SimulationState state = CreateState(new SimulationConfig(EventHistoryLimit: 1));

            engine.Run(state, maxTicks: 3);

            Assert.Equal([3L], state.EventHistory.Keys);
            Assert.Equal([3L], state.DiagnosticsHistory.Keys);
        }

        [Fact]
        public void SeededRuns_ProduceRepeatableEventsAndState()
        {
            static (float Hunger, IReadOnlyList<string> Events) RunSeeded()
            {
                var (state, systems) = TabulaRasa.Simulation.Composition.MinimalSimulationFactory.Create(
                    new SimulationConfig(Seed: 99));
                new SimulationEngine(systems).Run(state, maxTicks: 2);

                return (
                    state.Agents.Single().NeedState.Hunger,
                    state.GetRecentEvents().Select(simulationEvent => $"{simulationEvent.Tick}:{simulationEvent.Type}:{simulationEvent.Message}").ToList());
            }

            var first = RunSeeded();
            var second = RunSeeded();

            Assert.Equal(first.Hunger, second.Hunger);
            Assert.Equal(first.Events, second.Events);
        }

        private static SimulationState CreateState(SimulationConfig? config = null)
        {
            return new SimulationState(new WorldState(), new SimulationTime(0), new List<AgentState>(), config);
        }

        private sealed class RecordingSystem(
            string name,
            SimulationPhase phase,
            int priority,
            List<string> order) : ISystem
        {
            public string Name => name;
            public SimulationPhase Phase => phase;
            public int Priority => priority;

            public void Execute(SimulationState state)
            {
                order.Add(Name);
            }
        }

        private sealed class EmittingSystem : ISystem
        {
            public string Name => "Emitter";
            public SimulationPhase Phase => SimulationPhase.Evaluation;
            public int Priority => 0;

            public void Execute(SimulationState state)
            {
                state.EmitEvent("test.event", Name, $"Emitted at {state.Time.Tick + 1}.");
            }
        }
    }
}
