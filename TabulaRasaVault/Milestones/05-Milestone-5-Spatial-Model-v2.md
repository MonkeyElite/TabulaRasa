# Milestone 5 — Spatial Model v2

## Goal

Move from symbolic position or tile-center-only logic to a scalable spatial model.

## Outcome

The simulation can use grid-based topology while still supporting off-grid exact standing and interaction points.

---

# Scope

This milestone introduces the spatial foundation needed for believable interaction and future movement.

The grid is treated as topology and traversal structure, while exact position is represented separately.

---

# Main Systems

## Grid Topology

Includes:

- `GridCell`
- dimensions/bounds
- traversability
- adjacency
- regions/chunks later

## Exact World Position

Includes:

- `WorldPosition`
- continuous x/y coordinates
- derived current cell

## Footprints

Includes:

- entity size
- occupied area
- broad-phase occupancy later

## Interaction Anchors

Includes:

- one or more usable points for interacting with an entity
- reservation capability later

## Spatial Queries

Includes:

- nearby entities by radius/cell
- current cell from world position
- exact interaction point lookup

---

# Recommended Project Areas

## World

Recommended folders:

```text
World/
├─ Spatial/
│  ├─ Grid/
│  ├─ Positioning/
│  ├─ Footprints/
│  ├─ Interaction/
│  └─ Navigation/
├─ Entities/
└─ Queries/
```

## Abstractions

Recommended folders:

```text
Abstractions/
└─ Spatial/
```

---

# Subsystems

## 1. Grid Topology

### Purpose
Represent the navigable/topological structure of the world.

### Checklist
- [ ] string positions are removed
- [ ] `GridCell` exists
- [ ] world/map dimensions exist
- [ ] cell adjacency can be queried
- [ ] traversability can be queried

## 2. Exact Position

### Purpose
Allow agents and entities to exist at precise coordinates rather than only tile centers.

### Checklist
- [ ] `WorldPosition` exists
- [ ] agents/entities can hold exact position
- [ ] current cell can be derived from exact position
- [ ] the world no longer assumes everything is tile-centered

## 3. Footprints

### Purpose
Represent the area an entity occupies or influences.

### Checklist
- [ ] entities can define occupied size or footprint at a simple level
- [ ] future collision/occupancy systems have a place in the architecture
- [ ] footprint logic is not mixed into unrelated movement or planning code

## 4. Interaction Points

### Purpose
Support off-grid standing and precise task execution.

### Checklist
- [ ] `InteractionPoint` or equivalent exists
- [ ] entities can expose specific stand/use locations
- [ ] agents are not limited to tile-center interaction targets
- [ ] interaction points can later be reserved

## 5. Spatial Queries

### Purpose
Provide reusable spatial lookup functions.

### Checklist
- [ ] spatial lookups exist for nearby cells/entities
- [ ] current cell and exact position relationships can be queried
- [ ] systems do not reimplement spatial logic everywhere
- [ ] spatial query code has a stable home in the structure

---

# Definition of Success

This milestone is complete when:

- [ ] the world supports both topological grid logic and exact position logic
- [ ] agents can stand off-center/off-grid relative to tile centers
- [ ] entities can expose exact interaction anchors
- [ ] the structure clearly supports future pathfinding and movement systems

---

# Notes

This milestone directly addresses the need for agents to stand at precise positions near partially occupied tiles or half-tile entities.

The grid should bring agents into the correct area. Exact coordinates and interaction anchors should determine where they finally stand.
