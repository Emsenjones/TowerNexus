# Task007 - Drone Behaviour Packages

## Status

Planned.

## Goal

Implement Drone Tower Behaviour Layer upgrades.

After this task, Drone Tower can use the prioritized Drone behaviour package while keeping Drone-specific logic inside Drone runtime.

## Source Documents

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

## Priority Scope

This task should prioritize:

- Twin Drones

Missile Drone and Final Dive are more complex and may be postponed unless explicitly included after review.

## Twin Drones Requirements

- Drone Tower launches two drones simultaneously when the package is active.
- Both drones use the existing Drone runtime rules unless explicitly overridden by package data.
- Drone runtime owns drone movement, targeting, burst fire, battery lifetime, and destruction.
- TowerUpgradeSystem only records that the tower owns the behaviour package.

## Final Dive Future Note

If Final Dive is implemented later, its relationship with the existing Drone battery-end state must be explicit.

Default battery-end behavior:

```text
Drone battery runs out
    ->
Default air-explode / despawn / recharge flow
```

With Final Dive:

```text
Drone battery runs out
    ->
Perform dive attack
    ->
Explosion
    ->
Tower enters recharge or next allowed attack cadence
```

Final Dive logic must stay inside DroneBehaviour or the Drone runtime path.

## Out Of Scope

- Missile Drone unless explicitly approved.
- Final Dive unless explicitly approved.
- Generic tracking missile framework.
- Generic Buff or Effect System changes.
- Archer, Cannon, or Magic behaviour packages.

## Acceptance Criteria

- Applying Twin Drones causes Drone Tower to launch two drones from one attack release.
- Both drones can independently move, orbit, fire bursts, consume battery, and despawn according to Drone runtime rules.
- Existing single-drone behavior is preserved when Twin Drones is not applied.
- Drone behaviour execution remains in Drone runtime, not TowerUpgradeSystem.
- If Final Dive is not implemented, task documentation and code do not leave ambiguous half-implemented Final Dive hooks.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
