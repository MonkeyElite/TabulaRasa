# Milestone 2 — World State v1

## Goal

Create a stable world model that becomes the authoritative source of state.

## Outcome

A world with entities, IDs, map data, basic spatial data, and query/mutation boundaries.

---

# Scope

This milestone establishes the first real version of the simulated world.

It should answer:

- what exists?
- where is it?
- what state is it in?
- how is that state queried?
- how is that state changed?

---

# Main Systems

## World State

Includes:

- root `WorldState`
- entity identifiers
- initial entity collections
- resource/state containers

## World Queries

Includes:

- find entities by id
- find nearby entities
- find interactable entities
- query map/cell data

## World Mutations

Includes:

- controlled changes to positions
- controlled changes to resources/entities
- later reservation/occupancy mutation hooks

## Construction

Includes:

- world builder
- scenario builder
- map initialization

---

# Recommended Project Areas

## World

Recommended folders:

```text
World/
├─ State/
├─ Entities/
├─ Spatial/
├─ Queries/
├─ Mutations/
└─ Construction/
```

---

# Subsystems

## 1. Root World State

### Purpose
Provide one authoritative container for the persistent state of the world.

### Checklist
- [x] `WorldState` is defined and stable
- [x] all major persistent simulation state has a home under the world model
- [x] world state is not scattered across unrelated services
- [x] `WorldState` can be passed through `SimulationState`

## 2. Entity Identity

### Purpose
Ensure entities can be found, referenced, and mutated consistently.

### Checklist
- [x] entities have clear identifiers
- [x] identifiers are stable and not dependent on collection position
- [x] entities can be looked up reliably by id
- [x] future references from actions/tasks can target entities by identifier

## 3. Queries

### Purpose
Provide reusable read access to world data.

### Checklist
- [x] world queries exist for common read access
- [x] systems do not duplicate the same lookup logic everywhere
- [x] query code is separated from mutation code where practical
- [x] spatial and entity queries have a clear home in the structure

## 4. Mutations

### Purpose
Create explicit ways to change world state.

### Checklist
- [ ] common updates have defined mutation paths
- [ ] systems do not directly mutate random internals everywhere
- [ ] future reservation or occupancy logic has a place in the structure
- [ ] mutation code stays close to the state it changes

## 5. Construction

### Purpose
Provide a stable way to create test worlds and scenarios.

### Checklist
- [x] scenario/world setup is not hardcoded in `Runner`
- [x] there is a world/scenario construction path in `World` or `Simulation`
- [x] test scenarios can be created without duplicating setup logic
- [x] map/world creation has a clear subsystem home

---

# Definition of Success

This milestone is complete when:

- [x] `WorldState` is clearly the authoritative persistent state container
- [ ] world queries and mutations have stable homes
- [x] entity identity is reliable
- [x] new world features can be added without dumping code into random systems
- [x] scenarios can be built without bloating `Runner`

---

# Notes

This milestone is foundational for everything else.

If the world state is unclear, then perception, pathfinding, tasks, economy, and social systems will all become much harder to implement cleanly.
