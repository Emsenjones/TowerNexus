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
- Layer unlock by tower level
- Remaining slot count per layer
- Duplicate upgrade prevention
- Slot validation
- Compatibility and explicit incompatibility checks
- CanApplyUpgrade
- TryApplyUpgrade

Slot counts may be fixed in v1, but they must be centralized and not scattered across unrelated code paths.

## Required Contracts

- TowerUpgradeSystem owns eligibility and application rules.
- TowerUpgradeSystem does not own Draft pool generation.
- TowerUpgradeSystem does not execute runtime combat behaviour packages.
- Tower level unlocks upgrade layers:

| Tower Level | Unlocked Layers |
|---|---|
| Lv1 | Basic |
| Lv2 | Basic, Behaviour |
| Lv3 | Basic, Behaviour, Synergy |

- Applying any TowerUpgradeDefinition consumes one slot from that upgrade's layer.
- Basic, Behaviour, and Synergy slots are separate layer-specific slot pools in v1.
- Duplicate restriction is per tower, not global.

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
- TowerUpgradeSystem rejects an upgrade when the target tower has not unlocked the upgrade layer.
- TowerUpgradeSystem rejects an upgrade when no slot remains for the upgrade layer.
- TowerUpgradeSystem rejects duplicate upgrades on the same tower.
- TowerUpgradeSystem rejects explicitly incompatible upgrades.
- A successful TryApplyUpgrade records the upgrade and consumes the correct layer slot.
- A failed TryApplyUpgrade does not mutate tower upgrade state.
- A development-only debug path can apply a configured upgrade during Play Mode for validation.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
