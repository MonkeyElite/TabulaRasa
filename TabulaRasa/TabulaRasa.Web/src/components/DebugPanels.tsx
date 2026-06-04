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
  const jobs = snapshot?.jobs ?? [];
  const taskCounts = jobs.reduce(
    (counts, job) => ({
      total: counts.total + job.taskCount,
      pending: counts.pending + job.pendingTaskCount,
      active: counts.active + job.assignedTaskCount + job.inProgressTaskCount,
      completed: counts.completed + job.completedTaskCount,
      failed: counts.failed + job.failedTaskCount + job.cancelledTaskCount + job.interruptedTaskCount
    }),
    { total: 0, pending: 0, active: 0, completed: 0, failed: 0 }
  );

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
          Goals <strong>{snapshot?.goals.filter((goal) => goal.status === "Active").length ?? 0}/{snapshot?.goals.length ?? 0}</strong>
        </span>
        <span className="metric">
          Jobs <strong>{jobs.filter((job) => job.status === "Active").length}/{jobs.length}</strong>
        </span>
        <span className="metric">
          Tasks <strong>{taskCounts.active}/{taskCounts.total}</strong>
        </span>
        <span className="metric">
          Alive <strong>{snapshot?.aliveAgentCount ?? status?.aliveAgentCount ?? 0}</strong>
        </span>
        <span className="metric">
          Dead <strong>{snapshot?.deadAgentCount ?? status?.deadAgentCount ?? 0}</strong>
        </span>
        <span className="metric">
          Time <strong>{snapshot?.environment ? `${snapshot.environment.phase} ${snapshot.environment.tickOfDay}/${snapshot.environment.dayLengthTicks}` : "-"}</strong>
        </span>
        <span className="metric">
          Weather <strong>{snapshot?.environment ? `${snapshot.environment.weather} ${formatNumber(snapshot.environment.temperature)}C` : "-"}</strong>
        </span>
        <span className="metric">
          Plants <strong>{snapshot?.ecologyStats ? `${snapshot.ecologyStats.harvestablePlantCount}/${snapshot.ecologyStats.plantCount}` : "-"}</strong>
        </span>
        <span className="metric">
          Water <strong>{snapshot?.ecologyStats ? formatNumber(snapshot.ecologyStats.totalWaterVolume) : "-"}</strong>
        </span>
        <span className="metric">
          Seed <strong>{status?.config.seed ?? "-"}</strong>
        </span>
      </div>
      {snapshot && snapshot.speciesPopulation.length > 0 && (
        <div className="species-population-chart">
          {snapshot.speciesPopulation.map((species) => {
            const maxTotal = Math.max(1, ...snapshot.speciesPopulation.map((item) => item.total));
            const width = (species.total / maxTotal) * 100;

            return (
              <div className="species-population-row" key={species.speciesId}>
                <span>{species.displayName}</span>
                <div>
                  <i style={{ width: `${width}%` }} />
                </div>
                <strong>{species.alive}/{species.total}</strong>
              </div>
            );
          })}
        </div>
      )}
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
        {jobs.length > 0 && (
          <div className="system-row">
            <span>
              <strong>Task statuses</strong>
              <small>
                pending {taskCounts.pending} / done {taskCounts.completed} / failed {taskCounts.failed}
              </small>
            </span>
            <span>{taskCounts.active}</span>
            <span>{jobs.length}</span>
          </div>
        )}
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

export function SocialGraphPanel({
  snapshot,
  selectedAgentId,
  onSelectAgent
}: {
  snapshot: SimulationSnapshot | null;
  selectedAgentId: string | null;
  onSelectAgent: (agentId: string) => void;
}) {
  const graph = snapshot?.socialGraph;
  const width = 360;
  const height = 300;
  const nodes = graph?.nodes ?? [];
  const edges = graph?.edges ?? [];
  const nodePositions = new Map(nodes.map((node, index) => {
    const angle = nodes.length <= 1 ? 0 : (Math.PI * 2 * index) / nodes.length - Math.PI / 2;
    const radius = nodes.length <= 2 ? 82 : 112;
    return [
      node.agentId,
      {
        x: width / 2 + Math.cos(angle) * radius,
        y: height / 2 + Math.sin(angle) * radius
      }
    ];
  }));

  return (
    <section className="debug-panel social-panel">
      <div className="debug-header">
        <h2>Social</h2>
        <span className="pill">{edges.length} ties</span>
      </div>
      {!graph || nodes.length === 0 ? (
        <span className="metric">No social graph.</span>
      ) : (
        <>
          <svg className="social-graph" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Social graph">
            {edges.map((edge) => {
              const from = nodePositions.get(edge.fromAgentId);
              const to = nodePositions.get(edge.toAgentId);
              if (!from || !to) {
                return null;
              }

              const midX = (from.x + to.x) / 2;
              const midY = (from.y + to.y) / 2;
              return (
                <g key={`${edge.fromAgentId}:${edge.toAgentId}`}>
                  <line
                    x1={from.x}
                    y1={from.y}
                    x2={to.x}
                    y2={to.y}
                    stroke={edge.fear > edge.trust ? "#e06767" : "#6aa8ff"}
                    strokeWidth={1 + edge.familiarity * 5}
                    strokeOpacity={0.35 + edge.familiarity * 0.45}
                  />
                  <text x={midX} y={midY} className="social-edge-label">
                    {compactMetric(edge.trust)} / {compactMetric(edge.fear)} / {compactMetric(edge.affinity)}
                  </text>
                </g>
              );
            })}
            {nodes.map((node) => {
              const position = nodePositions.get(node.agentId);
              if (!position) {
                return null;
              }

              const selected = selectedAgentId === node.agentId;
              return (
                <g
                  key={node.agentId}
                  className="social-node"
                  transform={`translate(${position.x} ${position.y})`}
                  onClick={() => onSelectAgent(node.agentId)}
                >
                  <circle
                    r={selected ? 20 : 16}
                    fill={node.isDead ? "#5a5f68" : speciesColor(node.speciesId)}
                    stroke={selected ? "#ffffff" : "#20262b"}
                    strokeWidth={selected ? 4 : 2}
                  />
                  <text y={34}>{node.agentId}</text>
                </g>
              );
            })}
          </svg>
          <div className="system-list">
            {edges.slice(0, 8).map((edge) => (
              <div className="system-row" key={`${edge.fromAgentId}:${edge.toAgentId}`}>
                <span>
                  <strong>{`${edge.fromAgentId} -> ${edge.toAgentId}`}</strong>
                  <small>seen {compactMetric(edge.familiarity)} / trust {compactMetric(edge.trust)} / fear {compactMetric(edge.fear)}</small>
                </span>
                <span>{edge.interactionCount}</span>
                <span>{edge.lastInteractionTick ?? "-"}</span>
              </div>
            ))}
          </div>
        </>
      )}
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

function formatNumber(value: number) {
  return Number.isInteger(value) ? value.toString() : value.toFixed(1);
}

function compactMetric(value: number) {
  return value.toFixed(2);
}

function speciesColor(speciesId: string) {
  if (speciesId === "deer") {
    return "#d2a45f";
  }

  if (speciesId === "wolf") {
    return "#d76b6b";
  }

  return "#54c475";
}
