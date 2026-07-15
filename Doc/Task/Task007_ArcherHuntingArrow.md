# Task007 - Archer Hunting Arrow

Status: Ready for implementation

Depends on: Task002

## 1. Goal

Implement Hunting Arrow as Archer-specific tracking flight with independent initial targets, source-range eligibility, hit-history exclusion, and natural composition with Piercing Arrow and Scatter Arrow.

## 2. Source Documents

- `Doc/07_TowerFrameworkSystem.md`
- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`

## 3. Current State

- `AttackArchetype.TrackingProjectile` exists but `ProjectileBehaviour` rejects it as unsupported.
- Piercing and Scatter runtime behavior already exists.
- Scatter currently originates from one tower-selected target and directional offsets.

## 4. In Scope

- Activate tracking flight only for Archer projectiles released with Hunting Arrow.
- Resolve one initial target for every released Arrow.
- Use the tower's normal initial target-selection semantics while preferring different valid Monsters when alternatives exist.
- Allow target reuse when valid Monster count is lower than released Arrow count.
- Require all initial and reacquired targets to remain inside the source tower's resolved AttackRange.
- Reacquire when the current target dies, becomes invalid, leaves range, or is hit by a Piercing Arrow that still has remaining hits.
- Exclude the Arrow's complete hit history.
- Select the nearest reacquisition candidate relative to the Arrow.
- Continue in the current movement direction with no candidate and keep trying until lifetime expires.
- Preserve independent target, tracking, hit history, piercing count, lifetime, and Elemental state per Arrow.

## 5. Out of Scope

- Homing for Cannon or Drone projectiles.
- Target prediction or leading.
- Reacquisition outside source Tower AttackRange.
- Shared target state between scattered Arrows.
- A generic tracking-target service or AttackEntity framework.
- Periodic Elemental application from movement or target acquisition.

## 6. Runtime Contract

```text
Scatter + Hunting attack confirmed
    -> resolve one initial target per Arrow
    -> prefer distinct valid targets
    -> reuse targets only when distinct candidates are insufficient
    -> release independent tracking Arrows
```

```text
Tracking Arrow update
    -> current target valid and in source range: move toward current HitAnchor position
    -> target invalid / out of range / completed piercing hit: reacquire
    -> choose nearest eligible candidate relative to Arrow, excluding hit history
    -> no candidate: continue current direction and retry later
    -> lifetime expiry: cleanup
```

Only actual Monster Hits produce direct damage and Elemental opportunities.

## 7. Ownership Contract

- Tower Runtime resolves initial target assignments and supplies immutable Hunting/Piercing options.
- Projectile runtime owns movement, validity checks, source-range checks, reacquisition, hit history, lifetime, and impact results after release.
- `ProjectileConfig` continues to own speed, hit threshold, and lifetime.
- Hunting Arrow adds no V1 authored numeric parameter.

## 8. Unity Authoring Checklist

- Create or configure an Archer Hunting Arrow upgrade asset using `ArcherHuntingArrow`.
- Reuse the existing Arrow ProjectileConfig and prefab; confirm its speed, hit threshold, and lifetime support tracking.
- Prepare scenarios with one, two, three, and more than three Monsters inside range, plus Monsters crossing the range boundary.

## 9. Acceptance Criteria

- A Hunting Arrow continuously adjusts toward its valid target HitAnchor.
- Scatter + Hunting assigns independent initial targets.
- Distinct targets are preferred when enough candidates exist.
- Target reuse occurs when candidates are insufficient.
- Every Arrow reacquires independently.
- Hit history prevents a Piercing + Hunting Arrow from returning to a previously hit Monster.
- Reacquisition uses nearest-to-Arrow distance and source-range eligibility.
- With no candidate, the Arrow continues forward and can reacquire later.
- Lifetime remains authoritative and prevents indefinite existence.
- Actual hits preserve direct damage and explicit Elemental opportunities.
- Tracking alone never applies Elemental Buffs.
- Existing Piercing-only and Scatter-only behavior does not regress.

## 10. Validation

- Exercise Hunting alone and every Piercing/Scatter/Hunting combination.
- Verify independent initial targets with sufficient and insufficient candidates.
- Kill, disable, move out of range, and reintroduce targets during flight.
- Confirm hit-history exclusion and nearest-to-projectile reacquisition.
- Test zero-damage valid hits and lifetime expiry with no candidates.
- Run `git diff --check` and verify tracking changes remain Archer/projectile scoped.

## 11. Review Note

The independent initial-target rule is approved V1 behavior and must not be replaced by accidental reuse of one shared pending tower target.
