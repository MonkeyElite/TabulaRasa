"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import {
  Copy,
  Database,
  Download,
  Eye,
  GitFork,
  Map,
  Pause,
  Play,
  Plus,
  RotateCcw,
  Save,
  SkipBack,
  SkipForward,
  Square,
  StepForward,
  Trash2,
  Upload,
  X
} from "lucide-react";
import { DiscoveryTimelineMarkers, EventLogPanel, GenealogyPanel, KnowledgePanel, RuntimePanel, SocialGraphPanel, WatchPanel } from "@/components/DebugPanels";
import { Inspector } from "@/components/Inspector";
import { WorldCanvas } from "@/components/WorldCanvas";
import { simulationApi } from "@/lib/api";
import { setTerrainCell, toggleBlockedCell, updateAgentDraft, updateResourceContainerDraft } from "@/lib/draft";
import type {
  BuiltInSimulationScenario,
  GridCell,
  HoverInfo,
  Selection,
  SimulationConfig,
  SimulationDraft,
  SimulationDraftSchema,
  SimulationResourceLimits,
  SimulationRunPage,
  SimulationSnapshot,
  SimulationStatus,
  SimulationSummary,
  SimulationTimelinePoint
} from "@/types/simulation";

const systemOptions = [
  ["environment", "Environment"],
  ["ecology", "Ecology"],
  ["lifecycle", "Lifecycle"],
  ["need-decay", "Need decay"],
  ["memory", "Memory"],
  ["social", "Social"],
  ["planning", "Planning"],
  ["goal-generation", "Goals"],
  ["action-request-creation", "Actions"],
  ["route-planning", "Routes"],
  ["job-activation", "Jobs"],
  ["task-assignment", "Assignments"],
  ["task-action-dispatch", "Task actions"],
  ["movement-execution", "Movement"],
  ["task-execution", "Tasks"],
  ["action-execution", "Execution"],
  ["recovery", "Recovery"],
  ["reporting", "Reporting"]
] as const;

const defaultConfig: SimulationConfig = {
  seed: 12345,
  worldWidth: 10,
  worldHeight: 10,
  tickIntervalMilliseconds: 500,
  initialAgentCount: 1,
  initialFoodCount: 1,
  eventHistoryLimit: 100,
  snapshotHistoryLimit: 100,
  needDecay: {
    hungerDelta: 0.08,
    thirstDelta: 0.08,
    energyDelta: -0.02,
    fatigueDelta: 0.04
  },
  needRules: {
    maximumNeedValue: 10,
    maximumEnergyValue: 10,
    eatRecoveryAmount: 5,
    drinkRecoveryAmount: 5,
    restEnergyRecoveryAmount: 4,
    restFatigueRecoveryAmount: 5,
    criticalNeedThreshold: 8,
    harmNeedThreshold: 10,
    exhaustedEnergyThreshold: 0,
    survivalDamagePerTick: 1,
    heatWeatherThirstMultiplier: 1.25,
    hotTemperatureThreshold: 30,
    hotTemperatureThirstBonus: 0.25,
    coldTemperatureThreshold: 5,
    coldTemperatureThirstBonus: -0.15,
    minTemperatureThirstMultiplier: 0.5,
    maxTemperatureThirstMultiplier: 2
  },
  goals: {
    hungerThreshold: 4,
    urgentHungerThreshold: 7.5,
    interruptionPriorityDelta: 20,
    inventionMaxHunger: 4,
    inventionMaxThirst: 4,
    inventionMaxFatigue: 5
  },
  perceptionRadius: 20,
  movementSpeedPerTick: 0.25,
  pathfinding: {
    allowDiagonalMovement: false,
    maxVisitedCells: 1000,
    maxRepathAttempts: 3,
    arrivalTolerance: 0.05,
    interactionTolerance: 0.1,
    agentInteractionRangeBonus: 0.5
  },
  spawnResources: {
    foodStackQuantity: 2,
    plantStartingYield: 4,
    plantMaxYield: 5,
    waterStartingVolume: 16,
    waterMaxVolume: 20,
    depositQuantity: 5,
    depositMaxQuantity: 5
  },
  memory: {
    enabled: true,
    maxMemoriesPerAgent: 100,
    retentionTicks: 80,
    decayPerTick: 0.02,
    minimumStrength: 0.2,
    recallThreshold: 0.35
  },
  traits: {
    initialVariation: 0.12,
    mutationChancePerTrait: 0.08,
    mutationDelta: 0.06
  },
  environment: {
    dayLengthTicks: 100,
    weatherChangeIntervalTicks: 50,
    baseTemperature: 20,
    dawnEndRatio: 0.15,
    dayEndRatio: 0.65,
    duskEndRatio: 0.8,
    clearWeatherWeight: 55,
    rainWeatherWeight: 20,
    heatWeatherWeight: 15,
    coldWeatherWeight: 10,
    dayTemperatureDelta: 4,
    nightTemperatureDelta: -6,
    dawnTemperatureDelta: -2,
    duskTemperatureDelta: 1,
    heatTemperatureDelta: 8,
    coldTemperatureDelta: -8,
    rainTemperatureDelta: -2,
    maxPlantCooling: 3,
    plantCoolingFactor: 30,
    maxWaterCooling: 2,
    waterCoolingPerSource: 0.5
  },
  ecology: {
    initialPlantCount: 3,
    initialWaterSourceCount: 1,
    initialResourceDepositCount: 1,
    plantRegrowthTicks: 5,
    plantDecayTicksAfterDepleted: 20,
    waterRefillPerRainTick: 0.5,
    waterEvaporationPerHeatTick: 0.25,
    collapsePlantYieldThreshold: 0,
    collapseWaterVolumeThreshold: 0,
    recoveryPlantYieldThreshold: 1,
    recoveryWaterVolumeThreshold: 1
  },
  speciesPopulation: {
    human: 1,
    deer: 0,
    wolf: 0
  },
  lifecycle: {
    ageDaysPerTick: 0.01,
    daysPerYear: 365
  },
  speciesRules: {
    human: {
      maxHealth: 10,
      adultAgeDays: 20,
      maxAgeDays: 2000,
      reproductionCooldownTicks: 80,
      perceptionMultiplier: 1,
      movementSpeedMultiplier: 1,
      attackDamage: 2,
      hungerDecayMultiplier: 1,
      thirstDecayMultiplier: 1,
      fatigueDecayMultiplier: 1,
      startingNeeds: { hunger: 1, thirst: 0.5, energy: 10, fatigue: 0 },
      edibleResourceIds: ["food"],
      preySpeciesIds: []
    },
    deer: {
      maxHealth: 6,
      adultAgeDays: 15,
      maxAgeDays: 1200,
      reproductionCooldownTicks: 60,
      perceptionMultiplier: 1.15,
      movementSpeedMultiplier: 1.25,
      attackDamage: 0,
      hungerDecayMultiplier: 1.1,
      thirstDecayMultiplier: 1,
      fatigueDecayMultiplier: 0.9,
      startingNeeds: { hunger: 1.5, thirst: 0.75, energy: 10, fatigue: 0 },
      edibleResourceIds: ["food"],
      preySpeciesIds: []
    },
    wolf: {
      maxHealth: 8,
      adultAgeDays: 18,
      maxAgeDays: 1400,
      reproductionCooldownTicks: 90,
      perceptionMultiplier: 1.25,
      movementSpeedMultiplier: 1.15,
      attackDamage: 4,
      hungerDecayMultiplier: 1.2,
      thirstDecayMultiplier: 1.05,
      fatigueDecayMultiplier: 1,
      startingNeeds: { hunger: 2.5, thirst: 0.75, energy: 10, fatigue: 0 },
      edibleResourceIds: [],
      preySpeciesIds: ["deer"]
    }
  },
  believability: {
    behavior: {
      eat: 1.2,
      drink: 1.15,
      rest: 0.9,
      wander: 0.75,
      social: 0.85,
      reproduce: 0.65,
      flee: 1.25,
      attack: 0.85,
      craft: 0.9,
      experiment: 0.75,
      explorationChance: 0.08,
      personalityInfluence: 0.3
    },
    social: {
      perceptionFamiliarity: 0.08,
      communicationFamiliarity: 0.12,
      communicationTrust: 0.06,
      communicationFear: -0.02,
      communicationAffinity: 0.05,
      attackTrust: -0.2,
      attackFear: 0.35,
      attackAffinity: -0.2,
      reproductionFamiliarity: 0.2,
      reproductionTrust: 0.1,
      reproductionFear: -0.05,
      reproductionAffinity: 0.25
    },
    reproduction: {
      needThreshold: 3.5,
      range: 1.25,
      cooldownScale: 1.35,
      populationPressureInfluence: 0.75,
      parentHungerCost: 1.25,
      parentThirstCost: 0.75,
      parentFatigueCost: 1
    },
    recovery: {
      failedTargetCooldownTicks: 20,
      maxRepeatedActionFailures: 3,
      maxGoalAgeTicks: 100,
      idleRecoveryTicks: 8,
      movementStuckTicks: 3
    }
  },
  enabledSystems: systemOptions.map(([id]) => id)
};

