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
- Animator parameter control for tower attack state
- Runtime hooks for attack visual effects
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
- Detailed VFX asset creation
- Final VFX timing and art polish

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
Cooldown Ready / Attack State Ready
    ↓
Read AttackConfig Animator Presentation Parameters
    ↓
Trigger Attack Animation State
    ↓
Animation Event Or Runtime Tick Executes Attack Payload
    ↓
Dispatch Damage Or Spawn Projectile / Effect
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
IsAttacking
CurrentChannelTarget
ChannelTimer
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

### Random

Select a random valid enemy.

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

### Animator-Driven Attack Control

Tower attack presentation should be animation-driven where possible.

Projectile-based attacks typically use the Trigger parameter configured by:

```text
AttackConfig.attackAnimatorTriggerName
```

Recommended default:

```text
Attack
```

Flow:

```text
Cooldown Ready
    ↓
Read AttackConfig.attackAnimatorTriggerName
    ↓
Animator.SetTrigger(attackAnimatorTriggerName)
    ↓
Attack Animation Event
    ↓
Release Projectile / Attack Payload
```

Continuous or stateful attacks typically use the Bool parameter configured by:

```text
AttackConfig.attackingAnimatorBoolName
```

Recommended default:

```text
IsAttacking
```

Examples:

```text
ChannelBeam: Animator.SetBool(attackingAnimatorBoolName, true / false)
PeriodicArea: Animator.SetBool(attackingAnimatorBoolName, true / false)
```

For continuous attacks, the Animator controls whether the tower is visually in an attacking state, while runtime logic still owns target validation, damage ticks, cooldown timing, and attack stop conditions.

If no Animator is configured, the runtime may fall back to logic-only attack execution for prototype safety.

### Attack Visual Effect Hooks

Tower Runtime Combat should leave runtime hooks for attack visual effects, but detailed VFX assets and polish are not part of the first implementation.

Examples of future visual hooks:

- Muzzle flash or fire burst when a projectile is released
- ChannelBeam laser connection from tower attack point to target
- PeriodicArea field effect centered on the tower attack point
- Impact effect triggered by projectile or effect execution

The runtime combat implementation should expose clear extension points for these effects without implementing final VFX behavior.

---

### 9.1 StraightProjectile

Used by Archer Tower.

Flow:

```text
Select Target
    ↓
Animator.SetTrigger(attackAnimatorTriggerName)
    ↓
Animation Event
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
Animator.SetTrigger(attackAnimatorTriggerName)
    ↓
Animation Event
    ↓
Spawn Arc Projectile
    ↓
Projectile Lands
    ↓
Impact Effect / Area Damage
```

Explosion logic belongs to Projectile System or Effect System.

---

### 9.3 ChannelBeam

Used by Magic Tower.

Flow:

```text
Select Target
    ↓
Animator.SetBool(attackingAnimatorBoolName, true)
    ↓
Start Channel State
    ↓
Every channelDamageInterval
Apply Damage
    ↓
Channel Duration Reaches maxChannelDuration
OR Target Becomes Invalid
    ↓
Animator.SetBool(attackingAnimatorBoolName, false)
    ↓
Enter Cooldown
    ↓
Cooldown Reaches attackInterval
    ↓
Search For Target Again
```

The tower remains connected to the target while channeling.

ChannelBeam should reserve a visual hook for a future beam or laser effect from the tower attack point to the current target.

Channel duration rules are defined by AttackConfig.

ChannelBeam damage is applied in discrete damage ticks. The first version uses AttackConfig.damage together with AttackConfig.channelDamageInterval. A separate continuous damage field is not required.

---

### 9.4 PeriodicArea

Used by Watch Tower.

Flow:

```text
One Or More Valid Enemies In Range
    ↓
Animator.SetBool(attackingAnimatorBoolName, true)
    ↓
Every attackInterval
Apply Damage To All Valid Targets
    ↓
No Valid Enemies Remain
    ↓
Animator.SetBool(attackingAnimatorBoolName, false)
```

This archetype does not require projectiles.

PeriodicArea should reserve a visual hook for a future area field effect centered on the tower attack point or tower origin.

---

## 10. Damage Dispatch

Damage dispatch depends on attack archetype.

Simple projectile-to-monster hits may be dispatched by the Projectile System:

```text
Straight Projectile
    ↓
ProjectileBehaviour
    ↓
MonsterBehaviour.TakeDamage(...)
```

Projectile impact area damage is delegated through the Buff And Effect System:

```text
Arc Projectile Impact
    ↓
ProjectileImpactContext
    ↓
AreaDamageEffectExecutor
    ↓
MonsterBehaviour.TakeDamage(...)
```

Non-projectile tower attacks are dispatched by Tower Runtime Combat:

```text
ChannelBeam / PeriodicArea
    ↓
Tower Runtime Combat Damage Tick
    ↓
MonsterBehaviour.TakeDamage(...)
```

Monster health, death state, and death handling remain owned by the Monster System.

---

## 11. Relationship With Other Systems

### Tower Framework System

Provides:

- TowerDefinition
- AttackConfig
- AttackArchetype
- TargetSelectionType
- attackAnimatorTriggerName
- attackingAnimatorBoolName

---

### Projectile System

Responsible for:

- Projectile spawning
- Projectile movement
- Projectile collision
- Projectile lifetime
- Simple single-target projectile hit damage
- Projectile impact context generation

---

### Visual Effect System / Art Assets

Future responsibility:

- Projectile muzzle flash
- Beam or laser visuals
- Periodic area field visuals
- Impact visuals

The runtime combat implementation should only reserve runtime hooks and references for these effects. Final VFX asset creation and polish are outside the first Tower Runtime Combat implementation.

---

### Buff And Effect System

Responsible for:

- AreaDamageEffect execution
- Future buff creation
- Future buff stacking
- Future buff duration
- Future buff removal

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
- Animator Trigger control for projectile attack release using `AttackConfig.attackAnimatorTriggerName`
- Animator Bool control for continuous attack states using `AttackConfig.attackingAnimatorBoolName`
- Placeholder hooks for attack visual effects

Supported target selection:

- Nearest
- HighestHealth
- LowestHealth
- Random

TargetSelectionType is ignored by PeriodicArea because the archetype applies damage to all valid enemies inside attackRange.

Advanced features are intentionally excluded:

- Multi-target chaining
- Ricochet attacks
- Smart targeting priorities
- Attack prediction
- Dynamic threat evaluation
- Final VFX asset implementation
- Advanced VFX timing and polish
- Beam rendering implementation details
- Periodic area field rendering implementation details

These may be added in future versions.

---

## 13. Summary

The Tower Runtime Combat System consumes TowerDefinition and AttackConfig data defined by the Tower Framework System and converts those configurations into runtime combat behavior.

The system is responsible for:

- Detecting enemies
- Selecting targets
- Managing cooldowns
- Executing attacks
- Driving attack animation parameters configured by AttackConfig
- Providing extension hooks for attack visual effects
- Dispatching damage

The system should remain fully data-driven and independent from tower-specific implementations.
