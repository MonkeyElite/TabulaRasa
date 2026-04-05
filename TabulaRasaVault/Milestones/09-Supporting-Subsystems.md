# Supporting Subsystems

## Purpose

This document lists supporting systems that enable larger systems.

These subsystems are often the hidden dependencies that determine whether higher-level features can be implemented cleanly.

---

# 1. Pathfinding Subsystem

## Purpose
Provides route planning over the world topology.

## Depends on

- grid/world topology
- traversability data
- destination selection
- basic spatial queries

## Required before

- believable movement
- task execution at a distance
- job logistics
- realistic scheduling

## Checklist
- [ ] navigation representation exists
- [ ] pathfinding input/output types exist
- [ ] route planning is testable independently
- [ ] routes can be cached or reused later if needed

---

# 2. Reservation Subsystem

## Purpose
Prevents multiple agents from using the same target, point, or resource incorrectly.

## Depends on

- interaction points
- action/task pipeline
- target/resource identifiers

## Required before

- believable job systems
- contested tasks
- multi-agent resource use
- exact standing at shared targets

## Checklist
- [ ] interaction points can be reserved
- [ ] resources can be reserved
- [ ] reservations can expire or release
- [ ] failed reservations are handled cleanly

---

# 3. Perception Subsystem

## Purpose
Provides structured world input to agents.

## Depends on

- world queries
- spatial queries
- locality/visibility rules

## Required before

- planning
- social awareness
- environment reaction
- memory formation

## Checklist
- [ ] agents receive structured perception
- [ ] perception is localized
- [ ] perception can evolve independently of planning logic
- [ ] perception does not directly mutate authoritative world state

---

# 4. Event Subsystem

## Purpose
Provides simulation events for reporting, memory, analytics, and later reactive behavior.

## Depends on

- action results
- task completion/failure
- environment updates

## Required before

- memory/history
- analytics/debugging
- event-driven reactions later

## Checklist
- [ ] simulation events have a defined representation
- [ ] events can be emitted during systems
- [ ] events can be stored per tick
- [ ] future replay/logging hooks are possible

---

# 5. Inventory / Resource Subsystem

## Purpose
Tracks resources on agents, entities, and in the world.

## Depends on

- world entity structure
- task/action pipeline
- mutation paths

## Required before

- economy
- jobs
- crafting/production
- ownership

## Checklist
- [ ] resources/inventory are modeled explicitly
- [ ] transfer between holders is possible
- [ ] consumption/production is explicit
- [ ] the world does not fake inventory with ad hoc flags

---

# 6. Diagnostics / Observability Subsystem

## Purpose
Makes it possible to inspect behavior and understand why the simulation is doing what it is doing.

## Depends on

- kernel execution visibility
- action results
- events
- reporting hooks

## Required before

- meaningful debugging
- performance profiling
- replay/logging later

## Checklist
- [ ] current tick and phase can be inspected
- [ ] system execution order can be inspected
- [ ] actions/results can be reported
- [ ] the subsystem has a stable place in the structure
