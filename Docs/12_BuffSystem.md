# Buff System

## 1. System Overview

The Buff System owns persistent gameplay state attached to monsters: duration, refresh, stack count, periodic timing, lifecycle events, Elemental overload and Protection, status UI, and persistent Buff VFX.

A Buff is not an Effect. A BuffDefinition describes static authoring; a runtime instance owns mutable state. When a Buff needs to do something, one of its lifecycle bindings invokes an EffectDefinition through the Effect System. Buffs do not directly reference other Buffs.

```text
Effect ApplyBuff action
    -> Buff System creates, refreshes, or stacks runtime state
    -> Buff lifecycle binding invokes Effect System when configured
```

## 2. Responsibility Boundary

The Buff System owns:

- BuffDefinition validation and runtime instance lifecycle
- Apply, refresh, stack, periodic tick, Protection, and removal rules
- Elemental debuff stacks and overload transitions
- Buff lifecycle-to-Effect bindings
- Active Buff status presentation data and persistent Buff VFX lifecycle

It does not own Effect target resolution or action execution, tower attack timing, projectile behavior, Monster pathfinding, or direct Monster Transform and movement-state mutation. Lifecycle Effects request Monster System APIs through Effect actions.

## 3. Definitions And Runtime State

Buff definitions are Inspector-assigned static ScriptableObject configuration. They include display name and description, ElementType when relevant, duration, tick interval when relevant, whether the Buff uses stacks, max stacks and Buff apply cooldown when stackable, lifecycle bindings, Protection duration when applicable, status icon, and persistent Buff VFX prefab.

Mutable runtime state never lives in the definition asset. A Monster owns plain C# Buff runtime state:

```text
MonsterBehaviour
    -> MonsterBuffRuntime
        -> List<MonsterBuffInstance>
```

An instance includes definition reference, owner monster, remaining duration, tick timer, stack count, phase, and stackable-Buff apply-cooldown tracking. Source tower and source upgrade may be retained for diagnostics but never change shared Buff gameplay after application.

### 3.1 Two First-Version Runtime Models

| Model | Behavior |
|---|---|
| Stackable | First application starts at one stack. A successful eligible reapply refreshes duration and gains one stack up to max; it may trigger stack and overload behavior. |
| Non-stackable | One instance only. Reapplication refreshes duration but does not add a stack, trigger overload, or create parallel state. |

Max stacks, Buff apply cooldown, and Protection are authored only for stackable Buffs; a stackable Buff has max stacks of at least two. Non-stackable Buffs do not expose stack, overload, or Protection authoring. PeriodicTick remains valid for either model when tick interval is positive.

Applications return explicit results such as Applied, Refreshed, Stacked, BlockedByBuffApplyCooldown, BlockedByProtectionPhase, or Invalid.

## 4. Lifecycle Effect Bindings

Buff lifecycle bindings are universal authoring slots. Each binding selects a timing event and an EffectDefinition. The EffectDefinition contains its actions; a Buff never directly applies another Buff.

| Buff Event | Meaning |
|---|---|
| Applied | A new Buff runtime instance was created and is active on its owner |
| PeriodicTick | The Buff reaches a configured positive tick interval |
| StackApplied | An existing stackable Buff successfully gains one stack |
| Overload | A stackable Buff reaches max stacks |
| EnteredProtection | A stackable Elemental Buff transitions from Stacking to Protection |
| Removed | An instance is leaving its owner for any reason |

Applied, PeriodicTick, StackApplied, Overload, and EnteredProtection execute after their corresponding runtime state update. Removed executes while owner and source context remain valid, before the instance leaves the active snapshot collection and before consumers receive their state-change notification.

Removed covers natural expiry, explicit removal, Clear, monster death, target arrival, reset, and destruction. Add a future Expired event only when content requires natural-expiry-only behavior.

Validation rejects StackApplied, Overload, or EnteredProtection bindings on a non-stackable Buff, and rejects PeriodicTick when tick interval is nonpositive.

Behaviour Layer EffectBindings remain for external tower events such as OnHit. Elemental Layer authoring provides its Elemental apply Effect at the runtime-selected attack boundary. Buff lifecycle bindings decide what an already-active Buff does afterward.

## 5. Buff Feedback

MonsterStatusBar owns active-Buff UI. It shows one icon per active BuffDefinition and may show stack count above one. During Protection it keeps the icon but pulses alpha continuously from one to zero and back; no stack count appears.

MonsterBuffVisualController owns persistent world-space Buff VFX. It attaches an authored Buff VFX to the monster hit/reference anchor, preserves one instance through stack, refresh, and Protection changes, and destroys it when the Buff leaves runtime state. Protection uses the UI pulse only in the first version.

