# Task001 - Upgrade Definition And Database

## Status

Planned.

## Goal

Create the upgrade data entry and authoring foundation for Tower Upgrade content.

After this task, upgrade assets can be authored and collected by a central database, but upgrades are not yet applied to runtime towers.

## Source Documents

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/07_TowerFrameworkSystem.md`
- `Docs/05_DraftSystem.md`

## Scope

This task should introduce the data model for:

- TowerUpgradeDefinition
- TowerUpgradeDatabase
- UpgradeLayer
- TowerFamily binding
- Basic additive stat deltas
- Behaviour package identifier
- Authoring validation for invalid content combinations

TowerUpgradeDefinition should represent one independent upgrade option.

TowerUpgradeDatabase should be the central source of truth for configured TowerUpgradeDefinition assets.

## Required Contracts

- Use TowerFamily as the tower compatibility identity.
- Do not introduce TargetTowerType as a separate matching field.
- Basic stat deltas only support additive values in v1.
- Do not introduce Multiply or Override modifier types.
- TowerUpgradeDefinition assets must not be attached to AttackConfig.
- AttackConfig remains the immutable default combat configuration template.
- TowerUpgradeDatabase contains no gameplay selection or application logic.
- DraftSystem selects candidates later.
- TowerUpgradeSystem applies selected upgrades later.

Basic stat delta authoring should support the stat categories required by the current System docs, including common tower stats and tower-family-specific stats.

## Out Of Scope

- Applying upgrades to TowerInstance.
- Runtime stat resolution.
- DraftSystem upgrade pool generation.
- BattleHUD Tower Upgrade Draft UI.
- Any concrete Archer, Cannon, Magic, or Drone behaviour package implementation.
- Synergy gameplay.

## Acceptance Criteria

- Designers can create TowerUpgradeDefinition assets.
- A TowerUpgradeDefinition can specify TowerFamily, UpgradeLayer, required tower level, display data, additive Basic stat deltas, and one Behaviour package identifier when applicable.
- A TowerUpgradeDatabase can reference all configured TowerUpgradeDefinition assets.
- TowerUpgradeDatabase can be queried or inspected as a content lookup source without owning gameplay logic.
- Authoring validation warns about clearly invalid combinations, such as Magic Orb-specific stat deltas on a Cannon upgrade.
- No TowerUpgradeDefinition data is stored on AttackConfig.
- No TargetTowerType field is introduced.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
