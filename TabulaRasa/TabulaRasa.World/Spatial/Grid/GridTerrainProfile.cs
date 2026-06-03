namespace TabulaRasa.World.Spatial.Grid
{
    public sealed record GridTerrainProfile(
        GridTerrainType TerrainType,
        float TraversalCost,
        float SpeedMultiplier,
        float PerceptionMultiplier = 1,
        float HungerDeltaMultiplier = 1,
        float ThirstDeltaMultiplier = 1,
        float FatigueDeltaMultiplier = 1,
        bool IsWater = false)
    {
        public static GridTerrainProfile For(GridTerrainType terrainType)
        {
            return terrainType switch
            {
                GridTerrainType.Road => new GridTerrainProfile(terrainType, 0.5f, 1.25f),
                GridTerrainType.Forest => new GridTerrainProfile(terrainType, 2f, 0.75f, PerceptionMultiplier: 0.75f),
                GridTerrainType.Mud => new GridTerrainProfile(terrainType, 3f, 0.5f, FatigueDeltaMultiplier: 1.5f),
                GridTerrainType.Water => new GridTerrainProfile(terrainType, 10f, 0.25f, ThirstDeltaMultiplier: 0.75f, IsWater: true),
                _ => new GridTerrainProfile(GridTerrainType.Plain, 1f, 1f)
            };
        }

        public static float MinimumTraversalCost { get; } = 0.5f;
    }
}
