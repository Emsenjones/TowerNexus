# Buff And Effect System

## 1. System Overview

The Buff And Effect System is responsible for reusable gameplay effects, persistent buff state, future Elemental debuff stacking, overload rules, and EffectZone-style gameplay entities.

It is not only a Buff system.

It is the gameplay rule framework that can consume trigger events, build runtime context, resolve targets, execute actions, manage persistent states, and coordinate elemental stack and overload behavior.

Core direction:

```text
Attack Entity / Projectile / EffectZone / Buff
    -> emits trigger context
Buff And Effect System
    -> resolves target or radius targets
    -> executes Effect actions
    -> applies or updates Buff runtime state when needed
```

Direct base attack damage does not need to migrate into this system immediately.

The current direct damage path may remain simple while Buff And Effect System handles additional gameplay results such as area damage, buff application, buff ticks, EffectZone ticks, elemental normal phase effects, and overload effects.

Projectile impact visual effects are separate from gameplay Effects. Projectile-specific impact VFX belongs to ProjectileConfig and Projectile System.

Purely visual feedback should not require Buff And Effect System data.

EffectDefinition is the gameplay Effect source of truth for reusable effect data. EffectBinding references and projectile gameplay impact effects should point to EffectDefinition.

---

## 2. Responsibility Boundary

### Owns

The Buff And Effect System owns:

- Effect trigger context consumption
- Effect target resolution
- Effect action execution
- Area damage and other reusable gameplay effect execution
- Future buff application
- Future buff lifecycle
- Future buff stacking, refresh, tick, and removal rules
- Future Elemental debuff stacking
- Future Elemental overload and post-overload Protection phase rules
- Future EffectZone duration, tick, and effect execution

### Does Not Own

The Buff And Effect System does not own:

- Tower target selection
- Tower attack cooldown
- Attack Entity release timing
- Projectile movement
- Projectile collision detection
- Projectile lifetime
- Projectile impact VFX spawning
- Projectile impact VFX cleanup
- Monster pathfinding implementation
- Monster death rewards
- Draft generation
- Tower upgrade application rules

These responsibilities belong to their respective systems.

Movement or path effects may request safe Monster System operations, but Buff And Effect System should not directly manipulate monster transforms or bypass pathfinding ownership.

---

## 3. Core Design Philosophy

## 3.1 Effect, Buff, Damage, And EffectZone Are Different Concepts

An Effect is one rule execution.

Examples:

- Deal damage
- Apply buff
- Spawn EffectZone
- Apply movement effect

A Buff is persistent runtime state attached to a unit.

Examples:

- Burning
- Cold
- ElectricShock
- Windcut
- Elemental Buff Protection phase

Damage is an action result. It may be direct base attack damage, effect damage, buff tick damage, zone tick damage, overload damage, or future reaction damage.

EffectZone is a gameplay entity that exists in the world for a duration, checks targets in an area, and executes effects over time.

Examples:

- FireZone
- WindVortex
- SlowField
- DelayedExplosionZone

EffectZone is not a Buff and is not pure VFX.

---

## 3.2 Keep Simple Damage Simple

Direct single-target base attack damage may remain in the existing Projectile, Attack Entity, and Monster damage path.

Example:

```text
Arrow Hits Monster
    -> Direct Damage
    -> Optional effect trigger context
```

Buff And Effect System should be used when a combat event needs reusable gameplay rule execution.

Examples:

- Area damage
- Apply buff
- Spawn persistent field
- Delayed damage
- Repeated damage
- Elemental stack
- Elemental overload
- Movement or path effect request

This keeps early implementation scope controlled while preserving clean extension points.

---

## 4. Trigger Context

Effect trigger context is the runtime data package created when a gameplay event wants Buff And Effect System execution.

Typical trigger producers:

- Projectile impact
- Projectile or Attack Entity hit
- Magic Orb contact
- Drone-fired projectile hit
- EffectZone tick
- Buff tick
- Elemental max stack event

First-version trigger types:

| Trigger Type | Meaning |
|---|---|
| OnHit | An attack entity hits a specific monster |
| OnImpact | An attack entity reaches a resolve point or impact position |
| OnBuffTick | A persistent Buff performs a periodic tick |
| OnZoneTick | An EffectZone performs a periodic tick |
| OnMaxStack | An elemental debuff reaches max stack and triggers overload |

Elemental tower attacks do not need a separate trigger type. They are regular trigger events with source tower and explicit stack eligibility context.

Trigger context may carry:

- Source tower
- Source upgrade
- Target monster
- Trigger position
- Impact position
- Zone position
- Zone radius
- Base damage or resolved damage value when relevant
- Attack Entity concept
- Trigger type
- Element type when relevant
- Whether this event can apply Elemental stacks

