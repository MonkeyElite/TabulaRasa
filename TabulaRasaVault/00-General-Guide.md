# Tabula Rasa — General Guide

## Purpose

This document explains the overall structure and direction of **Tabula Rasa**, a modular life simulation built in .NET. It is intended to act as the high-level companion to the milestone documents.

The goal is to help keep the project:

- structurally clean
- expandable without chaos
- aligned with long-term simulation goals
- understandable while still in early development

This guide assumes the current project structure includes:

- `Abstractions`
- `Kernel`
- `World`
- `Agents`
- `Simulation`
- `Runner`

and that:

- `SimulationPhase` exists
- `SimulationState` exists
- a minimal prototype already runs

---

# 1. Core Architectural Direction

## Main separation of responsibilities

The project should be built around a few strict boundaries.

### Kernel vs Simulation

- `Kernel` runs the simulation engine
- `Simulation` defines the actual rules of the world

The kernel should not know what hunger, pathfinding, economy, or social interaction mean. It should only know how to execute systems in a defined order.

### World vs Agents

- `World` stores authoritative state
- `Agents` decide what they want to do

The world answers: **what exists right now?**

The agent layer answers: **what does this agent want to do next?**

### Intention vs Execution

Agents should not directly mutate the world.

Instead:

1. the agent forms an intention
2. the simulation turns that intention into a request
3. the simulation validates and resolves it
4. the world is updated by execution systems

### Persistent vs Transient State

Persistent state belongs in long-lived structures such as `WorldState`.

Transient state belongs in `SimulationState`, or in related runtime buffers.

Examples of transient state:

- per-tick intentions
- pending action requests
- action results
- events emitted this tick
- temporary perception buffers
- reservation buffers

### Topology vs Exact Position

The grid should be treated as:

- topology
- traversal structure
- lookup partitioning

It should **not** be treated as the only possible exact location model.

Exact standing should be represented separately using continuous world coordinates and interaction anchors.

---

# 2. Current Recommended Project Responsibilities

## Abstractions

Contains shared contracts and small primitives.

Typical content:

- `ISystem`
- action/intention contracts
- `IAgentMind`
- phase and execution contracts
- shared IDs
- shared spatial value types

### Rule
Only place a type here when more than one project must agree on it.

Do not place implementation logic here.

---

## Kernel

Contains the execution engine.

Responsibilities:

- phase execution
- system scheduling
- simulation loop
- clock/tick progression
- runtime lifecycle
- diagnostics and profiling hooks

### Rule
The kernel should stay generic.

If a class contains domain rules like hunger, eating, movement behavior, task definitions, or economy, it probably does not belong in `Kernel`.

---

## World

Contains authoritative simulation state.

Responsibilities:

- entities
- map/spatial data
- navigation structures
- interaction points
- world queries
- mutation helpers
- world/scenario construction primitives

### Rule
`World` should answer what exists and what state it is in.

It should not become the place where all simulation logic lives.

---

## Agents

Contains cognition and decision-making.

Responsibilities:

- needs/drives
- perception models
- intention generation
- planning
- memory
- behavior logic
- concrete minds

### Rule
Agents decide. They do not authoritatively execute world changes.

---

## Simulation

Contains the actual rules of the simulation.

Responsibilities:

- concrete systems
- action pipeline
- movement execution
- task/job execution
- scenario assembly
- events/reporting
- higher-level budgets

### Rule
This is where the domain-specific runtime behavior belongs.

---

## Runner

Contains executable startup logic.

Responsibilities:

- bootstrapping
- config selection
- scenario selection
- starting the engine
- output/debugging hooks

### Rule
Keep it thin.

If `Runner` begins to contain real simulation logic, move that logic elsewhere.

---

# 3. Recommended Folder Structure

## Abstractions

```text
Abstractions/
├─ Agents/
├─ Execution/
├─ Simulation/
├─ Spatial/
├─ Time/
└─ World/
```

## Kernel

```text
Kernel/
├─ Engine/
├─ Scheduling/
├─ State/
├─ Time/
├─ Lifecycle/
└─ Diagnostics/
```

## World

