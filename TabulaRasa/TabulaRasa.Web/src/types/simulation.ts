export type SimulationStatus = {
  currentTick: number;
  status: "Idle" | "Running" | "Paused" | string;
  minimumTick: number;
  maximumTick: number;
  gridWidth: number;
  gridHeight: number;
  agentCount: number;
  foodCount: number;
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

export type AgentNeeds = {
  hunger: number;
  thirst: number;
  energy: number;
};

export type SimulationSnapshot = {
  tick: number;
  grid: {
    width: number;
    height: number;
    blockedCells: GridCell[];
  };
  agents: AgentSnapshot[];
  food: FoodSnapshot[];
  activeMovements: MovementSnapshot[];
  jobs: JobSnapshot[];
  reservations: ReservationSnapshot[];
  recentActionResults: ActionResultSnapshot[];
  pendingIntentCount: number;
  pendingActionRequestCount: number;
};

export type AgentSnapshot = {
  id: string;
  entityType: string;
  position: Position;
  cell: GridCell;
  footprint: Footprint;
  needs: AgentNeeds;
  movement: MovementSnapshot | null;
};

export type FoodSnapshot = {
  id: string;
  entityType: string;
  position: Position;
  cell: GridCell;
  footprint: Footprint;
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
  };
  agents: EditableAgent[];
  food: EditableFood[];
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
  valueType: "number" | "string" | "boolean" | "gridCells" | string;
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
