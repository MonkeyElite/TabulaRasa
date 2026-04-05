# Milestone 3 — Agent Cognition v1

## Goal

Allow agents to perceive the world, evaluate state, and produce intentions.

## Outcome

Agents become decision-making units rather than passive state containers.

---

# Scope

This milestone introduces the first meaningful cognition layer.

The objective is not to create highly intelligent agents yet, but to define the structure that makes richer cognition possible later.

---

# Main Systems

## Agent Models

Includes:

- internal state
- needs/drives
- optional preferences/traits later

## Perception

Includes:

- local world snapshot
- visible nearby entities/resources
- interaction opportunities

## Needs

Includes:

- hunger
- energy
- other simple pressures later

## Intentions / Decisions

Includes:

- basic output contract
- selected action goal or intended next step

## Minds / Planning

Includes:

- default agent mind
- rule-based planning
- utility-style planning later

---

# Recommended Project Areas

## Agents

Recommended folders:

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

---

# Subsystems

## 1. Agent State

### Purpose
Represent the internal condition of an agent.

### Checklist
- [x] agents have internal state beyond just location
- [ ] needs are represented explicitly
- [ ] planning-relevant state is accessible in a structured way
- [ ] agent state does not rely on hidden ad hoc fields spread everywhere

## 2. Perception

### Purpose
Provide a structured, limited view of the world to the agent.

### Checklist
- [ ] agents receive a structured perception snapshot
- [ ] perception is separated from direct world mutation
- [ ] nearby/interactable information can be queried
- [ ] perception can later grow more complex without changing the engine structure

## 3. Needs / Drives

### Purpose
Create internal pressures that influence decisions.

### Checklist
- [ ] hunger exists as an explicit need
- [ ] at least one additional future need has a clear place in the structure
- [ ] need evaluation is separated from direct execution
- [ ] the system can be expanded without rewriting the whole agent model

## 4. Intentions

### Purpose
Represent what an agent wants to do, before execution.

### Checklist
- [ ] agents produce intentions instead of directly changing world state
- [ ] an intention type exists
- [ ] intention creation can be tested independently of execution
- [ ] intentions can later support targets and contextual data

## 5. Minds / Planning

### Purpose
Convert perception and internal state into intentions.

### Checklist
- [ ] at least one default mind exists
- [ ] planning can choose between multiple possible actions
- [ ] cognition logic is isolated from engine code
- [ ] planning logic can later be upgraded without rewriting unrelated systems

---

# Definition of Success

This milestone is complete when:

- [ ] agents can perceive the world in a structured way
- [ ] agents have explicit internal needs
- [ ] agents can produce intentions
- [ ] planning is not mixed into kernel/runtime code
- [ ] richer cognition can now be layered in without changing the basic architecture

---

# Notes

The most important boundary in this milestone is:

**agent chooses** != **simulation executes**

Keep that separation strong from the start.
