# Task003 - Tower Placement Preview Runtime

---

# 1. Goal

Implement normal Tower Draft placement preview for empty deployment areas.

During a Tower Draft drag operation, there should be exactly one active Tower Preview. Valid empty deployment areas should show the tower base plus the ghost model for the dragged Draft item's resolved deployment level. Invalid placement should keep the active preview visible with red-tinted ghost feedback.

---

# 2. Source Documents

- `Docs/06_TowerPlacementSystem.md`
- `Docs/07_TowerFrameworkSystem.md`
- `Docs/02_BattleHUDUISystem.md`

---

# 3. Dependencies

Depends on:

- `Task001_TowerVisualControllerFoundation.md`
- `Task002_TowerLevelModelRuntime.md`

This task assumes deployment-level model spawning and preview material-state hooks exist through TowerVisualController.

---

# 4. Scope

Included:

- Create and maintain exactly one active Tower Preview during Tower Draft drag.
- For valid empty deployable areas, show:
  - TowerBaseVisualRoot
  - tower model for the dragged Tower Draft item's resolved deployment level
  - original material color multiplied by valid preview tint plus preview alpha
- For invalid placement, keep the active Tower Preview visible with original material color multiplied by invalid preview tint plus preview alpha.
- Add serialized preview feedback config fields on TowerPlacementPreview:
  - `Color validPreviewTint = Color.white`
  - `Color invalidPreviewTint = Color.red`
  - `float previewAlpha = 0.5f`
- Recache all renderers under VisualRoot after TowerVisualController spawns the level model.
- Cache preview material instances per preview object and reuse them when switching valid or invalid state.
- Keep PreviewRenderer for compatibility but disable or visually ignore it unless existing runtime logic still depends on its transforms.
- Snap preview using Center Anchor and GridNode center position.
- Keep existing placement validation ownership in TowerPlacementSystem.
- Disable combat runtime components on temporary Tower Preview instances.
- On successful release, deploy the tower normally and initialize its resolved deployment-level visual model.
- Destroy temporary preview objects when placement completes or drag ends.

Excluded:

- Tower Level-Up Preview on existing towers.
- DragCancel behavior.
- Attack range preview rendering.
- Future Tower Upgrade Draft item effect preview.
- Path-blocking validation expansion beyond the existing placement validation path.

---

# 5. Runtime Flow

```text
BattleHUDUISystem
    ↓ Begin Tower Draft Drag
TowerPlacementSystem
    ↓ Create Active Tower Preview
TowerVisualController
    ↓ Show TowerBaseVisualRoot + Deployment-Level Ghost Model
TowerPlacementSystem
    ↓ Snap And Validate Placement
TowerVisualController
    ↓ Apply Whole-Preview Tint + Alpha
```

Successful release:

```text
TowerPlacementSystem
    ↓ Final Placement Validation
TowerBehaviour
    ↓ Initialize Tower Runtime
TowerVisualController
    ↓ Spawn Deployment-Level Deployed Model
```

---

# 6. Preview Material Rules

- Active Tower Preview remains semi-transparent until release or cleanup.
- Valid placement uses original material color multiplied by `validPreviewTint` plus `previewAlpha`.
- Invalid placement uses original material color multiplied by `invalidPreviewTint` plus `previewAlpha`.
- Tint and transparency apply to TowerBaseVisualRoot, the spawned tower model, and their child renderers.
- Preview material state applies only to preview instances.
- Preview material state is applied per renderer material instance.
- Do not replace all preview renderers with one shared ghost material.
- Support `_BaseColor` and `_Color` material properties.
- Runtime must not mutate `sharedMaterial`.
- Cache material instances per preview object and reuse them when switching valid or invalid state.
- Deployed towers should not inherit preview transparency.
- PreviewRenderer should not be the main valid or invalid feedback visual.
- TowerPlacementPreview should not require a serialized PreviewRenderer reference for material feedback.
- Do not delete PreviewRenderer in this task.

---

# 7. Acceptance Criteria

- Dragging a Tower Draft creates exactly one active Tower Preview.
- Valid empty placement displays base plus the resolved deployment-level ghost model.
- Valid placement displays original material color multiplied by `validPreviewTint` plus `previewAlpha`.
- Invalid placement keeps preview visible with original material color multiplied by `invalidPreviewTint` plus `previewAlpha`.
- Preview follows/snap updates without creating duplicate active previews.
- Successful release deploys a real tower with the resolved deployment-level model.
- Preview cleanup destroys temporary preview objects.
- Preview transparency never affects deployed towers.
- Preview material instances are cached per preview object and reused for valid/invalid switches.
- PreviewRenderer is not required for visible valid/invalid feedback.
- Temporary Tower Preview instances do not run TowerCombatBehaviour.
- TowerPlacementSystem does not directly manipulate tower visual hierarchy or renderer materials.

---

# 8. Review Checklist

- Placement validation remains in TowerPlacementSystem.
- Visual rendering remains in TowerVisualController path.
- PreviewRenderer remains compatibility-only and is not the primary feedback visual.
- No Tower Level-Up Preview behavior is mixed into this task.
- No attack range rendering behavior is mixed into this task.
