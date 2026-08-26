# Effect System

Document Set: System

## 1. System Overview

The Effect System executes reusable, one-shot gameplay rules. It consumes runtime trigger context, resolves targets, executes ordered actions, and owns the reviewed WindVortex gameplay entity.

An Effect is execution, not persistent state. Tower-owned direct and Behaviour damage may use the shared TowerScaled formula. The Effect System handles reusable results such as TowerScaled area damage, FixedBuff shared-state lifecycle and Elemental hit-reaction damage, Buff application, elemental overload results, and movement requests.

```text
Attack Entity / Projectile / Buff lifecycle
    -> EffectTriggerContext
Effect System
    -> resolve targets
    -> execute EffectDefinition actions
    -> request Monster or Buff System operations as needed
```

Projectile impact VFX remains Projectile System presentation. EffectDefinition may own optional execution VFX. Execution feedback is short-lived and must not drive gameplay timing or target resolution.

## 2. Responsibility Boundary

The Effect System owns:

- Effect trigger context consumption
- Target and radius resolution
- EffectDefinition action execution and execution feedback
- Reusable single-target and area gameplay execution
- Reviewed specialized effect entities, including WindVortex
- Elemental-eligibility enforcement and non-inheritance rules after an attack producer explicitly authorizes an application

It does not own persistent Buff lifetime, stack count, Protection phase, status UI, persistent Buff VFX, tower target selection, attack cadence, projectile movement or collision, Monster pathfinding implementation, or Monster reward and arrival flow.

Effects request Monster System capabilities for damage, slow, and movement lock. They never directly change a Monster's world position, Grid Node, path, or stored effective speed.

## 3. Core Concepts

| Concept | Meaning |
|---|---|
| Effect | One reusable rule execution, defined by an EffectDefinition and its ordered actions |
| EffectTriggerContext | Runtime source, target, position, Attack Entity, and eligibility data for an execution |
| Buff | Persistent runtime state owned by the Buff System; an Effect action may apply one |
| WindVortex | A reviewed specialized moving effect entity, not a generic moving-zone requirement |

EffectDefinition is the source of truth for reusable gameplay Effect data. Behaviour package authoring, projectile gameplay impact configuration, and Buff lifecycle bindings reference EffectDefinitions. A Buff never directly references another Buff: a Buff lifecycle binding invokes an Effect, and an ApplyBuff action links that Effect to the target BuffDefinition.

## 4. Trigger Context And Targeting

EffectTriggerContext is created when an explicit runtime boundary requires Effect execution. Typical producers are projectile Position Impact, Monster Hit, Magic Orb contact, Drone projectile hit, reviewed Behaviour execution, Buff lifecycle execution, and elemental max-stack overload.

The first version does not serialize or transport one shared trigger enum. Producers express event semantics through their reviewed runtime entry points, then construct EffectTriggerContext with only the execution data that Effect System consumes. Elemental tower attacks use their actual runtime attack event with an explicit elemental-application eligibility flag.

EffectTriggerContext does not carry an inherited integer damage value or a
direct-hit damage resolution. A TowerScaled DealDamage action creates its own
immutable resolution from the current source Tower at that Effect's execution
boundary. A FixedBuff action reads only its authored FixedDamage. Direct-hit
resolution facts remain in their attack-specific result contexts for gameplay
and diagnostics and are never a fallback Effect damage authority.

Context may carry source Tower, source Upgrade, target Monster, trigger or impact position, zone radius, Attack Entity identity, Elemental type, Elemental-application eligibility, and the explicitly validated stack units requested by an ordinary Elemental application. It may also carry attack-result provenance solely to publish read-only diagnostics before request creation. Provenance has no gameplay authority and is discarded before BuffApplyRequest. Position presence must be explicit; no valid world coordinate is treated as a missing-value sentinel.

Only the reviewed Elemental attack boundary may place a positive stack-unit request in context. Effect execution forwards that value and never derives it from a retained source Upgrade. Buff lifecycle, reaction, and other non-Elemental contexts carry no stack-unit request, even when diagnostic source context remains present.

