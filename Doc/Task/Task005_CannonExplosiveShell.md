# Task005 - Cannon Explosive Shell

Status: Ready for implementation

Depends on: Task003

## 1. Goal

Implement Explosive Shell as an additive Position Impact Effect after the Cannon baseline optional direct result.

## 2. Source Documents

- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. In Scope

- Read the applied Explosive Shell definition and its authored area Effect.
- Pass only the resolved immutable Effect reference and required source context to each initial Cannon Shell.
- Resolve the Cannon baseline optional direct result first.
- Execute the Explosive Shell Effect at Position Impact regardless of direct-target resolution.
- Expose every valid explosion target for an independent Elemental opportunity.
- Preserve synchronous direct-before-explosion ordering.
- Support every initial Shell released by Twin Shells without special-case composition code.

## 4. Out of Scope

- Restoring a default Cannon explosion in ProjectileConfig.
- Replacing baseline direct damage with explosion damage.
- Attack-level target deduplication.
- Bouncing Shell target selection or child creation.
- Persistent explosion state or delayed damage.

## 5. Runtime Contract

```text
Cannon Position Impact
    -> resolve optional baseline direct Monster Hit
        -> direct damage
        -> direct Elemental opportunity
    -> execute authored Explosive Shell Effect at impact position
        -> resolve every valid explosion target
        -> execute Effect actions
        -> one Elemental opportunity per explosion target
    -> cleanup or continue through later reviewed Shell behavior
```

The explosion executes even when no direct Monster Hit exists. A direct Monster inside the explosion radius may receive both direct and explosion damage and two independent Elemental attempts.

## 6. Ownership Contract

- `TowerUpgradeDefinition` owns the Explosive Shell Effect reference.
- Projectile runtime owns Position Impact timing and sequencing.
- Effect System owns explosion target resolution, actions, and execution VFX.
- Buff System owns cooldown, Protection, stacks, and final application results.

## 7. Unity Authoring Checklist

- Create or configure a Cannon Explosive Shell upgrade asset with the correct package type.
- Assign a valid radius-based EffectDefinition with the intended damage action and explosion VFX.
- Keep the Cannon ProjectileConfig baseline gameplay Effect empty.
- Ensure the Effect radius and damage are authored independently from `hitDistanceThreshold` and baseline direct damage.

## 8. Acceptance Criteria

- Without Explosive Shell, Cannon produces no explosion gameplay result.
- With Explosive Shell, every Position Impact executes exactly one explosion per Shell.
- Direct damage resolves before explosion actions.
- No direct target still permits explosion damage and Elemental attempts.
- A surviving center Monster may receive direct plus explosion damage.
- Direct and explosion Elemental attempts are independent and not gated by positive damage.
- Twin Shells causes each initial Shell to execute its own explosion.
- BuffApplyCooldown and Protection, not projectile code, decide whether two attempts both apply.
- Impact and explosion VFX do not change gameplay success.

## 9. Validation

- Test direct target inside explosion radius, outside direct threshold but inside explosion radius, and no Monsters.
- Test zero direct damage, zero Effect damage fallback, cooldown blocking, and Protection blocking.
- Test Twin Shells with overlapping and separated impact positions.
- Verify target resolution and VFX occur once per impact.
- Run `git diff --check` and confirm ProjectileConfig baseline remains non-explosive.

## 10. Review Note

Task006 must wait until all synchronous direct, Elemental, explosion, Buff, overload, death, and target-state consequences from this task have completed before selecting a bounce target.
