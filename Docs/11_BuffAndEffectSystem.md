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
- Future Elemental overload and same-element stack immunity rules
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
- ElementalStackImmunity

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

Elemental tower hits do not need a separate trigger type. They are OnHit events with source tower and stack eligibility context.

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
- Whether this hit can apply Elemental stacks

The exact runtime fields may evolve during implementation. The stable contract is that Attack Entities, projectiles, zones, and buffs provide enough source, target, and position context for Buff And Effect System to resolve effects without owning their upstream behavior.

---

## 5. Effect Targeting

First-version Effect targeting should stay simple.

The trigger context should provide one of:

- Target monster
- Position

Effect targeting uses:

- Target monster
- Position
- Radius

Targeting rule:

```text
If radius <= 0 and target monster exists:
    target = target monster

If radius > 0:
    center = target monster position if target monster exists
    otherwise center = context position
    targets = valid monsters inside radius around center
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

Reaction-generated damage should not apply Elemental stacks by default.

### 6.2 ApplyBuff

ApplyBuff creates, refreshes, or stacks a Buff runtime instance.

Examples:

- Apply Burning
- Apply Cold
- Apply ElectricShock
- Apply Windcut
- Apply ElementalStackImmunity

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
- Stackability
- Max stack
- Stack amount per apply
- Refresh duration on reapply
- Same-source apply cooldown
- Normal phase effect
- Overload effect
- Remove on overload
- Same-element stack immunity duration

Buff runtime state should include:

- Definition reference
- Owner monster
- Source tower
- Source upgrade
- Remaining duration
- Tick timer
- Stack count
- Same-source cooldown tracking when required

Runtime state must not be stored in definition assets.

---

## 8. Elemental Debuff Rules

Elemental Layer upgrades convert towers into elemental towers.

Direct attacks from elemental towers can apply elemental debuff stacks through Buff And Effect System.

First-version elemental debuffs:

- Burning
- Cold
- ElectricShock
- Windcut

Each elemental debuff has two phases:

| Phase | Meaning |
|---|---|
| Normal Phase | Persistent stackable debuff behavior before overload |
| Overload Phase | Effect triggered when max stack is reached |

General stacking rule:

1. If the monster does not have this elemental debuff, apply it with initial stacks.
2. If the monster already has this elemental debuff and the application is not blocked, run the normal phase when configured, refresh duration, and add stacks.
3. Multiple towers with the same elemental upgrade can add stacks to the same monster.
4. The same source tower may have an apply cooldown for the same elemental debuff on the same monster.
5. If the elemental debuff expires before max stack is reached, the debuff is removed and stacks are lost.
6. When max stack is reached, trigger overload.
7. After overload triggers, apply same-element ElementalStackImmunity.

First application of an elemental debuff should apply the debuff only. Normal phase extra effects such as Electric extra damage or WindVortex spawn should trigger only when the monster already has that elemental debuff and the direct elemental hit successfully applies or refreshes it.

Same-source apply cooldown prevents one tower from stacking the same elemental debuff too quickly on the same monster. When this cooldown blocks an application, no stack is added, duration is not refreshed, and normal phase extra effects do not trigger.

After normal phase resolution, the system checks whether max stacks have been reached. If max stacks are reached, overload executes, the normal debuff is removed when configured to do so, and same-element stack immunity is applied.

---

## 9. Elemental Stack Immunity

ElementalStackImmunity is a temporary state applied after elemental overload.

It prevents the monster from gaining stacks of the same element for a short duration.

It does not block damage by default.

Example:

```text
Monster triggers Cold overload
    -> Monster gains Cold ElementalStackImmunity
    -> Cold stacks cannot be added or refreshed during immunity
    -> Fire, Electric, and Wind stacks can still be applied normally
```

ElementalStackImmunity can be implemented as a system Buff when Buff runtime is available.

It should be treated as a system-level state, not as a normal elemental debuff.

---

## 10. Recursion Rule

Only direct elemental tower attacks can apply elemental stacks by default.

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

---

## 11. EffectZone

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

---

## 12. Relationship With Other Systems

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

## 13. Implementation Scope Direction

The next implementation should be split into Task Documents rather than implemented as one large change.

Recommended order:

1. Effect trigger context, Effect definition, simple target resolution, and Effect action foundation.
2. Buff definition, Monster buff runtime state, stacking, refresh, same-source cooldown, and same-element stack immunity foundation.
3. Fire vertical slice: Burning and FlameBurst.
4. Cold and Electric vertical slices.
5. EffectZone foundation.
6. WindVortex and Storm Shift after EffectZone and Monster movement/path APIs are ready.

Effect-backed Behaviour Layer upgrades such as Magic Orb Splash, Cannon Timed Shell, and Cannon Burning Shell should wait until the required Effect binding and Buff And Effect System foundation exists.

---

## 14. Summary

The Buff And Effect System handles reusable gameplay rule execution beyond simple direct attack damage.

It should grow from a small Effect foundation into Buff runtime, Elemental debuff stacking, overload, and EffectZone support through staged Task Documents.

Direct base attack damage can remain in the existing runtime path until a later DamageContext migration is explicitly reviewed.
