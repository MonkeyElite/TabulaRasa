using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.Api.Services
{
    public sealed class SimulationSessionService : IDisposable
    {
        private readonly object _sync = new();
        private readonly SortedDictionary<long, SimulationSnapshotDto> _snapshots = [];

        private SimulationState _state;
        private IReadOnlyList<ISystem> _systems;
        private SimulationEngine _engine;
        private Timer? _timer;
        private string _status = "Idle";

        public SimulationSessionService()
        {
            (_state, _systems) = MinimalSimulationFactory.Create();
            _engine = new SimulationEngine(_systems);
            StoreCurrentSnapshot();
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
                _engine.ExecuteTick(_state);
                return StoreCurrentSnapshot();
            }
        }

        public SimulationStatusDto Run(int intervalMilliseconds)
        {
            lock (_sync)
            {
                int safeInterval = Math.Clamp(intervalMilliseconds, 50, 60_000);

                _timer?.Dispose();
                _timer = new Timer(_ => Step(), null, safeInterval, safeInterval);
                _status = "Running";

                return ToStatus();
            }
        }

        public SimulationStatusDto Pause()
        {
            lock (_sync)
            {
                _timer?.Dispose();
                _timer = null;
                _status = "Paused";

                return ToStatus();
            }
        }

        public SimulationSnapshotDto Reset()
        {
            lock (_sync)
            {
                StopTimer();
                (_state, _systems) = MinimalSimulationFactory.Create();
                _engine = new SimulationEngine(_systems);
                _snapshots.Clear();
                _status = "Idle";

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

                _state = BuildStateFromDraft(draft);
                (_, _systems) = MinimalSimulationFactory.Create();
                _engine = new SimulationEngine(_systems);
                _snapshots.Clear();
                _status = "Idle";

                return RestartFromDraftResult.Success(StoreCurrentSnapshot());
            }
        }

        public void Dispose()
        {
            StopTimer();
        }

        private SimulationSnapshotDto StoreCurrentSnapshot()
        {
            SimulationSnapshotDto snapshot = SimulationSnapshotMapper.ToSnapshot(_state);
            _snapshots[_state.Time.Tick] = snapshot;

            return snapshot;
        }

        private SimulationStatusDto ToStatus()
        {
            long minimumTick = _snapshots.Count == 0 ? _state.Time.Tick : _snapshots.Keys.First();
            long maximumTick = _snapshots.Count == 0 ? _state.Time.Tick : _snapshots.Keys.Last();

            return new SimulationStatusDto(
                _state.Time.Tick,
                _status,
                minimumTick,
                maximumTick,
                _state.World.Grid.Width,
                _state.World.Grid.Height,
                _state.World.Agents.Count,
                _state.World.Foods.Count);
        }

        private void StopTimer()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private static SimulationState BuildStateFromDraft(SimulationDraftDto draft)
        {
            GridMap grid = new(draft.Grid.Width, draft.Grid.Height);

            foreach (GridCellDto blockedCell in draft.Grid.BlockedCells)
            {
                grid.SetTraversable(new GridCell(blockedCell.X, blockedCell.Y), false);
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

            return new SimulationState(world, new SimulationTime((int)draft.Tick), agentStates);
        }

        private static Dictionary<string, string[]> ValidateDraft(SimulationDraftDto? draft)
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
