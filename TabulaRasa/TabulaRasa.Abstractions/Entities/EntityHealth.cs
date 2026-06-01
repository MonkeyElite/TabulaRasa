namespace TabulaRasa.Abstractions.Entities
{
    public sealed class EntityHealth
    {
        public EntityHealth(float maximum)
            : this(maximum, maximum)
        {
        }

        public EntityHealth(float maximum, float current)
        {
            if (maximum <= 0 || float.IsNaN(maximum) || float.IsInfinity(maximum))
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum health must be finite and greater than zero.");
            }

            if (float.IsNaN(current) || float.IsInfinity(current))
            {
                throw new ArgumentOutOfRangeException(nameof(current), "Current health must be finite.");
            }

            Maximum = maximum;
            Current = Math.Clamp(current, 0, maximum);
        }

        public float Current { get; set; }
        public float Maximum { get; }
        public bool IsDepleted => Current <= 0;
    }
}
