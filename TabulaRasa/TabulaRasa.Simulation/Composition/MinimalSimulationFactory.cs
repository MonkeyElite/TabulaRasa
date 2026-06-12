using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.State;
using TabulaRasa.Abstractions.Time;
using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Construction;
using TabulaRasa.World.Entities;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Simulation.Movement.Execution;
using TabulaRasa.Simulation.Movement.Planning;
using TabulaRasa.Simulation.Tasks.Assignment;
using TabulaRasa.Simulation.Tasks.Execution;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.World.Spatial.Grid;
using TabulaRasa.World.Resources;
using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Simulation.Evolution;
using TabulaRasa.Simulation.Species;

namespace TabulaRasa.Simulation.Composition
{
    public static class MinimalSimulationFactory
    {
        public static readonly IReadOnlyDictionary<string, string> SystemNamesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["need-decay"] = "Need Decay System",
            ["environment"] = "Environment System",
            ["ecology"] = "Ecology System",
            ["lifecycle"] = "Lifecycle System",
            ["memory"] = "Agent Memory System",
            ["social"] = "Social System",
            ["planning"] = "Planning System",
            ["goal-generation"] = "Goal Generation System",
            ["action-request-creation"] = "Action Request Creation System",
            ["route-planning"] = "Route Planning System",
            ["job-activation"] = "Job Activation System",
            ["task-assignment"] = "Task Assignment System",
            ["task-action-dispatch"] = "Task Action Dispatch System",
            ["movement-execution"] = "Movement Execution System",
            ["task-execution"] = "Task Execution System",
            ["action-execution"] = "Action Execution System",
            ["recovery"] = "Recovery System",
            ["reporting"] = "Reporting System"
        };

        public static (SimulationState State, IReadOnlyList<ISystem> Systems) Create(SimulationConfig? config = null)
        {
            SimulationConfig effectiveConfig = config ?? new SimulationConfig();
            Random random = new(effectiveConfig.Seed);
            SpeciesPopulationConfig speciesPopulation = effectiveConfig.EffectiveSpeciesPopulation;
            int initialAgentCount = speciesPopulation.Human + speciesPopulation.Deer + speciesPopulation.Wolf;
            List<WorldPosition> positions = BuildDeterministicPositions(
                effectiveConfig.WorldWidth,
                effectiveConfig.WorldHeight,
                initialAgentCount
                    + effectiveConfig.InitialFoodCount
                    + effectiveConfig.EffectiveEcology.InitialPlantCount
                    + effectiveConfig.EffectiveEcology.InitialWaterSourceCount
                    + effectiveConfig.EffectiveEcology.InitialResourceDepositCount,
                random);

            List<AgentEntity> agentEntities = [];
            List<AgentState> agentStates = [];
            List<ResourceContainerEntity> resourceContainers = [];
            List<PlantEntity> plants = [];
            List<WaterSourceEntity> waterSources = [];
            List<ResourceDepositEntity> resourceDeposits = [];
            SpawnResourceConfig spawnResources = effectiveConfig.EffectiveSpawnResources;

            int nextPositionIndex = 0;
            CreateAgents(SpeciesRegistry.HumanId, speciesPopulation.Human, ref nextPositionIndex);
            CreateAgents(SpeciesRegistry.DeerId, speciesPopulation.Deer, ref nextPositionIndex);
            CreateAgents(SpeciesRegistry.WolfId, speciesPopulation.Wolf, ref nextPositionIndex);

            for (int index = 0; index < effectiveConfig.InitialFoodCount; index++)
            {
                int positionIndex = initialAgentCount + index;
                if (positionIndex >= positions.Count)
                {
                    break;
                }

                ResourceContainerEntity container = new()
                {
                    Id = $"resource-container-{index + 1}",
                    Position = positions[positionIndex]
                };
                container.Inventory.Stacks.Add(new ResourceStack
                {
                    StackId = $"food-stack-{index + 1}",
                    ResourceId = ResourceDefinition.FoodId,
                    Quantity = spawnResources.FoodStackQuantity
                });
                resourceContainers.Add(container);
            }

            GridMap grid = new(effectiveConfig.WorldWidth, effectiveConfig.WorldHeight);
            int ecologyPositionStart = initialAgentCount + effectiveConfig.InitialFoodCount;
            for (int index = 0; index < effectiveConfig.EffectiveEcology.InitialPlantCount; index++)
            {
                int positionIndex = ecologyPositionStart + index;
                if (positionIndex >= positions.Count)
                {
                    break;
                }

                plants.Add(new PlantEntity
                {
                    Id = $"plant-{index + 1}",
                    Position = positions[positionIndex],
                    MaxYield = spawnResources.PlantMaxYield,
                    Yield = Math.Min(spawnResources.PlantStartingYield, spawnResources.PlantMaxYield),
                    RegrowthTicks = effectiveConfig.EffectiveEcology.PlantRegrowthTicks,
                    DecayTicksAfterDepleted = effectiveConfig.EffectiveEcology.PlantDecayTicksAfterDepleted
                });
                grid.SetTerrain(positions[positionIndex].ToGridCell(), GridTerrainType.Forest);
            }

            int waterPositionStart = ecologyPositionStart + effectiveConfig.EffectiveEcology.InitialPlantCount;
            for (int index = 0; index < effectiveConfig.EffectiveEcology.InitialWaterSourceCount; index++)
            {
                int positionIndex = waterPositionStart + index;
                if (positionIndex >= positions.Count)
                {
                    break;
                }

                waterSources.Add(new WaterSourceEntity
                {
                    Id = $"water-source-{index + 1}",
                    Position = positions[positionIndex],
                    CurrentVolume = Math.Min(spawnResources.WaterStartingVolume, spawnResources.WaterMaxVolume),
                    MaxVolume = spawnResources.WaterMaxVolume,
                    RefillPerRainTick = effectiveConfig.EffectiveEcology.WaterRefillPerRainTick,
                    EvaporationPerHeatTick = effectiveConfig.EffectiveEcology.WaterEvaporationPerHeatTick
                });
                grid.SetTerrain(positions[positionIndex].ToGridCell(), GridTerrainType.Water);
            }

            int depositPositionStart = waterPositionStart + effectiveConfig.EffectiveEcology.InitialWaterSourceCount;
            for (int index = 0; index < effectiveConfig.EffectiveEcology.InitialResourceDepositCount; index++)
            {
                int positionIndex = depositPositionStart + index;
                if (positionIndex >= positions.Count)
                {
                    break;
                }

                resourceDeposits.Add(new ResourceDepositEntity
                {
                    Id = $"resource-deposit-{index + 1}",
                    Position = positions[positionIndex],
                    ResourceId = ResourceDefinition.StoneId,
                    Quantity = Math.Min(spawnResources.DepositQuantity, spawnResources.DepositMaxQuantity),
                    MaxQuantity = spawnResources.DepositMaxQuantity
                });
                grid.SetTerrain(positions[positionIndex].ToGridCell(), GridTerrainType.Mud);
            }

            WorldState world = WorldFactory.Create(agentEntities, resourceContainers, grid, plants, waterSources, resourceDeposits);
            SimulationState state = new(world, new SimulationTime(Tick: 0), agentStates, effectiveConfig);

            return (state, BuildSystems(state.Config));

            void CreateAgents(string speciesId, int count, ref int positionIndex)
            {
                SpeciesDefinition species = SpeciesRegistry.Get(speciesId, effectiveConfig.EffectiveSpeciesRules);
                for (int index = 0; index < count && positionIndex < positions.Count; index++)
                {
                    string id = $"{species.Id}-{index + 1}";
                    agentEntities.Add(new AgentEntity
                    {
                        Id = id,
                        Position = positions[positionIndex],
                        SpeciesId = species.Id,
                        Health = new EntityHealth(species.MaxHealth),
                        Traits = AgentTraitService.CreateInitialTraits(random, effectiveConfig.EffectiveTraits)
                    });

                    agentStates.Add(new AgentState(id, CreateStartingNeeds(effectiveConfig, species.Id), new DefaultAgentMind()));
                    positionIndex++;
                }
            }
        }

