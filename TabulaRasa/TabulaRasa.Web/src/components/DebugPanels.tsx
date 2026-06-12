import React from "react";
import type { AgentSnapshot, DiscoveryMarkerSnapshot, PopulationTraitMetric, RecipeDefinitionSnapshot, Selection, SimulationEvent, SimulationSnapshot, SimulationStatus, SimulationSummary, SimulationTimelinePoint } from "@/types/simulation";

export function RuntimePanel({
  status,
  snapshot,
  timeline = [],
  comparisonTimeline = [],
  comparisonSimulationId = "",
  simulations = [],
  configDraft,
  onConfigDraftChange,
  onComparisonSimulationChange = () => undefined
}: {
  status: SimulationStatus | null;
  snapshot: SimulationSnapshot | null;
  timeline?: SimulationTimelinePoint[];
  comparisonTimeline?: SimulationTimelinePoint[];
  comparisonSimulationId?: string;
  simulations?: SimulationSummary[];
  configDraft: SimulationStatus["config"] | null;
  onConfigDraftChange: (config: SimulationStatus["config"]) => void;
  onComparisonSimulationChange?: (simulationId: string) => void;
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
      {snapshot && snapshot.evolution.currentTraits.length > 0 && (
        <div className="trait-population-chart">
          <div className="subsection-title">Selection pressure</div>
          {snapshot.evolution.currentTraits.map((metric) => (
            <TraitMetricRow key={metric.trait} metric={metric} />
          ))}
        </div>
      )}
      {timeline.length > 0 && (
        <div className="timeline-chart-panel">
          <div className="subsection-title">Run timeline</div>
          <MetricSparkline label="Alive" points={timeline} value={(point) => aliveTotal(point)} compare={comparisonTimeline} />
          <MetricSparkline label="Food" points={timeline} value={(point) => point.foodCount + point.totalPlantYield} compare={comparisonTimeline} />
          <MetricSparkline label="Water" points={timeline} value={(point) => point.totalWaterVolume} compare={comparisonTimeline} />
          <MetricSparkline label="Needs" points={timeline} value={(point) => point.averageHunger + point.averageThirst + point.averageFatigue} compare={comparisonTimeline} />
          <MetricSparkline label="Events" points={timeline} value={(point) => point.importantEventCount} compare={comparisonTimeline} />
          <MetricSparkline label="Tick ms" points={timeline} value={(point) => point.durationMilliseconds} compare={comparisonTimeline} />
          <div className="row compact-controls">
            <select value={comparisonSimulationId} onChange={(event) => onComparisonSimulationChange(event.target.value)}>
              <option value="">Compare none</option>
              {simulations.map((simulation) => (
                <option key={simulation.simulationId} value={simulation.simulationId}>
                  {simulation.name}
                </option>
              ))}
            </select>
          </div>
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
          <div key={`${event.tick}:${event.sequence}:${event.type}`} className={`event-row ${eventTone(event)}`}>
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

export function GenealogyPanel({
  snapshot,
  selectedAgentId,
  onSelectAgent
}: {
  snapshot: SimulationSnapshot | null;
  selectedAgentId: string | null;
  onSelectAgent: (agentId: string) => void;
}) {
  const agents = snapshot?.agents ?? [];
  const selectedAgent = selectedAgentId
    ? agents.find((agent) => agent.id === selectedAgentId) ?? null
    : agents[0] ?? null;
  const parents = selectedAgent ? selectedAgent.parentIds
    .map((id) => agents.find((agent) => agent.id === id))
    .filter((agent): agent is AgentSnapshot => Boolean(agent)) : [];
  const offspring = selectedAgent ? selectedAgent.offspringIds
    .map((id) => agents.find((agent) => agent.id === id))
    .filter((agent): agent is AgentSnapshot => Boolean(agent)) : [];
  const width = 360;
  const height = 300;
  const selectedPosition = { x: width / 2, y: height / 2 };
  const parentPositions = rowPositions(parents.length, width, 64);
  const offspringPositions = rowPositions(offspring.length, width, 236);

  return (
    <section className="debug-panel genealogy-panel">
      <div className="debug-header">
        <h2>Family</h2>
        <span className="pill">{selectedAgent?.id ?? "no agent"}</span>
      </div>
      {!selectedAgent ? (
        <span className="metric">No genealogy.</span>
      ) : (
        <>
          <svg className="social-graph genealogy-graph" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Family tree">
            {parents.map((parent, index) => (
              <line
                key={`parent-line:${parent.id}`}
                x1={parentPositions[index].x}
                y1={parentPositions[index].y + 18}
                x2={selectedPosition.x}
                y2={selectedPosition.y - 18}
                stroke="#6aa8ff"
                strokeOpacity="0.55"
              />
            ))}
            {offspring.map((child, index) => (
              <line
                key={`child-line:${child.id}`}
                x1={selectedPosition.x}
                y1={selectedPosition.y + 18}
                x2={offspringPositions[index].x}
                y2={offspringPositions[index].y - 18}
                stroke="#54c475"
                strokeOpacity="0.55"
              />
            ))}
            {parents.map((parent, index) => (
              <GenealogyNode key={parent.id} agent={parent} position={parentPositions[index]} selected={false} onSelectAgent={onSelectAgent} />
            ))}
            <GenealogyNode agent={selectedAgent} position={selectedPosition} selected onSelectAgent={onSelectAgent} />
            {offspring.map((child, index) => (
              <GenealogyNode key={child.id} agent={child} position={offspringPositions[index]} selected={false} onSelectAgent={onSelectAgent} />
            ))}
          </svg>
          <div className="system-list">
            <div className="system-row">
              <span>
                <strong>Parents</strong>
                <small>{selectedAgent.parentIds.length > 0 ? selectedAgent.parentIds.join(", ") : "none"}</small>
              </span>
              <span>{parents.length}</span>
              <span>{selectedAgent.parentIds.length}</span>
            </div>
            <div className="system-row">
              <span>
                <strong>Offspring</strong>
                <small>{selectedAgent.offspringIds.length > 0 ? selectedAgent.offspringIds.join(", ") : "none"}</small>
              </span>
              <span>{offspring.length}</span>
              <span>{selectedAgent.offspringIds.length}</span>
            </div>
            <TraitSummary agent={selectedAgent} />
          </div>
        </>
      )}
    </section>
  );
}

export function WatchPanel({
  snapshot,
  timeline,
  selection,
  onSelectAgent
}: {
  snapshot: SimulationSnapshot | null;
  timeline: SimulationTimelinePoint[];
  selection: Selection;
  onSelectAgent: (agentId: string) => void;
}) {
  const selectedAgent = selection?.type === "agent"
    ? snapshot?.agents.find((agent) => agent.id === selection.id) ?? null
    : null;
  const selectedSpecies = selectedAgent?.speciesId ?? snapshot?.speciesPopulation.find((species) => species.alive > 0)?.speciesId ?? "human";
  const species = snapshot?.speciesPopulation.find((item) => item.speciesId === selectedSpecies) ?? null;
  const importantEvents = (snapshot?.recentEvents ?? [])
    .filter((event) => (event.importance ?? 0) >= 0.5 || event.severity === "warning" || event.severity === "critical")
    .slice(-8)
    .reverse();

  return (
    <section className="debug-panel watch-panel">
      <div className="debug-header">
        <h2>Watch</h2>
        <span className="pill">{selectedAgent?.id ?? species?.displayName ?? "no target"}</span>
      </div>
      <div className="entity-list compact-watch-list">
        {(snapshot?.agents ?? []).slice(0, 12).map((agent) => (
          <button
            key={agent.id}
            className={`entity-row ${selectedAgent?.id === agent.id ? "selected" : ""}`}
            onClick={() => onSelectAgent(agent.id)}
          >
            <span className={`entity-dot ${agent.isDead ? "corpse" : "agent"}`} />
            <span>
              <strong>{agent.id}</strong>
              <small>{agent.speciesId} / {agent.personality?.label ?? "Balanced"}</small>
            </span>
          </button>
        ))}
      </div>
      {selectedAgent ? (
        <>
          <div className="debug-summary">
            <span className="metric">Personality <strong>{selectedAgent.personality?.label ?? "Balanced"}</strong></span>
            <span className="metric">Goal <strong>{selectedAgent.currentGoal?.needKey ?? "none"}</strong></span>
            <span className="metric">Action <strong>{selectedAgent.decision?.selectedAction ?? selectedAgent.movement?.requestedAction ?? "idle"}</strong></span>
            <span className="metric">Health <strong>{formatNumber(selectedAgent.health?.current ?? 0)}</strong></span>
          </div>
          <div className="watch-vitals">
            <NeedMiniBar label="H" value={selectedAgent.needs.hunger} invert />
            <NeedMiniBar label="T" value={selectedAgent.needs.thirst} invert />
            <NeedMiniBar label="E" value={selectedAgent.needs.energy} />
            <NeedMiniBar label="F" value={selectedAgent.needs.fatigue} invert />
          </div>
        </>
      ) : (
        <span className="metric">Select an agent to watch individual behavior.</span>
      )}
      {species && (
        <div className="species-population-chart">
          <div className="subsection-title">{species.displayName}</div>
          <MetricSparkline
            label="Species alive"
            points={timeline}
            value={(point) => point.speciesPopulation.find((item) => item.speciesId === species.speciesId)?.alive ?? 0}
          />
          <span className="metric">Alive <strong>{species.alive}/{species.total}</strong></span>
        </div>
      )}
      <div className="system-list">
        <div className="subsection-title">Important events</div>
        {importantEvents.map((event) => (
          <div className={`system-row ${eventTone(event)}`} key={`${event.tick}:${event.sequence}`}>
            <span>
              <strong>{event.type}</strong>
              <small>{event.message}</small>
            </span>
            <span>{event.tick}</span>
            <span>{formatNumber(event.importance ?? 0)}</span>
          </div>
        ))}
        {importantEvents.length === 0 && <span className="metric">No important events yet.</span>}
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

export function KnowledgePanel({
  snapshot,
  selectedAgentId,
  onSelectAgent
}: {
  snapshot: SimulationSnapshot | null;
  selectedAgentId: string | null;
  onSelectAgent: (agentId: string) => void;
}) {
  const selectedAgent = selectedAgentId
    ? snapshot?.agents.find((agent) => agent.id === selectedAgentId) ?? null
    : null;
  const recipes = snapshot?.recipeCatalog ?? [];
  const knownRecipeIds = new Set(selectedAgent?.knowledge.records
    .filter((record) => record.kind === "Recipe")
    .map((record) => record.subjectId) ?? []);

  return (
    <section className="debug-panel knowledge-panel">
      <div className="debug-header">
        <h2>Knowledge</h2>
        <span className="pill">{selectedAgent ? selectedAgent.id : "no agent"}</span>
      </div>
      <div className="knowledge-summary">
        <span className="metric">
          Recipes <strong>{selectedAgent?.knowledge.records.filter((record) => record.kind === "Recipe").length ?? 0}/{recipes.length}</strong>
        </span>
        <span className="metric">
          Groups <strong>{snapshot?.groupKnowledge.length ?? 0}</strong>
        </span>
        <span className="metric">
          Discoveries <strong>{snapshot?.discoveryMarkers.length ?? 0}</strong>
        </span>
      </div>
      <div className="system-list">
        <div className="subsection-title">Recipe catalog</div>
        {recipes.map((recipe) => (
          <RecipeCatalogRow key={recipe.id} recipe={recipe} known={knownRecipeIds.has(recipe.id)} />
        ))}
        {recipes.length === 0 && <span className="metric">No recipes registered.</span>}

        <div className="subsection-title">Group knowledge</div>
        {(snapshot?.groupKnowledge ?? []).map((group) => (
          <div className="system-row" key={group.groupId}>
            <span>
              <strong>{group.displayName}</strong>
              <small>{group.knownRecipeIds.length} recipes / {group.knownActionUnlockIds.length} unlocks</small>
            </span>
            <span>{group.memberAgentIds.length}</span>
            <span>{group.knownRecipeIds.length}</span>
          </div>
        ))}
        {(snapshot?.groupKnowledge.length ?? 0) === 0 && <span className="metric">No group knowledge.</span>}

        <div className="subsection-title">Agents</div>
        {(snapshot?.agents ?? []).map((agent) => (
          <button
            key={agent.id}
            className={`entity-row ${selectedAgentId === agent.id ? "selected" : ""}`}
            onClick={() => onSelectAgent(agent.id)}
          >
            <span className="entity-dot agent" />
            <span>
              <strong>{agent.id}</strong>
              <small>{agent.knowledge.records.filter((record) => record.kind === "Recipe").length} recipes known</small>
            </span>
          </button>
        ))}
      </div>
    </section>
  );
}

export function DiscoveryTimelineMarkers({
  markers,
  minimumTick,
  maximumTick,
  onSelectTick
}: {
  markers: DiscoveryMarkerSnapshot[];
  minimumTick: number;
  maximumTick: number;
  onSelectTick: (tick: number) => void;
}) {
  if (markers.length === 0 || maximumTick <= minimumTick) {
    return <div className="timeline-markers" />;
  }

  return (
    <div className="timeline-markers">
      {markers.map((marker) => {
        const left = ((marker.tick - minimumTick) / (maximumTick - minimumTick)) * 100;

        return (
          <button
            key={`${marker.tick}:${marker.agentId}:${marker.recipeId}`}
            className="timeline-marker"
            style={{ left: `${left}%` }}
            title={`${marker.displayName} discovered by ${marker.agentId} at tick ${marker.tick}`}
            onClick={() => onSelectTick(marker.tick)}
          />
        );
      })}
    </div>
  );
}

function RecipeCatalogRow({ recipe, known }: { recipe: RecipeDefinitionSnapshot; known: boolean }) {
  return (
    <div className={`perception-row ${known ? "known" : ""}`}>
      <strong>{recipe.displayName}</strong>
      <small>{known ? "known" : "unknown"} / chance {formatNumber(recipe.discoveryChance)}</small>
      <small>in {formatRecipeParts(recipe.inputs)} / tools {formatRecipeParts(recipe.tools)} / out {formatRecipeParts(recipe.outputs)}</small>
      <small>{recipe.unlocks.map((unlock) => unlock.displayName).join(", ") || "no unlocks"}</small>
    </div>
  );
}

function MetricSparkline({
  label,
  points,
  value,
  compare = []
}: {
  label: string;
  points: SimulationTimelinePoint[];
  value: (point: SimulationTimelinePoint) => number;
  compare?: SimulationTimelinePoint[];
}) {
  const values = points.map(value);
  const compareValues = compare.map(value);
  const allValues = [...values, ...compareValues];
  const min = allValues.length === 0 ? 0 : Math.min(...allValues);
  const max = allValues.length === 0 ? 1 : Math.max(...allValues);
  const path = sparklinePath(values, min, max);
  const comparePath = sparklinePath(compareValues, min, max);
  const latest = values.at(-1) ?? 0;

  return (
    <div className="sparkline-row">
      <span>{label}</span>
      <svg viewBox="0 0 120 34" role="img" aria-label={`${label} timeline`}>
        {comparePath && <path d={comparePath} className="compare" />}
        {path && <path d={path} />}
      </svg>
      <strong>{formatNumber(latest)}</strong>
    </div>
  );
}

function sparklinePath(values: number[], min: number, max: number) {
  if (values.length === 0) {
    return "";
  }

  const width = 120;
  const height = 34;
  const range = Math.max(0.0001, max - min);
  return values.map((value, index) => {
    const x = values.length <= 1 ? 0 : (index / (values.length - 1)) * width;
    const y = height - ((value - min) / range) * (height - 4) - 2;
    return `${index === 0 ? "M" : "L"}${x.toFixed(1)},${y.toFixed(1)}`;
  }).join(" ");
}

function NeedMiniBar({ label, value, invert }: { label: string; value: number; invert?: boolean }) {
  const clamped = clamp01(value / 10);
  const tone = invert
    ? value >= 8 ? "bad" : value >= 5 ? "warn" : "good"
    : value <= 2 ? "bad" : value <= 5 ? "warn" : "good";

  return (
    <span className={`need-mini ${tone}`}>
      <b>{label}</b>
      <i><span style={{ width: `${clamped * 100}%` }} /></i>
      <strong>{formatNumber(value)}</strong>
    </span>
  );
}

function eventTone(event: SimulationEvent) {
  if (event.severity === "critical" || (event.importance ?? 0) >= 0.85) {
    return "critical";
  }

  if (event.severity === "warning" || (event.importance ?? 0) >= 0.5) {
    return "important";
  }

  return "";
}

function aliveTotal(point: SimulationTimelinePoint) {
  return point.speciesPopulation.reduce((sum, species) => sum + species.alive, 0);
}

function TraitMetricRow({ metric }: { metric: PopulationTraitMetric }) {
  const average = clamp01(metric.average);
  const minimum = clamp01(metric.minimum);
  const maximum = clamp01(metric.maximum);
  const left = minimum * 100;
  const width = Math.max(2, (maximum - minimum) * 100);

  return (
    <div className="trait-population-row">
      <span>{traitDisplayName(metric.trait)}</span>
      <div>
        <i style={{ left: `${left}%`, width: `${width}%` }} />
        <b style={{ left: `${average * 100}%` }} />
      </div>
      <strong>{formatNumber(metric.average)}</strong>
      <small>alive {formatNumber(metric.aliveAverage)} / dead {formatNumber(metric.deadAverage)}</small>
    </div>
  );
}

function GenealogyNode({
  agent,
  position,
  selected,
  onSelectAgent
}: {
  agent: AgentSnapshot;
  position: { x: number; y: number };
  selected: boolean;
  onSelectAgent: (agentId: string) => void;
}) {
  return (
    <g className="social-node" transform={`translate(${position.x} ${position.y})`} onClick={() => onSelectAgent(agent.id)}>
      <circle
        r={selected ? 21 : 17}
        fill={agent.isDead ? "#5a5f68" : speciesColor(agent.speciesId)}
        stroke={selected ? "#ffffff" : "#20262b"}
        strokeWidth={selected ? 4 : 2}
      />
      <text y={34}>{agent.id}</text>
      <text y={47} className="genealogy-trait-label">{formatNumber(agent.traits.speed)} spd / {formatNumber(agent.traits.perception)} per</text>
    </g>
  );
}

function TraitSummary({ agent }: { agent: AgentSnapshot }) {
  return (
    <>
      {traitEntries(agent).map(([key, value]) => (
        <div className="system-row" key={key}>
          <span>
            <strong>{traitDisplayName(key)}</strong>
            <small>{traitEffectText(key, value)}</small>
          </span>
          <span>{formatNumber(value)}</span>
          <span>{Math.round(value * 100)}</span>
        </div>
      ))}
    </>
  );
}

function rowPositions(count: number, width: number, y: number) {
  if (count <= 0) {
    return [];
  }

  if (count === 1) {
    return [{ x: width / 2, y }];
  }

  const spacing = Math.min(108, (width - 80) / Math.max(1, count - 1));
  const start = width / 2 - (spacing * (count - 1)) / 2;

  return Array.from({ length: count }, (_, index) => ({ x: start + spacing * index, y }));
}

function traitEntries(agent: AgentSnapshot): Array<[string, number]> {
  return [
    ["perception", agent.traits.perception],
    ["speed", agent.traits.speed],
    ["metabolism", agent.traits.metabolism],
    ["riskTolerance", agent.traits.riskTolerance],
    ["learningRate", agent.traits.learningRate]
  ];
}

function traitDisplayName(trait: string) {
  switch (trait) {
    case "riskTolerance":
      return "Risk";
    case "learningRate":
      return "Learning";
    default:
      return trait.charAt(0).toUpperCase() + trait.slice(1);
  }
}

function traitEffectText(trait: string, value: number) {
  if (trait === "metabolism") {
    return `need decay x${formatNumber(1.2 - clamp01(value) * 0.4)}`;
  }

  if (trait === "learningRate") {
    return `learning ${formatNumber(0.1 + clamp01(value) * 0.3)}`;
  }

  if (trait === "riskTolerance") {
    return `risk score ${formatNumber((clamp01(value) - 0.5) * 0.7)}`;
  }

  return `effect x${formatNumber(0.75 + clamp01(value) * 0.5)}`;
}

function clamp01(value: number) {
  return Math.max(0, Math.min(1, value));
}

function formatRecipeParts(parts: Array<{ resourceId: string; quantity: number }>) {
  return parts.length === 0
    ? "none"
    : parts.map((part) => `${part.resourceId} x${part.quantity}`).join(", ");
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