const terrainBrushes = ["Plain", "Road", "Forest", "Mud", "Water"] as const;

export default function Home() {
  const [simulations, setSimulations] = useState<SimulationSummary[]>([]);
  const [runPage, setRunPage] = useState<SimulationRunPage | null>(null);
  const [scenarioPresets, setScenarioPresets] = useState<BuiltInSimulationScenario[]>([]);
  const [activeSimulationId, setActiveSimulationId] = useState<string | null>(null);
  const [limits, setLimits] = useState<SimulationResourceLimits | null>(null);
  const [status, setStatus] = useState<SimulationStatus | null>(null);
  const [snapshot, setSnapshot] = useState<SimulationSnapshot | null>(null);
  const [timeline, setTimeline] = useState<SimulationTimelinePoint[]>([]);
  const [comparisonSimulationId, setComparisonSimulationId] = useState<string>("");
  const [comparisonTimeline, setComparisonTimeline] = useState<SimulationTimelinePoint[]>([]);
  const [draft, setDraft] = useState<SimulationDraft | null>(null);
  const [schema, setSchema] = useState<SimulationDraftSchema | null>(null);
  const [selection, setSelection] = useState<Selection>(null);
  const [viewedTick, setViewedTick] = useState(0);
  const [sliderTick, setSliderTick] = useState(0);
  const [speed, setSpeed] = useState(500);
  const [configDraft, setConfigDraft] = useState<SimulationConfig | null>(null);
  const [eventScope, setEventScope] = useState<"recent" | "current">("recent");
  const [eventType, setEventType] = useState("all");
  const [rightRailTab, setRightRailTab] = useState<"inspect" | "runtime" | "settings" | "events" | "social" | "genealogy" | "knowledge" | "watch">("inspect");
  const [editing, setEditing] = useState(false);
  const [hover, setHover] = useState<HoverInfo>(null);
  const [showNavigationOverlay, setShowNavigationOverlay] = useState(false);
  const [showPerceptionOverlay, setShowPerceptionOverlay] = useState(false);
  const [speciesFilters, setSpeciesFilters] = useState<Record<string, boolean>>({
    human: true,
    deer: true,
    wolf: true
  });
  const [terrainBrush, setTerrainBrush] = useState<(typeof terrainBrushes)[number] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [selectedScenarioName, setSelectedScenarioName] = useState("custom");
  const [createName, setCreateName] = useState("Simulation");
  const [createConfig, setCreateConfig] = useState<SimulationConfig>(defaultConfig);
  const importInputRef = useRef<HTMLInputElement | null>(null);
  const tickRequestIdRef = useRef(0);

  const activeSummary = simulations.find((simulation) => simulation.simulationId === activeSimulationId) ?? null;
  const canEdit = Boolean(status && snapshot && snapshot.tick === status.currentTick);
  const isRunning = status?.status === "Running";
  const isStopped = status?.status === "Stopped";
  const canTune = status?.status === "Paused";

  const loadSimulations = useCallback(async () => {
    const [nextSimulations, nextLimits, nextRuns, nextScenarios] = await Promise.all([
      simulationApi.list(),
      simulationApi.resourceLimits(),
      simulationApi.runs(),
      simulationApi.scenarios()
    ]);
    setSimulations(nextSimulations);
    setLimits(nextLimits);
    setRunPage(nextRuns);
    setScenarioPresets(nextScenarios);
    setActiveSimulationId((current) => {
      if (current && nextSimulations.some((simulation) => simulation.simulationId === current)) {
        return current;
      }

      return nextSimulations[0]?.simulationId ?? null;
    });
    return nextSimulations;
  }, []);

  const loadStatus = useCallback(async (simulationId = activeSimulationId) => {
    if (!simulationId) {
      return null;
    }

    const nextStatus = await simulationApi.status(simulationId);
    setStatus(nextStatus);
    return nextStatus;
  }, [activeSimulationId]);

  const loadCurrent = useCallback(async (simulationId = activeSimulationId) => {
    if (!simulationId) {
      return;
    }

    const [nextStatus, current, nextTimeline] = await Promise.all([
      simulationApi.status(simulationId),
      simulationApi.current(simulationId),
      simulationApi.timeline(simulationId, undefined, undefined, 1)
    ]);
    setStatus(nextStatus);
    setConfigDraft(nextStatus.config);
    setSnapshot(current);
    setTimeline(nextTimeline);
    setViewedTick(current.tick);
    setSliderTick(current.tick);
    await loadSimulations();
  }, [activeSimulationId, loadSimulations]);

  useEffect(() => {
    if (!comparisonSimulationId) {
      setComparisonTimeline([]);
      return;
    }

    simulationApi.timeline(comparisonSimulationId, undefined, undefined, 1)
      .then(setComparisonTimeline)
      .catch((reason: unknown) => setError(toMessage(reason)));
  }, [comparisonSimulationId]);

  useEffect(() => {
    loadSimulations().catch((reason: unknown) => setError(toMessage(reason)));
  }, [loadSimulations]);

  useEffect(() => {
    if (!activeSimulationId) {
      setStatus(null);
      setSnapshot(null);
      return;
    }

    setDraft(null);
    setSchema(null);
    setSelection(null);
    setEditing(false);
    setViewedTick(0);
    setSliderTick(0);
    tickRequestIdRef.current++;
    loadCurrent(activeSimulationId).catch((reason: unknown) => setError(toMessage(reason)));
  }, [activeSimulationId, loadCurrent]);

  useEffect(() => {
    if (!isRunning || !activeSimulationId) {
      return;
    }

    const interval = window.setInterval(() => {
      loadCurrent(activeSimulationId).catch((reason: unknown) => setError(toMessage(reason)));
    }, Math.max(150, Math.min(speed, 1500)));

    return () => window.clearInterval(interval);
  }, [activeSimulationId, isRunning, loadCurrent, speed]);

  useEffect(() => {
    if (!editing || !activeSimulationId) {
      return;
    }

    Promise.all([
      simulationApi.draft(activeSimulationId),
      simulationApi.draftSchema(activeSimulationId)
    ])
      .then(([nextDraft, nextSchema]) => {
        setDraft(nextDraft);
        setSchema(nextSchema);
      })
      .catch((reason: unknown) => setError(toMessage(reason)));
  }, [activeSimulationId, editing]);

  const selectedTickLabel = useMemo(() => {
    if (!status) {
      return "-";
    }

    return `${viewedTick} / ${status.maximumTick}`;
  }, [status, viewedTick]);

  const visibleEvents = useMemo(() => {
    const events = eventScope === "current" ? snapshot?.events ?? [] : snapshot?.recentEvents ?? [];

    return eventType === "all" ? events : events.filter((event) => event.type === eventType);
  }, [eventScope, eventType, snapshot]);

  const eventTypes = useMemo(() => {
    const events = snapshot?.recentEvents ?? [];
    return Array.from(new Set(events.map((event) => event.type))).sort();
  }, [snapshot]);

  function handleScenarioChange(name: string) {
    setSelectedScenarioName(name);
    if (name === "custom") {
      return;
    }

    const preset = scenarioPresets.find((scenario) => scenario.name === name);
    if (!preset) {
      return;
    }

    setCreateName(preset.displayName);
    setCreateConfig(preset.config);
  }

  async function handleCreate(startAfterCreate = false) {
    setError(null);
    const created = await simulationApi.create({ name: createName, config: createConfig });
    if (startAfterCreate) {
      await simulationApi.run(created.simulationId, speed);
    }

    setCreating(false);
    await loadSimulations();
    setActiveSimulationId(created.simulationId);
  }

  async function handleClone() {
    if (!activeSimulationId || !activeSummary) {
      return;
    }

    setError(null);
    const clone = await simulationApi.clone(activeSimulationId, { name: `${activeSummary.name} copy` });
    await loadSimulations();
    setActiveSimulationId(clone.simulationId);
  }

  async function handleLoadRun(runId: string) {
    setError(null);
    const loaded = await simulationApi.loadRun(runId);
    await loadSimulations();
    setActiveSimulationId(loaded.simulationId);
  }

  async function handleForkViewedTick() {
    if (!activeSimulationId || !activeSummary) {
      return;
    }

    setError(null);
    const fork = await simulationApi.forkRun(activeSimulationId, {
      name: `${activeSummary.name} @ ${viewedTick}`,
      sourceTick: viewedTick
    });
    await loadSimulations();
    setActiveSimulationId(fork.simulationId);
  }

  async function handleSave() {
    if (!activeSimulationId) {
      return;
    }

    setError(null);
    await simulationApi.save(activeSimulationId);
    await loadSimulations();
  }

  async function handleExportScenario() {
    if (!activeSimulationId) {
      return;
    }

    setError(null);
    const exported = await simulationApi.exportScenario(activeSimulationId);
    const blob = new Blob([JSON.stringify(exported, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `${exported.name.replace(/[^a-z0-9-_]+/gi, "-").toLowerCase()}-scenario.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  async function handleImportScenarioFile(file: File | null) {
    if (!file) {
      return;
    }

    setError(null);
    const parsed = JSON.parse(await file.text()) as { name?: string; scenario?: SimulationDraft } | SimulationDraft;
    const scenario = "scenario" in parsed && parsed.scenario ? parsed.scenario : parsed as SimulationDraft;
    const name = "name" in parsed && parsed.name ? parsed.name : file.name.replace(/\.[^.]+$/, "");
    const imported = await simulationApi.importScenario({ name, scenario });
    await loadSimulations();
    setActiveSimulationId(imported.simulationId);
    if (importInputRef.current) {
      importInputRef.current.value = "";
    }
  }

  async function handleDelete() {
    if (!activeSimulationId) {
      return;
    }

    setError(null);
    await simulationApi.delete(activeSimulationId);
    const nextSimulations = await loadSimulations();
    setActiveSimulationId(nextSimulations[0]?.simulationId ?? null);
  }

  async function handleStep() {
    if (!activeSimulationId) {
      return;
    }

    setError(null);
    const next = await simulationApi.step(activeSimulationId);
    const nextStatus = await loadStatus(activeSimulationId);
    setStatus(nextStatus);
    setSnapshot(next);
    setViewedTick(next.tick);
    setSliderTick(next.tick);
    await loadSimulations();
  }

  async function handleRunPause() {
    if (!activeSimulationId) {
      return;
    }

    setError(null);
    const nextStatus = isRunning
      ? await simulationApi.pause(activeSimulationId)
      : await simulationApi.run(activeSimulationId, speed);
    setStatus(nextStatus);
    setConfigDraft(nextStatus.config);
    await loadSimulations();
  }

  async function handleStop() {
    if (!activeSimulationId) {
      return;
    }

    setError(null);
    const nextStatus = await simulationApi.stop(activeSimulationId);
    setStatus(nextStatus);
    setConfigDraft(nextStatus.config);
    await loadSimulations();
  }

  async function handleReset() {
    if (!activeSimulationId) {
      return;
    }

    setError(null);
    const next = await simulationApi.reset(activeSimulationId, configDraft ?? status?.config);
    const nextStatus = await loadStatus(activeSimulationId);
    setConfigDraft(nextStatus?.config ?? null);
    setDraft(null);
    setEditing(false);
    setSelection(null);
    setStatus(nextStatus);
    setSnapshot(next);
    setViewedTick(next.tick);
    setSliderTick(next.tick);
    await loadSimulations();
  }

  async function handleApplyConfig() {
    if (!activeSimulationId || !configDraft) {
      return;
    }

    setError(null);
    const nextStatus = await simulationApi.updateConfig(activeSimulationId, configDraft);
    setStatus(nextStatus);
    setConfigDraft(nextStatus.config);
    await loadCurrent(activeSimulationId);
  }

  async function handleRestartFromDraft() {
    if (!activeSimulationId || !draft) {
      return;
    }

    setError(null);
    const next = await simulationApi.restartFromDraft(activeSimulationId, {
      ...draft,
      config: configDraft ?? draft.config
    });
    const nextStatus = await loadStatus(activeSimulationId);
    setStatus(nextStatus);
    setConfigDraft(nextStatus?.config ?? null);
    setSnapshot(next);
    setViewedTick(next.tick);
    setSliderTick(next.tick);
    setEditing(false);
    await loadSimulations();
  }

  async function loadTick(tick: number) {
    if (!activeSimulationId || !status || tick < status.minimumTick || tick > status.maximumTick) {
      return;
    }

    const requestId = ++tickRequestIdRef.current;
    setError(null);
    const next = await simulationApi.tick(activeSimulationId, tick);
    if (requestId !== tickRequestIdRef.current) {
      return;
    }

    setSnapshot(next);
    setViewedTick(next.tick);
    setSliderTick(next.tick);
  }

  function previewTick(tick: number) {
    setSliderTick(tick);
  }

  function commitSliderTick() {
    if (sliderTick !== viewedTick) {
      loadTick(sliderTick).catch((reason: unknown) => setError(toMessage(reason)));
    }
  }

  function moveAgent(id: string, cell: GridCell) {
    if (!draft) {
      return;
    }

    setDraft(updateAgentDraft(draft, id, { position: { x: cell.x + 0.5, y: cell.y + 0.5 } }));
  }

  function moveResourceContainer(id: string, cell: GridCell) {
    if (!draft) {
      return;
    }

    setDraft(updateResourceContainerDraft(draft, id, { position: { x: cell.x + 0.5, y: cell.y + 0.5 } }));
  }

  return (
    <main className="app-shell">
      <header className="toolbar">
        <div className="brand">TabulaRasa</div>
        <button className="icon" onClick={handleRunPause} title={isRunning ? "Pause" : "Run"} disabled={!activeSimulationId || isStopped}>
          {isRunning ? <Pause size={18} /> : <Play size={18} />}
        </button>
        <button className="icon" onClick={handleStep} title="Step" disabled={!activeSimulationId || isRunning || isStopped}>
          <StepForward size={18} />
        </button>
        <button className="icon" onClick={handleStop} title="Stop" disabled={!activeSimulationId || isStopped}>
          <Square size={16} />
        </button>
        <button className="icon" onClick={handleReset} title="Reset" disabled={!activeSimulationId}>
          <RotateCcw size={18} />
        </button>
        <button className="icon" onClick={handleSave} title="Save checkpoint" disabled={!activeSimulationId}>
          <Save size={17} />
        </button>
        <button className="icon" onClick={handleExportScenario} title="Export scenario" disabled={!activeSimulationId}>
          <Download size={17} />
        </button>
        <button className="icon" onClick={() => importInputRef.current?.click()} title="Import scenario">
          <Upload size={17} />
        </button>
        <input
          ref={importInputRef}
          type="file"
          accept="application/json,.json"
          hidden
          onChange={(event) => handleImportScenarioFile(event.target.files?.[0] ?? null).catch((reason: unknown) => setError(toMessage(reason)))}
        />
        <button
          className={`icon ${showNavigationOverlay ? "selected" : ""}`}
          onClick={() => setShowNavigationOverlay((value) => !value)}
          title="Toggle path and cost layer"
          disabled={!snapshot}
        >
          <Map size={18} />
        </button>
        <button
          className={`icon ${showPerceptionOverlay ? "selected" : ""}`}
          onClick={() => setShowPerceptionOverlay((value) => !value)}
          title="Toggle selected agent perception"
          disabled={!snapshot || selection?.type !== "agent" || editing}
        >
          <Eye size={18} />
        </button>
        <select value={speed} onChange={(event) => setSpeed(Number(event.target.value))} title="Tick speed">
          <option value={1000}>1.0s</option>
          <option value={500}>0.5s</option>
          <option value={250}>0.25s</option>
          <option value={100}>0.1s</option>
        </select>
        <button onClick={() => setEditing((value) => !value)} disabled={!canEdit}>
          Edit
        </button>
        {editing && (
          <select
            value={terrainBrush ?? ""}
            onChange={(event) => setTerrainBrush(event.target.value ? event.target.value as (typeof terrainBrushes)[number] : null)}
            title="Terrain brush"
          >
            <option value="">Select</option>
            {terrainBrushes.map((brush) => (
              <option key={brush} value={brush}>{brush}</option>
            ))}
          </select>
        )}
        <button onClick={handleRestartFromDraft} disabled={!editing || !draft}>
          <Save size={16} />
          Restart
        </button>
        <span className="spacer" />
        <span className="metric">
          Tick <strong>{selectedTickLabel}</strong>
        </span>
        <span className="metric">
          Status <strong>{status?.status ?? "-"}</strong>
        </span>
        <span className="metric">
          Alive <strong>{status ? `${status.aliveAgentCount}/${status.agentCount}` : "-"}</strong>
        </span>
      </header>

      <section className="main">
        <aside className="simulation-sidebar">
          <div className="sidebar-header">
            <h2>Simulations</h2>
            <button className="icon" onClick={() => setCreating(true)} title="Create simulation">
              <Plus size={17} />
            </button>
          </div>
          <div className="simulation-list">
            {(runPage?.runs ?? simulations.map((simulation) => ({
              simulationId: simulation.simulationId,
              name: simulation.name,
              status: simulation.status,
              currentTick: simulation.currentTick,
              minimumTick: 0,
              maximumTick: simulation.currentTick,
              agentCount: simulation.agentCount,
              aliveAgentCount: simulation.aliveAgentCount,
              deadAgentCount: simulation.deadAgentCount,
              storageBytes: 0,
              checkpointBytes: 0,
              eventBytes: 0,
              createdAt: simulation.createdAt,
              updatedAt: simulation.updatedAt,
              sourceSimulationId: null,
              sourceTick: null
            }))).map((simulation) => {
              const isLoaded = simulations.some((active) => active.simulationId === simulation.simulationId);
              return (
              <button
                key={simulation.simulationId}
                className={`simulation-row ${simulation.simulationId === activeSimulationId ? "selected" : ""}`}
                onClick={() => {
                  if (isLoaded) {
                    setActiveSimulationId(simulation.simulationId);
                  } else {
                    handleLoadRun(simulation.simulationId).catch((reason: unknown) => setError(toMessage(reason)));
                  }
                }}
              >
                <span>
                  <strong>{simulation.name}</strong>
                  <small>{simulation.status} / ticks {simulation.minimumTick}-{simulation.maximumTick}</small>
                  <small>{formatBytes(simulation.storageBytes)} stored / {isLoaded ? "loaded" : "click to load"}</small>
                </span>
                <span className="pill">{simulation.aliveAgentCount}/{simulation.agentCount}a</span>
              </button>
            );})}
          </div>
          <div className="species-filter-list">
            {(["human", "deer", "wolf"] as const).map((speciesId) => (
              <label key={speciesId}>
                <input
                  type="checkbox"
                  checked={speciesFilters[speciesId] ?? true}
                  onChange={(event) => setSpeciesFilters((current) => ({
                    ...current,
                    [speciesId]: event.target.checked
                  }))}
                />
                <span>{speciesId}</span>
              </label>
            ))}
          </div>
          <div className="sidebar-actions">
            <button onClick={handleClone} disabled={!activeSimulationId}>
              <Copy size={16} />
              Clone
            </button>
            <button onClick={handleForkViewedTick} disabled={!activeSimulationId || !status || viewedTick < status.minimumTick || viewedTick > status.maximumTick}>
              <GitFork size={16} />
              Fork
            </button>
            <button className="danger" onClick={handleDelete} disabled={!activeSimulationId}>
              <Trash2 size={16} />
              Delete
            </button>
          </div>
          {limits && (
            <div className="limit-list">
              <span>Running {limits.maxConcurrentRunningSimulations}</span>
              <span>TPS {limits.maxTicksPerSecond}</span>
              <span>Agents {limits.maxAgents}</span>
              <span>Snapshots {limits.maxRetainedSnapshots}</span>
              <span>Runs {runPage?.total ?? simulations.length}</span>
              <span><Database size={12} /> {formatBytes((runPage?.runs ?? []).reduce((sum, run) => sum + run.storageBytes, 0))}</span>
            </div>
          )}
        </aside>

        <div className="viewport">
          <WorldCanvas
            snapshot={snapshot}
            draft={draft}
            editing={editing}
            canEdit={canEdit}
            selection={selection}
            showNavigationOverlay={showNavigationOverlay}
            showPerceptionOverlay={showPerceptionOverlay}
            perceptionRadius={status?.config.perceptionRadius ?? 0}
            speciesFilters={speciesFilters}
            onSelect={setSelection}
            onMoveAgent={moveAgent}
            onMoveResourceContainer={moveResourceContainer}
            onToggleBlockedCell={(cell) => draft && setDraft(toggleBlockedCell(draft, cell))}
            terrainBrush={terrainBrush}
            onPaintTerrain={(cell, terrainType) => draft && setDraft(setTerrainCell(draft, cell, terrainType))}
            onHover={setHover}
          />
          {hover && (
            <div className="map-tooltip" style={{ left: hover.x + 14, top: hover.y + 14 }}>
              <strong>{hover.label}</strong>
              <span>{hover.detail}</span>
            </div>
          )}
        </div>
        <aside className="right-rail">
          <div className="rail-tabs">
            <button className={rightRailTab === "inspect" ? "selected" : ""} onClick={() => setRightRailTab("inspect")}>Inspect</button>
            <button className={rightRailTab === "runtime" ? "selected" : ""} onClick={() => setRightRailTab("runtime")}>Runtime</button>
            <button className={rightRailTab === "watch" ? "selected" : ""} onClick={() => setRightRailTab("watch")}>Watch</button>
            <button className={rightRailTab === "social" ? "selected" : ""} onClick={() => setRightRailTab("social")}>Social</button>
            <button className={rightRailTab === "genealogy" ? "selected" : ""} onClick={() => setRightRailTab("genealogy")}>Family</button>
            <button className={rightRailTab === "knowledge" ? "selected" : ""} onClick={() => setRightRailTab("knowledge")}>Knowledge</button>
            <button className={rightRailTab === "settings" ? "selected" : ""} onClick={() => setRightRailTab("settings")}>Settings</button>
            <button className={rightRailTab === "events" ? "selected" : ""} onClick={() => setRightRailTab("events")}>Events</button>
          </div>
          <div className="rail-panel">
            {rightRailTab === "inspect" && (
              <Inspector
                snapshot={snapshot}
                draft={draft}
                schema={schema}
                selection={selection}
                onSelect={setSelection}
                editing={editing}
                canEdit={canEdit}
                onDraftChange={setDraft}
                onTerrainChange={(cell, terrainType) => draft && setDraft(setTerrainCell(draft, cell, terrainType))}
              />
            )}
            {rightRailTab === "runtime" && (
              <RuntimePanel
                status={status}
                snapshot={snapshot}
                timeline={timeline}
                comparisonTimeline={comparisonTimeline}
                comparisonSimulationId={comparisonSimulationId}
                simulations={simulations}
                configDraft={null}
                onConfigDraftChange={() => undefined}
                onComparisonSimulationChange={setComparisonSimulationId}
              />
            )}
            {rightRailTab === "watch" && (
              <WatchPanel
                snapshot={snapshot}
                timeline={timeline}
                selection={selection}
                onSelectAgent={(id) => setSelection({ type: "agent", id })}
              />
            )}
            {rightRailTab === "settings" && (
              <SettingsPanel
                config={configDraft}
                canTune={canTune}
                onChange={setConfigDraft}
                onApply={handleApplyConfig}
              />
            )}
            {rightRailTab === "social" && (
              <SocialGraphPanel
                snapshot={snapshot}
                selectedAgentId={selection?.type === "agent" ? selection.id : null}
                onSelectAgent={(id) => setSelection({ type: "agent", id })}
              />
            )}
            {rightRailTab === "genealogy" && (
              <GenealogyPanel
                snapshot={snapshot}
                selectedAgentId={selection?.type === "agent" ? selection.id : null}
                onSelectAgent={(id) => setSelection({ type: "agent", id })}
              />
            )}
            {rightRailTab === "knowledge" && (
              <KnowledgePanel
                snapshot={snapshot}
                selectedAgentId={selection?.type === "agent" ? selection.id : null}
                onSelectAgent={(id) => setSelection({ type: "agent", id })}
              />
            )}
            {rightRailTab === "events" && (
              <EventLogPanel
                events={visibleEvents}
                eventTypes={eventTypes}
                eventScope={eventScope}
                eventType={eventType}
                onEventScopeChange={setEventScope}
                onEventTypeChange={setEventType}
              />
            )}
          </div>
        </aside>
      </section>

      <footer className="timeline">
        <button className="icon" onClick={() => loadTick(viewedTick - 1)} disabled={!status || viewedTick <= status.minimumTick}>
          <SkipBack size={18} />
        </button>
        <input
          type="range"
          min={status?.minimumTick ?? 0}
          max={status?.maximumTick ?? 0}
          value={sliderTick}
          onChange={(event) => previewTick(Number(event.target.value))}
          onPointerUp={commitSliderTick}
          onKeyUp={commitSliderTick}
        />
        <DiscoveryTimelineMarkers
          markers={snapshot?.discoveryMarkers ?? []}
          minimumTick={status?.minimumTick ?? 0}
          maximumTick={status?.maximumTick ?? 0}
          onSelectTick={(tick) => loadTick(tick)}
        />
        <label className="tick-jump">
          <span>Go to</span>
          <input
            type="number"
            min={status?.minimumTick ?? 0}
            max={status?.maximumTick ?? 0}
            value={sliderTick}
            onChange={(event) => previewTick(Number(event.target.value))}
            onBlur={commitSliderTick}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                commitSliderTick();
              }
            }}
          />
        </label>
        <button className="icon" onClick={() => loadTick(viewedTick + 1)} disabled={!status || viewedTick >= status.maximumTick}>
          <SkipForward size={18} />
        </button>
        {error && <span className="error">{error}</span>}
      </footer>

      {creating && (
        <div className="modal-backdrop">
          <section className="modal">
            <div className="debug-header">
              <h2>Create Simulation</h2>
              <button className="icon" onClick={() => setCreating(false)} title="Close">
                <X size={17} />
              </button>
            </div>
            <div className="modal-fields">
              <label className="field wide">
                <span>Name</span>
                <input value={createName} onChange={(event) => setCreateName(event.target.value)} />
              </label>
              <label className="field wide">
                <span>Scenario</span>
                <select value={selectedScenarioName} onChange={(event) => handleScenarioChange(event.target.value)}>
                  <option value="custom">Custom</option>
                  {scenarioPresets.map((scenario) => (
                    <option key={scenario.name} value={scenario.name}>{scenario.displayName}</option>
                  ))}
                </select>
              </label>
            </div>
            <ConfigFields config={createConfig} disabled={false} includeRebuildFields onChange={setCreateConfig} />
            <div className="row modal-actions">
              <button onClick={() => handleCreate().catch((reason: unknown) => setError(toMessage(reason)))}>
                <Plus size={16} />
                Create
              </button>
              <button onClick={() => handleCreate(true).catch((reason: unknown) => setError(toMessage(reason)))}>
                <Play size={16} />
                Create & Run
              </button>
              <button onClick={() => setCreating(false)}>Cancel</button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}

function SettingsPanel({
  config,
  canTune,
  onChange,
  onApply
}: {
  config: SimulationConfig | null;
  canTune: boolean;
  onChange: (config: SimulationConfig) => void;
  onApply: () => void;
}) {
  return (
    <section className="debug-panel settings-panel">
      <div className="debug-header">
        <h2>Settings</h2>
        <button onClick={onApply} disabled={!config || !canTune}>
          <Save size={16} />
          Apply
        </button>
      </div>
      {config ? (
        <ConfigFields config={config} disabled={!canTune} includeRebuildFields={false} onChange={onChange} />
      ) : (
        <span className="metric">No active simulation.</span>
      )}
    </section>
  );
}

function ConfigFields({
  config,
  disabled,
  includeRebuildFields,
  onChange
}: {
  config: SimulationConfig;
  disabled: boolean;
  includeRebuildFields: boolean;
  onChange: (config: SimulationConfig) => void;
}) {
  const lifecycle = config.lifecycle ?? defaultConfig.lifecycle!;
  const needRules = config.needRules ?? defaultConfig.needRules!;
  const goals = config.goals ?? defaultConfig.goals!;
  const pathfinding = {
    ...defaultConfig.pathfinding,
    ...config.pathfinding
  };
  const spawnResources = config.spawnResources ?? defaultConfig.spawnResources!;
  const speciesRules = config.speciesRules ?? defaultConfig.speciesRules!;
  const human = speciesRules.human ?? defaultConfig.speciesRules!.human!;
  const deer = speciesRules.deer ?? defaultConfig.speciesRules!.deer!;
  const wolf = speciesRules.wolf ?? defaultConfig.speciesRules!.wolf!;
  const believability = config.believability ?? defaultConfig.believability!;
  const behavior = believability.behavior ?? defaultConfig.believability!.behavior!;
  const social = believability.social ?? defaultConfig.believability!.social!;
  const reproduction = believability.reproduction ?? defaultConfig.believability!.reproduction!;
  const recovery = believability.recovery ?? defaultConfig.believability!.recovery!;
  const rebuildDisabled = disabled || !includeRebuildFields;
  const updateSpecies = (species: "human" | "deer" | "wolf", next: typeof human) => {
    onChange({ ...config, speciesRules: { ...speciesRules, [species]: next } });
  };
  const updateSpeciesNeeds = (species: "human" | "deer" | "wolf", current: typeof human, key: "hunger" | "thirst" | "energy" | "fatigue", value: number) => {
    const startingNeeds = current.startingNeeds ?? defaultConfig.speciesRules![species]!.startingNeeds!;
    updateSpecies(species, { ...current, startingNeeds: { ...startingNeeds, [key]: value } });
  };

  return (
    <div className="config-sections">
      <ConfigSection title="Simulation">
        <NumberConfigField label="Seed" value={config.seed} disabled={rebuildDisabled} onChange={(seed) => onChange({ ...config, seed })} />
        <NumberConfigField label="World width" value={config.worldWidth} disabled={rebuildDisabled} onChange={(worldWidth) => onChange({ ...config, worldWidth })} />
        <NumberConfigField label="World height" value={config.worldHeight} disabled={rebuildDisabled} onChange={(worldHeight) => onChange({ ...config, worldHeight })} />
        <NumberConfigField label="Tick interval ms" value={config.tickIntervalMilliseconds} disabled={disabled} onChange={(tickIntervalMilliseconds) => onChange({ ...config, tickIntervalMilliseconds })} />
        <NumberConfigField label="Event history" value={config.eventHistoryLimit} disabled={disabled} onChange={(eventHistoryLimit) => onChange({ ...config, eventHistoryLimit })} />
        <NumberConfigField label="Snapshot history" value={config.snapshotHistoryLimit} disabled={disabled} onChange={(snapshotHistoryLimit) => onChange({ ...config, snapshotHistoryLimit })} />
      </ConfigSection>

      <ConfigSection title="Time & Lifecycle">
        <NumberConfigField label="Day length ticks" value={config.environment.dayLengthTicks} disabled={disabled} onChange={(dayLengthTicks) => onChange({ ...config, environment: { ...config.environment, dayLengthTicks } })} />
        <NumberConfigField label="Age days / tick" value={lifecycle.ageDaysPerTick} disabled={disabled} step={0.001} onChange={(ageDaysPerTick) => onChange({ ...config, lifecycle: { ...lifecycle, ageDaysPerTick } })} />
        <NumberConfigField label="Days / year" value={lifecycle.daysPerYear} disabled={disabled} onChange={(daysPerYear) => onChange({ ...config, lifecycle: { ...lifecycle, daysPerYear } })} />
      </ConfigSection>

      <ConfigSection title="Species">
        <NumberConfigField label="Humans" value={config.speciesPopulation.human} disabled={rebuildDisabled} onChange={(humanCount) => onChange({ ...config, initialAgentCount: humanCount, speciesPopulation: { ...config.speciesPopulation, human: humanCount } })} />
        <NumberConfigField label="Deer" value={config.speciesPopulation.deer} disabled={rebuildDisabled} onChange={(deerCount) => onChange({ ...config, speciesPopulation: { ...config.speciesPopulation, deer: deerCount } })} />
        <NumberConfigField label="Wolves" value={config.speciesPopulation.wolf} disabled={rebuildDisabled} onChange={(wolfCount) => onChange({ ...config, speciesPopulation: { ...config.speciesPopulation, wolf: wolfCount } })} />
        <NumberConfigField label="Human health" value={human.maxHealth} disabled={disabled} step={0.5} onChange={(maxHealth) => updateSpecies("human", { ...human, maxHealth })} />
        <NumberConfigField label="Human adult days" value={human.adultAgeDays} disabled={disabled} onChange={(adultAgeDays) => updateSpecies("human", { ...human, adultAgeDays })} />
        <NumberConfigField label="Human max days" value={human.maxAgeDays} disabled={disabled} onChange={(maxAgeDays) => updateSpecies("human", { ...human, maxAgeDays })} />
        <NumberConfigField label="Deer speed x" value={deer.movementSpeedMultiplier} disabled={disabled} step={0.05} onChange={(movementSpeedMultiplier) => updateSpecies("deer", { ...deer, movementSpeedMultiplier })} />
        <NumberConfigField label="Deer hunger x" value={deer.hungerDecayMultiplier} disabled={disabled} step={0.05} onChange={(hungerDecayMultiplier) => updateSpecies("deer", { ...deer, hungerDecayMultiplier })} />
        <NumberConfigField label="Wolf damage" value={wolf.attackDamage} disabled={disabled} step={0.5} onChange={(attackDamage) => updateSpecies("wolf", { ...wolf, attackDamage })} />
        <NumberConfigField label="Wolf prey hunger" value={wolf.startingNeeds?.hunger ?? 0} disabled={disabled} step={0.1} onChange={(value) => updateSpeciesNeeds("wolf", wolf, "hunger", value)} />
      </ConfigSection>

      <ConfigSection title="Needs">
        <NumberConfigField label="Hunger decay" value={config.needDecay.hungerDelta} disabled={disabled} step={0.01} onChange={(hungerDelta) => onChange({ ...config, needDecay: { ...config.needDecay, hungerDelta } })} />
        <NumberConfigField label="Thirst decay" value={config.needDecay.thirstDelta} disabled={disabled} step={0.01} onChange={(thirstDelta) => onChange({ ...config, needDecay: { ...config.needDecay, thirstDelta } })} />
        <NumberConfigField label="Energy decay" value={config.needDecay.energyDelta} disabled={disabled} step={0.01} onChange={(energyDelta) => onChange({ ...config, needDecay: { ...config.needDecay, energyDelta } })} />
        <NumberConfigField label="Fatigue decay" value={config.needDecay.fatigueDelta} disabled={disabled} step={0.01} onChange={(fatigueDelta) => onChange({ ...config, needDecay: { ...config.needDecay, fatigueDelta } })} />
        <NumberConfigField label="Critical at" value={needRules.criticalNeedThreshold} disabled={disabled} step={0.1} onChange={(criticalNeedThreshold) => onChange({ ...config, needRules: { ...needRules, criticalNeedThreshold } })} />
        <NumberConfigField label="Damage at" value={needRules.harmNeedThreshold} disabled={disabled} step={0.1} onChange={(harmNeedThreshold) => onChange({ ...config, needRules: { ...needRules, harmNeedThreshold } })} />
        <NumberConfigField label="Damage / tick" value={needRules.survivalDamagePerTick} disabled={disabled} step={0.1} onChange={(survivalDamagePerTick) => onChange({ ...config, needRules: { ...needRules, survivalDamagePerTick } })} />
        <NumberConfigField label="Eat recovery" value={needRules.eatRecoveryAmount} disabled={disabled} step={0.1} onChange={(eatRecoveryAmount) => onChange({ ...config, needRules: { ...needRules, eatRecoveryAmount } })} />
        <NumberConfigField label="Drink recovery" value={needRules.drinkRecoveryAmount} disabled={disabled} step={0.1} onChange={(drinkRecoveryAmount) => onChange({ ...config, needRules: { ...needRules, drinkRecoveryAmount } })} />
        <NumberConfigField label="Rest energy" value={needRules.restEnergyRecoveryAmount} disabled={disabled} step={0.1} onChange={(restEnergyRecoveryAmount) => onChange({ ...config, needRules: { ...needRules, restEnergyRecoveryAmount } })} />
        <NumberConfigField label="Rest fatigue" value={needRules.restFatigueRecoveryAmount} disabled={disabled} step={0.1} onChange={(restFatigueRecoveryAmount) => onChange({ ...config, needRules: { ...needRules, restFatigueRecoveryAmount } })} />
      </ConfigSection>

      <ConfigSection title="Movement & Pathfinding">
        <NumberConfigField label="Perception radius" value={config.perceptionRadius} disabled={disabled} step={0.5} onChange={(perceptionRadius) => onChange({ ...config, perceptionRadius })} />
        <NumberConfigField label="Move / tick" value={config.movementSpeedPerTick} disabled={disabled} step={0.05} onChange={(movementSpeedPerTick) => onChange({ ...config, movementSpeedPerTick })} />
        <NumberConfigField label="Visited cells" value={pathfinding.maxVisitedCells} disabled={disabled} onChange={(maxVisitedCells) => onChange({ ...config, pathfinding: { ...pathfinding, maxVisitedCells } })} />
        <NumberConfigField label="Repaths" value={pathfinding.maxRepathAttempts} disabled={disabled} onChange={(maxRepathAttempts) => onChange({ ...config, pathfinding: { ...pathfinding, maxRepathAttempts } })} />
        <NumberConfigField label="Arrival tol" value={pathfinding.arrivalTolerance} disabled={disabled} step={0.01} onChange={(arrivalTolerance) => onChange({ ...config, pathfinding: { ...pathfinding, arrivalTolerance } })} />
        <NumberConfigField label="Interact tol" value={pathfinding.interactionTolerance} disabled={disabled} step={0.01} onChange={(interactionTolerance) => onChange({ ...config, pathfinding: { ...pathfinding, interactionTolerance } })} />
        <NumberConfigField label="Agent reach" value={pathfinding.agentInteractionRangeBonus} disabled={disabled} step={0.05} onChange={(agentInteractionRangeBonus) => onChange({ ...config, pathfinding: { ...pathfinding, agentInteractionRangeBonus } })} />
        <label className="field checkbox-field"><span>Diagonal</span><input type="checkbox" checked={pathfinding.allowDiagonalMovement} disabled={disabled} onChange={(event) => onChange({ ...config, pathfinding: { ...pathfinding, allowDiagonalMovement: event.target.checked } })} /></label>
      </ConfigSection>

      <ConfigSection title="Environment">
        <NumberConfigField label="Weather interval" value={config.environment.weatherChangeIntervalTicks} disabled={disabled} onChange={(weatherChangeIntervalTicks) => onChange({ ...config, environment: { ...config.environment, weatherChangeIntervalTicks } })} />
        <NumberConfigField label="Base temp" value={config.environment.baseTemperature} disabled={disabled} step={0.5} onChange={(baseTemperature) => onChange({ ...config, environment: { ...config.environment, baseTemperature } })} />
        <NumberConfigField label="Clear weight" value={config.environment.clearWeatherWeight} disabled={disabled} onChange={(clearWeatherWeight) => onChange({ ...config, environment: { ...config.environment, clearWeatherWeight } })} />
        <NumberConfigField label="Rain weight" value={config.environment.rainWeatherWeight} disabled={disabled} onChange={(rainWeatherWeight) => onChange({ ...config, environment: { ...config.environment, rainWeatherWeight } })} />
        <NumberConfigField label="Heat weight" value={config.environment.heatWeatherWeight} disabled={disabled} onChange={(heatWeatherWeight) => onChange({ ...config, environment: { ...config.environment, heatWeatherWeight } })} />
        <NumberConfigField label="Cold weight" value={config.environment.coldWeatherWeight} disabled={disabled} onChange={(coldWeatherWeight) => onChange({ ...config, environment: { ...config.environment, coldWeatherWeight } })} />
      </ConfigSection>

      <ConfigSection title="Ecology & Resources">
        <NumberConfigField label="Food containers" value={config.initialFoodCount} disabled={rebuildDisabled} onChange={(initialFoodCount) => onChange({ ...config, initialFoodCount })} />
        <NumberConfigField label="Food stack qty" value={spawnResources.foodStackQuantity} disabled={rebuildDisabled} onChange={(foodStackQuantity) => onChange({ ...config, spawnResources: { ...spawnResources, foodStackQuantity } })} />
        <NumberConfigField label="Plants" value={config.ecology.initialPlantCount} disabled={rebuildDisabled} onChange={(initialPlantCount) => onChange({ ...config, ecology: { ...config.ecology, initialPlantCount } })} />
        <NumberConfigField label="Plant start yield" value={spawnResources.plantStartingYield} disabled={rebuildDisabled} onChange={(plantStartingYield) => onChange({ ...config, spawnResources: { ...spawnResources, plantStartingYield } })} />
        <NumberConfigField label="Plant max yield" value={spawnResources.plantMaxYield} disabled={rebuildDisabled} onChange={(plantMaxYield) => onChange({ ...config, spawnResources: { ...spawnResources, plantMaxYield } })} />
        <NumberConfigField label="Regrowth ticks" value={config.ecology.plantRegrowthTicks} disabled={disabled} onChange={(plantRegrowthTicks) => onChange({ ...config, ecology: { ...config.ecology, plantRegrowthTicks } })} />
        <NumberConfigField label="Water sources" value={config.ecology.initialWaterSourceCount} disabled={rebuildDisabled} onChange={(initialWaterSourceCount) => onChange({ ...config, ecology: { ...config.ecology, initialWaterSourceCount } })} />
        <NumberConfigField label="Water start" value={spawnResources.waterStartingVolume} disabled={rebuildDisabled} step={0.5} onChange={(waterStartingVolume) => onChange({ ...config, spawnResources: { ...spawnResources, waterStartingVolume } })} />
        <NumberConfigField label="Water max" value={spawnResources.waterMaxVolume} disabled={rebuildDisabled} step={0.5} onChange={(waterMaxVolume) => onChange({ ...config, spawnResources: { ...spawnResources, waterMaxVolume } })} />
        <NumberConfigField label="Rain refill" value={config.ecology.waterRefillPerRainTick} disabled={disabled} step={0.05} onChange={(waterRefillPerRainTick) => onChange({ ...config, ecology: { ...config.ecology, waterRefillPerRainTick } })} />
        <NumberConfigField label="Heat evap" value={config.ecology.waterEvaporationPerHeatTick} disabled={disabled} step={0.05} onChange={(waterEvaporationPerHeatTick) => onChange({ ...config, ecology: { ...config.ecology, waterEvaporationPerHeatTick } })} />
        <NumberConfigField label="Deposits" value={config.ecology.initialResourceDepositCount} disabled={rebuildDisabled} onChange={(initialResourceDepositCount) => onChange({ ...config, ecology: { ...config.ecology, initialResourceDepositCount } })} />
      </ConfigSection>

      <ConfigSection title="Memory & Learning">
        <NumberConfigField label="Memory max" value={config.memory.maxMemoriesPerAgent} disabled={disabled} onChange={(maxMemoriesPerAgent) => onChange({ ...config, memory: { ...config.memory, maxMemoriesPerAgent } })} />
        <NumberConfigField label="Memory ttl" value={config.memory.retentionTicks} disabled={disabled} onChange={(retentionTicks) => onChange({ ...config, memory: { ...config.memory, retentionTicks } })} />
        <NumberConfigField label="Memory decay" value={config.memory.decayPerTick} disabled={disabled} step={0.01} onChange={(decayPerTick) => onChange({ ...config, memory: { ...config.memory, decayPerTick } })} />
        <NumberConfigField label="Forget below" value={config.memory.minimumStrength} disabled={disabled} step={0.05} onChange={(minimumStrength) => onChange({ ...config, memory: { ...config.memory, minimumStrength } })} />
        <NumberConfigField label="Recall at" value={config.memory.recallThreshold} disabled={disabled} step={0.05} onChange={(recallThreshold) => onChange({ ...config, memory: { ...config.memory, recallThreshold } })} />
        <NumberConfigField label="Trait spread" value={config.traits.initialVariation} disabled={rebuildDisabled} step={0.01} onChange={(initialVariation) => onChange({ ...config, traits: { ...config.traits, initialVariation } })} />
        <NumberConfigField label="Mutate rate" value={config.traits.mutationChancePerTrait} disabled={disabled} step={0.01} onChange={(mutationChancePerTrait) => onChange({ ...config, traits: { ...config.traits, mutationChancePerTrait } })} />
        <NumberConfigField label="Mutate delta" value={config.traits.mutationDelta} disabled={disabled} step={0.01} onChange={(mutationDelta) => onChange({ ...config, traits: { ...config.traits, mutationDelta } })} />
        <label className="field checkbox-field"><span>Memory</span><input type="checkbox" checked={config.memory.enabled} disabled={disabled} onChange={(event) => onChange({ ...config, memory: { ...config.memory, enabled: event.target.checked } })} /></label>
      </ConfigSection>

      <ConfigSection title="Behavior & Social">
        {(["eat", "drink", "rest", "wander", "social", "flee", "attack", "craft", "experiment"] as const).map((key) => (
          <NumberConfigField key={key} label={key} value={behavior[key]} disabled={disabled} step={0.05} onChange={(value) => onChange({ ...config, believability: { ...believability, behavior: { ...behavior, [key]: value } } })} />
        ))}
        <NumberConfigField label="Explore" value={behavior.explorationChance} disabled={disabled} step={0.01} onChange={(explorationChance) => onChange({ ...config, believability: { ...believability, behavior: { ...behavior, explorationChance } } })} />
        <NumberConfigField label="Personality" value={behavior.personalityInfluence} disabled={disabled} step={0.01} onChange={(personalityInfluence) => onChange({ ...config, believability: { ...believability, behavior: { ...behavior, personalityInfluence } } })} />
        <NumberConfigField label="Comm trust" value={social.communicationTrust} disabled={disabled} step={0.01} onChange={(communicationTrust) => onChange({ ...config, believability: { ...believability, social: { ...social, communicationTrust } } })} />
        <NumberConfigField label="Attack fear" value={social.attackFear} disabled={disabled} step={0.01} onChange={(attackFear) => onChange({ ...config, believability: { ...believability, social: { ...social, attackFear } } })} />
      </ConfigSection>

      <ConfigSection title="Reproduction">
        <NumberConfigField label="Behavior weight" value={behavior.reproduce} disabled={disabled} step={0.05} onChange={(reproduce) => onChange({ ...config, believability: { ...believability, behavior: { ...behavior, reproduce } } })} />
        <NumberConfigField label="Need threshold" value={reproduction.needThreshold} disabled={disabled} step={0.1} onChange={(needThreshold) => onChange({ ...config, believability: { ...believability, reproduction: { ...reproduction, needThreshold } } })} />
        <NumberConfigField label="Range" value={reproduction.range} disabled={disabled} step={0.05} onChange={(range) => onChange({ ...config, believability: { ...believability, reproduction: { ...reproduction, range } } })} />
        <NumberConfigField label="Cooldown scale" value={reproduction.cooldownScale} disabled={disabled} step={0.05} onChange={(cooldownScale) => onChange({ ...config, believability: { ...believability, reproduction: { ...reproduction, cooldownScale } } })} />
        <NumberConfigField label="Pressure" value={reproduction.populationPressureInfluence} disabled={disabled} step={0.05} onChange={(populationPressureInfluence) => onChange({ ...config, believability: { ...believability, reproduction: { ...reproduction, populationPressureInfluence } } })} />
        <NumberConfigField label="Parent hunger" value={reproduction.parentHungerCost} disabled={disabled} step={0.1} onChange={(parentHungerCost) => onChange({ ...config, believability: { ...believability, reproduction: { ...reproduction, parentHungerCost } } })} />
        <NumberConfigField label="Parent thirst" value={reproduction.parentThirstCost} disabled={disabled} step={0.1} onChange={(parentThirstCost) => onChange({ ...config, believability: { ...believability, reproduction: { ...reproduction, parentThirstCost } } })} />
        <NumberConfigField label="Parent fatigue" value={reproduction.parentFatigueCost} disabled={disabled} step={0.1} onChange={(parentFatigueCost) => onChange({ ...config, believability: { ...believability, reproduction: { ...reproduction, parentFatigueCost } } })} />
      </ConfigSection>

      <ConfigSection title="Recovery">
        <NumberConfigField label="Target cooldown" value={recovery.failedTargetCooldownTicks} disabled={disabled} onChange={(failedTargetCooldownTicks) => onChange({ ...config, believability: { ...believability, recovery: { ...recovery, failedTargetCooldownTicks } } })} />
        <NumberConfigField label="Max failures" value={recovery.maxRepeatedActionFailures} disabled={disabled} onChange={(maxRepeatedActionFailures) => onChange({ ...config, believability: { ...believability, recovery: { ...recovery, maxRepeatedActionFailures } } })} />
        <NumberConfigField label="Goal age" value={recovery.maxGoalAgeTicks} disabled={disabled} onChange={(maxGoalAgeTicks) => onChange({ ...config, believability: { ...believability, recovery: { ...recovery, maxGoalAgeTicks } } })} />
        <NumberConfigField label="Idle ticks" value={recovery.idleRecoveryTicks} disabled={disabled} onChange={(idleRecoveryTicks) => onChange({ ...config, believability: { ...believability, recovery: { ...recovery, idleRecoveryTicks } } })} />
        <NumberConfigField label="Stuck ticks" value={recovery.movementStuckTicks} disabled={disabled} onChange={(movementStuckTicks) => onChange({ ...config, believability: { ...believability, recovery: { ...recovery, movementStuckTicks } } })} />
      </ConfigSection>

      <ConfigSection title="Goals">
        <NumberConfigField label="Hunger goal" value={goals.hungerThreshold} disabled={disabled} step={0.1} onChange={(hungerThreshold) => onChange({ ...config, goals: { ...goals, hungerThreshold } })} />
        <NumberConfigField label="Urgent hunger" value={goals.urgentHungerThreshold} disabled={disabled} step={0.1} onChange={(urgentHungerThreshold) => onChange({ ...config, goals: { ...goals, urgentHungerThreshold } })} />
        <NumberConfigField label="Interrupt delta" value={goals.interruptionPriorityDelta} disabled={disabled} onChange={(interruptionPriorityDelta) => onChange({ ...config, goals: { ...goals, interruptionPriorityDelta } })} />
      </ConfigSection>

      <ConfigSection title="Systems">
        <div className="system-toggles wide">
        {systemOptions.map(([id, label]) => (
          <label key={id}>
            <input
              type="checkbox"
              checked={config.enabledSystems.includes(id)}
              disabled={disabled}
              onChange={(event) => {
                const enabledSystems = event.target.checked
                  ? [...config.enabledSystems, id]
                  : config.enabledSystems.filter((systemId) => systemId !== id);
                onChange({ ...config, enabledSystems });
              }}
            />
            <span>{label}</span>
          </label>
        ))}
        </div>
      </ConfigSection>
    </div>
  );
}

function ConfigSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="config-section">
      <h3>{title}</h3>
      <div className="field-grid config-grid">{children}</div>
    </section>
  );
}

function NumberConfigField({
  label,
  value,
  disabled,
  step,
  onChange
}: {
  label: string;
  value: number;
  disabled: boolean;
  step?: number;
  onChange: (value: number) => void;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      <input
        type="number"
        step={step}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(Number(event.target.value))}
      />
    </label>
  );
}

function toMessage(reason: unknown) {
  return reason instanceof Error ? reason.message : "Request failed";
}

function formatBytes(value: number) {
  if (value < 1024) {
    return `${value} B`;
  }

  if (value < 1024 * 1024) {
    return `${(value / 1024).toFixed(1)} KB`;
  }

  return `${(value / (1024 * 1024)).toFixed(1)} MB`;
}
