
namespace TabulaRasa.Abstractions.Execution
{
    public interface ISystem
    {
        string Name { get; }
        int Order { get; }

        void Execute(SimulationContext context);
    }
}
