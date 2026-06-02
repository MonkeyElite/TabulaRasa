"use client";

import React from "react";
import type { AgentSnapshot, EditableField, GridCell, SimulationDraftSchema, TerrainType } from "@/types/simulation";
import type { Selection, SimulationDraft, SimulationSnapshot } from "@/types/simulation";
import { addAgentDraft, addFoodDraft, removeAgentDraft, removeFoodDraft, updateAgentDraft, updateFoodDraft } from "@/lib/draft";
import { getValue, setValue } from "@/lib/objectPath";

type Props = {
  snapshot: SimulationSnapshot | null;
  draft: SimulationDraft | null;
  schema: SimulationDraftSchema | null;
  selection: Selection;
  onSelect: (selection: Selection) => void;
  editing: boolean;
  canEdit: boolean;
  onDraftChange: (draft: SimulationDraft) => void;
  onTerrainChange: (cell: GridCell, terrainType: TerrainType) => void;
};

export function Inspector({ snapshot, draft, schema, selection, onSelect, editing, canEdit, onDraftChange, onTerrainChange }: Props) {
  const editable = editing && canEdit && draft;
  const listedAgents = editable ? draft.agents : snapshot?.agents ?? [];
  const listedFood = editable ? draft.food : snapshot?.food ?? [];
  const agent =
    selection?.type === "agent"
      ? (editable ? draft.agents : snapshot?.agents)?.find((candidate) => candidate.id === selection.id)
      : null;
  const selectedSnapshotAgent =
    selection?.type === "agent"
      ? snapshot?.agents.find((candidate) => candidate.id === selection.id) ?? null
      : null;
  const food =
    selection?.type === "food"
      ? (editable ? draft.food : snapshot?.food)?.find((candidate) => candidate.id === selection.id)
      : null;
  const selectedCell = selection?.type === "cell" ? selection.cell : null;
  const blockedCells = (editable ? draft.grid.blockedCells : snapshot?.grid.blockedCells) ?? [];
  const cellIsBlocked = selectedCell ? blockedCells.some((cell) => sameCell(cell, selectedCell)) : false;
  const cellOccupants = selectedCell ? getCellOccupants(snapshot, draft, Boolean(editable), selectedCell) : [];
  const selectedCellTerrain = selectedCell ? getCellTerrain(snapshot, draft, Boolean(editable), selectedCell) : terrainProfile("Plain");

  return (
    <aside className="inspector">
      <section className="section">
        <h2>State</h2>
        <div className="stack">
          <div className="row">
            <span className="metric">
              Tick <strong>{snapshot?.tick ?? "-"}</strong>
            </span>
            <span className="metric">
              Grid <strong>{snapshot ? `${snapshot.grid.width}x${snapshot.grid.height}` : "-"}</strong>
            </span>
          </div>
          <div className="row">
            <span className="pill">{snapshot?.agents.length ?? 0} agents</span>
            <span className="pill">{snapshot?.food.length ?? 0} food</span>
          </div>
          {editing && draft && schema && (
            <div className="field-grid">
              {schema.stateFields.map((field) => (
                <DraftField
                  key={field.path}
                  field={field}
                  disabled={!editable}
                  value={getValue(draft, field.path)}
                  onChange={(value) => onDraftChange(setValue(draft, field.path, value))}
                />
              ))}
              {schema.gridFields
                .filter((field) => field.valueType !== "gridCells" && field.valueType !== "terrainCells")
                .map((field) => (
                  <DraftField
                    key={field.path}
                    field={field}
                    disabled={!editable}
                    value={getValue(draft, field.path)}
                    onChange={(value) => onDraftChange(setValue(draft, field.path, value))}
                  />
                ))}
            </div>
          )}
        </div>
      </section>

      <section className="section">
        <h3>Entities</h3>
        {editable && (
          <div className="row entity-actions">
            <button
              onClick={() => {
                const nextDraft = addAgentDraft(draft);
                const nextAgent = nextDraft.agents.at(-1);
                onDraftChange(nextDraft);
                if (nextAgent) {
                  onSelect({ type: "agent", id: nextAgent.id });
                }
              }}
            >
              Add agent
            </button>
            <button
              onClick={() => {
                const nextDraft = addFoodDraft(draft);
                const nextFood = nextDraft.food.at(-1);
                onDraftChange(nextDraft);
                if (nextFood) {
                  onSelect({ type: "food", id: nextFood.id });
                }
              }}
            >
              Add food
            </button>
          </div>
        )}
        <div className="entity-list">
          {listedAgents.map((item) => (
            <button
              key={`agent:${item.id}`}
              className={selection?.type === "agent" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "agent", id: item.id })}
            >
              <span className="entity-dot agent" />
              <span>
                <strong>{item.id}</strong>
                <small>
                  AgentEntity - cell {Math.floor(item.position.x)}, {Math.floor(item.position.y)}
                </small>
              </span>
            </button>
          ))}
          {listedFood.map((item) => (
            <button
              key={`food:${item.id}`}
              className={selection?.type === "food" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "food", id: item.id })}
            >
              <span className="entity-dot food" />
              <span>
                <strong>{item.id}</strong>
                <small>FoodEntity - {item.isConsumed ? "consumed" : "available"}</small>
              </span>
            </button>
          ))}
        </div>
      </section>

      {agent && (
        <section className="section">
          <h3>Agent - {agent.id}</h3>
          {(() => {
            const movement = snapshot?.agents.find((item) => item.id === agent.id)?.movement ?? null;

            return (
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(agent.position.x)}, ${Math.floor(agent.position.y)}`],
              ["Position", `${formatNumber(agent.position.x)}, ${formatNumber(agent.position.y)}`],
              ["Type", entityType(agent, "AgentEntity")],
              ["Footprint", footprint(agent, "0.8 x 0.8")],
              ["Occupies", occupiesSpace(agent, true)],
              ["Occupied cells", occupiedCells(agent)],
              ["Health", health(agent)],
              ["Needs", `H ${formatNumber(agent.needs.hunger)} / T ${formatNumber(agent.needs.thirst)} / E ${formatNumber(agent.needs.energy)}`],
              ["Movement", movement?.status ?? "idle"],
              ["Route target", movement?.targetId ?? "none"],
              ["Destination", movement ? `${formatNumber(movement.destination.x)}, ${formatNumber(movement.destination.y)}` : "none"],
              ["Waypoint", movement ? `${Math.min(movement.currentWaypointIndex + 1, movement.waypoints.length)} / ${movement.waypoints.length}` : "none"],
              ["Route cost", movement ? formatNumber(movement.routeCost) : "0"],
              ["Speed", movement ? `${formatNumber(movement.lastEffectiveSpeedPerTick)} / ${formatNumber(movement.speedPerTick)}` : "0"],
              ["Stuck", movement ? `${movement.stuckTicks} / ${movement.maxStuckTicks}` : "0"],
              ["Repaths", movement ? `${movement.repathCount} / ${movement.maxRepathAttempts}` : "0"],
              ["Last repath", movement?.lastRepathReason ?? "none"]
            ]}
          />
            );
          })()}
          <GenericEntityEditor
            fields={schema?.agentFields}
            entity={agent}
            disabled={!editable}
            onChange={(nextEntity) =>
              draft &&
              onDraftChange(
                updateAgentDraft(draft, agent.id, nextEntity as Partial<SimulationDraft["agents"][number]>)
              )
            }
          />
          {!editing && selectedSnapshotAgent && <PerceptionDetails agent={selectedSnapshotAgent} />}
          {editable && (
            <button
              className="danger"
              onClick={() => {
                onDraftChange(removeAgentDraft(draft, agent.id));
                onSelect(null);
              }}
            >
              Remove agent
            </button>
          )}
        </section>
      )}

      {food && (
        <section className="section">
          <h3>Food - {food.id}</h3>
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(food.position.x)}, ${Math.floor(food.position.y)}`],
              ["Position", `${formatNumber(food.position.x)}, ${formatNumber(food.position.y)}`],
              ["Type", entityType(food, "FoodEntity")],
              ["Footprint", footprint(food, "0.5 x 0.5")],
              ["Occupies", occupiesSpace(food, !food.isConsumed)],
              ["Occupied cells", occupiedCells(food)],
              ["Health", health(food)],
              ["Consumed", food.isConsumed ? "yes" : "no"]
            ]}
          />
          <GenericEntityEditor
            fields={schema?.foodFields}
            entity={food}
            disabled={!editable}
            onChange={(nextEntity) =>
              draft &&
              onDraftChange(updateFoodDraft(draft, food.id, nextEntity as Partial<SimulationDraft["food"][number]>))
            }
          />
          {editable && (
            <button
              className="danger"
              onClick={() => {
                onDraftChange(removeFoodDraft(draft, food.id));
                onSelect(null);
              }}
            >
              Remove food
            </button>
          )}
        </section>
      )}

      {selection?.type === "cell" && (
        <section className="section">
          <h3>Cell</h3>
          <EntitySummary
            rows={[
              ["X", selection.cell.x.toString()],
              ["Y", selection.cell.y.toString()],
              ["Terrain", `${selectedCellTerrain.terrainType} / cost ${formatNumber(selectedCellTerrain.traversalCost)} / speed x${formatNumber(selectedCellTerrain.speedMultiplier)}`],
              ["Blocked", cellIsBlocked ? "yes" : "no"],
              ["Occupancy", cellOccupants.length === 0 ? "unoccupied" : cellOccupants.map((occupant) => `${occupant.entityId} (${occupant.entityType})`).join(", ")]
            ]}
          />
          {editable && (
            <label className="field">
              <span>Terrain</span>
              <select
                value={selectedCellTerrain.terrainType}
                onChange={(event) => onTerrainChange(selection.cell, event.target.value as TerrainType)}
              >
                {terrainTypes.map((terrainType) => (
                  <option key={terrainType} value={terrainType}>{terrainType}</option>
                ))}
              </select>
            </label>
          )}
        </section>
      )}

      <section className="section">
        <h3>Runtime</h3>
        <div className="stack">
          <span className="metric">Intents: {snapshot?.pendingIntentCount ?? 0}</span>
          <span className="metric">Action requests: {snapshot?.pendingActionRequestCount ?? 0}</span>
          <span className="metric">Jobs: {snapshot?.jobs.length ?? 0}</span>
          <span className="metric">Reservations: {snapshot?.reservations.length ?? 0}</span>
        </div>
      </section>
    </aside>
  );
}

