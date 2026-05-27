import type { GridCell, SimulationDraft } from "@/types/simulation";

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
