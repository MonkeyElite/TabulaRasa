# Milestone 8 — Life Simulation Budgets

## Goal

Build the larger simulation layers that create an actual living world.

## Outcome

The simulation expands from movement and task execution into social, economic, and environmental behavior.

---

# Scope

This milestone groups the higher-level simulation domains that depend on the lower-level architecture already being stable.

These should be added modularly rather than all at once.

---

# Major Budgets

## Social

Includes:

- relationships
- familiarity
- group behavior
- institutions later

## Daily Life

Includes:

- schedules
- routines
- needs balancing
- role-based behavior

## Economy

Includes:

- resources
- production and consumption
- inventory flow
- ownership later

## Memory / History

Includes:

- remembered events
- recent interactions
- preference or learned behavior later

## Ecology / Environment

Includes:

- environment state
- weather later
- resource regeneration/decay
- world feedback loops

---

# Recommended Project Areas

## Simulation

Recommended folders:

```text
Simulation/
├─ Budgets/
│  ├─ Social/
│  ├─ DailyLife/
│  ├─ Economy/
│  ├─ Memory/
│  └─ Ecology/
├─ Events/
└─ Systems/
```

## Agents

Recommended folders:

```text
Agents/
├─ Memory/
├─ Behaviour/
└─ Planning/
```

## World

Recommended folders:

```text
World/
├─ State/
├─ Entities/
└─ Queries/
```

---

# Subsystems

## 1. Social

### Purpose
Represent persistent relationships and interpersonal structure.

### Checklist
- [ ] a relationship model exists
- [ ] agents can track others persistently
- [ ] social state is separate from immediate per-tick perception
- [ ] future group/institution behavior has a clear subsystem home

## 2. Daily Life

### Purpose
Create broader routines and schedules beyond immediate reactive behavior.

### Checklist
- [ ] schedules or routines are representable
- [ ] task choice can take broader life structure into account
- [ ] needs no longer dominate every single tick by themselves
- [ ] role-based or routine-based behavior has a stable structure

## 3. Economy

### Purpose
Represent resource flow and production/consumption.

### Checklist
- [ ] resources and inventory flow exist
- [ ] production and consumption are explicit
- [ ] ownership or trade can be added without restructuring everything
- [ ] economy data is not hidden in unrelated systems

## 4. Memory / History

### Purpose
Allow agents and the simulation to retain meaningful past information.

### Checklist
- [ ] memory/history has a subsystem home
- [ ] remembered information is separate from raw current perception
- [ ] future behavior can depend on stored past state
- [ ] the event subsystem can feed memory/history later

## 5. Ecology / Environment

### Purpose
Add environment-driven feedback loops to the world.

### Checklist
- [ ] environment updates have dedicated systems
- [ ] world feedback loops can be modeled
- [ ] environment is not hardcoded into unrelated systems
- [ ] future weather or regeneration systems have a stable place to live

---

# Definition of Success

This milestone is complete when:

- [ ] the simulation can support broader life patterns instead of only local reactive actions
- [ ] social, economic, memory, and environment systems have explicit subsystem homes
- [ ] high-level complexity is layered on top of a stable lower-level engine/state/action/spatial foundation

---

# Notes

These budgets should be added progressively.

Do not try to implement them all at once. Each budget depends on the lower layers being stable first.
