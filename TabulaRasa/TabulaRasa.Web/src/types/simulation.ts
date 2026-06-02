export type SimulationSummary = {
  simulationId: string;
  name: string;
  status: "Idle" | "Running" | "Paused" | "Stopped" | string;
  currentTick: number;
  gridWidth: number;
  gridHeight: number;
  agentCount: number;
  aliveAgentCount: number;
  deadAgentCount: number;
  foodCount: number;
  createdAt: string;
  updatedAt: string;
};

export type SimulationResourceLimits = {
  maxConcurrentRunningSimulations: number;
  maxTicksPerSecond: number;
  maxAgents: number;
  maxRetainedSnapshots: number;
};

export type SimulationStatus = {
  currentTick: number;
  status: "Idle" | "Running" | "Paused" | "Stopped" | string;
  minimumTick: number;
  maximumTick: number;
  gridWidth: number;
  gridHeight: number;
  agentCount: number;
  aliveAgentCount: number;
  deadAgentCount: number;
  foodCount: number;
  config: SimulationConfig;
  latestTickSummary: SimulationTickSummary | null;
  eventHistoryMinimumTick: number | null;
  eventHistoryMaximumTick: number | null;
};

export type SimulationConfig = {
  seed: number;
  worldWidth: number;
  worldHeight: number;
  tickIntervalMilliseconds: number;
  initialAgentCount: number;
  initialFoodCount: number;
  eventHistoryLimit: number;
  snapshotHistoryLimit: number;
  needDecay: {
    hungerDelta: number;
    thirstDelta: number;
    energyDelta: number;
    fatigueDelta: number;
  };
  perceptionRadius: number;
  movementSpeedPerTick: number;
  pathfinding: {
    allowDiagonalMovement: boolean;
    maxVisitedCells: number;
    maxRepathAttempts: number;
  };
  memory: {
    enabled: boolean;
    maxMemoriesPerAgent: number;
    retentionTicks: number;
    decayPerTick: number;
    minimumStrength: number;
    recallThreshold: number;
  };
  enabledSystems: string[];
};

export type SimulationTickSummary = {
  tick: number;
  durationMilliseconds: number;
  eventCount: number;
};

export type GridCell = {
  x: number;
  y: number;
};

export type Position = {
  x: number;
  y: number;
};

export type Footprint = {
  width: number;
  height: number;
};

export type EntityHealth = {
  current: number;
  maximum: number;
  isDepleted: boolean;
};

export type OccupiedCell = {
  cell: GridCell;
  entityId: string;
  entityType: string;
};

export type TerrainType = "Plain" | "Road" | "Forest" | "Mud";

export type GridTerrainCell = {
  cell: GridCell;
  terrainType: TerrainType | string;
  traversalCost: number;
  speedMultiplier: number;
};

export type EditableGridTerrainCell = {
  cell: GridCell;
  terrainType: TerrainType | string;
};

export type AgentNeeds = {
  hunger: number;
  thirst: number;
  energy: number;
  fatigue: number;
};

export type SimulationSnapshot = {
  tick: number;
  grid: {
    width: number;
    height: number;
    blockedCells: GridCell[];
    terrainCells: GridTerrainCell[];
    occupiedCells: OccupiedCell[];
  };
  agents: AgentSnapshot[];
  food: FoodSnapshot[];
  activeMovements: MovementSnapshot[];
  jobs: JobSnapshot[];
  reservations: ReservationSnapshot[];
  recentActionResults: ActionResultSnapshot[];
  pendingIntentCount: number;
  pendingActionRequestCount: number;
  events: SimulationEvent[];
  recentEvents: SimulationEvent[];
  populationCount: number;
  aliveAgentCount: number;
  deadAgentCount: number;
  diagnostics: SimulationTickDiagnostics | null;
};

export type SimulationEvent = {
  tick: number;
  sequence: number;
  type: string;
  sourceSystem: string;
  message: string;
  entityId: string | null;
  metadata: Record<string, string>;
};

export type SimulationTickDiagnostics = {
  tick: number;
  startedAt: string;
  completedAt: string;
  durationMilliseconds: number;
  eventCount: number;
  systems: SystemExecutionDiagnostic[];
};

export type SystemExecutionDiagnostic = {
  phase: string;
  systemName: string;
  priority: number;
  durationMilliseconds: number;
  emittedEventCount: number;
};

