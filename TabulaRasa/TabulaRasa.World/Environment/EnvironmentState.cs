namespace TabulaRasa.World.Environment
{
    public sealed class EnvironmentState
    {
        public int DayLengthTicks { get; set; } = 100;
        public int TickOfDay { get; set; }
        public int Day { get; set; }
        public DayPhase Phase { get; set; } = DayPhase.Dawn;
        public EnvironmentWeather Weather { get; set; } = EnvironmentWeather.Clear;
        public float Temperature { get; set; } = 20;
    }
}
