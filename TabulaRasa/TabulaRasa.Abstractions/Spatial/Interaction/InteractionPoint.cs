using TabulaRasa.Abstractions.World;

namespace TabulaRasa.Abstractions.Spatial.Interaction
{
    public readonly record struct InteractionPoint(
        WorldPosition StandPosition,
        WorldPosition UsePosition,
        bool IsReserved = false);
}
