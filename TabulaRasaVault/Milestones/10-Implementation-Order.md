# Implementation Order

## Purpose

This document provides the recommended implementation order from the current project state.

It is intended to reduce confusion when deciding what to build next.

---

# Immediate Next Steps

- [ ] stabilize `SimulationState` usage across the engine
- [ ] use phase + priority consistently for systems
- [ ] define transient buffers inside `SimulationState`
- [ ] replace string position with `GridCell`
- [ ] add `WorldPosition`
- [ ] add `InteractionPoint`
- [ ] move from direct pending decisions toward an intent/request/result pipeline

---

# Short-Term Order

- [ ] Milestone 2 — World State v1
- [ ] Milestone 3 — Agent Cognition v1
- [ ] Milestone 4 — Action Pipeline v1
- [ ] Milestone 5 — Spatial Model v2

---

# Medium-Term Order

- [ ] Milestone 6 — Navigation and Movement
- [ ] Milestone 7 — Tasks and Jobs

---

# Long-Term Order

- [ ] Milestone 8 — Life Simulation Budgets

---

# Recommended Sequence Rationale

## 1. World before deeper agents

The agent layer becomes much easier to grow once the world state is authoritative and stable.

## 2. Action pipeline before jobs
n
Jobs and multi-step tasks depend on actions being explicit and buffered.

## 3. Spatial model before believable movement

Pathfinding and exact interaction positions depend on the world having both topology and exact coordinates.

## 4. Navigation before structured work at a distance

Task systems become much more meaningful when agents can actually navigate through the environment.

## 5. Higher-level budgets last

Social, economic, memory, and environment systems depend on the lower layers being stable first.
