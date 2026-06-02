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

        public void ApplyConfig(SimulationConfig config, bool reseedRandom = false)
        {
            Config = NormalizeConfig(config);
            if (reseedRandom)
            {
                Random = new Random(Config.Seed);
            }

            TrimHistory(_eventsByTick, Config.EventHistoryLimit);
            TrimHistory(_diagnosticsByTick, Config.EventHistoryLimit);
        }

        private static SimulationConfig NormalizeConfig(SimulationConfig config)
        {
            NeedDecayConfig needDecay = config.EffectiveNeedDecay;
            PathfindingConfig pathfinding = config.EffectivePathfinding;
            IReadOnlyList<string> enabledSystems = config.EffectiveEnabledSystems
                .Where(systemId => !string.IsNullOrWhiteSpace(systemId))
                .Select(systemId => systemId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return config with
            {
                WorldWidth = Math.Clamp(config.WorldWidth, 1, 500),
                WorldHeight = Math.Clamp(config.WorldHeight, 1, 500),
                InitialAgentCount = Math.Clamp(config.InitialAgentCount, 0, 10_000),
                InitialFoodCount = Math.Clamp(config.InitialFoodCount, 0, 10_000),
                EventHistoryLimit = Math.Clamp(config.EventHistoryLimit, 1, 10_000),
                SnapshotHistoryLimit = Math.Clamp(config.SnapshotHistoryLimit, 1, 10_000),
                TickIntervalMilliseconds = Math.Clamp(config.TickIntervalMilliseconds, 50, 60_000),
                NeedDecay = new NeedDecayConfig(
                    ClampFinite(needDecay.HungerDelta, -100, 100),
                    ClampFinite(needDecay.ThirstDelta, -100, 100),
                    ClampFinite(needDecay.EnergyDelta, -100, 100)),
                PerceptionRadius = ClampFinite(config.PerceptionRadius, 0, 1_000),
                MovementSpeedPerTick = ClampFinite(config.MovementSpeedPerTick, 0.01f, 100),
                Pathfinding = new PathfindingConfig(
                    pathfinding.AllowDiagonalMovement,
                    Math.Clamp(pathfinding.MaxVisitedCells, 1, 1_000_000),
                    Math.Clamp(pathfinding.MaxRepathAttempts, 0, 1_000)),
                EnabledSystems = enabledSystems.Count == 0
                    ? SimulationConfig.DefaultEnabledSystems
                    : enabledSystems
            };
        }

        private static float ClampFinite(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return min;
            }

            return Math.Clamp(value, min, max);
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
