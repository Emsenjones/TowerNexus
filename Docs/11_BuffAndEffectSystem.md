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

The first effect foundation only needs to support instant area damage.

Delayed area damage and repeated area damage over duration are extensions of the same effect foundation. They should be added when adopted Behaviour Layer upgrades require them, instead of being duplicated inside individual tower runtimes.

Buff-related interfaces may be reserved for future expansion.

Projectile impact visual effects are separate from gameplay Effects.

Impact VFX is configured by ProjectileConfig and triggered by the Projectile System when projectile impact occurs.

The Buff And Effect System should not be required for purely visual impact feedback.

Tower upgrade behaviours that only change attack shape or released attack entity count do not require this system.

Tower upgrade behaviours that create reusable area damage, delayed area damage, repeated area damage, buffs, or other complex combat results should delegate that execution to this system once those effects are in scope.

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
- Projectile impact VFX spawning
- Projectile impact VFX cleanup
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

The first effect foundation supports AreaDamageEffect.

Delayed or repeated area damage should extend this section only when an approved gameplay behaviour needs it.

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
| radius | float | Area damage radius |

EffectConfig assets are referenced directly by gameplay data that needs an effect configuration. They should not maintain a hand-authored effect id unless a future persistence, external-data, or lookup requirement needs a stable id. Debug output should use the ScriptableObject asset name.

AreaDamageEffect data should not include projectile impact VFX prefab references in the first version.

Projectile-specific impact presentation belongs to ProjectileConfig.

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

### 5.2 Delayed And Repeated Area Damage

Delayed area damage represents area damage that is triggered after a configured delay.

Repeated area damage represents area damage that ticks over a configured duration.

These are effect-foundation extensions, not separate tower-owned query systems.

Typical Behaviour Layer consumers:

- Cannon Timed Shell
- Cannon Burning Shell

These effects should reuse the same monster validity and radius-evaluation contract as AreaDamageEffect.

They should not own projectile movement, projectile impact VFX, tower target selection, or monster health.

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
- Triggering optional projectile impact VFX
- Providing impact position and attack damage when triggering an effect

Example:

```text
Projectile Impact
    ↓
Trigger Optional Impact VFX
    ↓
Trigger AreaDamageEffect
```

Impact VFX playback must remain presentation-only. It must not affect area damage resolution, direct damage dispatch, target selection, or monster validity checks.

---

### Tower Runtime Combat System

Responsible for:

- Deciding when a tower attacks
- Creating projectiles when needed
- Triggering non-projectile attack behavior

Example:

```text
Attack Entity Contact Or Projectile Impact
    ↓
May Trigger Direct Damage Or Effect Execution Depending On The Attack Result
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
