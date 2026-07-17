# Task007 - Archer Hunting Arrow

Status: Implementation complete; Unity Play Mode validation pending

Depends on: Task002

## 1. Goal

Implement Hunting Arrow as Archer-specific locked-target flight with confirmation-time target assignment, release-time range snapshots, one-way Direction fallback, and natural composition with Piercing Arrow and Scatter Arrow.

## 2. Source Documents

- `Doc/07_TowerFrameworkSystem.md`
- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`

## 3. Current State

- `TowerCombatBehaviour` captures fixed Hunting target slots and the confirmation-time main target position.
- Released Hunting Arrows use the Tracking flight override with immutable release-time range data.
- `ProjectileBehaviour` owns locked-target movement, one-way Direction fallback, Piercing continuation, and lifetime cleanup.

## 4. In Scope

- Activate tracking flight only for Archer projectiles released with Hunting Arrow.
- Capture the authoritative main target position and fixed Center, Left, and Right Hunting target slots at attack confirmation.
- Use the tower's normal initial target-selection semantics and assign distinct targets without replacement.
- Preserve confirmation-time Scatter directions for slots without a valid locked target at release.
- Revalidate the authoritative main target inside the animation release entry point and cancel the complete attack when it is invalid or outside the current resolved AttackRange.
- Snapshot one release-time TrackingRangeOrigin and resolved TrackingRange for every released Arrow.
- Track only the assigned locked target while both the Arrow and target remain inside the snapshotted range.
- Permanently transition to ordinary Direction flight when the locked target is hit, becomes invalid, leaves range, or the Arrow leaves range.
- Preserve independent target, tracking, hit history, piercing count, lifetime, and Elemental state per Arrow.

## 5. Out of Scope

- Homing for Cannon or Drone projectiles.
- Target prediction or leading.
- Tracking target reacquisition.
- Restoring Tracking after a Direction fallback.
- Shared target state between scattered Arrows.
- A generic tracking-target service or AttackEntity framework.
- Periodic Elemental application from movement or target acquisition.

## 6. Runtime Contract

```text
Scatter + Hunting attack confirmed
    -> capture the main target position
    -> capture fixed Center / Left / Right target slots without replacement
    -> release valid assigned slots as Tracking Arrows
    -> release unassigned or invalid secondary slots in their Scatter directions
```

```text
Tracking Arrow update
    -> Arrow and locked target valid and inside snapshotted range: move toward current HitAnchor position
    -> hit / invalid target / target out of range / Arrow out of range: permanently enter Direction flight
    -> surviving Piercing Arrow continues ordinary Direction hits
    -> lifetime expiry: cleanup
```

Only actual Monster Hits produce direct damage and Elemental opportunities.

## 7. Ownership Contract

- Tower Runtime owns confirmation-time target-slot assignment, the confirmation-time main-position snapshot, and the release-time immutable range snapshot.
- Projectile runtime owns locked-target tracking, tracking termination, Direction fallback, hit history, Piercing, lifetime, and impact results after release.
- `ProjectileConfig` continues to own speed, hit threshold, and lifetime.
- Hunting Arrow adds no V1 authored numeric parameter.

## 8. Unity Authoring Checklist

- Create or configure an Archer Hunting Arrow upgrade asset using `ArcherHuntingArrow`.
- Reuse the existing Arrow ProjectileConfig and prefab; confirm its speed, hit threshold, and lifetime support tracking.
- Prepare scenarios with zero, one, two, three, and more than three Monsters inside range, plus Monsters and Arrows crossing the range boundary.

## 9. Acceptance Criteria

- A Hunting Arrow continuously adjusts toward its valid target HitAnchor.
- Scatter + Hunting uses stable Center, Left, and Right slots with distinct initial targets and no slot compaction.
- Unassigned or invalid secondary slots use their preserved confirmation-time Scatter directions.
- The authoritative main target becoming invalid or leaving range cancels the complete pending attack.
- Tracking checks only the locked target and is overshoot-safe.
- Tracking ends permanently after the first hit or range/validity failure.
- A surviving Piercing Arrow continues in Direction flight and remains governed only by hit history, maximum hit count, and lifetime.
- Lifetime remains authoritative and prevents indefinite existence.
- Actual hits preserve direct damage and explicit Elemental opportunities.
- Tracking alone never applies Elemental Buffs.
- Existing Piercing-only and Scatter-only behavior does not regress.

## 10. Validation

- Exercise Hunting alone and every Piercing/Scatter/Hunting combination.
- Verify fixed slot assignment with sufficient and insufficient candidates and invalidated secondary slots.
- Kill, disable, and move the locked target or Arrow out of range during flight.
- Confirm one-way Direction fallback, current-target-only Tracking hits, and ordinary Piercing hits after fallback.
- Verify zero Monsters confirms no attack and test zero-damage valid hits and lifetime expiry.
- Run `git diff --check` and verify tracking changes remain Archer/projectile scoped.

## 11. Review Note

The fixed-slot, no-replacement initial-target rule is approved V1 behavior. Hunting never reacquires after release, and a fallback Arrow must preserve its corresponding confirmation-time Scatter direction.
