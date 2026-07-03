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
- When attack hit, contact, or impact events can provide context for reusable Effect execution
- When attack animation and presentation requests are issued

The Tower Runtime Combat System consumes data from the Tower Framework System and coordinates downstream runtime systems such as Projectile System, Monster System, and Buff And Effect System.

It does not define what a tower is.

It does not own tower placement, projectile movement, buff state, elemental stack rules, overload rules, monster health, or static tower configuration.

---

# 2. Responsibility Boundary

The Tower Runtime Combat System owns:

- Runtime tower combat state
- Enemy detection within attack range
- Target selection execution
- Attack cooldown management
- Runtime stat resolution from base config plus tower upgrade state
- Attack state transitions
- Attack presentation request timing
- Attack Entity release orchestration
- Projectile creation and initialization
- Magic Orb release orchestration
- Drone release orchestration
- Direct damage dispatch coordination
- Effect trigger context coordination at attack hit, contact, or impact boundaries
- Presentation hook triggering for attack VFX

The Tower Runtime Combat System does not own:

- TowerDefinition structure
- AttackConfig field definitions
- TowerVisualController ownership
- Tower model replacement
- AttackOrigin fallback resolution
- Model presentation hierarchy lookup
- Tower placement workflow
- Runtime projectile movement
- Projectile collision detection
- Projectile lifetime management
- Monster spawning
- Monster movement
- Monster health state
- Buff lifetime state
- Elemental stack, overload, or stack immunity rules
- Effect asset authoring
- VFX prefab authoring, particle tuning, material tuning, or shader setup

Recommended ownership boundary:

| System | Owns |
|---|---|
| Tower Framework System | TowerDefinition, AttackConfig, attack archetype definitions, target selection definitions |
| Tower Placement System | Tower placement workflow, footprint validation, GridNode occupation |
| Tower Runtime Combat System | Tower attack state, target selection execution, resolved runtime stats, cooldowns, attack execution |
| Projectile System | Projectile movement, hit detection, impact event triggering, projectile destruction |
| Monster System | Monster lifecycle, movement, health, death handling |
| Buff And Effect System | Buff application, buff lifetime, reusable effect execution, Elemental stack and overload rules |

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
Release Arrow, Shell, Magic Orb, or Drone Attack Entity behavior
```

Tower categories describe design identity.

Attack archetypes and Attack Entities describe runtime execution behavior.

---

## 3.3 Runtime Combat Coordinates, Downstream Systems Execute Their Domain

Tower Runtime Combat may start downstream behavior, but it should not absorb downstream system responsibilities.

Examples:

- It creates and initializes a projectile, then Projectile System moves and resolves that projectile.
- It releases a Magic Orb, then Magic Orb behavior owns orbit, contact detection, hit count, and lifetime from its release-time orbit center.
- It releases a Drone, then Drone behavior owns movement, target orbit, battery, battery-end destruction, and projectile burst fire timing.
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
- Optional tower model presentation entry resolved by the tower runtime
- Current active AttackOrigin transform resolved by the tower runtime

Recommended runtime references:

| Reference | Purpose |
|---|---|
| TowerInstance | Provides the placed tower instance and TowerDefinition |
| TowerDefinition | Provides static tower data |
| AttackConfig | Provides attack behavior configuration |
| MonsterManager | Provides alive monsters for detection |
| Tower Model Presentation | Receives attack presentation requests and applies model-local presentation parameters |
| Current Active AttackOrigin | Provides attack range origin and projectile spawn position |
| MonsterBehaviour.HitAnchor | Provides the monster-side hit/reference position for targeting, range checks, projectile target snapshots, Magic Orb contact checks, and Drone orbit targeting |

The current active AttackOrigin should come from the spawned tower level model presentation when available.

If the current tower model does not provide AttackOrigin, the owning tower visual/runtime layer must log a warning and use AttackOriginFallback. Missing model AttackOrigin is a configuration error, not a normal runtime behavior.

Tower Runtime Combat consumes the resolved current active AttackOrigin. It should not inspect tower model hierarchy or choose fallback references directly.

Tower Runtime Combat should request attack presentation through the tower-owned visual/model presentation path. It should not search the spawned tower model hierarchy for presentation components directly.

---

# 5. Runtime Combat State

Tower Runtime Combat may maintain the following runtime state per tower:

- Detected enemies
- Current target
- Pending projectile target
- Pending projectile target position, usually captured from the target monster hit/reference anchor
- Cooldown timer
- Current attack state

This state should never be stored in TowerDefinition or AttackConfig.

Recommended attack states:

| State | Meaning |
|---|---|
| Idle | Tower is not currently executing an attack |
| WaitingForAnimationRelease | Tower has prepared an attack and is waiting for the animation release moment |

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
| Magic Orb | Magic Orb release update |
| Drone | Drone release update |

## 6.1 Cooldown Timing

Cooldown timing is based on successful Attack Entity release for every first-version tower attack archetype.

First-version tower attacks start cooldown when their Attack Entity is successfully released:

- Direction Projectile cooldown starts when the projectile is released.
- Arc Projectile cooldown starts when the projectile is launched.
- Tracking Projectile cooldown starts when the projectile is released.
- Magic Orb cooldown starts when the Magic Orb is generated.
- Drone cooldown starts when the Drone is launched.

After release, Projectile, Magic Orb, and Drone Attack Entities own their own lifecycle. Tower Runtime Combat should not wait for projectile impact, Magic Orb hit-count depletion, Drone battery depletion, or Drone destruction before starting the next attack interval.

Drone uses resolved attack interval for tower-side release cadence. Drone battery timing remains Drone-local lifetime behavior, while Drone projectile fire timing is controlled by resolved Drone burst stats plus static Drone burst configuration.

---

# 7. Enemy Detection

Tower Runtime Combat detects valid enemies by querying alive monsters from Monster System and filtering them by tower attack range.

A valid target should be:

- Not null
- Active in the scene
- Alive
- Inside attackRange

Range should be measured from the current active AttackOrigin resolved by the tower runtime.

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
Request attack animation presentation if configured
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

Projectile damage should be resolved before dispatch using the source tower's current TowerLevelConfig.basicDamage and resolved runtime damage bonus.

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

Projectile prefab roots should follow the project-wide orientation convention of local +Y Up and local +Z Forward. Tower Runtime Combat creates and initializes the projectile, while ProjectileBehaviour owns aligning the projectile root's local +Z axis to its movement direction.

---

# 10. Magic Orb Runtime

Magic Orb behavior represents a released orbiting Attack Entity created by Magic Tower.

Recommended Magic Orb flow:

```text
Cooldown ready
    ↓
