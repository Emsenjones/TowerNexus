# Buff System

Document Set: System

## 1. System Overview

The Buff System owns persistent gameplay state attached to monsters: Active Duration and refresh, stack count, Periodic Tick timing, lifecycle events, per-instance Elemental hit-reaction cooldown, Elemental Overload and Protection, status UI, and persistent Buff VFX.

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
- Shared Electric/Wind Elemental hit-reaction eligibility and per-instance
  cooldown state after receiving an explicit Tower-owned hit fact
- Active Buff status presentation data and persistent Buff VFX lifecycle

It does not own Effect target resolution or action execution, Tower attack timing, Behaviour Elemental eligibility, projectile behavior, Monster pathfinding, or direct Monster position and movement-state mutation. Lifecycle Effects request Monster System capabilities through Effect actions.

## 3. Definitions And Runtime State

BuffDefinition is reusable authored configuration. It includes display identity, Elemental type when relevant, Active Duration, optional Periodic Tick Interval, stack model, Maximum Stacks and Source Apply Cooldown when stackable, lifecycle bindings, optional positive Elemental hit-reaction cooldown, optional Overload Protection Duration, status icon, and persistent presentation.

Mutable runtime state never lives in the definition. Each Monster owns its active Buff runtime state:

```text
Monster
    -> Active Buff Runtime
        -> Buff Instances
```

An instance includes definition reference, owner monster, remaining phase duration, Periodic Tick timer, stack count, phase, source-scoped application cooldown tracking, optional shared Elemental hit-reaction cooldown, and a Stacking-cycle identity. The latest successful source Tower and source Upgrade remain available to lifecycle Effects and diagnostics. Source Tower identity selects the application-cooldown entry; it does not create parallel Buff state, a separate stack count, or a reaction-damage authority. Shared-state lifecycle and Elemental hit-reaction damage remains source-independent FixedBuff.

### 3.1 Two First-Version Runtime Models

| Model | Behavior |
|---|---|
| Stackable | A successful request atomically adds its explicit positive stack-unit contribution up to Maximum Stacks. First application runs Applied but never StackApplied; a successful reapply that commits units runs StackApplied once and may then overload. |
| Non-stackable | One instance only. Reapplication refreshes Active Duration but does not add a stack, trigger overload, or create parallel state. |

Maximum Stacks, Source Apply Cooldown, and Overload Protection Duration are authored only for stackable Buffs; a stackable Buff has Maximum Stacks of at least two. Non-stackable Buffs do not expose stack, overload, or Protection authoring. PeriodicTick remains valid for either model when Periodic Tick Interval is positive.

Applications return explicit results such as Applied, Refreshed, Stacked, BlockedBySourceApplyCooldown, BlockedByProtectionPhase, or Invalid. Their immutable outcomes additionally distinguish eligible requested, applied, and discarded stack units. Invalid, cooldown-blocked, Protection-blocked, and non-stackable results report zero units.

Buff System receives application requests after another system has resolved the relevant target and eligibility. It does not inspect the associated damage amount or DealDamage result. An ordinary unsuccessful damage action is not a Buff-level reason to reject an otherwise valid application request. The producing Tower-owned boundary is responsible for withholding the request entirely when its source Tower or TowerScaled numeric context is invalid. A Monster killed or removed before the request is no longer a valid Buff target; this is lifecycle invalidation rather than damage-result gating.

## 4. Lifecycle Effect Bindings

The lifecycle event names below are stable authoring identities, not required programming-language callback names.

Buff lifecycle bindings are universal authoring slots. Each binding selects a timing event and an EffectDefinition. The EffectDefinition contains its actions; a Buff never directly applies another Buff.

