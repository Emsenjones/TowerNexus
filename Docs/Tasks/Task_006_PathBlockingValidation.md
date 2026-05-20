

# Task 006 - Path Blocking Validation

## 1. Overview

This task introduces tower placement path blocking validation for TowerNexus.

This task is designed based on:

- ProjectOverview.md
- 03_MonsterSystem.md

This document only defines the implementation scope of the current task.

## Related Tasks

This task is closely related to:

- Task_003_AStarPathfinding
- Task_004_MonsterMovement
- Task_005_DynamicPathRecalculation
- Task_007_MonsterDeathAndReward

Relationship:

- AStarPathfinding provides path validation APIs used to check whether placement blocks all valid paths.
- MonsterMovement exposes currentNode for validating paths from alive monsters to the Target Node.
- DynamicPathRecalculation updates monster paths after valid tower placement or removal changes walkability.
- MonsterDeathAndReward will later remove dead monsters from alive monster validation checks.

This task focuses only on preventing tower placement from fully blocking valid monster paths.

This task does NOT include:

- A* pathfinding implementation
- Monster spawning implementation
- Monster movement implementation
- Monster death handling
- Player damage handling
- Tower upgrade logic

These systems are implemented in other tasks.

---

# 2. Goal

Implement runtime path blocking validation for tower placement.

The system should ensure:

- Towers cannot be deployed if they fully block monster paths
- Spawn Node to Target Node must remain pathable
- Alive monsters should not be trapped by new tower placement
- Temporary validation should not corrupt runtime Node.IsWalkable state
- Path validation reuses the shared AStarPathfindingService

---

# 3. Runtime Architecture

Recommended runtime structure:

| Layer | Responsibility |
|---|---|
| TowerPlacementController | Requests placement validation before deployment |
| Map System | Provides occupied nodes and runtime walkability data |
| AStarPathfindingService | Validates whether paths still exist |
| MonsterManager | Provides alive monsters for currentNode validation |
| MonsterBehaviour | Provides currentNode and targetNode |

Path blocking validation should reuse existing map and pathfinding logic.

It should not implement a separate pathfinding algorithm.

---

# 4. Validation Rules

Tower placement is only valid when all required conditions are true:

1. All tower occupied nodes exist
2. All tower occupied nodes are currently walkable
3. Spawn Node to Target Node still has a valid path
4. Every alive monster currentNode to Target Node still has a valid path

If any condition fails:

- Tower placement must be rejected
- Runtime map walkability must remain unchanged

---

# 5. Temporary Blocked Node Simulation

Path validation should use temporary blocked node simulation.

Recommended API usage:

```csharp
HasValidPath(
    NodeBehaviour startNode,
    NodeBehaviour targetNode,
    IReadOnlyCollection<NodeBehaviour> temporaryBlockedNodes
)
```

Purpose:

- Validate tower placement before committing it
- Avoid changing Node.IsWalkable during preview validation
- Avoid rollback bugs
- Keep placement validation safe and deterministic

---

# 6. Spawn to Target Validation

Before deployment succeeds, the system must validate:

```text
Spawn Node → Target Node
```

If this path does not exist after simulating occupied nodes as blocked:

- Tower placement must fail

This ensures future spawned monsters can still reach the Target Node.

---

# 7. Alive Monster Validation

Before deployment succeeds, the system should also validate all alive monsters.

For each alive monster:

```text
monster.currentNode → Target Node
```

If any alive monster has no valid path after simulating occupied nodes as blocked:

- Tower placement must fail

This prevents players from trapping existing monsters in unreachable areas.

---

# 8. TowerPlacementController Integration

TowerPlacementController should call path blocking validation before committing tower deployment.

Recommended deployment flow:

```text
Player drags tower preview
→ Tower footprint nodes are calculated
→ Placement controller validates node occupation
→ Placement controller validates path blocking
→ If valid, commit tower placement
→ Update Node.IsWalkable
→ Trigger monster path recalculation
```

This task should integrate with existing tower placement validation flow without rewriting unrelated placement logic.

---

# 9. Runtime Walkability Commit

Runtime Node.IsWalkable should only change after all validation passes.

Recommended behavior:

```text
Validation succeeds
→ Mark occupied nodes as non-walkable
→ Finalize tower placement
→ Notify MonsterManager to recalculate paths
```

If validation fails:

```text
Validation fails
→ Do not change Node.IsWalkable
→ Keep preview in invalid state
```

---

# 10. Preview Feedback

This task may reuse the existing tower placement preview validity system.

Recommended behavior:

- If path blocking validation fails, preview should be treated as invalid
- Existing preview color feedback should continue to work
- No new UI system is required

---

# 11. MonsterManager Dependency

This task may need access to alive monsters through MonsterManager.

Recommended API:

```csharp
IReadOnlyList<MonsterBehaviour> GetAliveMonsters()
```

The validation system should not directly search the scene for monsters every time if MonsterManager already tracks them.

---

# 12. Runtime Constraints

Current constraints:

- Only supports one Target Node
- Supports one or more Spawn Nodes if Map System already exposes them
- Uses grid-based ground pathfinding only
- Does not support flying monsters yet
- Does not support partial blocking rules yet
- Does not support advanced crowd simulation

Future versions may support:

- Multi-target maps
- Flying monster exceptions
- Monster type specific path rules
- Terrain cost validation
- Boss path override rules

---

# 13. Out of Scope

The following systems are intentionally excluded from this task:

- A* implementation
- Monster spawning
- Monster movement
- Monster death flow
- Monster reward flow
- Player damage system
- Tower upgrade logic
- Tower selling logic unless already supported
- Advanced placement UI
- Special monster path rules

---

# 14. Acceptance Criteria

This task is considered complete when:

1. Tower placement validates whether Spawn Node to Target Node remains pathable.
2. Tower placement validates whether alive monsters can still reach the Target Node.
3. Validation uses temporary blocked nodes instead of directly mutating Node.IsWalkable.
4. Invalid path blocking placement is rejected.
5. Runtime Node.IsWalkable only changes after successful validation.
6. Existing tower footprint validation still works.
7. Existing placement preview feedback can represent path blocking failure.
8. Successful tower placement can trigger monster path recalculation.
9. The implementation reuses AStarPathfindingService.
10. No separate pathfinding algorithm is introduced in TowerPlacementController.