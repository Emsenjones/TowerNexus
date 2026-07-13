# Task009-1 - Buff Lifecycle Effect Binding Refactor

## Objective

Refactor BuffDefinition authoring so Buff lifecycle slots select EffectDefinition behavior, while preserving the existing Buff and Effect ownership boundary.

This task makes Fire the regression-validation content for the refactor. Cold and Frozen content remain deferred to Task009.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/12_BuffSystem.md`

## Prerequisites

- Task003 Monster Buff runtime foundation is stable.
- Task005 and Task006 Fire vertical-slice behavior is stable.
- Task007 Buff feedback foundation is stable.
- Task008 Monster slow and Frozen control API is stable.

## Scope

### Buff Runtime Models

- BuffDefinition explicitly distinguishes stackable and non-stackable Buffs.
- Stackable Buffs author max stacks and shared per-monster apply cooldown, gain one stack on a successful reapply, and can trigger stack, overload, and Protection behavior.
- Non-stackable Buffs keep one runtime instance; a successful reapply refreshes duration only.
- Non-stackable Buffs do not author or execute stack, overload, or Protection behavior.

### Lifecycle Effect Bindings

- Buff event bindings support Applied, PeriodicTick, StackApplied, Overload, EnteredProtection, and Removed.
- Each lifecycle slot references an EffectDefinition; BuffDefinitions do not directly reference other BuffDefinitions.
- Removed runs for every runtime-state exit: expiry, explicit removal, Clear, death, target arrival, reset, and destroy.
- Runtime state updates occur before their corresponding non-Removed lifecycle Effect. Removed retains valid owner context while its Effect executes, then leaves active Buff state before presentation refresh.

### Movement Effect Actions

- Effect actions use `SetMoveSpeedMultiplier` and `ClearMoveSpeedMultiplier` for the current reduction-only move-speed slot, plus `SetMovementLock(bool)` for Frozen lock state, through Task008's Monster System APIs.
- The multiplier must satisfy `0 < multiplier < 1`; movement lock remains a separate semantic action that Monster System resolves to zero effective speed.
- Effect actions do not directly modify a Monster Transform, current node, path, or stored effective speed.

### Fire Migration And Validation

- Migrate Burning to the lifecycle-binding model without changing its gameplay identity.
- Burning PeriodicTick continues to execute periodic damage.
- Burning Overload continues to execute FlameBurst area damage, then enters its existing Protection phase.
- Existing stack, cooldown, Protection, Buff UI, and VFX behavior remains valid after migration.

## Shared Constraints

- Do not create Cold, Frozen, Electric, Wind, or TowerUpgrade content in this task.
- Do not create a generic movement-modifier, crowd-control, or ElementalBuff inheritance framework.
- Do not let Effect System or Buff System directly change Monster movement, Transform, grid node, or path state.
- Do not change base attack damage ownership.

## Out Of Scope

- Task009 Cold and Frozen content.
- Task011 Storm Shift relocation.
- EffectZone or WindVortex work.
- Behaviour Layer Phase 2 upgrades.

## Acceptance Criteria

- Inspector authoring clearly separates stackable from non-stackable Buff configuration.
- Validation rejects stack, overload, and Protection bindings on a non-stackable Buff, plus PeriodicTick when tick interval is not positive.
- Applied and Removed lifecycle Effects run exactly once per corresponding Buff instance transition.
- Removed cleanup executes for natural expiry, explicit removal, Clear, death, target arrival, reset, and destroy.
- Movement lifecycle actions request only Task008 Monster System APIs and preserve Monster ownership.
- Burning retains its existing periodic damage, stacking, cooldown, FlameBurst, Protection, UI, and VFX behavior.
- No Cold, Frozen, Electric, Wind, or new TowerUpgrade assets are introduced.