function PerceptionDetails({ agent }: { agent: AgentSnapshot }) {
  return (
    <div className="perception-details">
      <div className="subsection-title">Perceived entities</div>
      {agent.perception.nearbyEntities.length === 0 ? (
        <span className="empty-state">No perceived entities.</span>
      ) : (
        <div className="perception-list">
          {agent.perception.nearbyEntities.map((entity) => (
            <div className="perception-row" key={`${entity.channel}:${entity.entityId}`}>
              <strong>{entity.entityId}</strong>
              <small>
                {entity.entityType} / {entity.channel} / distance {formatNumber(entity.distance)}
              </small>
              <small>
                certainty {formatNumber(entity.certainty)} / relevance {formatNumber(entity.relevance)} / interactable {entity.isInteractable ? "yes" : "no"}
              </small>
            </div>
          ))}
        </div>
      )}

      <div className="subsection-title">Opportunities</div>
      {agent.perception.opportunities.length === 0 ? (
        <span className="empty-state">No opportunities.</span>
      ) : (
        <div className="perception-list">
          {agent.perception.opportunities.map((opportunity, index) => (
            <div className="perception-row" key={`${opportunity.actionType}:${opportunity.targetId ?? index}`}>
              <strong>{opportunity.actionType}</strong>
              <small>
                target {opportunity.targetId ?? "none"} / source {opportunity.sourceEntityId ?? "none"} / {opportunity.channel}
              </small>
              <small>
                relevance {formatNumber(opportunity.relevance)} / position {formatNumber(opportunity.targetPosition.x)}, {formatNumber(opportunity.targetPosition.y)}
              </small>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function GenericEntityEditor({
  fields,
  entity,
  disabled,
  onChange
}: {
  fields: EditableField[] | undefined;
  entity: unknown;
  disabled: boolean;
  onChange: (entity: unknown) => void;
}) {
  const visibleFields = fields ?? [];

  return (
    <div className="field-grid">
      {visibleFields.map((field) => (
        <DraftField
          key={field.path}
          field={field}
          disabled={disabled || !field.isEditable}
          value={getValue(entity, field.path)}
          onChange={(value) => onChange(setValue(entity, field.path, value))}
          wide={field.valueType === "string"}
        />
      ))}
    </div>
  );
}

function DraftField({
  field,
  value,
  disabled,
  onChange,
  wide
}: {
  field: EditableField;
  value: unknown;
  disabled: boolean;
  onChange: (value: unknown) => void;
  wide?: boolean;
}) {
  if (field.valueType === "boolean") {
    return (
      <label className={`field${wide ? " wide" : ""}`}>
        <span>{field.label}</span>
        <input
          type="checkbox"
          checked={Boolean(value)}
          disabled={disabled}
          onChange={(event) => onChange(event.target.checked)}
        />
      </label>
    );
  }

  if (field.valueType === "number") {
    return (
      <NumberField
        label={field.label}
        value={typeof value === "number" ? value : Number(value)}
        disabled={disabled}
        onChange={onChange}
      />
    );
  }

  return <ReadonlyField label={field.label} value={String(value ?? "")} wide={wide} />;
}

function EntitySummary({ rows }: { rows: Array<[string, string]> }) {
  return (
    <dl className="detail-list">
      {rows.map(([label, value]) => (
        <div key={label}>
          <dt>{label}</dt>
          <dd>{value}</dd>
        </div>
      ))}
    </dl>
  );
}

function formatNumber(value: number) {
  return Number.isInteger(value) ? value.toString() : value.toFixed(2);
}

function entityType(entity: unknown, fallback: string) {
  if (entity && typeof entity === "object" && "entityType" in entity && typeof entity.entityType === "string") {
    return entity.entityType;
  }

  return fallback;
}

function footprint(entity: unknown, fallback: string) {
  if (
    entity
    && typeof entity === "object"
    && "footprint" in entity
    && entity.footprint
    && typeof entity.footprint === "object"
    && "width" in entity.footprint
    && "height" in entity.footprint
    && typeof entity.footprint.width === "number"
    && typeof entity.footprint.height === "number"
  ) {
    return `${formatNumber(entity.footprint.width)} x ${formatNumber(entity.footprint.height)}`;
  }

  return fallback;
}

function occupiesSpace(entity: unknown, fallback: boolean) {
  if (entity && typeof entity === "object" && "occupiesSpace" in entity && typeof entity.occupiesSpace === "boolean") {
    return entity.occupiesSpace ? "yes" : "no";
  }

  return fallback ? "yes" : "no";
}

function occupiedCells(entity: unknown) {
  if (entity && typeof entity === "object" && "isConsumed" in entity && entity.isConsumed === true) {
    return "none";
  }

  if (
    entity
    && typeof entity === "object"
    && "occupiedCells" in entity
    && Array.isArray(entity.occupiedCells)
  ) {
    return entity.occupiedCells.length === 0
      ? "none"
      : entity.occupiedCells.map((cell) => `${cell.x}, ${cell.y}`).join("; ");
  }

  if (
    entity
    && typeof entity === "object"
    && "position" in entity
    && entity.position
    && typeof entity.position === "object"
    && "x" in entity.position
    && "y" in entity.position
    && typeof entity.position.x === "number"
    && typeof entity.position.y === "number"
  ) {
    return `${Math.floor(entity.position.x)}, ${Math.floor(entity.position.y)}`;
  }

  return "none";
}

function health(entity: unknown) {
  if (
    entity
    && typeof entity === "object"
    && "health" in entity
    && entity.health
    && typeof entity.health === "object"
    && "current" in entity.health
    && "maximum" in entity.health
    && typeof entity.health.current === "number"
    && typeof entity.health.maximum === "number"
  ) {
    return `${formatNumber(entity.health.current)} / ${formatNumber(entity.health.maximum)}`;
  }

  return "n/a";
}

function getCellOccupants(
  snapshot: SimulationSnapshot | null,
  draft: SimulationDraft | null,
  editing: boolean,
  cell: GridCell
) {
  if (editing && draft) {
    return [
      ...draft.agents.map((agent) => ({
        cell: { x: Math.floor(agent.position.x), y: Math.floor(agent.position.y) },
        entityId: agent.id,
        entityType: "AgentEntity"
      })),
      ...draft.food
        .filter((food) => !food.isConsumed)
        .map((food) => ({
          cell: { x: Math.floor(food.position.x), y: Math.floor(food.position.y) },
          entityId: food.id,
          entityType: "FoodEntity"
        }))
    ].filter((occupant) => sameCell(occupant.cell, cell));
  }

  return snapshot?.grid.occupiedCells.filter((occupant) => sameCell(occupant.cell, cell)) ?? [];
}

const terrainTypes: TerrainType[] = ["Plain", "Road", "Forest", "Mud"];

function getCellTerrain(
  snapshot: SimulationSnapshot | null,
  draft: SimulationDraft | null,
  editing: boolean,
  cell: GridCell
) {
  if (editing && draft) {
    const terrainCell = draft.grid.terrainCells.find((candidate) => sameCell(candidate.cell, cell));

    return terrainProfile(terrainCell?.terrainType ?? "Plain");
  }

  const terrainCell = snapshot?.grid.terrainCells.find((candidate) => sameCell(candidate.cell, cell));

  if (!terrainCell) {
    return terrainProfile("Plain");
  }

  return {
    terrainType: terrainCell.terrainType,
    traversalCost: terrainCell.traversalCost,
    speedMultiplier: terrainCell.speedMultiplier
  };
}

function terrainProfile(terrainType: string) {
  switch (terrainType) {
    case "Road":
      return { terrainType: "Road" as const, traversalCost: 0.5, speedMultiplier: 1.25 };
    case "Forest":
      return { terrainType: "Forest" as const, traversalCost: 2, speedMultiplier: 0.75 };
    case "Mud":
      return { terrainType: "Mud" as const, traversalCost: 3, speedMultiplier: 0.5 };
    default:
      return { terrainType: "Plain" as const, traversalCost: 1, speedMultiplier: 1 };
  }
}

function sameCell(left: GridCell, right: GridCell) {
  return left.x === right.x && left.y === right.y;
}

function ReadonlyField({ label, value, wide }: { label: string; value: string; wide?: boolean }) {
  return (
    <label className={`field${wide ? " wide" : ""}`}>
      <span>{label}</span>
      <input value={value} readOnly />
    </label>
  );
}

function NumberField({
  label,
  value,
  disabled,
  onChange
}: {
  label: string;
  value: number;
  disabled: boolean;
  onChange: (value: number) => void;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      <input
        type="number"
        step="0.1"
        value={Number.isFinite(value) ? value : 0}
        disabled={disabled}
        onChange={(event) => onChange(Number(event.target.value))}
      />
    </label>
  );
}
