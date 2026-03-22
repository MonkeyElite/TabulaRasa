using TabulaRasa.Abstractions.World;

namespace TabulaRasa.World.State
{
    public sealed class WorldState : IWorldState
    {
        public List<AgentEntity> Agents { get; } = [];
        public List<FoodEntity> Foods { get; } = [];
    }
}
