# Milestone 6 — Navigation and Movement

## Goal

Give agents believable movement through the world.

## Outcome

Agents can navigate using the grid/topology while moving toward exact destinations.

---

# Scope

This milestone builds on the spatial model by adding route planning and locomotion.

The goal is to move from symbolic or instant repositioning toward movement that unfolds through time.

---

# Main Systems

## Navigation

Includes:

- pathfinding
- route planning
- destination selection support
- reachability checks

## Locomotion

Includes:

- movement progress
- movement toward exact positions
- arrival tolerance
- current destination state

## Route Execution

Includes:

- follow path over time
- step from cell to cell
- later blend with continuous local movement

## Failure Handling

Includes:

- stuck detection
- blocked target handling
- route invalidation

---

# Recommended Project Areas

## World

Recommended folders:

```text
World/
└─ Spatial/
   └─ Navigation/
      ├─ Grid/
      ├─ Costs/
      └─ Reachability/
```

## Simulation

Recommended folders:

```text
Simulation/
├─ Movement/
│  ├─ Planning/
│  ├─ Execution/
│  ├─ Pathing/
│  └─ Steering/
└─ Systems/
```

---

# Subsystems

## 1. Pathfinding

### Purpose
Find routes through the world topology.

### Checklist
- [ ] a pathfinding algorithm exists
- [ ] pathfinding input/output types exist
- [ ] agents can request a route to a target
- [ ] unreachable destinations fail cleanly
- [ ] pathfinding logic is testable independently

## 2. Route Planning

### Purpose
Turn a destination into a route the movement layer can follow.

### Checklist
- [ ] routes are represented explicitly
- [ ] route planning uses topology data instead of ad hoc agent logic
- [ ] routes can later be cached or reused if useful
- [ ] route planning can target exact stand positions via interaction anchors

## 3. Locomotion

### Purpose
Move agents over time toward precise positions.

### Checklist
- [ ] movement is represented as progress over time
- [ ] agents can move toward exact positions
- [ ] arrival can be checked using tolerance/radius logic
- [ ] movement logic is separate from high-level planning

## 4. Route Execution

### Purpose
Advance an agent along a planned route over multiple ticks.

### Checklist
- [ ] routes can be followed over multiple ticks
- [ ] movement does not require teleportation
- [ ] world position updates happen cleanly
- [ ] route-following has a clear subsystem home

## 5. Failure Handling

### Purpose
Handle the cases where movement fails or becomes invalid.

### Checklist
- [ ] movement failure can be reported
- [ ] route invalidation has a defined place
- [ ] future replanning hooks are possible
- [ ] blocked/unreachable conditions do not require hacks in unrelated systems

---

# Definition of Success

This milestone is complete when:

- [ ] agents can travel through the world over time
- [ ] pathfinding is separated from cognition and world mutation
- [ ] movement uses exact destinations rather than only tile centers
- [ ] the engine can support route failure and future replanning

---

# Notes

A good medium-term target is a hybrid system:

- topology and pathfinding over a grid/nav structure
- exact movement in continuous world coordinates

That gives you believable movement without forcing full freeform geometry too early.
