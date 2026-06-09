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

export type SimulationRun = {
  simulationId: string;
  name: string;
  status: "Idle" | "Running" | "Paused" | "Stopped" | string;
  currentTick: number;
  minimumTick: number;
  maximumTick: number;
  agentCount: number;
  aliveAgentCount: number;
  deadAgentCount: number;
  storageBytes: number;
  checkpointBytes: number;
  eventBytes: number;
  createdAt: string;
  updatedAt: string;
  sourceSimulationId: string | null;
  sourceTick: number | null;
};

export type SimulationRunPage = {
  runs: SimulationRun[];
  offset: number;
  limit: number;
  total: number;
};

export type SimulationCheckpointSummary = {
  simulationId: string;
  tick: number;
  payloadBytes: number;
  isCompressed: boolean;
  createdAt: string;
};

export type SaveSimulationResponse = {
  simulationId: string;
  tick: number;
  savedAt: string;
  checkpointBytes: number;
};

export type ScenarioExport = {
  name: string;
  version: number;
  exportedAt: string;
  scenario: SimulationDraft;
};

export type RetentionResult = {
  deletedRuns: number;
  deletedCheckpoints: number;
  deletedEvents: number;
  deletedTickSummaries: number;
  removedBytes: number;
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
  traits: TraitConfig;
  environment: {
    dayLengthTicks: number;
    weatherChangeIntervalTicks: number;
    baseTemperature: number;
  };
  ecology: {
    initialPlantCount: number;
    initialWaterSourceCount: number;
    initialResourceDepositCount: number;
    plantRegrowthTicks: number;
    plantDecayTicksAfterDepleted: number;
    waterRefillPerRainTick: number;
    waterEvaporationPerHeatTick: number;
  };
  speciesPopulation: SpeciesPopulationConfig;
  enabledSystems: string[];
};

export type SpeciesPopulationConfig = {
  human: number;
  deer: number;
  wolf: number;
};