MonsterBuffRuntime exposes read-only state snapshots and state-change notifications; it does not operate UI or ParticleSystem behavior. Clear removes all state first, then emits one notification so consumers rebuild from an empty snapshot.

EffectDefinition execution VFX is separate: it is short-lived feedback for an executed Effect, not persistent Buff presentation.

## 6. Elemental Buff Rules

Each ElementType has one shared Elemental BuffDefinition. Tower-family Elemental upgrades only declare which Elemental apply Effect their attacks may execute. Once a Buff exists on a monster, its behavior comes solely from that shared definition.

First-version Elemental Buffs are Burning, Cold, ElectricShock, and Windcut. Each is stackable and has Stacking and Protection phases:

| Phase | Meaning |
|---|---|
| Stacking | Persistent stackable behavior before overload |
| Protection | Temporary post-overload state that blocks application, refresh, and stacking for that same definition |

Rules:

1. First application creates the Buff with initial stacks only.
2. A successful existing-Buff application refreshes duration and adds one stack while below max.
3. Multiple towers with the same ElementType contribute to the same shared Buff on a monster.
4. Buff apply cooldown applies across those towers; a blocked application does not refresh, stack, or run stack Effects.
5. Expiry before max removes the instance and loses its stacks.
6. A successful stack reaching max runs Overload, then enters Protection when a protection duration is configured.
7. Protection blocks only the same BuffDefinition by default; other Elemental Buffs may still apply.

StackApplied bindings run only for a successful added stack, not first application or pure refresh.

## 7. First-Version Elemental Content Contracts

| Element | Normal phase | Overload | Boundary |
|---|---|---|---|
| Fire | Burning applies persistent damage pressure | FlameBurst deals area damage around the owner | Tick and FlameBurst damage do not apply Burning by default |
| Cold | Cold slows while active | Apply Frozen Buff | Slow and movement lock use safe Monster APIs |
| Electric | ElectricShock makes later direct Electric hits deal extra damage | Overcharged is an instant sequential lightning sequence | Lightning strikes do not apply ElectricShock by default |
| Wind | A later successful stack creates WindVortex | Storm Shift requests relocation to a valid nearby node | Vortex and Storm Shift do not apply Windcut by default |

### 7.1 Fire

Burning PeriodicTick binds an Effect that deals persistent damage. Its max-stack Overload binding runs FlameBurst area damage. Both use authored Effect values rather than the source tower's resolved attack damage.

### 7.2 Cold And Frozen

Cold's Applied binding invokes `SetMoveSpeedMultiplier`. Cold's EnteredProtection and Removed bindings invoke `ClearMoveSpeedMultiplier`.

Cold max-stack Overload binds an Apply Frozen EffectDefinition. That Effect contains ApplyBuff(Frozen), which creates or refreshes Frozen through the ordinary Effect-to-Buff link.

Frozen is non-Elemental and non-stackable. Its Applied binding invokes `SetMovementLock(true)`; its Removed binding invokes `SetMovementLock(false)`. Its duration, UI, and persistent VFX belong to its own runtime instance. Reapplying it refreshes that one instance without a parallel lock.

### 7.3 Electric

ElectricShock overload invokes the instant Overcharged Effect. Overcharged selects random monsters in an authored radius and drops sequential single-target LightningStrike Effects with an authored interval.

### 7.4 Wind

Windcut StackApplied invokes SpawnWindVortex. Its overload invokes instant Storm Shift, which requests Monster System relocation to a random valid nearby, walkable node reachable to the goal, followed by path recalculation. The Buff System does not modify Transform, grid node, or path state.

## 8. Relationships And Scope

- Effect System executes lifecycle-bound Effects and owns their targeting and actions.
- Tower Upgrade System owns Elemental authoring, eligibility, and application rules, not Buff runtime behavior.
- Tower Runtime Combat supplies the Elemental apply Effect at the real attack boundary.
- Monster System owns movement, pathfinding, lifecycle cleanup, and safe requested operations.

The foundation is implemented in staged Task Documents: Effect trigger/action work, Buff runtime, shared Elemental entry, feedback, lifecycle bindings, then Cold, Electric, and Wind slices. Direct base attack damage remains independent until a DamageContext migration is explicitly reviewed.

## 9. Summary

The Buff System is the persistent state layer. Its lifecycle slots make Buff behavior data-driven while Effects remain reusable execution inserts, preserving the one-directional `Buff -> Effect -> Buff` configuration chain.