Spawn Magic Orb
    ↓
Capture release-time orbit center from current active AttackOrigin
    ↓
Start cooldown
    ↓
Orbit around release-time center
    ↓
Contact detection against monsters
    ↓
Apply damage on successful contact
    ↓
Decrease hit count
    ↓
If hit count reaches zero or maximum lifetime is reached, despawn Magic Orb
```

Magic Orb rules:

- Magic Orb rotates around its release-time orbit center.
- Magic Orb checks distance to monsters.
- Contact deals damage.
- Magic Orb has a configurable maximum hit count.
- Hit count decreases after each successful hit.
- The same monster cannot be hit again by the same Magic Orb until sameTargetHitCooldown has elapsed.
- When hit count reaches zero, the Magic Orb disappears.
- When maximum lifetime is reached, the Magic Orb disappears even if remaining hit count is greater than zero.
- Cooldown starts when the Magic Orb is generated.
- After cooldown, a new Magic Orb may be generated without checking older released Magic Orbs.
- Magic Orb should start from a runtime-selected orbit angle so repeated releases do not all begin from the same point.

Magic Orb damage is owned by attack entity behavior in the first version.

Magic Orb contact damage should be resolved from the source tower's current TowerLevelConfig.basicDamage and resolved runtime damage bonus.

Magic Orb combat parameters such as orbit radius, contact distance, same-target hit cooldown, and maximum lifetime belong to AttackConfig because they define shared attack rules.

MagicOrbBehaviour executes orbit movement, contact detection, hit count consumption, and lifetime using AttackConfig data.

Persistent status effects applied by future Magic Orb upgrades should be delegated to Buff And Effect System.

---

# 11. Drone Runtime

Drone behavior represents a released autonomous Attack Entity launched by Drone Tower.

Recommended Drone flow:

```text
Cooldown ready
    ↓
Select target inside tower AttackRange
    ↓
If no valid target exists, remain Idle and do not start cooldown
    ↓
Request attack animation presentation if configured
    ↓
