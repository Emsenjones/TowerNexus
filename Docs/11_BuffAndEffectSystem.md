# Buff And Effect System

## 1. System Overview

The Buff And Effect System is responsible for handling additional or complex combat results beyond direct single-target damage.

In the first version, direct projectile hit damage is not treated as an Effect.

Example:

```text
Arrow Hits Monster
    ↓
Projectile System Dispatches Direct Damage
    ↓
MonsterBehaviour Processes Damage
```

Effects are used when a combat event needs to produce additional behavior.

Example:

```text
Cannonball Reaches Target Position
    ↓
Projectile System Triggers AreaDamageEffect
    ↓
Buff And Effect System Executes Area Damage
```

The first version only needs to support AreaDamageEffect.

Buff-related interfaces may be reserved for future expansion.

---

## 2. Responsibility Boundary

### Owns

The Buff And Effect System owns:

- Effect execution
- Area damage resolution
- Future buff application
- Future buff lifecycle
- Future complex combat results

### Does Not Own

The Buff And Effect System does not own:

- Projectile movement
- Projectile collision detection
- Projectile lifetime
- Tower target selection
- Tower attack cooldown
- Monster pathfinding
- Monster death rewards

These responsibilities belong to their respective systems.

---

## 3. Core Design Philosophy

The system should avoid wrapping simple damage into unnecessary layers.

Direct single-target damage should remain simple.

Example:

```text
Projectile Hit Target
    ↓
Direct Damage
```

Effects should only be used when the result is more complex than direct single-target damage.

Examples:

```text
Area Damage
Apply Buff
Spawn Persistent Field
Chain Damage
Explosion Result
```

This keeps the first version simple while leaving clean extension points for future combat features.

---

## 4. Effect And Buff Definitions

### Effect

An Effect is an additional combat result triggered by a combat event.

Effects are usually immediate or short-lived.

Examples:

- Area damage
- Apply buff
- Spawn temporary field
- Trigger chain damage

### Buff

A Buff is a persistent status attached to a unit.

Buffs usually have duration, stack rules, and repeated or continuous behavior.

Examples:

- Poison
- Slow
- Burn
- Weakened

Buffs are not required in the first version.

---

## 5. First Version Effect Type

The first version only supports AreaDamageEffect.

---

### 5.1 AreaDamageEffect

AreaDamageEffect applies attack damage to all valid monsters within a radius around an impact position.

Monster inclusion in the radius should be evaluated using the monster-side hit/reference anchor provided by the Monster System.

Typical source:

```text
Arc Projectile Impact
```

Example flow:

```text
Projectile Impact Position
    ↓
Trigger AreaDamageEffect
    ↓
Find Monsters In Radius
    ↓
Dispatch Attack Damage To Each MonsterBehaviour
```

AreaDamageEffect data may include:

| Field | Type | Description |
|---|---|---|
| effectId | string | Unique effect identifier |
| radius | float | Area damage radius |

Attack damage is not owned by AreaDamageEffect.

Attack damage is provided by the combat event that triggered the effect.

For example:

```text
AttackConfig.damage
    ↓
Projectile Impact
    ↓
AreaDamageEffect
    ↓
Dispatch Damage To All Valid Monsters
```

The first version does not need advanced falloff rules.

All valid monsters inside the radius receive the same damage.

AreaDamageEffect does not own monster positioning, collision shape, or target validity. It consumes monster references and dispatches damage to valid monsters selected by the effect query.

---

## 6. Buff Reserved For Future

Buff support is reserved for future versions.

The first version may define interfaces or placeholders, but does not need complete buff runtime behavior.

Future buff features may include:

- Buff duration
- Buff tick interval
- Buff stacking rules
- Buff refresh rules
- Buff removal rules
- Stat modification
- Damage over time
- Slow effects

Example future flow:

```text
Projectile Hit
    ↓
ApplyBuffEffect
    ↓
Attach Buff To Monster
    ↓
Buff Runtime Updates Over Time
```

---

## 7. Relationship With Other Systems

### Projectile System

Responsible for:

- Detecting projectile hit or arrival
- Dispatching direct damage for simple single-target hits
- Triggering Effect execution for complex results
- Providing impact position and attack damage when triggering an effect

Example:

```text
Projectile Impact
    ↓
Trigger AreaDamageEffect
```

---

### Tower Runtime Combat System

Responsible for:

- Deciding when a tower attacks
- Creating projectiles when needed
- Triggering non-projectile attack behavior

Example:

```text
Watch Tower Tick
    ↓
May Trigger Area Damage Logic Directly
```

---

### Monster System

Responsible for:

- Receiving damage
- Updating monster health
- Handling monster death
- Handling monster cleanup
- Providing monster-side hit/reference anchors for effect queries

---

## 8. First Version Scope

The first version supports:

- AreaDamageEffect
- Damage dispatch to monsters within radius
- Effect trigger from projectile impact position

The first version intentionally excludes:

- Buff runtime
- Damage over time
- Slow effects
- Burn effects
- Poison effects
- Buff stacking
- Buff refresh rules
- Damage falloff by distance

These features may be added in future versions.

---

## 9. Summary

The Buff And Effect System handles additional or complex combat results beyond direct single-target damage.

In the first version, the system only needs to support AreaDamageEffect and area damage resolution.

Direct projectile hit damage remains part of the Projectile System and MonsterBehaviour damage flow.

This keeps the current combat implementation simple while preserving clear extension points for future buff and effect features.
