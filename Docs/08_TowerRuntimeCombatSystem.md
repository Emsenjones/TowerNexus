

# Tower Runtime Combat System

## 1. System Overview

The Tower Runtime Combat System is responsible for consuming TowerDefinition and AttackConfig data provided by the Tower Framework System and converting them into runtime combat behavior.

The system manages:

- Target detection
- Target selection
- Attack cooldowns
- Runtime combat state
- Attack execution
- Damage dispatch

The system does not define combat data.

Combat-related configuration is defined by the Tower Framework System.

---

## 2. Responsibility Boundary

### Owns

The Tower Runtime Combat System owns:

- Runtime combat state
- Enemy detection
- Target selection
- Attack cooldown management
- Attack execution logic
- Runtime attack flow
- Damage dispatch requests

### Does Not Own

The Tower Runtime Combat System does not own:

- TowerDefinition
- AttackConfig
- ProjectileConfig
- BuffConfig
- EffectConfig
- Tower placement
- Projectile movement
- Buff execution

These responsibilities belong to their respective systems.

---

## 3. Core Design Philosophy

The Tower Runtime Combat System should be completely data-driven.

The runtime system should not contain tower-specific logic.

Example:

Bad:

```text
If Archer Tower
    Shoot Arrow

If Cannon Tower
    Fire Cannonball
```

Good:

```text
Read AttackConfig
    ↓
Read AttackArchetype
    ↓
Execute Matching Runtime Logic
```

The runtime system only consumes configuration data.

---

## 4. Runtime Combat Flow

Standard runtime flow:

```text
Tower Spawned
    ↓
Load TowerDefinition
    ↓
Load AttackConfig
    ↓
Detect Enemies
    ↓
Select Target
    ↓
Cooldown Ready
    ↓
Execute Attack
    ↓
Dispatch Damage
    ↓
Return To Detection Loop
```

This loop continues until the tower is removed from the battlefield.

---

## 5. Runtime Combat State

Every deployed tower owns an independent runtime combat state.

Example runtime data:

```text
CurrentTarget
DetectedEnemies
CooldownTimer
AttackState
```

Runtime state should never be stored inside TowerDefinition or AttackConfig.

---

## 6. Enemy Detection

Enemy detection determines which enemies are currently attackable by the tower.

The first version uses a circular detection area.

Detection range comes from:

```text
AttackConfig.attackRange
```

Detected enemies are stored in a runtime collection.

Example:

```text
List<MonsterBehaviour>
```

Invalid targets should be removed automatically.

Examples:

- Dead monsters
- Despawned monsters
- Monsters leaving attack range

---

## 7. Target Selection

After enemies are detected, the system selects a target according to TargetSelectionType.

Target selection rules are defined by the Tower Framework System.

Examples:

### Nearest

Select the nearest valid enemy.

### HighestHealth

Select the enemy with the highest current health.

### LowestHealth

Select the enemy with the lowest current health.

The runtime system only executes the selected rule.

---

## 8. Cooldown Management

Attack intervals are controlled by:

```text
AttackConfig.attackInterval
```

Example:

```text
attackInterval = 1.0
```

The tower may execute one attack every second.

The cooldown timer is runtime-only data.

---

## 9. Attack Execution

Attack execution is determined by AttackArchetype.

The runtime system selects the correct execution path based on AttackConfig.

---

### 9.1 StraightProjectile

Used by Archer Tower.

Flow:

```text
Select Target
    ↓
Spawn Projectile
    ↓
Projectile System Handles Flight
```

Damage is applied when the projectile hits a valid target.

---

### 9.2 ArcProjectile

Used by Cannon Tower.

Flow:

```text
Select Target Position
    ↓
Spawn Arc Projectile
    ↓
Projectile Lands
    ↓
Area Damage
```

Explosion logic belongs to Projectile System or Effect System.

---

### 9.3 ChannelBeam

Used by Magic Tower.

Flow:

```text
Select Target
    ↓
Create Beam Connection
    ↓
Apply Continuous Damage
```

The tower remains connected to the target while channeling.

Channel duration rules are defined by AttackConfig.

---

### 9.4 PeriodicArea

Used by Watch Tower.

Flow:

```text
Detect Enemies In Area
    ↓
Periodic Tick
    ↓
Apply Damage To All Valid Targets
```

This archetype does not require projectiles.

---

## 10. Damage Dispatch

The Tower Runtime Combat System does not directly modify monster health.

Instead, it dispatches damage requests.

Example:

```text
Tower Runtime Combat
    ↓
Damage Request
    ↓
Monster System
```

Monster health calculation belongs to Monster System.

---

## 11. Relationship With Other Systems

### Tower Framework System

Provides:

- TowerDefinition
- AttackConfig
- AttackArchetype
- TargetSelectionType

---

### Projectile System

Responsible for:

- Projectile spawning
- Projectile movement
- Projectile collision
- Projectile lifetime

---

### Buff System

Responsible for:

- Buff creation
- Buff stacking
- Buff duration
- Buff removal

---

### Monster System

Responsible for:

- Health
- Damage processing
- Death handling
- Reward generation

---

## 12. First Version Scope

The first version supports:

- StraightProjectile
- ArcProjectile
- ChannelBeam
- PeriodicArea

Supported target selection:

- Nearest
- First
- Last
- HighestHealth
- LowestHealth

Advanced features are intentionally excluded:

- Multi-target chaining
- Ricochet attacks
- Smart targeting priorities
- Attack prediction
- Dynamic threat evaluation

These may be added in future versions.

---

## 13. Summary

The Tower Runtime Combat System consumes TowerDefinition and AttackConfig data defined by the Tower Framework System and converts those configurations into runtime combat behavior.

The system is responsible for:

- Detecting enemies
- Selecting targets
- Managing cooldowns
- Executing attacks
- Dispatching damage

The system should remain fully data-driven and independent from tower-specific implementations.