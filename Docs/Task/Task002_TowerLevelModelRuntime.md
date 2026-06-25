# Task002 - Tower Level Model Runtime

---

# 1. Goal

Make deployed towers use level-specific model prefabs from TowerDefinition per-level config data.

Newly deployed towers should spawn the model for the deployment result's resolved tower level. Accepted tower level-ups should replace only the spawned tower model and refresh the current active AttackOrigin.

---

# 2. Source Documents

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/10_TowerUpgradeSystem.md`

---

# 3. Dependencies

Depends on:

- `Task001_TowerVisualControllerFoundation.md`

This task assumes TowerBehaviour owns TowerVisualController and that model operations go through that ownership path.

---

# 4. Scope

Included:

- Use `TowerDefinition.towerLevelConfigs`.
- Use `TowerLevelConfig.towerModelPrefab`.
- Spawn the tower model for the newly deployed tower's resolved deployment level.
- Keep the first-version configuration compatible with Tower Draft results that all resolve to Lv1.
- Replace only the spawned tower model when tower level changes.
- Keep TowerBaseVisualRoot unchanged across tower levels.
- Resolve current active AttackOrigin from the spawned tower level model.
- Warn and use AttackOriginFallback when the spawned model lacks AttackOrigin.
- Update TowerRuntimeCombatSystem consumption path so combat uses current active AttackOrigin.
- Keep Drone Tower rest, launch, return, and recharge anchored to current active AttackOrigin.

Excluded:

- Placement preview ghost model workflow.
- Tower Level-Up Preview before release.
- DragCancel workflow.
- Attack range preview rendering.
- Tower upgrade draft effect previews.

---

# 5. Runtime Flow

New tower deployment:

```text
Tower Draft Deployment Result
    ↓
Resolved Tower Level
    ↓
TowerDefinition.towerLevelConfigs
    ↓
towerModelPrefab
    ↓
TowerVisualController.SetTowerVisual()
    ↓
Resolve Current Active AttackOrigin
```

Accepted tower level-up:

```text
TowerUpgradeSystem
    ↓ Accepted Level-Up
TowerBehaviour
    ↓ Request Visual Refresh
TowerVisualController.SetTowerVisual()
    ↓
Resolve Current Active AttackOrigin
```

Combat consumption:

```text
TowerVisualController
    ↓ GetCurrentAttackOrigin()
TowerRuntimeCombatSystem
    ↓ Uses active AttackOrigin for range and spawn origin
```

---

# 6. Acceptance Criteria

- New tower deployment spawns the `towerModelPrefab` for the deployment result's resolved tower level.
- Current first-version Tower Draft results that resolve to Lv1 still spawn the Lv1 model.
- TowerBaseVisualRoot is never replaced by level changes.
- Tower level-up replaces only the spawned tower model.
- Previous spawned tower model is cleaned up when replaced.
- Current active AttackOrigin is refreshed after model spawn or replacement.
- Missing model AttackOrigin logs a warning and uses AttackOriginFallback.
- TowerRuntimeCombatSystem uses current active AttackOrigin, not tower transform fallback.
- Drone Tower uses current active AttackOrigin for rest, launch, return, and recharge behavior.

---

# 7. Review Checklist

- Tower model replacement is driven through TowerVisualController.
- TowerUpgradeSystem does not directly manipulate VisualRoot or TowerPrefabSpawnPoint.
- TowerRuntimeCombatSystem does not inspect tower model hierarchy.
- Serialized tower definitions remain compatible with existing TowerLevelConfig data.
