# Task003 - Cannon Baseline Position Impact

Status: Implementation complete; Unity Play Mode validation pending

Depends on: Task002

## 1. Goal

Replace the old Cannon baseline explosion path with the approved position-snapshot Arc Shell baseline: Position Impact, one local nearest-valid-Monster query, optional direct damage, and optional direct Elemental application.

This task must produce a complete playable Cannon baseline before any Cannon Behaviour upgrade is implemented.

## 2. Source Documents

- `Doc/07_TowerFrameworkSystem.md`
- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`

## 3. Current State

- Cannon captures one target position before release.
- Projectile release currently cancels when the original target becomes invalid during animation wait.
- Arc arrival raises impact with no direct Monster target.
- Cannon direct/Elemental resolution currently depends on an authored impact Effect target set.

## 4. In Scope

- Confirm and store an immutable Cannon target-position snapshot when entering `WaitingForAnimationRelease`.
- Represent snapshot presence explicitly; never use `Vector3.zero` as a missing-position sentinel.
- Do not cancel the confirmed Arc release when the source Monster later becomes invalid.
- Reject nonpositive Arc `hitDistanceThreshold` before projectile instantiation with a warning emitted once per tower initialization, while keeping a defensive projectile-side validation.
- Travel toward the stored position without retargeting.
- Complete normalized Arc travel, snap exactly to the stored target-position snapshot, and use `ProjectileConfig.hitDistanceThreshold` only as the local Monster-query radius.
- At Position Impact, select at most one nearest valid Monster around the actual impact position.
- Resolve direct damage using the release-time resolved attack damage.
- Produce one direct Elemental opportunity for the resolved Monster.
- Preserve impact VFX and cleanup when no Monster is resolved.
- Remove the Cannon baseline dependency on an area gameplay Effect.
- Preserve the optional direct Monster in `ProjectileImpactContext`, but create Arc Position Impact Effect context with no target Monster and the actual impact position so later area Effects remain centered on the landing.

## 5. Out of Scope

- Twin Shells, Explosive Shell, or Bouncing Shell behavior.
- Generic multi-projectile snapshot storage.
- Changing Direction Projectile invalidation rules.
- Generic collision or physics-based Cannon targeting.
- New explosion radius or damage data.

## 6. Runtime Contract

```text
Confirm Cannon attack
    -> capture target HitAnchor position
    -> mark the position snapshot present
    -> enter WaitingForAnimationRelease
    -> validate Arc configuration before instantiation
    -> release Arc Shell even if original Monster later becomes invalid
    -> travel to captured position
    -> Position Impact
    -> query nearest valid Monster within hitDistanceThreshold
        -> found: Monster Hit -> direct damage -> direct Elemental opportunity
        -> none: no direct result
    -> impact feedback
    -> cleanup
```

Direct damage and its Elemental opportunity remain independent results. Position Impact alone does not globally imply damage.

An Arc landing that resolves a direct Monster produces both facts without conflating their Effect data:

```text
ProjectileImpactContext
    -> actual impact position
    -> optional resolved direct Monster

Arc Position Impact EffectTriggerContext
    -> TargetMonster is null
    -> TriggerPosition is the actual impact position

Direct Elemental opportunity
    -> explicitly targets the resolved direct Monster
```

## 7. Ownership Contract

- Tower Runtime owns confirmation, the stored position, animation wait, one release presentation, and cooldown.
- Projectile runtime owns Arc movement, arrival, local query, direct result, impact feedback, and cleanup.
- `ProjectileConfig` owns speed, lifetime, `hitDistanceThreshold`, and presentation VFX.
- Cannon baseline does not require an EffectDefinition.

## 8. Unity Authoring Checklist

- Update the Cannon ProjectileConfig so `hitDistanceThreshold` is positive and appropriate for the local direct-target search around the completed landing position.
- Remove the old baseline explosion gameplay Effect reference from the Cannon ProjectileConfig.
- Preserve the existing impact VFX reference.
- Confirm the Cannon AttackConfig still uses ArcProjectile and a valid Arc height.

Asset changes may be completed by the user, but they are required before final Play Mode acceptance.

## 9. Acceptance Criteria

- Cannon fires after animation release even if the original Monster died during the wait.
- The Shell never retargets after confirmation.
- A valid world-origin target position is not mistaken for a missing snapshot.
- Invalid Arc hit distance is rejected before projectile instantiation and does not create repeated failed Shell objects.
- Arrival always produces Position Impact and impact feedback.
- The nearest valid Monster within `hitDistanceThreshold` receives one direct hit.
- A Monster outside the threshold receives nothing.
- No nearby Monster means no direct damage and no direct Elemental opportunity.
- Zero direct damage does not suppress an otherwise eligible direct Elemental opportunity.
- No default area explosion occurs.
- Future Position Impact Effects remain centered on the actual landing even when a direct Monster is resolved.
- Cooldown begins once on successful release, not at impact.
- Archer Direction Projectiles retain their current invalidation behavior.

## 10. Validation

- Test original target valid, dead during animation wait, moved away, and replaced by another nearer Monster at impact.
- Test no Monster within threshold.
- Test a valid target-position snapshot at `Vector3.zero`.
- Test nonpositive `hitDistanceThreshold` and confirm no projectile instance is created and the warning is not repeated every frame.
- Test zero direct damage with an Elemental upgrade.
- Confirm impact VFX plays exactly once and no gameplay explosion executes.
- Run `git diff --check` and verify no Cannon Behaviour packages were implemented early.

## 11. Review Note

Task004 and Task005 branch from this baseline. Task006 depends on the direct Monster Hit fact created here.
