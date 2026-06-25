# Task004 - Tower Level-Up Preview And DragCancel

---

# 1. Goal

Implement Tower Draft same-TowerFamily level-up preview and generic DragCancel behavior.

Tower Level-Up Preview applies only when a Tower Draft item is dragged onto an existing deployed tower with the same TowerFamily. It displays the Current Level + 1 ghost model before release and does not modify the existing tower until the level-up request is accepted.

DragCancel is a generic current drag operation cancel path. It applies to any draggable Draft item, not only Tower Draft.

---

# 2. Source Documents

- `Docs/06_TowerPlacementSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/05_DraftSystem.md`
- `Docs/02_BattleHUDUISystem.md`
- `Docs/07_TowerFrameworkSystem.md`

---

# 3. Dependencies

Depends on:

- `Task001_TowerVisualControllerFoundation.md`
- `Task002_TowerLevelModelRuntime.md`
- `Task003_TowerPlacementPreviewRuntime.md`

This task assumes placement preview and level model runtime are already available.

---

# 4. Scope

Included:

- Detect Tower Draft target intent using occupied GridNode identity.
- Do not use deployed tower Colliders, raycast tower hits, distance thresholds, or nearest spatial matching for level-up target detection.
- Require matching TowerFamily for Tower Level-Up Preview.
- Require target tower below max level.
- Current max tower level target is Lv3.
- Show Current Level + 1 ghost tower model.
- Keep existing deployed tower unchanged before release.
- If target cannot be upgraded, do not enter Tower Level-Up Preview.
- On successful release, forward level-up request to TowerUpgradeSystem.
- TowerUpgradeSystem owns validation and level data application only.
- After accepted level-up, TowerPlacementSystem asks the target TowerBehaviour to refresh visuals through the tower-owned visual path.
- Implement DragCancel when current dragged Draft item is released back into Battle HUD Draft Item Interaction Area.

Excluded:

- Future Tower Upgrade Draft item effect preview.
- Attack damage, extra Magic Orb, or behavior-upgrade preview.
- Attack range preview rendering.
- New upgrade definition rules.
- Draft pool generation changes.

---

# 5. Tower Level-Up Preview Rules

Tower Level-Up Preview may be entered only when:

- The dragged item is a Tower Draft item.
- The active Tower Preview Center Anchor snaps to a GridNode contained in an existing deployed tower's TowerInstance.OccupiedNodes.
- The dragged Tower Draft TowerFamily matches the deployed tower TowerFamily.
- The deployed tower is below max tower level.

Target detection rules:

1. Preview CenterAnchor snaps to the current target GridNode.
2. Search deployed towers.
3. If current target GridNode is contained in tower.TowerInstance.OccupiedNodes, that tower is the hovered tower candidate.
4. If a candidate exists, run TowerUpgradeSystem.CanLevelUpTower(candidate.TowerInstance, draftTowerDefinition, out nextLevel).
5. If valid, snap preview to candidate TowerAnchorSet.CenterAnchor, show nextLevel ghost model, and mark level-up preview active.
6. If invalid, clear level-up preview active, keep preview invalid, and do not run normal placement validation for this node.
7. If no candidate exists, run normal grid placement validation.

Do not use Colliders, raycast tower hits, configurable thresholds, or nearest spatial matching for this task.

TowerPlacementSystem should remove null entries from its deployed tower tracking list before target detection.

If SetPreviewLevel(nextLevel) fails while entering Tower Level-Up Preview, TowerPlacementSystem should clear currentLevelUpTarget, mark level-up preview inactive, keep the tower target candidate from falling through to normal deployment, and set the preview invalid.

Preview visual:

```text
TowerBaseVisualRoot
    +
Current Level + 1 Ghost Tower Model
```

The preview:

- Is semi-transparent.
- Does not modify the deployed tower before release.
- Does not consume the Draft item before accepted release.
- Uses tower level-up validation rather than grid occupation validation.

Preview and release should use the same TowerUpgradeSystem validation helper, such as CanLevelUpTower(..., out int nextLevel), so the preview state and accepted release path cannot drift.

TowerUpgradeSystem.TryLevelUpTower(...) is the final authority on release. A previously valid preview must not bypass release-time validation.

---

# 6. DragCancel Rules

Use the term `DragCancel` consistently for this behavior.

DragCancel applies when any currently dragged Draft item is released back inside the Battle HUD Draft Item Interaction Area.

DragCancel applies to:

- Tower Draft items.
- Future Tower Upgrade Draft items.
- Future draggable Draft item types.

DragCancel behavior:

- Cancel the current drag operation immediately.
- DragCancel has highest release priority.
- Do not perform scene placement validation.
- Do not perform tower level-up validation.
- Do not perform tower upgrade application validation.
- Do not deploy a tower.
- Do not upgrade a tower.
- Return the Draft item to Battle HUD ownership.
- Destroy the active Tower Preview if one exists.
- Clear drag state.

Task004 should wire this generic DragCancel route only through the existing Tower Draft drag path. Future draggable Draft item types can reuse the same route later.

Attack range preview cleanup is implemented in `Task005_AttackRangePreviewDuringDrag.md`.

---

# 7. Acceptance Criteria

- Tower Draft over same-TowerFamily deployed tower enters Tower Level-Up Preview when below max level.
- Tower Draft over different TowerFamily tower does not enter Tower Level-Up Preview.
- Max-level same-TowerFamily tower does not enter Tower Level-Up Preview.
- Tower Level-Up Preview shows Current Level + 1 ghost model.
- Existing deployed tower remains unchanged before release.
- Accepted release routes to TowerUpgradeSystem and consumes the Draft item only if accepted.
- Accepted level-up applies level data through TowerUpgradeSystem, then TowerPlacementSystem requests TowerBehaviour.RefreshTowerVisual().
- Failed level-up release does not consume the Draft item, clears the active preview, and returns or keeps the item in HUD.
- Deployed tower tracking removes null entries before target detection.
- If SetPreviewLevel(nextLevel) fails, level-up preview is cleared and preview becomes invalid.
- Release-time level-up must call TowerUpgradeSystem.TryLevelUpTower(...) before consuming the Draft item.
- Releasing any dragged Draft item back into Battle HUD Draft Item Interaction Area triggers DragCancel.
- DragCancel does not run scene placement, tower level-up, or upgrade application validation.
- DragCancel clears preview and drag state.

---

# 8. Review Checklist

- `DragCancel` wording is consistent.
- DragCancel is generic current drag operation behavior, not Tower Draft only.
- Tower Level-Up Preview is same-TowerFamily only.
- Future Tower Upgrade Draft item effect previews are not implemented here.
- Attack range preview cleanup is left for Task005.
