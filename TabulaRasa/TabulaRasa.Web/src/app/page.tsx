"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Copy,
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
  X
} from "lucide-react";
import { EventLogPanel, RuntimePanel } from "@/components/DebugPanels";
import { Inspector } from "@/components/Inspector";
import { WorldCanvas } from "@/components/WorldCanvas";
import { simulationApi } from "@/lib/api";
import { toggleBlockedCell, updateAgentDraft, updateFoodDraft } from "@/lib/draft";
import type {
  GridCell,
  HoverInfo,
  Selection,
  SimulationConfig,
  SimulationDraft,
  SimulationDraftSchema,
  SimulationResourceLimits,
  SimulationSnapshot,
  SimulationStatus,
  SimulationSummary
} from "@/types/simulation";

const systemOptions = [
  ["need-decay", "Need decay"],
  ["planning", "Planning"],
  ["action-request-creation", "Actions"],
  ["route-planning", "Routes"],
  ["job-activation", "Jobs"],
  ["task-assignment", "Assignments"],
  ["movement-execution", "Movement"],
  ["task-execution", "Tasks"],
  ["action-execution", "Execution"],
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
    hungerDelta: 1,
    thirstDelta: 1,
    energyDelta: -1
  },
  perceptionRadius: 20,
  movementSpeedPerTick: 0.25,
  pathfinding: {
    allowDiagonalMovement: false,
    maxVisitedCells: 1000
  },
  enabledSystems: systemOptions.map(([id]) => id)
};

