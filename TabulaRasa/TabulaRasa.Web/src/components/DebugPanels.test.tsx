import React from "react";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { DiscoveryTimelineMarkers, EventLogPanel, GenealogyPanel, KnowledgePanel, RuntimePanel, SocialGraphPanel } from "./DebugPanels";
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
    expect(screen.getByText("Selection pressure")).toBeTruthy();
    expect(screen.getByText("Perception")).toBeTruthy();
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

  it("renders the social graph", () => {
    const onSelectAgent = vi.fn();

    render(
      <SocialGraphPanel
        snapshot={snapshot}
        selectedAgentId="agent-1"
        onSelectAgent={onSelectAgent}
      />
    );

    expect(screen.getByText("Social")).toBeTruthy();
    expect(screen.getAllByText("agent-1").length).toBeGreaterThan(0);
    expect(screen.getByText("agent-1 -> agent-2")).toBeTruthy();
  });

  it("renders knowledge catalog and group knowledge", () => {
    render(
      <KnowledgePanel
        snapshot={knowledgeSnapshot}
        selectedAgentId="agent-1"
        onSelectAgent={vi.fn()}
      />
    );

    expect(screen.getByText("Knowledge")).toBeTruthy();
    expect(screen.getByText("Stone Knapping")).toBeTruthy();
    expect(screen.getByText("known / chance 0.7")).toBeTruthy();
    expect(screen.getByText("Human species")).toBeTruthy();
    expect(screen.getAllByText("agent-1").length).toBeGreaterThan(1);
    expect(screen.getByText("1 recipes known")).toBeTruthy();
  });

  it("renders genealogy for the selected agent", () => {
    const onSelectAgent = vi.fn();

    render(
      <GenealogyPanel
        snapshot={genealogySnapshot}
        selectedAgentId="agent-2"
        onSelectAgent={onSelectAgent}
      />
    );

    expect(screen.getByText("Family")).toBeTruthy();
    expect(screen.getAllByText("agent-2").length).toBeGreaterThan(0);
    expect(screen.getAllByText("agent-1").length).toBeGreaterThan(1);
    expect(screen.getByText("Parents")).toBeTruthy();
    expect(screen.getByText("Offspring")).toBeTruthy();
  });

  it("renders discovery timeline markers", () => {
    const onSelectTick = vi.fn();

    render(
      <DiscoveryTimelineMarkers
        markers={knowledgeSnapshot.discoveryMarkers}
        minimumTick={0}
        maximumTick={10}
        onSelectTick={onSelectTick}
      />
    );

    const marker = screen.getByTitle("Stone Knapping discovered by agent-1 at tick 4");
    fireEvent.click(marker);

    expect(marker).toBeTruthy();
    expect(onSelectTick).toHaveBeenCalledWith(4);
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
    enabledSystems: ["need-decay", "social", "planning"]
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
  evolution: {
    currentTraits: [
      { trait: "perception", average: 0.5, minimum: 0.4, maximum: 0.6, aliveAverage: 0.5, deadAverage: 0 },
      { trait: "speed", average: 0.5, minimum: 0.4, maximum: 0.6, aliveAverage: 0.5, deadAverage: 0 }
    ],
    traitHistory: [
      { tick: 1, trait: "perception", average: 0.5, minimum: 0.4, maximum: 0.6, aliveAverage: 0.5, deadAverage: 0 }
    ]
  },
  socialGraph: {
    nodes: [
      {
        agentId: "agent-1",
        speciesId: "human",
        isDead: false,
        position: { x: 0.5, y: 0.5 },
        groupIds: ["species:human"]
      },
      {
        agentId: "agent-2",
        speciesId: "human",
        isDead: false,
        position: { x: 1.5, y: 0.5 },
        groupIds: ["species:human"]
      }
    ],
    edges: [
      {
        fromAgentId: "agent-1",
        toAgentId: "agent-2",
        familiarity: 0.6,
        trust: 0.4,
        fear: 0.1,
        affinity: 0.3,
        interactionCount: 2,
        lastInteractionTick: 1,
        sharedGroupIds: ["species:human"]
      }
    ]
  },
  recipeCatalog: [],
  groupKnowledge: [],
  discoveryMarkers: [],
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

const knowledgeSnapshot = {
  ...snapshot,
  agents: [
    {
      id: "agent-1",
      knowledge: {
        records: [
          {
            id: "knowledge:Recipe:stone-knapping",
            kind: "Recipe",
            subjectId: "stone-knapping",
            displayName: "Stone Knapping",
            discoveredTick: 4,
            lastUpdatedTick: 4,
            source: "Experiment",
            sourceAgentId: null,
            metadata: { description: "Shape stone." }
          }
        ]
      }
    }
  ],
  recipeCatalog: [
    {
      id: "stone-knapping",
      displayName: "Stone Knapping",
      description: "Shape stone.",
      inputs: [{ resourceId: "stone", quantity: 2 }],
      tools: [],
      outputs: [{ resourceId: "stone-tool", quantity: 1 }],
      unlocks: [{ id: "use-stone-tool", displayName: "Use Stone Tool", description: "Use a stone tool." }],
      discoveryChance: 0.65
    }
  ],
  groupKnowledge: [
    {
      groupId: "species:human",
      displayName: "Human species",
      memberAgentIds: ["agent-1"],
      knownRecipeIds: ["stone-knapping"],
      knownActionUnlockIds: ["use-stone-tool"]
    }
  ],
  discoveryMarkers: [
    {
      tick: 4,
      agentId: "agent-1",
      recipeId: "stone-knapping",
      displayName: "Stone Knapping",
      source: "Experiment"
    }
  ]
} as SimulationSnapshot;

const genealogySnapshot = {
  ...snapshot,
  agents: [
    {
      id: "agent-1",
      entityType: "AgentEntity",
      position: { x: 0.5, y: 0.5 },
      cell: { x: 0, y: 0 },
      footprint: { width: 0.8, height: 0.8 },
      occupiedCells: [{ x: 0, y: 0 }],
      occupiesSpace: true,
      health: null,
      isDead: false,
      speciesId: "deer",
      ageTicks: 80,
      bornTick: 0,
      parentIds: [],
      offspringIds: ["agent-2"],
      lastReproducedTick: 4,
      deathTick: null,
      deathCause: null,
      inventory: { maxSlots: 8, maxWeight: 10, usedSlots: 0, usedWeight: 0, stacks: [] },
      needs: { hunger: 1, thirst: 1, energy: 10, fatigue: 0 },
      traits: { perception: 0.4, speed: 0.6, metabolism: 0.5, riskTolerance: 0.3, learningRate: 0.7 },
      movement: null,
      currentGoal: null,
      taskQueue: [],
      perception: { nearbyEntities: [], opportunities: [] },
      memory: { memories: [] },
      social: { relationships: [], groups: [] },
      knowledge: { records: [] },
      decision: null,
      learning: { entries: [] }
    },
    {
      id: "agent-2",
      entityType: "AgentEntity",
      position: { x: 1.5, y: 0.5 },
      cell: { x: 1, y: 0 },
      footprint: { width: 0.8, height: 0.8 },
      occupiedCells: [{ x: 1, y: 0 }],
      occupiesSpace: true,
      health: null,
      isDead: false,
      speciesId: "deer",
      ageTicks: 5,
      bornTick: 4,
      parentIds: ["agent-1"],
      offspringIds: [],
      lastReproducedTick: null,
      deathTick: null,
      deathCause: null,
      inventory: { maxSlots: 8, maxWeight: 10, usedSlots: 0, usedWeight: 0, stacks: [] },
      needs: { hunger: 1, thirst: 1, energy: 10, fatigue: 0 },
      traits: { perception: 0.5, speed: 0.5, metabolism: 0.5, riskTolerance: 0.5, learningRate: 0.5 },
      movement: null,
      currentGoal: null,
      taskQueue: [],
      perception: { nearbyEntities: [], opportunities: [] },
      memory: { memories: [] },
      social: { relationships: [], groups: [] },
      knowledge: { records: [] },
      decision: null,
      learning: { entries: [] }
    }
  ]
} as SimulationSnapshot;
