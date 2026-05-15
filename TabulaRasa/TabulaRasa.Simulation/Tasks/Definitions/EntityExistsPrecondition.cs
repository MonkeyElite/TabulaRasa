using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Tasks.Definitions
{
    public sealed class EntityExistsPrecondition : ITaskPrecondition
    {
        private readonly string _entityId;

        public EntityExistsPrecondition(string entityId)
        {
            _entityId = entityId;
        }

        public TaskPreconditionResult Evaluate(SimulationState state, TaskInstance task)
        {
            bool exists = state.World.Agents.Any(a => a.Id == _entityId)
                || state.World.Foods.Any(f => f.Id == _entityId);

            return exists
                ? TaskPreconditionResult.Success
                : TaskPreconditionResult.Failure($"Required entity '{_entityId}' does not exist.");
        }
    }
}