Position Impact and Monster Hit are independent semantic facts. Position Impact means an Attack Entity reached its intended gameplay position and does not require a resolved Monster. Monster Hit requires a valid Monster. One Cannon or Final Dive landing may produce both facts, while an empty landing may produce Position Impact only. The System contract does not require these facts to become separate serialized Effect fields; the implementation may represent them through trigger context and reviewed runtime entry points.

When a landing produces both facts, the runtime may retain the optional direct Monster in its attack-specific impact payload while creating the Position Impact Effect context with no target Monster and an explicitly valid impact position. This keeps an area Position Impact Effect centered on the landing because normal Effect targeting otherwise prefers a valid target Monster's hit/reference anchor over the trigger position. Any direct Monster Elemental opportunity is dispatched separately.

First-version targeting remains simple:

```text
radius <= 0 + target monster: execute on that target
radius <= 0 + no target: do nothing and warn
radius > 0: center on target monster, otherwise valid trigger position; resolve valid monsters in radius
radius > 0 + neither center: do nothing and warn
```

Radius inclusion uses the Monster System hit/reference anchor.

When a caller requests the resolved-target output, Effect execution clears that output before validation or resolution. A false result leaves it empty. A true result exposes the complete target snapshot for that execution independently from whether any authored action succeeds.

## 5. EffectDefinition Actions

The action names below are stable authoring identities, not required programming-language method names.

| Action | Responsibility |
|---|---|
| DealDamage | Apply configured gameplay damage to resolved targets |
| ApplyBuff | Ask Buff System to add, refresh, or stack a configured BuffDefinition |
| ExecuteMultiTargetEffect | Execute one authored single-target EffectDefinition immediately on a random, non-repeating subset of resolved targets |
| SpawnWindVortex | Create the reviewed specialized moving Wind entity |
| SetMoveSpeedMultiplier / ClearMoveSpeedMultiplier | Set or clear the first-version move-speed multiplier through Monster System |
| SetMovementLock | Set the first-version movement-lock state through Monster System |

Every authored action executes once in authored order. Earlier action success or failure does not suppress later sibling actions; aggregate success must be evaluated without short-circuiting action execution.

### 5.1 DealDamage

DealDamage is instant Effect damage with one explicit authored DamageMode. It never falls back from one mode to another.

| DamageMode | Required Data | Formula And Ownership |
|---|---|---|
| TowerScaled | Positive finite DamageScale and valid source Tower | `RoundToInt(source Tower current resolved BasicDamage * DamageScale)` at execution time |
| FixedBuff | Positive integer FixedDamage | Apply the authored amount; source Tower may remain diagnostic/cooldown context but never changes this damage |

TowerScaled is used by Tower-owned Behaviour results. FixedBuff is used by Periodic, Overload, Protection, persistent, and shared Electric/Wind Elemental hit-reaction results. A Level or Basic Damage Bonus change may affect a later TowerScaled execution, but it never replays an earlier execution or alters FixedBuff damage.

One TowerScaled action resolves one immutable damage fact and uses that same
fact for Monster damage, execution context, and read-only diagnostics. Its
damage-source identity includes the EffectDefinition and authored action
ordinal. Diagnostics never call the resolver again. Non-finite scale, resolved
BasicDamage, or raw product, non-positive resolved BasicDamage, or an invalid
source Tower rejects the complete owning Tower result rather than clamping it.
For a Tower-owned attack boundary, invalid source context also suppresses its
Behaviour Effects and Elemental opportunity. This source-context rejection is
distinct from an ordinary valid-context DealDamage action that simply finds no
target or cannot apply damage.

Every successful target application of TowerScaled Effect damage returns one
target-specific committed result to its owning Tower-hit transaction. This is
the gameplay transport used to offer an existing Electric/Wind Buff one
Elemental hit-reaction opportunity. An aggregate action-success boolean or
Recorder damage observation cannot substitute for that committed target result.
FixedBuff actions never return a Tower-owned hit result.

Reaction-generated damage does not apply Elemental stacks by default.

With valid Tower source and numeric context, one target-specific DealDamage result does not globally control whether another explicitly eligible Elemental ApplyBuff action may execute. An ordinary unsuccessful damage action, such as one with no valid resolved target, does not suppress a separately authorized application attempt against another valid target. Invalid Tower source or invalid TowerScaled numeric context rejects the owning Tower result before sibling Behaviour or Elemental actions run. If valid damage kills or removes a target before the later action boundary, that target is no longer gameplay-targetable; skipping its Buff request is lifecycle invalidation, not damage-result gating.

