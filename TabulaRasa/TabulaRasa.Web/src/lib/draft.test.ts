import { describe, expect, it } from "vitest";
import {
  addAgentDraft,
  addResourceContainerDraft,
  removeAgentDraft,
  removeResourceContainerDraft,
  setTerrainCell,
  toggleBlockedCell,
  updateAgentDraft,
  updateResourceContainerDraft
} from "./draft";
import { getValue, setValue } from "./objectPath";
import type { SimulationDraft } from "@/types/simulation";

const draft: SimulationDraft = {
  tick: 0,
  grid: { width: 10, height: 10, blockedCells: [], terrainCells: [] },
  agents: [{ id: "agent-1", position: { x: 0.5, y: 1 }, inventory: { maxSlots: 8, maxWeight: 10, stacks: [] }, needs: { hunger: 1, thirst: 2, energy: 3, fatigue: 4 } }],
  resourceDefinitions: [{
    id: "food",
    displayName: "Food",
    iconKey: "food",
    unitWeight: 1,
    maxStackQuantity: 10,
    isConsumable: true,
    needEffects: { hungerDelta: -5, thirstDelta: 0, energyDelta: 0, fatigueDelta: 0 }
  }],
  resourceContainers: [{
    id: "resource-container-1",
    position: { x: 1, y: 1 },
    inventory: { maxSlots: 4, maxWeight: 100, stacks: [{ stackId: "food-stack-1", resourceId: "food", quantity: 1 }] }
  }],
  config: null
};

describe("draft helpers", () => {
  it("updates agent fields immutably", () => {
    const next = updateAgentDraft(draft, "agent-1", { needs: { hunger: 4, thirst: 5, energy: 6, fatigue: 7 } });

    expect(next).not.toBe(draft);
    expect(next.agents[0].needs.hunger).toBe(4);
    expect(draft.agents[0].needs.hunger).toBe(1);
  });

  it("updates resource container fields immutably", () => {
    const next = updateResourceContainerDraft(draft, "resource-container-1", { position: { x: 2, y: 2 } });

    expect(next.resourceContainers[0].position.x).toBe(2);
    expect(draft.resourceContainers[0].position.x).toBe(1);
  });

  it("toggles blocked cells", () => {
    const blocked = toggleBlockedCell(draft, { x: 2, y: 3 });
    const unblocked = toggleBlockedCell(blocked, { x: 2, y: 3 });

    expect(blocked.grid.blockedCells).toEqual([{ x: 2, y: 3 }]);
    expect(unblocked.grid.blockedCells).toEqual([]);
  });

  it("sets and clears terrain cells", () => {
    const forest = setTerrainCell(draft, { x: 2, y: 3 }, "Forest");
    const plain = setTerrainCell(forest, { x: 2, y: 3 }, "Plain");

    expect(forest.grid.terrainCells).toEqual([{ cell: { x: 2, y: 3 }, terrainType: "Forest" }]);
    expect(plain.grid.terrainCells).toEqual([]);
  });

  it("reads and writes nested fields by path", () => {
    const next = setValue(draft, "agents", [
      setValue(draft.agents[0], "needs.hunger", 9)
    ]);

    expect(getValue(next, "agents.0.needs.hunger")).toBe(9);
    expect(draft.agents[0].needs.hunger).toBe(1);
  });

  it("adds and removes draft entities", () => {
    const withAgent = addAgentDraft(draft);
    const withContainer = addResourceContainerDraft(withAgent);
    const removedAgent = removeAgentDraft(withContainer, "agent-2");
    const removedContainer = removeResourceContainerDraft(removedAgent, "resource-container-2");

    expect(withAgent.agents.map((agent) => agent.id)).toContain("agent-2");
    expect(withContainer.resourceContainers.map((container) => container.id)).toContain("resource-container-2");
    expect(removedAgent.agents.map((agent) => agent.id)).not.toContain("agent-2");
    expect(removedContainer.resourceContainers.map((container) => container.id)).not.toContain("resource-container-2");
  });
});
