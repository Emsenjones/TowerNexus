# Projectile System

## 1. System Overview

The Projectile System is responsible for managing projectile lifecycle after a projectile has been spawned by the Tower Runtime Combat System.

The system manages:

- Projectile movement
- Projectile collision detection
- Projectile lifetime management
- Impact event triggering
- Projectile destruction

The Projectile System does not own tower combat logic, buff execution, area damage execution, or monster health.

---

## 2. Responsibility Boundary

### Owns

The Projectile System owns:

- Projectile runtime state
- Projectile movement
- Projectile collision detection
- Projectile lifetime management
- Impact event generation
- Direct single-target hit dispatch for projectile-to-monster impacts
- Projectile destruction

### Does Not Own

The Projectile System does not own:

- Target selection
- Attack cooldowns
- Damage formula calculation
- Area damage execution
- Buff application
- Monster health
- Tower combat logic

These responsibilities belong to other systems.

---

## 3. Core Design Philosophy

The Projectile System should remain independent from tower-specific logic.

Projectile behaviour should be determined by AttackConfig rather than tower type.

Bad:

```text
If Archer Tower
    Arrow Logic

If Cannon Tower
    Cannonball Logic
```

Good:

```text
Read AttackConfig.attackArchetype
    ↓
Spawn Projectile
    ↓
Execute Matching Projectile Behaviour
```

Projectile movement style is determined by AttackConfig.attackArchetype.

ProjectileConfig should never contain a movement type field.

Movement ownership belongs to AttackConfig.attackArchetype to avoid duplicate configuration and conflicting runtime behavior.

The Projectile System should only care about projectile runtime execution.

---

## 4. Projectile Lifecycle

Standard projectile lifecycle:

```text
Spawn
    ↓
Move
    ↓
Hit
    ↓
Trigger Impact Event
    ↓
Destroy
```

The lifecycle begins when a projectile is created and ends when the projectile is destroyed.

---

## 5. Runtime Components

The Projectile System is centered around a runtime projectile component.

Example:

```text
ProjectileBehaviour
```

ProjectileBehaviour acts as the runtime entry point of the Projectile System.

Typical responsibilities:

- Storing runtime projectile state
- Moving the projectile
- Detecting collisions
- Triggering impact events
- Managing lifetime
- Destroying the projectile

Each projectile owns an independent ProjectileBehaviour instance.

---

## 6. Runtime Projectile State

Example runtime data:

```text
Source Tower
Target Monster
Target Position
Current Position
Lifetime Timer
Projectile Config
Attack Config
Attack Damage
```

Runtime state should never be stored inside configuration assets.

---

## 7. ProjectileConfig (First Version)

The first version introduces a lightweight ProjectileConfig.

ProjectileConfig is responsible for projectile-specific runtime data and visual behavior.

ProjectileConfig should not duplicate data already owned by AttackConfig.

Examples of data that should remain in AttackConfig:

- damage
- attackRange
- attackInterval
- attackArchetype
- arcHeight

ProjectileConfig should focus on projectile-specific configuration.

Recommended first-version fields:

| projectilePrefab | GameObject | Projectile prefab reference |
| projectileSpeed | float | Projectile movement speed (Unity units per second) |
| impactEffectConfig | EffectConfig | Optional effect triggered on impact |

Notes:

- projectileSpeed controls how quickly the projectile reaches its target.
- Projectile prefabs are expected to face negative Y axis (-Y) in local space.
- ProjectileBehaviour should rotate the projectile so its local -Y direction points toward the movement direction.
- This orientation convention should be used consistently across all projectile prefabs to avoid per-projectile rotation fixes.
- impactEffectConfig is optional.
- Direct single-target projectile hits may dispatch damage directly to MonsterBehaviour.
- Complex combat results should still be represented as Effects.
- AreaDamageEffect is an example of a valid impact effect.
- The first version supports a single impact effect.
- Future versions may support multiple impact effects.

Example:

```text
Arrow
    ↓
Direct Damage
```

No impact effect required.

```text
Cannonball
    ↓
AreaDamageEffect
```

Impact effect required.

Design Principle:

```text
AttackConfig
    Owns attack behavior and damage

ProjectileConfig
    Owns projectile runtime data

EffectConfig
    Owns complex impact results
```

This separation prevents duplicate configuration and keeps responsibilities clear.

---

## 8. Projectile Movement Types

The first version supports two movement types.

### Straight Movement

Used by arrow-style projectiles.

Example:

```text
Spawn
    ↓
Move Directly Toward Target
    ↓
Hit Monster
```

---

### Arc Movement

Used by cannonball-style projectiles.

Example:

```text
Spawn
    ↓
Travel Along Arc
    ↓
Reach Target Position
```

Arc height is provided by AttackConfig.arcHeight.

---

### Projectile Orientation Convention

Projectile visual orientation should follow a single project-wide convention:

```text
Projectile Head
      ↓
Local -Y Axis
```

When a projectile is moving, ProjectileBehaviour should align the projectile's local -Y axis with the current travel direction.

This prevents projectile prefabs from appearing sideways, backwards, or requiring special-case rotation logic.

---

Future versions may support:

- Homing
- Chain
- Split
- Boomerang
- Piercing

---

## 8. Hit Detection

Different projectile types may use different hit conditions.

### Monster Collision

Example:

```text
Arrow
    ↓
Collides With Monster
    ↓
Trigger Impact Event
```

---

### Position Arrival

Example:

```text
Cannonball
    ↓
Reach Target Position
    ↓
Trigger Impact Event
```

---

The Projectile System only determines when a hit occurs.

The Projectile System does not determine what the hit does.

---

## 9. Impact Event Triggering

When a projectile hits a valid target or arrives at a valid position, it generates an impact event.

Example:

```text
Projectile Hit
    ↓
Impact Event
    ↓
Effect System
```

The Projectile System may directly dispatch single-target damage when a projectile successfully hits a monster.

This exception exists to keep simple projectile attacks lightweight.

Examples:

Arrow
    ↓
Hit Monster
    ↓
MonsterBehaviour.TakeDamage(...)

Cannonball
    ↓
Reach Target Position
    ↓
Impact Event
    ↓
AreaDamageEffect

The Projectile System should not directly apply buffs.

For complex impact behavior such as area damage, buff application, chained effects, or future special mechanics, the Projectile System should generate an impact event and delegate execution to the Buff And Effect System.

---

## 10. Relationship With Other Systems

### Tower Runtime Combat System

Responsible for:

- Creating projectiles
- Initializing projectile runtime state
- Providing target information
- Providing AttackConfig data

---

### Buff And Effect System

Responsible for:

- Area damage effects
- Buff application effects
- Visual impact effects

---

### Monster System

Responsible for:

- Health
- Damage processing
- Death handling

---

## 11. First Version Scope

The first version supports:

- Straight Movement
- Arc Movement
- Monster Collision Hit Detection
- Position Arrival Hit Detection
- Impact Event Triggering

The first version intentionally excludes:

- Homing Projectiles
- Piercing Projectiles
- Chain Projectiles
- Split Projectiles
- Ricochet Projectiles

These features may be added in future versions.

---

## 12. Summary

Projectile creation is owned by the Tower Runtime Combat System. The Projectile System begins responsibility after a projectile has been initialized.

The Projectile System manages projectile lifecycle after a projectile has been spawned by the Tower Runtime Combat System.

The system is responsible for:

- Moving projectiles
- Detecting hits
- Triggering impact events
- Destroying projectiles

The system should remain independent from tower-specific logic. Simple projectile-to-monster hits may dispatch direct single-target damage, while complex combat results should be delegated to the Buff And Effect System.