namespace TabulaRasa.Abstractions.Spatial.Footprints
{
    public readonly record struct EntityFootprint(float Width, float Height)
    {
        public static EntityFootprint SingleCell { get; } = new(1, 1);
    }
}
