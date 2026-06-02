namespace TabulaRasa.World.Spatial.Grid
{
    public sealed record GridTerrainProfile(
        GridTerrainType TerrainType,
        float TraversalCost,
        float SpeedMultiplier)
    {
        public static GridTerrainProfile For(GridTerrainType terrainType)
        {
            return terrainType switch
            {
                GridTerrainType.Road => new GridTerrainProfile(terrainType, 0.5f, 1.25f),
                GridTerrainType.Forest => new GridTerrainProfile(terrainType, 2f, 0.75f),
                GridTerrainType.Mud => new GridTerrainProfile(terrainType, 3f, 0.5f),
                _ => new GridTerrainProfile(GridTerrainType.Plain, 1f, 1f)
            };
        }

        public static float MinimumTraversalCost { get; } = 0.5f;
    }
}
