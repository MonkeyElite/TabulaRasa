using System.Diagnostics.CodeAnalysis;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Observability;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.State
{
    public sealed class SimulationState
    {
        public required WorldState World { get; set; }
        public required SimulationTime Time { get; set; }
        public List<AgentState> Agents { get; set; } = [];
        public List<AgentIntent> PendingIntents { get; } = [];
        public List<ActionRequest> PendingActionRequests { get; } = [];
        public List<ActionResult> ActionResults { get; } = [];
        public List<ActiveMovement> ActiveMovements { get; } = [];
        public List<JobInstance> PendingJobs { get; } = [];
        public List<JobInstance> ActiveJobs { get; } = [];
        public ReservationRegistry Reservations { get; } = new();
        public SimulationConfig Config { get; private set; }
        public Random Random { get; private set; }
        public IReadOnlyList<SimulationEvent> CurrentTickEvents => _currentTickEvents;
        public IReadOnlyDictionary<long, IReadOnlyList<SimulationEvent>> EventHistory => _eventsByTick;
        public IReadOnlyDictionary<long, SimulationTickDiagnostics> DiagnosticsHistory => _diagnosticsByTick;

        public bool IsRunning { get; set; } = true;

        private readonly List<SimulationEvent> _currentTickEvents = [];
        private readonly SortedDictionary<long, IReadOnlyList<SimulationEvent>> _eventsByTick = [];
        private readonly SortedDictionary<long, SimulationTickDiagnostics> _diagnosticsByTick = [];
        private long _eventSequence;
        private long _activeEventTick;

        [SetsRequiredMembers]
        public SimulationState(
            WorldState world,
            SimulationTime time,
            List<AgentState> agentStates,
            SimulationConfig? config = null)
        {
            World = world;
            Time = time;
            Agents = agentStates;
            Config = NormalizeConfig(config ?? new SimulationConfig());
            Random = new Random(Config.Seed);
            _activeEventTick = time.Tick;
        }

        public AgentState? GetAgentById(string id)
        {
            AgentState? agent = Agents.Find(a => a.Id == id);

            return agent;
        }

        public void BeginTickObservability(long tick)
        {
            _activeEventTick = tick;
            _eventSequence = 0;
            _currentTickEvents.Clear();
        }

        public SimulationEvent EmitEvent(
            string type,
            string sourceSystem,
            string message,
            string? entityId = null,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            SimulationEvent simulationEvent = new(
                _activeEventTick,
                ++_eventSequence,
                type,
                sourceSystem,
                message,
                entityId,
                metadata ?? new Dictionary<string, string>());

            _currentTickEvents.Add(simulationEvent);

            return simulationEvent;
        }

        public SimulationEvent RecordEvent(
            long tick,
            string type,
            string sourceSystem,
            string message,
            string? entityId = null,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            List<SimulationEvent> events = _eventsByTick.TryGetValue(tick, out IReadOnlyList<SimulationEvent>? existingEvents)
                ? existingEvents.ToList()
                : [];
            long sequence = events.Count == 0 ? 1 : events.Max(simulationEvent => simulationEvent.Sequence) + 1;
            SimulationEvent simulationEvent = new(
                tick,
                sequence,
                type,
                sourceSystem,
                message,
                entityId,
                metadata ?? new Dictionary<string, string>());

            events.Add(simulationEvent);
            _eventsByTick[tick] = events;
            TrimHistory(_eventsByTick, Config.EventHistoryLimit);

            return simulationEvent;
        }

        public void CompleteTickObservability(long tick, SimulationTickDiagnostics diagnostics)
        {
            _eventsByTick[tick] = _currentTickEvents.ToList();
            _diagnosticsByTick[tick] = diagnostics;
            TrimHistory(_eventsByTick, Config.EventHistoryLimit);
            TrimHistory(_diagnosticsByTick, Config.EventHistoryLimit);
        }

        public IReadOnlyList<SimulationEvent> GetEventsForTick(long tick)
        {
            return _eventsByTick.GetValueOrDefault(tick) ?? [];
        }

        public SimulationTickDiagnostics? GetDiagnosticsForTick(long tick)
        {
            return _diagnosticsByTick.GetValueOrDefault(tick);
        }

        public IReadOnlyList<SimulationEvent> GetRecentEvents()
        {
            return _eventsByTick.Values.SelectMany(events => events).ToList();
        }

        public void ApplyConfig(SimulationConfig config)
        {
            Config = NormalizeConfig(config);
            Random = new Random(Config.Seed);
            TrimHistory(_eventsByTick, Config.EventHistoryLimit);
            TrimHistory(_diagnosticsByTick, Config.EventHistoryLimit);
        }

        private static SimulationConfig NormalizeConfig(SimulationConfig config)
        {
            return config with
            {
                EventHistoryLimit = Math.Clamp(config.EventHistoryLimit, 1, 10_000),
                TickIntervalMilliseconds = Math.Clamp(config.TickIntervalMilliseconds, 50, 60_000)
            };
        }

        private static void TrimHistory<T>(SortedDictionary<long, T> history, int limit)
        {
            while (history.Count > limit)
            {
                history.Remove(history.Keys.First());
            }
        }
    }
}