When a trigger position is carried, the context must expose explicit validity such as HasTriggerPosition. Systems should not use Vector3.zero as an implicit "no position" sentinel.

The exact runtime fields may evolve during implementation. The stable contract is that Attack Entities, projectiles, zones, and buffs provide enough source, target, and position context for Buff And Effect System to resolve effects without owning their upstream behavior.

---

## 5. Effect Targeting

First-version Effect targeting should stay simple.

The trigger context should provide one of:

- Target monster
- Trigger position with explicit validity

Effect targeting uses:

- Target monster
- Valid trigger position
- Radius

Targeting rule:

```text
If radius <= 0 and target monster exists:
    target = target monster

If radius <= 0 and target monster is missing:
    execute nothing and log a warning

If radius > 0:
    center = target monster position if target monster exists
    otherwise center = context trigger position only when HasTriggerPosition is true
    targets = valid monsters inside radius around center

If radius > 0 and neither target monster nor valid trigger position exists:
    execute nothing and log a warning
```

Monster inclusion in radius should use the monster-side hit/reference anchor provided by Monster System.

This avoids a large first-version targeting enum while supporting direct effects, area effects, buff application, and overload effects.

---

## 6. Effect Actions

Effect actions define what happens after targets are resolved.

Core action types:

| Action | Responsibility |
|---|---|
| DealDamage | Apply configured gameplay damage to resolved targets |
| ApplyBuff | Add, refresh, or stack a Buff runtime instance on resolved targets |
| SpawnEffectZone | Create a gameplay zone that owns duration and tick timing |
| ApplyMovementEffect | Request a safe Monster System movement or path effect |

### 6.1 DealDamage

DealDamage is an instant action.

Examples:

- Area impact damage
- Burning tick damage
- FlameBurst overload damage
- Electric extra damage
- LightningStrike damage
- WindVortex tick damage
- EffectZone tick damage

First implementation can route Effect, Buff, Zone, and Overload damage through Effect actions while keeping base attack damage unchanged.

DealDamage may use an authored action damage amount when configured. If no positive authored damage amount is provided, it can fall back to the resolved damage carried by the EffectTriggerContext.

Reaction-generated damage should not apply Elemental stacks by default.

### 6.2 ApplyBuff

ApplyBuff creates, refreshes, or stacks a Buff runtime instance.

ApplyBuff is an Effect action. When executed through EffectExecutor, it applies the configured Buff definition to each resolved target monster while preserving source tower and source upgrade context from the EffectTriggerContext when available.

Examples:

- Apply Burning
- Apply Cold
- Apply ElectricShock
- Apply Windcut
- Enter Elemental Buff Protection phase

### 6.3 SpawnEffectZone

SpawnEffectZone creates a gameplay entity with duration, radius, tick interval, source context, and an on-tick Effect.

Examples:

- Spawn FireZone
- Spawn WindVortex
- Spawn SlowField

EffectZone gameplay timing should be authoritative. VFX may follow gameplay timing, but gameplay should not depend on animation timing.

### 6.4 ApplyMovementEffect

ApplyMovementEffect requests monster movement or path changes through safe Monster System APIs.

Examples:

- Frozen movement lock
- Storm Shift
- Future knockback
- Future pull

Buff And Effect System should not directly modify monster transforms for path logic.

---

## 7. Buff Definition And Runtime State

Buff definitions are static configuration.

Buff runtime instances are runtime state.

Buff definitions should be referenced through Inspector-assigned ScriptableObject references. They should not depend on hand-authored string ids unless a future persistence, external-data, or lookup requirement needs stable ids.

Buff definition data may include:

- Display name and description
- Element type when relevant
- Duration
- Tick interval when relevant
- Max stack
- Buff apply cooldown
- Buff event bindings for lifecycle-driven effects
- Protection duration when an Elemental Buff should block restacking after overload

Buff runtime state should include:

- Definition reference
- Owner monster
- Source tower
- Source upgrade
- Remaining duration
- Tick timer
- Stack count
- Buff phase when an Elemental Buff can enter post-overload Protection
- Buff apply cooldown tracking when required

Runtime state must not be stored in definition assets.

MonsterBehaviour owns attached Buff runtime state through an internal plain C# runtime container:

```text
MonsterBehaviour
    -> MonsterBuffRuntime
        -> List<MonsterBuffInstance>
```

Buff runtime instances are not MonoBehaviour components.

First-version successful apply always adds one stack, up to max stack, and refreshes duration to the Buff definition duration.

Buff apply attempts should return explicit results such as Applied, Refreshed, Stacked, BlockedByBuffApplyCooldown, BlockedByProtectionPhase, or Invalid.

