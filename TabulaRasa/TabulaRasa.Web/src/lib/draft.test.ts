import { describe, expect, it } from "vitest";
import {
  addAgentDraft,
  addFoodDraft,
  removeAgentDraft,
  removeFoodDraft,
  toggleBlockedCell,
  updateAgentDraft,
  updateFoodDraft
} from "./draft";
import { getValue, setValue } from "./objectPath";
import type { SimulationDraft } from "@/types/simulation";

const draft: SimulationDraft = {
  tick: 0,
  grid: { width: 10, height: 10, blockedCells: [] },
  agents: [{ id: "agent-1", position: { x: 0.5, y: 1 }, needs: { hunger: 1, thirst: 2, energy: 3 } }],
  food: [{ id: "food-1", position: { x: 1, y: 1 }, isConsumed: false }]
};

describe("draft helpers", () => {
  it("updates agent fields immutably", () => {
    const next = updateAgentDraft(draft, "agent-1", { needs: { hunger: 4, thirst: 5, energy: 6 } });

    expect(next).not.toBe(draft);
    expect(next.agents[0].needs.hunger).toBe(4);
    expect(draft.agents[0].needs.hunger).toBe(1);
  });

  it("updates food fields immutably", () => {
    const next = updateFoodDraft(draft, "food-1", { isConsumed: true });

    expect(next.food[0].isConsumed).toBe(true);
    expect(draft.food[0].isConsumed).toBe(false);
  });

  it("toggles blocked cells", () => {
    const blocked = toggleBlockedCell(draft, { x: 2, y: 3 });
    const unblocked = toggleBlockedCell(blocked, { x: 2, y: 3 });

    expect(blocked.grid.blockedCells).toEqual([{ x: 2, y: 3 }]);
    expect(unblocked.grid.blockedCells).toEqual([]);
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
    const withFood = addFoodDraft(withAgent);
    const removedAgent = removeAgentDraft(withFood, "agent-2");
    const removedFood = removeFoodDraft(removedAgent, "food-2");

    expect(withAgent.agents.map((agent) => agent.id)).toContain("agent-2");
    expect(withFood.food.map((food) => food.id)).toContain("food-2");
    expect(removedAgent.agents.map((agent) => agent.id)).not.toContain("agent-2");
    expect(removedFood.food.map((food) => food.id)).not.toContain("food-2");
  });
});
