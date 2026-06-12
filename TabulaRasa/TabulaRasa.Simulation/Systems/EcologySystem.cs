using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Environment;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class EcologySystem : ISystem
    {
        public string Name => "Ecology System";
        public SimulationPhase Phase => SimulationPhase.Sensing;
        public int Priority => -10;

        public void Execute(SimulationState state)
        {
            foreach (PlantEntity plant in state.World.Plants.ToList())
            {
                UpdatePlant(state, plant);
            }

            foreach (WaterSourceEntity waterSource in state.World.WaterSources)
            {
                UpdateWaterSource(state, waterSource);
            }

            EmitEcologyStateEvents(state);
        }

        private void UpdatePlant(SimulationState state, PlantEntity plant)
        {
            if (plant.IsDecayed)
            {
                state.World.Plants.Remove(plant);
                return;
            }

            if (plant.Yield > 0)
            {
                plant.Yield = Math.Min(plant.Yield, plant.MaxYield);
                plant.DepletedTicks = 0;
                return;
            }

            plant.DepletedTicks++;
            if (plant.DepletedTicks >= plant.DecayTicksAfterDepleted)
            {
                plant.IsDecayed = true;
                state.World.Plants.Remove(plant);
                state.EmitEvent(
                    "ecology.plant_decayed",
                    Name,
                    $"{plant.Id} decayed.",
                    plant.Id,
                    severity: "warning",
                    importance: 0.58f,
                    tags: ["ecology", "plant"]);
                return;
            }

            if (plant.TicksUntilRegrowth > 0)
            {
                plant.TicksUntilRegrowth--;
            }

            if (plant.TicksUntilRegrowth <= 0 && plant.RegrowthTicks >= 0)
            {
                plant.Yield = Math.Max(1, plant.MaxYield);
                plant.DepletedTicks = 0;
                state.EmitEvent(
                    "ecology.plant_regrown",
                    Name,
                    $"{plant.Id} regrew.",
                    plant.Id,
                    severity: "info",
                    importance: 0.50f,
                    tags: ["ecology", "recovery"]);
            }
        }

        private static void UpdateWaterSource(SimulationState state, WaterSourceEntity waterSource)
        {
            if (state.World.Environment.Weather == EnvironmentWeather.Rain)
            {
                waterSource.CurrentVolume = Math.Min(
                    waterSource.MaxVolume,
                    waterSource.CurrentVolume + waterSource.RefillPerRainTick);
            }

            if (state.World.Environment.Weather == EnvironmentWeather.Heat)
            {
                waterSource.CurrentVolume = Math.Max(
                    0,
                    waterSource.CurrentVolume - waterSource.EvaporationPerHeatTick);
            }
        }

        private void EmitEcologyStateEvents(SimulationState state)
        {
            int totalPlantYield = state.World.Plants.Sum(plant => plant.Yield);
            float totalWater = state.World.WaterSources.Sum(water => water.CurrentVolume);
            var ecology = state.Config.EffectiveEcology;
            if (state.World.Plants.Count > 0 && totalPlantYield <= ecology.CollapsePlantYieldThreshold)
            {
                state.EmitEvent(
                    "ecology.resource_collapse",
                    Name,
                    "All plants are depleted.",
                    metadata: new Dictionary<string, string>
                    {
                        ["kind"] = "plants"
                    },
                    severity: "critical",
                    importance: 0.86f,
                    tags: ["ecology", "collapse"]);
            }

            if (state.World.WaterSources.Count > 0 && totalWater <= ecology.CollapseWaterVolumeThreshold)
            {
                state.EmitEvent(
                    "ecology.resource_collapse",
                    Name,
                    "All water sources are dry.",
                    metadata: new Dictionary<string, string>
                    {
                        ["kind"] = "water"
                    },
                    severity: "critical",
                    importance: 0.88f,
                    tags: ["ecology", "collapse"]);
            }

            if (totalPlantYield >= ecology.RecoveryPlantYieldThreshold && totalWater >= ecology.RecoveryWaterVolumeThreshold)
            {
                state.EmitEvent(
                    "ecology.recovery_signal",
                    Name,
                    "Food and water are both available.",
                    severity: "info",
                    importance: 0.30f,
                    tags: ["ecology", "recovery"]);
            }
        }
    }
}
