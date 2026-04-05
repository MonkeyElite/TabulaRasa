# Milestone 1 — Stable Engine Foundation

## Goal

Establish a reliable simulation runtime structure that can scale cleanly.

## Outcome

A phase-driven kernel that executes systems through a stable `SimulationState`.

---

# Scope

This milestone focuses on the runtime spine of the simulation.

It does **not** aim to make the simulation deep or realistic yet. Its purpose is to make sure the engine can execute future complexity without becoming unstable or disorganized.

---

# Main Systems

## Kernel Execution

Includes:

- simulation engine
- per-tick execution loop
- system scheduler
- phase ordering
- priority ordering within phases

## Runtime State

Includes:

- `SimulationState`
- simulation clock/tick state
- runtime flags
- per-tick transient buffers

## Diagnostics

Includes:

- system execution logging
- tick summaries
- basic profiling hooks
- system order inspection

---

# Recommended Project Areas

## Kernel

Recommended folders:

```text
Kernel/
├─ Engine/
├─ Scheduling/
├─ State/
├─ Time/
├─ Lifecycle/
└─ Diagnostics/
```

## Abstractions

Recommended folders:

```text
Abstractions/
├─ Execution/
├─ Simulation/
└─ Time/
```

---

# Subsystems

## 1. Engine Loop

### Purpose
Run the simulation one tick at a time and execute systems in the correct order.

### Checklist
- [x] `SimulationEngine` accepts a root `SimulationState`
- [x] engine can run for a configured number of ticks
- [x] engine can execute systems across multiple phases
- [x] system execution order is deterministic
- [x] engine can safely complete a tick even when no systems exist in some phases

## 2. Scheduling

### Purpose
Define execution order in a way that stays readable as the simulation grows.

### Checklist
- [x] systems are grouped by `SimulationPhase`
- [x] systems support priority ordering within a phase
- [x] the scheduler produces a stable execution plan
- [x] execution order can be inspected/debugged
- [x] the engine does not rely on hardcoded system calls in `Runner`

## 3. Runtime State

### Purpose
Provide one root runtime object that contains the active simulation state.

### Checklist
- [x] `SimulationState` contains `World`
- [x] `SimulationState` contains time/clock state
- [x] `SimulationState` contains transient per-tick buffers
- [x] `SimulationState` is treated as the runtime root object
- [x] transient per-tick data can be cleared/reset at the right time

## 4. Lifecycle / Control

### Purpose
Support basic runtime control over the simulation.

### Checklist
- [ ] the simulation can be started cleanly
- [ ] the simulation can stop cleanly
- [ ] pause support exists if needed
- [ ] lifecycle transitions are explicit rather than hidden in unrelated code

## 5. Diagnostics

### Purpose
Make it possible to inspect and debug runtime behavior.

### Checklist
- [x] current tick can be logged
- [ ] current phase can be logged
- [ ] current system name can be logged
- [ ] system execution order can be printed or inspected
- [ ] a simple timing/profiling hook exists or has a defined place in the structure

---

# Definition of Success

This milestone is complete when:

- [x] the engine is clearly phase-driven
- [x] `SimulationState` is the runtime root
- [ ] scheduling is deterministic and inspectable
- [ ] logging/debugging can show how a tick is executed
- [x] adding a new system no longer requires awkward manual wiring in multiple places

---

# Notes

This milestone is about **stability and clarity**, not feature depth.

A simple simulation with a very strong runtime structure is more valuable than a feature-rich simulation built on a weak engine.
