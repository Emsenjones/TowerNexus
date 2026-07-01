# Task004 - Phase 1 Drone Twin Drones

## Objective

Implement the Phase 1 Drone Behaviour Layer upgrade:

- Twin Drones

Twin Drones should change the number of released Drone attack entities from one successful Drone Tower attack release. It should not introduce Missile Drone, Final Dive, or new Drone lifecycle rules.

## System References

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/07_TowerFrameworkSystem.md`
- `Docs/09_ProjectileSystem.md`

## Prerequisites

- Task001 Phase 1 Behaviour Runtime Activation is complete.
- Drone Tower can release a Drone attack entity without Behaviour Layer upgrades.
- Released Drone attack entities already own movement, target selection, orbiting, battery lifetime, battery-end destruction, and Drone-fired projectile burst timing.

## Scope

Task004 should implement Twin Drones as a low-dependency released attack entity count change.

Twin Drones:

- Causes Drone Tower to release two Drone attack entities from one successful Drone Tower attack release.
- Both Drones use the same source tower context and release-time attack data.
- Each released Drone owns its own target selection, movement, orbit direction, burst timing, battery lifetime, and destruction.
- Drone Tower cooldown starts when the Twin Drones release succeeds, following the existing Drone cooldown contract.
- If no valid target exists at release time, Drone Tower should not release either Drone and should not start cooldown.

## Requirements

- Twin Drones uses existing Drone runtime rules.
- Twin Drones does not require a separate Drone parking anchor.
- Released Drones do not depend on tower model child Transforms after launch.
- Drone-fired projectiles continue to use Projectile System behavior after projectile creation.
- Drone FireAnchor remains owned by the Drone prefab or Drone runtime path.
- Drone local presentation such as propeller spinning remains Drone-local.

## Out Of Scope

- Missile Drone.
- Final Dive.
- Drone battery-end gameplay damage.
- Drone lifecycle rewrites.
- Drone pooling.
- New Drone target selection rules beyond normal independent Drone behavior.
- Buff And Effect System integration.
- Area damage, delayed damage, or repeated damage.

## Acceptance Criteria

- Applying Twin Drones causes Drone Tower to release two Drones from one successful attack release.
- Each released Drone independently follows existing Drone movement, target selection, burst fire, battery, and despawn rules.
- If no valid target exists at release time, no Drone is released and cooldown does not start.
- Existing single-Drone behavior is preserved when Twin Drones is not active.
- Missile Drone and Final Dive are not partially implemented.
- Before implementation begins, present the concrete Task004 implementation plan in chat for review.
