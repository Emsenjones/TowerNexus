

# Projectile System

## 1. System Overview

The Projectile System is responsible for managing projectile lifecycle after a projectile has been spawned by the Tower Runtime Combat System.

The system manages:

- Projectile spawning
- Projectile movement
- Projectile collision detection
- Projectile lifetime
- Impact event triggering
- Projectile destruction

The Projectile System does not calculate damage, apply buffs, or modify monster health.

These responsibilities belong to the Buff and Effect System and Monster System.

---

## 2. Responsibility Boundary

### Owns

The Projectile System owns:

- Projectile runtime state
- Projectile movement
- Projectile collision detection
- Projectile lifetime management
- Impact event generation
- Projectile destruction

### Does Not Own

The Projectile System does not own:

- Target selection
- Attack cooldowns
- Damage calculation
- Buff application
- Monster health
- Tower combat logic

These responsibilities belong to other systems.

---

## 3. Core Design Philosophy

The Projectile System should remain independent from tower-specific logic.

Projectile behaviour should be determined by projectile configuration rather than tower type.

Bad:

```text
If Archer Tower
    Arrow Logic

If Cannon Tower
    Cannonball Logic
```

Good:

```text
Read Projectile Config
    ↓
Read Movement Type
    ↓
Execute Matching Projectile Behaviour
```

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
```

Runtime state should never be stored inside tower configuration.

---

## 7. Projectile Movement Types

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

The Projectile System should not directly apply damage.

The Projectile System should not directly apply buffs.

Instead, the Projectile System notifies the Effect System.

---

## 10. Relationship With Other Systems

### Tower Runtime Combat System

Responsible for:

- Creating projectiles
- Providing target information

---

### Buff And Effect System

Responsible for:

- Direct damage effects
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

The Projectile System manages projectile lifecycle after a projectile has been spawned by the Tower Runtime Combat System.

The system is responsible for:

- Spawning projectiles
- Moving projectiles
- Detecting hits
- Triggering impact events
- Destroying projectiles

The system should remain independent from tower-specific logic and should delegate combat results to the Buff and Effect System.