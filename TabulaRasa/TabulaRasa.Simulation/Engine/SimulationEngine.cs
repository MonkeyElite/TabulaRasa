using System.Diagnostics;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Observability;
using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Engine
{
    public sealed class SimulationEngine
    {
        private readonly List<ISystem> _systems = [];

        public SimulationEngine(IEnumerable<ISystem> systems)
        {
            _systems = systems
                .OrderBy(s => s.Phase)
                .ThenBy(s => s.Priority)
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .ToList();
        }

        public void Run(SimulationState state, int maxTicks)
        {
            for (int tick = 0; tick < maxTicks; tick++)
            {
                ExecuteTick(state);
            }
        }

        public void ExecuteTick(SimulationState state)
        {
            long completedTick = state.Time.Tick + 1;
            DateTimeOffset startedAt = DateTimeOffset.UtcNow;
            Stopwatch tickTimer = Stopwatch.StartNew();
            List<SystemExecutionDiagnostic> systemDiagnostics = [];

            state.BeginTickObservability(completedTick);
            state.EmitEvent(
                "tick.started",
                nameof(SimulationEngine),
                $"Tick {completedTick} started.");

            foreach (var system in _systems)
            {
                int eventsBeforeSystem = state.CurrentTickEvents.Count;
                Stopwatch systemTimer = Stopwatch.StartNew();

                try
                {
                    system.Execute(state);
                    systemTimer.Stop();

                    int emittedEventCount = state.CurrentTickEvents.Count - eventsBeforeSystem;
                    systemDiagnostics.Add(new SystemExecutionDiagnostic(
                        system.Phase,
                        system.Name,
                        system.Priority,
                        systemTimer.Elapsed.TotalMilliseconds,
                        emittedEventCount));

                    state.EmitEvent(
                        "system.completed",
                        system.Name,
                        $"{system.Name} completed.",
                        metadata: new Dictionary<string, string>
                        {
                            ["phase"] = system.Phase.ToString(),
                            ["durationMilliseconds"] = systemTimer.Elapsed.TotalMilliseconds.ToString("0.###")
                        });
                }
                catch (Exception exception)
                {
                    systemTimer.Stop();
                    state.EmitEvent(
                        "system.failed",
                        system.Name,
                        $"{system.Name} failed: {exception.Message}",
                        metadata: new Dictionary<string, string>
                        {
                            ["phase"] = system.Phase.ToString(),
                            ["durationMilliseconds"] = systemTimer.Elapsed.TotalMilliseconds.ToString("0.###")
                        });

                    systemDiagnostics.Add(new SystemExecutionDiagnostic(
                        system.Phase,
                        system.Name,
                        system.Priority,
                        systemTimer.Elapsed.TotalMilliseconds,
                        state.CurrentTickEvents.Count - eventsBeforeSystem));

                    throw;
                }
            }

            state.Time = new SimulationTime(state.Time.Tick + 1);
            tickTimer.Stop();
            state.EmitEvent(
                "tick.completed",
                nameof(SimulationEngine),
                $"Tick {completedTick} completed.");

            SimulationTickDiagnostics diagnostics = new(
                completedTick,
                startedAt,
                DateTimeOffset.UtcNow,
                tickTimer.Elapsed.TotalMilliseconds,
                state.CurrentTickEvents.Count,
                systemDiagnostics);

            state.CompleteTickObservability(completedTick, diagnostics);
        }
    }
}
