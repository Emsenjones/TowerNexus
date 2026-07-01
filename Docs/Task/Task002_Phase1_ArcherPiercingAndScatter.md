# Task002 - Phase 1 Archer Piercing And Scatter

## Objective

Implement the Phase 1 Archer Behaviour Layer upgrades:

- Piercing Arrow
- Scatter Arrow

Piercing Arrow and Scatter Arrow should compose naturally through normal projectile initialization. There should not be a separate Piercing + Scatter upgrade or a composition-specific behaviour package.

## System References

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

## Prerequisites

- Task001 Phase 1 Behaviour Runtime Activation is complete.
- Archer projectile release already works without Behaviour Layer upgrades.
- Projectile System can own projectile-level piercing state when granted by an Archer Behaviour package.

## Scope

Task002 should implement the two low-dependency Archer behaviour packages.

Piercing Arrow:

- Grants Archer arrows projectile-level piercing behaviour.
- The Archer runtime decides that the projectile should be initialized with piercing because the source tower owns Piercing Arrow.
- Projectile System executes projectile-level piercing state after initialization.
- Projectile System should not decide why the projectile has piercing.

Scatter Arrow:

- Changes Archer attack release from one arrow to three arrows.
- The center arrow targets the selected monster.
- The side arrows use configured sector angle offsets.
- Scatter Arrow should reuse the existing Archer projectile configuration unless a reviewed task later introduces separate projectile data.

Natural composition:

- If the tower owns both Piercing Arrow and Scatter Arrow, each scattered arrow should receive the same projectile-level piercing state as a normal Archer arrow.
- This should fall out of shared projectile initialization for Archer arrows, not a composition-only code path.

## Requirements

- Piercing Arrow affects arrows released by the Archer tower that owns the upgrade.
- Scatter Arrow affects only the Archer tower that owns the upgrade.
- A tower with only Piercing Arrow still releases one arrow.
- A tower with only Scatter Arrow releases three non-piercing arrows.
- A tower with both Piercing Arrow and Scatter Arrow releases three piercing arrows.
- Other tower families are unaffected.
- Existing Archer behaviour is preserved when neither Behaviour Layer package is active.

## Out Of Scope

- Hunting Arrow.
- Chain projectiles.
- Split projectile ownership inside Projectile System.
- Ricochet projectile behavior.
- Buff or effect execution.
- New upgrade exclusion rules.
- New projectile configs unless explicitly approved in the implementation plan.

## Acceptance Criteria

- Applying Piercing Arrow lets Archer arrows hit multiple valid enemies according to projectile-level piercing rules.
- Applying Scatter Arrow makes Archer release exactly three arrows from one attack release.
- Scatter Arrow's center arrow uses the selected target direction.
- Scatter Arrow's side arrows use the configured angle offsets.
- Applying both Piercing Arrow and Scatter Arrow makes every scattered arrow pierce.
- The implementation does not add a separate PiercingScatter upgrade, composition-specific upgrade state, or special-case composition package.
- Existing single-arrow Archer behaviour remains unchanged when no relevant Behaviour Layer package is active.
- Before implementation begins, present the concrete Task002 implementation plan in chat for review.
