# Task001 - Tower Visual Controller Foundation

---

# 1. Goal

Establish the tower-owned visual foundation required by tower level visuals, placement preview, level-up preview, and future attack range preview.

This task introduces the runtime ownership boundary:

```text
TowerBehaviour
    owns
TowerVisualController
```

Gameplay systems may request visual changes, but tower-local visual rendering is performed through TowerVisualController.

---

# 2. Source Documents

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/06_TowerPlacementSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/10_TowerUpgradeSystem.md`

---

# 3. Dependencies

This is the first task in the visual foundation sequence.

No later preview or tower level model task should directly manipulate tower visual hierarchy instead of using this ownership path.

---

# 4. Scope

Included:

- Add or finalize TowerVisualController runtime component.
- Attach or bind TowerVisualController through TowerBehaviour.
- Support references for:
  - VisualRoot
  - TowerBaseVisualRoot
  - TowerPrefabSpawnPoint
  - PreviewRenderer
  - AttackOriginFallback
- Define TowerVisualController as the owner of tower-local visual rendering operations.
- Provide API direction for later tasks:
  - `SetTowerVisual(GameObject towerModelPrefab)`
  - `GetCurrentAttackOrigin()`
  - `SetPreviewMaterialState(Color tint, float alpha)`
  - `ClearCurrentTowerModel()`
  - `ShowAttackRangePreview()`
  - `HideAttackRangePreview()`
  - `UpdateAttackRangePreview()`

Excluded:

- Actual tower level model spawning logic.
- Placement preview drag workflow.
- Tower level-up preview workflow.
- DragCancel workflow.
- Attack range rendering implementation.
- Tower upgrade rule implementation.

---

# 5. Current Code Snapshot

Current relevant runtime scripts:

| Script | Current Role |
|---|---|
| `Assets/Scripts/TowerFramework/TowerInstance.cs` | Stores placed tower definition, current level, basic damage, and occupied nodes. |
| `Assets/Scripts/TowerRuntimeCombat/TowerCombatBehaviour.cs` | Runs tower combat and currently owns a serialized `attackOrigin` fallback path. |
| `Assets/Scripts/TowerDeployment/TowerPlacementPreview.cs` | Owns current preview color/material feedback for placement preview instances. |
| `Assets/Scripts/TowerDeployment/TowerPlacementController.cs` | Creates and updates the current placement preview instance. |
| `Assets/Scripts/TowerDeployment/TowerDeployController.cs` | Instantiates deployed tower prefabs and initializes `TowerInstance` / `TowerCombatBehaviour`. |
| `Assets/Scripts/TowerDeployment/TowerAnchorSet.cs` | Defines placement anchors and should remain unchanged in this task. |

Current gap:

- There is no `TowerBehaviour.cs` yet.
- There is no `TowerVisualController.cs` yet.
- Tower visual ownership is not centralized.
- `TowerPlacementPreview` directly owns preview renderer color logic.
- `TowerCombatBehaviour` currently falls back to the tower transform when `attackOrigin` is missing. Task001 should not expand that pattern; Task002 should move combat consumption to the current active AttackOrigin path.

---

# 6. Script Impact

## 6.1 Create

### `Assets/Scripts/TowerFramework/TowerBehaviour.cs`

Purpose:

- Become the tower-owned runtime facade for tower-local shared components.
- Own or expose the tower's `TowerVisualController`.
- Keep `TowerInstance` as runtime state data instead of turning `TowerInstance` into a visual/controller owner.

Expected responsibilities:

- Cache or require `TowerInstance`.
- Cache or require `TowerVisualController`.
- Expose `TowerInstance`.
- Expose `TowerVisualController`.
- Provide `Initialize(TowerInstance towerInstance)` as a small initialization path that later tasks can call after tower deployment.
- Make `Initialize(TowerInstance towerInstance)` idempotent and safe to call multiple times without duplicate initialization or inconsistent state.

Do not add:

- Placement validation.
- Upgrade validation.
- Combat targeting.
- Model replacement implementation.

### `Assets/Scripts/TowerFramework/TowerVisualController.cs`

Purpose:

- Own tower-local visual references and future visual operation entry points.

Serialized/reference fields:

- `visualRoot`
- `towerBaseVisualRoot`
- `towerPrefabSpawnPoint`
- `previewRendererRoot` or `previewRenderer`
- `attackOriginFallback`

Initial API surface:

```csharp
public Transform GetCurrentAttackOrigin()
public void SetTowerVisual(GameObject towerModelPrefab)
public void ClearCurrentTowerModel()
public void SetPreviewMaterialState(Color tint, float alpha)
public void ShowAttackRangePreview(float attackRange)
public void HideAttackRangePreview()
public void UpdateAttackRangePreview(float attackRange)
```

Task001 implementation expectation:

- It is acceptable for model, transparency, and range methods to be placeholder-safe methods if their actual behavior belongs to later tasks.
- `SetTowerVisual(GameObject towerModelPrefab)` is the only public model assignment API. Later implementation may internally destroy the previous model when a model already exists.
- Task001 `SetTowerVisual(GameObject towerModelPrefab)` should not implement full model spawn or replacement behavior.
- Task001 `GetCurrentAttackOrigin()` should return `attackOriginFallback`.
- Task002 will extend `GetCurrentAttackOrigin()` to resolve the active tower model's AttackOrigin while preserving the same public API.
- Missing model AttackOrigin warning belongs to Task002 when active tower model resolution exists.

## 6.2 Modify

### `Assets/Scripts/TowerDeployment/TowerDeployController.cs`

Purpose:

- Ensure deployed tower prefabs have a `TowerBehaviour` runtime owner.
- Initialize or cache `TowerBehaviour` after `TowerInstance.Initialize(...)`.

Expected change:

```text
Instantiate tower prefab
    ↓
