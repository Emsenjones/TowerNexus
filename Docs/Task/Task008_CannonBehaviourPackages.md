# Task008 - Cannon Behaviour Packages

## Status

Planned.

## Goal

Implement Cannon Tower Behaviour Layer upgrades.

After this task, Cannon Tower can use the prioritized Cannon behaviour package without introducing a generic Buff Framework.

## Source Documents

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

## Priority Scope

This task should prioritize:

- Burning Shell

Bouncing Shell and Timed Shell may be included later depending on complexity and explicit review.

## Burning Shell Requirements

- Burning Shell is Cannon-local area damage over time.
- Do not introduce a generic Buff Framework for Burning Shell.
- Do not attach persistent generic buff state to monsters in v1.
- Damage is fixed integer damage per tick.
- Do not use percentage HP damage in v1.
- Burning Shell logic should have a future migration path to Buff And Effect System, but this task should not build that general framework.

## Bouncing Shell Future Note

If Bouncing Shell is included later:

- Explosion launches another shell toward a nearby monster.
- SearchRadius and MaximumBounceCount control behavior.
- Cannon runtime or projectile impact handling should own the behavior enabled by the package.

## Timed Shell Future Note

If Timed Shell is included later:

- Explosion creates a delayed explosive.
- Delay and ExplosionRadius control behavior.
- The implementation should stay local to Cannon/projectile behavior until a shared effect framework is intentionally introduced.

## Out Of Scope

- Generic Buff Framework.
- Generic status effect framework.
- Percentage HP damage.
- Archer, Magic, or Drone behaviour packages.
- Cross-tower Synergy gameplay.

## Acceptance Criteria

- Applying Burning Shell causes Cannon impact to create local area damage over time.
- Burning Shell deals fixed integer damage per tick.
- Burning Shell uses configured area radius, tick interval, duration, and damage per tick.
- Burning Shell does not require or introduce a generic Buff Framework.
- Existing Cannon behavior is preserved when Burning Shell is not applied.
- If Bouncing Shell or Timed Shell is not implemented, task documentation and code do not leave ambiguous half-implemented hooks for them.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
