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

namespace TabulaRasa.Simulation.Composition
{
    public static class MinimalSimulationFactory
    {
        public static readonly IReadOnlyDictionary<string, string> SystemNamesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["need-decay"] = "Need Decay System",
            ["memory"] = "Agent Memory System",
            ["planning"] = "Planning System",
            ["action-request-creation"] = "Action Request Creation System",
            ["route-planning"] = "Route Planning System",
            ["job-activation"] = "Job Activation System",
            ["task-assignment"] = "Task Assignment System",
            ["movement-execution"] = "Movement Execution System",
            ["task-execution"] = "Task Execution System",
            ["action-execution"] = "Action Execution System",
            ["reporting"] = "Reporting System"
        };

        public static (SimulationState State, IReadOnlyList<ISystem> Systems) Create(SimulationConfig? config = null)
        {
            SimulationConfig effectiveConfig = config ?? new SimulationConfig();
            Random random = new(effectiveConfig.Seed);
            List<WorldPosition> positions = BuildDeterministicPositions(
                effectiveConfig.WorldWidth,
                effectiveConfig.WorldHeight,
                effectiveConfig.InitialAgentCount + effectiveConfig.InitialFoodCount,
                random);

            List<AgentEntity> agentEntities = [];
            List<AgentState> agentStates = [];
            List<FoodEntity> foods = [];

            for (int index = 0; index < effectiveConfig.InitialAgentCount && index < positions.Count; index++)
            {
                string id = $"agent-{index + 1}";
                agentEntities.Add(new AgentEntity
                {
                    Id = id,
                    Position = positions[index]
                });

                agentStates.Add(new AgentState(id, new AgentNeedState
                {
                    Hunger = 1,
                    Thirst = 0,
                    Energy = 10,
                    Fatigue = 0
                }, new DefaultAgentMind()));
            }

            for (int index = 0; index < effectiveConfig.InitialFoodCount; index++)
            {
                int positionIndex = effectiveConfig.InitialAgentCount + index;
                if (positionIndex >= positions.Count)
                {
                    break;
                }

                foods.Add(new FoodEntity
                {
                    Id = $"food-{index + 1}",
                    Position = positions[positionIndex],
                    IsConsumed = false
                });
            }

            GridMap grid = new(effectiveConfig.WorldWidth, effectiveConfig.WorldHeight);
            WorldState world = WorldFactory.Create(agentEntities, foods, grid);
            SimulationState state = new(world, new SimulationTime(Tick: 0), agentStates, effectiveConfig);

            return (state, BuildSystems(state.Config));
        }

        public static IReadOnlyList<ISystem> BuildSystems(SimulationConfig config)
        {
            Dictionary<string, Func<ISystem>> factories = new(StringComparer.OrdinalIgnoreCase)
            {
                ["need-decay"] = () => new NeedDecaySystem(),
                ["memory"] = () => new AgentMemorySystem(),
                ["planning"] = () => new PlanningSystem(),
                ["action-request-creation"] = () => new ActionRequestCreationSystem(),
                ["route-planning"] = () => new RoutePlanningSystem(),
                ["job-activation"] = () => new JobActivationSystem(),
                ["task-assignment"] = () => new TaskAssignmentSystem(),
                ["movement-execution"] = () => new MovementExecutionSystem(),
                ["task-execution"] = () => new TaskExecutionSystem(),
                ["action-execution"] = () => new ActionExecutionSystem(),
                ["reporting"] = () => new ReportingSystem()
            };

            return config.EffectiveEnabledSystems
                .Where(factories.ContainsKey)
                .Select(systemId => factories[systemId]())
                .ToList();
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
