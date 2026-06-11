# Tower Nexus - Tower Runtime Combat System

---

# 1. System Overview

The Tower Runtime Combat System is responsible for converting static tower combat configuration into live battlefield attack behavior.

This system answers:

- When a placed tower can attack
- Which monsters are valid targets
- Which target should be selected
- Which attack archetype should execute
- When projectiles are created
- When direct runtime damage is applied
- When attack animation and presentation hooks are triggered

The Tower Runtime Combat System consumes data from the Tower Framework System and coordinates downstream runtime systems such as Projectile System, Monster System, and Buff And Effect System.

It does not define what a tower is.

It does not own tower placement, projectile movement, buff state, monster health, or static tower configuration.

---

# 2. Responsibility Boundary

The Tower Runtime Combat System owns:

- Runtime tower combat state
- Enemy detection within attack range
- Target selection execution
- Attack cooldown management
- Attack state transitions
- Attack animation parameter control
- Projectile creation and initialization
- ChannelBeam damage timing
- PeriodicArea damage timing
- Direct damage dispatch coordination
- Presentation hook triggering for attack VFX

The Tower Runtime Combat System does not own:

- TowerDefinition structure
- AttackConfig field definitions
- Tower placement workflow
- Runtime projectile movement
- Projectile collision detection
- Projectile lifetime management
- Monster spawning
- Monster movement
- Monster health state
- Buff lifetime state
- Effect asset authoring
- VFX prefab authoring, particle tuning, material tuning, or shader setup

Recommended ownership boundary:

| System | Owns |
|---|---|
| Tower Framework System | TowerDefinition, AttackConfig, attack archetype definitions, target selection definitions |
| Tower Placement System | Tower placement workflow, footprint validation, GridNode occupation |
| Tower Runtime Combat System | Tower attack state, target selection execution, cooldowns, attack execution |
| Projectile System | Projectile movement, hit detection, impact event triggering, projectile destruction |
| Monster System | Monster lifecycle, movement, health, death handling |
| Buff And Effect System | Buff application, buff lifetime, reusable effect execution |

---

# 3. Core Design Philosophy

## 3.1 Configuration Defines, Runtime Executes

Tower Framework data defines what a tower can do.

Tower Runtime Combat executes that behavior at runtime.

Example:

```text
TowerDefinition
    ↓
AttackConfig
    ↓
TowerCombatBehaviour
    ↓
Runtime Attack Execution
```

AttackConfig should not store runtime combat state.

Runtime state belongs to the tower instance currently fighting in the battlefield.

---

## 3.2 Archetype-Based Runtime Behavior

Runtime combat behavior should branch by AttackArchetype, not by tower category.

Bad:

```text
If Archer Tower
    Fire Arrow

If Cannon Tower
    Fire Cannonball
```

Good:

```text
Read AttackConfig.attackArchetype
    ↓
Execute StraightProjectile, ArcProjectile, ChannelBeam, or PeriodicArea
```

Tower categories describe design identity.

Attack archetypes describe runtime execution behavior.

---

## 3.3 Runtime Combat Coordinates, Downstream Systems Execute Their Domain

Tower Runtime Combat may start downstream behavior, but it should not absorb downstream system responsibilities.

Examples:

- It creates and initializes a projectile, then Projectile System moves and resolves that projectile.
- It selects a ChannelBeam target, then applies channel damage according to channel timing.
- It triggers PeriodicArea damage ticks, but persistent enemy-attached state belongs to Buff And Effect System.
- It may trigger attack VFX hooks, but VFX components should own visual presentation only.

---

# 4. Runtime Entry Point

The recommended runtime entry point is:

```text
TowerCombatBehaviour
```

Each placed tower that can attack should have one TowerCombatBehaviour.

TowerCombatBehaviour is initialized from:

- TowerInstance
- TowerDefinition
- AttackConfig
- MonsterManager
- Optional Animator
- Optional AttackOrigin transform

Recommended runtime references:

| Reference | Purpose |
|---|---|
| TowerInstance | Provides the placed tower instance and TowerDefinition |
| TowerDefinition | Provides static tower data |
| AttackConfig | Provides attack behavior configuration |
| MonsterManager | Provides alive monsters for detection |
| Animator | Receives attack presentation parameters |
| AttackOrigin | Provides attack range origin and projectile spawn position |

If AttackOrigin is not assigned, the tower transform may be used as the fallback origin.

---

# 5. Runtime Combat State

Tower Runtime Combat may maintain the following runtime state per tower:

- Detected enemies
- Current target
- Pending projectile target
- Pending projectile target position
- Current channel target
- Cooldown timer
- Channel timer
- Channel tick timer
- Channel damage accumulator
- Current attack state

This state should never be stored in TowerDefinition or AttackConfig.

Recommended attack states:

