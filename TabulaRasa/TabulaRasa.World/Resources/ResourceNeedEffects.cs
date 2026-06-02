namespace TabulaRasa.World.Resources
{
    public sealed record ResourceNeedEffects(
        float HungerDelta = 0,
        float ThirstDelta = 0,
        float EnergyDelta = 0,
        float FatigueDelta = 0);
}
