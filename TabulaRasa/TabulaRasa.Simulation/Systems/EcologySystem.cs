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
                state.EmitEvent("ecology.plant_decayed", Name, $"{plant.Id} decayed.", plant.Id);
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
                state.EmitEvent("ecology.plant_regrown", Name, $"{plant.Id} regrew.", plant.Id);
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
    }
}
