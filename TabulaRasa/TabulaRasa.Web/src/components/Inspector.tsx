"use client";

import React from "react";
import type { AgentMemoryRecordSnapshot, AgentNeeds, AgentSnapshot, EditableField, EntityHealth, GridCell, KnowledgeRecordSnapshot, SimulationDraftSchema, SocialRelationshipSnapshot, TerrainType } from "@/types/simulation";
import type { Selection, SimulationDraft, SimulationSnapshot } from "@/types/simulation";
import {
  addAgentDraft,
  addPlantDraft,
  addResourceContainerDraft,
  addResourceDefinitionDraft,
  addResourceDepositDraft,
  addWaterSourceDraft,
  removeAgentDraft,
  removePlantDraft,
  removeResourceContainerDraft,
  removeResourceDepositDraft,
  removeWaterSourceDraft,
  updateAgentDraft,
  updatePlantDraft,
  updateResourceContainerDraft,
  updateResourceDefinitionDraft,
  updateResourceDepositDraft,
  updateWaterSourceDraft
} from "@/lib/draft";
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
  const [inspectorTab, setInspectorTab] = React.useState<"state" | "entities" | "selection">("selection");
  const [agentTab, setAgentTab] = React.useState<"overview" | "traits" | "work" | "perception" | "memory" | "knowledge" | "relationships" | "learning">("overview");
  const editable = editing && canEdit && draft;
  const listedAgents = editable ? draft.agents : snapshot?.agents ?? [];
  const listedResourceContainers = editable ? draft.resourceContainers : snapshot?.resourceContainers ?? [];
  const listedResourceDefinitions = editable ? draft.resourceDefinitions : snapshot?.resourceDefinitions ?? [];
  const listedPlants = editable ? draft.plants : snapshot?.plants ?? [];
  const listedWaterSources = editable ? draft.waterSources : snapshot?.waterSources ?? [];
  const listedResourceDeposits = editable ? draft.resourceDeposits : snapshot?.resourceDeposits ?? [];
  const agent =
    selection?.type === "agent"
      ? (editable ? draft.agents : snapshot?.agents)?.find((candidate) => candidate.id === selection.id)
      : null;
  const selectedSnapshotAgent =
    selection?.type === "agent"
      ? snapshot?.agents.find((candidate) => candidate.id === selection.id) ?? null
      : null;
  const resourceContainer =
    selection?.type === "resourceContainer"
      ? (editable ? draft.resourceContainers : snapshot?.resourceContainers)?.find((candidate) => candidate.id === selection.id)
      : null;
  const resourceDefinition =
    selection?.type === "resourceDefinition"
      ? (editable ? draft.resourceDefinitions : snapshot?.resourceDefinitions)?.find((candidate) => candidate.id === selection.id)
      : null;
  const plant =
    selection?.type === "plant"
      ? (editable ? draft.plants : snapshot?.plants)?.find((candidate) => candidate.id === selection.id)
      : null;
  const waterSource =
    selection?.type === "waterSource"
      ? (editable ? draft.waterSources : snapshot?.waterSources)?.find((candidate) => candidate.id === selection.id)
      : null;
  const resourceDeposit =
    selection?.type === "resourceDeposit"
      ? (editable ? draft.resourceDeposits : snapshot?.resourceDeposits)?.find((candidate) => candidate.id === selection.id)
      : null;
  const selectedCell = selection?.type === "cell" ? selection.cell : null;
  const blockedCells = (editable ? draft.grid.blockedCells : snapshot?.grid.blockedCells) ?? [];
  const cellIsBlocked = selectedCell ? blockedCells.some((cell) => sameCell(cell, selectedCell)) : false;
  const cellOccupants = selectedCell ? getCellOccupants(snapshot, draft, Boolean(editable), selectedCell) : [];
  const selectedCellTerrain = selectedCell ? getCellTerrain(snapshot, draft, Boolean(editable), selectedCell) : terrainProfile("Plain");
  const addSpeciesAgent = (speciesId: "human" | "deer" | "wolf") => {
    if (!draft) {
      return;
    }

    const nextDraft = addAgentDraft(draft);
    const nextAgent = nextDraft.agents.at(-1);
    if (!nextAgent) {
      onDraftChange(nextDraft);
      return;
    }

    const speciesDraft = updateAgentDraft(nextDraft, nextAgent.id, {
      speciesId,
      id: `${speciesId}-${nextDraft.agents.filter((agent) => agent.speciesId === speciesId).length + 1}`
    });
    onDraftChange(speciesDraft);
    onSelect({ type: "agent", id: speciesDraft.agents.at(-1)?.id ?? nextAgent.id });
  };

  return (
    <aside className="inspector">
      <div className="rail-tabs inspector-tabs">
        <button className={inspectorTab === "state" ? "selected" : ""} onClick={() => setInspectorTab("state")}>State</button>
        <button className={inspectorTab === "entities" ? "selected" : ""} onClick={() => setInspectorTab("entities")}>Entities</button>
        <button className={inspectorTab === "selection" ? "selected" : ""} onClick={() => setInspectorTab("selection")}>Selection</button>
      </div>

      {inspectorTab === "state" && (
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
            <span className="pill">{snapshot?.populationCount ?? snapshot?.agents.length ?? 0} population</span>
            <span className="pill">{snapshot?.aliveAgentCount ?? snapshot?.agents.filter((item) => !item.isDead).length ?? 0} alive</span>
            <span className="pill">{snapshot?.deadAgentCount ?? snapshot?.agents.filter((item) => item.isDead).length ?? 0} dead</span>
            <span className="pill">{snapshot?.resourceContainers.length ?? 0} containers</span>
            <span className="pill">{snapshot?.ecologyStats?.plantCount ?? 0} plants</span>
            <span className="pill">{snapshot?.ecologyStats?.waterSourceCount ?? 0} water</span>
            <span className="pill">{snapshot?.environment ? `${snapshot.environment.weather} ${formatNumber(snapshot.environment.temperature)}C` : "weather -"}</span>
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
      )}

      {inspectorTab === "entities" && (
      <section className="section">
        <h3>Entities</h3>
        {editable && (
          <div className="row entity-actions">
            <button onClick={() => addSpeciesAgent("human")}>Add human</button>
            <button onClick={() => addSpeciesAgent("deer")}>Add deer</button>
            <button onClick={() => addSpeciesAgent("wolf")}>Add wolf</button>
            <button
              onClick={() => {
                const nextDraft = addResourceContainerDraft(draft);
                const nextContainer = nextDraft.resourceContainers.at(-1);
                onDraftChange(nextDraft);
                if (nextContainer) {
                  onSelect({ type: "resourceContainer", id: nextContainer.id });
                }
              }}
            >
              Add container
            </button>
            <button
              onClick={() => {
                const nextDraft = addResourceDefinitionDraft(draft);
                const nextDefinition = nextDraft.resourceDefinitions.at(-1);
                onDraftChange(nextDraft);
                if (nextDefinition) {
                  onSelect({ type: "resourceDefinition", id: nextDefinition.id });
                }
              }}
            >
              Add resource
            </button>
            <button
              onClick={() => {
                const nextDraft = addPlantDraft(draft);
                const nextPlant = nextDraft.plants.at(-1);
                onDraftChange(nextDraft);
                if (nextPlant) {
                  onSelect({ type: "plant", id: nextPlant.id });
                }
              }}
            >
              Add plant
            </button>
            <button
              onClick={() => {
                const nextDraft = addWaterSourceDraft(draft);
                const nextWaterSource = nextDraft.waterSources.at(-1);
                onDraftChange(nextDraft);
                if (nextWaterSource) {
                  onSelect({ type: "waterSource", id: nextWaterSource.id });
                }
              }}
            >
              Add water
            </button>
            <button
              onClick={() => {
                const nextDraft = addResourceDepositDraft(draft);
                const nextDeposit = nextDraft.resourceDeposits.at(-1);
                onDraftChange(nextDraft);
                if (nextDeposit) {
                  onSelect({ type: "resourceDeposit", id: nextDeposit.id });
                }
              }}
            >
              Add deposit
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
              <span className={`entity-dot ${isDeadAgent(item) ? "corpse" : "agent"}`} />
              <span>
                <strong>{item.id}</strong>
                <small>
                  {isDeadAgent(item) ? "Corpse" : "AgentEntity"} - cell {Math.floor(item.position.x)}, {Math.floor(item.position.y)}
                  {"speciesId" in item ? ` - ${item.speciesId}` : ""}
                </small>
              </span>
            </button>
          ))}
          {listedResourceContainers.map((item) => (
            <button
              key={`resource-container:${item.id}`}
              className={selection?.type === "resourceContainer" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "resourceContainer", id: item.id })}
            >
              <span className="entity-dot food" />
              <span>
                <strong>{item.id}</strong>
                <small>Container - {inventoryQuantity(item.inventory)} resources</small>
              </span>
            </button>
          ))}
          {listedResourceDefinitions.map((item) => (
            <button
              key={`resource-definition:${item.id}`}
              className={selection?.type === "resourceDefinition" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "resourceDefinition", id: item.id })}
            >
              <span className="entity-dot food" />
              <span>
                <strong>{item.displayName}</strong>
                <small>{item.id} - {item.isConsumable ? "consumable" : "resource"}</small>
              </span>
            </button>
          ))}
          {listedPlants.map((item) => (
            <button
              key={`plant:${item.id}`}
              className={selection?.type === "plant" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "plant", id: item.id })}
            >
              <span className="entity-dot plant" />
              <span>
                <strong>{item.id}</strong>
                <small>Plant - {item.resourceId} {item.yield}/{item.maxYield}</small>
              </span>
            </button>
          ))}
          {listedWaterSources.map((item) => (
            <button
              key={`water-source:${item.id}`}
              className={selection?.type === "waterSource" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "waterSource", id: item.id })}
            >
              <span className="entity-dot water" />
              <span>
                <strong>{item.id}</strong>
                <small>Water - {formatNumber(item.currentVolume)} / {formatNumber(item.maxVolume)}</small>
              </span>
            </button>
          ))}
          {listedResourceDeposits.map((item) => (
            <button
              key={`resource-deposit:${item.id}`}
              className={selection?.type === "resourceDeposit" && selection.id === item.id ? "entity-row selected" : "entity-row"}
              onClick={() => onSelect({ type: "resourceDeposit", id: item.id })}
            >
              <span className="entity-dot deposit" />
              <span>
                <strong>{item.id}</strong>
                <small>{item.resourceId} - {item.quantity} / {item.maxQuantity}</small>
              </span>
            </button>
          ))}
        </div>
      </section>
      )}

      {inspectorTab === "selection" && (
      <>
      {agent && (
        <section className="section">
          <h3>Agent - {agent.id}</h3>
          {!editing && selectedSnapshotAgent && (
            <div className="tabs">
              <button className={agentTab === "overview" ? "selected" : ""} onClick={() => setAgentTab("overview")}>Overview</button>
              <button className={agentTab === "traits" ? "selected" : ""} onClick={() => setAgentTab("traits")}>Traits</button>
              <button className={agentTab === "work" ? "selected" : ""} onClick={() => setAgentTab("work")}>Work</button>
              <button className={agentTab === "perception" ? "selected" : ""} onClick={() => setAgentTab("perception")}>Perception</button>
              <button className={agentTab === "memory" ? "selected" : ""} onClick={() => setAgentTab("memory")}>Memory</button>
              <button className={agentTab === "knowledge" ? "selected" : ""} onClick={() => setAgentTab("knowledge")}>Knowledge</button>
              <button className={agentTab === "relationships" ? "selected" : ""} onClick={() => setAgentTab("relationships")}>Relationships</button>
              <button className={agentTab === "learning" ? "selected" : ""} onClick={() => setAgentTab("learning")}>Decision</button>
            </div>
          )}
          {(editing || agentTab === "overview") && (
            <>
          {(() => {
            const movement = snapshot?.agents.find((item) => item.id === agent.id)?.movement ?? null;

            return (
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(agent.position.x)}, ${Math.floor(agent.position.y)}`],
              ["Position", `${formatNumber(agent.position.x)}, ${formatNumber(agent.position.y)}`],
              ["Type", entityType(agent, "AgentEntity")],
              ["Species", "speciesId" in agent ? agent.speciesId : "human"],
              ["Age", "ageTicks" in agent ? agent.ageTicks.toString() : "0"],
              ["Born", "bornTick" in agent ? agent.bornTick.toString() : "0"],
              ["Parents", "parentIds" in agent && agent.parentIds.length > 0 ? agent.parentIds.join(", ") : "none"],
              ["Offspring", "offspringIds" in agent && agent.offspringIds.length > 0 ? agent.offspringIds.join(", ") : "none"],
              ["Last reproduced", "lastReproducedTick" in agent && agent.lastReproducedTick !== null ? agent.lastReproducedTick.toString() : "never"],
              ["Death", selectedSnapshotAgent?.deathTick !== null && selectedSnapshotAgent?.deathTick !== undefined ? `${selectedSnapshotAgent.deathCause ?? "unknown"} @ ${selectedSnapshotAgent.deathTick}` : "n/a"],
              ["Footprint", footprint(agent, "0.8 x 0.8")],
              ["Occupies", occupiesSpace(agent, true)],
              ["Occupied cells", occupiedCells(agent)],
              ["Health", health(agent)],
              ["Status", isDeadAgent(agent) ? "dead" : "alive"],
              ["Needs", `H ${formatNumber(agent.needs.hunger)} / T ${formatNumber(agent.needs.thirst)} / E ${formatNumber(agent.needs.energy)} / F ${formatNumber(agent.needs.fatigue)}`],
              ["Goal", selectedSnapshotAgent?.currentGoal ? `${selectedSnapshotAgent.currentGoal.needKey} / ${selectedSnapshotAgent.currentGoal.status}` : "none"],
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
          <AgentVitals needs={agent.needs} health={selectedSnapshotAgent?.health ?? healthValue(agent)} isDead={isDeadAgent(agent)} />
          {"inventory" in agent && agent.inventory && <InventoryDetails inventory={agent.inventory} />}
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
            </>
          )}
          {!editing && selectedSnapshotAgent && agentTab === "work" && <WorkDetails agent={selectedSnapshotAgent} />}
          {!editing && selectedSnapshotAgent && agentTab === "traits" && <TraitDetails agent={selectedSnapshotAgent} snapshot={snapshot} />}
          {!editing && selectedSnapshotAgent && agentTab === "perception" && <PerceptionDetails agent={selectedSnapshotAgent} />}
          {!editing && selectedSnapshotAgent && agentTab === "memory" && <MemoryDetails agent={selectedSnapshotAgent} />}
          {!editing && selectedSnapshotAgent && agentTab === "knowledge" && <KnowledgeDetails agent={selectedSnapshotAgent} />}
          {!editing && selectedSnapshotAgent && agentTab === "relationships" && <RelationshipDetails agent={selectedSnapshotAgent} />}
          {!editing && selectedSnapshotAgent && agentTab === "learning" && <LearningDetails agent={selectedSnapshotAgent} />}
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

      {resourceContainer && (
        <section className="section">
          <h3>Container - {resourceContainer.id}</h3>
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(resourceContainer.position.x)}, ${Math.floor(resourceContainer.position.y)}`],
              ["Position", `${formatNumber(resourceContainer.position.x)}, ${formatNumber(resourceContainer.position.y)}`],
              ["Type", entityType(resourceContainer, "ResourceContainerEntity")],
              ["Footprint", footprint(resourceContainer, "0.5 x 0.5")],
              ["Occupies", occupiesSpace(resourceContainer, inventoryQuantity(resourceContainer.inventory) > 0)],
              ["Occupied cells", occupiedCells(resourceContainer)],
              ["Health", health(resourceContainer)]
            ]}
          />
          <InventoryDetails inventory={resourceContainer.inventory} />
          <GenericEntityEditor
            fields={schema?.resourceContainerFields}
            entity={resourceContainer}
            disabled={!editable}
            onChange={(nextEntity) =>
              draft &&
              onDraftChange(updateResourceContainerDraft(draft, resourceContainer.id, nextEntity as Partial<SimulationDraft["resourceContainers"][number]>))
            }
          />
          {editable && (
            <button
              className="danger"
              onClick={() => {
                onDraftChange(removeResourceContainerDraft(draft, resourceContainer.id));
                onSelect(null);
              }}
            >
              Remove container
            </button>
          )}
        </section>
      )}

      {resourceDefinition && (
        <section className="section">
          <h3>Resource - {resourceDefinition.displayName}</h3>
          <EntitySummary
            rows={[
              ["Id", resourceDefinition.id],
              ["Icon", resourceDefinition.iconKey],
              ["Unit weight", formatNumber(resourceDefinition.unitWeight)],
              ["Stack max", resourceDefinition.maxStackQuantity.toString()],
              ["Consumable", resourceDefinition.isConsumable ? "yes" : "no"],
              ["Need effects", `H ${formatNumber(resourceDefinition.needEffects.hungerDelta)} / T ${formatNumber(resourceDefinition.needEffects.thirstDelta)} / E ${formatNumber(resourceDefinition.needEffects.energyDelta)} / F ${formatNumber(resourceDefinition.needEffects.fatigueDelta)}`]
            ]}
          />
          <GenericEntityEditor
            fields={schema?.resourceDefinitionFields}
            entity={resourceDefinition}
            disabled={!editable || resourceDefinition.id === "food"}
            onChange={(nextEntity) =>
              draft &&
              onDraftChange(updateResourceDefinitionDraft(draft, resourceDefinition.id, nextEntity as Partial<SimulationDraft["resourceDefinitions"][number]>))
            }
          />
        </section>
      )}

      {plant && (
        <section className="section">
          <h3>Plant - {plant.id}</h3>
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(plant.position.x)}, ${Math.floor(plant.position.y)}`],
              ["Position", `${formatNumber(plant.position.x)}, ${formatNumber(plant.position.y)}`],
              ["Resource", plant.resourceId],
              ["Yield", `${plant.yield} / ${plant.maxYield}`],
              ["Regrowth", `${plant.ticksUntilRegrowth} / ${plant.regrowthTicks}`],
              ["Depleted", `${plant.depletedTicks} / ${plant.decayTicksAfterDepleted}`],
              ["Status", plant.isDecayed ? "decayed" : plant.yield > 0 ? "harvestable" : "depleted"]
            ]}
          />
          <GenericEntityEditor
            fields={schema?.plantFields}
            entity={plant}
            disabled={!editable}
            onChange={(nextEntity) =>
              draft &&
              onDraftChange(updatePlantDraft(draft, plant.id, nextEntity as Partial<SimulationDraft["plants"][number]>))
            }
          />
          {editable && (
            <button
              className="danger"
              onClick={() => {
                onDraftChange(removePlantDraft(draft, plant.id));
                onSelect(null);
              }}
            >
              Remove plant
            </button>
          )}
        </section>
      )}

      {waterSource && (
        <section className="section">
          <h3>Water - {waterSource.id}</h3>
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(waterSource.position.x)}, ${Math.floor(waterSource.position.y)}`],
              ["Position", `${formatNumber(waterSource.position.x)}, ${formatNumber(waterSource.position.y)}`],
              ["Volume", `${formatNumber(waterSource.currentVolume)} / ${formatNumber(waterSource.maxVolume)}`],
              ["Rain refill", formatNumber(waterSource.refillPerRainTick)],
              ["Heat evap", formatNumber(waterSource.evaporationPerHeatTick)]
            ]}
          />
          <GenericEntityEditor
            fields={schema?.waterSourceFields}
            entity={waterSource}
            disabled={!editable}
            onChange={(nextEntity) =>
              draft &&
              onDraftChange(updateWaterSourceDraft(draft, waterSource.id, nextEntity as Partial<SimulationDraft["waterSources"][number]>))
            }
          />
          {editable && (
            <button
              className="danger"
              onClick={() => {
                onDraftChange(removeWaterSourceDraft(draft, waterSource.id));
                onSelect(null);
              }}
            >
              Remove water
            </button>
          )}
        </section>
      )}

      {resourceDeposit && (
        <section className="section">
          <h3>Deposit - {resourceDeposit.id}</h3>
          <EntitySummary
            rows={[
              ["Cell", `${Math.floor(resourceDeposit.position.x)}, ${Math.floor(resourceDeposit.position.y)}`],
              ["Position", `${formatNumber(resourceDeposit.position.x)}, ${formatNumber(resourceDeposit.position.y)}`],
              ["Resource", resourceDeposit.resourceId],
              ["Quantity", `${resourceDeposit.quantity} / ${resourceDeposit.maxQuantity}`]
            ]}
          />
          <GenericEntityEditor
            fields={schema?.resourceDepositFields}
            entity={resourceDeposit}
            disabled={!editable}
            onChange={(nextEntity) =>
              draft &&
              onDraftChange(updateResourceDepositDraft(draft, resourceDeposit.id, nextEntity as Partial<SimulationDraft["resourceDeposits"][number]>))
            }
          />
          {editable && (
            <button
              className="danger"
              onClick={() => {
                onDraftChange(removeResourceDepositDraft(draft, resourceDeposit.id));
                onSelect(null);
              }}
            >
              Remove deposit
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

      {!agent && !resourceContainer && !resourceDefinition && !plant && !waterSource && !resourceDeposit && selection?.type !== "cell" && (
        <section className="section">
          <h3>Selection</h3>
          <span className="empty-state">No entity or cell selected.</span>
        </section>
      )}
      </>
      )}
    </aside>
  );
}

