

# Task 002 - Monster Wave Spawner

## 1. Overview

This task introduces the wave-based monster spawning system for TowerNexus.

The purpose of this task is to:

- Spawn monsters through configurable wave structures
- Support multiple monster types inside a single wave
- Instantiate runtime monster prefabs
- Initialize MonsterBehaviour using MonsterDefinition

This task focuses only on runtime spawning flow.

This task does NOT include:

- Monster movement
- Monster pathfinding
- Monster combat logic
- Monster death logic
- Dynamic path recalculation
- Tower interaction

These systems will be implemented in later tasks.

This task is designed based on:

- 00_ProjectOverview.md
- 03_MonsterSystem.md


This document only defines the implementation scope of the current task.

## Related Tasks

This task is closely related to:

- Task_001_MonsterDefinition
- Task_003_AStarPathfinding
- Task_004_MonsterMovement

Relationship:

- MonsterDefinition provides the runtime monster configuration used during spawning.
- AStarPathfinding will later provide runtime movement paths for spawned monsters.
- MonsterMovement will later consume spawned monster instances and runtime path results.

---

# 2. Goal

Implement a configurable wave spawning system.

The system should support:

- Multiple waves
- Multiple monster entries per wave
- Spawn delays
- Spawn intervals
- Runtime monster initialization

---

# 3. Runtime Architecture

Recommended runtime structure:

| Layer | Responsibility |
|---|---|
| MonsterWaveConfig | Stores wave configuration data |
| MonsterSpawnEntry | Stores single monster spawn entry data |
| MonsterSpawner | Controls runtime spawning flow |
| MonsterBehaviour | Runtime monster object |

---

# 4. MonsterWaveConfig

## 4.1 Description

MonsterWaveConfig is a ScriptableObject.

It stores all monster wave data for a battle.

Each wave contains:

- Wave delay
- Multiple monster spawn entries

---

## 4.2 Required Fields

| Field | Type | Description |
|---|---|---|
| waveId | string | Unique wave id |
| waveDelay | float | Delay before this wave starts |
| spawnEntries | List<MonsterSpawnEntry> | Monster entries inside this wave |

---

# 5. MonsterSpawnEntry

## 5.1 Description

MonsterSpawnEntry represents one monster spawning instruction.

Each entry defines:

- Which monster to spawn
- How many monsters to spawn
- Spawn interval timing

---

## 5.2 Required Fields

| Field | Type | Description |
|---|---|---|
| monsterDefinition | MonsterDefinition | Monster configuration reference |
| count | int | Spawn count |
| spawnInterval | float | Interval between spawned monsters |

---

# 6. MonsterSpawner

## 6.1 Description

MonsterSpawner is responsible for:

- Running wave spawning flow
- Spawning monsters in sequence
- Applying wave delays
- Applying spawn intervals
- Initializing MonsterBehaviour

MonsterSpawner should not contain:

- Monster movement logic
- Monster pathfinding logic
- Monster combat logic

---

## 6.2 Runtime Responsibilities

MonsterSpawner responsibilities:

1. Start battle spawning flow
2. Process waves in sequence
3. Wait for waveDelay before each wave
4. Spawn monsters from MonsterSpawnEntry
5. Wait for spawnInterval between monsters
6. Instantiate monster prefab
7. Initialize MonsterBehaviour using MonsterDefinition

---

## 6.3 Spawn Flow

Recommended flow:

```text
Battle Start
→ Start Wave 1
→ Wait waveDelay
→ Spawn Monster Entry A
→ Spawn Monster Entry B
→ Finish Wave 1
→ Start Wave 2
```

---

# 7. Monster Spawn Position

This task should support spawning monsters at Spawn Nodes.

Recommended behavior:

1. Query Spawn Node from Map System
2. Instantiate monster at Spawn Node position
3. Initialize MonsterBehaviour

This task does not implement movement toward the Target Node yet.

---

# 8. Runtime Initialization

After monster prefab instantiation:

```csharp
MonsterBehaviour.Initialize(MonsterDefinition definition)
```

Initialization should:

- Assign runtime values
- Cache Animator reference
- Prepare future movement systems

---

# 9. Folder Structure

Recommended structure:

```text
Assets/
├── Scripts/
│   ├── Monster/
│   │   ├── MonsterSpawner.cs
│   │   ├── MonsterWaveConfig.cs
│   │   ├── MonsterSpawnEntry.cs
│
├── ScriptableObjects/
│   ├── Monster/
│   │   ├── MonsterWaveConfig assets
```

---

# 10. Out of Scope

The following systems are intentionally excluded from this task:

- Monster movement
- A* pathfinding
- Monster states
- Monster death flow
- Monster combat
- Dynamic path recalculation
- Tower interaction
- Player damage system
- Runtime battle victory conditions

---

# 11. Acceptance Criteria

This task is considered complete when:

1. MonsterWaveConfig exists.
2. MonsterWaveConfig supports multiple waves.
3. MonsterSpawnEntry exists.
4. MonsterSpawnEntry supports monsterDefinition, count, and spawnInterval.
5. MonsterSpawner exists.
6. MonsterSpawner can process waves sequentially.
7. MonsterSpawner can instantiate monster prefabs.
8. Spawned monsters correctly initialize MonsterBehaviour.
9. Monsters spawn at Spawn Node positions.
10. No movement or pathfinding logic is implemented yet.