export type AgentSnapshot = {
  id: string;
  entityType: string;
  position: Position;
  cell: GridCell;
  footprint: Footprint;
  occupiedCells: GridCell[];
  occupiesSpace: boolean;
  health: EntityHealth | null;
  isDead: boolean;
  needs: AgentNeeds;
  movement: MovementSnapshot | null;
  perception: AgentPerceptionSnapshot;
  memory: AgentMemorySnapshot;
};

export type AgentPerceptionSnapshot = {
  nearbyEntities: PerceivedEntitySnapshot[];
  opportunities: InteractionOpportunitySnapshot[];
};

export type PerceivedEntitySnapshot = {
  entityId: string;
  entityType: string;
  position: Position;
  isInteractable: boolean;
  channel: "Sight" | "Hearing" | "Smell" | string;
  distance: number;
  certainty: number;
  relevance: number;
};

export type InteractionOpportunitySnapshot = {
  actionType: string;
  targetId: string | null;
  targetPosition: Position;
  sourceEntityId: string | null;
  channel: "Sight" | "Hearing" | "Smell" | string;
  relevance: number;
};

export type AgentMemorySnapshot = {
  memories: AgentMemoryRecordSnapshot[];
};

export type AgentMemoryRecordSnapshot = {
  id: string;
  kind: "Entity" | "Location" | "Event" | "ActionOutcome" | string;
  subjectId: string;
  subjectType: string;
  position: Position;
  createdTick: number;
  lastUpdatedTick: number;
  strength: number;
  certainty: number;
  expiresAtTick: number | null;
  summary: string;
  metadata: Record<string, string>;
};

export type FoodSnapshot = {
  id: string;
  entityType: string;
  position: Position;
  cell: GridCell;
  footprint: Footprint;
  occupiedCells: GridCell[];
  occupiesSpace: boolean;
  health: EntityHealth | null;
  isConsumed: boolean;
};

export type MovementSnapshot = {
  agentId: string;
  requestedAction: string;
  targetId: string | null;
  status: string;
  waypoints: Position[];
  destination: Position;
  currentWaypointIndex: number;
  speedPerTick: number;
  arrivalTolerance: number;
  failureReason: string | null;
  routeCost: number;
  repathCount: number;
  maxRepathAttempts: number;
  stuckTicks: number;
  maxStuckTicks: number;
  lastRepathReason: string | null;
  lastEffectiveSpeedPerTick: number;
};

export type JobSnapshot = {
  id: string;
  definitionId: string;
  name: string;
  status: string;
  taskCount: number;
  pendingTaskCount: number;
  assignedTaskCount: number;
  inProgressTaskCount: number;
  completedTaskCount: number;
  failedTaskCount: number;
  cancelledTaskCount: number;
};

export type ReservationSnapshot = {
  id: string;
  targetType: string;
  targetId: string;
  ownerId: string;
  reservedAtTick: number;
  expiresAtTick: number | null;
};

export type ActionResultSnapshot = {
  agentId: string;
  actionType: string;
  succeeded: boolean;
  reason: string | null;
};

export type SimulationDraft = {
  tick: number;
  grid: {
    width: number;
    height: number;
    blockedCells: GridCell[];
    terrainCells: EditableGridTerrainCell[];
  };
  agents: EditableAgent[];
  food: EditableFood[];
  config: SimulationConfig | null;
};

export type SimulationDraftSchema = {
  stateFields: EditableField[];
  gridFields: EditableField[];
  agentFields: EditableField[];
  foodFields: EditableField[];
};

export type EditableField = {
  path: string;
  label: string;
  valueType: "number" | "string" | "boolean" | "gridCells" | "terrainCells" | string;
  isEditable: boolean;
  sourceType: string;
  sourceProperty: string;
};

export type EditableAgent = {
  id: string;
  position: Position;
  needs: AgentNeeds;
};

export type EditableFood = {
  id: string;
  position: Position;
  isConsumed: boolean;
};

export type Selection =
  | { type: "agent"; id: string }
  | { type: "food"; id: string }
  | { type: "cell"; cell: GridCell }
  | null;

export type HoverInfo = {
  label: string;
  detail: string;
  x: number;
  y: number;
} | null;
