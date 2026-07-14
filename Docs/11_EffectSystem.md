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

Projectile impact VFX remains Projectile System presentation. EffectDefinition may own optional execution VFX, spawned once after at least one of its actions succeeds at a valid target or trigger position.

## 2. Responsibility Boundary

The Effect System owns:

- Effect trigger context consumption
- Target and radius resolution
- EffectDefinition action execution and execution feedback
- Reusable area, delayed, and repeated gameplay execution
- EffectZone timing and on-tick Effect execution
- Reviewed specialized effect entities, including WindVortex
- The baseline recursion and elemental-application eligibility rule

It does not own persistent Buff lifetime, stack count, Protection phase, status UI, persistent Buff VFX, tower target selection, attack cadence, projectile movement or collision, Monster pathfinding implementation, or Monster reward and arrival flow.

Effects request Monster System capabilities for damage, slow, movement lock, and relocation. They never directly mutate a monster Transform, grid node, path, or stored effective speed.

## 3. Core Concepts

| Concept | Meaning |
|---|---|
| Effect | One reusable rule execution, defined by an EffectDefinition and its ordered actions |
| EffectTriggerContext | Runtime source, target, position, damage, and eligibility data for an execution |
| Buff | Persistent runtime state owned by the Buff System; an Effect action may apply one |
| EffectZone | A world gameplay entity that lasts, finds targets, and executes an on-tick Effect |
| WindVortex | A reviewed specialized moving effect entity, not a generic moving-zone requirement |

EffectDefinition is the source of truth for reusable gameplay Effect data. Behaviour Layer bindings, projectile gameplay impact effects, and Buff lifecycle bindings reference EffectDefinitions. A Buff never directly references another Buff: a Buff lifecycle binding invokes an Effect, and an ApplyBuff action links that Effect to the target BuffDefinition.

## 4. Trigger Context And Targeting

EffectTriggerContext is created when a gameplay event requires Effect execution. Typical producers are projectile impact, attack-entity hit, Magic Orb contact, Drone projectile hit, EffectZone tick, Buff lifecycle binding, and elemental max-stack overload.

First-version trigger types are OnHit, OnImpact, OnBuffApplied, OnBuffTick, OnBuffStackApplied, OnMaxStack, OnBuffEnteredProtection, OnBuffRemoved, and OnZoneTick. Elemental tower attacks use their actual runtime attack event with an explicit elemental-application eligibility flag; they do not need a designer-authored elemental trigger type.

Context may carry source tower, source upgrade, target monster, trigger or impact position, zone radius, resolved damage, attack-entity identity, element type, and elemental-application eligibility. A position must have explicit validity such as `HasTriggerPosition`; `Vector3.zero` is not a missing-position sentinel.

First-version targeting remains simple:

```text
radius <= 0 + target monster: execute on that target
radius <= 0 + no target: do nothing and warn
radius > 0: center on target monster, otherwise valid trigger position; resolve valid monsters in radius
radius > 0 + neither center: do nothing and warn
```

Radius inclusion uses the Monster System hit/reference anchor.

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
| RelocateMonster | Request safe Monster relocation and path recalculation |

### 5.1 DealDamage

DealDamage is instant Effect damage. It supports area impact damage, Burning tick damage, FlameBurst, Electric extra damage, LightningStrike, WindVortex hits, and EffectZone ticks. An action may use a positive authored amount or fall back to resolved damage carried by its trigger context.

Reaction-generated damage does not apply Elemental stacks by default.

### 5.2 ApplyBuff

ApplyBuff is the only first-version link from an Effect to persistent Buff state. It executes on resolved targets and preserves source tower and source upgrade context when present. It may apply Burning, Cold, ElectricShock, Windcut, Frozen, or future BuffDefinitions.

The Buff System owns the resulting instance lifecycle. Effects do not update a Buff's duration, stacks, Protection, UI, or persistent VFX directly.

### 5.3 ExecuteMultiTargetEffect

ExecuteMultiTargetEffect is a small execution action, not a reaction or skill-sequencing framework. The parent EffectDefinition's normal radius targeting resolves the candidate monsters. The action randomly selects up to its authored target count without repetition, then immediately executes its authored child EffectDefinition once per selected monster.

