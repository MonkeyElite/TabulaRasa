using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TabulaRasa.Simulation.Composition;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.Engine;
using TabulaRasa.Simulation.Scenarios;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Resources;

string command = args.FirstOrDefault()?.ToLowerInvariant() ?? "soak";
Dictionary<string, string> options = ParseOptions(args.Skip(1).ToArray());
string scenario = options.GetValueOrDefault("scenario", "stable-mixed");
int ticks = int.TryParse(options.GetValueOrDefault("ticks"), out int parsedTicks) ? Math.Max(1, parsedTicks) : 1_000;
int seed = int.TryParse(options.GetValueOrDefault("seed"), out int parsedSeed) ? parsedSeed : 12345;
bool verbose = options.ContainsKey("verbose");
string? output = options.GetValueOrDefault("output");

if (command == "determinism")
{
    DeterminismReport report = RunDeterminism(scenario, ticks, seed, verbose);
    WriteReport(report, output);
    Environment.Exit(report.Matches ? 0 : 2);
}

if (command is not "soak" and not "benchmark")
{
    Console.Error.WriteLine("Usage: soak|benchmark|determinism --scenario <name> --ticks <n> --seed <seed> [--output <path>] [--verbose]");
    Environment.Exit(1);
}

RunReport runReport = RunScenario(command, scenario, ticks, seed, verbose);
WriteReport(runReport, output);

static RunReport RunScenario(string command, string scenario, int ticks, int seed, bool verbose)
{
    SimulationConfig config = BuildConfig(scenario, seed, verbose);
    var (state, systems) = MinimalSimulationFactory.Create(config);
    SimulationEngine engine = new(systems);
    Stopwatch timer = Stopwatch.StartNew();
    List<BalancePoint> points = [];

    for (int tick = 0; tick < ticks; tick++)
    {
        engine.ExecuteTick(state);
        if (tick % Math.Max(1, ticks / 200) == 0 || tick == ticks - 1)
        {
            points.Add(ToBalancePoint(state));
        }
    }

    timer.Stop();
    IReadOnlyList<string> anomalies = DetectAnomalies(points, ticks);

    return new RunReport(
        command,
        scenario,
        seed,
        ticks,
        DeterministicHash(state),
        ToFinalMetrics(state),
        ToMetricSummary(points),
        anomalies,
        new PerformanceReport(timer.Elapsed.TotalMilliseconds, state.DiagnosticsHistory.Values.Select(d => d.DurationMilliseconds).DefaultIfEmpty(0).Average()));
}

static DeterminismReport RunDeterminism(string scenario, int ticks, int seed, bool verbose)
{
    RunReport first = RunScenario("determinism", scenario, ticks, seed, verbose);
    RunReport second = RunScenario("determinism", scenario, ticks, seed, verbose);
    RunReport differentSeed = RunScenario("determinism", scenario, ticks, seed + 1, verbose);

    return new DeterminismReport(
        scenario,
        ticks,
        seed,
        first.DeterministicHash,
        second.DeterministicHash,
        differentSeed.DeterministicHash,
        first.DeterministicHash == second.DeterministicHash,
        first.DeterministicHash != differentSeed.DeterministicHash);
}

static SimulationConfig BuildConfig(string scenario, int seed, bool verbose)
{
    SimulationConfig config = SimulationScenarioCatalog.Create(scenario, seed);
    if (verbose)
    {
        return config;
    }

    return config with
    {
        EnabledSystems = config.EffectiveEnabledSystems
            .Where(systemId => !string.Equals(systemId, "reporting", StringComparison.OrdinalIgnoreCase))
            .ToList()
    };
}

