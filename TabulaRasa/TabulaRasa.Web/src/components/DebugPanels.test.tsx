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
      energyDelta: -1
    },
    perceptionRadius: 20,
    movementSpeedPerTick: 0.25,
    pathfinding: {
      allowDiagonalMovement: false,
      maxVisitedCells: 1000
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
    occupiedCells: []
  },
  agents: [],
  food: [],
  activeMovements: [],
  jobs: [],
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