The child EffectDefinition must be single-target. Each child execution keeps the parent trigger's source context and receives the selected monster's hit/reference position. Child executions do not inherit Elemental application eligibility, so secondary damage such as LightningStrike cannot apply Elemental Buffs by default. There is no execution interval, temporary scene object, coroutine, persistent state, chaining rule, or target-selection mode in the first version.

### 5.4 SpawnEffectZone And SpawnWindVortex

SpawnEffectZone creates a world gameplay entity with its own duration, radius, tick interval, source context, and on-tick Effect. Gameplay timing is authoritative; VFX follows gameplay and never drives it.

SpawnWindVortex is deliberately narrow. It supports the reviewed Wind Buff StackApplied behavior and does not require a generic moving EffectZone abstraction.

### 5.5 Movement And Path Actions

Movement actions request Monster System-owned behavior. The first version has one active move-speed multiplier and one movement lock per monster. `SetMoveSpeedMultiplier` is currently a reduction-only slot and must author `0 < multiplier < 1`; `ClearMoveSpeedMultiplier` restores that slot to `1`. Zero is only the Monster System's derived effective-speed result while a movement lock is active. Haste and multiple concurrent move-speed modifiers require a later reviewed model rather than an implicit extension of this slot.

Frozen uses `SetMovementLock(true)` and `SetMovementLock(false)`. Monster System resolves the lock through the same effective-speed path as the multiplier, with the lock taking priority and producing effective speed zero. Storm Shift uses RelocateMonster and remains dependent on the safe relocation and path-recalculation contract.

## 6. Elemental Eligibility And Recursion

Only tower-owned attack events whose runtime context explicitly allows Elemental stack application apply Elemental stacks by default.

Burning ticks, FlameBurst, Electric extra damage, LightningStrike, WindVortex, EffectZone ticks, overload damage, and Buff ticks do not recursively apply Elemental stacks. A future exception is separate reviewed upgrade content, not an implicit baseline behavior.

## 7. EffectZone And WindVortex

EffectZone is a zone-like gameplay entity for reviewed effects needing duration, position, radius, tick interval, target detection, and on-tick Effect execution. Static examples include FireZone, SlowField, PoisonCloud, and DelayedExplosionZone.

WindVortex is created only by a successful additional Windcut stack through the Wind Buff's StackApplied lifecycle binding. Initial application, pure refresh, blocked application, Buff tick, and overload do not create one.

WindVortex behavior:

1. Spawn at the Wind Buff owner's current hit/reference position.
2. Randomly lock one alive monster in its authored search radius, or choose a random position in that radius when none exists.
3. Before first hit, pursue the locked target or position; a valid locked monster may be tracked.
4. On first hit of any monster, stop tracking, lock the current direction, and continue straight.
5. Keep a per-vortex already-hit set; each monster receives its hit Effect at most once.
6. Expire after its authored short lifetime once movement begins.

WindVortex owns this pursuit, first-hit transition, lifetime, and hit set. Its hit Effect does not apply Windcut or trigger Elemental reactions by default. It does not move monsters, use Monster pathfinding, or require Storm Shift relocation.

## 8. Relationships And Scope

- Tower Upgrade System owns authoring, eligibility, and application rules. It does not execute Effects.
- Tower Runtime Combat decides attack timing and target scope, then provides the current Elemental apply Effect and context at the real attack boundary.
- Projectile System owns movement, collision, simple direct hit damage, and projectile impact VFX; it may emit Effect trigger context for complex outcomes.
- Buff System owns persistent Buff runtime state and invokes Effects from Buff lifecycle bindings.
- Monster System owns health, movement, pathfinding, death, arrival, anchors, and safe movement/path operations.

Effect-backed Behaviour Layer upgrades wait for this Effect foundation and the relevant Buff foundation when they need reusable area damage, delayed damage, repeated damage, or persistent state.

## 9. Summary

The Effect System is the reusable one-shot gameplay execution layer beyond simple direct attack damage. It stays separate from persistent Buff state while providing the action bridge that can apply Buffs and request Monster-owned operations.
