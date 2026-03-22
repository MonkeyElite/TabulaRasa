using TabulaRasa.Kernel.Engine;
using TabulaRasa.Simulation.Composition;

var (world, systems) = MinimalSimulationFactory.Create();

var engine = new SimulationEngine(systems);

engine.Run(world, ticks: 10);
