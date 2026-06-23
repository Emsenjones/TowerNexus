# Tower Nexus - Tower Runtime Combat System

---

# 1. System Overview

The Tower Runtime Combat System is responsible for converting static tower combat configuration into live battlefield attack behavior.

This system answers:

- When a placed tower can attack
- Which monsters are valid targets
- Which target should be selected
- Which attack archetype or Attack Entity behavior should execute
- When Attack Entities or projectiles are created
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
- Attack Entity spawning and control
- Projectile creation and initialization
- Magic Orb lifecycle orchestration
- Drone launch, hover, return, and recharge orchestration
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

## 3.2 Attack Entity-Based Runtime Behavior

Runtime combat behavior should be expressed through attack archetypes and Attack Entity behavior, not hardcoded tower category branches.

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
Spawn or control Arrow, Shell, Magic Orb, or Drone Attack Entity behavior
```

Tower categories describe design identity.

Attack archetypes and Attack Entities describe runtime execution behavior.

---

## 3.3 Runtime Combat Coordinates, Downstream Systems Execute Their Domain

Tower Runtime Combat may start downstream behavior, but it should not absorb downstream system responsibilities.

Examples:

- It creates and initializes a projectile, then Projectile System moves and resolves that projectile.
- It spawns or controls a Magic Orb, then Magic Orb behavior owns orbit, contact detection, hit count, and lifetime.
- It launches a Drone, then Drone behavior owns move, hover, battery, return, recharge, and projectile fire timing.
- Drone is an Attack Entity which may spawn Projectile Attack Entities.
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
| MonsterBehaviour.HitAnchor | Provides the monster-side hit/reference position for targeting, range checks, projectile target snapshots, Magic Orb contact checks, and Drone hover targeting |

If AttackOrigin is not assigned, the tower transform may be used as the fallback origin.

---

# 5. Runtime Combat State

Tower Runtime Combat may maintain the following runtime state per tower:

- Detected enemies
- Current target
- Pending projectile target
- Pending projectile target position, usually captured from the target monster hit/reference anchor
- Cooldown timer
- Active Magic Orb reference
- Magic Orb cooldown or respawn timer
- Active Drone reference
- Drone state
- Drone target
- Drone battery timer
- Drone recharge timer
- Current attack state

This state should never be stored in TowerDefinition or AttackConfig.

Recommended attack states:

| State | Meaning |
|---|---|
| Idle | Tower is not currently executing an attack |
| WaitingForAnimationRelease | Tower has selected a projectile target and is waiting for the attack release moment |
| MagicOrbActive | Tower currently owns an active Magic Orb |
| DroneResting | Drone is resting on the tower and waiting for launch conditions |
| DroneLaunched | Drone is away from the tower and executing move, hover, fire, or return behavior |
| DroneRecharging | Drone has returned and is recharging before it may launch again |

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
| Direction Projectile | Projectile attack update |
| Arc Projectile | Projectile attack update |
| Magic Orb | Magic Orb lifecycle update |
| Drone | Drone lifecycle update |

## 6.1 Cooldown Timing

Cooldown timing is based on when the active attack process begins or ends for each attack archetype.

Projectile-style attacks start cooldown when the projectile is successfully released:

- Direction Projectile cooldown starts when the projectile is released.
- Arc Projectile cooldown starts when the projectile is launched.
- Tracking Projectile cooldown starts when the projectile is released.

Persistent Attack Entities start cooldown when the active entity finishes its attack process:

- Magic Orb cooldown starts when the active Magic Orb ends or is destroyed by its hit-count lifetime.
- Drone cooldown or recharge timing starts when the Drone returns to the tower.

Magic Orb existence and Drone active flight are themselves part of the attack process, so their tower cooldown should not start when they are spawned or launched.

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

The monster-side reference point for range and target distance evaluation is provided by the Monster System through MonsterBehaviour.HitAnchor.

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

- Direction Projectile
- Arc Projectile
- Drone

TargetSelectionType is not used by first-version Magic Orb behavior because the orb detects monster contact while orbiting.

---

# 9. Projectile Attack Runtime

Projectile attack runtime is used by:

- Direction Projectile
- Arc Projectile
- Projectile Attack Entities fired by Drone

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

Attack cooldown starts immediately after Archer arrows and Cannon shells are fired, not after projectile impact or explosion.
Tracking Projectile follows the same projectile-style cooldown rule when implemented: cooldown starts after projectile release, not after impact.

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

Direction Projectile and Arc Projectile may use the same Tower Runtime Combat release flow while Projectile System handles their different movement behavior.

---

# 10. Magic Orb Runtime

Magic Orb behavior represents a persistent orbiting attack entity owned by Magic Tower.

Recommended Magic Orb flow:

```text
Cooldown ready
    ↓
Spawn Magic Orb
    ↓
Orbit around tower
    ↓
Contact detection against monsters
    ↓
Apply damage on successful contact
    ↓
Decrease hit count
    ↓
If hit count reaches zero, despawn Magic Orb
    ↓
