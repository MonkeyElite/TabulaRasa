"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Pause, Play, RotateCcw, Save, SkipBack, SkipForward, StepForward } from "lucide-react";
import { Inspector } from "@/components/Inspector";
import { WorldCanvas } from "@/components/WorldCanvas";
import { simulationApi } from "@/lib/api";
import { toggleBlockedCell, updateAgentDraft, updateFoodDraft } from "@/lib/draft";
import type {
  GridCell,
  HoverInfo,
  Selection,
  SimulationDraft,
  SimulationDraftSchema,
  SimulationSnapshot,
  SimulationStatus
} from "@/types/simulation";

export default function Home() {
  const [status, setStatus] = useState<SimulationStatus | null>(null);
  const [snapshot, setSnapshot] = useState<SimulationSnapshot | null>(null);
  const [draft, setDraft] = useState<SimulationDraft | null>(null);
  const [schema, setSchema] = useState<SimulationDraftSchema | null>(null);
  const [selection, setSelection] = useState<Selection>(null);
  const [viewedTick, setViewedTick] = useState(0);
  const [sliderTick, setSliderTick] = useState(0);
  const [speed, setSpeed] = useState(500);
  const [editing, setEditing] = useState(false);
  const [hover, setHover] = useState<HoverInfo>(null);
  const [error, setError] = useState<string | null>(null);
  const tickRequestIdRef = useRef(0);

  const canEdit = Boolean(status && snapshot && snapshot.tick === status.currentTick);
  const isRunning = status?.status === "Running";

  const loadStatus = useCallback(async () => {
    const nextStatus = await simulationApi.status();
    setStatus(nextStatus);
    return nextStatus;
  }, []);

  const loadCurrent = useCallback(async () => {
    const [nextStatus, current] = await Promise.all([simulationApi.status(), simulationApi.current()]);
    setStatus(nextStatus);
    setSnapshot(current);
    setViewedTick(current.tick);
    setSliderTick(current.tick);
  }, []);

  useEffect(() => {
    loadCurrent().catch((reason: unknown) => setError(toMessage(reason)));
  }, [loadCurrent]);

  useEffect(() => {
    if (!isRunning) {
      return;
    }

    const interval = window.setInterval(() => {
      loadCurrent().catch((reason: unknown) => setError(toMessage(reason)));
    }, Math.max(150, Math.min(speed, 1500)));

    return () => window.clearInterval(interval);
  }, [isRunning, loadCurrent, speed]);

  useEffect(() => {
    if (!editing) {
      return;
    }

    Promise.all([simulationApi.draft(), simulationApi.draftSchema()])
      .then(([nextDraft, nextSchema]) => {
        setDraft(nextDraft);
        setSchema(nextSchema);
      })
      .catch((reason: unknown) => setError(toMessage(reason)));
  }, [editing]);

  const selectedTickLabel = useMemo(() => {
    if (!status) {
      return "-";
    }

    return `${viewedTick} / ${status.maximumTick}`;
  }, [status, viewedTick]);

  async function handleStep() {
    setError(null);
    const next = await simulationApi.step();
    const nextStatus = await loadStatus();
    setStatus(nextStatus);
    setSnapshot(next);
    setViewedTick(next.tick);
    setSliderTick(next.tick);
  }

  async function handleRunPause() {
    setError(null);
    const nextStatus = isRunning ? await simulationApi.pause() : await simulationApi.run(speed);
    setStatus(nextStatus);
  }

  async function handleReset() {
    setError(null);
    const next = await simulationApi.reset();
    const nextStatus = await loadStatus();
    setDraft(null);
    setEditing(false);
    setSelection(null);
    setStatus(nextStatus);
    setSnapshot(next);
    setViewedTick(next.tick);
  }

  async function handleRestartFromDraft() {
    if (!draft) {
      return;
    }

    setError(null);
    const next = await simulationApi.restartFromDraft(draft);
    const nextStatus = await loadStatus();
    setStatus(nextStatus);
    setSnapshot(next);
    setViewedTick(next.tick);
    setSliderTick(next.tick);
    setEditing(false);
  }

  async function loadTick(tick: number) {
    if (!status || tick < status.minimumTick || tick > status.maximumTick) {
      return;
    }

    const requestId = ++tickRequestIdRef.current;
    setError(null);
    const next = await simulationApi.tick(tick);
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
        <button className="icon" onClick={handleRunPause} title={isRunning ? "Pause" : "Run"}>
          {isRunning ? <Pause size={18} /> : <Play size={18} />}
        </button>
        <button className="icon" onClick={handleStep} title="Step" disabled={isRunning}>
          <StepForward size={18} />
        </button>
        <button className="icon" onClick={handleReset} title="Reset">
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
    </main>
  );
}

function toMessage(reason: unknown) {
  return reason instanceof Error ? reason.message : "Request failed";
}
