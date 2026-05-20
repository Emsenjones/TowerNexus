# Task 001 - Monster Definition

## 1. Overview

This task introduces the foundational monster configuration structure for the TowerNexus Monster System.

The purpose of this task is to separate:

- Monster static configuration data
- Monster runtime behavior logic

This task only focuses on monster configuration assets and runtime initialization structure.

This task does NOT include:

- Monster movement
- Monster pathfinding
- Monster wave spawning
- Monster combat logic
- Monster state machine
- Monster death handling

These systems will be implemented in later tasks.

This task is designed based on:

- 00_ProjectOverview.md
- 03_MonsterSystem.md


This document only defines the implementation scope of the current task.

## Related Tasks

This task is closely related to:

- Task_002_MonsterWaveSpawner
- Task_004_MonsterMovement
- Task_007_MonsterDeathAndReward

Relationship:

- MonsterWaveSpawner will use MonsterDefinition to instantiate and initialize runtime monster instances.
- MonsterMovement will use runtime values initialized from MonsterDefinition, especially movement speed.
- MonsterDeathAndReward will use MonsterDefinition values such as maxHealth, expReward, damageToPlayer, deathAnimationName, and deathDelay.

---

# 2. Goal

Implement a data-driven monster configuration structure using ScriptableObject.

The system should support:

- Reusable monster configurations
- Multiple monster prefabs
- Runtime initialization from configuration data
- Future monster expansion

---

# 3. Runtime Architecture

The monster system should separate:

| Layer | Responsibility |
|---|---|
| MonsterDefinition | Static monster configuration data |
| MonsterBehaviour | Runtime monster logic |

MonsterBehaviour should not hardcode monster stats.

Instead:

- MonsterBehaviour receives MonsterDefinition during initialization
- Runtime values are initialized from MonsterDefinition

---

# 4. MonsterDefinition

## 4.1 Description

MonsterDefinition is a ScriptableObject.

It stores static monster configuration data.

Each monster type should have its own MonsterDefinition asset.

---

## 4.2 Required Fields

| Field | Type | Description |
|---|---|---|
| monsterId | string | Unique monster id |
| displayName | string | Monster display name |
| monsterPrefab | GameObject | Runtime monster prefab |
| moveSpeed | float | Monster movement speed |
| maxHealth | int | Monster maximum health |
| expReward | int | EXP rewarded to player after death |
| damageToPlayer | int | Damage dealt to player when reaching target |
| walkAnimationName | string | Walk animation state name |
| hitAnimationName | string | Hit animation state name |
| deathAnimationName | string | Death animation state name |
| deathDelay | float | Delay before monster object destruction |

---

# 5. MonsterBehaviour

## 5.1 Description

MonsterBehaviour is responsible for runtime monster logic.

At this stage:

- The script only stores runtime monster data
- The script initializes runtime values from MonsterDefinition
- Complex monster logic is intentionally postponed

---

## 5.2 Runtime Fields

Recommended runtime fields:

| Field | Description |
|---|---|
| definition | Current MonsterDefinition reference |
| currentHealth | Runtime monster HP |
| currentMoveSpeed | Runtime movement speed |
| animator | Monster Animator reference |

---

## 5.3 Initialization

MonsterBehaviour should support initialization through:

```csharp
Initialize(MonsterDefinition definition)
```

Initialization behavior:

1. Store MonsterDefinition reference
2. Initialize runtime values
3. Cache Animator reference
4. Prepare future runtime systems

---

# 6. Animator Rules

Monster prefabs already contain Animator components and configured animation clips.

The Monster System should:

- Reuse existing Animator setup
- Use animation state names from MonsterDefinition
- Avoid hardcoded animation state strings inside runtime logic

This task does not implement animation playback behavior yet.

---

# 7. Prefab Rules

MonsterDefinition references a runtime monster prefab.

Monster prefab requirements:

- Must contain MonsterBehaviour
- Must contain Animator
- Must support future pathfinding movement
- Must support future combat interactions

This task does not implement monster spawning yet.

---

# 8. Folder Structure

Recommended structure:

```text
Assets/
├── Scripts/
│   ├── Monster/
│   │   ├── MonsterBehaviour.cs
│   │   ├── MonsterDefinition.cs
│
├── ScriptableObjects/
│   ├── Monster/
│   │   ├── MonsterDefinition assets
```

---

# 9. Out of Scope

The following systems are intentionally excluded from this task:

- Monster movement
- A* pathfinding
- Monster spawning
- Wave system
- Monster states
- Monster death flow
- Monster reward flow
- Runtime path recalculation
- Tower interaction
- Damage system

---

# 10. Acceptance Criteria

This task is considered complete when:

1. MonsterDefinition ScriptableObject exists.
2. MonsterDefinition supports all required configuration fields.
3. MonsterBehaviour exists.
4. MonsterBehaviour supports Initialize(MonsterDefinition definition).
5. Runtime values are initialized from MonsterDefinition.
6. Monster prefab can reference MonsterDefinition correctly.
7. No monster movement or combat logic is implemented yet.
