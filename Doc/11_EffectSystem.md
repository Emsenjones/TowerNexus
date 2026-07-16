# Effect System

## 1. System Overview

The Effect System executes reusable, one-shot gameplay rules. It consumes runtime trigger context, resolves targets, executes ordered actions, and owns reviewed gameplay entities such as EffectZone and WindVortex.

An Effect is execution, not persistent state. Direct base attack damage may remain on the existing attack and projectile path; the Effect System handles additional reusable results such as area damage, Buff application, delayed execution, repeated zone execution, elemental overload results, and movement requests.

```text
Attack Entity / Projectile / EffectZone / Buff lifecycle
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
- Reusable area, delayed, and repeated gameplay execution
- EffectZone timing and on-tick Effect execution
- Reviewed specialized effect entities, including WindVortex
- Elemental-eligibility enforcement and non-inheritance rules after an attack producer explicitly authorizes an application

It does not own persistent Buff lifetime, stack count, Protection phase, status UI, persistent Buff VFX, tower target selection, attack cadence, projectile movement or collision, Monster pathfinding implementation, or Monster reward and arrival flow.

Effects request Monster System capabilities for damage, slow, and movement lock. They never directly mutate a monster Transform, grid node, path, or stored effective speed.

## 3. Core Concepts

| Concept | Meaning |
|---|---|
| Effect | One reusable rule execution, defined by an EffectDefinition and its ordered actions |
| EffectTriggerContext | Runtime source, target, position, damage, and eligibility data for an execution |
| Buff | Persistent runtime state owned by the Buff System; an Effect action may apply one |
| EffectZone | A world gameplay entity that lasts, finds targets, and executes an on-tick Effect |
| WindVortex | A reviewed specialized moving effect entity, not a generic moving-zone requirement |

EffectDefinition is the source of truth for reusable gameplay Effect data. Behaviour package authoring, projectile gameplay impact configuration, and Buff lifecycle bindings reference EffectDefinitions. A Buff never directly references another Buff: a Buff lifecycle binding invokes an Effect, and an ApplyBuff action links that Effect to the target BuffDefinition.

## 4. Trigger Context And Targeting

EffectTriggerContext is created when an explicit runtime boundary requires Effect execution. Typical producers are projectile Position Impact, Monster Hit, Magic Orb contact, Drone projectile hit, reviewed Behaviour execution, EffectZone tick, Buff lifecycle execution, and elemental max-stack overload.

The first version does not serialize or transport one shared trigger enum. Producers express event semantics through their reviewed runtime entry points, then construct EffectTriggerContext with only the execution data that Effect System consumes. Elemental tower attacks use their actual runtime attack event with an explicit elemental-application eligibility flag.

Context may carry source tower, source upgrade, target monster, trigger or impact position, zone radius, resolved damage, attack-entity identity, element type, and elemental-application eligibility. A position must have explicit validity such as `HasTriggerPosition`; `Vector3.zero` is not a missing-position sentinel.

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

| Action | Responsibility |
|---|---|
| DealDamage | Apply configured gameplay damage to resolved targets |
| ApplyBuff | Ask Buff System to add, refresh, or stack a configured BuffDefinition |
| ExecuteMultiTargetEffect | Execute one authored single-target EffectDefinition immediately on a random, non-repeating subset of resolved targets |
| SpawnEffectZone | Create a gameplay zone with duration, radius, tick interval, and on-tick Effect |
| SpawnWindVortex | Create the reviewed specialized moving Wind entity |
| SetMoveSpeedMultiplier / ClearMoveSpeedMultiplier | Set or clear the first-version move-speed multiplier through Monster System |
| SetMovementLock | Set the first-version movement-lock state through Monster System |

Every authored action executes once in authored order. Earlier action success or failure does not suppress later sibling actions; aggregate success must be evaluated without short-circuiting action execution.

### 5.1 DealDamage

DealDamage is instant Effect damage. It supports area impact damage, Burning tick damage, FlameBurst, Electric extra damage, LightningStrike, WindVortex hits, and EffectZone ticks. An action may use a positive authored amount or fall back to resolved damage carried by its trigger context.

Reaction-generated damage does not apply Elemental stacks by default.

DealDamage result does not globally control whether another explicitly eligible Elemental ApplyBuff action may execute. Zero damage, non-positive resolved damage, or unsuccessful damage execution does not suppress a separately authorized application attempt. If damage kills or removes a target before the later action boundary, that target is no longer gameplay-targetable; skipping its Buff request is lifecycle invalidation, not damage-result gating.

### 5.2 ApplyBuff

ApplyBuff is the only first-version link from an Effect to persistent Buff state. It executes on resolved targets and preserves source tower and source upgrade context when present. It may apply Burning, Cold, ElectricShock, Windcut, Frozen, or future BuffDefinitions.

The Buff System owns the resulting instance lifecycle. Effects do not update a Buff's duration, stacks, Protection, UI, or persistent VFX directly.

Non-elemental ApplyBuff actions are not gated by `AllowsElementalApplication`. Any ApplyBuff action whose BuffDefinition has a non-None ElementType requires the runtime context to explicitly authorize the opportunity. Its execution is independent from a sibling DealDamage action's amount or success while still requiring a gameplay-targetable Monster. BuffApplyCooldown, Protection, and Buff runtime decide whether the request applies, refreshes, stacks, or is blocked.

### 5.3 ExecuteMultiTargetEffect

ExecuteMultiTargetEffect is a small execution action, not a reaction or skill-sequencing framework. The parent EffectDefinition's normal radius targeting resolves the candidate monsters. When authored, it may exclude the trigger-context target from that candidate set. The action randomly selects up to its authored target count without repetition, then immediately executes its authored child EffectDefinition once per selected monster.

The child EffectDefinition must be single-target. Each child execution keeps the parent trigger's source context and receives the selected monster's hit/reference position. Child executions do not inherit Elemental application eligibility, so secondary damage such as LightningStrike cannot apply Elemental Buffs by default. The action succeeds only when at least one child execution performs an action successfully; an empty candidate set, including one emptied by trigger-target exclusion, is unsuccessful. There is no execution interval, temporary scene object, coroutine, persistent state, chaining rule, or target-selection mode in the first version.

Windcut uses this action with target count one and trigger-target exclusion. Its parent Effect owns optional execution feedback, and its selected-target child Effect owns optional Wind-attack feedback and damage. No target therefore means no damage and no owner fallback. Execution feedback remains presentation-only and never changes the action success result.

### 5.4 Execution Feedback

When a valid EffectDefinition has an execution VFX prefab, the Effect System instantiates it once for that execution without changing the prefab rotation. This presentation is independent of target resolution and Action success: a radius Effect with a valid trigger position still plays its VFX when no valid monster is in range. The spawn position is the valid trigger position when present, otherwise a valid target hit/reference position. If neither exists, the system has no world position and does not instantiate the prefab.

### 5.5 SpawnEffectZone And SpawnWindVortex

SpawnEffectZone creates a world gameplay entity with its own duration, radius, tick interval, source context, and on-tick Effect. Gameplay timing is authoritative; VFX follows gameplay and never drives it.

SpawnWindVortex is deliberately narrow. It supports the reviewed Wind Buff Overload behavior and does not require a generic moving EffectZone abstraction. The action references a dedicated WindVortexConfig, whose complete runtime prefab contains WindVortexBehaviour, rather than expanding EffectZone authoring prematurely.

### 5.6 Movement And Path Actions

Movement actions request Monster System-owned behavior. The first version has one active move-speed multiplier and one movement lock per monster. `SetMoveSpeedMultiplier` is currently a reduction-only slot and must author `0 < multiplier < 1`; `ClearMoveSpeedMultiplier` restores that slot to `1`. Zero is only the Monster System's derived effective-speed result while a movement lock is active. Haste and multiple concurrent move-speed modifiers require a later reviewed model rather than an implicit extension of this slot.

Frozen uses `SetMovementLock(true)` and `SetMovementLock(false)`. Monster System resolves the lock through the same effective-speed path as the multiplier, with the lock taking priority and producing effective speed zero.

### 5.7 Behaviour-Backed Effects

Explosive Shell, Blast Rounds, Arcane Detonation, Arcane Field ticks, and Final Dive explosion use Effect System for reusable target resolution and gameplay actions, while their trigger timing and lifecycle stay with their owning Attack Entity or tower runtime.

Explosive Shell is an additive Position Impact Effect after the baseline Cannon direct result. Blast Rounds is an additive area Effect after a Drone projectile direct hit. Arcane Detonation executes only after reviewed normal Magic Orb completion. Final Dive first performs its Behaviour-owned local direct-target result, then executes its additive explosion at Position Impact even when no direct Monster Hit was resolved. The optional Final Dive direct target and every explosion target receive independent Elemental opportunities. Each of these explicitly reviewed Behaviour results may provide Elemental eligibility to its resolved targets independently from DealDamage success.

Arcane Field is not a generic EffectZone ownership transfer. Tower runtime owns the tower-attached field instance, follow behavior, uniqueness, and cleanup. Effect System resolves and executes each tick. Every valid target resolved by a V1 field tick receives one 100% Elemental application attempt; this exception is explicit to Arcane Field and does not broaden ordinary zone-tick or periodic Effect defaults.

Bouncing Shell remains Projectile runtime behavior triggered after Position Impact results complete, whether or not that landing resolved a direct Monster Hit. Effect System may execute its landing explosion, but it does not gain a generic SpawnProjectile action or own bounce target selection, chain history, child creation, or remaining bounce count.

## 6. Elemental Eligibility And Recursion

Only tower-owned primary attacks and reviewed Behaviour attack extensions whose runtime context explicitly allows Elemental application may apply Elemental Buffs by default. Technical origin as a Projectile, Attack Entity, or Effect does not grant eligibility.

Eligibility is independent from damage amount and DealDamage success. It still requires the reviewed attack boundary and a resolved target that remains gameplay-targetable at the application boundary defined by the owning Behaviour contract.

Burning ticks, FlameBurst, Electric extra damage, LightningStrike, WindVortex, EffectZone ticks, overload damage, and Buff ticks do not recursively apply Elemental stacks. A future exception is separate reviewed upgrade content, not an implicit baseline behavior.

## 7. EffectZone And WindVortex

EffectZone is a zone-like gameplay entity for reviewed effects needing duration, position, radius, tick interval, target detection, and on-tick Effect execution. Static examples include FireZone, SlowField, PoisonCloud, and DelayedExplosionZone.

Windcut StackApplied is an immediate, single-target Wind attack against up to one other nearby monster. It excludes the Windcut owner from candidates, uses authored extra damage, and has no Elemental-application eligibility. First application, pure refresh, blocked application, Buff tick, and Protection-phase application do not run that attack.

WindVortex is created only by a successful Windcut max-stack Overload through the Overload lifecycle binding. It spawns at the owner's current Transform position before the same Buff enters Protection. First application, ordinary StackApplied attacks, pure refresh, blocked application, Buff tick, and Protection-phase application do not create one.

WindVortex behavior:

1. Spawn at the Wind Buff owner's current Transform position and start its authored lifetime immediately.
2. Its dedicated WindVortexConfig owns its complete runtime prefab, lifetime, movement speed, target-search radius, damage radius, damage tick interval, arrival threshold, and on-tick EffectDefinition.
3. Randomly lock one valid monster in its target-search radius and move directly toward that monster's current Transform position. It does not use Monster pathfinding.
4. On reaching the target Transform position, immediately reacquire. Prefer a different valid monster when one exists; otherwise the reached monster may remain eligible.
5. When the current target becomes invalid, immediately clear and reacquire. With no valid target, remain in place, continue visual presentation, and check again on a small internal runtime interval; the interval is not first-version authoring data.
6. At each authored damage tick, resolve every valid monster inside the independent damage radius and execute the single-target on-tick Effect once per target. A monster may be hit again on later ticks while it remains inside the area.
7. Expire when its authored lifetime ends, regardless of whether it ever acquired a target.

WindVortex owns this direct pursuit, target validity, lifetime, tick timing, and repeated area damage. Its on-tick Effect does not apply Windcut or trigger Elemental reactions by default. It does not move monsters or use Monster pathfinding.

## 8. Relationships And Scope

- Tower Upgrade System owns authoring, eligibility, and application rules. It does not execute Effects.
- Tower Runtime Combat and the owning Attack Entity decide attack timing and target scope, then provide the current Elemental apply Effect and explicit eligibility context at the real attack boundary.
- Projectile System owns movement, collision, simple direct hit damage, and projectile impact VFX; it may emit Effect trigger context for complex outcomes.
- Buff System owns persistent Buff runtime state and invokes Effects from Buff lifecycle bindings.
- Monster System owns health, movement, pathfinding, death, arrival, anchors, and safe movement/path operations.

Effect-backed Behaviour Layer upgrades consume this Effect foundation and, where persistent state is required, the relevant Buff foundation. Their System-level trigger, target, ordering, and Elemental-eligibility contracts remain owned by the corresponding Behaviour content definition.

## 9. Summary

The Effect System is the reusable one-shot gameplay execution layer beyond simple direct attack damage. It stays separate from persistent Buff state while providing the action bridge that can apply Buffs and request Monster-owned operations.