| Buff Event | Meaning |
|---|---|
| Applied | A new Buff runtime instance was created and is active on its owner |
| PeriodicTick | The Buff reaches a configured positive Periodic Tick Interval |
| StackApplied | An existing stackable Buff successfully gains one stack |
| TowerHitReceived | A successful Tower-owned damage hit evaluates one active Electric/Wind Buff's shared normal reaction |
| Overload | A stackable Buff reaches Maximum Stacks |
| EnteredProtection | A stackable Elemental Buff transitions from Stacking to Protection |
| Removed | An instance is leaving its owner for any reason |

Applied, PeriodicTick, StackApplied, Overload, and EnteredProtection execute after their corresponding runtime state update. Removed executes while owner and source context remain valid, before the instance leaves the active snapshot collection and before consumers receive their state-change notification.

`StackApplied` remains a logical contribution event but active ElectricShock and
Windcut content binds no normal damage to it. `TowerHitReceived`, Periodic,
Overload, Protection, removal, and persistent shared-state damage uses FixedBuff.
An Elemental hit reaction never reads the hit Tower's BasicDamage, Damage Bonus,
DamageScale, or Elemental Upgrade.

Removed covers natural expiry, explicit removal, Clear, monster death, target arrival, reset, and destruction. Add a future Expired event only when content requires natural-expiry-only behavior.

Validation rejects StackApplied, Overload, EnteredProtection, or TowerHitReceived bindings on a non-stackable Buff, rejects PeriodicTick when Periodic Tick Interval is nonpositive, and requires lifecycle and shared reaction damage to use its approved explicit damage authority. TowerHitReceived is valid only for ElectricShock or Windcut and always requires FixedBuff damage plus a positive finite per-instance cooldown.

Behaviour packages provide package-specific Effect references to their reviewed runtime boundaries. Elemental Layer authoring provides its Elemental apply Effect at the runtime-selected attack boundary. Buff lifecycle event identity decides what an already-active Buff does afterward; lifecycle bindings are not a generic attack-trigger authoring path.

## 5. Buff Feedback

MonsterStatusBar owns active-Buff UI. It shows one icon per active BuffDefinition and may show stack count above one. During Protection it keeps the icon but pulses alpha continuously from one to zero and back; no stack count appears.

Monster-owned Buff visual presentation attaches an authored persistent visual to the Monster Hit Reference, preserves one instance through stack, refresh, and Protection changes, and removes it when the Buff leaves runtime state. Protection uses the UI emphasis only in the first version.

Buff runtime exposes read-only state snapshots and state-change notifications; it does not operate UI or visual playback. Clearing a Monster removes all Buff state first, then publishes one change so consumers rebuild from an empty snapshot.

EffectDefinition execution VFX is separate: it is short-lived feedback for an executed Effect, not persistent Buff presentation.

### 5.1 Read-Only Runtime Observation

Buff System publishes immutable diagnostic observations for application attempts and logical lifecycle events. An application observation includes the Buff definition, current source Tower and Elemental Upgrade, explicit apply result, eligible requested units, applied units, discarded units, stack and phase state before and after the attempt, whether Maximum Stacks was reached, and observation time. A lifecycle observation includes the logical event even when no Effect is bound to that event.

Removal observations distinguish Active Duration expiry, Protection expiry, explicit removal, Monster death, Target arrival, technical cleanup, runtime reset, and the no-Protection overload path. Diagnostics therefore consume event history instead of trying to reconstruct expired or removed state from active snapshots.

Observations are published after the outermost Buff state mutation has completed and read-only snapshots have been refreshed. Diagnostic consumers have no gameplay authority; their absence or failure cannot change application, lifecycle Effect execution, Protection, or removal results.

FixedBuff damage diagnostics use a FixedBuff-specific signature: EffectDefinition identity, authored action ordinal, and FixedDamage. Source Tower may be retained as nullable diagnostic context, but Tower Level, Level BasicDamage, Damage Bonus, and DamageScale are not required and never participate in FixedBuff aggregation. Repeated ticks and reactions aggregate counts and totals by this signature rather than requiring one exported JSON record per result.