Buff ticks may execute periodic tick Buff event bindings through EffectExecutor using OnBuffTick context.

Buff event bindings express Buff-owned lifecycle events:

| Buff Event | Meaning |
|---|---|
| PeriodicTick | The Buff reaches a configured tick interval |
| StackApplied | An existing Buff successfully gains one stack |
| Overload | The Buff reaches max stack and triggers overload |

TowerUpgradeDefinition EffectBindings remain responsible for external tower events such as OnHit. BuffDefinition event bindings are responsible for what the Buff does after it already exists on a monster.

---

## 8. Elemental Debuff Rules

Elemental Layer upgrades convert towers into elemental towers.

Tower-owned attack events from elemental towers can apply elemental debuff stacks through Buff And Effect System when their runtime context explicitly allows Elemental stack application.

First-version elemental debuffs:

- Burning
- Cold
- ElectricShock
- Windcut

Each elemental debuff has two runtime phases:

| Phase | Meaning |
|---|---|
| Stacking | Persistent stackable debuff behavior before overload |
| Protection | Temporary post-overload state that blocks restacking for that same BuffDefinition |

General stacking rule:

1. If the monster does not have this elemental debuff, apply it with initial stacks.
2. If the monster already has this elemental debuff and the application is not blocked, refresh duration and add one stack when below max stacks.
3. Multiple towers with the same elemental upgrade can add stacks to the same monster.
4. The same elemental debuff on the same monster may have an apply cooldown that blocks applications from any tower while active.
5. If the elemental debuff expires before max stack is reached, the debuff is removed and stacks are lost.
6. When max stack is reached, trigger overload.
7. After overload triggers, the elemental debuff enters Protection phase when it has a protection duration.

First application of an elemental debuff should apply the debuff only. StackApplied Buff event bindings, such as Electric extra damage or WindVortex spawn, should trigger only when an existing elemental debuff successfully gains one stack. A pure refresh should not trigger stack effects.

Buff apply cooldown prevents the same elemental debuff from stacking too quickly on the same monster, regardless of which tower attempts the application. When this cooldown blocks an application, no stack is added, duration is not refreshed, and stack effects do not trigger.

After a successful stack increase, the system checks whether max stacks have been reached. If max stacks are reached, overload executes and the Buff enters Protection phase when it has a protection duration. While in Protection phase, the same BuffDefinition cannot be applied, refreshed, or stacked.

---

## 9. Elemental Buff Protection Phase

Protection phase is a temporary state on the same Buff instance after elemental overload.

It prevents the monster from gaining stacks for that same Elemental BuffDefinition for a short duration.

It does not block damage by default.

Example:

```text
Monster triggers Cold overload
    -> Cold Buff enters Protection phase
    -> Cold stacks cannot be added or refreshed while Protection is active
    -> Fire, Electric, and Wind stacks can still be applied normally
```

Protection behavior is authored directly on the same BuffDefinition with a protection duration.

Example Fire authoring:

```text
Burning:
    Element Type = Fire
    Max Stacks = 3
    Protection Duration = 2s
```

Protection phase should be treated as status on the same Buff runtime instance, not as a separate elemental debuff reaction chain.

---

## 10. Elemental First-Version Design Contracts

The first Elemental content set should preserve clear identity for each element without locking numeric tuning or concrete implementation details into the system contract.

| Element | Normal Phase | Overload | Important Boundary |
|---|---|---|---|
| Fire | Burning creates persistent damage pressure while active | FlameBurst deals area damage around the target monster | Burning tick damage and FlameBurst damage do not apply Burning stacks by default |
| Cold | Cold slows the monster while active | Frozen is an instant overload effect that applies a temporary movement lock | Movement lock must go through safe Monster movement APIs |
| Electric | ElectricShock causes later direct Electric tower hits to trigger extra electric damage | Overcharged is an instant lightning sequence that selects random monsters near the target and strikes them one by one | Overcharged is not a persistent Buff, and lightning strikes do not apply ElectricShock stacks by default |
| Wind | Windcut causes later direct Wind tower hits to spawn WindVortex | Storm Shift is an instant overload effect that moves the target monster to a valid nearby grid node | WindVortex damage and Storm Shift do not apply Windcut stacks by default |

### 10.1 Burning Overload

Burning overload triggers FlameBurst.

FlameBurst deals area damage around the monster that reached max Burning stacks.

FlameBurst damage is Effect damage and should not apply Burning stacks or trigger Elemental reactions by default.

### 10.2 Cold Overload

Cold overload triggers Frozen.

Frozen is an instant overload effect that applies a temporary movement lock to the target monster.

Frozen should request movement control through safe Monster movement APIs. Buff And Effect System should not directly stop movement by bypassing Monster System ownership.

### 10.3 Electric Overload

