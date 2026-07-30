# Buff System

Document Set: System

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

It does not own Effect target resolution or action execution, Tower attack timing, Behaviour Elemental eligibility, projectile behavior, Monster pathfinding, or direct Monster position and movement-state mutation. Lifecycle Effects request Monster System capabilities through Effect actions.

## 3. Definitions And Runtime State

BuffDefinition is reusable authored configuration. It includes display identity, Elemental type when relevant, duration, optional tick interval, stack model, maximum stacks and apply cooldown when stackable, lifecycle bindings, optional Protection duration, status icon, and persistent presentation.

Mutable runtime state never lives in the definition. Each Monster owns its active Buff runtime state:

```text
Monster
    -> Active Buff Runtime
        -> Buff Instances
```

An instance includes definition reference, owner monster, remaining duration, tick timer, stack count, phase, and stackable-Buff apply-cooldown tracking. Source tower and source upgrade may be retained for diagnostics but never change shared Buff gameplay after application.

### 3.1 Two First-Version Runtime Models

| Model | Behavior |
|---|---|
| Stackable | First application starts at one stack. A successful eligible reapply refreshes duration and gains one stack up to max; it may trigger stack and overload behavior. |
| Non-stackable | One instance only. Reapplication refreshes duration but does not add a stack, trigger overload, or create parallel state. |

Max stacks, Buff apply cooldown, and Protection are authored only for stackable Buffs; a stackable Buff has max stacks of at least two. Non-stackable Buffs do not expose stack, overload, or Protection authoring. PeriodicTick remains valid for either model when tick interval is positive.

Applications return explicit results such as Applied, Refreshed, Stacked, BlockedByBuffApplyCooldown, BlockedByProtectionPhase, or Invalid.

Buff System receives application requests after another system has resolved the relevant target and eligibility. It does not inspect the associated damage amount or DealDamage result. Zero damage, non-positive damage, or unsuccessful damage execution is not a Buff-level reason to reject an otherwise valid application request. A Monster killed or removed before the request is no longer a valid Buff target; this is lifecycle invalidation rather than damage-result gating.

## 4. Lifecycle Effect Bindings

The lifecycle event names below are stable authoring identities, not required programming-language callback names.

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

Behaviour packages provide package-specific Effect references to their reviewed runtime boundaries. Elemental Layer authoring provides its Elemental apply Effect at the runtime-selected attack boundary. Buff lifecycle event identity decides what an already-active Buff does afterward; lifecycle bindings are not a generic attack-trigger authoring path.

## 5. Buff Feedback

MonsterStatusBar owns active-Buff UI. It shows one icon per active BuffDefinition and may show stack count above one. During Protection it keeps the icon but pulses alpha continuously from one to zero and back; no stack count appears.

Monster-owned Buff visual presentation attaches an authored persistent visual to the Monster Hit Reference, preserves one instance through stack, refresh, and Protection changes, and removes it when the Buff leaves runtime state. Protection uses the UI emphasis only in the first version.

Buff runtime exposes read-only state snapshots and state-change notifications; it does not operate UI or visual playback. Clearing a Monster removes all Buff state first, then publishes one change so consumers rebuild from an empty snapshot.

EffectDefinition execution VFX is separate: it is short-lived feedback for an executed Effect, not persistent Buff presentation.

## 6. Elemental Buff Rules

Each ElementType has one shared Elemental BuffDefinition. Tower-family Elemental upgrades only declare which Elemental apply Effect their attacks may execute. Once a Buff exists on a monster, its behavior comes solely from that shared definition.

Tower Runtime Combat and reviewed Behaviour attack extensions decide when application attempts are produced. Multiple Attack Entities, direct-plus-explosion combinations, bounce children, Arcane Field ticks, and other reviewed attack results may submit multiple attempts against the same Monster. Buff System evaluates each request independently through BuffApplyCooldown, Protection, and current instance state; it does not add an attack-level hard deduplication rule.

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
6. A successful reapply reaching max runs StackApplied, then Overload, then enters Protection when a protection duration is configured.
7. Protection blocks only the same BuffDefinition by default; other Elemental Buffs may still apply.

StackApplied bindings run only for a successful added stack, not first application or pure refresh.

This ordering is shared Buff runtime lifecycle behavior, not an Element-specific sequence. Wind and future Elemental content must use the same dispatcher rather than adding local sequencing.

## 7. First-Version Elemental Content Contracts

