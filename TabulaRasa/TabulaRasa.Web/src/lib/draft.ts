import type {
  EditableAgent,
  EditablePlant,
  EditableResourceContainer,
  EditableResourceDefinition,
  EditableResourceDeposit,
  EditableWaterSource,
  GridCell,
  SimulationDraft,
  TerrainType
} from "@/types/simulation";

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

export function updatePlantDraft(
  draft: SimulationDraft,
  id: string,
  patch: Partial<SimulationDraft["plants"][number]>
): SimulationDraft {
  return {
    ...draft,
    plants: draft.plants.map((plant) => (plant.id === id ? { ...plant, ...patch } : plant))
  };
}

export function updateWaterSourceDraft(
  draft: SimulationDraft,
  id: string,
  patch: Partial<SimulationDraft["waterSources"][number]>
): SimulationDraft {
  return {
    ...draft,
    waterSources: draft.waterSources.map((waterSource) => (waterSource.id === id ? { ...waterSource, ...patch } : waterSource))
  };
}

export function updateResourceDepositDraft(
  draft: SimulationDraft,
  id: string,
  patch: Partial<SimulationDraft["resourceDeposits"][number]>
): SimulationDraft {
  return {
    ...draft,
    resourceDeposits: draft.resourceDeposits.map((deposit) => (deposit.id === id ? { ...deposit, ...patch } : deposit))
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
    renewability: "Renewable",
    category: "general",
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

export function addPlantDraft(draft: SimulationDraft): SimulationDraft {
  const id = nextId("plant", draft.plants.map((plant) => plant.id));
  const plant: EditablePlant = {
    id,
    position: firstOpenPosition(draft),
    resourceId: "food",
    yield: 2,
    maxYield: 3,
    regrowthTicks: 5,
    ticksUntilRegrowth: 0,
    decayTicksAfterDepleted: 20,
    depletedTicks: 0,
    isDecayed: false
  };

  return {
    ...draft,
    plants: [...draft.plants, plant]
  };
}

export function addWaterSourceDraft(draft: SimulationDraft): SimulationDraft {
  const id = nextId("water-source", draft.waterSources.map((waterSource) => waterSource.id));
  const waterSource: EditableWaterSource = {
    id,
    position: firstOpenPosition(draft),
    currentVolume: 8,
    maxVolume: 10,
    refillPerRainTick: 0.5,
    evaporationPerHeatTick: 0.25
  };

  return {
    ...draft,
    waterSources: [...draft.waterSources, waterSource]
  };
}

export function addResourceDepositDraft(draft: SimulationDraft): SimulationDraft {
  const id = nextId("resource-deposit", draft.resourceDeposits.map((deposit) => deposit.id));
  const deposit: EditableResourceDeposit = {
    id,
    position: firstOpenPosition(draft),
    resourceId: "stone",
    quantity: 5,
    maxQuantity: 5
  };

  return {
    ...draft,
    resourceDeposits: [...draft.resourceDeposits, deposit]
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

export function removePlantDraft(draft: SimulationDraft, id: string): SimulationDraft {
  return {
    ...draft,
    plants: draft.plants.filter((plant) => plant.id !== id)
  };
}

export function removeWaterSourceDraft(draft: SimulationDraft, id: string): SimulationDraft {
  return {
    ...draft,
    waterSources: draft.waterSources.filter((waterSource) => waterSource.id !== id)
  };
}

export function removeResourceDepositDraft(draft: SimulationDraft, id: string): SimulationDraft {
  return {
    ...draft,
    resourceDeposits: draft.resourceDeposits.filter((deposit) => deposit.id !== id)
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
      .map((container) => `${Math.floor(container.position.x)}:${Math.floor(container.position.y)}`),
    ...draft.plants
      .filter((plant) => !plant.isDecayed)
      .map((plant) => `${Math.floor(plant.position.x)}:${Math.floor(plant.position.y)}`),
    ...draft.waterSources.map((waterSource) => `${Math.floor(waterSource.position.x)}:${Math.floor(waterSource.position.y)}`),
    ...draft.resourceDeposits
      .filter((deposit) => deposit.quantity > 0)
      .map((deposit) => `${Math.floor(deposit.position.x)}:${Math.floor(deposit.position.y)}`)
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
