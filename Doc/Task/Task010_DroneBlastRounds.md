# Task010 - Drone Blast Rounds

Status: Ready for implementation

Depends on: Task002

## 1. Goal

Implement Blast Rounds as an additive area Effect after every valid primary direct hit from a Drone-fired projectile, with independent direct and explosion Elemental opportunities.

## 2. Source Documents

- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. Current State

Drone projectiles use the shared Direction Projectile path but do not receive Drone Behaviour package options. Their valid direct hit already resolves direct damage and the normal tower Elemental entry.

## 4. In Scope

- Resolve Blast Rounds and its Effect reference from the source tower's applied packages.
- Pass the Effect to each Drone projectile through narrow immutable projectile runtime options.
- Trigger Blast Rounds only after a valid primary direct Monster Hit.
- Finish direct damage and the direct Elemental attempt before explosion execution.
- Execute the authored area Effect at the actual hit position even if direct damage killed the contacted Monster.
- Produce one independent Elemental opportunity for every valid explosion target.
- Apply the option to projectiles fired by every Twin Drone.

## 5. Out of Scope

- Modifying the shared Drone ProjectileConfig asset to contain instance-specific Blast Rounds behavior.
- Explosion on projectile lifetime expiry or a miss with no direct Monster Hit.
- Replacing direct damage.
- Attack-level deduplication between direct and explosion targets.
- Final Dive behavior or Drone battery-state changes.

## 6. Runtime Contract

```text
Drone projectile resolves primary Monster Hit
    -> direct damage
    -> direct Elemental application opportunity
    -> execute Blast Rounds Effect at hit position
    -> one Elemental opportunity per valid explosion target
    -> projectile cleanup
```

The contacted Monster may receive both results if it survives direct damage and remains a valid explosion target. If direct damage kills it, the explosion still executes but target resolution uses the current post-direct valid state.

## 7. Ownership Contract

- Drone/Tower runtime resolves whether Blast Rounds is active and supplies the immutable Effect reference.
- Projectile runtime owns direct-hit timing and direct-before-explosion ordering.
- Effect System owns area resolution, actions, and explosion VFX.
- Buff System owns final Elemental application outcomes.

## 8. Unity Authoring Checklist

- Create or configure a Drone Blast Rounds upgrade asset.
- Assign a valid radius-based EffectDefinition with intended damage and explosion VFX.
- Keep the shared Drone ProjectileConfig free of instance-specific Blast Rounds gameplay data.
- Prepare one-Drone and Twin-Drones Play Mode configurations.

## 9. Acceptance Criteria

- Without Blast Rounds, Drone projectiles retain direct-only behavior.
- With Blast Rounds, every valid direct hit executes exactly one explosion.
- A miss or lifetime cleanup with no direct hit executes no explosion.
- Direct results complete before explosion resolution.
- Explosion executes even when direct damage kills the contacted Monster.
- A surviving contacted Monster may receive direct plus explosion damage and two independent Elemental attempts.
- Other explosion targets receive one explosion opportunity each.
- Twin Drones inherit Blast Rounds independently.
- Positive damage is not an Elemental prerequisite.
- Shared ProjectileConfig assets are not mutated per tower instance.

## 10. Validation

- Test direct target survival/death, empty explosion radius, and multiple explosion targets.
- Test misses, lifetime cleanup, zero damage, BuffApplyCooldown, and Protection.
- Test overlapping projectiles from Twin Drones.
- Inspect projectile options to confirm they contain only relevant immutable data.
- Run `git diff --check` and verify no Final Dive changes entered this task.

## 11. Review Note

Task011 follows this task to reduce overlap in Drone option transport and attack-flow changes. The two upgrades remain independently functional.