Each FixedBuff DealDamage execution publishes one exception-isolated read-only
observation after its synchronous target applications. The observation records
resolved-target count, successful-application count, and the authored fixed
amount; it cannot change Buff lifecycle or Monster damage.

## 6. Elemental Buff Rules

Each ElementType has one shared Elemental BuffDefinition. Tower-family Elemental upgrades only declare which Elemental apply Effect their attacks may execute. Once a Buff exists on a monster, its behavior comes solely from that shared definition.

This shared definition also owns the Element's normal-value shape. Fire
periodic damage, Cold movement control, Electric same-owner reaction damage,
and Wind secondary-target reaction damage are calibrated as source-independent
package value. Overload is a distinct conditional reward whose frequency comes
from successful application topology, stack contribution, shared coverage, and
the stacking lifecycle. Neither normal value nor Overload potency reads the
carrying or triggering Tower's damage output.

Tower Runtime Combat authorizes application only from the baseline primary attack
path. Behaviour-added or Behaviour-extended results do not submit ordinary
Elemental requests. Buff System receives an already authorized request and never
infers eligibility from attack provenance, member identity, result role, damage,
Effect identity, or the presence of an Elemental Upgrade.

Buff System evaluates each request through Source Apply Cooldown, Protection, and current instance state. Source Apply Cooldown is scoped by owner Monster, BuffDefinition, and source Tower instance. A different Tower has an independent cooldown entry and is never blocked by the first Tower's entry. Attack-result diagnostic provenance is discarded before request creation and never becomes Buff state or cooldown identity.

First-version Elemental Buffs are Burning, Cold, ElectricShock, and Windcut. Each is stackable and has Stacking and Protection phases:

| Phase | Meaning |
|---|---|
| Stacking | Persistent stackable behavior before overload |
| Protection | Temporary post-overload state that blocks application, refresh, and stacking for that same definition |

Rules:

1. First application creates the Buff with the request's positive contribution capped at Maximum Stacks, starts Active Duration, starts the applying source Tower's Source Apply Cooldown, and runs Applied once. It never runs StackApplied.
2. A successful existing-Buff application refreshes Active Duration, records the exact contributing Tower and Upgrade, and atomically adds its requested units up to Maximum Stacks. When at least one unit commits, it runs StackApplied exactly once regardless of unit count.
3. Multiple towers with the same ElementType contribute to the same shared Buff on a monster.
4. Source Apply Cooldown blocks only a repeated attempt from the same source Tower against that Monster and BuffDefinition. A blocked application does not refresh Active Duration, request or apply diagnostic stack units, or run StackApplied Effects.
5. A successful attempt from a different Tower uses its independent cooldown entry and may contribute immediately, including during the first Tower's cooldown.
6. Active Duration is measured from the latest successful application. Expiry before Maximum Stacks removes the instance and loses its stacks, source cooldown entries, and reaction timing sequence.
7. A first application or reapplication that commits Maximum Stacks earns exactly one pending Overload completion after Applied or StackApplied finishes. Ordinary non-hit application finalizes it immediately. A Tower-hit transaction delays finalization until that hit's Electric/Wind normal reactions have resolved.
8. The pending completion is single-use and captures immutable overload context. Once threshold mutation commits, every normal return, early return, and exception path deterministically finalizes it; runtime may not leave Maximum Stacks externally observable in Stacking phase.
9. If a preceding normal Electric reaction resolves the owner, the earned Overload still executes once from its captured threshold position and context. The dead owner does not enter Protection and no stale Buff instance remains.
10. When the owner survives, finalized Overload enters Protection once when configured. Entering Protection clears stacks, source cooldown entries, and reaction readiness. Protection blocks every source from applying or refreshing that same BuffDefinition until its duration expires and the instance is removed.
11. Protection blocks only the same BuffDefinition by default; other Elemental Buffs may still apply.