        public static IReadOnlyList<ISystem> BuildSystems(SimulationConfig config)
        {
            Dictionary<string, Func<ISystem>> factories = new(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = () => new EnvironmentSystem(),
                ["ecology"] = () => new EcologySystem(),
                ["lifecycle"] = () => new LifecycleSystem(),
                ["need-decay"] = () => new NeedDecaySystem(),
                ["memory"] = () => new AgentMemorySystem(),
                ["social"] = () => new SocialSystem(),
                ["planning"] = () => new PlanningSystem(),
                ["goal-generation"] = () => new GoalGenerationSystem(),
                ["action-request-creation"] = () => new ActionRequestCreationSystem(),
                ["route-planning"] = () => new RoutePlanningSystem(),
                ["job-activation"] = () => new JobActivationSystem(),
                ["task-assignment"] = () => new TaskAssignmentSystem(),
                ["task-action-dispatch"] = () => new TaskActionDispatchSystem(),
                ["movement-execution"] = () => new MovementExecutionSystem(),
                ["task-execution"] = () => new TaskExecutionSystem(),
                ["action-execution"] = () => new ActionExecutionSystem(),
                ["recovery"] = () => new RecoverySystem(),
                ["reporting"] = () => new ReportingSystem()
            };

            return config.EffectiveEnabledSystems
                .Where(factories.ContainsKey)
                .Select(systemId => factories[systemId]())
                .ToList();
        }

        private static AgentNeedState CreateStartingNeeds(SimulationConfig config, string speciesId)
        {
            StartingNeedsConfig needs = SpeciesRegistry.NormalizeId(speciesId) switch
            {
                SpeciesRegistry.WolfId => config.EffectiveSpeciesRules.EffectiveWolf.StartingNeeds ?? new StartingNeedsConfig(),
                SpeciesRegistry.DeerId => config.EffectiveSpeciesRules.EffectiveDeer.StartingNeeds ?? new StartingNeedsConfig(),
                _ => config.EffectiveSpeciesRules.EffectiveHuman.StartingNeeds ?? new StartingNeedsConfig()
            };
            var rules = config.EffectiveNeedRules;
            return new AgentNeedState
            {
                Hunger = Math.Clamp(needs.Hunger, 0, rules.MaximumNeedValue),
                Thirst = Math.Clamp(needs.Thirst, 0, rules.MaximumNeedValue),
                Energy = Math.Clamp(needs.Energy, 0, rules.MaximumEnergyValue),
                Fatigue = Math.Clamp(needs.Fatigue, 0, rules.MaximumNeedValue)
            };
        }

        private static List<WorldPosition> BuildDeterministicPositions(
            int width,
            int height,
            int requestedCount,
            Random random)
        {
            List<WorldPosition> positions = [];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    positions.Add(new WorldPosition(x + 0.5f, y + 0.5f));
                }
            }

            for (int index = positions.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (positions[index], positions[swapIndex]) = (positions[swapIndex], positions[index]);
            }

            return positions.Take(Math.Min(requestedCount, positions.Count)).ToList();
        }
    }
}
