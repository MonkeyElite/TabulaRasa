namespace TabulaRasa.Abstractions.World
{
    public readonly record struct WorldPosition
    {
        public readonly float X;
        public readonly float Y;

        public WorldPosition(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
