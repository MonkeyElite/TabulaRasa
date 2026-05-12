using System.Diagnostics;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Engine;

var (state, systems) = MinimalSimulationFactory.Create();

Console.WriteLine("Starting simulation...");

SimulationEngine engine = new SimulationEngine(systems);

var timer = Stopwatch.StartNew();

engine.Run(state, maxTicks: 10);

timer.Stop();
Console.WriteLine($"Simulation completed in: {timer.ElapsedMilliseconds} ms");
