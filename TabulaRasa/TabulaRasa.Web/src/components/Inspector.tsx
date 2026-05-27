"use client";

import type { Selection, SimulationDraft, SimulationSnapshot } from "@/types/simulation";
import { updateAgentDraft, updateFoodDraft } from "@/lib/draft";
import { getValue, setValue } from "@/lib/objectPath";
import type { EditableField, SimulationDraftSchema } from "@/types/simulation";

type Props = {
  snapshot: SimulationSnapshot | null;
  draft: SimulationDraft | null;
  schema: SimulationDraftSchema | null;
  selection: Selection;
  onSelect: (selection: Selection) => void;
  editing: boolean;
  canEdit: boolean;
  onDraftChange: (draft: SimulationDraft) => void;
};

export function Inspector({ snapshot, draft, schema, selection, onSelect, editing, canEdit, onDraftChange }: Props) {
  const editable = editing && canEdit && draft;
  const agent =
    selection?.type === "agent"
      ? (editable ? draft.agents : snapshot?.agents)?.find((candidate) => candidate.id === selection.id)
      : null;
  const food =
    selection?.type === "food"
      ? (editable ? draft.food : snapshot?.food)?.find((candidate) => candidate.id === selection.id)
      : null;

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
                .filter((field) => field.valueType !== "gridCells")
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
        <div className="entity-list">
          {(snapshot?.agents ?? []).map((item) => (
            <button
              key={`agent:${item.id}`}
              className={selection?.type === "agent" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "agent", id: item.id })}
            >
              <span className="entity-dot agent" />
              <span>
                <strong>{item.id}</strong>
                <small>
                  AgentEntity · cell {item.cell.x}, {item.cell.y}
                </small>
              </span>
            </button>
          ))}
          {(snapshot?.food ?? []).map((item) => (
            <button
              key={`food:${item.id}`}
              className={selection?.type === "food" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "food", id: item.id })}
            >
              <span className="entity-dot food" />
              <span>
                <strong>{item.id}</strong>
                <small>
                  FoodEntity · {item.isConsumed ? "consumed" : "available"}
                </small>
              </span>
            </button>
          ))}
        </div>
      </section>

      {agent && (
        <section className="section">
          <h3>Agent · {agent.id}</h3>
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(agent.position.x)}, ${Math.floor(agent.position.y)}`],
              ["Position", `${formatNumber(agent.position.x)}, ${formatNumber(agent.position.y)}`],
              ["Type", entityType(agent, "AgentEntity")],
              ["Footprint", footprint(agent, "0.8 x 0.8")],
              ["Needs", `H ${formatNumber(agent.needs.hunger)} / T ${formatNumber(agent.needs.thirst)} / E ${formatNumber(agent.needs.energy)}`],
              ["Movement", snapshot?.agents.find((item) => item.id === agent.id)?.movement?.status ?? "idle"],
              ["Route target", snapshot?.agents.find((item) => item.id === agent.id)?.movement?.targetId ?? "none"]
            ]}
          />
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
        </section>
      )}

      {food && (
        <section className="section">
          <h3>Food · {food.id}</h3>
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(food.position.x)}, ${Math.floor(food.position.y)}`],
              ["Position", `${formatNumber(food.position.x)}, ${formatNumber(food.position.y)}`],
              ["Type", entityType(food, "FoodEntity")],
              ["Footprint", footprint(food, "0.5 x 0.5")],
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
        </section>
      )}

      {selection?.type === "cell" && (
        <section className="section">
          <h3>Cell</h3>
          <div className="field-grid">
            <ReadonlyField label="X" value={selection.cell.x.toString()} />
            <ReadonlyField label="Y" value={selection.cell.y.toString()} />
          </div>
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
