# Task011 - Drone Final Dive Lifecycle

Status: Ready for implementation

Depends on: Task002

Recommended implementation order: After Task010

## 1. Goal

Implement the approved Drone battery lifecycle and Final Dive state: no battery drain while Launching, VFX-only aerial despawn when no gameplay impact is available, dynamic locked-target pursuit with last-valid-position fallback, optional local direct damage, additive explosion, and final cleanup.

## 2. Source Documents

- `Doc/07_TowerFrameworkSystem.md`
- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. Current State

- Drone runtime has only `Launching` and `Orbiting` states.
- Battery currently drains during Launching.
- Battery depletion directly destroys the Drone with no reviewed Final Dive branch.
- Position arrival currently uses one hard-coded local threshold.

## 4. In Scope

- Stop battery consumption during `Launching`.
- Consume battery while actively `Orbiting`.
- Play VFX-only aerial explosion feedback before despawn when Final Dive is absent and battery depletes.
- At natural Orbiting battery depletion with Final Dive, validate the current target before ordinary retargeting.
- Invalid current target: VFX-only aerial explosion and despawn without reacquisition.
- Valid current target: lock its reference, store its current HitAnchor position, stop firing, and enter `FinalDiving`.
- While valid, dynamically pursue the target's current HitAnchor and refresh `lastValidHitPosition`.
- If it becomes invalid, continue toward `lastValidHitPosition` without reacquiring or canceling.
- Ignore source Tower AttackRange after FinalDiving begins.
- Produce Position Impact when the Drone enters the positive package-owned `finalDiveHitThreshold` around its active destination.
- Search around the actual impact position within `finalDiveHitThreshold` and select the nearest valid Monster.
- Resolve optional direct damage from the Drone's release-time resolved attack damage and one direct Elemental opportunity.
- Always execute the authored Final Dive explosion after the optional direct result.
- Despawn after every synchronous direct, Effect, Elemental, Buff, overload, death, and target-state consequence completes.

## 5. Out of Scope

- Battery drain during Launching or FinalDiving.
- Final Dive retargeting.
- Canceling because the locked target leaves source Tower AttackRange.
- Reusing `ProjectileConfig.hitDistanceThreshold`.
- Using explosion radius as direct-hit threshold.
- Adding a Final Dive projectile prefab.
- Generic Drone state-machine framework or visual flight-tilt redesign.

## 6. Runtime Contract

```text
Battery naturally depletes while Orbiting
    -> Final Dive absent: VFX-only aerial explosion -> despawn
    -> Final Dive active:
        -> current target invalid: VFX-only aerial explosion -> despawn
        -> current target valid:
            -> lock target reference
            -> store current HitAnchor position
            -> stop firing
            -> enter FinalDiving
```

```text
FinalDiving
    -> valid target: pursue current HitAnchor and refresh last-valid position
    -> invalid target: pursue last-valid position
    -> distance <= finalDiveHitThreshold: Position Impact
    -> query nearest valid Monster within finalDiveHitThreshold
        -> found: Monster Hit -> direct damage -> direct Elemental opportunity
        -> none: no direct result
    -> execute Final Dive explosion at impact position
    -> one Elemental opportunity per valid explosion target
    -> despawn
```

A Monster may receive direct plus explosion damage and two independent Elemental attempts. If direct damage kills it, it is not a valid explosion target, but the explosion still executes.

## 7. Ownership Contract

- `DroneBehaviour` owns state, movement, target reference, last-valid position, local direct-target resolution, ordering, and cleanup.
- `TowerUpgradeDefinition` owns positive `finalDiveHitThreshold` and the Final Dive explosion Effect reference.
- The Drone stores release-time resolved attack damage.
- Effect System owns explosion radius resolution, actions, and execution VFX.
- Drone prefab/runtime presentation owns the VFX-only aerial despawn prefab or reference; it is not a gameplay Effect.
- Buff System owns final Elemental application outcomes.

## 8. Unity Authoring Checklist

- Create or configure a Drone Final Dive upgrade asset.
- Author a positive `finalDiveHitThreshold` and valid radius-based impact EffectDefinition.
- Configure explosion damage/radius and execution VFX on the EffectDefinition.
- Configure the Drone prefab's VFX-only aerial explosion presentation reference.
- Ensure the Drone prefab supports the existing FireAnchor and movement presentation while adding no gameplay logic to visual children.
- Prepare short battery-duration configurations for repeatable Play Mode validation.

## 9. Acceptance Criteria

- Launching never decreases battery and cannot trigger Final Dive.
- Baseline battery depletion produces one VFX-only aerial explosion and no gameplay damage.
- Final Dive battery handling occurs only after natural Orbiting depletion.
- Invalid current target at depletion does not reacquire.
- FinalDiving stops burst fire and never returns to Orbiting.
- A valid locked target is dynamically tracked without a source-range requirement.
- Target invalidation switches to the last valid position.
- Arrival uses `finalDiveHitThreshold`, not exact position equality.
- Position Impact always occurs at arrival.
- Local query selects at most one nearest valid Monster.
- A resolved direct target receives release-time direct damage and one direct Elemental opportunity.
- No resolved direct target means no direct damage, but the explosion still executes.
- Explosion targets receive independent Elemental opportunities.
- A surviving center target may receive direct plus explosion results.
- Twin Drones resolve battery and Final Dive independently.
- Drone despawns only after all synchronous impact results complete.

## 10. Validation

- Test long Launching duration and confirm battery remains unchanged.
- Test baseline battery depletion, invalid target at depletion, valid moving target, target leaving range, target dying during dive, and another Monster near last-valid position.
- Test direct target survival/death, no direct target, empty explosion, and multiple explosion targets.
- Test zero damage, BuffApplyCooldown, Protection, overload/death consequences, and Twin Drones.
- Confirm aerial VFX-only despawn never executes gameplay Effects.
- Run `git diff --check` and inspect state transition and impact ordering.

## 11. Review Note

Final Dive intentionally shares Cannon's `Position Impact -> local nearest direct target -> additive explosion` shape, but it owns its own threshold and Drone lifecycle rather than using ProjectileConfig.
