import type { EditableAgent, EditableResourceContainer, EditableResourceDefinition, GridCell, SimulationDraft, TerrainType } from "@/types/simulation";

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

export function updateResourceContainerDraft(
  draft: SimulationDraft,
  id: string,
  patch: Partial<SimulationDraft["resourceContainers"][number]>
): SimulationDraft {
  return {
    ...draft,
    resourceContainers: draft.resourceContainers.map((container) => (container.id === id ? { ...container, ...patch } : container))
  };
}

export function updateResourceDefinitionDraft(
  draft: SimulationDraft,
  id: string,
  patch: Partial<SimulationDraft["resourceDefinitions"][number]>
): SimulationDraft {
  return {
    ...draft,
    resourceDefinitions: draft.resourceDefinitions.map((definition) => (definition.id === id ? { ...definition, ...patch } : definition))
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

export function setTerrainCell(draft: SimulationDraft, cell: GridCell, terrainType: TerrainType): SimulationDraft {
  const terrainCells = draft.grid.terrainCells.filter(
    (candidate) => candidate.cell.x !== cell.x || candidate.cell.y !== cell.y
  );

  return {
    ...draft,
    grid: {
      ...draft.grid,
      terrainCells: terrainType === "Plain"
        ? terrainCells
        : [...terrainCells, { cell, terrainType }]
    }
  };
}

export function addAgentDraft(draft: SimulationDraft): SimulationDraft {
  const id = nextId("agent", draft.agents.map((agent) => agent.id));
  const position = firstOpenPosition(draft);
  const agent: EditableAgent = {
    id,
    position,
    inventory: {
      maxSlots: 8,
      maxWeight: 10,
      stacks: []
    },
    needs: {
      hunger: 1,
      thirst: 0,
      energy: 10,
      fatigue: 0
    }
  };

  return {
    ...draft,
    agents: [...draft.agents, agent]
  };
}

export function addResourceContainerDraft(draft: SimulationDraft): SimulationDraft {
  const id = nextId("resource-container", draft.resourceContainers.map((container) => container.id));
  const container: EditableResourceContainer = {
    id,
    position: firstOpenPosition(draft),
    inventory: {
      maxSlots: 4,
      maxWeight: 100,
      stacks: [{
        stackId: `${id}-food`,
        resourceId: "food",
        quantity: 1
      }]
    }
  };

  return {
    ...draft,
    resourceContainers: [...draft.resourceContainers, container]
  };
}

export function addResourceDefinitionDraft(draft: SimulationDraft): SimulationDraft {
  const id = nextId("resource", draft.resourceDefinitions.map((definition) => definition.id));
  const definition: EditableResourceDefinition = {
    id,
    displayName: "Resource",
    iconKey: "box",
    unitWeight: 1,
    maxStackQuantity: 10,
    isConsumable: false,
    needEffects: {
      hungerDelta: 0,
      thirstDelta: 0,
      energyDelta: 0,
      fatigueDelta: 0
    }
  };

  return {
    ...draft,
    resourceDefinitions: [...draft.resourceDefinitions, definition]
  };
}

export function removeAgentDraft(draft: SimulationDraft, id: string): SimulationDraft {
  return {
    ...draft,
    agents: draft.agents.filter((agent) => agent.id !== id)
  };
}

export function removeResourceContainerDraft(draft: SimulationDraft, id: string): SimulationDraft {
  return {
    ...draft,
    resourceContainers: draft.resourceContainers.filter((container) => container.id !== id)
  };
}

export function removeResourceDefinitionDraft(draft: SimulationDraft, id: string): SimulationDraft {
  return {
    ...draft,
    resourceDefinitions: draft.resourceDefinitions.filter((definition) => definition.id !== id)
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
    ...draft.resourceContainers
      .filter((container) => container.inventory.stacks.length > 0)
      .map((container) => `${Math.floor(container.position.x)}:${Math.floor(container.position.y)}`)
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
