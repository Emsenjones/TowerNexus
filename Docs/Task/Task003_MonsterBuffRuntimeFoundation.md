# Task003 - Monster Buff Runtime Foundation

## Objective

Add the runtime foundation for persistent Buff state on monsters.

This task creates the system needed for duration, ticking, refresh, stacking, Buff apply cooldown, and future post-overload Protection phase, without implementing full Elemental content yet.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/12_BuffSystem.md`

## Prerequisites

- Task001 Effect trigger context and binding foundation is complete.
- Task002 Effect definition, action, and targeting foundation is complete.
- Monster damage and death flow remains stable after Task002.

## Scope

### Buff Definition Contract

Create the first Buff definition contract for persistent monster-attached state.

Buff definitions should be referenced through Inspector-assigned asset references. They should not require hand-authored string ids.

Buff definition data may include:

- Display/debug authoring information
- Duration
- Tick interval when needed
- Max stack
- Buff apply cooldown
- Tick EffectDefinition
- Element type
- Protection phase support when this Buff temporarily blocks restacking after overload

Task003 should not add stack amount per apply or refresh duration policy fields. Successful apply always adds one stack and refreshes duration to the Buff definition duration.

### Monster Buff Runtime State

MonsterBehaviour should own a plain C# Buff runtime container internally.

```text
MonsterBehaviour
    -> MonsterBuffRuntime
        -> List<MonsterBuffInstance>
```

Buff runtime containers and instances should not be MonoBehaviour components.

Runtime state should track:

- Buff definition reference
- Owner monster
- Source tower
- Source upgrade
- Remaining duration
- Tick timer
- Stack count
- Buff apply cooldown data when required

Runtime Buff state must not be stored in definition assets.

### Buff Lifecycle

The Buff runtime should support:

- Apply
- Refresh
- Stack increase
- Duration countdown
- Tick timing
- Expiration
- Removal

Buff ticks may execute an EffectDefinition through the Task002 Effect foundation.

Task003 tick context should use OnBuffTick, owner monster as TargetMonster, a valid owner hit-anchor trigger position, and ResolvedDamage = 0. Concrete tick damage authoring belongs to later content slices.

EffectActionType should add ApplyBuff only. ApplyBuff should expose BuffDefinition in the Inspector and apply it to each resolved target through MonsterBehaviour.ApplyBuff while preserving source tower and source upgrade context from EffectTriggerContext when available.

### Buff Apply Cooldown

Buff apply cooldown limits pre-overload stack frequency for the same Buff or Elemental debuff on the same monster, regardless of which tower tries to apply it.

It is not the same system as post-overload element blocking.

When Buff apply cooldown blocks an application:

- No stack is added.
- Duration is not refreshed.
- Normal phase behavior does not trigger.

When an application succeeds in the first version:

- Stack count increases by 1.
- Duration refreshes to the Buff definition duration.

Buff apply attempts should return explicit results:

- Applied
- Refreshed
- Stacked
- BlockedByBuffApplyCooldown
- BlockedByProtectionPhase
- Invalid

### Protection Phase Foundation

Post-overload element blocking should be representable as Protection phase state on a Buff runtime instance.

It blocks post-overload same-element restacking from all sources.

It does not block damage by default.

Task003 only creates the runtime capability. Specific overload behavior belongs to later Elemental tasks.

## Out Of Scope

- Elemental upgrade eligibility.
- Fire, Cold, Electric, or Wind content.
- FlameBurst.
- Frozen.
- Overcharged.
- WindVortex.
- Storm Shift.
- EffectZone runtime.
- Visual status UI.
- DamageContext migration.
- Behaviour Layer Phase 2 upgrades.

## Acceptance Criteria

- Monsters can hold persistent Buff runtime state.
- Buff runtime state tracks duration, stack count, tick timing, source tower, and source upgrade when relevant.
- Buff definitions remain static configuration and do not store runtime state.
- Buffs can expire and remove themselves without breaking Monster death or cleanup flow.
- Buff ticks can call the shared Effect execution path when configured.
- EffectActionType supports ApplyBuff without adding movement, zone, delayed, overload, or elemental reaction actions.
- Buff apply attempts return explicit result values.
- Buff apply cooldown is represented separately from post-overload element blocking.
- Buff apply cooldown blocks stack, refresh, and normal phase behavior when active.
- Protection phase can block same-BuffDefinition restacking from all sources.
- No concrete Elemental content is required for this task.
