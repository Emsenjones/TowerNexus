# Task007 - Monster Buff Visual Feedback And Debugging Foundation

## Objective

Make Buff runtime state visible and reliably debuggable without changing Buff gameplay rules.

This task creates the visual and observation foundation required before Cold, Electric, and Wind vertical slices are implemented.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

## Prerequisites

- Task006 shared Elemental entry and Fire validation are stable.
- Monster Buff runtime applies, stacks, enters Protection, removes, and clears state safely.

## Scope

### Buff State Observation

- Monster Buff runtime exposes read-only Buff state snapshots and state-change notification support.
- Consumers refresh after apply, refresh, stack, Protection transition, removal, expiry, and cleanup.
- Runtime Buff state does not operate UI or ParticleSystem behavior directly.

### MonsterStatusBar

- Evolve the existing monster health-bar presentation into MonsterStatusBar.
- Keep health display and add active Buff icon display below it.
- Display one icon slot per active BuffDefinition, not one per stack.
- Show stack count only when useful and display Protection state with the authored fallback icon rule.
- First version may rebuild the active icon slots on each Buff state refresh.

### Buff And Effect Visual Feedback

- BuffDefinition supports first-version status icon, Protection icon, persistent Buff VFX, and optional Protection VFX references.
- Monster-local Buff visual presentation owns persistent VFX lifecycle at the monster hit/reference anchor.
- EffectDefinition may provide one-shot gameplay-effect feedback for tick, overload, or special Effect execution.
- Projectile impact VFX remains owned by ProjectileConfig and Projectile System.

## Shared Constraints

- Do not alter Elemental stack, cooldown, overload, or Protection gameplay rules.
- Do not introduce pooling or final VFX polish.
- Do not create a generic ElementalBuff inheritance framework.
- Keep presentation data-driven through BuffDefinition and EffectDefinition.

## Out Of Scope

- Cold, Electric, and Wind gameplay content.
- New Elemental reactions.
- Generic UI inventory, tooltip, or status-effect framework.

## Acceptance Criteria

- Fire Buff state visibly reflects Applied, Stacked, Protection, and Removed or Expired states.
- MonsterStatusBar keeps health display behavior and correctly displays active Buff state.
- Persistent Buff VFX is created, retained through refresh or stack, updated for Protection when configured, and destroyed on removal.
- One-shot gameplay Effect VFX remains distinct from projectile impact VFX.
- Monster death, target arrival, reset, and destruction clear presentation safely.
- This foundation is stable before Task009, Task010, or Task013 begins implementation.
