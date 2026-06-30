# Task002 - Per-Tower Upgrade State And Application Rules

## Status

Planned.

## Goal

Allow each deployed tower to record and receive upgrades.

After this task, runtime tower instances can track applied upgrades and TowerUpgradeSystem can validate and apply a selected TowerUpgradeDefinition to a target tower through code.

## Source Documents

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/07_TowerFrameworkSystem.md`
- `Docs/06_TowerPlacementSystem.md`

## Scope

This task should introduce per-tower upgrade state for:

- Applied upgrade ids or references
- Basic slots
- Behaviour slots
- Synergy slots
- RequiredTowerLevel unlock by tower level
- Remaining slot count per required-level slot pool
- Duplicate upgrade prevention
- Slot validation
- TowerFamily compatibility checks
- CanApplyUpgrade
- TryApplyUpgrade

Slot counts may be fixed in v1, but they must be centralized and not scattered across unrelated code paths.

## Required Contracts

- TowerUpgradeSystem owns eligibility and application rules.
- TowerUpgradeSystem does not own Draft pool generation.
- TowerUpgradeSystem does not execute runtime combat behaviour packages.
- Tower level unlocks upgrade categories by RequiredTowerLevel:

| Tower Level | Eligible Upgrade Categories |
|---|---|
| Lv1 | RequiredTowerLevel 1 / Basic |
| Lv2 | RequiredTowerLevel 1 / Basic, RequiredTowerLevel 2 / Behaviour |
| Lv3 | RequiredTowerLevel 1 / Basic, RequiredTowerLevel 2 / Behaviour, RequiredTowerLevel 3 / Synergy |

- Applying any TowerUpgradeDefinition consumes one slot from that upgrade's required-level slot pool.
- Basic, Behaviour, and Synergy slots are separate required-level-derived slot pools in v1.
- Duplicate restriction is per tower, not global.
- V1 does not include upgrade-exclusion rules where one applied upgrade blocks a different upgrade.
- V1 does not introduce a separate code-level layer field.

## Debug Testing Requirement

Before full DraftSystem integration exists, this task should provide or define a simple development-only debug path for applying a configured TowerUpgradeDefinition to a spawned tower during Play Mode.

The debug path may be a temporary debug component or another simple test entry. It must not become part of final gameplay flow.

The exact debug approach should be proposed in the implementation plan before coding.

## Out Of Scope

- Final BattleHUD Tower Upgrade Draft item flow.
- DraftSystem upgrade pool generation.
- Runtime stat resolver integration.
- Concrete behaviour package execution.
- Valid target highlight UX.

## Acceptance Criteria

- A tower can track which upgrades it owns.
- A tower can report remaining Basic, Behaviour, and Synergy slots.
- TowerUpgradeSystem rejects an upgrade when TowerFamily does not match.
- TowerUpgradeSystem rejects an upgrade when required tower level is not met.
- TowerUpgradeSystem rejects an upgrade when the target tower has not unlocked that upgrade's required-level category.
- TowerUpgradeSystem rejects an upgrade when no slot remains for that upgrade's required-level slot pool.
- TowerUpgradeSystem rejects duplicate upgrades on the same tower.
- A successful TryApplyUpgrade records the upgrade and consumes the correct required-level slot.
- A failed TryApplyUpgrade does not mutate tower upgrade state.
- A development-only debug path can apply a configured upgrade during Play Mode for validation.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
