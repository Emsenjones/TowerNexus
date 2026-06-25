# Task005 - Attack Range Preview During Drag

---

# 1. Goal

Implement attack range preview visibility during Draft item drag operations.

When a Draft item drag begins, deployed towers should show attack range previews and the active Tower Preview should show its own attack range. When the drag ends or DragCancel occurs, all attack range previews should be hidden.

This task is intentionally separate because range rendering may require dedicated prefab, material, mesh, scaling, and cleanup work.

---

# 2. Source Documents

- `Docs/06_TowerPlacementSystem.md`
- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/02_BattleHUDUISystem.md`

---

# 3. Dependencies

Depends on:

- `Task001_TowerVisualControllerFoundation.md`
- `Task003_TowerPlacementPreviewRuntime.md`
- `Task004_TowerLevelUpPreviewAndDragCancel.md`

This task assumes drag lifecycle, active preview lifecycle, and DragCancel are already defined.

---

# 4. Scope

Included:

- Show attack range preview for all deployed towers when a Draft item drag begins.
- Show attack range preview for the active Tower Preview.
- Read attack range from:

```text
TowerDefinition
    ↓
AttackConfig
    ↓
attackRange
```

- Hide deployed tower attack range previews when drag ends.
- Hide deployed tower attack range previews when DragCancel occurs.
- Cleanup active preview attack range rendering with active preview cleanup.
- Keep range rendering owned by TowerVisualController or tower-local visual path.
- Use an `AttackRangePreview` child on the Tower Base Prefab when available.
- Scale the `AttackRangePreview` child uniformly to match `AttackConfig.attackRange`.

Excluded:

- Placement preview creation.
- Tower Level-Up Preview creation.
- DragCancel trigger detection.
- Tower combat range calculation changes.
- Tower upgrade effect preview.

---

# 5. Runtime Flow

Drag begin:

```text
BattleHUDUISystem
    ↓ Begin Draft Item Drag
TowerPlacementSystem
    ↓ Request Range Preview Visibility
Deployed TowerBehaviour instances
    ↓ TowerVisualController.ShowAttackRangePreview()
Active Tower Preview
    ↓ TowerVisualController.ShowAttackRangePreview()
```

Drag end or DragCancel:

```text
TowerPlacementSystem
    ↓ Request Range Preview Hidden
Deployed TowerBehaviour instances
    ↓ TowerVisualController.HideAttackRangePreview()
Active Tower Preview
    ↓ Preview Cleanup
```

---

# 6. Rendering Rules

- Attack range preview should visually match `AttackConfig.attackRange`.
- `AttackRangePreview` is expected to contain a circular mesh whose radius is 1 when local scale is 1.
- TowerVisualController should set `AttackRangePreview.localScale = Vector3.one * attackRange`.
- Attack range preview is presentation-only.
- Attack range preview must not change combat targeting, detection, or damage.
- Range preview rendering should not mutate shared materials.
- Prefab-authored range preview objects should be hidden when not in use.
- Deployed tower range previews and active Tower Preview range previews should follow the same visual standard unless a later design explicitly separates them.

---

# 7. Acceptance Criteria

- Starting any Draft item drag shows attack ranges for all deployed towers.
- Active Tower Preview shows its own attack range while visible.
- Range size reflects `AttackConfig.attackRange`.
- Drag end hides all deployed tower attack ranges.
- DragCancel hides all deployed tower attack ranges.
- Active preview cleanup removes its range preview.
- Attack range preview does not affect combat logic.
- Task does not require runtime mesh or material generation.

---

# 8. Review Checklist

- Task remains focused on range preview rendering and cleanup.
- No placement preview or Tower Level-Up Preview logic is implemented here.
- No DragCancel trigger logic is implemented here.
- Rendering/prefab/material ownership is clear and rollback-safe.