StackApplied bindings run only once for a successful reapplication that commits one or more units, not first application or pure refresh. Their contributor context comes from that exact successful request. One application creates at most one StackApplied and one pending Overload regardless of requested magnitude. Reaching Maximum Stacks is the only Overload condition; the first version has no Primed phase, minimum-distinct-source requirement, source contribution quota, or fixed non-refreshing stacking window.

This ordering is shared Buff runtime lifecycle behavior, not an Element-specific sequence. Wind and future Elemental content must use the same dispatcher rather than adding local sequencing. During one Tower-owned hit, Electric is evaluated before Wind. If Electric resolves the owner, Wind records `OwnerResolvedByEarlierElementalReaction` and does not execute.

### 6.1 Elemental Hit-Reaction Cooldown

ElectricShock and Windcut may author one `TowerHitReceived` binding and one
positive finite cooldown. The mutable timer belongs to the owner Monster's one
Buff instance, not to the Tower that hit it or the Tower that applied it. All
Towers share that readiness while the Buff is in Stacking phase.

One successful positive Tower-owned damage result creates one opportunity for
each active reactive Buff on its target. A Buff newly Applied by that same hit is
included. Cooldown-blocked opportunities do not execute an Effect and do not
restart the timer. Electric consumes readiness only when its FixedBuff damage
commits to the owner. Wind consumes readiness only when its FixedBuff damage
commits to one valid randomly selected secondary target; no target means no
damage, no cooldown consumption, and no reaction execution VFX. Protection,
removal, expiry, and terminal cleanup destroy readiness. A later Stacking cycle
starts ready.

FixedBuff damage, lifecycle damage, Overload, WindVortex, and Elemental hit-
reaction damage never create another opportunity. Runtime publishes one
target-specific committed outcome per `(hit, target Monster, BuffDefinition)`
for diagnostics after gameplay resolution; diagnostics never decide readiness,
selection, ordering, or execution.

## 7. First-Version Elemental Content Contracts

| Element | Normal phase | Overload | Boundary |
|---|---|---|---|
| Fire | Burning applies persistent damage pressure | FlameBurst deals area damage around the owner | Tick and FlameBurst damage do not apply Burning by default |
| Cold | Cold slows while active | Apply Frozen Buff | Slow and movement lock use Monster-owned capabilities |
| Electric | ElectricShock adds cooldown-bounded FixedBuff damage when Tower-owned damage hits its owner | Overcharged is an instant multi-target LightningStrike execution | Hit-reaction and LightningStrike damage do not recurse |
| Wind | Windcut uses a cooldown-ready Tower-owned hit on its owner to damage at most one nearby Monster | Maximum Stacks spawn a persistent moving WindVortex at the owner | Hit-reaction and Vortex damage do not recurse |

### 7.1 Fire

Burning PeriodicTick binds a FixedBuff Effect that deals persistent damage. Its Maximum-Stacks Overload binding runs FixedBuff FlameBurst area damage. Both use authored FixedDamage rather than the source Tower's resolved BasicDamage.

### 7.2 Cold And Frozen

Cold's Applied binding requests its authored movement-speed reduction. EnteredProtection and Removed release that reduction.

Cold Maximum-Stacks Overload binds an Apply Frozen EffectDefinition. That Effect contains ApplyBuff(Frozen), which creates or refreshes Frozen through the ordinary Effect-to-Buff link.

Frozen is non-Elemental and non-stackable. Its Applied binding requests movement lock; its Removed binding releases that lock. Its duration, UI, and persistent presentation belong to its own runtime instance. Reapplying it refreshes that one instance without a parallel lock.

### 7.3 Electric

ElectricShock binds one same-owner `TowerHitReceived` Effect containing exactly
one approved FixedBuff DealDamage action. The same Tower-owned hit that first
applies ElectricShock may trigger it when the per-instance cooldown is ready.
Its fixed value is independent from hit source Tower, BasicDamage, Damage Bonus,
DamageScale, stack contribution, and matching Elemental identity.

