using System.Diagnostics;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Engine;

var (state, systems) = MinimalSimulationFactory.Create();

Console.WriteLine("Starting simulation...");

SimulationEngine engine = new SimulationEngine(systems);

var Timer = Stopwatch.StartNew();

engine.Run(state, maxTicks: 10);

Timer.Stop();
Console.WriteLine($"Simulation had ended in: {Timer.ElapsedMilliseconds}");
