using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Scenarios;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Resources;

namespace TabulaRasa.UnitTests.Simulation.Believability
{
    public sealed class LongRunStabilityTests
    {
        [Theory]
        [InlineData(12345)]
        [InlineData(55)]
        [InlineData(77)]
        public void StableMixed_SurvivesFiveThousandTicks(int seed)
        {
            SimulationConfig config = Quiet(SimulationScenarioCatalog.Create("stable-mixed", seed));
            var (state, systems) = MinimalSimulationFactory.Create(config);
            int initialAlive = state.World.Agents.Count(agent => !agent.IsDead);

            new SimulationEngine(systems).Run(state, 5_000);

            int alive = state.World.Agents.Count(agent => !agent.IsDead);
            Assert.True(alive > 0, $"Stable mixed went extinct for seed {seed}.");
            Assert.True(alive >= Math.Max(1, initialAlive / 2), $"Stable mixed retained {alive}/{initialAlive} agents for seed {seed}.");
            Assert.DoesNotContain(state.World.Agents, agent =>
                agent.DeathCause == "survival"
                && agent.DeathTick is not null
                && agent.DeathTick <= 1_000);
            Assert.True(TotalFood(state) > 0);
            Assert.True(TotalWater(state) > 0);
        }

        [Theory]
        [InlineData("recovery")]
        [InlineData("overpopulation")]
        [InlineData("resource-collapse")]
        public void StressScenarios_RunLongAndExposeDeathCauses(string scenario)
        {
            SimulationConfig config = Quiet(SimulationScenarioCatalog.Create(scenario, seed: 12345));
            var (state, systems) = MinimalSimulationFactory.Create(config);

            new SimulationEngine(systems).Run(state, 5_000);

            Assert.Equal(5_000, state.Time.Tick);
            Assert.NotEmpty(state.World.Agents);
            Assert.All(
                state.World.Agents.Where(agent => agent.IsDead),
                agent =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(agent.DeathCause));
                    Assert.NotNull(agent.DeathTick);
                });
        }

        private static SimulationConfig Quiet(SimulationConfig config)
        {
            return config with
            {
                EnabledSystems = config.EffectiveEnabledSystems
                    .Where(systemId => !string.Equals(systemId, "reporting", StringComparison.OrdinalIgnoreCase))
                    .ToList()
            };
        }

        private static int TotalFood(SimulationState state)
        {
            return state.World.ResourceContainers.Sum(container => container.Inventory.GetQuantity(ResourceDefinition.FoodId))
                + state.World.Plants.Sum(plant => plant.Yield);
        }

        private static float TotalWater(SimulationState state)
        {
            return state.World.WaterSources.Sum(water => water.CurrentVolume);
        }
    }
}