static BalancePoint ToBalancePoint(SimulationState state)
{
    int alive = state.World.Agents.Count(agent => !agent.IsDead);
    IReadOnlyList<AgentNeeds> needs = state.Agents
        .Where(agentState => state.World.Agents.Any(agent => agent.Id == agentState.Id && !agent.IsDead))
        .Select(agentState => new AgentNeeds(
            agentState.NeedState.Hunger,
            agentState.NeedState.Thirst,
            agentState.NeedState.Energy,
            agentState.NeedState.Fatigue))
        .ToList();

    return new BalancePoint(
        state.Time.Tick,
        state.World.Agents.Count,
        alive,
        state.World.Agents.Count(agent => agent.IsDead),
        Average(needs, need => need.Hunger),
        Average(needs, need => need.Thirst),
        Average(needs, need => need.Energy),
        Average(needs, need => need.Fatigue),
        state.World.ResourceContainers.Sum(container => container.Inventory.GetQuantity(ResourceDefinition.FoodId)),
        state.World.Plants.Count,
        state.World.Plants.Sum(plant => plant.Yield),
        state.World.WaterSources.Count,
        state.World.WaterSources.Sum(water => water.CurrentVolume),
        state.DiagnosticsHistory.Values.LastOrDefault()?.DurationMilliseconds ?? 0,
        state.GetEventsForTick(state.Time.Tick).Count(simulationEvent => simulationEvent.Importance >= 0.5f));
}

static FinalMetrics ToFinalMetrics(SimulationState state)
{
    IReadOnlyDictionary<string, int> deathCauses = state.World.Agents
        .Where(agent => agent.IsDead)
        .GroupBy(agent => string.IsNullOrWhiteSpace(agent.DeathCause) ? "unknown" : agent.DeathCause, StringComparer.OrdinalIgnoreCase)
        .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

    return new FinalMetrics(
        state.Time.Tick,
        state.World.Agents.Count,
        state.World.Agents.Count(agent => !agent.IsDead),
        state.World.Agents.Count(agent => agent.IsDead),
        state.World.Plants.Count,
        state.World.Plants.Sum(plant => plant.Yield),
        state.World.WaterSources.Sum(water => water.CurrentVolume),
        state.ActionResults.Count(result => !result.Succeeded),
        state.GetRecentEvents().Count(simulationEvent => simulationEvent.Importance >= 0.5f),
        deathCauses,
        deathCauses.GetValueOrDefault("survival"),
        state.World.Agents
            .Where(agent => agent.DeathTick is not null)
            .Select(agent => (long?)agent.DeathTick!.Value)
            .DefaultIfEmpty(null)
            .Min(),
        state.World.Agents.All(agent => agent.IsDead)
            ? state.World.Agents
                .Where(agent => agent.DeathTick is not null)
                .Select(agent => (long?)agent.DeathTick!.Value)
                .DefaultIfEmpty(null)
                .Max()
            : null);
}

static MetricSummary ToMetricSummary(IReadOnlyList<BalancePoint> points)
{
    return new MetricSummary(
        ToRange(points.Select(point => (double)point.AliveAgents)),
        points.Max(point => point.AliveAgents),
        ToRange(points.Select(point => (double)point.AverageHunger)),
        ToRange(points.Select(point => (double)point.FoodCount + point.TotalPlantYield)),
        ToRange(points.Select(point => (double)point.TotalWaterVolume)),
        ToRange(points.Select(point => point.DurationMilliseconds)));
}

static IReadOnlyList<string> DetectAnomalies(IReadOnlyList<BalancePoint> points, int ticks)
{
    List<string> anomalies = [];
    if (points.Count == 0)
    {
        return anomalies;
    }

    BalancePoint final = points[^1];
    if (final.AliveAgents == 0)
    {
        anomalies.Add("extinction");
    }

    if (final.FoodCount + final.TotalPlantYield == 0)
    {
        anomalies.Add("food-collapse");
    }

    if (final.TotalWaterVolume <= 0)
    {
        anomalies.Add("water-collapse");
    }

    if (points.Max(point => point.AliveAgents) > Math.Max(50, points[0].AliveAgents * 5))
    {
        anomalies.Add("population-spike");
    }

    if (points.Average(point => point.DurationMilliseconds) > 25 && ticks >= 1_000)
    {
        anomalies.Add("slow-average-tick");
    }

    return anomalies;
}