```text
World/
├─ State/
├─ Entities/
├─ Spatial/
│  ├─ Grid/
│  ├─ Positioning/
│  ├─ Footprints/
│  ├─ Interaction/
│  └─ Navigation/
├─ Queries/
├─ Mutations/
└─ Construction/
```

## Agents

```text
Agents/
├─ Models/
├─ Needs/
├─ Perception/
├─ Intentions/
├─ Planning/
├─ Memory/
├─ Behaviour/
└─ Minds/
```

## Simulation

```text
Simulation/
├─ Systems/
├─ Actions/
├─ Movement/
├─ Tasks/
├─ Scenarios/
├─ Budgets/
├─ Events/
├─ Reporting/
└─ Composition/
```

Suggested system substructure:

```text
Simulation/
└─ Systems/
   ├─ BeginTick/
   ├─ Environment/
   ├─ Needs/
   ├─ Perception/
   ├─ Deliberation/
   ├─ Actions/
   ├─ StateCommit/
   └─ Reporting/
```

## Runner

```text
Runner/
├─ Startup/
├─ Configuration/
├─ Commands/
└─ Output/
```

---

# 4. Structure Rules for Growth

## Rule 1 — Organize by domain responsibility

Prefer folders like:

- `Movement/Execution`
- `Actions/Validation`
- `Tasks/Reservations`
- `Spatial/Interaction`

Avoid generic buckets like:

- `Helpers`
- `Services`
- `Misc`
- `Managers`

## Rule 2 — Keep top-level folders stable

Do not invent new top-level folders casually.

A top-level folder should represent a real subsystem.

## Rule 3 — Split crowded folders by subdomain, not class shape

Prefer:

- `Actions/Requests`
- `Actions/Resolution`
- `Actions/Results`

Avoid:

- `Actions/Interfaces`
- `Actions/Classes`
- `Actions/Utils`

## Rule 4 — Keep transient runtime state out of persistent entities unless it truly belongs there

If a value only matters for one tick or one pipeline step, it probably belongs in `SimulationState`.

## Rule 5 — Do not move complexity into Runner

If something is part of the simulation, it likely belongs in `Simulation`, `World`, `Agents`, or `Kernel`.

---

# 5. Long-Term Development Strategy

The simulation should grow in layers.

The recommended progression is:

1. stable engine foundation
2. authoritative world state
3. agent cognition
4. explicit action pipeline
5. spatial model v2
6. navigation and locomotion
7. task/job execution
8. higher-level life simulation budgets

This progression reduces rewrites and keeps later complexity grounded in stable foundations.

---

# 6. Immediate Recommended Focus

Given the current project state, the most valuable near-term focus is:

## Priority 1 — Spatial foundation

- `GridCell`
- `WorldPosition`
- `InteractionPoint`

## Priority 2 — Action pipeline

- intentions
- action requests
- validation
- results

## Priority 3 — Navigation-ready world structure

- map bounds
- traversability
- adjacency
- spatial queries

These three areas create the base for:

- believable movement
- off-grid standing
- pathfinding
- task execution
- future resource/task reservation

---

# 7. Near-Term Definition of Success

Tabula Rasa is in a strong next-stage position when the following are all true:

- [ ] systems run in a clear phase-driven pipeline
- [ ] `SimulationState` is the root runtime state object
- [ ] world state is authoritative and structured
- [ ] agents produce intentions instead of directly mutating world state
- [ ] actions flow through a buffered pipeline
- [ ] positions support both `GridCell` and exact `WorldPosition`
- [ ] entities can expose off-grid interaction points
- [ ] the structure clearly supports future pathfinding and tasks

---

# 8. Notes

## About spatial realism

The grid should not be treated as the final reality model.

It should be treated as:

- a topology model
- a traversal model
- a lookup partitioning structure

Exact standing should be represented separately using continuous coordinates and interaction anchors.

## About complexity growth

Each higher-level system should be added only after its supporting lower-level systems are stable.

Avoid adding large feature systems before the state model, action pipeline, and spatial foundation are ready.

## About future modularization

Do not split into more projects too early.

Prefer folders and namespaces inside existing projects until a subsystem becomes large enough to justify a new project boundary.
