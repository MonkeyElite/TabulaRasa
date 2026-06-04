import React from "react";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { EventLogPanel, RuntimePanel } from "./DebugPanels";
import type { SimulationSnapshot, SimulationStatus } from "@/types/simulation";

describe("DebugPanels", () => {
  it("renders runtime timing and system diagnostics", () => {
    render(
      <RuntimePanel
        status={status}
        snapshot={snapshot}
        configDraft={status.config}
        onConfigDraftChange={vi.fn()}
      />
    );

    expect(screen.getByText("Runtime")).toBeTruthy();
    expect(screen.getByText("Need Decay System")).toBeTruthy();
    expect(screen.getByText("Evaluation / priority 1")).toBeTruthy();
    expect(screen.getByText("12345")).toBeTruthy();
    expect(screen.getByText("Task statuses")).toBeTruthy();
    expect(screen.getByText("pending 1 / done 1 / failed 0")).toBeTruthy();
  });

  it("renders event log rows", () => {
    render(
      <EventLogPanel
        events={snapshot.recentEvents}
        eventTypes={["tick.started", "action.result"]}
        eventScope="recent"
        eventType="all"
        onEventScopeChange={vi.fn()}
        onEventTypeChange={vi.fn()}
      />
    );

    expect(screen.getByText("Events")).toBeTruthy();
    expect(screen.getAllByText("tick.started").length).toBeGreaterThan(1);
    expect(screen.getByText("Action Execution System")).toBeTruthy();
    expect(screen.getByText("agent-1 Eat succeeded.")).toBeTruthy();
  });
});

const status: SimulationStatus = {
  currentTick: 1,
  status: "Paused",
  minimumTick: 0,
  maximumTick: 1,
  gridWidth: 10,
  gridHeight: 10,
  agentCount: 1,
  aliveAgentCount: 1,
  deadAgentCount: 0,
  foodCount: 1,
  config: {
    seed: 12345,
    worldWidth: 10,
    worldHeight: 10,
    tickIntervalMilliseconds: 500,
    initialAgentCount: 1,
    initialFoodCount: 1,
    eventHistoryLimit: 100,
    snapshotHistoryLimit: 100,
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
  },
  latestTickSummary: {
    tick: 1,
    durationMilliseconds: 1.25,
    eventCount: 2
  },
  eventHistoryMinimumTick: 0,
  eventHistoryMaximumTick: 1
};

const snapshot: SimulationSnapshot = {
  tick: 1,
  grid: {
    width: 10,
    height: 10,
    blockedCells: [],
    terrainCells: [],
    occupiedCells: []
  },
  agents: [],
  resourceDefinitions: [],
  resourceContainers: [],
  activeMovements: [],
  goals: [
    {
      id: "goal-1",
      agentId: "agent-1",
      needKey: "Hunger",
      reason: "Resolve hunger with food.",
      priority: 70,
      targetId: "food-1",
      targetType: "Food",
      jobId: "job-1",
      status: "Active",
      createdTick: 1,
      lastUpdatedTick: 1,
      failureReason: null
    }
  ],
  jobs: [
    {
      id: "job-1",
      definitionId: "hunger-find-and-eat-food",
      name: "Find And Eat Food",
      status: "Active",
      ownerAgentId: "agent-1",
      goalId: "goal-1",
      taskCount: 3,
      pendingTaskCount: 1,
      assignedTaskCount: 1,
      inProgressTaskCount: 0,
      completedTaskCount: 1,
      failedTaskCount: 0,
      cancelledTaskCount: 0,
      interruptedTaskCount: 0,
      tasks: []
    }
  ],
  reservations: [],
  recentActionResults: [],
  pendingIntentCount: 0,
  pendingActionRequestCount: 0,
  events: [
    {
      tick: 1,
      sequence: 1,
      type: "tick.started",
      sourceSystem: "SimulationEngine",
      message: "Tick 1 started.",
      entityId: null,
      metadata: {}
    }
  ],
  recentEvents: [
    {
      tick: 1,
      sequence: 1,
      type: "tick.started",
      sourceSystem: "SimulationEngine",
      message: "Tick 1 started.",
      entityId: null,
      metadata: {}
    },
    {
      tick: 1,
      sequence: 2,
      type: "action.result",
      sourceSystem: "Action Execution System",
      message: "agent-1 Eat succeeded.",
      entityId: "agent-1",
      metadata: {
        actionType: "Eat",
        succeeded: "True"
      }
    }
  ],
  populationCount: 1,
  aliveAgentCount: 1,
  deadAgentCount: 0,
  speciesPopulation: [
    { speciesId: "human", displayName: "Human", total: 1, alive: 1, dead: 0 },
    { speciesId: "deer", displayName: "Deer", total: 0, alive: 0, dead: 0 },
    { speciesId: "wolf", displayName: "Wolf", total: 0, alive: 0, dead: 0 }
  ],
  environment: {
    dayLengthTicks: 100,
    tickOfDay: 1,
    day: 0,
    phase: "Dawn",
    weather: "Clear",
    temperature: 18
  },
  ecologyStats: {
    plantCount: 3,
    harvestablePlantCount: 2,
    totalPlantYield: 5,
    waterSourceCount: 1,
    totalWaterVolume: 8,
    resourceDepositCount: 1,
    totalDepositQuantity: 5
  },
  plants: [],
  waterSources: [],
  resourceDeposits: [],
  diagnostics: {
    tick: 1,
    startedAt: "2026-06-01T00:00:00Z",
    completedAt: "2026-06-01T00:00:00Z",
    durationMilliseconds: 1.25,
    eventCount: 2,
    systems: [
      {
        phase: "PreUpdate",
        systemName: "Need Decay System",
        priority: 0,
        durationMilliseconds: 0.4,
        emittedEventCount: 0
      },
      {
        phase: "Evaluation",
        systemName: "Action Request Creation System",
        priority: 1,
        durationMilliseconds: 0.2,
        emittedEventCount: 1
      }
    ]
  }
};
