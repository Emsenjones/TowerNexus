# Buff System

Document Set: System

## 1. System Overview

The Buff System owns persistent gameplay state attached to monsters: Active Duration and refresh, stack count, Periodic Tick timing, lifecycle events, Elemental Overload and Protection, status UI, and persistent Buff VFX.

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

BuffDefinition is reusable authored configuration. It includes display identity, Elemental type when relevant, Active Duration, optional Periodic Tick Interval, stack model, Maximum Stacks and Source Apply Cooldown when stackable, lifecycle bindings, optional Overload Protection Duration, status icon, and persistent presentation.

Mutable runtime state never lives in the definition. Each Monster owns its active Buff runtime state:

```text
Monster
    -> Active Buff Runtime
        -> Buff Instances
```

An instance includes definition reference, owner monster, remaining phase duration, Periodic Tick timer, stack count, phase, and source-scoped cooldown tracking. The latest successful source Tower and source Upgrade remain available to lifecycle Effects and diagnostics. Source Tower identity additionally selects the cooldown entry; it does not create parallel Buff state or a separate stack count. When that exact source successfully adds a stack, approved StackApplied damage may use it as the TowerScaled contributor. Shared-state lifecycle and reaction damage remains source-independent FixedBuff.

### 3.1 Two First-Version Runtime Models

| Model | Behavior |
|---|---|
| Stackable | First application starts at one stack. A successful eligible reapply refreshes Active Duration and gains one stack up to Maximum Stacks; it may trigger stack and overload behavior. |
| Non-stackable | One instance only. Reapplication refreshes Active Duration but does not add a stack, trigger overload, or create parallel state. |

Maximum Stacks, Source Apply Cooldown, and Overload Protection Duration are authored only for stackable Buffs; a stackable Buff has Maximum Stacks of at least two. Non-stackable Buffs do not expose stack, overload, or Protection authoring. PeriodicTick remains valid for either model when Periodic Tick Interval is positive.

Applications return explicit results such as Applied, Refreshed, Stacked, BlockedBySourceApplyCooldown, BlockedByProtectionPhase, or Invalid.

Buff System receives application requests after another system has resolved the relevant target and eligibility. It does not inspect the associated damage amount or DealDamage result. An ordinary unsuccessful damage action is not a Buff-level reason to reject an otherwise valid application request. The producing Tower-owned boundary is responsible for withholding the request entirely when its source Tower or TowerScaled numeric context is invalid. A Monster killed or removed before the request is no longer a valid Buff target; this is lifecycle invalidation rather than damage-result gating.

## 4. Lifecycle Effect Bindings

The lifecycle event names below are stable authoring identities, not required programming-language callback names.

Buff lifecycle bindings are universal authoring slots. Each binding selects a timing event and an EffectDefinition. The EffectDefinition contains its actions; a Buff never directly applies another Buff.

| Buff Event | Meaning |
|---|---|
| Applied | A new Buff runtime instance was created and is active on its owner |
| PeriodicTick | The Buff reaches a configured positive Periodic Tick Interval |
| StackApplied | An existing stackable Buff successfully gains one stack |
| Overload | A stackable Buff reaches Maximum Stacks |
| EnteredProtection | A stackable Elemental Buff transitions from Stacking to Protection |
| Removed | An instance is leaving its owner for any reason |

Applied, PeriodicTick, StackApplied, Overload, and EnteredProtection execute after their corresponding runtime state update. Removed executes while owner and source context remain valid, before the instance leaves the active snapshot collection and before consumers receive their state-change notification.

StackApplied is the only approved lifecycle slot whose immediate damage may be
TowerScaled from the exact Tower that contributed the current stack. Periodic,
Overload, Protection, removal, and persistent reaction damage remains FixedBuff.
This distinction changes damage authority, not Buff state ownership.

Removed covers natural expiry, explicit removal, Clear, monster death, target arrival, reset, and destruction. Add a future Expired event only when content requires natural-expiry-only behavior.

Validation rejects StackApplied, Overload, or EnteredProtection bindings on a non-stackable Buff, rejects PeriodicTick when Periodic Tick Interval is nonpositive, requires approved contributor-owned StackApplied damage to be TowerScaled, and requires other lifecycle or reaction damage to be FixedBuff.

Behaviour packages provide package-specific Effect references to their reviewed runtime boundaries. Elemental Layer authoring provides its Elemental apply Effect at the runtime-selected attack boundary. Buff lifecycle event identity decides what an already-active Buff does afterward; lifecycle bindings are not a generic attack-trigger authoring path.

## 5. Buff Feedback

MonsterStatusBar owns active-Buff UI. It shows one icon per active BuffDefinition and may show stack count above one. During Protection it keeps the icon but pulses alpha continuously from one to zero and back; no stack count appears.

Monster-owned Buff visual presentation attaches an authored persistent visual to the Monster Hit Reference, preserves one instance through stack, refresh, and Protection changes, and removes it when the Buff leaves runtime state. Protection uses the UI emphasis only in the first version.

