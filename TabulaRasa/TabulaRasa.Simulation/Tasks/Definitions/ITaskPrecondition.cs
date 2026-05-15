using TabulaRasa.Simulation.State;

namespace TabulaRasa.Simulation.Tasks.Definitions
{
    public interface ITaskPrecondition
    {
        TaskPreconditionResult Evaluate(SimulationState state, TaskInstance task);
    }
}
