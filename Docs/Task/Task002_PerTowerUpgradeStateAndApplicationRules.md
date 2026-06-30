# Task002 - Per-Tower Upgrade State And Application Rules

## Status

Implemented.

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
- RequiredTowerLevel unlock by tower level
- Duplicate upgrade prevention
- TowerFamily compatibility checks
- CanApplyUpgrade
- TryApplyUpgrade

V1 does not impose an upgrade slot count limit. A tower may receive multiple different upgrades from the same RequiredTowerLevel category.

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

- Applying a TowerUpgradeDefinition records that upgrade on the target tower.
- RequiredTowerLevel gates when an upgrade can be applied, but it does not impose a quantity limit in v1.
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
- TowerUpgradeSystem rejects an upgrade when TowerFamily does not match.
- TowerUpgradeSystem rejects an upgrade when required tower level is not met.
- TowerUpgradeSystem rejects an upgrade when the target tower has not unlocked that upgrade's required-level category.
- TowerUpgradeSystem rejects duplicate upgrades on the same tower.
- A successful TryApplyUpgrade records the upgrade.
- A failed TryApplyUpgrade does not mutate tower upgrade state.
- A development-only debug path can apply a configured upgrade during Play Mode for validation.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