| State | Meaning |
|---|---|
| Idle | Tower is not currently executing an attack |
| WaitingForAnimationRelease | Tower has selected a projectile target and is waiting for the attack release moment |
| Channeling | Tower is actively channeling damage into one target |
| PeriodicAreaActive | Tower has valid enemies in range and is running periodic area behavior |

---

# 6. Update Flow

Recommended per-frame runtime flow:

```text
Validate runtime references
    ↓
Update cooldown timer
    ↓
Detect enemies within attackRange
    ↓
Read AttackConfig.attackArchetype
    ↓
Execute matching attack update
```

The attack archetype determines the update branch:

| AttackArchetype | Runtime Branch |
|---|---|
| StraightProjectile | Projectile attack update |
| ArcProjectile | Projectile attack update |
| ChannelBeam | Channel attack update |
| PeriodicArea | Periodic area update |

---

# 7. Enemy Detection

Tower Runtime Combat detects valid enemies by querying alive monsters from Monster System and filtering them by tower attack range.

A valid target should be:

- Not null
- Active in the scene
- Alive
- Inside attackRange

Range should be measured from AttackOrigin when available.

If AttackOrigin is missing, the tower transform may be used.

Tower Runtime Combat should not spawn monsters, move monsters, or own monster health.

---

# 8. Target Selection

Target selection is executed by Tower Runtime Combat using TargetSelectionType from AttackConfig.

Recommended first-version target selection rules:

| TargetSelectionType | Runtime Meaning |
|---|---|
| Nearest | Select the valid enemy closest to the tower |
| HighestHealth | Select the valid enemy with the highest current health |
| LowestHealth | Select the valid enemy with the lowest current health |
| Random | Select a random valid enemy |

TargetSelectionType is used by:

- StraightProjectile
- ArcProjectile
- ChannelBeam

TargetSelectionType is not used by PeriodicArea because PeriodicArea affects all valid enemies inside attackRange.

---

# 9. Projectile Attack Runtime

Projectile attack runtime is used by:

- StraightProjectile
- ArcProjectile

Recommended flow:

```text
Cooldown ready
    ↓
Select target
    ↓
Store pending projectile target
    ↓
Store pending projectile target position
    ↓
Enter WaitingForAnimationRelease
    ↓
Trigger attack animation if configured
    ↓
Release projectile from animation event or immediate fallback
    ↓
Create projectile
    ↓
Initialize ProjectileBehaviour
    ↓
Return to Idle
```

Projectile creation belongs to Tower Runtime Combat.

Projectile movement, collision detection, impact handling, lifetime management, and destruction belong to Projectile System.

---

## 9.1 Animation Release

Projectile attacks may wait for an animation release event before spawning the projectile.

Recommended animation event method:

```text
OnAttackAnimationRelease
```

If no attack animation trigger is configured, Tower Runtime Combat may release the projectile immediately.

If the pending target becomes invalid before the release moment, the pending attack should be canceled and the tower should return to Idle.

---

## 9.2 Projectile Initialization

When releasing a projectile, Tower Runtime Combat provides:

- Source TowerInstance
- MonsterManager
- ProjectileConfig
- AttackConfig
- Pending target
- Pending target position

ProjectileBehaviour then owns projectile runtime execution after initialization.

StraightProjectile and ArcProjectile may use the same Tower Runtime Combat release flow while Projectile System handles their different movement behavior.

---

# 10. ChannelBeam Runtime

ChannelBeam attacks select one valid target and continuously apply damage while the target remains valid and within range.

Recommended channel start flow:

```text
Cooldown ready
    ↓
Select target
    ↓
Set current channel target
    ↓
Reset channel timers
    ↓
Enter Channeling
    ↓
Set attacking animator bool
    ↓
Trigger channel started hook
```

Recommended channel update flow:

```text
Validate current channel target
    ↓
Validate attack range
    ↓
Advance channel timer
    ↓
Advance channel tick timer
    ↓
Apply damage when tick interval is reached
    ↓
Stop channel when max duration is reached
```

Recommended channel end flow:

```text
Clear current channel target
    ↓
Reset channel timers
    ↓
Enter Idle
    ↓
Start cooldown
    ↓
Clear attacking animator bool
    ↓
Trigger channel ended hook
```

ChannelBeam damage is owned by Tower Runtime Combat in the first version.

Persistent status effects applied by future channel attacks should be delegated to Buff And Effect System.

---

# 11. PeriodicArea Runtime

PeriodicArea attacks damage all valid enemies inside attackRange at a fixed interval.

Recommended flow:

```text
Detect enemies
    ↓
If no valid enemies exist, enter Idle
    ↓
If valid enemies exist, enter PeriodicAreaActive
    ↓
When cooldown reaches zero, damage each valid enemy
    ↓
Reset cooldown to attackInterval
    ↓
Trigger periodic area tick hook
```

