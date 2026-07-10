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
- Active snapshots contain only currently existing Buff runtime states.
- Applied, Refreshed, Stacked, and EnteredProtection notifications occur after the runtime state has been updated.
- Removed and Expired notifications occur after the affected state has been removed from the active snapshot collection.
- Presentation consumers refresh by rereading the complete snapshot collection. They do not reproduce Buff state transitions themselves.
- Clear removes all Buff runtime states first, then emits one Clear notification. Consumers rebuild from the resulting empty snapshot collection instead of receiving one refresh per removed Buff.
- Runtime Buff state does not operate UI or ParticleSystem behavior directly.

### MonsterStatusBar

- Evolve the existing monster health-bar presentation into MonsterStatusBar.
- Keep health display and add active Buff icon display below it.
- Display one icon slot per active BuffDefinition, not one per stack.
- Show stack count only when useful and display Protection state with the authored fallback icon rule.
- First version may rebuild the active icon slots on each Buff state refresh.

### Buff And Effect Visual Feedback

- BuffDefinition supports first-version status icon, Protection icon, and persistent Buff VFX references.
- Protection is represented by the Protection status icon, with normal status icon fallback. The normal persistent Buff VFX remains active during Protection and is not replaced.
- Monster-local Buff visual presentation owns persistent VFX lifecycle at the monster hit/reference anchor.
- EffectDefinition may provide one-shot gameplay-effect feedback for a successfully executed tick, overload, or special Effect action.
- A rejected ApplyBuff action, including Buff apply cooldown rejection, does not spawn one-shot Effect VFX.
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
- Persistent Buff VFX is created, retained through refresh, stack, and Protection, and destroyed on removal.
- Protection uses the authored Protection status icon with normal icon fallback; it does not replace the persistent Buff VFX.
- One-shot gameplay Effect VFX remains distinct from projectile impact VFX and spawns only when its Effect action succeeds.
- Buff apply cooldown rejection causes no StatusBar or one-shot Effect VFX change.
- Monster death, target arrival, reset, and destruction clear runtime states first, then clear presentation from one resulting Clear notification.
- This foundation is stable before Task009, Task010, or Task013 begins implementation.