| Element | Normal phase | Overload | Boundary |
|---|---|---|---|
| Fire | Burning applies persistent damage pressure | FlameBurst deals area damage around the owner | Tick and FlameBurst damage do not apply Burning by default |
| Cold | Cold slows while active | Apply Frozen Buff | Slow and movement lock use Monster-owned capabilities |
| Electric | ElectricShock makes later successful stacks deal configured extra damage | Overcharged is an instant multi-target LightningStrike execution | Lightning strikes do not apply ElectricShock by default |
| Wind | A later successful stack attacks up to one other nearby monster for authored extra damage | Max stacks spawn a persistent moving WindVortex at the owner | Wind attack and Vortex damage do not apply Windcut by default |

### 7.1 Fire

Burning PeriodicTick binds an Effect that deals persistent damage. Its max-stack Overload binding runs FlameBurst area damage. Both use authored Effect values rather than the source tower's resolved attack damage.

### 7.2 Cold And Frozen

Cold's Applied binding requests its authored movement-speed reduction. EnteredProtection and Removed release that reduction.

Cold max-stack Overload binds an Apply Frozen EffectDefinition. That Effect contains ApplyBuff(Frozen), which creates or refreshes Frozen through the ordinary Effect-to-Buff link.

Frozen is non-Elemental and non-stackable. Its Applied binding requests movement lock; its Removed binding releases that lock. Its duration, UI, and persistent presentation belong to its own runtime instance. Reapplying it refreshes that one instance without a parallel lock.

### 7.3 Electric

ElectricShock StackApplied binds an authored extra-damage Effect. Because StackApplied runs only after a successful later stack, first application, pure refresh, and blocked application do not execute that damage.

ElectricShock Overload invokes the instant Overcharged Effect. Overcharged uses its authored radius to resolve nearby candidates, then ExecuteMultiTargetEffect randomly selects up to its authored target count without repetition and immediately executes one single-target LightningStrike Effect on each selected monster. Overcharged has no interval, runtime state, or persistent Buff of its own.

### 7.4 Wind

Windcut StackApplied invokes a radius-based Effect that excludes its owner, randomly selects up to one remaining valid monster, and executes an authored single-target Wind attack. The initial Windcut application, pure refresh, cooldown-blocked application, Buff tick, and Protection-phase application do not run this Effect. When there is no other valid nearby monster, a configured parent execution VFX still plays at the owner trigger position, but the owner is never used as a fallback damage target.

Windcut Overload invokes SpawnWindVortex at the owner's current world position, then the existing Buff runtime enters Protection when configured. WindVortex is an Effect System-owned persistent gameplay entity; it moves itself directly between nearby valid Monsters, independently ticks area damage, and never relocates a Monster. Buff System does not modify world position, Grid Node, or path state.

## 8. Relationships And Scope

- Effect System executes lifecycle-bound Effects and owns their targeting and actions.
- Tower Upgrade System owns Elemental authoring and upgrade application rules, not Buff runtime behavior.
- Tower Runtime Combat and reviewed Behaviour runtime decide attack-boundary eligibility and supply the Elemental apply Effect at that boundary.
- Monster System owns movement, pathfinding, lifecycle cleanup, and safe requested operations.

Direct base attack damage remains independent unless a future unified damage-context design is explicitly reviewed. The configuration relationship remains one-directional: Buff lifecycle bindings invoke Effects, and an Effect may request another Buff. BuffDefinitions never directly reference other BuffDefinitions.

## 9. Validation

Buff authoring validation should report at minimum:

- Non-positive duration
- Stackable Buff with maximum stacks below two
- Negative apply cooldown or Protection duration
- Stack, Overload, or Protection bindings on a non-stackable Buff
- Periodic binding with a non-positive tick interval
- Missing EffectDefinition in an authored lifecycle binding
- Elemental Buff without a valid Elemental type
- Persistent presentation or status data that is configured but unusable

Validation does not silently convert one runtime model into another.

## 10. Approved Scope And Deferred Topics

Current scope includes stackable and non-stackable Buffs, duration refresh, apply cooldown, periodic ticks, lifecycle Effects, Elemental stacking, Overload, Protection, status presentation, persistent presentation, and the Fire, Cold, Electric, and Wind content contracts.

Deferred topics include multiple simultaneous speed modifiers, haste, Buff replacement priorities, natural-expiry-only events, dispel categories, cross-Buff dependency graphs, and recursive Elemental application.
