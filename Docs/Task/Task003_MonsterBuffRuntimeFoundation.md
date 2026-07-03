# Task003 - Monster Buff Runtime Foundation

## Objective

Add the runtime foundation for persistent Buff state on monsters.

This task creates the system needed for duration, ticking, refresh, stacking, same-source apply cooldown, and ElementalStackImmunity, without implementing full Elemental content yet.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

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
- Stackability
- Max stack
- Stack amount per apply
- Refresh duration on reapply
- Same-source apply cooldown
- Normal phase Effect reference when needed later
- Overload Effect reference when needed later
- Remove on overload
- Same-element stack immunity duration

The exact field list should stay as small as the implementation plan can support.

### Monster Buff Runtime State

Monster runtime should own or expose a Buff runtime container.

Runtime state should track:

- Buff definition reference
- Owner monster
- Source tower
- Source upgrade
- Remaining duration
- Tick timer
- Stack count
- Same-source cooldown data when required

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

### Same-Source Apply Cooldown

Same-source apply cooldown limits pre-overload stack frequency from the same tower to the same monster for the same Buff or Elemental debuff.

It is not the same system as ElementalStackImmunity.

When same-source cooldown blocks an application:

- No stack is added.
- Duration is not refreshed.
- Normal phase behavior does not trigger.

### ElementalStackImmunity Foundation

ElementalStackImmunity should be representable as a system-level Buff or equivalent runtime state.

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
- Same-source apply cooldown is represented separately from ElementalStackImmunity.
- Same-source cooldown blocks stack, refresh, and normal phase behavior when active.
- ElementalStackImmunity can be represented as a system-level state that blocks same-element restacking from all sources.
- No concrete Elemental content is required for this task.