### 5.2 ApplyBuff

ApplyBuff is the only first-version link from an Effect to persistent Buff state. It executes on resolved targets and preserves source Tower, source Upgrade, and the context's explicit stack-unit request when present. It forwards those facts without recalculating contribution. The source Tower is the contribution identity used by stackable Buff Source Apply Cooldown; Attack Entities created by that Tower do not become separate Buff sources. ApplyBuff may apply Burning, Cold, ElectricShock, Windcut, Frozen, or future BuffDefinitions.

For an Elemental ApplyBuff action, the Effect gate publishes the diagnostic
`dispatched` fact only after explicit authorization and target validation pass
and immediately before creating and submitting BuffApplyRequest. It does not wait
for Buff success, Source Apply Cooldown, or Protection. Non-Elemental Buff
requests do not participate in this reconciliation.

The Buff System owns the resulting instance lifecycle. Effects do not update a Buff's duration, stacks, Protection, UI, or persistent VFX directly.

Non-Elemental ApplyBuff actions require no Elemental authorization and produce no stack-unit mutation for non-stackable Buffs. Any ApplyBuff action whose BuffDefinition has an Elemental type requires the runtime context to explicitly authorize the opportunity and carry a positive validated stack-unit request. Its execution is independent from a sibling DealDamage action's amount or success while still requiring a gameplay-targetable Monster. Source Apply Cooldown, Protection, and Buff runtime decide whether the request applies, refreshes, stacks, overloads, discards overflow, or is blocked.

### 5.3 ExecuteMultiTargetEffect

ExecuteMultiTargetEffect is a small execution action, not a reaction or skill-sequencing framework. The parent EffectDefinition's normal radius targeting resolves the candidate monsters. When authored, it may exclude the trigger-context target from that candidate set. The action randomly selects up to its authored target count without repetition, then immediately executes its authored child EffectDefinition once per selected monster.

The child EffectDefinition must be single-target. Each child execution keeps the parent trigger's source context and receives the selected Monster's Hit Reference position. Child executions do not inherit Elemental application eligibility, so secondary damage such as LightningStrike cannot apply Elemental Buffs by default. The action succeeds only when at least one child execution performs an action successfully; an empty candidate set, including one emptied by trigger-target exclusion, is unsuccessful. The first version adds no interval, persistent state, chaining rule, or additional target-selection mode.

Windcut Elemental hit reaction uses this action with target count one, trigger-
target exclusion, and random non-repeating selection. Its selected-target child
owns one FixedBuff damage action. The reaction transaction first establishes
that a valid secondary target exists, then executes the approved Effect graph.
No target therefore means no damage, no owner fallback, no cooldown consumption,
and no reaction execution feedback. Execution feedback remains presentation-
only and never changes the action success result.

### 5.4 Execution Feedback

When a valid EffectDefinition has execution presentation, Effect System requests it once at the authored orientation. Presentation is independent of target resolution and Action success: a radius Effect with a valid trigger position still presents when no valid Monster is in range. Its position is the valid trigger position when present, otherwise a valid target Hit Reference. Without either position, no world presentation is requested.

### 5.5 SpawnWindVortex

SpawnWindVortex is deliberately narrow. It supports the reviewed Wind Buff Overload behavior and references a complete WindVortex runtime prefab rather than introducing a generic persistent-zone abstraction. The prefab root's WindVortexBehaviour owns the entity's authored runtime parameters.

### 5.6 Movement And Path Actions

Movement actions request Monster System-owned behavior. The first version has one active move-speed multiplier and one movement lock per monster. `SetMoveSpeedMultiplier` is currently a reduction-only slot and must author `0 < multiplier < 1`; `ClearMoveSpeedMultiplier` restores that slot to `1`. Zero is only the Monster System's derived effective-speed result while a movement lock is active. Haste and multiple concurrent move-speed modifiers require a later reviewed model rather than an implicit extension of this slot.

Frozen requests movement lock when applied and releases that lock when removed. Monster System resolves the lock through the same effective-speed path as the multiplier, with the lock taking priority and producing effective speed zero.