Ensure TowerInstance
    ↓
Initialize TowerInstance
    ↓
Ensure TowerBehaviour
    ↓
Initialize TowerBehaviour with TowerInstance
    ↓
Initialize TowerCombatBehaviour as currently done
```

Keep this change minimal. Do not change deployment validation or node occupation.

### `Assets/Scripts/TowerDeployment/TowerPlacementPreview.cs`

Purpose:

- Prepare preview instances to use TowerVisualController ownership later.

Expected Task001 change:

- Cache optional `TowerVisualController` if present.
- Keep existing preview color behavior functional.
- Do not move full preview transparency behavior in this task unless it is a small compatibility wrapper.

### Tower prefabs under `Assets/Art/Prefab/Tower/`

Purpose:

- Authoring follow-up may be required so tower prefabs can serialize the new component references.

Expected prefabs:

- `Assets/Art/Prefab/Tower/Prefab_ArcherTower.prefab`
- `Assets/Art/Prefab/Tower/Prefab_CannonTower.prefab`
- `Assets/Art/Prefab/Tower/Prefab_MagicTower.prefab`
- `Assets/Art/Prefab/Tower/Prefab_DroneTower.prefab`

Task001 may add components/references only if implementation needs prefab-backed validation now. Otherwise, leave prefab migration to the first runtime task that needs visual behavior.

## 6.3 Do Not Modify In Task001

Do not modify these scripts for behavior changes in Task001:

| Script | Reason |
|---|---|
| `Assets/Scripts/TowerRuntimeCombat/TowerCombatBehaviour.cs` | Active AttackOrigin consumption should be handled in Task002 when level model runtime exists. |
| `Assets/Scripts/TowerDeployment/TowerPlacementController.cs` | Placement preview lifecycle belongs to Task003. |
| `Assets/Scripts/TowerDeployment/TowerPlacementValidator.cs` | Placement validation is out of scope. |
| `Assets/Scripts/TowerDeployment/BattleHUDUI.cs` | DragCancel and HUD release area behavior belongs to Task004. |
| `Assets/Scripts/TowerDeployment/TowerDraftSystem.cs` | Draft generation is out of scope. |
| `Assets/Scripts/TowerFramework/TowerDefinition.cs` | `TowerLevelConfig.towerModelPrefab` already exists; schema change is not required for Task001. |
| `Assets/Scripts/TowerFramework/TowerLevelConfig.cs` | `towerModelPrefab` already exists; model runtime belongs to Task002. |

---

# 7. Implementation Plan

1. Create `TowerVisualController.cs`.
   - Add serialized references for visual roots, preview renderer root, and AttackOriginFallback.
   - Add read-only properties for key references where useful.
   - Add safe API stubs for model, transparency, range, and origin methods.
   - Implement `GetCurrentAttackOrigin()` as `attackOriginFallback` only for Task001.
   - Keep `SetTowerVisual(GameObject towerModelPrefab)` as a forward-compatible stub or no-op path for Task002.

2. Create `TowerBehaviour.cs`.
   - Cache `TowerInstance`.
   - Cache or add `TowerVisualController`.
   - Expose `VisualController`.
   - Provide `Initialize(TowerInstance towerInstance)` for deployment flow.
   - Make `Initialize(TowerInstance towerInstance)` idempotent and safe to call multiple times.

3. Update `TowerDeployController.cs`.
   - Ensure deployed tower object has `TowerBehaviour`.
   - Initialize `TowerBehaviour` after `TowerInstance.Initialize(...)`.
   - Preserve existing `TowerCombatBehaviour.Initialize(...)` flow for this task.

4. Lightly prepare `TowerPlacementPreview.cs`.
   - Cache optional `TowerVisualController`.
   - Keep current renderer color path working.
   - Avoid moving preview lifecycle or placement validation into the new component.

5. Compile and inspect warnings.
   - Fix missing namespace/import issues.
   - Confirm existing tower deployment path still compiles.
   - Confirm no new dependency requires Task002/003 behavior prematurely.

---

# 8. Ownership Rules

TowerBehaviour owns TowerVisualController.

TowerVisualController owns:

- Tower-local model visual management.
- Tower-local preview transparency application.
- Tower-local AttackOrigin resolution.
- Tower-local range preview rendering hooks.

TowerPlacementSystem may request visual preview changes, but must not directly manipulate:

- VisualRoot
- TowerPrefabSpawnPoint
- spawned tower model instances
- renderer materials

TowerUpgradeSystem may request visual refresh after an accepted level-up, but must not directly manipulate tower visual hierarchy.

Later TowerRuntimeCombatSystem integration should consume the current active AttackOrigin resolved by the owning tower runtime and must not resolve visual hierarchy or fallback references directly. The actual combat integration belongs to Task002.

---

# 9. Implementation Notes

Recommended prefab structure:

```text
TowerPrefab
├── VisualRoot
│   ├── TowerBaseVisualRoot
│   └── TowerPrefabSpawnPoint
├── Collider
├── TowerAnchorSet
├── PreviewRenderer
└── AttackOriginFallback
```

AttackOriginFallback is a runtime safety fallback only.

If a spawned tower level model does not provide AttackOrigin, runtime must log a warning before using AttackOriginFallback.

Missing model AttackOrigin is an authoring/configuration error, not a normal runtime path.

Task001 does not spawn or replace tower level models yet, so it should not implement active model AttackOrigin resolution. Task002 extends the same `GetCurrentAttackOrigin()` API to prefer the active model AttackOrigin and warn before using AttackOriginFallback.

---

# 10. Verification Plan

Minimum verification:

- Unity compile succeeds.
- `TowerVisualController.cs` compiles without requiring Task002 model runtime implementation.
- `TowerBehaviour.cs` compiles and can be attached to tower prefabs or added at runtime.
- `TowerDeployController.cs` still deploys towers through the existing flow.
- `TowerPlacementPreview.cs` still supports current valid/invalid preview feedback.

Search checks:

```text
rg -n "class TowerBehaviour|class TowerVisualController" Assets/Scripts
rg -n "sharedMaterial" Assets/Scripts/TowerFramework Assets/Scripts/TowerDeployment
rg -n "TowerVisualController" Assets/Scripts
```

Manual runtime smoke test:

- Start a tower placement drag.
- Confirm preview still appears.
- Place a tower on a valid node.
- Confirm deployed tower receives `TowerInstance`, `TowerBehaviour`, `TowerVisualController`, and `TowerCombatBehaviour`.
- Confirm no new warnings appear except intentional missing visual reference warnings during incomplete prefab authoring.

---

# 11. Acceptance Criteria

- TowerBehaviour owns or exposes a TowerVisualController reference.
- `TowerBehaviour.Initialize(TowerInstance towerInstance)` is idempotent.
- TowerVisualController can resolve required prefab visual references.
- TowerVisualController exposes an API path for later model, preview, transparency, range, and AttackOrigin tasks.
- Missing required visual references are reported clearly.
- Task001 `GetCurrentAttackOrigin()` returns AttackOriginFallback without attempting active model AttackOrigin resolution.
- TowerPlacementSystem and TowerUpgradeSystem do not directly manipulate tower visual hierarchy.
- Existing TowerAnchorSet and placement anchor behavior remain unchanged.

---

# 12. Review Checklist

- Visual ownership is tower-local.
- Gameplay systems request visuals instead of owning visuals.
- No shared material mutation is introduced.
- No placement or upgrade validation logic is added to TowerVisualController.
- No attack range rendering implementation is forced into this task.