ElectricShock overload triggers Overcharged.

Overcharged is an instant overload effect, not a long-lived Buff state.

Overcharged selects random monsters within a configured radius around the target monster, then drops sequential single-target lightning strikes with a configured strike interval.

LightningStrike damage is Electric Effect damage and should not apply ElectricShock stacks or trigger Elemental reactions by default.

### 10.4 Wind Overload

Windcut overload triggers Storm Shift.

Storm Shift is an instant overload effect that moves the target monster to a random valid nearby grid node.

The selected node must be walkable and reachable to the goal. After relocation, the monster should recalculate its path through the pathfinding system.

Buff And Effect System should request safe Monster movement and pathfinding APIs for Storm Shift. It must not directly modify the monster Transform, current grid node, or path state.

---

## 11. Recursion Rule

Only tower-owned attack events whose runtime context explicitly allows Elemental stack application can apply elemental stacks by default.

The following should not apply elemental stacks by default:

- Burning tick damage
- FlameBurst damage
- Electric extra damage
- LightningStrike damage
- WindVortex damage
- EffectZone tick damage
- Overload damage
- Buff tick damage

Future upgrades may explicitly override this rule, but that should be reviewed as separate upgrade content rather than assumed by the baseline Elemental Layer.

The baseline rule is that tower attacks can carry Elemental stack application, while Elemental reactions do not recursively carry Elemental stack application.

---

## 12. EffectZone

EffectZone is a zone-like gameplay entity.

It owns:

- Duration
- Position
- Radius
- Tick interval
- Target detection inside radius
- On-tick Effect execution
- Source tower reference
- Source upgrade reference

EffectZone may be static or moving.

Examples:

| Zone Type | Examples |
|---|---|
| Static | FireZone, SlowField, PoisonCloud |
| Moving | WindVortex, Tornado, MovingStormField |

Static and moving zones may share one EffectZone runtime if the implementation remains clean.

WindVortex should be reviewed after Buff core and simpler elemental effects are stable because it needs moving EffectZone behavior and target selection separate from radius-based damage resolution.

WindVortex is a moving EffectZone.

WindVortex movement target selection may use TargetSelectionType, with Nearest as the default first-version movement target rule.

WindVortex damage targeting uses monsters inside the radius around the current WindVortex position. Movement target selection and damage target resolution are separate concepts.

WindVortex damage may use fixed configured damage in the first version.

WindVortex damage should not apply Windcut stacks or trigger Elemental reactions by default.

---

## 13. Relationship With Other Systems

### Tower Upgrade System

Tower Upgrade System owns upgrade definitions, eligibility, and application rules.

Behaviour and Elemental upgrades may reference Effect bindings, Elemental profiles, Buff definitions, or Effect definitions.

Tower Upgrade System should not execute Buff, Effect, Elemental stack, or overload behavior.

### Tower Runtime Combat System

Tower Runtime Combat decides when towers attack, resolves combat stats, and releases Attack Entities.

Runtime Combat may provide source context and resolved damage values to Attack Entities or trigger context, but it should not own Buff, Elemental stack, or overload rules.

### Projectile System

Projectile System owns projectile movement, hit detection, impact event generation, simple single-target projectile damage dispatch, and projectile-specific impact VFX.

Projectile System may emit trigger context for complex gameplay results.

Projectile System should not directly apply Buffs or handle Elemental stack logic.

### Monster System

Monster System owns monster health, movement, pathfinding, death, arrival, hit/reference anchors, and safe movement/path operations.

Buff And Effect System may request damage or movement/path effects through Monster System-owned APIs, but it should not bypass Monster System ownership.

---

## 14. Implementation Scope Direction

The next implementation should be split into Task Documents rather than implemented as one large change.

Recommended order:

1. Effect trigger context, Effect definition, simple target resolution, and Effect action foundation.
2. Buff definition, Monster buff runtime state, stacking, refresh, Buff apply cooldown, and post-overload Protection phase foundation.
3. Fire vertical slice: Burning and FlameBurst.
4. Cold and Electric vertical slices.
5. EffectZone foundation.
6. WindVortex and Storm Shift after EffectZone and Monster movement/path APIs are ready.

Effect-backed Behaviour Layer upgrades such as Magic Orb Splash, Cannon Timed Shell, and Cannon Burning Shell should wait until the required Effect binding and Buff And Effect System foundation exists.

---

## 15. Summary

The Buff And Effect System handles reusable gameplay rule execution beyond simple direct attack damage.

It should grow from a small Effect foundation into Buff runtime, Elemental debuff stacking, overload, and EffectZone support through staged Task Documents.

Direct base attack damage can remain in the existing runtime path until a later DamageContext migration is explicitly reviewed.