Buff runtime exposes read-only state snapshots and state-change notifications; it does not operate UI or visual playback. Clearing a Monster removes all Buff state first, then publishes one change so consumers rebuild from an empty snapshot.

EffectDefinition execution VFX is separate: it is short-lived feedback for an executed Effect, not persistent Buff presentation.

### 5.1 Read-Only Runtime Observation

Buff System publishes immutable diagnostic observations for application attempts and logical lifecycle events. An application observation includes the Buff definition, current source Tower and Elemental Upgrade, explicit apply result, stack and phase state before and after the attempt, whether Maximum Stacks was reached, and observation time. A lifecycle observation includes the logical event even when no Effect is bound to that event.

Removal observations distinguish Active Duration expiry, Protection expiry, explicit removal, Monster death, Target arrival, technical cleanup, runtime reset, and the no-Protection overload path. Diagnostics therefore consume event history instead of trying to reconstruct expired or removed state from active snapshots.

Observations are published after the outermost Buff state mutation has completed and read-only snapshots have been refreshed. Diagnostic consumers have no gameplay authority; their absence or failure cannot change application, lifecycle Effect execution, Protection, or removal results.

FixedBuff damage diagnostics use a FixedBuff-specific signature: EffectDefinition identity, authored action ordinal, and FixedDamage. Source Tower may be retained as nullable diagnostic context, but Tower Level, Level BasicDamage, Damage Bonus, and DamageScale are not required and never participate in FixedBuff aggregation. Repeated ticks and reactions aggregate counts and totals by this signature rather than requiring one exported JSON record per result.

Contributor-owned StackApplied damage uses the ordinary TowerScaled diagnostic
signature. Its source Tower is the exact successful stack contributor, not the
Tower that originally created the shared Buff.

Each FixedBuff DealDamage execution publishes one exception-isolated read-only
observation after its synchronous target applications. The observation records
resolved-target count, successful-application count, and the authored fixed
amount; it cannot change Buff lifecycle or Monster damage.

## 6. Elemental Buff Rules

Each ElementType has one shared Elemental BuffDefinition. Tower-family Elemental upgrades only declare which Elemental apply Effect their attacks may execute. Once a Buff exists on a monster, its behavior comes solely from that shared definition.

Tower Runtime Combat and reviewed Behaviour attack extensions decide when application attempts are produced. Multiple Attack Entities, direct-plus-explosion combinations, bounce children, Arcane Field ticks, and other reviewed attack results may submit multiple attempts against the same Monster.

Buff System evaluates each request through Source Apply Cooldown, Protection, and current instance state. Source Apply Cooldown is scoped by owner Monster, BuffDefinition, and source Tower instance. All application opportunities produced by one Tower against one Monster share that Tower's cooldown entry for the Buff, regardless of which of its primary, additional, area, bounce, contact, or persistent Attack Entities produced the attempt. A different Tower has an independent cooldown entry and is never blocked by the first Tower's entry. This source-scoped gate replaces attack-entity deduplication without making one Tower suppress matching-element cooperation.

First-version Elemental Buffs are Burning, Cold, ElectricShock, and Windcut. Each is stackable and has Stacking and Protection phases:

| Phase | Meaning |
|---|---|
| Stacking | Persistent stackable behavior before overload |
| Protection | Temporary post-overload state that blocks application, refresh, and stacking for that same definition |

Rules:

1. First application creates the Buff with one stack, starts Active Duration, and starts the applying source Tower's Source Apply Cooldown.
2. A successful existing-Buff application refreshes Active Duration, records the exact contributing Tower and Upgrade, and adds one stack while below Maximum Stacks.
3. Multiple towers with the same ElementType contribute to the same shared Buff on a monster.
4. Source Apply Cooldown blocks only a repeated attempt from the same source Tower against that Monster and BuffDefinition. A blocked application does not refresh Active Duration, add a stack, or run StackApplied Effects.
5. A successful attempt from a different Tower uses its independent cooldown entry and may contribute immediately, including during the first Tower's cooldown.
6. Active Duration is measured from the latest successful application. Expiry before Maximum Stacks removes the instance and loses its stacks and source cooldown entries.
7. A successful reapply reaching Maximum Stacks runs contributor-owned StackApplied, then source-independent Overload, then enters Protection when an Overload Protection Duration is configured.
8. Entering Protection clears stacks and source cooldown entries. Protection blocks every source from applying or refreshing that same BuffDefinition until Overload Protection Duration expires and the instance is removed.
9. Protection blocks only the same BuffDefinition by default; other Elemental Buffs may still apply.

StackApplied bindings run only for a successful added stack, not first application or pure refresh. Their contributor context comes from that exact successful request. Reaching Maximum Stacks is the only Overload condition; the first version has no Primed phase, minimum-distinct-source requirement, source contribution quota, or fixed non-refreshing stacking window.

This ordering is shared Buff runtime lifecycle behavior, not an Element-specific sequence. Wind and future Elemental content must use the same dispatcher rather than adding local sequencing.

## 7. First-Version Elemental Content Contracts

