using TabulaRasa.Abstractions.Spatial.Grid;

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

        public GridCell ToGridCell()
        {
            return new GridCell((int)MathF.Floor(X), (int)MathF.Floor(Y));
        }

        public float DistanceTo(WorldPosition other)
        {
            float deltaX = X - other.X;
            float deltaY = Y - other.Y;

            return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
}
