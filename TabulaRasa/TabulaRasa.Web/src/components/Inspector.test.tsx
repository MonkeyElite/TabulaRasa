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
        speedMultiplier: 0.5
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
      needs: { hunger: 1, thirst: 0, energy: 2 },
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
      }
    }
  ],
  food: [],
  activeMovements: [],
  jobs: [],
  reservations: [],
  recentActionResults: [],
  pendingIntentCount: 0,
  pendingActionRequestCount: 0,
  events: [],
  recentEvents: [],
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
    needs: agent.needs
  })),
  food: [],
  config: null
};

const schema: SimulationDraftSchema = {
  stateFields: [],
  gridFields: [],
  agentFields: [],
  foodFields: []
};
