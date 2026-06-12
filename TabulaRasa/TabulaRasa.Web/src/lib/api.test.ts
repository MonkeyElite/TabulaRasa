import { afterEach, describe, expect, it, vi } from "vitest";
import { apiBaseUrl, simulationApi } from "./api";

describe("api client", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("uses the local API default", () => {
    expect(apiBaseUrl).toBe("/api");
  });

  it("calls the ID-scoped stop endpoint", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ status: "Stopped" }));

    await simulationApi.stop("sim-1");

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/sim-1/stop",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("creates simulations", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ simulationId: "sim-2" }));

    await simulationApi.create({ name: "Second", config });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ name: "Second", config })
      })
    );
  });

  it("clones simulations", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ simulationId: "sim-2" }));

    await simulationApi.clone("sim-1", { name: "Fork" });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/sim-1/clone",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ name: "Fork" })
      })
    );
  });

  it("loads persisted runs", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ simulationId: "sim-1" }));

    await simulationApi.loadRun("sim-1");

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/runs/sim-1/load",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("loads built-in scenario presets", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse([]));

    await simulationApi.scenarios();

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/scenarios",
      expect.objectContaining({ headers: expect.any(Object) })
    );
  });

  it("forks persisted runs from a tick", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ simulationId: "sim-2" }));

    await simulationApi.forkRun("sim-1", { name: "Fork", sourceTick: 12 });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/runs/sim-1/fork",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ name: "Fork", sourceTick: 12 })
      })
    );
  });

  it("saves checkpoints", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ simulationId: "sim-1" }));

    await simulationApi.save("sim-1");

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/sim-1/save",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("loads timeline samples", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse([]));

    await simulationApi.timeline("sim-1", 2, 12, 3);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/sim-1/timeline?from=2&to=12&sampleEvery=3",
      expect.objectContaining({ headers: expect.any(Object) })
    );
  });

  it("imports scenarios", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ simulationId: "sim-import" }));
    const scenario = draft();

    await simulationApi.importScenario({ name: "Imported", scenario });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/import-scenario",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ name: "Imported", scenario })
      })
    );
  });

  it("sends config when resetting", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ tick: 0 }));

    await simulationApi.reset("sim-1", config);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulations/sim-1/reset",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ config })
      })
    );
  });
});

const config = {
  seed: 42,
  worldWidth: 10,
  worldHeight: 10,
  tickIntervalMilliseconds: 250,
  initialAgentCount: 1,
  initialFoodCount: 1,
  eventHistoryLimit: 2,
  snapshotHistoryLimit: 5,
  needDecay: {
    hungerDelta: 0.08,
    thirstDelta: 0.08,
    energyDelta: -0.02,
    fatigueDelta: 0.04
  },
  needRules: {
    maximumNeedValue: 10,
    maximumEnergyValue: 10,
    eatRecoveryAmount: 5,
    drinkRecoveryAmount: 5,
    restEnergyRecoveryAmount: 4,
    restFatigueRecoveryAmount: 5,
    criticalNeedThreshold: 8,
    harmNeedThreshold: 10,
    exhaustedEnergyThreshold: 0,
    survivalDamagePerTick: 1,
    heatWeatherThirstMultiplier: 1.25,
    hotTemperatureThreshold: 30,
    hotTemperatureThirstBonus: 0.25,
    coldTemperatureThreshold: 5,
    coldTemperatureThirstBonus: -0.15,
    minTemperatureThirstMultiplier: 0.5,
    maxTemperatureThirstMultiplier: 2
  },
  goals: {
    hungerThreshold: 4,
    urgentHungerThreshold: 7.5,
    interruptionPriorityDelta: 20,
    inventionMaxHunger: 4,
    inventionMaxThirst: 4,
    inventionMaxFatigue: 5
  },
  perceptionRadius: 20,
  movementSpeedPerTick: 0.25,
  pathfinding: {
    allowDiagonalMovement: false,
    maxVisitedCells: 1000,
    maxRepathAttempts: 3,
    arrivalTolerance: 0.05,
    interactionTolerance: 0.1,
    agentInteractionRangeBonus: 0.5
  },
  spawnResources: {
    foodStackQuantity: 2,
    plantStartingYield: 4,
    plantMaxYield: 5,
    waterStartingVolume: 16,
    waterMaxVolume: 20,
    depositQuantity: 5,
    depositMaxQuantity: 5
  },
  memory: {
    enabled: true,
    maxMemoriesPerAgent: 100,
    retentionTicks: 80,
    decayPerTick: 0.02,
    minimumStrength: 0.2,
    recallThreshold: 0.35
  },
  traits: {
    initialVariation: 0.12,
    mutationChancePerTrait: 0.08,
    mutationDelta: 0.06
  },
  environment: {
    dayLengthTicks: 100,
    weatherChangeIntervalTicks: 50,
    baseTemperature: 20,
    dawnEndRatio: 0.15,
    dayEndRatio: 0.65,
    duskEndRatio: 0.8,
    clearWeatherWeight: 55,
    rainWeatherWeight: 20,
    heatWeatherWeight: 15,
    coldWeatherWeight: 10,
    dayTemperatureDelta: 4,
    nightTemperatureDelta: -6,
    dawnTemperatureDelta: -2,
    duskTemperatureDelta: 1,
    heatTemperatureDelta: 8,
    coldTemperatureDelta: -8,
    rainTemperatureDelta: -2,
    maxPlantCooling: 3,
    plantCoolingFactor: 30,
    maxWaterCooling: 2,
    waterCoolingPerSource: 0.5
  },
  ecology: {
    initialPlantCount: 3,
    initialWaterSourceCount: 1,
    initialResourceDepositCount: 1,
    plantRegrowthTicks: 5,
    plantDecayTicksAfterDepleted: 20,
    waterRefillPerRainTick: 0.5,
    waterEvaporationPerHeatTick: 0.25,
    collapsePlantYieldThreshold: 0,
    collapseWaterVolumeThreshold: 0,
    recoveryPlantYieldThreshold: 1,
    recoveryWaterVolumeThreshold: 1
  },
  speciesPopulation: {
    human: 1,
    deer: 0,
    wolf: 0
  },
  lifecycle: {
    ageDaysPerTick: 0.01,
    daysPerYear: 365
  },
  speciesRules: {
    human: {
      maxHealth: 10,
      adultAgeDays: 20,
      maxAgeDays: 2000,
      reproductionCooldownTicks: 80,
      perceptionMultiplier: 1,
      movementSpeedMultiplier: 1,
      attackDamage: 2,
      hungerDecayMultiplier: 1,
      thirstDecayMultiplier: 1,
      fatigueDecayMultiplier: 1,
      startingNeeds: { hunger: 1, thirst: 0.5, energy: 10, fatigue: 0 },
      edibleResourceIds: ["food"],
      preySpeciesIds: []
    },
    deer: null,
    wolf: null
  },
  enabledSystems: ["need-decay", "planning"]
};

function draft() {
  return {
    tick: 0,
    grid: {
      width: 2,
      height: 2,
      blockedCells: [],
      terrainCells: []
    },
    agents: [],
    resourceDefinitions: [],
    resourceContainers: [],
    config,
    plants: [],
    waterSources: [],
    resourceDeposits: []
  };
}

function jsonResponse(body: unknown) {
  return {
    ok: true,
    json: () => Promise.resolve(body)
  } as Response;
}