PeriodicArea does not select one target.

PeriodicArea does not use TargetSelectionType.

PeriodicArea direct damage is owned by Tower Runtime Combat in the first version.

If future PeriodicArea attacks apply persistent states such as slow, burn, poison, or armor reduction, those states should be owned by Buff And Effect System.

---

# 12. Animation Integration

Tower Runtime Combat may control Animator parameters configured by AttackConfig.

Recommended fields:

| Field | Runtime Usage |
|---|---|
| attackAnimatorTriggerName | Triggered when a projectile attack starts |
| attackingAnimatorBoolName | Set while continuous attacks are active |

Recommended default values:

| Field | Value |
|---|---|
| attackAnimatorTriggerName | Attack |
| attackingAnimatorBoolName | IsAttacking |

Animator parameter names should come from AttackConfig.

Tower Runtime Combat should not hardcode tower-specific animation parameter names.

---

# 13. Runtime Presentation Hooks

Tower Runtime Combat may expose attack lifecycle hooks for VFX and presentation systems.

Recommended hooks:

| Hook | Trigger Timing |
|---|---|
| OnProjectileReleased | After projectile release is confirmed |
| OnChannelStarted | When ChannelBeam enters Channeling |
| OnChannelEnded | When ChannelBeam exits Channeling |
| OnPeriodicAreaTick | When PeriodicArea applies a damage tick |

These hooks are presentation and integration points.

They must not transfer combat authority to VFX components.

VFX components must not own:

- Damage
- Target selection
- Target searching
- Range checks
- Cooldown logic
- Attack state transitions

Optional VFX runtime spawning, binding, update, stop, and cleanup should remain presentation-only runtime behavior owned by Tower Runtime Combat or dedicated VFX presentation components.

---

# 14. Relationship With Projectile System

Tower Runtime Combat owns projectile creation and initialization.

Projectile System owns projectile runtime lifecycle after initialization.

Boundary:

```text
Tower Runtime Combat
    ↓
Instantiate projectile prefab
    ↓
Initialize ProjectileBehaviour
    ↓
Projectile System
    ↓
Move, detect hit, trigger impact, destroy
```

Tower Runtime Combat should not update projectile movement after the projectile has been initialized.

Projectile System should not select tower targets or manage tower cooldowns.

---

# 15. Relationship With Buff And Effect System

Tower Runtime Combat may directly apply simple runtime damage for first-version non-projectile attacks.

Examples:

- ChannelBeam damage ticks
- PeriodicArea damage ticks

Buff And Effect System should own reusable effect and buff execution.

Examples:

- AreaDamageEffect triggered by a projectile impact
- Buff application effects
- Enemy-attached states such as poison, slow, burn, weaken, or armor reduction

Tower Runtime Combat should delegate future complex effects instead of embedding buff-specific logic into tower combat code.

---

# 16. Relationship With Tower Placement System

Tower Placement System creates or places the runtime tower object.

After placement, the tower may receive or already contain TowerCombatBehaviour.

Tower Placement System may initialize TowerCombatBehaviour with TowerInstance and MonsterManager references.

Tower Placement System should not:

- Select combat targets
- Manage tower attack cooldowns
- Execute damage
- Spawn projectiles as combat behavior
- Own attack animation state

---

# 17. First Version Scope

Included:

- TowerCombatBehaviour runtime entry point
- AttackConfig consumption
- Enemy detection
- Target selection
- Attack cooldowns
- Projectile attack release flow
- Projectile creation and initialization
- ChannelBeam runtime damage
- PeriodicArea runtime damage
- Attack animation parameter control
- Runtime presentation hooks

Excluded:

- Projectile movement implementation
- Projectile hit detection implementation
- Projectile impact VFX
- Projectile travel VFX
- Buff lifetime implementation
- Tower upgrade modifiers
- Object pooling
- Final VFX prefab authoring and particle polish
- Particle collision driven combat logic

---

# 18. Summary

Tower Runtime Combat is the live execution layer for placed towers.

It consumes TowerDefinition and AttackConfig, manages runtime combat state, selects targets, runs cooldowns, executes attack archetypes, creates projectiles, and coordinates simple damage dispatch.

It should remain between Tower Framework data and downstream runtime systems without taking over placement, projectile lifecycle, monster lifecycle, or buff state ownership.

---

# Change Log

## 2026-06-11

- Reorganized the document as a full Tower Runtime Combat System design document.
- Clarified runtime ownership boundaries against Tower Framework, Tower Placement, Projectile, Monster, and Buff And Effect systems.
- Added sections for runtime state, update flow, enemy detection, target selection, projectile attacks, ChannelBeam, PeriodicArea, animation integration, and presentation hooks.
- Clarified runtime VFX presentation ownership within the long-term system design.