export default function Home() {
  const [simulations, setSimulations] = useState<SimulationSummary[]>([]);
  const [activeSimulationId, setActiveSimulationId] = useState<string | null>(null);
  const [limits, setLimits] = useState<SimulationResourceLimits | null>(null);
  const [status, setStatus] = useState<SimulationStatus | null>(null);
  const [snapshot, setSnapshot] = useState<SimulationSnapshot | null>(null);
  const [draft, setDraft] = useState<SimulationDraft | null>(null);
  const [schema, setSchema] = useState<SimulationDraftSchema | null>(null);
  const [selection, setSelection] = useState<Selection>(null);
  const [viewedTick, setViewedTick] = useState(0);
  const [sliderTick, setSliderTick] = useState(0);
  const [speed, setSpeed] = useState(500);
  const [configDraft, setConfigDraft] = useState<SimulationConfig | null>(null);
  const [eventScope, setEventScope] = useState<"recent" | "current">("recent");
  const [eventType, setEventType] = useState("all");
  const [editing, setEditing] = useState(false);
  const [hover, setHover] = useState<HoverInfo>(null);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [createName, setCreateName] = useState("Simulation");
  const [createConfig, setCreateConfig] = useState<SimulationConfig>(defaultConfig);
  const tickRequestIdRef = useRef(0);

  const activeSummary = simulations.find((simulation) => simulation.simulationId === activeSimulationId) ?? null;
  const canEdit = Boolean(status && snapshot && snapshot.tick === status.currentTick);
  const isRunning = status?.status === "Running";
  const isStopped = status?.status === "Stopped";
  const canTune = status?.status === "Paused";

  const loadSimulations = useCallback(async () => {
    const [nextSimulations, nextLimits] = await Promise.all([
      simulationApi.list(),
      simulationApi.resourceLimits()
    ]);
    setSimulations(nextSimulations);
    setLimits(nextLimits);
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

    const [nextStatus, current] = await Promise.all([
      simulationApi.status(simulationId),
      simulationApi.current(simulationId)
    ]);
    setStatus(nextStatus);
    setConfigDraft(nextStatus.config);
    setSnapshot(current);
    setViewedTick(current.tick);
    setSliderTick(current.tick);
    await loadSimulations();
  }, [activeSimulationId, loadSimulations]);

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

  async function handleCreate() {
    setError(null);
    const created = await simulationApi.create({ name: createName, config: createConfig });
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

  function moveFood(id: string, cell: GridCell) {
    if (!draft) {
      return;
    }

    setDraft(updateFoodDraft(draft, id, { position: { x: cell.x + 0.5, y: cell.y + 0.5 } }));
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
        <select value={speed} onChange={(event) => setSpeed(Number(event.target.value))} title="Tick speed">
          <option value={1000}>1.0s</option>
          <option value={500}>0.5s</option>
          <option value={250}>0.25s</option>
          <option value={100}>0.1s</option>
        </select>
        <button onClick={() => setEditing((value) => !value)} disabled={!canEdit}>
          Edit
        </button>
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
            {simulations.map((simulation) => (
              <button
                key={simulation.simulationId}
                className={`simulation-row ${simulation.simulationId === activeSimulationId ? "selected" : ""}`}
                onClick={() => setActiveSimulationId(simulation.simulationId)}
              >
                <span>
                  <strong>{simulation.name}</strong>
                  <small>{simulation.status} / tick {simulation.currentTick}</small>
                </span>
                <span className="pill">{simulation.agentCount}a</span>
              </button>
            ))}
          </div>
          <div className="sidebar-actions">
            <button onClick={handleClone} disabled={!activeSimulationId}>
              <Copy size={16} />
              Clone
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
            onSelect={setSelection}
            onMoveAgent={moveAgent}
            onMoveFood={moveFood}
            onToggleBlockedCell={(cell) => draft && setDraft(toggleBlockedCell(draft, cell))}
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
          <Inspector
            snapshot={snapshot}
            draft={draft}
            schema={schema}
            selection={selection}
            onSelect={setSelection}
            editing={editing}
            canEdit={canEdit}
            onDraftChange={setDraft}
          />
          <RuntimePanel
            status={status}
            snapshot={snapshot}
            configDraft={null}
            onConfigDraftChange={() => undefined}
          />
          <SettingsPanel
            config={configDraft}
            canTune={canTune}
            onChange={setConfigDraft}
            onApply={handleApplyConfig}
          />
          <EventLogPanel
            events={visibleEvents}
            eventTypes={eventTypes}
            eventScope={eventScope}
            eventType={eventType}
            onEventScopeChange={setEventScope}
            onEventTypeChange={setEventType}
          />
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
            <label className="field wide">
              <span>Name</span>
              <input value={createName} onChange={(event) => setCreateName(event.target.value)} />
            </label>
            <ConfigFields config={createConfig} disabled={false} includeRebuildFields onChange={setCreateConfig} />
            <div className="row modal-actions">
              <button onClick={handleCreate}>
                <Plus size={16} />
                Create
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
  return (
    <div className="field-grid config-grid">
      <NumberConfigField
        label="Seed"
        value={config.seed}
        disabled={disabled || !includeRebuildFields}
        onChange={(seed) => onChange({ ...config, seed })}
      />
      <NumberConfigField
        label="Width"
        value={config.worldWidth}
        disabled={disabled || !includeRebuildFields}
        onChange={(worldWidth) => onChange({ ...config, worldWidth })}
      />
      <NumberConfigField
        label="Height"
        value={config.worldHeight}
        disabled={disabled || !includeRebuildFields}
        onChange={(worldHeight) => onChange({ ...config, worldHeight })}
      />
      <NumberConfigField
        label="Agents"
        value={config.initialAgentCount}
        disabled={disabled || !includeRebuildFields}
        onChange={(initialAgentCount) => onChange({ ...config, initialAgentCount })}
      />
      <NumberConfigField
        label="Food"
        value={config.initialFoodCount}
        disabled={disabled || !includeRebuildFields}
        onChange={(initialFoodCount) => onChange({ ...config, initialFoodCount })}
      />
      <NumberConfigField
        label="Interval"
        value={config.tickIntervalMilliseconds}
        disabled={disabled}
        onChange={(tickIntervalMilliseconds) => onChange({ ...config, tickIntervalMilliseconds })}
      />
      <NumberConfigField
        label="Events"
        value={config.eventHistoryLimit}
        disabled={disabled}
        onChange={(eventHistoryLimit) => onChange({ ...config, eventHistoryLimit })}
      />
      <NumberConfigField
        label="Snapshots"
        value={config.snapshotHistoryLimit}
        disabled={disabled}
        onChange={(snapshotHistoryLimit) => onChange({ ...config, snapshotHistoryLimit })}
      />
      <NumberConfigField
        label="Hunger"
        value={config.needDecay.hungerDelta}
        disabled={disabled}
        onChange={(hungerDelta) => onChange({ ...config, needDecay: { ...config.needDecay, hungerDelta } })}
      />
      <NumberConfigField
        label="Thirst"
        value={config.needDecay.thirstDelta}
        disabled={disabled}
        onChange={(thirstDelta) => onChange({ ...config, needDecay: { ...config.needDecay, thirstDelta } })}
      />
      <NumberConfigField
        label="Energy"
        value={config.needDecay.energyDelta}
        disabled={disabled}
        onChange={(energyDelta) => onChange({ ...config, needDecay: { ...config.needDecay, energyDelta } })}
      />
      <NumberConfigField
        label="Radius"
        value={config.perceptionRadius}
        disabled={disabled}
        onChange={(perceptionRadius) => onChange({ ...config, perceptionRadius })}
      />
      <NumberConfigField
        label="Move"
        value={config.movementSpeedPerTick}
        disabled={disabled}
        step={0.05}
        onChange={(movementSpeedPerTick) => onChange({ ...config, movementSpeedPerTick })}
      />
      <NumberConfigField
        label="Visited"
        value={config.pathfinding.maxVisitedCells}
        disabled={disabled}
        onChange={(maxVisitedCells) => onChange({ ...config, pathfinding: { ...config.pathfinding, maxVisitedCells } })}
      />
      <label className="field checkbox-field">
        <span>Diagonal</span>
        <input
          type="checkbox"
          checked={config.pathfinding.allowDiagonalMovement}
          disabled={disabled}
          onChange={(event) => onChange({
            ...config,
            pathfinding: { ...config.pathfinding, allowDiagonalMovement: event.target.checked }
          })}
        />
      </label>
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
    </div>
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