export type TraitConfig = {
  initialVariation: number;
  mutationChancePerTrait: number;
  mutationDelta: number;
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

export type TerrainType = "Plain" | "Road" | "Forest" | "Mud" | "Water";

export type GridTerrainCell = {
  cell: GridCell;
  terrainType: TerrainType | string;
  traversalCost: number;
  speedMultiplier: number;
  perceptionMultiplier: number;
  hungerDeltaMultiplier: number;
  thirstDeltaMultiplier: number;
  fatigueDeltaMultiplier: number;
  isWater: boolean;
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

export type ResourceNeedEffects = {
  hungerDelta: number;
  thirstDelta: number;
  energyDelta: number;
  fatigueDelta: number;
};

export type ResourceDefinition = {
  id: string;
  displayName: string;
  iconKey: string;
  unitWeight: number;
  maxStackQuantity: number;
  isConsumable: boolean;
  needEffects: ResourceNeedEffects;
  renewability: "Renewable" | "Nonrenewable" | string;
  category: string;
};

export type ResourceStack = {
  stackId: string;
  resourceId: string;
  quantity: number;
};

export type Inventory = {
  maxSlots: number;
  maxWeight: number;
  usedSlots: number;
  usedWeight: number;
  stacks: ResourceStack[];
};

export type EditableInventory = {
  maxSlots: number;
  maxWeight: number;
  stacks: EditableResourceStack[];
};

export type EditableResourceStack = {
  stackId: string;
  resourceId: string;
  quantity: number;
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
  resourceDefinitions: ResourceDefinition[];
  resourceContainers: ResourceContainerSnapshot[];
  activeMovements: MovementSnapshot[];
  goals: GoalSnapshot[];
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
  speciesPopulation: SpeciesPopulationCount[];
  socialGraph: SocialGraphSnapshot;
  evolution: EvolutionSummary;
  recipeCatalog: RecipeDefinitionSnapshot[];
  groupKnowledge: GroupKnowledgeSnapshot[];
  discoveryMarkers: DiscoveryMarkerSnapshot[];
  diagnostics: SimulationTickDiagnostics | null;
  environment: EnvironmentState | null;
  ecologyStats: EcologyStats | null;
  plants: PlantSnapshot[];
  waterSources: WaterSourceSnapshot[];
  resourceDeposits: ResourceDepositSnapshot[];
};

export type EnvironmentState = {
  dayLengthTicks: number;
  tickOfDay: number;
  day: number;
  phase: "Dawn" | "Day" | "Dusk" | "Night" | string;
  weather: "Clear" | "Rain" | "Heat" | "Cold" | string;
  temperature: number;
};

export type EcologyStats = {
  plantCount: number;
  harvestablePlantCount: number;
  totalPlantYield: number;
  waterSourceCount: number;
  totalWaterVolume: number;
  resourceDepositCount: number;
  totalDepositQuantity: number;
};

export type SpeciesPopulationCount = {
  speciesId: string;
  displayName: string;
  total: number;
  alive: number;
  dead: number;
};

export type AgentTraits = {
  perception: number;
  speed: number;
  metabolism: number;
  riskTolerance: number;
  learningRate: number;
};

export type EvolutionSummary = {
  currentTraits: PopulationTraitMetric[];
  traitHistory: TraitHistoryPoint[];
};

export type PopulationTraitMetric = {
  trait: string;
  average: number;
  minimum: number;
  maximum: number;
  aliveAverage: number;
  deadAverage: number;
};

export type TraitHistoryPoint = PopulationTraitMetric & {
  tick: number;
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
  speciesId: string;
  ageTicks: number;
  bornTick: number;
  parentIds: string[];
  offspringIds: string[];
  lastReproducedTick: number | null;
  deathTick: number | null;
  deathCause: string | null;
  inventory: Inventory;
  needs: AgentNeeds;
  traits: AgentTraits;
  movement: MovementSnapshot | null;
  currentGoal: GoalSnapshot | null;
  taskQueue: TaskSnapshot[];
  perception: AgentPerceptionSnapshot;
  memory: AgentMemorySnapshot;
  social: AgentSocialSnapshot;
  knowledge: AgentKnowledgeSnapshot;
  decision: AgentDecisionSnapshot | null;
  learning: AgentLearningSnapshot;
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

export type AgentSocialSnapshot = {
  relationships: SocialRelationshipSnapshot[];
  groups: SocialGroupMembershipSnapshot[];
};

export type AgentKnowledgeSnapshot = {
  records: KnowledgeRecordSnapshot[];
};

export type KnowledgeRecordSnapshot = {
  id: string;
  kind: "Recipe" | "ActionUnlock" | string;
  subjectId: string;
  displayName: string;
  discoveredTick: number;
  lastUpdatedTick: number;
  source: string;
  sourceAgentId: string | null;
  metadata: Record<string, string>;
};

export type SocialRelationshipSnapshot = {
  agentId: string;
  otherAgentId: string;
  familiarity: number;
  trust: number;
  fear: number;
  affinity: number;
  interactionCount: number;
  createdTick: number;
  lastUpdatedTick: number;
  lastSeenTick: number | null;
  lastInteractionTick: number | null;
  sharedGroupIds: string[];
};

export type SocialGroupMembershipSnapshot = {
  groupId: string;
  displayName: string;
  kind: string;
  joinedTick: number;
};

export type SocialGraphSnapshot = {
  nodes: SocialGraphNode[];
  edges: SocialGraphEdge[];
};

export type GroupKnowledgeSnapshot = {
  groupId: string;
  displayName: string;
  memberAgentIds: string[];
  knownRecipeIds: string[];
  knownActionUnlockIds: string[];
};

export type DiscoveryMarkerSnapshot = {
  tick: number;
  agentId: string;
  recipeId: string;
  displayName: string;
  source: string;
};

export type RecipeDefinitionSnapshot = {
  id: string;
  displayName: string;
  description: string;
  inputs: RecipeIngredientSnapshot[];
  tools: RecipeIngredientSnapshot[];
  outputs: RecipeOutputSnapshot[];
  unlocks: ActionUnlockSnapshot[];
  discoveryChance: number;
};

export type RecipeIngredientSnapshot = {
  resourceId: string;
  quantity: number;
};

export type RecipeOutputSnapshot = {
  resourceId: string;
  quantity: number;
};

export type ActionUnlockSnapshot = {
  id: string;
  displayName: string;
  description: string;
};

export type SocialGraphNode = {
  agentId: string;
  speciesId: string;
  isDead: boolean;
  position: Position;
  groupIds: string[];
};

export type SocialGraphEdge = {
  fromAgentId: string;
  toAgentId: string;
  familiarity: number;
  trust: number;
  fear: number;
  affinity: number;
  interactionCount: number;
  lastInteractionTick: number | null;
  sharedGroupIds: string[];
};

export type AgentDecisionSnapshot = {
  needPressures: Record<string, number>;
  actionScores: AgentActionScoreSnapshot[];
  selectedGoal: string;
  selectedAction: string;
  targetId: string | null;
  contextKey: string;
  explored: boolean;
};

export type AgentActionScoreSnapshot = {
  actionType: string;
  targetId: string | null;
  selectedGoal: string;
  contextKey: string;
  targetType: string;
  channel: string;
  needPressure: number;
  opportunityRelevance: number;
  learnedWeight: number;
  score: number;
};

export type AgentLearningSnapshot = {
  entries: AgentLearningEntrySnapshot[];
};

export type AgentLearningEntrySnapshot = {
  contextKey: string;
  actionType: string;
  attempts: number;
  successes: number;
  failures: number;
  lastOutcomeScore: number;
  averageOutcomeScore: number;
  learnedWeight: number;
};

export type ResourceContainerSnapshot = {
  id: string;
  entityType: string;
  position: Position;
  cell: GridCell;
  footprint: Footprint;
  occupiedCells: GridCell[];
  occupiesSpace: boolean;
  health: EntityHealth | null;
  inventory: Inventory;
};

export type PlantSnapshot = {
  id: string;
  entityType: string;
  position: Position;
  cell: GridCell;
  footprint: Footprint;
  occupiedCells: GridCell[];
  occupiesSpace: boolean;
  health: EntityHealth | null;
  resourceId: string;
  yield: number;
  maxYield: number;
  regrowthTicks: number;
  ticksUntilRegrowth: number;
  decayTicksAfterDepleted: number;
  depletedTicks: number;
  isDecayed: boolean;
};

export type WaterSourceSnapshot = {
  id: string;
  entityType: string;
  position: Position;
  cell: GridCell;
  footprint: Footprint;
  occupiedCells: GridCell[];
  occupiesSpace: boolean;
  currentVolume: number;
  maxVolume: number;
  refillPerRainTick: number;
  evaporationPerHeatTick: number;
};

export type ResourceDepositSnapshot = {
  id: string;
  entityType: string;
  position: Position;
  cell: GridCell;
  footprint: Footprint;
  occupiedCells: GridCell[];
  occupiesSpace: boolean;
  resourceId: string;
  quantity: number;
  maxQuantity: number;
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
  ownerAgentId: string | null;
  goalId: string | null;
  taskCount: number;
  pendingTaskCount: number;
  assignedTaskCount: number;
  inProgressTaskCount: number;
  completedTaskCount: number;
  failedTaskCount: number;
  cancelledTaskCount: number;
  interruptedTaskCount: number;
  tasks: TaskSnapshot[];
};

export type GoalSnapshot = {
  id: string;
  agentId: string;
  needKey: string;
  reason: string;
  priority: number;
  targetId: string | null;
  targetType: string | null;
  jobId: string | null;
  status: string;
  createdTick: number;
  lastUpdatedTick: number;
  failureReason: string | null;
};

export type TaskSnapshot = {
  id: string;
  jobId: string;
  stepId: string;
  definitionId: string;
  name: string;
  status: string;
  executionKind: string;
  assignedAgentId: string | null;
  progressTicks: number;
  requiredProgressTicks: number;
  dispatchCount: number;
  targetId: string | null;
  targetType: string | null;
  atomicAction: string | null;
  selectedGoal: string | null;
  contextKey: string | null;
  failureReason: string | null;
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
  targetId: string | null;
  contextKey: string | null;
  outcomeScore: number | null;
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
  resourceDefinitions: EditableResourceDefinition[];
  resourceContainers: EditableResourceContainer[];
  config: SimulationConfig | null;
  plants: EditablePlant[];
  waterSources: EditableWaterSource[];
  resourceDeposits: EditableResourceDeposit[];
};

export type SimulationDraftSchema = {
  stateFields: EditableField[];
  gridFields: EditableField[];
  agentFields: EditableField[];
  resourceDefinitionFields: EditableField[];
  resourceContainerFields: EditableField[];
  plantFields: EditableField[];
  waterSourceFields: EditableField[];
  resourceDepositFields: EditableField[];
  inventoryFields: EditableField[];
  resourceStackFields: EditableField[];
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
  inventory: EditableInventory;
  needs: AgentNeeds;
  speciesId: string;
  ageTicks: number;
  bornTick: number;
  parentIds: string[];
  offspringIds: string[];
  lastReproducedTick: number | null;
  deathTick: number | null;
  deathCause: string | null;
  traits: AgentTraits;
};

export type EditableResourceDefinition = ResourceDefinition;

export type EditableResourceContainer = {
  id: string;
  position: Position;
  inventory: EditableInventory;
};

export type EditablePlant = {
  id: string;
  position: Position;
  resourceId: string;
  yield: number;
  maxYield: number;
  regrowthTicks: number;
  ticksUntilRegrowth: number;
  decayTicksAfterDepleted: number;
  depletedTicks: number;
  isDecayed: boolean;
};

export type EditableWaterSource = {
  id: string;
  position: Position;
  currentVolume: number;
  maxVolume: number;
  refillPerRainTick: number;
  evaporationPerHeatTick: number;
};

export type EditableResourceDeposit = {
  id: string;
  position: Position;
  resourceId: string;
  quantity: number;
  maxQuantity: number;
};

export type Selection =
  | { type: "agent"; id: string }
  | { type: "resourceContainer"; id: string }
  | { type: "resourceDefinition"; id: string }
  | { type: "plant"; id: string }
  | { type: "waterSource"; id: string }
  | { type: "resourceDeposit"; id: string }
  | { type: "cell"; cell: GridCell }
  | null;

export type HoverInfo = {
  label: string;
  detail: string;
  x: number;
  y: number;
} | null;
