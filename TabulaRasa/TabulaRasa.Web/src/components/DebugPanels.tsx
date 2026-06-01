import React from "react";
import type { SimulationEvent, SimulationSnapshot, SimulationStatus } from "@/types/simulation";

export function RuntimePanel({
  status,
  snapshot,
  configDraft,
  onConfigDraftChange
}: {
  status: SimulationStatus | null;
  snapshot: SimulationSnapshot | null;
  configDraft: SimulationStatus["config"] | null;
  onConfigDraftChange: (config: SimulationStatus["config"]) => void;
}) {
  const systems = snapshot?.diagnostics?.systems ?? [];
  const slowestSystems = [...systems]
    .sort((left, right) => right.durationMilliseconds - left.durationMilliseconds)
    .slice(0, 3);

  return (
    <section className="debug-panel">
      <div className="debug-header">
        <h2>Runtime</h2>
        <span className="pill">{status?.status ?? "-"}</span>
      </div>
      <div className="debug-summary">
        <span className="metric">
          Tick <strong>{status?.currentTick ?? "-"}</strong>
        </span>
        <span className="metric">
          Duration <strong>{formatMilliseconds(snapshot?.diagnostics?.durationMilliseconds)}</strong>
        </span>
        <span className="metric">
          Events <strong>{snapshot?.diagnostics?.eventCount ?? snapshot?.events.length ?? 0}</strong>
        </span>
        <span className="metric">
          Seed <strong>{status?.config.seed ?? "-"}</strong>
        </span>
      </div>
      {configDraft && (
        <div className="field-grid compact-fields">
          <NumberConfigField
            label="Seed"
            value={configDraft.seed}
            onChange={(seed) => onConfigDraftChange({ ...configDraft, seed })}
          />
          <NumberConfigField
            label="Events"
            value={configDraft.eventHistoryLimit}
            onChange={(eventHistoryLimit) => onConfigDraftChange({ ...configDraft, eventHistoryLimit })}
          />
          <NumberConfigField
            label="Interval"
            value={configDraft.tickIntervalMilliseconds}
            onChange={(tickIntervalMilliseconds) => onConfigDraftChange({ ...configDraft, tickIntervalMilliseconds })}
          />
        </div>
      )}
      <div className="system-list">
        {systems.map((system) => (
          <div key={`${system.phase}:${system.systemName}:${system.priority}`} className="system-row">
            <span>
              <strong>{system.systemName}</strong>
              <small>{system.phase} / priority {system.priority}</small>
            </span>
            <span>{formatMilliseconds(system.durationMilliseconds)}</span>
            <span>{system.emittedEventCount}</span>
          </div>
        ))}
        {systems.length === 0 && <span className="metric">No tick diagnostics yet.</span>}
      </div>
      {slowestSystems.length > 0 && (
        <div className="slow-list">
          {slowestSystems.map((system) => (
            <span key={system.systemName} className="pill">
              {system.systemName}: {formatMilliseconds(system.durationMilliseconds)}
            </span>
          ))}
        </div>
      )}
    </section>
  );
}

export function EventLogPanel({
  events,
  eventTypes,
  eventScope,
  eventType,
  onEventScopeChange,
  onEventTypeChange
}: {
  events: SimulationEvent[];
  eventTypes: string[];
  eventScope: "recent" | "current";
  eventType: string;
  onEventScopeChange: (scope: "recent" | "current") => void;
  onEventTypeChange: (type: string) => void;
}) {
  return (
    <section className="debug-panel event-panel">
      <div className="debug-header">
        <h2>Events</h2>
        <div className="row compact-controls">
          <select value={eventScope} onChange={(event) => onEventScopeChange(event.target.value as "recent" | "current")}>
            <option value="recent">Recent</option>
            <option value="current">Current</option>
          </select>
          <select value={eventType} onChange={(event) => onEventTypeChange(event.target.value)}>
            <option value="all">All</option>
            {eventTypes.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </div>
      </div>
      <div className="event-list">
        {events.map((event) => (
          <div key={`${event.tick}:${event.sequence}:${event.type}`} className="event-row">
            <span>{event.tick}</span>
            <span>{event.type}</span>
            <span>{event.sourceSystem}</span>
            <span>{event.message}</span>
          </div>
        ))}
        {events.length === 0 && <span className="metric">No events match the current filter.</span>}
      </div>
    </section>
  );
}

function NumberConfigField({
  label,
  value,
  onChange
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      <input
        type="number"
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
      />
    </label>
  );
}

function formatMilliseconds(value: number | undefined) {
  if (typeof value !== "number") {
    return "-";
  }

  return `${value.toFixed(value < 10 ? 2 : 1)}ms`;
}
