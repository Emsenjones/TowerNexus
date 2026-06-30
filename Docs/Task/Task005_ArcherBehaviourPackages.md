# Task005 - Archer Behaviour Packages

## Status

Planned.

## Goal

Implement the first Behaviour Layer package composition test through Archer upgrades.

After this task, Archer can use Piercing Arrow, Scatter Arrow, and their composition.

## Source Documents

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

## Scope

This task should implement:

- Piercing Arrow
- Scatter Arrow
- Piercing Arrow plus Scatter Arrow composition

Do not implement Hunting Arrow in this task unless explicitly approved later.

## Piercing Arrow Requirements

- Arrow continues flying after hitting enemies.
- MaximumPierceCount controls how many enemies it can pierce.
- Projectile-level runtime may track remaining pierce count.
- ProjectileSystem may execute projectile-level piercing state.
- ProjectileSystem should not decide why a projectile has piercing.
- The source tower's active Behaviour packages determine whether piercing is enabled.

## Scatter Arrow Requirements

- Archer fires exactly 3 arrows in v1.
- Center arrow targets the selected monster.
- Left and right arrows use `-SectorAngle / 2` and `+SectorAngle / 2`.
- Scatter Arrow reuses the existing ProjectileConfig.
- No additional ProjectileConfig is required.

## Composition Requirement

If one Archer owns both Piercing Arrow and Scatter Arrow, all scattered arrows should also support piercing.

Behaviour upgrades are composable by default in v1. This task should not introduce an upgrade-exclusion rule where applying one Archer Behaviour package blocks another.

## System Document Sync Check

During this task, keep `Docs/09_ProjectileSystem.md` aligned with the Archer Behaviour package contract. ProjectileSystem may execute projectile-level piercing state when an Archer Behaviour package grants it, but it should not decide why a projectile has piercing.

## Out Of Scope

- Hunting Arrow.
- Cannon, Magic, or Drone behaviour packages.
- Generic projectile modifier framework beyond what Piercing and Scatter require.
- Generic Buff or Effect System changes.

## Acceptance Criteria

- Applying Piercing Arrow lets Archer arrows hit multiple enemies before disappearing.
- MaximumPierceCount limits the number of additional enemy hits.
- Applying Scatter Arrow makes Archer fire exactly 3 arrows.
- Scatter Arrow's center arrow targets the selected monster.
- Scatter Arrow's side arrows use the configured sector angle offsets.
- Applying both Piercing Arrow and Scatter Arrow makes each scattered arrow able to pierce.
- Existing Archer behavior is preserved when no Archer Behaviour package is applied.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