Release Drone from current active AttackOrigin
    ↓
Start cooldown
    ↓
Drone rises vertically to droneFlightHeight above release position
    ↓
Approach selected target
    ↓
Orbit selected target at droneOrbitRadius
    ↓
Fire projectile bursts
    ↓
Consume battery while active
    ↓
When battery is depleted, explode in the air and despawn
```

Drone runtime rules:

- Drone Tower runtime should release a Drone prefab from the current active AttackOrigin when a Drone attack is confirmed.
- If no valid monster exists inside AttackRange at release time, Drone Tower should not release a Drone and should not start cooldown.
- Drone launches from AttackOrigin when available, but does not keep depending on AttackOrigin after release.
- Drone first rises vertically from its release position to AttackConfig.droneFlightHeight above that position.
- Drone maintains configured flight height during active flight.
- DroneBehaviour owns target selection while using AttackConfig.targetSelectionType.
- Drone target selection only considers valid monsters inside the source tower AttackRange.
- Drone movement speed should use AttackConfig.droneFlightSpeed.
- Drone orbits around the selected target using AttackConfig.droneOrbitRadius.
- Drone orbit angular speed should be derived from droneFlightSpeed and droneOrbitRadius rather than configured separately.
- When entering Orbiting or retargeting, Drone should choose orbit direction from the tangent direction around the target that is closer to the Drone's current local +Z forward direction.
- Drone orbit direction is runtime state and should not be configured on AttackConfig.
- Whenever the Drone is moving, runtime should rotate the Drone root Transform so local +Z faces the current planar movement direction.
- Runtime should not apply model-specific rotation offsets; imported model orientation should be corrected inside the Drone prefab's VisualRoot.
- Rotation toward movement direction should happen immediately in the first version, without turn-speed smoothing.
- During stable Orbiting, Drone model local +Z should face the current orbit tangent / flight direction rather than the target center.
- Drone fires straight projectile bursts in the first version.
- Drone burst fire should use AttackConfig.droneBurstCount, droneBurstInterval, and droneBurstCooldown.
- Drone-fired projectile data should come from AttackConfig.droneProjectileConfig.
- Drone-fired projectile damage should be resolved from the source tower's current TowerLevelConfig.basicDamage and resolved runtime damage bonus.
- Drone-fired projectiles should spawn from the Drone FireAnchor when available.
- Drone-fired projectile prefabs should follow the same local +Y Up and local +Z Forward root orientation convention as other Projectile System prefabs.
- Drone-fired attack release VFX should spawn from the Drone FireAnchor when configured and face the selected target Monster direction.
- Drone flight consumes battery.
- If the Drone's current target becomes invalid after launch, Drone should retarget to another valid monster inside the source tower AttackRange when possible.
- If no valid monster remains inside AttackRange after launch, Drone should explode in the air and despawn.
- When battery is depleted, Drone should explode in the air and despawn.
- Drone tower cooldown starts when the Drone is launched, not when the Drone is destroyed.
- If attackInterval is shorter than Drone lifetime, multiple released Drones may exist at the same time.
- Drone is an Attack Entity which may spawn Projectile Attack Entities.
- Drone runtime may own lightweight Drone-local presentation such as propeller visual spinning.
- Propeller visual spinning should be active while Drone is launched and moving.
- During active flight, Drone model facing assumes local +Z points along the current planar movement direction.
- Drone-fired projectile launch direction and attack release VFX should still aim from FireAnchor to the selected monster hit position, independent of Drone body movement-facing.

Drone uses attackRange as the tower detect and launch range in the first version.

Drone projectile movement and projectile hit detection belong to Projectile System after projectile creation.

Persistent status effects applied by future Drone projectiles or Drone battery-end effects should be delegated to Buff And Effect System.

For Drone Tower, AttackOrigin acts as the Drone release point only. Released Drones should not depend on tower model child Transforms after launch.

FireAnchor is owned by the Drone prefab or DroneBehaviour because it moves with the Drone.

Neither AttackOrigin nor FireAnchor should own gameplay decisions such as target selection, battery rules, projectile hit detection, or damage.

---

# 12. Animation Integration

Tower Runtime Combat may request attack presentation from the current tower model presentation entry.

The current tower model presentation entry owns model-local animator references and attack presentation parameter names. It applies the actual presentation parameter changes on the spawned tower model.

Recommended fields:

| Field | Runtime Usage |
|---|---|
| attackAnimatorTriggerName | Requested when a released attack starts |

Recommended default values:

| Field | Value |
|---|---|
| attackAnimatorTriggerName | Attack |

Attack presentation parameter names should come from the current tower model presentation entry.

Tower Runtime Combat should not hardcode tower-specific animation parameter names.

Tower Runtime Combat should not cache or search for spawned tower model presentation components. Model-local presentation lookup and parameter application belong to the current tower model presentation entry resolved through TowerVisualController.

---

# 13. Runtime Presentation Hooks

Tower Runtime Combat may expose attack lifecycle hooks for VFX and presentation systems.

AttackConfig.attackReleaseVfxPrefab may be spawned as a one-shot presentation effect when a tower attack or Attack Entity release is confirmed.

For Direction Projectile attacks, attackReleaseVfxPrefab should face the projectile launch direction. Drone-fired projectile release VFX follows the same direction-projectile rule and should face the selected target Monster direction from the Drone FireAnchor. Other attack archetypes should spawn attackReleaseVfxPrefab with the prefab's default direction unless that archetype later defines its own orientation rule.

Recommended hooks:

| Hook | Trigger Timing |
|---|---|
| OnProjectileReleased | After projectile release is confirmed |
| OnAttackEntitySpawned | When an Attack Entity is created or launched |
| OnAttackEntityEnded | When an Attack Entity despawns or is destroyed |
| OnMagicOrbHit | When Magic Orb contact damage is applied |
| OnDroneStateChanged | When Drone launch, orbit, burst, battery-end, or destruction state changes |

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

The first-version damage formula is:

```text
FinalDamage = TowerLevelConfig.basicDamage + RuntimeDamageBonus
```

Tower level data owns the basic damage value. Tower upgrade runtime state may add instance-specific damage bonuses. Other runtime stats are resolved from immutable base configuration plus same-type additive upgrade deltas, then clamped before combat uses them.

AttackConfig does not define a runtime damage multiplier. Final damage comes from resolved runtime combat stats.

Example first-version stat direction:

```text
FinalAttackRange = BaseAttackRange + Sum(AttackRangeDeltas)
FinalAttackInterval = Clamp(BaseAttackInterval + Sum(AttackIntervalDeltas))
```

AttackInterval improvements may use negative deltas.

Behaviour upgrades are active packages recorded on the tower instance. Tower Runtime Combat may coordinate those packages, but the actual behaviour should remain inside the corresponding runtime module instead of moving into TowerUpgradeSystem.

Buff And Effect System should own reusable effect and buff execution.

Examples:

- AreaDamageEffect triggered by a projectile impact
- Delayed or repeated area damage triggered by an effect-backed Behaviour package
- Buff application effects
- Enemy-attached states such as poison, slow, burn, weaken, or armor reduction
- Elemental debuff stack application from direct elemental tower attacks
- Elemental normal phase and overload effects

Tower Runtime Combat should delegate future complex effects instead of embedding buff-specific logic into tower combat code.

Runtime Combat and Attack Entity behavior may provide trigger context that includes source tower, source upgrade, target monster, trigger position, impact position, resolved damage, and stack eligibility when relevant.

The first-version damage direction remains that base attack damage can use the existing direct damage path. Buff And Effect System may run additional Effect, Buff, Zone, and Elemental results around that path without forcing an immediate DamageContext migration.

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
- Magic Orb release orchestration
- Drone release orchestration
- Resolved runtime stat consumption
- Active behaviour package coordination
- Attack presentation request timing
- Runtime presentation hooks

Excluded:

- Projectile movement implementation
- Projectile hit detection implementation
- Projectile impact VFX
- Projectile travel VFX
- Buff lifetime implementation
- Tower upgrade application rules
- Concrete tower upgrade behaviour package implementation
- Object pooling
- Final VFX prefab authoring and particle polish
- Particle collision driven combat logic

---

# 18. Summary

Tower Runtime Combat is the live execution layer for placed towers.

It consumes TowerDefinition and AttackConfig, manages runtime combat state, selects targets, runs cooldowns, executes attack archetypes, releases Attack Entities, creates projectiles, and coordinates simple damage dispatch.

It should remain between Tower Framework data and downstream runtime systems without taking over placement, projectile lifecycle, monster lifecycle, or buff state ownership.
