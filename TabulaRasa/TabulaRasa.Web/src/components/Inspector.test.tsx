import React from "react";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { Inspector } from "./Inspector";
import type { SimulationDraft, SimulationDraftSchema, SimulationSnapshot } from "@/types/simulation";

describe("Inspector", () => {
  it("renders active route diagnostics for selected agents", () => {
    render(
      <Inspector
        snapshot={snapshot}
        draft={null}
        schema={null}
        selection={{ type: "agent", id: "agent-1" }}
        editing={false}
        canEdit={false}
        onSelect={vi.fn()}
        onDraftChange={vi.fn()}
        onTerrainChange={vi.fn()}
      />
    );

    expect(screen.getByText("Route cost")).toBeTruthy();
    expect(screen.getByText("4.50")).toBeTruthy();
    expect(screen.getByText("Repaths")).toBeTruthy();
    expect(screen.getByText("1 / 3")).toBeTruthy();
    expect(screen.getByText("Route became blocked.")).toBeTruthy();
  });

  it("edits terrain for selected cells while editing a draft", () => {
    const onTerrainChange = vi.fn();

    render(
      <Inspector
        snapshot={snapshot}
        draft={draft}
        schema={schema}
        selection={{ type: "cell", cell: { x: 1, y: 1 } }}
        editing
        canEdit
        onSelect={vi.fn()}
        onDraftChange={vi.fn()}
        onTerrainChange={onTerrainChange}
      />
    );

    fireEvent.change(screen.getByDisplayValue("Mud"), { target: { value: "Road" } });

    expect(screen.getByText("Mud / cost 3 / speed x0.50")).toBeTruthy();
    expect(onTerrainChange).toHaveBeenCalledWith({ x: 1, y: 1 }, "Road");
  });

  it("renders perceived entities and opportunities for selected agents", () => {
    render(
      <Inspector
        snapshot={snapshot}
        draft={null}
        schema={null}
        selection={{ type: "agent", id: "agent-1" }}
        editing={false}
        canEdit={false}
        onSelect={vi.fn()}
        onDraftChange={vi.fn()}
        onTerrainChange={vi.fn()}
      />
    );

    fireEvent.click(screen.getByText("Perception"));

    expect(screen.getByText("Perceived entities")).toBeTruthy();
    expect(screen.getByText("food-1")).toBeTruthy();
    expect(screen.getByText("Food / Sight / distance 1.25")).toBeTruthy();
    expect(screen.getByText("Opportunities")).toBeTruthy();
    expect(screen.getByText("Eat")).toBeTruthy();
    expect(screen.getByText("target food-1 / source food-1 / Sight")).toBeTruthy();
  });

  it("renders selected agent goal and task queue in the work tab", () => {
    render(
      <Inspector
        snapshot={snapshot}
        draft={null}
        schema={null}
        selection={{ type: "agent", id: "agent-1" }}
        editing={false}
        canEdit={false}
        onSelect={vi.fn()}
        onDraftChange={vi.fn()}
        onTerrainChange={vi.fn()}
      />
    );

    fireEvent.click(screen.getByText("Work"));

    expect(screen.getByText("Current goal")).toBeTruthy();
    expect(screen.getByText("Resolve hunger with food.")).toBeTruthy();
    expect(screen.getByText("Task queue")).toBeTruthy();
    expect(screen.getByText("Move To Food")).toBeTruthy();
    expect(screen.getByText("Assigned / Movement / step move-to-food")).toBeTruthy();
  });

  it("renders empty perception states for selected agents", () => {
    const emptySnapshot: SimulationSnapshot = {
      ...snapshot,
      agents: [
        {
          ...snapshot.agents[0],
          perception: {
            nearbyEntities: [],
            opportunities: []
          }
        }
      ]
    };

    render(
      <Inspector
        snapshot={emptySnapshot}
        draft={null}
        schema={null}
        selection={{ type: "agent", id: "agent-1" }}
        editing={false}
        canEdit={false}
        onSelect={vi.fn()}
        onDraftChange={vi.fn()}
        onTerrainChange={vi.fn()}
      />
    );

    fireEvent.click(screen.getByText("Perception"));

    expect(screen.getByText("No perceived entities.")).toBeTruthy();
    expect(screen.getByText("No opportunities.")).toBeTruthy();
  });

  it("renders selected agent memories in the memory tab", () => {
    render(
      <Inspector
        snapshot={snapshot}
        draft={null}
        schema={null}
        selection={{ type: "agent", id: "agent-1" }}
        editing={false}
        canEdit={false}
        onSelect={vi.fn()}
        onDraftChange={vi.fn()}
        onTerrainChange={vi.fn()}
      />
    );

    fireEvent.click(screen.getByText("Memory"));

    expect(screen.getByText("Remembered locations and entities")).toBeTruthy();
    expect(screen.getByText("Location / Food / strength 0.85 / certainty 0.90")).toBeTruthy();
  });

  it("renders selected agent decision scores and learned outcomes", () => {
    render(
      <Inspector
        snapshot={snapshot}
        draft={null}
        schema={null}
        selection={{ type: "agent", id: "agent-1" }}
        editing={false}
        canEdit={false}
        onSelect={vi.fn()}
        onDraftChange={vi.fn()}
        onTerrainChange={vi.fn()}
      />
    );

    fireEvent.click(screen.getByText("Decision"));

    expect(screen.getByText("Decision explanation")).toBeTruthy();
    expect(screen.getAllByText("Hunger|Food|Sight").length).toBeGreaterThan(1);
    expect(screen.getByText("score 0.93 / weight 0.25 / need 0.70")).toBeTruthy();
    expect(screen.getByText("Learned outcomes")).toBeTruthy();
    expect(screen.getByText("Eat / weight 0.25 / avg 1 / last 1")).toBeTruthy();
  });
});

