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
            environment.Phase = CalculatePhase(environment.TickOfDay, dayLength);
            environment.Weather = CalculateWeather(state.Config.Seed, tick, config.WeatherChangeIntervalTicks);
            environment.Temperature = CalculateTemperature(state, config, environment);
        }

        private static DayPhase CalculatePhase(int tickOfDay, int dayLength)
        {
            float normalized = tickOfDay / (float)dayLength;

            if (normalized < 0.15f)
            {
                return DayPhase.Dawn;
            }

            if (normalized < 0.65f)
            {
                return DayPhase.Day;
            }

            return normalized < 0.8f ? DayPhase.Dusk : DayPhase.Night;
        }

        private static EnvironmentWeather CalculateWeather(int seed, long tick, int interval)
        {
            long bucket = tick / Math.Max(1, interval);
            int value = Math.Abs(HashCode.Combine(seed, bucket)) % 100;

            return value switch
            {
                < 55 => EnvironmentWeather.Clear,
                < 75 => EnvironmentWeather.Rain,
                < 90 => EnvironmentWeather.Heat,
                _ => EnvironmentWeather.Cold
            };
        }

        private static float CalculateTemperature(
            SimulationState state,
            EnvironmentConfig config,
            EnvironmentState environment)
        {
            float phaseDelta = environment.Phase switch
            {
                DayPhase.Day => 4,
                DayPhase.Night => -6,
                DayPhase.Dawn => -2,
                DayPhase.Dusk => 1,
                _ => 0
            };
            float weatherDelta = environment.Weather switch
            {
                EnvironmentWeather.Heat => 8,
                EnvironmentWeather.Cold => -8,
                EnvironmentWeather.Rain => -2,
                _ => 0
            };
            float cellCount = Math.Max(1, state.World.Grid.Width * state.World.Grid.Height);
            float plantCooling = Math.Min(3, state.World.Plants.Count(plant => !plant.IsDecayed) / cellCount * 30);
            float waterCooling = Math.Min(2, state.World.WaterSources.Count(water => water.CurrentVolume > 0) * 0.5f);

            return config.BaseTemperature + phaseDelta + weatherDelta - plantCooling - waterCooling;
        }
    }
}
