# Task004 - Tower Upgrade Draft Flow Integration

## Status

Planned.

## Goal

Connect Tower Upgrade Draft selection to actual tower upgrade application.

After this task, the Tower Upgrade Draft core framework should be playable end to end.

## Source Documents

- `Docs/05_DraftSystem.md`
- `Docs/06_TowerPlacementSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/02_BattleHUDUISystem.md`

## Required Core Flow

This task should implement the formal Tower Upgrade Draft flow:

```text
PlayerSystem raises OnPlayerLevelUp
    ->
DraftSystem generates Draft Choices
    ->
BattleHUD opens Draft Window
    ->
Player selects Tower Upgrade Draft choice
    ->
DraftSystem processes selected Draft result
    ->
BattleHUD creates Tower Upgrade Draft item
    ->
Player drags Tower Upgrade Draft item onto target tower
    ->
TowerPlacementSystem detects target tower intent
    ->
TowerPlacementSystem routes target intent to TowerUpgradeSystem
    ->
TowerUpgradeSystem validates and applies upgrade
    ->
Successful apply consumes the draft item
```

Rejected apply must not consume the draft item.

## DraftSystem Requirements

DraftSystem should generate eligible Tower Upgrade Draft choices from current tower instance state:

- TowerFamily
- TowerLevel
- RequiredTowerLevel eligibility
- Already applied upgrades
- TowerUpgradeDefinition assets provided by TowerUpgradeSystem or TowerUpgradeDatabase

DraftSystem owns candidate gathering, tower-instance weighting, sampling, displayed choice count, and displayed-choice deduplication.

DraftSystem should follow the first-version combined candidate pool model from `Docs/05_DraftSystem.md`: Tower Draft candidates and Tower Upgrade Draft candidates are merged into one candidate pool, sampled as equal-weight candidate entries, and deduplicated for final display by Draft item identity.

TowerUpgradeSystem remains the eligibility and application authority.

## Placement And UI Requirements

- BattleHUD creates a draggable Tower Upgrade Draft item.
- TowerPlacementSystem detects existing tower target intent.
- TowerPlacementSystem forwards upgrade target intent to TowerUpgradeSystem.
- TowerPlacementSystem does not decide upgrade rules.
- BattleHUD removes the draft item only after successful upgrade application.

## Valid Target Highlight UX

Valid target highlight for Tower Upgrade Draft items should use a first-version lightweight tower-model highlight.

When dragging a Tower Upgrade Draft item, valid target towers should be visually highlighted by changing the material BaseMap color on the tower model under:

```text
Prefab_TowerBaseModel / VisualRoot
```

The highlight should preferably include a small motion or pulse animation, similar in spirit to the existing AttackRangePreviewMotion script.

Implementation should investigate whether AttackRangePreviewMotionTarget or the same small presentation pattern can be extracted into a reusable helper so attack range preview motion and tower valid-target highlight can share lightweight presentation logic.

Suggested ownership flow:

```text
BattleHUD / Drag Item starts upgrade-drag mode
    ->
TowerPlacementSystem or a dedicated helper queries candidate towers
    ->
TowerUpgradeSystem validates whether each tower is a valid target
    ->
Valid target towers receive highlight visual state
    ->
Invalid target towers remain unchanged
```

TowerUpgradeSystem must remain the validation authority, but it must not directly control visual presentation.

Before implementing this highlight, present the concrete implementation plan for review.

## Material Safety

Tower model materials may be shared between tower instances.

The highlight implementation must avoid mutating shared Material assets directly.

Use a per-renderer runtime material instance or MaterialPropertyBlock where feasible so highlighting one tower does not recolor every tower using the same material.

## Highlight Cleanup

Highlight state must always be restored or cleared when:

- Drag is cancelled.
- Drop succeeds.
- Drop fails.
- Draft item is consumed.
- Draft item is returned to the UI.
- Target tower is destroyed or unavailable during drag.

The implementation should avoid ghost highlight states after any drag termination path.

## UX Scope

Task004 may implement only the first-version tower-model highlight after the plan is reviewed.

Do not expand into outline shaders, screen-space indicators, custom VFX, hover tooltips, or other advanced UX unless explicitly approved later.

## Out Of Scope

- Reroll.
- Rarity.
- Global Buff Draft.
- Temporary Buff Draft.
- Synergy Draft gameplay.
- New concrete behaviour package implementation.

## Acceptance Criteria

- Tower Upgrade Draft choices can be generated from current deployed tower state.
- A Tower Upgrade Draft item can be created in the BattleHUD draft interaction area.
- DraftSystem generation is part of the formal Tower Upgrade Draft flow.
- Player selection of a Tower Upgrade Draft choice produces a draggable upgrade draft item.
- Dragging a Tower Upgrade Draft item onto a valid target tower applies the upgrade.
- Dragging a Tower Upgrade Draft item onto an invalid target tower does not apply the upgrade.
- Invalid target rejection does not consume the draft item.
- Successful apply consumes the draft item.
- DraftSystem does not apply upgrades directly.
- TowerUpgradeSystem does not generate or sample Draft choices.
- Valid target towers can be visually highlighted during upgrade draft dragging.
- Highlight uses TowerUpgradeSystem eligibility results.
- Highlight does not mutate shared material assets globally.
- Highlight is cleared after drag success, failure, or cancellation.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