Start cooldown
```

Magic Orb rules:

- Magic Orb rotates around the tower.
- Magic Orb checks distance to monsters.
- Contact deals damage.
- Magic Orb has a configurable maximum hit count.
- Hit count decreases after each successful hit.
- The same monster cannot be hit again by the same Magic Orb until sameTargetHitCooldown has elapsed.
- When hit count reaches zero, the Magic Orb disappears.
- Cooldown starts after the active Magic Orb ends.
- After cooldown, a new Magic Orb may be generated.

Magic Orb damage is owned by attack entity behavior in the first version.

Magic Orb combat parameters such as orbit radius, contact distance, and same-target hit cooldown belong to AttackConfig because they define shared attack rules.

MagicOrbBehaviour executes orbit movement, contact detection, hit count consumption, and lifetime using AttackConfig data.

Persistent status effects applied by future Magic Orb upgrades should be delegated to Buff And Effect System.

---

# 11. Drone Runtime

Drone behavior represents an autonomous Attack Entity launched by Drone Tower.

Recommended Drone flow:

```text
Drone resting at AttackOrigin
    ↓
Enemy enters tower AttackRange
    ↓
Launch Drone and rise to DroneLaunchHeight
    ↓
Select target inside tower AttackRange
    ↓
Move to hover point
    ↓
Hover while facing target
    ↓
Fire projectile attack entities at attack interval
    ↓
Consume battery while active
    ↓
Return when battery is depleted or no valid monsters remain inside tower AttackRange
    ↓
Recharge
    ↓
Launch again if monsters exist
```

Drone runtime rules:

- Drone rests on the tower when inactive.
- Drone rest position should use AttackOrigin when available.
- Drone launches from AttackOrigin when available.
- Drone first rises from AttackOrigin to AttackConfig.droneLaunchHeight.
- Drone maintains launch height during active flight.
- DroneBehaviour owns target selection while using AttackConfig.targetSelectionType.
- Drone target selection only considers valid monsters inside the source tower AttackRange.
- Drone movement speed should be configured on AttackConfig.
- Drone relocates when the target moves away from the desired hover distance.
- Drone periodically fires straight projectiles in the first version.
- Drone-fired projectile data should come from AttackConfig.droneProjectileConfig.
- Drone-fired projectiles should spawn from the Drone FireAnchor when available.
- Drone-fired attack release VFX should spawn from the Drone FireAnchor when configured.
- Drone flight consumes battery.
- Drone returns to AttackOrigin for recharge when battery is depleted or no valid monsters remain inside source tower AttackRange.
- Drone recharge or cooldown timing starts after the Drone returns to the tower, not when it launches.
- Drone is an Attack Entity which may spawn Projectile Attack Entities.
- DroneBehaviour should own Drone Animator driving for the Drone prefab.
- Drone Animator may use Bool parameter `IsFlying`.
- `IsFlying` should be false while Drone is Resting or Recharging.
- `IsFlying` should be true while Drone is Launching, Hovering, or Returning.
- During active flight, Drone model facing assumes local +Z points forward.
- When Drone is resting or recharging at AttackOrigin, runtime should align it to face local -Z relative to AttackOrigin in the first version.

Drone uses attackRange as the tower detect and launch range in the first version.

Drone projectile movement and projectile hit detection belong to Projectile System after projectile creation.

Persistent status effects applied by future Drone projectiles should be delegated to Buff And Effect System.

For Drone Tower, AttackOrigin acts as the Drone parking, launch, return, and recharge anchor in the first version.

FireAnchor is owned by the Drone prefab or DroneBehaviour because it moves with the Drone.

Neither AttackOrigin nor FireAnchor should own gameplay decisions such as target selection, battery rules, projectile hit detection, or damage.

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

AttackConfig.attackReleaseVfxPrefab may be spawned as a one-shot presentation effect when a tower attack or Attack Entity release is confirmed.

Recommended hooks:

| Hook | Trigger Timing |
|---|---|
| OnProjectileReleased | After projectile release is confirmed |
| OnAttackEntitySpawned | When an Attack Entity is created or launched |
| OnAttackEntityEnded | When an Attack Entity finishes, returns, despawns, or is destroyed |
| OnMagicOrbHit | When Magic Orb contact damage is applied |
| OnDroneStateChanged | When Drone launch, hover, return, or recharge state changes |

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

- Magic Orb contact damage, if implemented directly by runtime combat before a dedicated Attack Entity runtime exists
- Drone-fired projectile damage follows projectile impact rules

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
- Magic Orb lifecycle orchestration
- Drone lifecycle orchestration
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

It consumes TowerDefinition and AttackConfig, manages runtime combat state, selects targets, runs cooldowns, executes attack archetypes, creates or controls Attack Entities, creates projectiles, and coordinates simple damage dispatch.

It should remain between Tower Framework data and downstream runtime systems without taking over placement, projectile lifecycle, monster lifecycle, or buff state ownership.

---

# Change Log

## 2026-06-23

- Added explicit cooldown timing rules for projectile-style attacks, Magic Orb, and Drone.

## 2026-06-22

- Updated current runtime direction from ChannelBeam and PeriodicArea first-version behavior to Magic Orb and Drone attack entity behavior.
- Added Attack Entity responsibility language and Drone as an Attack Entity that may spawn Projectile Attack Entities.
- Updated target selection, runtime state, presentation hooks, and first-version scope for Archer, Cannon, Magic, and Drone.

## 2026-06-12

- Added MonsterBehaviour.HitAnchor as the monster-side hit/reference anchor consumed by tower targeting, range checks, projectile target snapshots, and beam binding.

## 2026-06-11

- Reorganized the document as a full Tower Runtime Combat System design document.
- Clarified runtime ownership boundaries against Tower Framework, Tower Placement, Projectile, Monster, and Buff And Effect systems.
- Added sections for runtime state, update flow, enemy detection, target selection, projectile attacks, ChannelBeam, PeriodicArea, animation integration, and presentation hooks.
- Clarified runtime VFX presentation ownership within the long-term system design.
