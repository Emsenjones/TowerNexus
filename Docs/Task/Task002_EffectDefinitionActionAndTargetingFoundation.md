# Task002 - Effect Definition, Action, And Targeting Foundation

## Objective

Implement the first reusable Effect execution foundation using simple target resolution and Effect actions.

This task turns Task001 trigger context and bindings into executable Effect behavior while keeping targeting deliberately small.

## System References

- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/04_MonsterSystem.md`

## Prerequisites

- Task001 Effect trigger context and Effect binding foundation is complete.
- Existing projectile direct hit and cannon impact behavior has been verified after Task001.

## Scope

### Effect Definition

Create the first Effect definition contract for reusable gameplay effects.

Effect definitions should be referenced directly through Inspector-assigned asset references. They should not require hand-authored string ids.

EffectDefinition is the new gameplay effect source of truth and should be used by new EffectBinding and projectile gameplay impact effect references.

Effect definitions may own:

- Display/debug authoring information
- Radius
- One or more Effect actions, or a first-version equivalent that can grow into multiple actions later

Projectile impact VFX must remain outside gameplay Effect data.

### Simple Target Resolution

First-version target resolution should remain simple.

Inputs:

- Trigger context provides TargetMonster or a TriggerPosition with explicit validity.
- Effect definition provides Radius.

Rules:

```text
If Radius <= 0 and TargetMonster exists:
    Resolve the single TargetMonster.

If Radius <= 0 and TargetMonster is missing:
    Execute nothing and log a warning.

If Radius > 0:
    Center on TargetMonster when present.
    Otherwise center on TriggerPosition only when HasTriggerPosition is true.
    Resolve all valid monsters inside Radius.

If Radius > 0 and neither TargetMonster nor valid TriggerPosition exists:
    Execute nothing and log a warning.
```

Monster inclusion should use the Monster System hit/reference anchor.

Do not build a large target-selection enum in this task.

### Effect Actions

First executable action:

- DealDamage

DealDamage should use the resolved target set and apply gameplay damage through the existing Monster damage ownership path.

ApplyBuff may be reserved as a future action connection point, but full Buff runtime belongs to Task003.

SpawnEffectZone and ApplyMovementEffect are out of scope for this task.

### Area Damage Migration Direction

Existing area damage behavior should move toward the shared EffectDefinition plus radius targeting path.

Magic Orb Splash and Cannon Behaviour Phase 2 content should eventually use this shared path rather than custom area queries inside tower or attack entity behavior.

For projectile types that use direct hit damage, existing direct hit damage executes first. Optional authored impact EffectDefinition execution happens afterward as additional gameplay effect.

## Out Of Scope

- Buff runtime.
- ApplyBuff behavior beyond a placeholder or reserved action type.
- Elemental stack rules.
- Elemental overload.
- Same-source apply cooldown.
- Element blocker Buffs.
- EffectZone runtime.
- Delayed effect execution.
- Moving EffectZone behavior.
- Magic Orb Splash implementation.
- Cannon Timed Shell implementation.
- Cannon Burning Shell implementation.
- Large targeting enum or advanced targeting strategy system.
- Base damage migration into DamageContext.

## Acceptance Criteria

- EffectDefinition can express Radius and executable Effect behavior for damage.
- Radius targeting follows the simple TargetMonster or valid TriggerPosition plus Radius rule.
- Radius <= 0 resolves single target only when TargetMonster exists.
- Radius > 0 resolves valid monsters around TargetMonster or valid TriggerPosition.
- Effect targeting uses Monster hit/reference anchors.
- DealDamage can damage resolved targets without owning Monster health rules directly.
- Projectile impact or another existing trigger can execute an EffectDefinition through the Task001 binding/context path.
- Projectile impact VFX remains owned by ProjectileConfig and Projectile System.
- Projectile impact gameplay effect references EffectDefinition directly.
- HitDistanceThreshold <= 0 disables projectile direct monster hit checks for projectile types that should resolve only impact effects.
- Projectile direct hit damage executes before optional projectile impact EffectDefinition execution.
- No large targeting enum is introduced.
- Existing direct projectile damage remains behaviorally unchanged unless an authored Effect binding explicitly adds an additional effect.
