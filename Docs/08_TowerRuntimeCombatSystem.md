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
- Runtime playback control for configured tower attack VFX
- Runtime binding for continuous VFX such as beam and area effects

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
- VFX prefab authoring
- VFX material or shader setup

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

### Runtime State Ownership

TowerCombatBehaviour owns all runtime combat state.

Examples:

```text
CurrentTarget
DetectedEnemies
CooldownTimer
AttackState
ChannelTimer
```

TowerDefinition and AttackConfig are immutable runtime inputs.

Runtime combat logic should never write state back into configuration assets.

---

## 6. Enemy Detection

Enemy detection determines which enemies are currently attackable by the tower.

The first version uses a circular detection area.

Detection range comes from:

```text
AttackConfig.attackRange
```

Detection is measured from the tower attackOrigin.

attackOrigin represents the actual attack launch point configured by the tower prefab.

attackOrigin may be different from the tower root transform and should be used as the center point for runtime enemy detection and attack range validation.

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

### Attack Visual Effect Runtime Hooks

Tower Runtime Combat should consume VFX references defined by AttackConfig and play them at the correct runtime timing.

The runtime system owns when and where configured attack VFX are spawned, attached, updated, stopped, or destroyed.

The runtime system does not create final VFX assets or tune their materials, particles, shaders, colors, or timing polish.

Common runtime VFX hooks:

- Projectile release VFX spawned at attackOrigin when a StraightProjectile or ArcProjectile is released
- Projectile impact VFX triggered by projectile impact handling
- ChannelBeam VFX spawned when channeling starts and updated while the target remains valid
- PeriodicArea field VFX spawned when the area attack becomes active and stopped when no valid enemies remain

One-shot VFX should usually be instantiated, played, and destroyed after completion.

Looping VFX should be explicitly started, attached or positioned, and stopped when the corresponding runtime state ends.

Continuous VFX must remain presentation-only. Damage timing, target validation, cooldowns, and hit logic remain owned by runtime combat logic.

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

Runtime VFX behavior:

```text
Animation Event Releases Projectile
    ↓
Spawn projectileReleaseVfxPrefab at attackOrigin if configured
```

The release VFX is presentation-only and does not affect projectile launch direction, damage, or hit detection.

Projectile travel visuals belong to the Projectile System and projectile prefab setup rather than Tower Runtime Combat.

Additional rules:

- The selected target is used only to determine projectile launch direction.
- After launch, the projectile travels independently.
- StraightProjectile hit detection belongs to the Projectile System.
- The projectile is not required to hit the originally selected target.
- The projectile may hit any valid monster encountered during flight.
- The projectile is automatically destroyed when its maximum lifetime expires.

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

Runtime VFX behavior:

```text
Animation Event Releases Projectile
    ↓
Spawn projectileReleaseVfxPrefab at attackOrigin if configured
    ↓
Projectile landing or impact handling spawns explosion / impact VFX if configured
```

The cannon explosion visual should be triggered by projectile impact or effect execution timing, not by an independent particle collision result.

Projectile travel visuals belong to the Projectile System and projectile prefab setup rather than Tower Runtime Combat.

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

Runtime VFX behavior:

```text
Channel State Starts
    ↓
Spawn channelBeamVfxPrefab if configured
    ↓
Bind beam start to attackOrigin
    ↓
Bind beam end to current target HitAnchor or fallback target transform
    ↓
Update beam start and end every frame while channeling
    ↓
Stop and destroy beam VFX when channeling ends
```

ChannelBeam VFX should behave as a runtime visual controller rather than a one-shot particle effect.

The beam visual should not apply damage, search for targets, or decide whether the attack hits.

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

Runtime VFX behavior:

```text
One Or More Valid Enemies In Range
    ↓
Spawn periodicAreaVfxPrefab if configured and not already active
    ↓
Keep field VFX centered on attackOrigin or tower origin
    ↓
Use authored PeriodicArea VFX scale
    ↓
Stop and destroy field VFX when no valid enemies remain
```

PeriodicArea VFX should be looping presentation only. Periodic damage is still applied by Tower Runtime Combat using attackInterval and attackRange.

This archetype does not require projectiles.

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

- VFX prefab authoring
- Projectile muzzle flash visuals
- Beam or laser visual prefab authoring
- Periodic area field visual prefab authoring
- Impact visual prefab authoring
- Particle, material, shader, color, and timing polish

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
- Runtime hooks for configured projectile release VFX
- Runtime hooks for configured ChannelBeam VFX
- Runtime hooks for configured PeriodicArea VFX

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
- Final Beam VFX art quality
- Final PeriodicArea VFX art quality
- Particle collision driven damage or hit detection

These may be added in future versions.

---

## 13. Summary

The Tower Runtime Combat System consumes TowerDefinition and AttackConfig data defined by the Tower Framework System and converts those configurations into runtime combat behavior.

The system is responsible for:

- Detecting enemies
- Selecting targets
- Managing cooldowns
- Owning runtime combat state
- Executing attacks
- Driving attack animation parameters configured by AttackConfig
- Providing runtime playback hooks for tower attack visual effects
- Dispatching damage

The system should remain fully data-driven and independent from tower-specific implementations.