| Element | Normal phase | Overload | Boundary |
|---|---|---|---|
| Fire | Burning applies persistent damage pressure | FlameBurst deals area damage around the owner | Tick and FlameBurst damage do not apply Burning by default |
| Cold | Cold slows while active | Apply Frozen Buff | Slow and movement lock use Monster-owned capabilities |
| Electric | ElectricShock makes later successful stacks deal configured extra damage | Overcharged is an instant multi-target LightningStrike execution | Lightning strikes do not apply ElectricShock by default |
| Wind | A later successful stack attacks up to one other nearby monster for authored extra damage | Maximum Stacks spawn a persistent moving WindVortex at the owner | Wind attack and Vortex damage do not apply Windcut by default |

### 7.1 Fire

Burning PeriodicTick binds a FixedBuff Effect that deals persistent damage. Its Maximum-Stacks Overload binding runs FixedBuff FlameBurst area damage. Both use authored FixedDamage rather than the source Tower's resolved BasicDamage.

### 7.2 Cold And Frozen

Cold's Applied binding requests its authored movement-speed reduction. EnteredProtection and Removed release that reduction.

Cold Maximum-Stacks Overload binds an Apply Frozen EffectDefinition. That Effect contains ApplyBuff(Frozen), which creates or refreshes Frozen through the ordinary Effect-to-Buff link.

Frozen is non-Elemental and non-stackable. Its Applied binding requests movement lock; its Removed binding releases that lock. Its duration, UI, and persistent presentation belong to its own runtime instance. Reapplying it refreshes that one instance without a parallel lock.

### 7.3 Electric

ElectricShock StackApplied binds an authored TowerScaled extra-damage Effect. It reads the current resolved BasicDamage of the Tower that successfully contributed that stack and the Effect's positive DamageScale. Because StackApplied runs only after a successful later stack, first application, pure refresh, and blocked application do not execute that damage.

ElectricShock Overload invokes the instant Overcharged Effect. Overcharged uses its authored radius to resolve nearby candidates, then ExecuteMultiTargetEffect randomly selects up to its authored target count without repetition and immediately executes one single-target LightningStrike Effect on each selected monster. Overcharged has no interval, runtime state, or persistent Buff of its own.

### 7.4 Wind

Windcut StackApplied invokes a radius-based Effect that excludes its owner, randomly selects up to one remaining valid monster, and executes an authored single-target TowerScaled Wind attack from the Tower that contributed the current stack. The initial Windcut application, pure refresh, cooldown-blocked application, Buff tick, and Protection-phase application do not run this Effect. When there is no other valid nearby monster, a configured parent execution VFX still plays at the owner trigger position, but the owner is never used as a fallback damage target.

Windcut Overload invokes SpawnWindVortex at the owner's current world position, then the existing Buff runtime enters Protection when configured. WindVortex is an Effect System-owned persistent gameplay entity; it moves itself directly between nearby valid Monsters, independently ticks area damage, and never relocates a Monster. Buff System does not modify world position, Grid Node, or path state.

## 8. Relationships And Scope

- Effect System executes lifecycle-bound Effects and owns their targeting and actions.
- Tower Upgrade System owns Elemental authoring and upgrade application rules, not Buff runtime behavior.
- Tower Runtime Combat and reviewed Behaviour runtime decide attack-boundary eligibility and supply the Elemental apply Effect at that boundary.
- Monster System owns movement, pathfinding, lifecycle cleanup, and safe requested operations.

Tower-owned direct, Behaviour, and approved StackApplied contribution damage follows the TowerScaled contract. Periodic, Overload, Protection, persistent, and other shared-state Buff or Elemental-reaction damage follows FixedBuff and ignores source-Tower BasicDamage changes. The configuration relationship remains one-directional: Buff lifecycle bindings invoke Effects, and an Effect may request another Buff. BuffDefinitions never directly reference other BuffDefinitions.

## 9. Validation

Buff authoring validation should report at minimum:

- Non-positive Active Duration
- Stackable Buff with Maximum Stacks below two
- Negative Source Apply Cooldown or Overload Protection Duration
- Stack, Overload, or Protection bindings on a non-stackable Buff
- Periodic binding with a non-positive Periodic Tick Interval
- Missing EffectDefinition in an authored lifecycle binding
- StackApplied damage whose nested DealDamage is not TowerScaled
- Other lifecycle or reaction damage whose nested DealDamage is not FixedBuff
- Elemental Buff without a valid Elemental type
- Persistent presentation or status data that is configured but unusable

Validation does not silently convert one runtime model into another.

## 10. Approved Scope And Deferred Topics

Current scope includes stackable and non-stackable Buffs, Active Duration refresh, Source Apply Cooldown, Periodic Ticks, lifecycle Effects, Elemental stacking, Overload, Protection, status presentation, persistent presentation, and the Fire, Cold, Electric, and Wind content contracts.

Deferred topics include multiple simultaneous speed modifiers, haste, Buff replacement priorities, natural-expiry-only events, dispel categories, cross-Buff dependency graphs, and recursive Elemental application.