type InventoryLike = {
  maxSlots: number;
  maxWeight: number;
  usedSlots?: number;
  usedWeight?: number;
  stacks: Array<{ stackId: string; resourceId: string; quantity: number }>;
};

function InventoryDetails({ inventory }: { inventory: InventoryLike }) {
  const usedSlots = inventory.usedSlots ?? inventory.stacks.length;
  const usedWeight = inventory.usedWeight ?? 0;

  return (
    <div className="perception-details">
      <div className="subsection-title">Inventory</div>
      <EntitySummary
        rows={[
          ["Slots", `${usedSlots} / ${inventory.maxSlots}`],
          ["Weight", `${formatNumber(usedWeight)} / ${formatNumber(inventory.maxWeight)}`]
        ]}
      />
      {inventory.stacks.length === 0 ? (
        <span className="empty-state">No resources.</span>
      ) : (
        <div className="perception-list">
          {inventory.stacks.map((stack) => (
            <div className="perception-row" key={stack.stackId}>
              <strong>{stack.resourceId}</strong>
              <small>{stack.quantity} in {stack.stackId}</small>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function WorkDetails({ agent }: { agent: AgentSnapshot }) {
  const goal = agent.currentGoal;
  const tasks = agent.taskQueue;

  return (
    <div className="perception-details">
      <div className="subsection-title">Current goal</div>
      {goal ? (
        <EntitySummary
          rows={[
            ["Need", goal.needKey],
            ["Status", goal.status],
            ["Priority", goal.priority.toString()],
            ["Target", goal.targetId ?? "none"],
            ["Job", goal.jobId ?? "none"],
            ["Reason", goal.failureReason ?? goal.reason]
          ]}
        />
      ) : (
        <span className="empty-state">No active goal.</span>
      )}

      <div className="subsection-title">Task queue</div>
      {tasks.length === 0 ? (
        <span className="empty-state">No queued tasks.</span>
      ) : (
        <div className="perception-list">
          {tasks.map((task) => (
            <div className="perception-row" key={task.id}>
              <strong>{task.name}</strong>
              <small>
                {task.status} / {task.executionKind} / step {task.stepId}
              </small>
              <small>
                progress {task.progressTicks} / {task.requiredProgressTicks} / dispatches {task.dispatchCount}
              </small>
              <small>
                action {task.atomicAction ?? "none"} / target {task.targetId ?? "none"} / assigned {task.assignedAgentId ?? "none"}
              </small>
              {task.failureReason && <small>{task.failureReason}</small>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function LearningDetails({ agent }: { agent: AgentSnapshot }) {
  const decision = agent.decision;
  const entries = agent.learning.entries;

  return (
    <div className="perception-details">
      <div className="subsection-title">Decision explanation</div>
      {decision ? (
        <>
          <EntitySummary
            rows={[
              ["Selected goal", decision.selectedGoal],
              ["Selected action", decision.selectedAction],
              ["Target", decision.targetId ?? "none"],
              ["Context", decision.contextKey],
              ["Explored", decision.explored ? "yes" : "no"]
            ]}
          />
          <div className="perception-list">
            {Object.entries(decision.needPressures).map(([need, pressure]) => (
              <div className="perception-row" key={need}>
                <strong>{need}</strong>
                <small>pressure {formatNumber(pressure)}</small>
              </div>
            ))}
          </div>
          <div className="subsection-title">Action scores</div>
          <div className="perception-list">
            {decision.actionScores.map((score) => (
              <div className="perception-row" key={`${score.contextKey}:${score.actionType}:${score.targetId ?? "none"}`}>
                <strong>{score.actionType}</strong>
                <small>
                  score {formatNumber(score.score)} / weight {formatNumber(score.learnedWeight)} / need {formatNumber(score.needPressure)}
                </small>
                <small>
                  goal {score.selectedGoal} / target {score.targetId ?? "none"} / {score.targetType} / {score.channel}
                </small>
                <small>context {score.contextKey}</small>
              </div>
            ))}
          </div>
        </>
      ) : (
        <span className="empty-state">No decision yet.</span>
      )}

      <div className="subsection-title">Learned outcomes</div>
      {entries.length === 0 ? (
        <span className="empty-state">No learned outcomes.</span>
      ) : (
        <div className="perception-list">
          {entries.map((entry) => (
            <div className="perception-row" key={`${entry.contextKey}:${entry.actionType}`}>
              <strong>{entry.contextKey}</strong>
              <small>
                {entry.actionType} / weight {formatNumber(entry.learnedWeight)} / avg {formatNumber(entry.averageOutcomeScore)} / last {formatNumber(entry.lastOutcomeScore)}
              </small>
              <small>
                attempts {entry.attempts} / success {entry.successes} / fail {entry.failures}
              </small>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function MemoryDetails({ agent }: { agent: AgentSnapshot }) {
  const memories = agent.memory.memories;

  return (
    <div className="perception-details">
      <div className="subsection-title">Remembered locations and entities</div>
      {memories.length === 0 ? (
        <span className="empty-state">No memories.</span>
      ) : (
        <div className="perception-list">
          {memories.map((memory) => (
            <MemoryRow key={memory.id} memory={memory} />
          ))}
        </div>
      )}
    </div>
  );
}

function MemoryRow({ memory }: { memory: AgentMemoryRecordSnapshot }) {
  return (
    <div className="perception-row">
      <strong>{memory.subjectId}</strong>
      <small>
        {memory.kind} / {memory.subjectType} / strength {formatNumber(memory.strength)} / certainty {formatNumber(memory.certainty)}
      </small>
      <small>
        tick {memory.lastUpdatedTick} / expires {memory.expiresAtTick ?? "never"} / {formatNumber(memory.position.x)}, {formatNumber(memory.position.y)}
      </small>
      <small>{memory.summary}</small>
    </div>
  );
}

function KnowledgeDetails({ agent }: { agent: AgentSnapshot }) {
  const records = agent.knowledge.records;

  return (
    <div className="perception-details">
      <div className="subsection-title">Known recipes and unlocks</div>
      {records.length === 0 ? (
        <span className="empty-state">No known recipes.</span>
      ) : (
        <div className="perception-list">
          {records.map((record) => (
            <KnowledgeRow key={record.id} record={record} />
          ))}
        </div>
      )}
    </div>
  );
}

function TraitDetails({ agent, snapshot }: { agent: AgentSnapshot; snapshot: SimulationSnapshot | null }) {
  const mutationNote = birthMutationNote(agent, snapshot);

  return (
    <div className="perception-details">
      <div className="subsection-title">Inherited traits</div>
      <div className="vitals trait-vitals">
        <TraitBar label="Perception" value={agent.traits.perception} detail={`sight x${formatNumber(traitMultiplier(agent.traits.perception))}`} />
        <TraitBar label="Speed" value={agent.traits.speed} detail={`move x${formatNumber(traitMultiplier(agent.traits.speed))}`} />
        <TraitBar label="Metabolism" value={agent.traits.metabolism} detail={`decay x${formatNumber(metabolismMultiplier(agent.traits.metabolism))}`} />
        <TraitBar label="Risk" value={agent.traits.riskTolerance} detail={`score ${formatNumber(riskAdjustment(agent.traits.riskTolerance))}`} />
        <TraitBar label="Learning" value={agent.traits.learningRate} detail={`rate ${formatNumber(learningRate(agent.traits.learningRate))}`} />
      </div>
      <EntitySummary
        rows={[
          ["Parents", agent.parentIds.length > 0 ? agent.parentIds.join(", ") : "none"],
          ["Offspring", agent.offspringIds.length > 0 ? agent.offspringIds.join(", ") : "none"],
          ["Mutation", mutationNote]
        ]}
      />
    </div>
  );
}

function TraitBar({ label, value, detail }: { label: string; value: number; detail: string }) {
  const clamped = Math.max(0, Math.min(1, value));

  return (
    <div className="vital trait-vital">
      <span>{label}</span>
      <div className="vital-track">
        <div style={{ width: `${clamped * 100}%` }} />
      </div>
      <strong>{formatNumber(value)}</strong>
      <small>{detail}</small>
    </div>
  );
}

function KnowledgeRow({ record }: { record: KnowledgeRecordSnapshot }) {
  return (
    <div className="perception-row">
      <strong>{record.displayName}</strong>
      <small>
        {record.kind} / {record.subjectId} / {record.source}
      </small>
      <small>
        discovered {record.discoveredTick} / updated {record.lastUpdatedTick} / from {record.sourceAgentId ?? "self"}
      </small>
      {record.metadata.description && <small>{record.metadata.description}</small>}
    </div>
  );
}

function RelationshipDetails({ agent }: { agent: AgentSnapshot }) {
  const relationships = agent.social.relationships;
  const groups = agent.social.groups;

  return (
    <div className="perception-details">
      <div className="subsection-title">Groups</div>
      {groups.length === 0 ? (
        <span className="empty-state">No groups.</span>
      ) : (
        <div className="perception-list">
          {groups.map((group) => (
            <div className="perception-row" key={group.groupId}>
              <strong>{group.displayName}</strong>
              <small>{group.groupId} / {group.kind} / joined {group.joinedTick}</small>
            </div>
          ))}
        </div>
      )}

      <div className="subsection-title">Relationships</div>
      {relationships.length === 0 ? (
        <span className="empty-state">No relationships.</span>
      ) : (
        <div className="perception-list">
          {relationships.map((relationship) => (
            <RelationshipRow key={relationship.otherAgentId} relationship={relationship} />
          ))}
        </div>
      )}
    </div>
  );
}

function RelationshipRow({ relationship }: { relationship: SocialRelationshipSnapshot }) {
  return (
    <div className="perception-row">
      <strong>{relationship.otherAgentId}</strong>
      <small>
        interactions {relationship.interactionCount} / seen {relationship.lastSeenTick ?? "never"} / talked {relationship.lastInteractionTick ?? "never"}
      </small>
      <RelationshipMeter label="Familiarity" value={relationship.familiarity} />
      <RelationshipMeter label="Trust" value={relationship.trust} />
      <RelationshipMeter label="Fear" value={relationship.fear} />
      <RelationshipMeter label="Affinity" value={relationship.affinity} />
      <small>{relationship.sharedGroupIds.length > 0 ? relationship.sharedGroupIds.join(", ") : "no shared groups"}</small>
    </div>
  );
}

function RelationshipMeter({ label, value }: { label: string; value: number }) {
  const clamped = Math.max(0, Math.min(1, value));

  return (
    <span className="relationship-meter">
      <span>{label}</span>
      <i><b style={{ width: `${clamped * 100}%` }} /></i>
      <strong>{formatNumber(value)}</strong>
    </span>
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

  return (
    <label className={`field${wide ? " wide" : ""}`}>
      <span>{field.label}</span>
      <input
        value={String(value ?? "")}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
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

function AgentVitals({ needs, health, isDead }: { needs: AgentNeeds; health: EntityHealth | null; isDead: boolean }) {
  return (
    <div className="vitals">
      <NeedBar label="Health" value={health?.current ?? (isDead ? 0 : 10)} maximum={health?.maximum ?? 10} tone={isDead ? "bad" : "good"} />
      <NeedBar label="Hunger" value={needs.hunger} maximum={10} tone={needs.hunger >= 8 ? "bad" : needs.hunger >= 5 ? "warn" : "good"} invert />
      <NeedBar label="Thirst" value={needs.thirst} maximum={10} tone={needs.thirst >= 8 ? "bad" : needs.thirst >= 5 ? "warn" : "good"} invert />
      <NeedBar label="Energy" value={needs.energy} maximum={10} tone={needs.energy <= 2 ? "bad" : needs.energy <= 5 ? "warn" : "good"} />
      <NeedBar label="Fatigue" value={needs.fatigue} maximum={10} tone={needs.fatigue >= 8 ? "bad" : needs.fatigue >= 5 ? "warn" : "good"} invert />
    </div>
  );
}

function NeedBar({
  label,
  value,
  maximum,
  tone,
  invert
}: {
  label: string;
  value: number;
  maximum: number;
  tone: "good" | "warn" | "bad";
  invert?: boolean;
}) {
  const clamped = Math.max(0, Math.min(maximum, value));
  const percent = maximum <= 0 ? 0 : (clamped / maximum) * 100;

  return (
    <div className={`vital ${tone}${invert ? " inverted" : ""}`}>
      <span>{label}</span>
      <div className="vital-track">
        <div style={{ width: `${percent}%` }} />
      </div>
      <strong>{formatNumber(value)}</strong>
    </div>
  );
}

function formatNumber(value: number) {
  return Number.isInteger(value) ? value.toString() : value.toFixed(2);
}

function traitMultiplier(value: number) {
  return 0.75 + clamp01(value) * 0.5;
}

function metabolismMultiplier(value: number) {
  return 1.2 - clamp01(value) * 0.4;
}

function learningRate(value: number) {
  return 0.1 + clamp01(value) * 0.3;
}

function riskAdjustment(value: number) {
  return (clamp01(value) - 0.5) * 0.7;
}

function clamp01(value: number) {
  return Math.max(0, Math.min(1, value));
}

function birthMutationNote(agent: AgentSnapshot, snapshot: SimulationSnapshot | null) {
  const birthEvent = snapshot?.recentEvents.find((event) => event.type === "agent.born" && event.entityId === agent.id);
  const mutated = birthEvent?.metadata["traits.mutated"];

  return mutated && mutated.length > 0 ? mutated : "none recorded";
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
  if (
    entity
    && typeof entity === "object"
    && "inventory" in entity
    && entity.inventory
    && typeof entity.inventory === "object"
    && "stacks" in entity.inventory
    && Array.isArray(entity.inventory.stacks)
    && entity.inventory.stacks.length === 0
  ) {
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

function healthValue(entity: unknown): EntityHealth | null {
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
    return entity.health as EntityHealth;
  }

  return null;
}

function isDeadAgent(entity: unknown) {
  return Boolean(entity && typeof entity === "object" && "isDead" in entity && entity.isDead === true);
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
      ...draft.resourceContainers
        .filter((container) => container.inventory.stacks.length > 0)
        .map((container) => ({
          cell: { x: Math.floor(container.position.x), y: Math.floor(container.position.y) },
          entityId: container.id,
          entityType: "ResourceContainerEntity"
        })),
      ...draft.plants
        .filter((plant) => !plant.isDecayed)
        .map((plant) => ({
          cell: { x: Math.floor(plant.position.x), y: Math.floor(plant.position.y) },
          entityId: plant.id,
          entityType: "PlantEntity"
        })),
      ...draft.waterSources.map((waterSource) => ({
        cell: { x: Math.floor(waterSource.position.x), y: Math.floor(waterSource.position.y) },
        entityId: waterSource.id,
        entityType: "WaterSourceEntity"
      })),
      ...draft.resourceDeposits
        .filter((deposit) => deposit.quantity > 0)
        .map((deposit) => ({
          cell: { x: Math.floor(deposit.position.x), y: Math.floor(deposit.position.y) },
          entityId: deposit.id,
          entityType: "ResourceDepositEntity"
        }))
    ].filter((occupant) => sameCell(occupant.cell, cell));
  }

  return snapshot?.grid.occupiedCells.filter((occupant) => sameCell(occupant.cell, cell)) ?? [];
}

const terrainTypes: TerrainType[] = ["Plain", "Road", "Forest", "Mud", "Water"];

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
    case "Water":
      return { terrainType: "Water" as const, traversalCost: 10, speedMultiplier: 0.25 };
    default:
      return { terrainType: "Plain" as const, traversalCost: 1, speedMultiplier: 1 };
  }
}

function sameCell(left: GridCell, right: GridCell) {
  return left.x === right.x && left.y === right.y;
}

function inventoryQuantity(inventory: InventoryLike) {
  return inventory.stacks.reduce((sum, stack) => sum + stack.quantity, 0);
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