ElectricShock Overload invokes the instant Overcharged Effect. Overcharged uses its authored radius to resolve nearby candidates, then ExecuteMultiTargetEffect randomly selects up to its authored target count without repetition and immediately executes one single-target LightningStrike Effect on each selected monster. Overcharged has no interval, runtime state, or persistent Buff of its own.

### 7.4 Wind

Windcut binds one `TowerHitReceived` radius wrapper that excludes its owner,
randomly selects at most one remaining valid Monster, and executes one authored
single-target FixedBuff child. The same Tower-owned hit that first applies
Windcut may trigger it when readiness and a secondary target exist. If none
exists, no damage, cooldown consumption, owner fallback, or reaction execution
VFX occurs.

Windcut Overload invokes SpawnWindVortex at the owner's current world position, then the existing Buff runtime enters Protection when configured. WindVortex is an Effect System-owned persistent gameplay entity; it moves itself directly between nearby valid Monsters, independently ticks area damage, and never relocates a Monster. Buff System does not modify world position, Grid Node, or path state.

## 8. Relationships And Scope

- Effect System executes lifecycle-bound Effects and owns their targeting and actions.
- Tower Upgrade System owns Elemental authoring and upgrade application rules, not Buff runtime behavior.
- Tower Runtime Combat decides baseline-primary eligibility; Effect System
  validates and dispatches the Elemental apply request at that boundary.
- Monster System owns movement, pathfinding, lifecycle cleanup, and safe requested operations.

Tower-owned direct and Behaviour damage follows the TowerScaled contract. Periodic, Overload, Protection, persistent, and Elemental hit-reaction damage follows FixedBuff and ignores source-Tower BasicDamage changes. The configuration relationship remains one-directional: Buff lifecycle bindings invoke Effects, and an Effect may request another Buff. BuffDefinitions never directly reference other BuffDefinitions.

## 9. Validation

Buff authoring validation should report at minimum:

- Non-positive Active Duration
- Stackable Buff with Maximum Stacks below two
- Negative Source Apply Cooldown or Overload Protection Duration
- Stack, Overload, or Protection bindings on a non-stackable Buff
- Periodic binding with a non-positive Periodic Tick Interval
- Missing EffectDefinition in an authored lifecycle binding
- TowerHitReceived without a positive finite reaction cooldown, or a positive
  reaction cooldown without that binding
- TowerHitReceived on content other than stackable ElectricShock or Windcut
- Electric reaction content outside one same-owner FixedBuff DealDamage action
- Wind reaction content outside one owner-excluding radius wrapper with random
  target count one and one single-target FixedBuff DealDamage child
- ApplyBuff, movement, SpawnWindVortex, additional gameplay branches, or
  recursive authorization inside an Elemental hit-reaction graph
- Lifecycle or shared reaction damage whose nested DealDamage is not FixedBuff
- Elemental Buff without a valid Elemental type
- Persistent presentation or status data that is configured but unusable

Validation does not silently convert one runtime model into another.

## 10. Approved Scope And Deferred Topics

Current scope includes stackable and non-stackable Buffs, Active Duration refresh, Source Apply Cooldown, Periodic Ticks, lifecycle Effects, Elemental stacking, Overload, Protection, status presentation, persistent presentation, and the Fire, Cold, Electric, and Wind content contracts.

Deferred topics include multiple simultaneous speed modifiers, haste, Buff replacement priorities, natural-expiry-only events, dispel categories, cross-Buff dependency graphs, and recursive Elemental application.

## Battle Dependency Lifetime

Buff lifecycle execution retains the owner's Battle identity independently of its
nullable source Tower. FixedBuff behavior continues without a source Tower while
that Battle and owner remain valid; TowerScaled source requirements are unchanged.
Closing a Battle stops new applications, ticks and Overload output, but removal
still releases existing owner-bound controls and presentation. Pending Overload
completion is finalized once without reviving combat or leaving an unfinished
stacking state. An earned Overload on a dead owner may still execute from its
captured context while the original Battle remains active.