### 5.7 Behaviour-Backed Effects

Explosive Arrow, Explosive Shell, Blast Rounds, Arcane Detonation, Arcane Field ticks, and Final Dive explosion use Effect System for reusable target resolution and gameplay actions, while their trigger timing and lifecycle stay with their owning Attack Entity or tower runtime.

Explosive Arrow is an additive area Effect after a direct Arrow Monster Hit. It centers on that hit and includes the direct target when that Monster remains gameplay-targetable after direct damage. Explosive Shell is an additive Position Impact Effect after the baseline Cannon direct result. Blast Rounds is an additive area Effect after a Drone projectile direct hit. Arcane Detonation executes only after reviewed normal Magic Orb completion. Final Dive first performs its Behaviour-owned local direct-target result, then executes its additive explosion at Position Impact even when no direct Monster Hit was resolved. These Behaviour-backed Effects preserve authored target resolution, damage, and lifecycle but never authorize ordinary Elemental application.

Cannon primary, additional, and bounced Shells own independent stable DamageScale values and use current resolved BasicDamage at their actual damage boundaries. Explosive Shell uses its own TowerScaled DealDamage action rather than inheriting a Shell's direct scale.

Arcane Field remains a Tower Runtime Combat-owned entity. Tower runtime owns the tower-attached field instance, follow behavior, uniqueness, and cleanup. Effect System resolves and executes each tick without granting an ordinary Elemental application.

Bouncing Shell remains Projectile runtime behavior triggered after Position Impact results complete, whether or not that landing resolved a direct Monster Hit. Effect System may execute its landing explosion, but it does not gain a generic SpawnProjectile action or own bounce target selection, chain history, child creation, or remaining bounce count.

### 5.8 Damage Ownership Inventory

Current TowerScaled Effect damage:

- Explosive Arrow
- Explosive Shell
- Blast Rounds impact
- Final Dive impact
- Arcane Detonation
- Arcane Field tick

Current FixedBuff lifecycle or reaction damage:

- Burning PeriodicTick
- Burning Overload / FlameBurst
- Electric same-owner Elemental hit reaction
- Windcut one-secondary-target Elemental hit reaction
- Electric Overload LightningStrike
- WindVortex tick

ApplyBuff, movement multiplier, movement lock, multi-target wrapper, and WindVortex-spawn actions do not own damage. Composite wrappers preserve the damage mode authored by their child Effect; they do not add or infer one.

## 6. Elemental Eligibility And Recursion

Only the baseline primary attack path explicitly authorized by Tower Runtime
Combat may apply Elemental Buffs by default. Behaviour-added or Behaviour-
extended results never gain authorization merely from source Tower, Upgrade,
Projectile, Attack Entity, Effect, or positive-damage identity.

Eligibility is independent from damage amount and DealDamage success. It still requires the reviewed attack boundary and a resolved target that remains gameplay-targetable at the application boundary defined by the owning Behaviour contract.

Read-only diagnostics distinguish candidate, eligible, and dispatched facts.
Recorder observes those facts but cannot promote a candidate to eligible or
dispatch a request. Effect System forwards explicit authorization and discards
diagnostic provenance before Buff state is entered.

Burning ticks, FlameBurst, Electric or Wind Elemental hit reactions,
LightningStrike, WindVortex, overload damage, and Buff ticks do not recursively
apply Elemental stacks or trigger another Elemental hit reaction. A future
exception is separate reviewed upgrade content, not an implicit baseline
behavior.

## 7. WindVortex

Windcut's normal Elemental hit reaction is a shared FixedBuff attack against at
most one randomly selected nearby Monster. It excludes the Windcut owner from
candidates, uses only authored FixedDamage, and has no Elemental-application
eligibility. Any successful Tower-owned damage hit may offer the active Windcut
instance one opportunity; a per-instance cooldown and target availability
decide whether damage commits.

WindVortex is created only by a successful Windcut max-stack Overload through the Overload lifecycle binding. It spawns at the owner's captured threshold position before the same surviving Buff enters Protection. First application without threshold, ordinary Elemental hit reactions, pure refresh, blocked application, Buff tick, and Protection-phase application do not create one.

WindVortex behavior:

1. Spawn at the Wind Buff owner's current world position and start its authored lifetime immediately.
2. Its complete runtime prefab owns its presentation, while the root WindVortexBehaviour owns lifetime, movement speed, target-search radius, damage radius, damage tick interval, arrival threshold, and on-tick EffectDefinition.
3. Randomly lock one valid Monster in its target-search radius and move directly toward that Monster's current world position. It does not use Monster pathfinding.
4. On reaching the target position, immediately reacquire. Prefer a different valid Monster when one exists; otherwise the reached Monster may remain eligible.
5. When the current target becomes invalid, immediately clear and reacquire. With no valid target, remain in place, continue visual presentation, and check again on a small internal runtime interval; the interval is not first-version authoring data.
6. At each authored damage tick, resolve every valid monster inside the independent damage radius and execute the single-target on-tick Effect once per target. A monster may be hit again on later ticks while it remains inside the area.
7. Expire when its authored lifetime ends, regardless of whether it ever acquired a target.

WindVortex owns this direct pursuit, target validity, lifetime, tick timing, and repeated area damage. Its on-tick Effect does not apply Windcut or trigger Elemental reactions by default. It does not move monsters or use Monster pathfinding.

## 8. Relationships And Scope

- Tower Upgrade System owns authoring, eligibility, and application rules. It does not execute Effects.
- Tower Runtime Combat and the owning Attack Entity decide attack timing and target scope, then provide the current Elemental apply Effect and explicit eligibility context at the real attack boundary.
- Projectile System owns movement, collision, simple direct hit damage, and projectile impact VFX; it may emit Effect trigger context for complex outcomes.
- Buff System owns persistent Buff runtime state and invokes Effects from Buff lifecycle bindings.
- Monster System owns health, movement, pathfinding, death, arrival, Hit References, and safe movement or path operations.

Effect-backed Behaviour Layer upgrades consume this Effect foundation and, where persistent state is required, the relevant Buff foundation. Their System-level trigger, target, ordering, and Elemental-eligibility contracts remain owned by the corresponding Behaviour content definition.

## 9. Validation

Effect authoring validation should report at minimum:

- Missing or empty EffectDefinition action data
- Missing required child Effect, Buff, or WindVortex runtime prefab
- WindVortex runtime prefab without a valid WindVortexBehaviour on its root
- Multi-target execution whose child is not single-target
- Radius execution without a valid way to resolve its center
- Invalid movement-speed multiplier or unsupported movement action combination
- Elemental ApplyBuff content without an explicit eligible runtime producer
- TowerScaled DealDamage without a valid source-Tower execution path, with non-positive or non-finite DamageScale, or whose runtime resolved BasicDamage or raw product is non-positive or non-finite
- FixedBuff DealDamage with non-positive FixedDamage
- DealDamage that authors fields for both modes or for neither mode
- Behaviour owner referencing FixedBuff damage, or shared-state lifecycle,
  Elemental hit-reaction, or WindVortex owner referencing TowerScaled damage
- Recursive EffectDefinition reference cycle, including a nested multi-target cycle

Validation reports the source definition and does not silently change target scope or action order.

Local action validation is not sufficient to prove ownership. Behaviour package
owners must recursively validate TowerScaled damage. Shared-state lifecycle,
Elemental hit-reaction, and WindVortex owners must recursively validate
FixedBuff damage. Electric reaction authoring is exactly one same-owner
FixedBuff DealDamage action. Wind reaction authoring is exactly one radius
wrapper that excludes the owner, randomly selects at most one target, and calls
one single-target FixedBuff DealDamage child. ApplyBuff, movement,
SpawnWindVortex, additional gameplay branches, and recursive authorization are
invalid inside either reaction graph. Nested multi-target children preserve the
expected mode; revisiting an EffectDefinition
in the active validation traversal reports an invalid cycle rather than silently
ending recursion.

## 10. Approved Scope And Deferred Topics

Current scope includes ordered one-shot actions, single-target and radius resolution, ApplyBuff, multi-target child execution, the reviewed WindVortex entity, movement requests, execution presentation, and explicit Elemental eligibility.

Deferred topics include persistent Effect Zones, delayed or repeated generic Effect execution, a generic moving-Zone framework, arbitrary action graphs, recursive Elemental propagation, and generalized projectile spawning.
