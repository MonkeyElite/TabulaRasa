using TabulaRasa.Abstractions.World;
using TabulaRasa.World.Entities;

namespace TabulaRasa.World.State
{
    public sealed class WorldState : IWorldState
    {
        public List<AgentEntity> Agents { get; } = [];
        public List<FoodEntity> Foods { get; } = [];
    }
}