static string DeterministicHash(SimulationState state)
{
    StringBuilder builder = new();
    builder.Append("tick=").Append(state.Time.Tick).Append(';');
    foreach (var agent in state.World.Agents.OrderBy(agent => agent.Id, StringComparer.Ordinal))
    {
        var needs = state.GetAgentById(agent.Id)?.NeedState;
        builder
            .Append(agent.Id).Append(':')
            .Append(agent.SpeciesId).Append(':')
            .Append(agent.IsDead).Append(':')
            .Append(agent.Position.X.ToString("0.###")).Append(',')
            .Append(agent.Position.Y.ToString("0.###")).Append(':')
            .Append(agent.Health.Current.ToString("0.###")).Append(':')
            .Append(needs?.Hunger.ToString("0.###")).Append(',')
            .Append(needs?.Thirst.ToString("0.###")).Append(',')
            .Append(needs?.Energy.ToString("0.###")).Append(',')
            .Append(needs?.Fatigue.ToString("0.###")).Append(';');
    }

    builder.Append("plants=");
    foreach (var plant in state.World.Plants.OrderBy(plant => plant.Id, StringComparer.Ordinal))
    {
        builder.Append(plant.Id).Append(':').Append(plant.Yield).Append(':').Append(plant.IsDecayed).Append(';');
    }

    builder.Append("water=");
    foreach (var water in state.World.WaterSources.OrderBy(water => water.Id, StringComparer.Ordinal))
    {
        builder.Append(water.Id).Append(':').Append(water.CurrentVolume.ToString("0.###")).Append(';');
    }

    byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
    return Convert.ToHexString(hash).ToLowerInvariant();
}

static RangeMetric ToRange(IEnumerable<double> values)
{
    IReadOnlyList<double> list = values.ToList();
    return list.Count == 0
        ? new RangeMetric(0, 0, 0)
        : new RangeMetric(list.Min(), list.Max(), list.Average());
}

static float Average(IReadOnlyList<AgentNeeds> needs, Func<AgentNeeds, float> selector)
{
    return needs.Count == 0 ? 0 : needs.Average(selector);
}

static Dictionary<string, string> ParseOptions(string[] tokens)
{
    Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);
    for (int index = 0; index < tokens.Length; index++)
    {
        string token = tokens[index];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        string key = token[2..];
        if (index + 1 < tokens.Length && !tokens[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            options[key] = tokens[++index];
        }
        else
        {
            options[key] = "true";
        }
    }

    return options;
}

static void WriteReport<T>(T report, string? output)
{
    string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    if (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine(json);
        return;
    }

    string? directory = Path.GetDirectoryName(output);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    File.WriteAllText(output, json);
    Console.WriteLine(output);
}

public sealed record AgentNeeds(float Hunger, float Thirst, float Energy, float Fatigue);

public sealed record BalancePoint(
    long Tick,
    int Population,
    int AliveAgents,
    int DeadAgents,
    float AverageHunger,
    float AverageThirst,
    float AverageEnergy,
    float AverageFatigue,
    int FoodCount,
    int PlantCount,
    int TotalPlantYield,
    int WaterSourceCount,
    float TotalWaterVolume,
    double DurationMilliseconds,
    int ImportantEventCount);

public sealed record FinalMetrics(
    long Tick,
    int Population,
    int AliveAgents,
    int DeadAgents,
    int PlantCount,
    int TotalPlantYield,
    float TotalWaterVolume,
    int FailedActions,
    int ImportantEvents,
    IReadOnlyDictionary<string, int> DeathCauses,
    int SurvivalDeaths,
    long? FirstDeathTick,
    long? ExtinctionTick);

public sealed record RangeMetric(double Min, double Max, double Average);

public sealed record MetricSummary(
    RangeMetric AliveAgents,
    int PeakPopulation,
    RangeMetric Hunger,
    RangeMetric Food,
    RangeMetric Water,
    RangeMetric TickDurationMilliseconds);

public sealed record PerformanceReport(double TotalMilliseconds, double AverageRecordedTickMilliseconds);

public sealed record RunReport(
    string Command,
    string Scenario,
    int Seed,
    int Ticks,
    string DeterministicHash,
    FinalMetrics FinalMetrics,
    MetricSummary Metrics,
    IReadOnlyList<string> Anomalies,
    PerformanceReport Performance);

public sealed record DeterminismReport(
    string Scenario,
    int Ticks,
    int Seed,
    string FirstHash,
    string SecondHash,
    string DifferentSeedHash,
    bool Matches,
    bool DifferentSeedDiverged);
