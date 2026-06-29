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
DraftSystem generates eligible upgrade pool
    ->
BattleHUD creates Tower Upgrade Draft item
    ->
Player drags Tower Upgrade Draft item onto target tower
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
- Unlocked upgrade layers
- Remaining upgrade slots
- Already applied upgrades
- TowerUpgradeDefinition assets provided by TowerUpgradeSystem or TowerUpgradeDatabase

DraftSystem owns candidate gathering, tower-instance weighting, sampling, displayed choice count, and displayed-choice deduplication.

TowerUpgradeSystem remains the eligibility and application authority.

## Placement And UI Requirements

- BattleHUD creates a draggable Tower Upgrade Draft item.
- TowerPlacementSystem detects existing tower target intent.
- TowerPlacementSystem forwards upgrade target intent to TowerUpgradeSystem.
- TowerPlacementSystem does not decide upgrade rules.
- BattleHUD removes the draft item only after successful upgrade application.

## Valid Target Highlight UX Pending Confirmation

Valid target highlight for Tower Upgrade Draft items is required, but the exact UX implementation is not finalized.

Before implementing the valid target highlight UX, explicitly present the proposed UX behavior for review and confirmation.

Do not silently choose a final highlight implementation.

This task should separate:

- Required core flow
- Valid target highlight UX pending confirmation

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
- Dragging a Tower Upgrade Draft item onto a valid target tower applies the upgrade.
- Dragging a Tower Upgrade Draft item onto an invalid target tower does not apply the upgrade.
- Invalid target rejection does not consume the draft item.
- Successful apply consumes the draft item.
- DraftSystem does not apply upgrades directly.
- TowerUpgradeSystem does not generate or sample Draft choices.
- Valid target highlight UX is either confirmed before implementation or remains clearly marked as pending.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
