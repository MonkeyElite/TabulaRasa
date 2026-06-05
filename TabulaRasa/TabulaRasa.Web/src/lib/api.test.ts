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
    hungerDelta: 1,
    thirstDelta: 1,
    energyDelta: -1,
    fatigueDelta: 1
  },
  perceptionRadius: 20,
  movementSpeedPerTick: 0.25,
  pathfinding: {
    allowDiagonalMovement: false,
    maxVisitedCells: 1000,
    maxRepathAttempts: 3
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
    baseTemperature: 20
  },
  ecology: {
    initialPlantCount: 3,
    initialWaterSourceCount: 1,
    initialResourceDepositCount: 1,
    plantRegrowthTicks: 5,
    plantDecayTicksAfterDepleted: 20,
    waterRefillPerRainTick: 0.5,
    waterEvaporationPerHeatTick: 0.25
  },
  speciesPopulation: {
    human: 1,
    deer: 0,
    wolf: 0
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
