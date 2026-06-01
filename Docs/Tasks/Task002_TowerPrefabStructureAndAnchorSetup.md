

# Task 002: Tower Prefab Structure And Anchor Setup

## 1. Task Overview

This task reviews and aligns the existing tower prefab structure and anchor setup with the current Tower Framework System documentation.

Most of this functionality is expected to already exist in the project.

The main goal of this task is calibration, not large-scale implementation.

Codex should inspect the current code and prefab-related structures first, then identify whether any small adjustments are needed.

This task should avoid unnecessary refactors.

---

## 2. Related System Documents

Please review the following system documents before implementation:

- Docs/00_ProjectOverview.md
- Docs/07_TowerFrameworkSystem.md
- Docs/02_TowerDeploySystem.md
- Docs/08_TowerRuntimeCombatSystem.md

The main source of truth for this task is:

```text
Docs/07_TowerFrameworkSystem.md
```

This task should also stay compatible with the existing Tower Placement / Deploy implementation.

---

## 3. Implementation Goal

Verify that tower prefabs have a consistent structure that can support:

- Tower placement
- Occupied node detection
- Center anchor snapping
- Future runtime combat behavior
- Future projectile spawn position usage

If the existing implementation already supports these requirements, no code changes are required.

If small gaps are found, Codex should propose minimal, targeted changes.

---

## 4. Expected Existing Concepts

The project is expected to already contain most or all of the following concepts:

- Tower prefab reference
- Tower anchor setup
- Center anchor
- Occupied anchors
- Tower placement preview structure
- Placement validation using occupied nodes
- Walkability update after successful placement

Codex should identify the exact current class and field names used by the project.

Do not create duplicate concepts if equivalent ones already exist.

---

## 5. Tower Prefab Structure Requirements

A tower prefab should be able to provide enough structure for placement and future combat.

Recommended conceptual structure:

```text
TowerPrefab
    ├── VisualRoot
    ├── AnchorRoot
    │   ├── CenterAnchor
    │   └── OccupiedAnchors
    ├── PreviewRoot / PreviewRenderers
    └── CombatRoot / AttackOrigin / ProjectileSpawnPoint
```

This structure is conceptual.

Codex should not force this exact hierarchy if the current project already has a working equivalent.

The goal is functional consistency, not hierarchy purity.

---

## 6. Anchor Setup Requirements

### 6.1 Center Anchor

The Center Anchor represents the point that snaps to the selected map node during placement.

Expected usage:

```text
Selected Node Position
    ↓
Tower Center Anchor
    ↓
Tower Placement Position
```

The Center Anchor should be clearly assigned and stable.

---

### 6.2 Occupied Anchors

Occupied Anchors represent all map nodes occupied by the tower after placement.

Expected usage:

```text
Occupied Anchors
    ↓
Find Corresponding Map Nodes
    ↓
Validate Walkability
    ↓
Mark Nodes As Not Walkable After Placement
```

Occupied Anchors are used by Tower Placement System.

They should not contain runtime combat logic.

---

## 7. Preview Structure Requirements

Tower placement preview should remain compatible with the existing placement system.

Codex should verify:

- Preview renderers can be collected or assigned correctly
- Preview color/state can be changed by placement validation
- Preview colliders do not interfere with placement raycasts
- Preview structure does not break occupied anchor detection

If this is already working, do not refactor it.

---

## 8. Future Combat Support Requirements

This task should not implement Tower Runtime Combat System.

However, the tower prefab structure should leave clean attachment points for future combat behavior.

Recommended future references:

| Concept | Purpose |
|---|---|
| AttackOrigin | The point used as the origin of attacks or beams |
| ProjectileSpawnPoint | The point where projectile objects are spawned |
| CombatRoot | Optional parent for combat-related transforms |

If these references already exist, Codex should verify them.

If they do not exist, Codex may recommend adding them only if the current prefab structure clearly needs them for upcoming combat tasks.

Do not implement attack logic in this task.

---

## 9. Constraints

Do not implement:

- TowerCombatBehaviour
- Attack cooldowns
- Enemy detection
- Target selection
- Projectile movement
- Buff or effect logic
- Monster damage logic
- Tower upgrade logic

Do not perform large prefab hierarchy refactors unless absolutely necessary.

Do not rename existing working fields or classes without a strong reason.

This task should preserve existing placement behavior.

---

## 10. Expected Files To Review

Before proposing any changes, inspect the current project structure.

Likely areas to inspect:

```text
Assets/Scripts
Assets/Scripts/Tower
Assets/Scripts/Placement
Assets/Scripts/Map
Assets/Prefabs/Towers
Assets/Prefabs/UI
```

Likely existing scripts may include concepts similar to:

```text
TowerPlacementController
TowerPlacementPreview
TowerDefinition
TowerAnchorSet
MapGeneratorBehaviour
NodeBehaviour
PendingTowerItemUI
BattleHUDUI
```

The exact names in the project are the source of truth.

---

## 11. Expected Output

Codex should provide one of the following outcomes:

### Outcome A: No Code Changes Needed

If the current implementation already satisfies the requirements, Codex should report:

- Which existing scripts/classes support the structure
- Which prefab fields are already present
- Why no changes are needed
- How the current setup supports future combat implementation

### Outcome B: Minimal Adjustments Needed

If small issues are found, Codex should propose minimal changes only.

Examples:

- Add a missing serialized reference
- Add a null validation warning
- Add a helper accessor
- Add an optional AttackOrigin or ProjectileSpawnPoint reference
- Improve inspector readability

Large refactors should be avoided.

---

## 12. Verification Checklist

Codex should explain how to verify the result.

Minimum verification:

- Unity compiles without errors
- Existing tower placement still works
- Tower preview still follows the cursor
- Preview valid/invalid state still works
- Tower snaps to the correct node using center anchor
- Occupied anchors still map to the expected nodes
- Successful placement still updates node walkability
- Existing path-blocking validation still works
- Existing prefabs do not lose serialized references
- Any newly added optional combat transform references are assigned or safely nullable

---

## 13. Implementation Plan Requirement

Before writing code, Codex must inspect the current project and provide an implementation plan.

The implementation plan must include:

1. Existing scripts/classes related to tower prefab structure and anchor setup
2. Existing prefab hierarchy assumptions found in code
3. Whether Center Anchor and Occupied Anchors are already implemented correctly
4. Whether preview structure is already implemented correctly
5. Whether future combat transform references are already available or should be added later
6. Files expected to be modified, if any
7. Risks of changing current placement behavior
8. How the result will be verified in Unity

Do not implement until the plan is reviewed and approved.

---

## 14. Notes

This task is primarily a review and calibration task.

The project already has working tower placement and anchor-related behavior.

The safest result may be no code changes.

The purpose of this task is to ensure the existing tower prefab structure is aligned with the latest Tower Framework System and ready for upcoming runtime combat implementation.