using TabulaRasa.Abstractions.Spatial.Grid;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Models;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Persistence;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Lifecycle;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Resources;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.State;

namespace TabulaRasa.Api.Services
{
    public sealed class SimulationSession : IDisposable
    {
        private readonly object _sync = new();
        private readonly SortedDictionary<long, SimulationSnapshotDto> _snapshots = [];
        private readonly ISimulationPersistenceStore _persistence;

        private SimulationState _state;
        private IReadOnlyList<ISystem> _systems;
        private SimulationEngine _engine;
        private Timer? _timer;
        private SimulationConfig _config;
        private SimulationLifecycleState _lifecycle = SimulationLifecycleState.Idle;
        private bool _disposed;

        public SimulationSession(
            string simulationId,
            string name,
            SimulationConfig? config = null,
            ISimulationPersistenceStore? persistence = null)
        {
            _persistence = persistence ?? new NullSimulationPersistenceStore();
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
                    _state.World.Agents.Count(agent => !agent.IsDead),
                    _state.World.Agents.Count(agent => agent.IsDead),
                    _state.World.ResourceContainers.Count,
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
                return _snapshots.GetValueOrDefault(tick) ?? ReconstructSnapshot(tick);
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

        public SaveSimulationResponseDto Save()
        {
            lock (_sync)
            {
                SimulationSnapshotDto snapshot = _snapshots[_state.Time.Tick];
                return SaveCheckpoint(snapshot);
            }
        }

        public ScenarioExportDto ExportScenario()
        {
            lock (_sync)
            {
                SimulationDraftDto draft = SimulationSnapshotMapper.ToDraft(_state);
                return _persistence.SaveScenario(Name, draft, []);
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
                || current.InitialFoodCount != requested.InitialFoodCount
                || current.EffectiveSpeciesPopulation.Human != requested.EffectiveSpeciesPopulation.Human
                || current.EffectiveSpeciesPopulation.Deer != requested.EffectiveSpeciesPopulation.Deer
                || current.EffectiveSpeciesPopulation.Wolf != requested.EffectiveSpeciesPopulation.Wolf
                || current.EffectiveEcology.InitialPlantCount != requested.EffectiveEcology.InitialPlantCount
                || current.EffectiveEcology.InitialWaterSourceCount != requested.EffectiveEcology.InitialWaterSourceCount
                || current.EffectiveEcology.InitialResourceDepositCount != requested.EffectiveEcology.InitialResourceDepositCount;
        }

        private SimulationSnapshotDto StoreCurrentSnapshot()
        {
            SimulationSnapshotDto snapshot = SimulationSnapshotMapper.ToSnapshot(_state, _snapshots.Values.ToList());
            _snapshots[_state.Time.Tick] = snapshot;
            TrimSnapshots();
            UpdatedAt = DateTimeOffset.UtcNow;
            PersistSnapshot(snapshot);

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
                _state.World.Agents.Count(agent => !agent.IsDead),
                _state.World.Agents.Count(agent => agent.IsDead),
                _state.World.ResourceContainers.Count,
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
            SaveCheckpoint(StoreCurrentSnapshot());
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

        private void PersistSnapshot(SimulationSnapshotDto snapshot)
        {
            if (!_persistence.IsDurable)
            {
                return;
            }

            _persistence.UpsertRun(GetSummary(), SimulationSnapshotMapper.ToConfig(_config));
            _persistence.SaveTick(SimulationId, snapshot);
            if (ShouldStoreCheckpoint(snapshot.Tick))
            {
                SaveCheckpoint(snapshot);
            }
        }

        private bool ShouldStoreCheckpoint(long tick)
        {
            int interval = Math.Max(1, _persistence.Options.CheckpointIntervalTicks);
            return tick == 0 || tick % interval == 0;
        }

        private SaveSimulationResponseDto SaveCheckpoint(SimulationSnapshotDto snapshot)
        {
            SimulationDraftDto draft = SimulationSnapshotMapper.ToDraft(snapshot, SimulationSnapshotMapper.ToConfig(_config));
            SimulationStateCheckpointDto checkpoint = new(
                snapshot.Tick,
                _lifecycle.ToString(),
                SimulationSnapshotMapper.ToConfig(_config),
                snapshot,
                draft,
                DateTimeOffset.UtcNow);

            return _persistence.SaveCheckpoint(SimulationId, checkpoint);
        }

        private SimulationSnapshotDto? ReconstructSnapshot(long tick)
        {
            if (!_persistence.IsDurable)
            {
                return null;
            }

            SimulationStateCheckpointDto? checkpoint = _persistence.GetNearestCheckpoint(SimulationId, tick);
            if (checkpoint is null)
            {
                return null;
            }

            if (checkpoint.Tick == tick)
            {
                return checkpoint.Snapshot;
            }

            SimulationSession replay = new(
                $"{SimulationId}-replay",
                $"{Name} replay",
                SimulationSnapshotMapper.ToConfig(checkpoint.Config, _config));
            RestartFromDraftResult restart = replay.RestartFromDraft(checkpoint.Draft);
            if (!restart.Succeeded)
            {
                return null;
            }

            while (replay.GetStatus().CurrentTick < tick)
            {
                replay.Step();
            }

            return replay.GetCurrentSnapshot();
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
                Position = ToWorldPosition(agent.Position),
                Inventory = ToInventory(agent.Inventory),
                SpeciesId = SpeciesRegistry.NormalizeId(agent.SpeciesId),
                AgeTicks = agent.AgeTicks,
                BornTick = agent.BornTick,
                LastReproducedTick = agent.LastReproducedTick,
                DeathTick = agent.DeathTick,
                DeathCause = agent.DeathCause,
                IsDead = agent.DeathTick is not null,
                Traits = ToAgentTraits(agent.Traits),
                Health = new EntityHealth(
                    SpeciesRegistry.Get(agent.SpeciesId).MaxHealth,
                    agent.DeathTick is null ? SpeciesRegistry.Get(agent.SpeciesId).MaxHealth : 0),
                ParentIds = (agent.ParentIds ?? []).ToList(),
                OffspringIds = (agent.OffspringIds ?? []).ToList()
            }).ToList();

            List<AgentState> agentStates = draft.Agents.Select(agent => new AgentState(
                agent.Id.Trim(),
                new AgentNeedState
                {
                    Hunger = agent.Needs.Hunger,
                    Thirst = agent.Needs.Thirst,
                    Energy = agent.Needs.Energy,
                    Fatigue = agent.Needs.Fatigue
                },
                new DefaultAgentMind())).ToList();

            List<ResourceContainerEntity> resourceContainers = draft.ResourceContainers.Select(container => new ResourceContainerEntity
            {
                Id = container.Id.Trim(),
                Position = ToWorldPosition(container.Position),
                Inventory = ToInventory(container.Inventory)
            }).ToList();

            bool includeEcologyDraftEntities = ShouldIncludeEcologyDraftEntities(draft);
            List<PlantEntity> plants = (includeEcologyDraftEntities ? draft.Plants ?? [] : []).Select(plant => new PlantEntity
            {
                Id = plant.Id.Trim(),
                Position = ToWorldPosition(plant.Position),
                ResourceId = plant.ResourceId.Trim(),
                Yield = plant.Yield,
                MaxYield = plant.MaxYield,
                RegrowthTicks = plant.RegrowthTicks,
                TicksUntilRegrowth = plant.TicksUntilRegrowth,
                DecayTicksAfterDepleted = plant.DecayTicksAfterDepleted,
                DepletedTicks = plant.DepletedTicks,
                IsDecayed = plant.IsDecayed
            }).ToList();

            List<WaterSourceEntity> waterSources = (includeEcologyDraftEntities ? draft.WaterSources ?? [] : []).Select(water => new WaterSourceEntity
            {
                Id = water.Id.Trim(),
                Position = ToWorldPosition(water.Position),
                CurrentVolume = water.CurrentVolume,
                MaxVolume = water.MaxVolume,
                RefillPerRainTick = water.RefillPerRainTick,
                EvaporationPerHeatTick = water.EvaporationPerHeatTick
            }).ToList();

            List<ResourceDepositEntity> resourceDeposits = (includeEcologyDraftEntities ? draft.ResourceDeposits ?? [] : []).Select(deposit => new ResourceDepositEntity
            {
                Id = deposit.Id.Trim(),
                Position = ToWorldPosition(deposit.Position),
                ResourceId = deposit.ResourceId.Trim(),
                Quantity = deposit.Quantity,
                MaxQuantity = deposit.MaxQuantity
            }).ToList();

            WorldState world = WorldFactory.Create(agents, resourceContainers, grid, plants, waterSources, resourceDeposits);
            world.ResourceDefinitions.Clear();
            world.ResourceDefinitions.AddRange(draft.ResourceDefinitions.Select(ToResourceDefinition));
            SimulationConfig draftConfig = config with
            {
                WorldWidth = draft.Grid.Width,
                WorldHeight = draft.Grid.Height,
                InitialAgentCount = agents.Count(agent => SpeciesRegistry.NormalizeId(agent.SpeciesId) == SpeciesRegistry.HumanId),
                InitialFoodCount = resourceContainers.Count,
                SpeciesPopulation = new SpeciesPopulationConfig(
                    agents.Count(agent => SpeciesRegistry.NormalizeId(agent.SpeciesId) == SpeciesRegistry.HumanId),
                    agents.Count(agent => SpeciesRegistry.NormalizeId(agent.SpeciesId) == SpeciesRegistry.DeerId),
                    agents.Count(agent => SpeciesRegistry.NormalizeId(agent.SpeciesId) == SpeciesRegistry.WolfId)),
                Ecology = config.EffectiveEcology with
                {
                    InitialPlantCount = plants.Count,
                    InitialWaterSourceCount = waterSources.Count,
                    InitialResourceDepositCount = resourceDeposits.Count
                }
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

            if (draft.ResourceDefinitions is null)
            {
                Add("resourceDefinitions", "Resource definitions are required.");
                return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            }

            if (draft.ResourceContainers is null)
            {
                Add("resourceContainers", "Resource containers are required.");
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
            HashSet<string> resourceDefinitionIds = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> resourceContainerIds = new(StringComparer.Ordinal);
            HashSet<string> plantIds = new(StringComparer.Ordinal);
            HashSet<string> waterSourceIds = new(StringComparer.Ordinal);
            HashSet<string> resourceDepositIds = new(StringComparer.Ordinal);

            for (int i = 0; i < draft.ResourceDefinitions.Count; i++)
            {
                EditableResourceDefinitionDto definition = draft.ResourceDefinitions[i];
                string prefix = $"resourceDefinitions[{i}]";

                ValidateId(definition.Id, $"{prefix}.id", resourceDefinitionIds);
                AddIf(string.IsNullOrWhiteSpace(definition.DisplayName), $"{prefix}.displayName", "Display name is required.");
                AddIf(string.IsNullOrWhiteSpace(definition.IconKey), $"{prefix}.iconKey", "Icon key is required.");
                ValidateFinite(definition.UnitWeight, $"{prefix}.unitWeight");
                AddIf(definition.UnitWeight <= 0, $"{prefix}.unitWeight", "Unit weight must be greater than zero.");
                AddIf(definition.MaxStackQuantity <= 0, $"{prefix}.maxStackQuantity", "Max stack quantity must be greater than zero.");
                ValidateFinite(definition.NeedEffects.HungerDelta, $"{prefix}.needEffects.hungerDelta");
                ValidateFinite(definition.NeedEffects.ThirstDelta, $"{prefix}.needEffects.thirstDelta");
                ValidateFinite(definition.NeedEffects.EnergyDelta, $"{prefix}.needEffects.energyDelta");
                ValidateFinite(definition.NeedEffects.FatigueDelta, $"{prefix}.needEffects.fatigueDelta");
            }

            for (int i = 0; i < draft.Agents.Count; i++)
            {
                EditableAgentDto agent = draft.Agents[i];
                string prefix = $"agents[{i}]";

                ValidateId(agent.Id, $"{prefix}.id", agentIds);
                ValidatePosition(agent.Position, $"{prefix}.position", draft.Grid.Width, draft.Grid.Height);
                AddIf(!SpeciesRegistry.IsKnown(agent.SpeciesId), $"{prefix}.speciesId", "Species id is invalid.");
                AddIf(agent.AgeTicks < 0, $"{prefix}.ageTicks", "Age must be zero or greater.");
                AddIf(agent.BornTick < 0, $"{prefix}.bornTick", "Born tick must be zero or greater.");
                AddIf(agent.LastReproducedTick is < 0, $"{prefix}.lastReproducedTick", "Last reproduced tick must be zero or greater.");
                AddIf(agent.DeathTick is < 0, $"{prefix}.deathTick", "Death tick must be zero or greater.");
                ValidateTraits(agent.Traits, $"{prefix}.traits");
                ValidateFinite(agent.Needs.Hunger, $"{prefix}.needs.hunger");
                ValidateFinite(agent.Needs.Thirst, $"{prefix}.needs.thirst");
                ValidateFinite(agent.Needs.Energy, $"{prefix}.needs.energy");
                ValidateFinite(agent.Needs.Fatigue, $"{prefix}.needs.fatigue");
                ValidateInventory(agent.Inventory, $"{prefix}.inventory", resourceDefinitionIds, draft.ResourceDefinitions);
            }

            for (int i = 0; i < draft.ResourceContainers.Count; i++)
            {
                EditableResourceContainerDto container = draft.ResourceContainers[i];
                string prefix = $"resourceContainers[{i}]";

                ValidateId(container.Id, $"{prefix}.id", resourceContainerIds);
                ValidatePosition(container.Position, $"{prefix}.position", draft.Grid.Width, draft.Grid.Height);
                ValidateInventory(container.Inventory, $"{prefix}.inventory", resourceDefinitionIds, draft.ResourceDefinitions);
            }

            bool includeEcologyDraftEntities = ShouldIncludeEcologyDraftEntities(draft);
            IReadOnlyList<EditablePlantDto> plants = includeEcologyDraftEntities ? draft.Plants ?? [] : [];
            for (int i = 0; i < plants.Count; i++)
            {
                EditablePlantDto plant = plants[i];
                string prefix = $"plants[{i}]";

                ValidateId(plant.Id, $"{prefix}.id", plantIds);
                ValidatePosition(plant.Position, $"{prefix}.position", draft.Grid.Width, draft.Grid.Height);
                AddIf(string.IsNullOrWhiteSpace(plant.ResourceId), $"{prefix}.resourceId", "Resource id is required.");
                AddIf(!resourceDefinitionIds.Contains(plant.ResourceId), $"{prefix}.resourceId", "Resource id must match a resource definition.");
                AddIf(plant.Yield < 0, $"{prefix}.yield", "Yield must be zero or greater.");
                AddIf(plant.MaxYield < 0, $"{prefix}.maxYield", "Max yield must be zero or greater.");
                AddIf(plant.Yield > plant.MaxYield, $"{prefix}.yield", "Yield cannot exceed max yield.");
                AddIf(plant.RegrowthTicks < 0, $"{prefix}.regrowthTicks", "Regrowth ticks must be zero or greater.");
                AddIf(plant.TicksUntilRegrowth < 0, $"{prefix}.ticksUntilRegrowth", "Ticks until regrowth must be zero or greater.");
                AddIf(plant.DecayTicksAfterDepleted <= 0, $"{prefix}.decayTicksAfterDepleted", "Decay ticks must be greater than zero.");
                AddIf(plant.DepletedTicks < 0, $"{prefix}.depletedTicks", "Depleted ticks must be zero or greater.");
            }

            IReadOnlyList<EditableWaterSourceDto> waterSources = includeEcologyDraftEntities ? draft.WaterSources ?? [] : [];
            for (int i = 0; i < waterSources.Count; i++)
            {
                EditableWaterSourceDto water = waterSources[i];
                string prefix = $"waterSources[{i}]";

                ValidateId(water.Id, $"{prefix}.id", waterSourceIds);
                ValidatePosition(water.Position, $"{prefix}.position", draft.Grid.Width, draft.Grid.Height);
                ValidateFinite(water.CurrentVolume, $"{prefix}.currentVolume");
                ValidateFinite(water.MaxVolume, $"{prefix}.maxVolume");
                ValidateFinite(water.RefillPerRainTick, $"{prefix}.refillPerRainTick");
                ValidateFinite(water.EvaporationPerHeatTick, $"{prefix}.evaporationPerHeatTick");
                AddIf(water.CurrentVolume < 0, $"{prefix}.currentVolume", "Volume must be zero or greater.");
                AddIf(water.MaxVolume < 0, $"{prefix}.maxVolume", "Max volume must be zero or greater.");
                AddIf(water.CurrentVolume > water.MaxVolume, $"{prefix}.currentVolume", "Volume cannot exceed max volume.");
                AddIf(water.RefillPerRainTick < 0, $"{prefix}.refillPerRainTick", "Refill must be zero or greater.");
                AddIf(water.EvaporationPerHeatTick < 0, $"{prefix}.evaporationPerHeatTick", "Evaporation must be zero or greater.");
            }

            IReadOnlyList<EditableResourceDepositDto> deposits = includeEcologyDraftEntities ? draft.ResourceDeposits ?? [] : [];
            for (int i = 0; i < deposits.Count; i++)
            {
                EditableResourceDepositDto deposit = deposits[i];
                string prefix = $"resourceDeposits[{i}]";

                ValidateId(deposit.Id, $"{prefix}.id", resourceDepositIds);
                ValidatePosition(deposit.Position, $"{prefix}.position", draft.Grid.Width, draft.Grid.Height);
                AddIf(string.IsNullOrWhiteSpace(deposit.ResourceId), $"{prefix}.resourceId", "Resource id is required.");
                AddIf(!resourceDefinitionIds.Contains(deposit.ResourceId), $"{prefix}.resourceId", "Resource id must match a resource definition.");
                AddIf(deposit.Quantity < 0, $"{prefix}.quantity", "Quantity must be zero or greater.");
                AddIf(deposit.MaxQuantity < 0, $"{prefix}.maxQuantity", "Max quantity must be zero or greater.");
                AddIf(deposit.Quantity > deposit.MaxQuantity, $"{prefix}.quantity", "Quantity cannot exceed max quantity.");
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

            void ValidateTraits(AgentTraitsDto? traits, string key)
            {
                if (traits is null)
                {
                    return;
                }

                ValidateFinite(traits.Perception, $"{key}.perception");
                ValidateFinite(traits.Speed, $"{key}.speed");
                ValidateFinite(traits.Metabolism, $"{key}.metabolism");
                ValidateFinite(traits.RiskTolerance, $"{key}.riskTolerance");
                ValidateFinite(traits.LearningRate, $"{key}.learningRate");
            }

            void ValidateInventory(
                EditableInventoryDto inventory,
                string key,
                HashSet<string> definitionIds,
                IReadOnlyList<EditableResourceDefinitionDto> definitions)
            {
                if (inventory is null)
                {
                    Add(key, "Inventory is required.");
                    return;
                }

                AddIf(inventory.MaxSlots < 0, $"{key}.maxSlots", "Max slots must be zero or greater.");
                ValidateFinite(inventory.MaxWeight, $"{key}.maxWeight");
                AddIf(inventory.MaxWeight < 0, $"{key}.maxWeight", "Max weight must be zero or greater.");

                if (inventory.Stacks is null)
                {
                    Add($"{key}.stacks", "Inventory stacks are required.");
                    return;
                }

                HashSet<string> stackIds = new(StringComparer.Ordinal);
                float usedWeight = 0;

                for (int stackIndex = 0; stackIndex < inventory.Stacks.Count; stackIndex++)
                {
                    EditableResourceStackDto stack = inventory.Stacks[stackIndex];
                    string stackKey = $"{key}.stacks[{stackIndex}]";

                    ValidateId(stack.StackId, $"{stackKey}.stackId", stackIds);
                    AddIf(string.IsNullOrWhiteSpace(stack.ResourceId), $"{stackKey}.resourceId", "Resource id is required.");
                    AddIf(!definitionIds.Contains(stack.ResourceId), $"{stackKey}.resourceId", "Resource id must match a resource definition.");
                    AddIf(stack.Quantity <= 0, $"{stackKey}.quantity", "Quantity must be greater than zero.");

                    EditableResourceDefinitionDto? definition = definitions.FirstOrDefault(candidate =>
                        string.Equals(candidate.Id, stack.ResourceId, StringComparison.OrdinalIgnoreCase));
                    if (definition is not null)
                    {
                        AddIf(stack.Quantity > definition.MaxStackQuantity, $"{stackKey}.quantity", "Quantity exceeds max stack quantity.");
                        usedWeight += stack.Quantity * definition.UnitWeight;
                    }
                }

                AddIf(inventory.Stacks.Count > inventory.MaxSlots, $"{key}.maxSlots", "Inventory uses more slots than allowed.");
                AddIf(usedWeight > inventory.MaxWeight, $"{key}.maxWeight", "Inventory uses more weight than allowed.");
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

        private static ResourceDefinition ToResourceDefinition(EditableResourceDefinitionDto definition)
        {
            return new ResourceDefinition
            {
                Id = definition.Id.Trim(),
                DisplayName = definition.DisplayName.Trim(),
                IconKey = definition.IconKey.Trim(),
                UnitWeight = definition.UnitWeight,
                MaxStackQuantity = definition.MaxStackQuantity,
                IsConsumable = definition.IsConsumable,
                NeedEffects = new ResourceNeedEffects(
                    definition.NeedEffects.HungerDelta,
                    definition.NeedEffects.ThirstDelta,
                    definition.NeedEffects.EnergyDelta,
                    definition.NeedEffects.FatigueDelta),
                Renewability = Enum.TryParse(definition.Renewability, ignoreCase: true, out ResourceRenewability renewability)
                    ? renewability
                    : ResourceRenewability.Renewable,
                Category = string.IsNullOrWhiteSpace(definition.Category) ? "general" : definition.Category.Trim()
            };
        }

        private static Inventory ToInventory(EditableInventoryDto inventory)
        {
            Inventory result = new()
            {
                MaxSlots = inventory.MaxSlots,
                MaxWeight = inventory.MaxWeight
            };

            result.Stacks.AddRange(inventory.Stacks.Select(stack => new ResourceStack
            {
                StackId = stack.StackId.Trim(),
                ResourceId = stack.ResourceId.Trim(),
                Quantity = stack.Quantity
            }));

            return result;
        }

        private static WorldPosition ToWorldPosition(PositionDto position)
        {
            return new WorldPosition(position.X, position.Y);
        }

        private static TabulaRasa.Abstractions.Agents.AgentTraits ToAgentTraits(AgentTraitsDto? traits)
        {
            return traits is null
                ? TabulaRasa.Abstractions.Agents.AgentTraits.Default
                : new TabulaRasa.Abstractions.Agents.AgentTraits(
                    traits.Perception,
                    traits.Speed,
                    traits.Metabolism,
                    traits.RiskTolerance,
                    traits.LearningRate);
        }

        private static bool ShouldIncludeEcologyDraftEntities(SimulationDraftDto draft)
        {
            return draft.Config is null || draft.Config.Ecology is not null;
        }
    }
}