const snapshot: SimulationSnapshot = {
  tick: 3,
  grid: {
    width: 3,
    height: 3,
    blockedCells: [],
    terrainCells: [
      {
        cell: { x: 1, y: 1 },
        terrainType: "Mud",
        traversalCost: 3,
        speedMultiplier: 0.5,
        perceptionMultiplier: 1,
        hungerDeltaMultiplier: 1,
        thirstDeltaMultiplier: 1,
        fatigueDeltaMultiplier: 1.5,
        isWater: false
      }
    ],
    occupiedCells: []
  },
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
      inventory: {
        maxSlots: 8,
        maxWeight: 10,
        usedSlots: 0,
        usedWeight: 0,
        stacks: []
      },
      needs: { hunger: 1, thirst: 0, energy: 2, fatigue: 0 },
      movement: {
        agentId: "agent-1",
        requestedAction: "Wander",
        targetId: null,
        status: "Repathing",
        waypoints: [{ x: 1.5, y: 0.5 }, { x: 2.5, y: 0.5 }],
        destination: { x: 2.5, y: 0.5 },
        currentWaypointIndex: 0,
        speedPerTick: 1,
        arrivalTolerance: 0.05,
        failureReason: null,
        routeCost: 4.5,
        repathCount: 1,
        maxRepathAttempts: 3,
        stuckTicks: 0,
        maxStuckTicks: 3,
        lastRepathReason: "Route became blocked.",
        lastEffectiveSpeedPerTick: 0.75
      },
      currentGoal: {
        id: "goal-1",
        agentId: "agent-1",
        needKey: "Hunger",
        reason: "Resolve hunger with food.",
        priority: 70,
        targetId: "food-1",
        targetType: "Food",
        jobId: "job-1",
        status: "Active",
        createdTick: 2,
        lastUpdatedTick: 3,
        failureReason: null
      },
      taskQueue: [
        {
          id: "job-1:find-food",
          jobId: "job-1",
          stepId: "find-food",
          definitionId: "find-food",
          name: "Find Food",
          status: "Completed",
          executionKind: "Progress",
          assignedAgentId: "agent-1",
          progressTicks: 1,
          requiredProgressTicks: 1,
          dispatchCount: 0,
          targetId: "food-1",
          targetType: "food",
          atomicAction: null,
          selectedGoal: "Hunger",
          contextKey: "Hunger|Food|Task",
          failureReason: null
        },
        {
          id: "job-1:move-to-food",
          jobId: "job-1",
          stepId: "move-to-food",
          definitionId: "move-to-food",
          name: "Move To Food",
          status: "Assigned",
          executionKind: "Movement",
          assignedAgentId: "agent-1",
          progressTicks: 0,
          requiredProgressTicks: 1,
          dispatchCount: 1,
          targetId: "food-1",
          targetType: "food",
          atomicAction: "Eat",
          selectedGoal: "Hunger",
          contextKey: "Hunger|Food|Task",
          failureReason: null
        }
      ],
      perception: {
        nearbyEntities: [
          {
            entityId: "food-1",
            entityType: "Food",
            position: { x: 1.5, y: 0.5 },
            isInteractable: true,
            channel: "Sight",
            distance: 1.25,
            certainty: 1,
            relevance: 0.5
          }
        ],
        opportunities: [
          {
            actionType: "Eat",
            targetId: "food-1",
            targetPosition: { x: 1, y: 0.5 },
            sourceEntityId: "food-1",
            channel: "Sight",
            relevance: 0.5
          }
        ]
      },
      memory: {
        memories: [
          {
            id: "location:Food:food-1",
            kind: "Location",
            subjectId: "food-1",
            subjectType: "Food",
            position: { x: 1.5, y: 0.5 },
            createdTick: 1,
            lastUpdatedTick: 3,
            strength: 0.85,
            certainty: 0.9,
            expiresAtTick: 83,
            summary: "Remembered Food location for food-1.",
            metadata: {}
          }
        ]
      },
      decision: {
        needPressures: {
          Hunger: 0.7,
          Thirst: 0.1,
          Fatigue: 0,
          LowEnergy: 0.8
        },
        actionScores: [
          {
            actionType: "Eat",
            targetId: "food-1",
            selectedGoal: "Hunger",
            contextKey: "Hunger|Food|Sight",
            targetType: "Food",
            channel: "Sight",
            needPressure: 0.7,
            opportunityRelevance: 0.5,
            learnedWeight: 0.25,
            score: 0.925
          },
          {
            actionType: "Rest",
            targetId: null,
            selectedGoal: "LowEnergy",
            contextKey: "LowEnergy|Self|Internal",
            targetType: "Self",
            channel: "Internal",
            needPressure: 0.8,
            opportunityRelevance: 0,
            learnedWeight: 0,
            score: 0.92
          }
        ],
        selectedGoal: "Hunger",
        selectedAction: "Eat",
        targetId: "food-1",
        contextKey: "Hunger|Food|Sight",
        explored: false
      },
      learning: {
        entries: [
          {
            contextKey: "Hunger|Food|Sight",
            actionType: "Eat",
            attempts: 1,
            successes: 1,
            failures: 0,
            lastOutcomeScore: 1,
            averageOutcomeScore: 1,
            learnedWeight: 0.25
          }
        ]
      }
    }
  ],
  resourceDefinitions: [{
    id: "food",
    displayName: "Food",
    iconKey: "food",
    unitWeight: 1,
    maxStackQuantity: 10,
    isConsumable: true,
    renewability: "Renewable",
    category: "plant",
    needEffects: { hungerDelta: -5, thirstDelta: 0, energyDelta: 0, fatigueDelta: 0 }
  }],
  resourceContainers: [],
  activeMovements: [],
  goals: [{
    id: "goal-1",
    agentId: "agent-1",
    needKey: "Hunger",
    reason: "Resolve hunger with food.",
    priority: 70,
    targetId: "food-1",
    targetType: "Food",
    jobId: "job-1",
    status: "Active",
    createdTick: 2,
    lastUpdatedTick: 3,
    failureReason: null
  }],
  jobs: [],
  reservations: [],
  recentActionResults: [],
  pendingIntentCount: 0,
  pendingActionRequestCount: 0,
  events: [],
  recentEvents: [],
  populationCount: 1,
  aliveAgentCount: 1,
  deadAgentCount: 0,
  environment: {
    dayLengthTicks: 100,
    tickOfDay: 3,
    day: 0,
    phase: "Dawn",
    weather: "Clear",
    temperature: 18
  },
  ecologyStats: {
    plantCount: 0,
    harvestablePlantCount: 0,
    totalPlantYield: 0,
    waterSourceCount: 0,
    totalWaterVolume: 0,
    resourceDepositCount: 0,
    totalDepositQuantity: 0
  },
  plants: [],
  waterSources: [],
  resourceDeposits: [],
  diagnostics: null
};

const draft: SimulationDraft = {
  tick: 3,
  grid: {
    width: 3,
    height: 3,
    blockedCells: [],
    terrainCells: [{ cell: { x: 1, y: 1 }, terrainType: "Mud" }]
  },
  agents: snapshot.agents.map((agent) => ({
    id: agent.id,
    position: agent.position,
    inventory: {
      maxSlots: agent.inventory.maxSlots,
      maxWeight: agent.inventory.maxWeight,
      stacks: agent.inventory.stacks
    },
    needs: agent.needs
  })),
  resourceDefinitions: snapshot.resourceDefinitions,
  resourceContainers: [],
  config: null,
  plants: [],
  waterSources: [],
  resourceDeposits: []
};

const schema: SimulationDraftSchema = {
  stateFields: [],
  gridFields: [],
  agentFields: [],
  resourceDefinitionFields: [],
  resourceContainerFields: [],
  plantFields: [],
  waterSourceFields: [],
  resourceDepositFields: [],
  inventoryFields: [],
  resourceStackFields: []
};
