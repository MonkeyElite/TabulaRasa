using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Environment;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class EnvironmentSystem : ISystem
    {
        public string Name => "Environment System";
        public SimulationPhase Phase => SimulationPhase.PreUpdate;
        public int Priority => -20;

        public void Execute(SimulationState state)
        {
            EnvironmentConfig config = state.Config.EffectiveEnvironment;
            EnvironmentState environment = state.World.Environment;
            int dayLength = Math.Max(4, config.DayLengthTicks);
            long tick = state.ActiveTick;

            environment.DayLengthTicks = dayLength;
            environment.Day = (int)(tick / dayLength);
            environment.TickOfDay = (int)(tick % dayLength);
            environment.Phase = CalculatePhase(environment.TickOfDay, dayLength, config);
            environment.Weather = CalculateWeather(state.Config.Seed, tick, config);
            environment.Temperature = CalculateTemperature(state, config, environment);
        }

        private static DayPhase CalculatePhase(int tickOfDay, int dayLength, EnvironmentConfig config)
        {
            float normalized = tickOfDay / (float)dayLength;

            if (normalized < config.DawnEndRatio)
            {
                return DayPhase.Dawn;
            }

            if (normalized < config.DayEndRatio)
            {
                return DayPhase.Day;
            }

            return normalized < config.DuskEndRatio ? DayPhase.Dusk : DayPhase.Night;
        }

        private static EnvironmentWeather CalculateWeather(int seed, long tick, EnvironmentConfig config)
        {
            long bucket = tick / Math.Max(1, config.WeatherChangeIntervalTicks);
            int totalWeight = Math.Max(1, config.ClearWeatherWeight + config.RainWeatherWeight + config.HeatWeatherWeight + config.ColdWeatherWeight);
            int value = DeterministicBucket(seed, bucket) % totalWeight;
            int rainBoundary = config.ClearWeatherWeight + config.RainWeatherWeight;
            int heatBoundary = rainBoundary + config.HeatWeatherWeight;

            if (value < config.ClearWeatherWeight)
            {
                return EnvironmentWeather.Clear;
            }

            if (value < rainBoundary)
            {
                return EnvironmentWeather.Rain;
            }

            return value < heatBoundary ? EnvironmentWeather.Heat : EnvironmentWeather.Cold;
        }

        private static int DeterministicBucket(int seed, long bucket)
        {
            unchecked
            {
                ulong value = (uint)seed;
                value ^= (ulong)bucket + 0x9E3779B97F4A7C15UL + (value << 6) + (value >> 2);
                value ^= value >> 30;
                value *= 0xBF58476D1CE4E5B9UL;
                value ^= value >> 27;
                value *= 0x94D049BB133111EBUL;
                value ^= value >> 31;

                return (int)(value % 100);
            }
        }

        private static float CalculateTemperature(
            SimulationState state,
            EnvironmentConfig config,
            EnvironmentState environment)
        {
            float phaseDelta = environment.Phase switch
            {
                DayPhase.Day => config.DayTemperatureDelta,
                DayPhase.Night => config.NightTemperatureDelta,
                DayPhase.Dawn => config.DawnTemperatureDelta,
                DayPhase.Dusk => config.DuskTemperatureDelta,
                _ => 0
            };
            float weatherDelta = environment.Weather switch
            {
                EnvironmentWeather.Heat => config.HeatTemperatureDelta,
                EnvironmentWeather.Cold => config.ColdTemperatureDelta,
                EnvironmentWeather.Rain => config.RainTemperatureDelta,
                _ => 0
            };
            float cellCount = Math.Max(1, state.World.Grid.Width * state.World.Grid.Height);
            float plantCooling = Math.Min(config.MaxPlantCooling, state.World.Plants.Count(plant => !plant.IsDecayed) / cellCount * config.PlantCoolingFactor);
            float waterCooling = Math.Min(config.MaxWaterCooling, state.World.WaterSources.Count(water => water.CurrentVolume > 0) * config.WaterCoolingPerSource);

            return config.BaseTemperature + phaseDelta + weatherDelta - plantCooling - waterCooling;
        }
    }
}
