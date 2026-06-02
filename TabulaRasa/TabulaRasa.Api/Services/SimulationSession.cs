using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Lifecycle;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.Api.Services
{
    public sealed class SimulationSession : IDisposable
    {
        private readonly object _sync = new();
        private readonly SortedDictionary<long, SimulationSnapshotDto> _snapshots = [];

        private SimulationState _state;
        private IReadOnlyList<ISystem> _systems;
        private SimulationEngine _engine;
        private Timer? _timer;
        private SimulationConfig _config;
        private SimulationLifecycleState _lifecycle = SimulationLifecycleState.Idle;
        private bool _disposed;

        public SimulationSession(string simulationId, string name, SimulationConfig? config = null)
        {
            SimulationId = simulationId;
            Name = string.IsNullOrWhiteSpace(name) ? simulationId : name.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = CreatedAt;
            _config = config ?? new SimulationConfig();
            (_state, _systems) = MinimalSimulationFactory.Create(_config);
            _config = _state.Config;
            _engine = new SimulationEngine(_systems);
            RecordLifecycleEvent(_lifecycle);
            StoreCurrentSnapshot();
        }

        public string SimulationId { get; }
        public string Name { get; private set; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset UpdatedAt { get; private set; }

        public bool IsRunning
        {
            get
            {
                lock (_sync)
                {
                    return _lifecycle == SimulationLifecycleState.Running;
                }
            }
        }

        public SimulationSummaryDto GetSummary()
        {
            lock (_sync)
            {
                return new SimulationSummaryDto(
                    SimulationId,
                    Name,
                    _lifecycle.ToString(),
                    _state.Time.Tick,
                    _state.World.Grid.Width,
                    _state.World.Grid.Height,
                    _state.World.Agents.Count,
                    _state.World.Foods.Count,
                    CreatedAt,
                    UpdatedAt);
            }
        }

        public SimulationStatusDto GetStatus()
        {
            lock (_sync)
            {
                return ToStatus();
            }
        }

        public SimulationSnapshotDto GetCurrentSnapshot()
        {
            lock (_sync)
            {
                return _snapshots[_state.Time.Tick];
            }
        }

        public SimulationSnapshotDto? GetSnapshot(long tick)
        {
            lock (_sync)
            {
                return _snapshots.GetValueOrDefault(tick);
            }
        }

        public SimulationSnapshotDto Step()
        {
            lock (_sync)
            {
                EnsureCanStep();
                return ExecuteAndStoreTick();
            }
        }

        public SimulationStatusDto Run(int intervalMilliseconds, SimulationConfigDto? config = null)
        {
            lock (_sync)
            {
                EnsureCanRun();
                if (config is not null)
                {
                    ApplyPausedConfig(config);
                }

                int safeInterval = Math.Clamp(intervalMilliseconds, 50, 60_000);
                _config = _config with { TickIntervalMilliseconds = safeInterval };
                _state.ApplyConfig(_config);
                _config = _state.Config;

                _timer?.Dispose();
                _timer = new Timer(_ => StepFromTimer(), null, safeInterval, safeInterval);
                SetLifecycle(SimulationLifecycleState.Running);

                return ToStatus();
            }
        }

        public SimulationStatusDto Pause()
        {
            lock (_sync)
            {
                if (_lifecycle != SimulationLifecycleState.Running)
                {
                    throw new InvalidOperationException("Only a running simulation can be paused.");
                }

                StopTimer();
                SetLifecycle(SimulationLifecycleState.Paused);

                return ToStatus();
            }
        }

        public SimulationStatusDto Stop()
        {
            lock (_sync)
            {
                if (_lifecycle == SimulationLifecycleState.Stopped)
                {
                    return ToStatus();
                }

                StopTimer();
                SetLifecycle(SimulationLifecycleState.Stopped);

                return ToStatus();
            }
        }

        public SimulationStatusDto UpdateConfig(SimulationConfigDto config)
        {
            lock (_sync)
            {
                if (_lifecycle == SimulationLifecycleState.Running)
                {
                    throw new InvalidOperationException("Configuration can only be updated while paused, idle, or stopped.");
                }

                ApplyPausedConfig(config);
                return ToStatus();
            }
        }

        public SimulationSnapshotDto Reset(SimulationConfigDto? config = null)
        {
            lock (_sync)
            {
                StopTimer();
                _config = SimulationSnapshotMapper.ToConfig(config, _config);
                (_state, _systems) = MinimalSimulationFactory.Create(_config);
                _config = _state.Config;
                _engine = new SimulationEngine(_systems);
                _snapshots.Clear();
                _lifecycle = SimulationLifecycleState.Idle;
                RecordLifecycleEvent(_lifecycle);

                return StoreCurrentSnapshot();
            }
        }

        public SimulationDraftDto GetDraft()
        {
            lock (_sync)
            {
                return SimulationSnapshotMapper.ToDraft(_state);
            }
        }

        public SimulationDraftDto? GetDraft(long tick)
        {
            lock (_sync)
            {
                SimulationSnapshotDto? snapshot = _snapshots.GetValueOrDefault(tick);
                return snapshot is null
                    ? null
                    : SimulationSnapshotMapper.ToDraft(snapshot, SimulationSnapshotMapper.ToConfig(_config));
            }
        }

        public SimulationDraftSchemaDto GetDraftSchema()
        {
            return SimulationDraftSchemaFactory.Create();
        }

        public RestartFromDraftResult RestartFromDraft(SimulationDraftDto draft)
        {
            Dictionary<string, string[]> errors = ValidateDraft(draft);

            if (errors.Count > 0)
            {
                return RestartFromDraftResult.Failure(errors);
            }

            lock (_sync)
            {
                StopTimer();

                _config = SimulationSnapshotMapper.ToConfig(draft.Config, _config);
                _state = BuildStateFromDraft(draft, _config);
                _config = _state.Config;
                _systems = MinimalSimulationFactory.BuildSystems(_config);
                _engine = new SimulationEngine(_systems);
                _snapshots.Clear();
                _lifecycle = SimulationLifecycleState.Idle;
                RecordLifecycleEvent(_lifecycle);

                return RestartFromDraftResult.Success(StoreCurrentSnapshot());
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                StopTimer();
            }
        }

        private void ApplyPausedConfig(SimulationConfigDto dto)
        {
            SimulationConfig requested = SimulationSnapshotMapper.ToConfig(dto, _config);
            if (_state.Time.Tick > 0 && RequiresRebuild(_config, requested))
            {
                throw new InvalidOperationException("Seed, world size, and initial entity counts require reset, create, or clone.");
            }

            _config = requested;
            _state.ApplyConfig(_config);
            _config = _state.Config;
            _systems = MinimalSimulationFactory.BuildSystems(_config);
            _engine = new SimulationEngine(_systems);
            StoreCurrentSnapshot();
        }

        private static bool RequiresRebuild(SimulationConfig current, SimulationConfig requested)
        {
            return current.Seed != requested.Seed
                || current.WorldWidth != requested.WorldWidth
                || current.WorldHeight != requested.WorldHeight
                || current.InitialAgentCount != requested.InitialAgentCount
                || current.InitialFoodCount != requested.InitialFoodCount;
        }

        private SimulationSnapshotDto StoreCurrentSnapshot()
        {
            SimulationSnapshotDto snapshot = SimulationSnapshotMapper.ToSnapshot(_state);
            _snapshots[_state.Time.Tick] = snapshot;
            TrimSnapshots();
            UpdatedAt = DateTimeOffset.UtcNow;

            return snapshot;
        }

        private SimulationSnapshotDto ExecuteAndStoreTick()
        {
            _engine.ExecuteTick(_state);
            return StoreCurrentSnapshot();
        }

        private void StepFromTimer()
        {
            if (!Monitor.TryEnter(_sync))
            {
                return;
            }

            try
            {
                if (_lifecycle == SimulationLifecycleState.Running)
                {
                    ExecuteAndStoreTick();
                }
            }
            finally
            {
                Monitor.Exit(_sync);
            }
        }

        private SimulationStatusDto ToStatus()
        {
            long minimumTick = _snapshots.Count == 0 ? _state.Time.Tick : _snapshots.Keys.First();
            long maximumTick = _snapshots.Count == 0 ? _state.Time.Tick : _snapshots.Keys.Last();

            return new SimulationStatusDto(
                _state.Time.Tick,
                _lifecycle.ToString(),
                minimumTick,
                maximumTick,
                _state.World.Grid.Width,
                _state.World.Grid.Height,
                _state.World.Agents.Count,
                _state.World.Foods.Count,
                SimulationSnapshotMapper.ToConfig(_config),
                ToLatestTickSummary(),
                _state.EventHistory.Count == 0 ? null : _state.EventHistory.Keys.First(),
                _state.EventHistory.Count == 0 ? null : _state.EventHistory.Keys.Last());
        }

        private void StopTimer()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private void SetLifecycle(SimulationLifecycleState lifecycle)
        {
            _lifecycle = lifecycle;
            RecordLifecycleEvent(lifecycle);
            StoreCurrentSnapshot();
        }

        private void RecordLifecycleEvent(SimulationLifecycleState lifecycle)
        {
            _state.RecordEvent(
                _state.Time.Tick,
                "lifecycle.changed",
                nameof(SimulationSession),
                $"Simulation lifecycle changed to {lifecycle}.",
                metadata: new Dictionary<string, string>
                {
                    ["state"] = lifecycle.ToString()
                });
        }

        private void EnsureCanRun()
        {
            if (_lifecycle is not SimulationLifecycleState.Idle and not SimulationLifecycleState.Paused)
            {
                throw new InvalidOperationException("Simulation can only run from idle or paused.");
            }
        }

        private void EnsureCanStep()
        {
            if (_lifecycle == SimulationLifecycleState.Stopped)
            {
                throw new InvalidOperationException("Stopped simulations cannot be stepped. Reset or restart from draft first.");
            }

            if (_lifecycle == SimulationLifecycleState.Running)
            {
                throw new InvalidOperationException("Running simulations cannot be manually stepped. Pause first.");
            }
        }

        private SimulationTickSummaryDto? ToLatestTickSummary()
        {
            if (_state.DiagnosticsHistory.Count == 0)
            {
                return null;
            }

            var diagnostics = _state.DiagnosticsHistory.Values.Last();

            return new SimulationTickSummaryDto(
                diagnostics.Tick,
                diagnostics.DurationMilliseconds,
                diagnostics.EventCount);
        }

        private void TrimSnapshots()
        {
            while (_snapshots.Count > _config.SnapshotHistoryLimit)
            {
                _snapshots.Remove(_snapshots.Keys.First());
            }
        }

        private static SimulationState BuildStateFromDraft(SimulationDraftDto draft, SimulationConfig config)
        {
            GridMap grid = new(draft.Grid.Width, draft.Grid.Height);

            foreach (GridCellDto blockedCell in draft.Grid.BlockedCells)
            {
                grid.SetTraversable(new GridCell(blockedCell.X, blockedCell.Y), false);
            }

            foreach (EditableGridTerrainCellDto terrainCell in draft.Grid.TerrainCells)
            {
                GridTerrainType terrainType = Enum.Parse<GridTerrainType>(
                    terrainCell.TerrainType,
                    ignoreCase: true);
                grid.SetTerrain(
                    new GridCell(terrainCell.Cell.X, terrainCell.Cell.Y),
                    terrainType);
            }

            List<AgentEntity> agents = draft.Agents.Select(agent => new AgentEntity
            {
                Id = agent.Id.Trim(),
                Position = ToWorldPosition(agent.Position)
            }).ToList();

            List<AgentState> agentStates = draft.Agents.Select(agent => new AgentState(
                agent.Id.Trim(),
                new AgentNeedState
                {
                    Hunger = agent.Needs.Hunger,
                    Thirst = agent.Needs.Thirst,
                    Energy = agent.Needs.Energy
                },
                new DefaultAgentMind())).ToList();

            List<FoodEntity> foods = draft.Food.Select(food => new FoodEntity
            {
                Id = food.Id.Trim(),
                Position = ToWorldPosition(food.Position),
                IsConsumed = food.IsConsumed
            }).ToList();

            WorldState world = WorldFactory.Create(agents, foods, grid);
            SimulationConfig draftConfig = config with
            {
                WorldWidth = draft.Grid.Width,
                WorldHeight = draft.Grid.Height,
                InitialAgentCount = agents.Count,
                InitialFoodCount = foods.Count
            };

            return new SimulationState(world, new SimulationTime((int)draft.Tick), agentStates, draftConfig);
        }

        public static Dictionary<string, string[]> ValidateDraft(SimulationDraftDto? draft)
        {
            Dictionary<string, List<string>> errors = [];

            if (draft is null)
            {
                errors["draft"] = ["Draft is required."];
                return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            }

            AddIf(draft.Tick < 0, nameof(draft.Tick), "Tick must be zero or greater.");
            AddIf(draft.Tick > int.MaxValue, nameof(draft.Tick), "Tick must fit within the simulation time range.");

            if (draft.Grid is null)
            {
                Add("grid", "Grid is required.");
                return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            }

            if (draft.Agents is null)
            {
                Add("agents", "Agents are required.");
                return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            }

            if (draft.Food is null)
            {
                Add("food", "Food is required.");
                return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            }

            if (draft.Grid.BlockedCells is null)
            {
                Add("grid.blockedCells", "Blocked cells are required.");
                return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            }

            if (draft.Grid.TerrainCells is null)
            {
                Add("grid.terrainCells", "Terrain cells are required.");
                return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            }

            AddIf(draft.Grid.Width <= 0, "grid.width", "Grid width must be greater than zero.");
            AddIf(draft.Grid.Height <= 0, "grid.height", "Grid height must be greater than zero.");

            HashSet<string> agentIds = new(StringComparer.Ordinal);
            HashSet<string> foodIds = new(StringComparer.Ordinal);

            for (int i = 0; i < draft.Agents.Count; i++)
            {
                EditableAgentDto agent = draft.Agents[i];
                string prefix = $"agents[{i}]";

                ValidateId(agent.Id, $"{prefix}.id", agentIds);
                ValidatePosition(agent.Position, $"{prefix}.position", draft.Grid.Width, draft.Grid.Height);
                ValidateFinite(agent.Needs.Hunger, $"{prefix}.needs.hunger");
                ValidateFinite(agent.Needs.Thirst, $"{prefix}.needs.thirst");
                ValidateFinite(agent.Needs.Energy, $"{prefix}.needs.energy");
            }

            for (int i = 0; i < draft.Food.Count; i++)
            {
                EditableFoodDto food = draft.Food[i];
                string prefix = $"food[{i}]";

                ValidateId(food.Id, $"{prefix}.id", foodIds);
                ValidatePosition(food.Position, $"{prefix}.position", draft.Grid.Width, draft.Grid.Height);
            }

            for (int i = 0; i < draft.Grid.BlockedCells.Count; i++)
            {
                GridCellDto cell = draft.Grid.BlockedCells[i];
                AddIf(
                    cell.X < 0 || cell.Y < 0 || cell.X >= draft.Grid.Width || cell.Y >= draft.Grid.Height,
                    $"grid.blockedCells[{i}]",
                    "Blocked cell must be inside the grid.");
            }

            HashSet<GridCell> terrainCells = [];

            for (int i = 0; i < draft.Grid.TerrainCells.Count; i++)
            {
                EditableGridTerrainCellDto terrainCell = draft.Grid.TerrainCells[i];
                string prefix = $"grid.terrainCells[{i}]";
                GridCell cell = new(terrainCell.Cell.X, terrainCell.Cell.Y);

                AddIf(
                    cell.X < 0 || cell.Y < 0 || cell.X >= draft.Grid.Width || cell.Y >= draft.Grid.Height,
                    prefix,
                    "Terrain cell must be inside the grid.");

                if (!terrainCells.Add(cell))
                {
                    Add(prefix, "Terrain cell must be unique.");
                }

                if (!Enum.TryParse(terrainCell.TerrainType, ignoreCase: true, out GridTerrainType _))
                {
                    Add($"{prefix}.terrainType", "Terrain type is invalid.");
                }
            }

            return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());

            void ValidateId(string? id, string key, HashSet<string> ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    Add(key, "Id is required.");
                    return;
                }

                if (!ids.Add(id.Trim()))
                {
                    Add(key, "Id must be unique.");
                }
            }

            void ValidatePosition(PositionDto position, string key, int width, int height)
            {
                ValidateFinite(position.X, $"{key}.x");
                ValidateFinite(position.Y, $"{key}.y");
                AddIf(
                    position.X < 0 || position.Y < 0 || position.X >= width || position.Y >= height,
                    key,
                    "Position must be inside the grid.");
            }

            void ValidateFinite(float value, string key)
            {
                AddIf(float.IsNaN(value) || float.IsInfinity(value), key, "Value must be finite.");
            }

            void AddIf(bool condition, string key, string message)
            {
                if (condition)
                {
                    Add(key, message);
                }
            }

            void Add(string key, string message)
            {
                if (!errors.TryGetValue(key, out List<string>? messages))
                {
                    messages = [];
                    errors[key] = messages;
                }

                messages.Add(message);
            }
        }

        private static WorldPosition ToWorldPosition(PositionDto position)
        {
            return new WorldPosition(position.X, position.Y);
        }
    }
}
