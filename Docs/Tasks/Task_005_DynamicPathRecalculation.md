

# Task 005 - Dynamic Path Recalculation

## 1. Overview

This task introduces runtime dynamic path recalculation for TowerNexus.

This task is designed based on:

- 00_ProjectOverview.md
- 03_MonsterSystem.md

This document only defines the implementation scope of the current task.

## Related Tasks

This task is closely related to:

- Task_003_AStarPathfinding
- Task_004_MonsterMovement
- Task_006_PathBlockingValidation
- Task_007_MonsterDeathAndReward

Relationship:

- AStarPathfinding provides runtime path calculation and validation APIs.
- MonsterMovement exposes currentNode and supports runtime path reassignment.
- PathBlockingValidation modifies battlefield walkability and triggers path updates.
- MonsterDeathAndReward will later remove dead monsters from runtime repathing flow.

This task focuses only on recalculating monster movement paths when runtime map walkability changes.

This task does NOT include:

- A* pathfinding implementation
- Monster spawning implementation
- Tower placement validation implementation
- Monster death handling
- Player damage handling
- Runtime battle result logic

These systems are implemented in other tasks.

---

# 2. Goal

Implement runtime dynamic monster path recalculation.

The system should support:

- Detecting battlefield walkability changes
- Recalculating paths for alive monsters
- Reassigning movement paths during runtime
- Preserving monster runtime positions during repathing
- Supporting future dynamic battlefield systems

---

# 3. Runtime Architecture

Recommended runtime structure:

| Layer | Responsibility |
|---|---|
| Map System | Provides runtime walkability state |
| AStarPathfindingService | Calculates updated paths |
| MonsterManager | Coordinates repathing flow |
| MonsterBehaviour | Receives updated movement paths |
| Tower Deployment System | Triggers runtime walkability changes |

Dynamic repathing should remain independent from monster spawning and combat systems.

---

# 4. Runtime Repathing Rules

Current runtime rules:

- Monsters should not continue following outdated blocked paths
- Monsters should recalculate paths from currentNode
- Monsters should keep the same targetNode
- Monsters should preserve runtime movement state when possible
- Repathing should not teleport monsters
- Repathing should support multiple alive monsters simultaneously

---

# 5. Runtime Repathing Trigger

Dynamic repathing should happen when:

- Tower placement changes Node.IsWalkable
- Tower removal changes Node.IsWalkable
- Future runtime obstacle systems modify walkability

Recommended runtime flow:

```text
Battlefield walkability changes
→ Notify MonsterManager
→ MonsterManager iterates alive monsters
→ Request updated path
→ Assign updated path to monster
```

---

# 6. MonsterManager

## 6.1 Description

MonsterManager is responsible for:

- Tracking alive monsters
- Coordinating runtime repathing
- Broadcasting path recalculation requests
- Managing future monster runtime events

MonsterManager should not contain:

- A* implementation
- Movement logic
- Tower placement logic
- Combat logic

---

## 6.2 Runtime Responsibilities

MonsterManager responsibilities:

1. Register spawned monsters
2. Unregister removed monsters
3. Track alive monsters
4. Request runtime path recalculation
5. Coordinate repathing flow

---

# 7. Required Runtime APIs

Recommended APIs:

```csharp
void RegisterMonster(MonsterBehaviour monster)
```

```csharp
void UnregisterMonster(MonsterBehaviour monster)
```

```csharp
void RecalculateAllMonsterPaths()
```

Used when runtime walkability changes.

```csharp
void RequestPathRecalculation(MonsterBehaviour monster)
```

Used to update an individual monster path.

---

# 8. Runtime Path Recalculation Flow

Recommended runtime flow:

```text
MonsterManager receives repathing request
→ Get monster currentNode
→ Get monster targetNode
→ Request updated path from AStarPathfindingService
→ Validate returned path
→ Assign updated path to monster
```

---

# 9. Current Node Dependency

Dynamic repathing depends on MonsterBehaviour currentNode tracking.

MonsterBehaviour must:

- Expose currentNode
- Update currentNode while moving
- Preserve current runtime position during path replacement

Repathing should always start from:

```text
currentNode
```

not:

```text
Spawn Node
```

---

# 10. Runtime Path Replacement

When a new path is assigned:

- Old path should be replaced safely
- pathIndex should reset correctly
- Monster should continue movement smoothly
- Monster should not restart from the beginning of the old path

Recommended runtime behavior:

```text
Old Path
→ Replace with New Path
→ Continue movement from currentNode
```

---

# 11. Invalid Path Handling

If runtime path recalculation fails:

- Monster should stop movement safely
- Runtime should avoid null exceptions
- Future recovery behavior can be implemented later

This task does not define advanced fallback logic yet.

---

# 12. Tower Deployment Integration

TowerDeploymentSystem should later trigger runtime repathing.

Recommended future integration:

```text
Tower deployed or removed
→ Map walkability updated
→ MonsterManager.RecalculateAllMonsterPaths()
```

This task only prepares the runtime repathing infrastructure.

Actual deployment integration is handled later.

---

# 13. Runtime Constraints

Current constraints:

- Only supports ground monsters
- Only supports grid-based movement
- Does not support crowd avoidance
- Does not support predictive repathing
- Does not support path smoothing
- Does not support asynchronous pathfinding

Future versions may support:

- Smart crowd systems
- Path smoothing
- Local avoidance
- Partial path recovery
- Async pathfinding
- High-performance repathing batching

---

# 14. Folder Structure

Recommended structure:

```text
Assets/
├── Scripts/
│   ├── Monster/
│   │   ├── MonsterManager.cs
```

---

# 15. Out of Scope

The following systems are intentionally excluded from this task:

- A* implementation
- Monster spawning
- Tower placement validation
- Monster death flow
- Monster reward flow
- Player damage system
- Crowd control effects
- Runtime battle result handling
- Advanced AI behavior

---

# 16. Acceptance Criteria

This task is considered complete when:

1. MonsterManager exists.
2. MonsterManager can track alive monsters.
3. MonsterManager can request runtime path recalculation.
4. Monsters can receive updated runtime paths.
5. Repathing starts from currentNode.
6. Monsters preserve runtime movement continuity during repathing.
7. Invalid paths are handled safely.
8. Runtime repathing remains independent from spawning and combat systems.
9. Tower deployment systems can later trigger runtime repathing.
10. No combat logic, death logic, or advanced AI behavior is implemented in this task.