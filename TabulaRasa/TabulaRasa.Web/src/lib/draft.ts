import type { EditableAgent, EditableFood, GridCell, SimulationDraft } from "@/types/simulation";

export function updateAgentDraft(
  draft: SimulationDraft,
  id: string,
  patch: Partial<SimulationDraft["agents"][number]>
): SimulationDraft {
  return {
    ...draft,
    agents: draft.agents.map((agent) => (agent.id === id ? { ...agent, ...patch } : agent))
  };
}

export function updateFoodDraft(
  draft: SimulationDraft,
  id: string,
  patch: Partial<SimulationDraft["food"][number]>
): SimulationDraft {
  return {
    ...draft,
    food: draft.food.map((food) => (food.id === id ? { ...food, ...patch } : food))
  };
}

export function toggleBlockedCell(draft: SimulationDraft, cell: GridCell): SimulationDraft {
  const exists = draft.grid.blockedCells.some((candidate) => candidate.x === cell.x && candidate.y === cell.y);

  return {
    ...draft,
    grid: {
      ...draft.grid,
      blockedCells: exists
        ? draft.grid.blockedCells.filter((candidate) => candidate.x !== cell.x || candidate.y !== cell.y)
        : [...draft.grid.blockedCells, cell]
    }
  };
}

export function addAgentDraft(draft: SimulationDraft): SimulationDraft {
  const id = nextId("agent", draft.agents.map((agent) => agent.id));
  const position = firstOpenPosition(draft);
  const agent: EditableAgent = {
    id,
    position,
    needs: {
      hunger: 1,
      thirst: 0,
      energy: 0
    }
  };

  return {
    ...draft,
    agents: [...draft.agents, agent]
  };
}

export function addFoodDraft(draft: SimulationDraft): SimulationDraft {
  const id = nextId("food", draft.food.map((food) => food.id));
  const food: EditableFood = {
    id,
    position: firstOpenPosition(draft),
    isConsumed: false
  };

  return {
    ...draft,
    food: [...draft.food, food]
  };
}

export function removeAgentDraft(draft: SimulationDraft, id: string): SimulationDraft {
  return {
    ...draft,
    agents: draft.agents.filter((agent) => agent.id !== id)
  };
}

export function removeFoodDraft(draft: SimulationDraft, id: string): SimulationDraft {
  return {
    ...draft,
    food: draft.food.filter((food) => food.id !== id)
  };
}

function nextId(prefix: string, existingIds: string[]) {
  const existing = new Set(existingIds);
  let index = existingIds.length + 1;
  let id = `${prefix}-${index}`;

  while (existing.has(id)) {
    index++;
    id = `${prefix}-${index}`;
  }

  return id;
}

function firstOpenPosition(draft: SimulationDraft) {
  const occupied = new Set([
    ...draft.agents.map((agent) => `${Math.floor(agent.position.x)}:${Math.floor(agent.position.y)}`),
    ...draft.food.map((food) => `${Math.floor(food.position.x)}:${Math.floor(food.position.y)}`)
  ]);
  const blocked = new Set(draft.grid.blockedCells.map((cell) => `${cell.x}:${cell.y}`));

  for (let y = 0; y < draft.grid.height; y++) {
    for (let x = 0; x < draft.grid.width; x++) {
      const key = `${x}:${y}`;

      if (!occupied.has(key) && !blocked.has(key)) {
        return { x: x + 0.5, y: y + 0.5 };
      }
    }
  }

  return { x: 0.5, y: 0.5 };
}
