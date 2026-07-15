# Task008 - Magic Arcane Detonation

Status: Ready for implementation

Depends on: Task002

## 1. Goal

Implement Arcane Detonation through explicit Magic Orb end reasons so the authored area Effect executes only after normal gameplay completion and never after forced cleanup.

## 2. Source Documents

- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. Current State

`MagicOrbBehaviour` has one parameterless end path and an `OnEnded` event. Lifetime expiry and hit-count exhaustion are not semantically distinguishable, and cleanup paths have no reviewed reason contract.

## 4. In Scope

- Introduce explicit Magic Orb end reasons.
- Route every Orb termination through one idempotent end/cleanup entry.
- Distinguish at least `HitCountExhausted`, `LifetimeExpired`, and non-triggering forced cleanup categories.
- Resolve the applied Arcane Detonation Effect into each Orb's immutable release data.
- Execute Detonation at the Orb's current world position only for `HitCountExhausted` and `LifetimeExpired`.
- Complete the final contact direct result before a hit-count Detonation.
- Resolve one Elemental opportunity for every valid Detonation target.
- Preserve independent end reason and Detonation execution for Twin Orbs.

## 5. Out of Scope

- Detonation on `OnDestroy()` without a semantic reason.
- Detonation after battle cleanup, reset, owner invalidation, forced removal, or failed initialization.
- Shared lifetime or hit-count state between Twin Orbs.
- Changing Magic Orb contact cooldown, orbit movement, or baseline contact damage.
- Generic Attack Entity end-reason framework.

## 6. Runtime Contract

```text
Orb contact consumes final hit
    -> contact direct damage
    -> contact Elemental opportunity
    -> EndOrb(HitCountExhausted)
    -> Arcane Detonation at current world position
    -> Elemental opportunity per resolved Detonation target
    -> cleanup
```

```text
Lifetime reaches maximum
    -> EndOrb(LifetimeExpired)
    -> Arcane Detonation
    -> cleanup

Forced cleanup reason
    -> no Detonation
    -> cleanup
```

The end entry must be idempotent so one Orb cannot detonate twice.

## 7. Ownership Contract

- Magic Orb runtime owns end reason, current position, end ordering, and cleanup.
- `TowerUpgradeDefinition` owns the Arcane Detonation Effect reference.
- Effect System owns area resolution, actions, and execution VFX.
- Buff System owns the final result of each Elemental application request.

## 8. Unity Authoring Checklist

- Create or configure a Magic Arcane Detonation upgrade asset.
- Assign a valid radius-based Detonation EffectDefinition with intended damage and VFX.
- Ensure the Magic Orb prefab continues to contain `MagicOrbBehaviour`.
- Prepare short-lifetime and low-hit-count test configurations for both normal completion paths.

## 9. Acceptance Criteria

- Hit-count exhaustion triggers exactly one Detonation after the final contact result.
- Lifetime expiry triggers exactly one Detonation.
- Forced cleanup categories never trigger Detonation.
- Every termination path reaches the same semantic end/cleanup entry.
- Detonation uses the Orb's current world position.
- Each valid Detonation target receives an independent Elemental opportunity even when damage is zero.
- Twin Orbs detonate independently and may end for different reasons.
- Cleanup destroys the Orb once and does not reenter the end path.
- Baseline Magic Orb behavior is unchanged when Arcane Detonation is absent.

## 10. Validation

- Test hit exhaustion, lifetime expiry, battle cleanup, owner removal, reset, failed initialization, and explicit forced cleanup.
- Test Twin Orbs with staggered completion reasons.
- Confirm final contact ordering before Detonation.
- Confirm no `OnDestroy()` inference or duplicate VFX.
- Run `git diff --check` and verify no generic end-reason framework was added.

## 11. Review Note

Any new Orb termination path added later must choose an explicit semantic reason before calling the shared end entry.
