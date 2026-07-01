# Task004-2 - Tower Upgrade Visual Feedback Foundation

## Status

Planned.

## Goal

Implement the first-version tower visual feedback foundation for Tower Upgrade Draft interactions and tower-related success moments.

After this task, the player should receive clear tower-side feedback when:

- Dragging a Tower Upgrade Draft item over the battlefield.
- Deploying a tower successfully.
- Leveling up a tower and refreshing its model.
- Applying a TowerUpgradeDefinition successfully.

This task is intended to be implementable as one focused pass because all included work belongs to the same tower-side visual feedback foundation.

## Relationship To Task004 And Task004-1

Task004 describes the full Tower Upgrade Draft Flow Integration target.

Task004-1 implemented the core playable Tower Upgrade Draft flow without valid-target highlight or presentation motion work.

Task004-2 completes the remaining first-version visual feedback foundation:

- Valid-target highlight while dragging Tower Upgrade Draft items.
- Tower-side success VFX hooks for deploy, level-up model refresh, and upgrade application.
- Simple tower-side VFX anchors.

Task004-2 should not expand Tower Upgrade Draft gameplay rules or implement Behaviour Layer upgrade content.

## Source Documents

- `Docs/00_ProjectOverview.md`
- `Docs/06_TowerPlacementSystem.md`
- `Docs/07_TowerFrameworkSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/Task/Task004-1_TowerUpgradeDraftCoreFlow.md`

## Required Flow

Task004-2 should implement this visual feedback flow:

```text
Player drags Tower Upgrade Draft item
    ->
TowerPlacementSystem resolves deployed tower candidates
    ->
TowerPlacementSystem asks TowerUpgradeSystem whether each candidate is eligible
    ->
Valid targets receive tower-side highlight through TowerVisualController
    ->
Invalid targets remain unhighlighted
    ->
Drag ends, succeeds, fails, or cancels
    ->
All valid-target highlights are cleared
```

Success feedback should follow the accepted gameplay result:

```text
Tower deploy succeeds
    ->
Request shared model spawn / refresh VFX through tower visual ownership path

Tower level-up succeeds and model visual refresh is accepted
    ->
Request shared model spawn / refresh VFX through tower visual ownership path

TowerUpgradeDefinition application succeeds
    ->
Request upgrade-applied VFX through tower visual ownership path
```

Visual feedback must never decide whether the action succeeds.

## Valid Target Highlight Requirements

When a Tower Upgrade Draft item is being dragged:

- Valid target towers should display a first-version periodic highlight or pulse.
- Invalid target towers should remain unchanged.
- TowerPlacementSystem may resolve deployed tower candidates during the active drag lifecycle.
- TowerPlacementSystem must use TowerUpgradeSystem eligibility results for valid-target feedback.
- TowerVisualController should own tower-local highlight presentation.
- BattleHUD should not own tower-local highlight presentation.
- TowerUpgradeSystem should not directly control highlight presentation.

The first version can use a lightweight brighter/dimmer pulse or equivalent readable tower-local highlight.

The highlight must avoid mutating shared Material assets globally.

Acceptable implementation directions include:

- Per-renderer runtime material instances.
- MaterialPropertyBlock.
- Another locally scoped renderer state approach that preserves shared asset safety.

Do not require a custom outline shader, screen-space indicator, hover tooltip, or upgrade-specific effect preview in this task.

## Highlight Cleanup Requirements

Highlight state must be cleared when:

- Drag is cancelled.
- The dragged item is released back into the Draft Item Interaction Area.
- Upgrade application succeeds.
- Upgrade application fails.
- The pending Draft item is restored.
- The target tower is destroyed or unavailable during drag.
- The active Tower Upgrade Draft drag state is replaced by another drag state.

The implementation must avoid ghost highlights after any drag termination path.

## Tower Success VFX Requirements

Task004-2 should support two first-version success VFX categories:

1. Shared model spawn / refresh VFX.
2. Upgrade-applied VFX.

The shared model spawn / refresh VFX should be used for:

- Tower deploy success.
- Tower level-up success after the tower model is refreshed.

Upgrade-applied VFX should be used for:

- Successful TowerUpgradeDefinition application.

Empty or unassigned VFX references should not block gameplay.

VFX playback is presentation-only. It must not affect:

- Placement validation.
- Level-up validation.
- Upgrade validation.
- Draft item consumption.
- Tower stats.
- Attack timing.
- Damage.
- Target selection.

## Feedback Anchor Requirements

Task004-2 should add support for simple tower-side VFX anchors.

The first version should support separate tower-side anchors for:

- Deploy success and level-up model refresh VFX.
- Upgrade-applied VFX.

Each anchor should provide a stable scene position for its corresponding tower-related feedback VFX.

Missing anchor handling should be safe and deterministic. Missing VFX anchor setup must not block tower deployment, tower level-up, upgrade application, or Draft item consumption.

## Ownership Requirements

TowerPlacementSystem owns:

- Active drag lifecycle.
- Deployed tower candidate lookup.
- Requesting valid-target feedback from tower visual ownership.
- Requesting success feedback after deploy, level-up, or upgrade application succeeds.
- Clearing valid-target feedback when the drag lifecycle ends.

TowerUpgradeSystem owns:

- Tower level-up validation and state application.
- Tower upgrade eligibility checks.
- Tower upgrade application.

TowerUpgradeSystem must not:

- Play VFX.
- Control tower highlight state.
- Mutate renderer materials.
- Operate tower visual hierarchy.

TowerVisualController owns:

- Tower-local valid-target highlight presentation.
- Tower-side success feedback playback.
- Tower VFX anchor usage.
- Any local renderer or material state needed for presentation.

BattleHUD owns:

- Draft item display.
- Drag interaction entry points.
- Pending Draft item removal after successful actions.

BattleHUD must not:

- Decide tower target eligibility.
- Own tower-local highlight presentation.
- Play tower-side VFX directly.

## VFX Configuration Direction

The implementation should inspect the current tower prefab, TowerVisualController, TowerDefinition, TowerLevelConfig, and related configuration structure before choosing where the VFX prefab references live.

The chosen ownership should preserve these rules:

- Deploy and level-up model refresh may share one model spawn / refresh VFX reference.
- Upgrade-applied feedback should have a separate VFX reference.
- VFX references are presentation configuration only.
- Gameplay systems should request playback through the tower visual ownership path instead of spawning tower-side VFX directly.

If multiple reasonable ownership options exist, present the trade-off before implementation.

## Out Of Scope

- Behaviour Layer upgrade gameplay implementation.
- Archer behaviour package implementation.
- Magic behaviour package implementation.
- Drone behaviour package implementation.
- Cannon behaviour package implementation.
- Upgrade-specific gameplay effect previews.
- Complex hover-state feedback.
- Different VFX by tower rarity, upgrade rarity, or upgrade type.
- Object pooling for VFX.
- Large visual animation framework.
- Custom outline shaders.
- Screen-space target indicators.
- Final art polish.
- Reworking the core DraftSystem candidate generation flow from Task004-1.
- Creating a separate TowerUpgradePlacementController.
- Creating a separate PendingTowerUpgradeItemUI.

## Acceptance Criteria

- Dragging a Tower Upgrade Draft item highlights eligible deployed towers.
- Ineligible deployed towers are not highlighted.
- Valid-target highlight uses TowerUpgradeSystem eligibility results.
- TowerUpgradeSystem does not directly control highlight presentation.
- BattleHUD does not own tower-local highlight presentation.
- TowerVisualController owns tower-local highlight presentation.
- Highlight does not mutate shared Material assets globally.
- Highlight is cleared after drag cancellation.
- Highlight is cleared after release back into the Draft Item Interaction Area.
- Highlight is cleared after successful upgrade application.
- Highlight is cleared after failed upgrade application.
- Highlight is cleared when the pending Draft item is restored.
- Highlight is cleared if the target tower is destroyed or unavailable during drag.
- Tower deploy success can request shared model spawn / refresh VFX.
- Tower level-up success can request shared model spawn / refresh VFX after model refresh.
- TowerUpgradeDefinition application success can request upgrade-applied VFX.
- Empty VFX references do not block deploy, level-up, upgrade application, or Draft item consumption.
- Tower-side VFX playback does not affect gameplay validation, stats, damage, targeting, or attack timing.
- Tower-side VFX anchors are supported for tower-related success VFX.
- Missing VFX anchor setup is handled safely and does not block gameplay.
- Existing Tower Draft placement behavior is preserved.
- Existing Tower Draft level-up behavior is preserved.
- Existing Tower Upgrade Draft application behavior from Task004-1 is preserved.
- Task004-2 does not implement Behaviour Layer upgrade gameplay.

## Implementation Review Note

Before implementation begins, present the concrete Task004-2 implementation plan in chat for review.

The plan should specifically explain:

- Where valid-target highlight state lives.
- How shared material mutation is avoided.
- Where model spawn / refresh and upgrade-applied VFX prefab references live.
- How tower-side VFX anchors are resolved and what fallback is used.
- Which drag termination paths clear highlight state.
