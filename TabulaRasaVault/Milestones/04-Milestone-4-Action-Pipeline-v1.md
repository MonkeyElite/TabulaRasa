# Milestone 4 — Action Pipeline v1

## Goal

Replace direct action mutation with a buffered, explicit pipeline.

## Outcome

Actions become structured simulation objects that can later support duration, conflicts, and failure.

---

# Scope

This milestone upgrades the simulation from a direct-decision-to-mutation flow into a proper action pipeline.

This is a major long-term stability milestone because many future systems depend on actions being explicit.

---

# Main Systems

## Intent Layer

Includes:

- `AgentIntent`
- intention generation

## Action Request Layer

Includes:

- convert intentions into executable requests
- request buffering in `SimulationState`

## Validation Layer

Includes:

- target validity
- action legality
- agent availability
- reachability checks later

## Resolution Layer

Includes:

- action conflict handling
- success/failure determination
- world application timing

## Results Layer

Includes:

- `ActionResult`
- success/failure information
- failure reasons
- future event generation hooks

---

# Recommended Project Areas

## Simulation

Recommended folders:

```text
Simulation/
├─ Actions/
│  ├─ Intents/
│  ├─ Requests/
│  ├─ Validation/
│  ├─ Resolution/
│  └─ Results/
└─ Systems/
```

---

# Subsystems

## 1. Intent Collection

### Purpose
Gather what agents want to do.

### Checklist
- [ ] agent output is represented as intentions
- [ ] intentions are stored outside direct world mutation
- [ ] intentions are collected through the simulation pipeline
- [ ] intention storage has a clear runtime home

## 2. Request Creation

### Purpose
Convert intentions into executable requests.

### Checklist
- [ ] intentions are converted into action requests
- [ ] requests are buffered in `SimulationState`
- [ ] request creation is separate from world application
- [ ] requests can later include richer metadata without changing agent minds too much

## 3. Validation

### Purpose
Check whether an action can proceed.

### Checklist
- [ ] request validation exists
- [ ] invalid actions fail cleanly
- [ ] preconditions are explicit
- [ ] future reachability/resource validation has a place in the structure

## 4. Resolution

### Purpose
Determine final action outcomes and apply the correct world changes.

### Checklist
- [ ] action resolution is centralized
- [ ] world mutation happens during execution/resolution, not during decision-making
- [ ] multiple requests can eventually be handled uniformly
- [ ] future conflict handling has a natural insertion point

## 5. Results

### Purpose
Represent what happened after execution.

### Checklist
- [ ] action results are stored
- [ ] actions can report success/failure
- [ ] result data can be inspected by reporting/debug systems
- [ ] result types can later feed events, memory, or analytics

---

# Definition of Success

This milestone is complete when:

- [ ] agents no longer directly cause world mutations through their decision layer
- [ ] intentions, requests, and results are distinct concepts
- [ ] action validation has an explicit place in the runtime
- [ ] action execution is easier to extend with duration, failure, and contention later

---

# Notes

This milestone is one of the most important for long-term scalability.

Without an explicit action pipeline, future systems like jobs, reservations, pathfinding integration, and multi-agent conflict resolution become much harder to implement cleanly.
