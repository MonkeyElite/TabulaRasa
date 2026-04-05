# Milestone 7 — Tasks and Jobs

## Goal

Allow agents to perform structured work rather than only atomic actions.

## Outcome

The simulation can represent multi-step activities with progress, reservations, and completion/failure.

---

# Scope

This milestone introduces structured work.

Instead of only performing isolated actions like moving or consuming, agents can now participate in jobs made up of multiple task steps.

---

# Main Systems

## Task Definitions

Includes:

- atomic tasks
- preconditions
- outputs

## Job Composition

Includes:

- multi-step jobs
- subtasks
- dependencies

## Assignment

Includes:

- choose who performs a task
- reserve task or target

## Execution

Includes:

- progress over time
- interruption/cancellation
- completion/failure

## Reservations

Includes:

- reserve interaction points
- reserve resources
- reserve jobs/tasks

---

# Recommended Project Areas

## Simulation

Recommended folders:

```text
Simulation/
├─ Tasks/
│  ├─ Definitions/
│  ├─ Assignment/
│  ├─ Execution/
│  ├─ Reservations/
│  └─ Progress/
└─ Systems/
```

---

# Subsystems

## 1. Task Definitions

### Purpose
Represent the smallest useful units of work.

### Checklist
- [ ] tasks are represented explicitly
- [ ] tasks have preconditions
- [ ] tasks can specify required targets/resources
- [ ] task definitions are not mixed into unrelated systems

## 2. Job Composition

### Purpose
Combine multiple tasks into larger work structures.

### Checklist
- [ ] jobs can contain multiple steps or tasks
- [ ] complex work can be decomposed
- [ ] dependency order between subtasks can be represented
- [ ] jobs can later carry contextual or priority data

## 3. Assignment

### Purpose
Assign work to agents in a structured way.

### Checklist
- [ ] tasks can be assigned to agents
- [ ] task ownership or reservation is possible
- [ ] task contention can be managed
- [ ] assignment logic has a stable subsystem home

## 4. Execution

### Purpose
Track work progress and final outcomes.

### Checklist
- [ ] task progress over time exists
- [ ] completion and failure are represented
- [ ] interruptions can be modeled
- [ ] the simulation can update work state without hacks across multiple projects

## 5. Reservations

### Purpose
Prevent multiple agents from conflicting over the same work target or resource.

### Checklist
- [ ] interaction points can be reserved
- [ ] resources can be reserved
- [ ] reservation logic has a clear subsystem location
- [ ] reservations can later expire or release cleanly

---

# Definition of Success

This milestone is complete when:

- [ ] agents can perform structured work over multiple ticks
- [ ] jobs can be decomposed into smaller tasks
- [ ] reservations prevent obviously broken contention behavior
- [ ] completion, interruption, and failure are explicit concepts

---

# Notes

This milestone is where the simulation starts feeling much more like a living world rather than a collection of isolated action selections.
