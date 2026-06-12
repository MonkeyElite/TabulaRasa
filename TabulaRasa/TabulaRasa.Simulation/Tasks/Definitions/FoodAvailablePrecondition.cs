using TabulaRasa.Simulation.State;
using TabulaRasa.World.Queries;

namespace TabulaRasa.Simulation.Tasks.Definitions
{
    public sealed class FoodAvailablePrecondition : ITaskPrecondition
    {
        private readonly string _containerId;

        public FoodAvailablePrecondition(string containerId)
        {
            _containerId = containerId;
        }

        public TaskPreconditionResult Evaluate(SimulationState state, TaskInstance task)
        {
            bool available = state.World.ResourceContainers.Any(container =>
                container.Id == _containerId
                && SpatialQueries.ContainerHasFood(container))
                || state.World.Plants.Any(plant =>
                    plant.Id == _containerId
                    && plant.IsHarvestable);

            return available
                ? TaskPreconditionResult.Success
                : TaskPreconditionResult.Failure($"Food target '{_containerId}' is unavailable.");
        }
    }
}
