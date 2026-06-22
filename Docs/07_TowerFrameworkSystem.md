# Tower Nexus - Tower Framework System

---

# 1. System Overview

The Tower Framework System is responsible for defining the shared tower architecture, static tower configuration data, and tower-related design concepts used by runtime gameplay systems.

This system defines what a tower is.

It does not define:

- Tower placement workflow
- Tower combat execution
- Projectile runtime behavior
- Buff execution
- Draft generation
- Tower upgrade application

The Tower Framework System provides shared tower framework data for:

- Draft System
- Tower Placement System
- Tower Runtime Combat System
- Tower Upgrade System
- Battle HUD UI System

---

# 2. Responsibility Boundary

The Tower Framework System owns:

- TowerDefinition structure
- Tower categories
- Attack archetype definitions
- Target selection definitions
- Shared tower configuration references
- Static attack VFX configuration references

The Tower Framework System does not own:

- Runtime tower placement
- Runtime tower attack logic
- Runtime projectile movement
- Runtime buff execution
- Runtime draft generation
- Runtime tower upgrades
- Runtime VFX spawning, binding, playback, or cleanup
- VFX prefab authoring, material setup, shader setup, or particle tuning

---

# 3. Core Design Philosophy

## 3.1 Tower Framework Defines Shared Concepts

The Tower Framework should define:

- What a tower is
- What configuration it uses
- What systems it references

TowerDefinition should not contain runtime combat state.

---

## 3.2 Runtime Systems Consume Tower Framework Data

Multiple gameplay systems may reference the same TowerDefinition.

Example:

```text
DraftSystem
    ↓
TowerDefinition
    ↓
BattleHUDUI
    ↓
TowerPlacementSystem
    ↓
TowerRuntimeCombatSystem
```

The Tower Framework acts as the common source of truth for tower-related systems.

---

# 4. Tower Definition

A TowerDefinition represents the basic data required by gameplay systems.

Recommended fields:

| Field | Type | Description |
|---|---|---|
| towerId | string | Unique tower identifier |
| displayName | string | Tower display name |
| description | string | Tower description |
| icon | Sprite | UI icon |
| towerPrefab | GameObject | Runtime tower prefab |
| towerCategory | TowerCategory | Tower category |
| attackConfig | AttackConfig | Attack configuration reference |
| towerLevelConfigs | List<TowerLevelConfig> | Optional per-level base stat and presentation data consumed by Tower Upgrade System |

Tower level base stat growth should be configured in TowerDefinition through per-level config data.

Upgrade definition references are owned by the Tower Upgrade System and should not be mixed with basic TowerDefinition attack configuration unless a later implementation explicitly requires a shared lookup.

Suggested TowerLevelConfig fields include:

| Field | Type | Description |
|---|---|---|
| level | int | Tower level represented by this config entry |
| baseDamageModifier | float | Optional base damage adjustment for this level |
| attackIntervalModifier | float | Optional attack interval adjustment for this level |
| rangeModifier | float | Optional attack range adjustment for this level |
| towerModelPrefab | GameObject | Optional visual/model replacement for this level |
| displayIcon | Sprite | Optional UI icon for this level |

---

# 5. Tower Structure

The Tower Structure defines the runtime prefab organization and placement-related anchor data used by gameplay systems.

A tower structure provides:

- Runtime visual representation
- Placement footprint definition
- Placement anchor references
- Runtime combat component attachment points
- Future expansion points for tower systems

The Tower Structure is shared by:

- Tower Placement System
- Tower Runtime Combat System
- Tower Upgrade System

---

## 5.1 Tower Prefab Structure

Recommended prefab structure:

```text
TowerPrefab
├── VisualRoot
├── Collider
├── TowerAnchorSet
│   ├── CenterAnchor
│   ├── OccupyAnchor_01
│   ├── OccupyAnchor_02
│   └── OccupyAnchor_03
└── AttackOrigin
```

Future versions may add additional runtime combat components.

---

## 5.2 TowerAnchorSet

TowerAnchorSet defines the placement footprint of a tower.

Tower Placement System uses TowerAnchorSet to:

- Snap towers onto GridNodes
- Calculate occupied GridNodes
- Validate placement legality
- Update walkability state

Different towers may use different footprint layouts.

---

## 5.3 Center Anchor

Each tower contains one Center Anchor.

The Center Anchor represents:

- Placement reference point
- Grid snap point
- Footprint origin

During placement, the Center Anchor aligns with the target GridNode.

---

## 5.4 Occupied Anchors

Occupied Anchors define the GridNodes occupied by the tower.

Requirements:

- Anchor Y position should normally remain 0
- Anchor X/Z offsets should use integer grid offsets
- Occupied Anchors must correspond to valid GridNodes
- Center Anchor may also be included in Occupied Anchors
- Tower rotation is not supported in the first version

Example:

```text
[X][X]
[ ][X]
```

Different towers may use different footprint shapes.

---

## 5.5 Footprint Usage

The placement footprint is calculated using:

```text
Target GridNode
    ↓
Center Anchor Snap
    ↓
Occupied Anchor Offsets
    ↓
GridNode Lookup
    ↓
Placement Validation
```

The Tower Framework System only defines footprint structure.

Runtime placement validation belongs to Tower Placement System.

---

# 6. Tower Categories

Current first-version tower categories:

| Category | Description |
|---|---|
| Archer | Fires fast straight-line arrows toward enemies |
| Cannon | Launches arcing explosive shells toward enemy positions |
| Magic | Maintains orbiting Magic Orb attack entities that contact enemies and consume hit count |
| Drone | Launches an autonomous Drone attack entity that fights away from the tower and may spawn projectile attack entities |

Future categories may include:

- Support Tower
- Trap Tower
- Summon Tower
- Resource Tower
- Watch Tower

---

# 7. Attack Entities and Archetypes

Attack archetypes define the fundamental attack behavior expressed by a tower.

Towers orchestrate combat while Attack Entities execute combat behavior.

Tower responsibilities:

- Detect monsters
- Select targets when required
- Manage cooldowns
- Decide attack timing
- Spawn or control Attack Entities

Attack Entity responsibilities:

- Movement
- Orbit or tracking behavior
- Hit detection
- Damage dispatch
- Lifetime

Tower decides when an attack happens.

Attack Entity decides how the attack behaves.

Current first-version Attack Entities:

| Archetype | Description |
|---|---|
| Arrow | Direction projectile attack entity fired by Archer Tower |
| Shell | Arc projectile attack entity fired by Cannon Tower |
| Magic Orb | Orbiting attack entity owned by Magic Tower |
| Drone | Autonomous attack entity launched by Drone Tower |

Projectile is a shared runtime concept supporting projectile-style Attack Entities.

Current first-version projectile flight behaviors:

- Direction
- Arc
- Tracking

Future Laser, Area, Trap, Boomerang, Missile, and Summon Towers should primarily be implemented by introducing new Attack Entity behavior rather than continuously expanding TowerCombatBehaviour.

Examples:

| Tower | Attack Entity | Core Behavior |
|---|---|---|
| Archer Tower | Arrow | Fires low-damage direction projectiles with short range and high attack speed |
| Cannon Tower | Shell | Fires slow arcing shells with long range; shells explode on arrival and deal area damage |
| Magic Tower | Magic Orb | Maintains orbiting magic weapon behavior with contact damage and hit-count lifetime |
| Drone Tower | Drone | Launches an autonomous drone that hovers near enemies, fires projectiles, consumes battery, returns, and recharges |

---

## 7.1 Archer Tower Attack Pattern

Archer Tower uses straight projectile attacks.

Design intent:

- Low damage per projectile
- Short attack range
- High attack speed
- The selected target's monster-side hit/reference anchor is used to determine the projectile launch direction
- After launch, the projectile travels independently and is not required to hit the originally selected target
- During flight, the projectile continuously checks whether any valid monster's hit/reference anchor is within its hit distance threshold
- If one or more valid monsters are within the hit distance threshold, the projectile hits the nearest valid monster and applies damage
- Attack cooldown starts immediately after the arrow is fired, not after impact
- If no valid monster is hit before the projectile reaches its maximum lifetime, the projectile is destroyed automatically


Archer Tower does not require buff or effect configuration in the first version.

VFX expectation:

- May play a projectile release VFX at AttackOrigin when the arrow is released
- May use optional projectile travel VFX on the projectile prefab, such as a trail or glow
- May play optional impact VFX at the hit position when the projectile hits a monster

These VFX are presentation-only and do not decide hit detection or damage.

---

## 7.2 Cannon Tower Attack Pattern

Cannon Tower uses arcing projectile attacks.

Design intent:

- Low attack speed
- Long attack range
- Projectile travels toward a selected enemy reference position
- Projectile explodes when it reaches the target position or impact point
- Explosion deals area damage to enemies within the explosion radius
- Attack cooldown starts immediately after the shell is launched, not after explosion

The cannon projectile itself should be handled by projectile runtime logic.


The explosion may be represented as an impact effect or area damage effect, but it should not be treated as a buff in the first version because it does not persist on enemies over time.

VFX expectation:

- May play a projectile release VFX at AttackOrigin when the shell is released
- May use optional projectile travel VFX on the projectile prefab, such as smoke, fire, sparks, or trail particles
- Should play impact or explosion VFX when the projectile reaches its target position or impact point

Explosion visuals should follow gameplay impact timing. Particle collision should not independently decide gameplay damage or projectile hit results.

---

## 7.3 Magic Tower Attack Pattern

Magic Tower uses an orbiting Magic Orb attack entity.

Design intent:

- The tower continuously owns one Magic Orb when the orb is active
- The Magic Orb rotates around the tower
- The Magic Orb checks distance to monsters while orbiting
- Contact deals damage
- The Magic Orb has a configurable maximum hit count
- Hit count decreases after each successful hit
- One Magic Orb may hit the same monster only once per orbit
- When hit count reaches zero, the Magic Orb disappears
- After cooldown, the tower generates a new Magic Orb


Magic Tower does not require buff configuration in the first version. Its damage is owned by Magic Orb attack entity behavior.

VFX expectation:

- May use visual references on the Magic Orb prefab for orbiting presentation
- The Magic Orb visual should follow the runtime orbit path
- Contact VFX may play when the Magic Orb successfully hits a monster
- The Magic Orb visual should stop or despawn when the Magic Orb lifetime ends

Magic Orb VFX is presentation-only and must not apply damage, search targets, or determine hit results.

---

## 7.4 Drone Tower Attack Pattern

Drone Tower uses an autonomous Drone attack entity.

Design intent:

- The Drone rests on the tower when inactive
- When monsters enter range, the Drone launches
- The Drone selects a target
- The Drone flies near the target and hovers while facing it
- If the target moves away, the Drone relocates to maintain hover distance
- The Drone periodically fires straight projectiles
- Drone flight consumes battery
- When battery is depleted or no monsters remain, the Drone returns to recharge
- Once recharged and monsters exist, the Drone launches again

Drone is an Attack Entity which may spawn Projectile Attack Entities.

Drone Tower does not require buff configuration in the first version. Projectile damage fired by Drone should follow projectile attack entity rules.

VFX expectation:

- The Drone visual should support resting, launching, hovering, returning, and recharging states
- Projectile travel and impact VFX should follow projectile runtime and impact timing
- Drone battery or recharge presentation should remain visual feedback only unless explicitly connected to gameplay state by runtime logic

Drone VFX is presentation-only and must not own target selection, battery rules, projectile hit detection, or damage.

# 8. Attack Configuration

Attack Configuration defines the static combat-related data consumed by the Tower Runtime Combat System.

TowerDefinition should not directly store combat parameters.

Instead, TowerDefinition references an AttackConfig through attackConfig.

Example:

```text
TowerDefinition
    ↓
attackConfig
    ↓
AttackConfig
    ↓
TowerRuntimeCombatSystem
```

This separation allows multiple towers to share the same attack configuration while keeping runtime combat logic independent from tower framework data.

---

## 8.1 AttackConfig Purpose

AttackConfig defines:

- Attack range
- Attack interval
- Damage values
- Target selection rules
- Projectile references
- Projectile trajectory parameters
- Attack Entity behavior parameters
- Animator parameter names for attack presentation
- Optional attack VFX prefab references

AttackConfig does not contain runtime state.

Runtime state belongs to Tower Runtime Combat System.

Examples of runtime state:

- Current target
- Cooldown timer
- Attack Entity lifetime state
- Detected enemies
- Attack execution state

---

## 8.2 Recommended AttackConfig Fields

Recommended first-version fields:

| Field | Type | Description |
|---|---|---|
| attackConfigId | string | Unique attack configuration identifier |
| attackArchetype | AttackArchetype | Attack behavior type |
| attackRange | float | Maximum attack range |
| attackInterval | float | Time between attacks |
| targetSelectionType | TargetSelectionType | Target selection rule |
| damage | int | Base damage value |
| projectileConfig | ProjectileConfig | Direct projectile configuration reference |
| arcHeight | float | Arc projectile trajectory height |
| magicOrbRotationSpeed | float | Rotation speed for Magic Orb behavior |
| magicOrbMaxHitCount | int | Maximum number of successful hits before a Magic Orb disappears |
| droneBatteryDuration | float | Maximum active flight duration before Drone must return |
| droneRechargeDuration | float | Recharge time before Drone may launch again |
| droneHoverDistance | float | Preferred hover distance from the selected target |
| attackAnimatorTriggerName | string | Animator Trigger parameter used by projectile-based attacks |
| attackingAnimatorBoolName | string | Animator Bool parameter used by continuous attack archetypes |
| projectileReleaseVfxPrefab | GameObject | Optional one-shot VFX prefab spawned at AttackOrigin when a projectile attack is released |
| magicOrbPrefab | GameObject | Optional Magic Orb attack entity prefab |
| dronePrefab | GameObject | Optional Drone attack entity prefab |

Not every attack archetype requires every field.

Unused fields should be hidden in the Inspector whenever practical.

Editor tooling may use Odin Inspector conditional display features to show only fields relevant to the selected AttackArchetype.

Attack VFX fields are optional. Empty VFX references should not block combat execution.

Attack VFX references are static presentation configuration only. They must not define gameplay damage, targeting rules, cooldown logic, projectile hit detection, or buff behavior.

```md
Animator parameter names should be configured in AttackConfig instead of hardcoded in Tower Runtime Combat.

Recommended naming convention:

| Field | Recommended Value |
|---|---|
| attackAnimatorTriggerName | Attack |
| attackingAnimatorBoolName | IsAttacking |

Most towers should follow the same naming convention to simplify animator setup and runtime combat implementation.

However, animator parameter names remain configurable through AttackConfig.
```

---

## 8.3 AttackConfig Usage By Archetype

Different attack archetypes consume different AttackConfig fields.

### Direction Projectile

Typically uses:

- attackRange
- attackInterval
- damage
- projectileConfig
- targetSelectionType
- attackAnimatorTriggerName
- projectileReleaseVfxPrefab

Notes:

- targetSelectionType is used by the tower to select an initial target before firing.
- The selected target's monster-side hit/reference anchor provides the launch direction for the projectile.
- After launch, direction projectile hit detection belongs to the Projectile System.
- The projectile may hit any valid monster encountered during flight, not only the originally selected target.
- Attack cooldown starts immediately after the projectile is fired.
- The projectile should be destroyed by projectile runtime logic when it exceeds its maximum lifetime.

VFX notes:

- projectileReleaseVfxPrefab may be played at AttackOrigin when the projectile is released.
- Projectile travel VFX should usually live on the projectile prefab or ProjectileConfig.
- Projectile impact VFX should usually be handled by projectile impact logic.

---

### Arc Projectile

Typically uses:

- attackRange
- attackInterval
- damage
- projectileConfig
- arcHeight
- targetSelectionType
- attackAnimatorTriggerName
- projectileReleaseVfxPrefab

Notes:

- The selected target's monster-side hit/reference anchor provides the target position snapshot.
- Explosion radius and area damage behavior belong to the Projectile System and Buff And Effect System, not AttackConfig.
- Attack cooldown starts immediately after the projectile is launched.

VFX notes:

- projectileReleaseVfxPrefab may be played at AttackOrigin when the projectile is released.
- Projectile travel VFX should usually live on the projectile prefab or ProjectileConfig.
- Explosion or impact VFX should follow projectile impact timing.

---

### Magic Orb

Typically uses:

- attackRange
- attackInterval
- damage
- magicOrbRotationSpeed
- magicOrbMaxHitCount
- attackingAnimatorBoolName
- magicOrbPrefab

Notes:

- magicOrbRotationSpeed controls how quickly the Magic Orb rotates around the tower.
- magicOrbMaxHitCount controls how many successful hits the active Magic Orb can perform before disappearing.
- The Magic Orb may hit the same monster only once per orbit.
- attackInterval controls the cooldown before a new Magic Orb is generated after the active orb ends.
- Magic Orb behavior does not require targetSelectionType in the first version.

VFX notes:

- magicOrbPrefab may contain visual references for orbiting and contact feedback.
- Magic Orb VFX should not own damage, target search, orbit hit rules, or hit validation.

---

### Drone

Typically uses:

- attackRange
- attackInterval
- damage
- projectileConfig
- targetSelectionType
- droneBatteryDuration
- droneRechargeDuration
- droneHoverDistance
- attackingAnimatorBoolName
- dronePrefab

Notes:

- targetSelectionType is used when the Drone chooses a target.
- droneBatteryDuration controls how long the Drone can remain active away from the tower.
- droneRechargeDuration controls how long the Drone must recharge after returning.
- droneHoverDistance controls the preferred distance between the Drone and its current target.
- Drone is an Attack Entity which may spawn Projectile Attack Entities.
- Projectiles fired by Drone should use ProjectileConfig and Projectile System behavior.
- Drone battery, return, and recharge behavior should belong to Drone attack entity runtime logic.

attackInterval controls how often the Drone fires while it is in a valid hover attack state.

VFX notes:

- dronePrefab may contain visual references for launch, hover, return, and recharge presentation.
- Drone VFX should not own target selection, battery rules, projectile hit detection, or damage.

---

## 8.4 Runtime Consumption

The Tower Framework System only defines AttackConfig data.

Runtime attack execution belongs to the Tower Runtime Combat System.

Example:

```text
TowerDefinition
    ↓
AttackConfig
    ↓
TowerRuntimeCombatSystem
    ↓
Attack Execution
```

The Tower Runtime Combat System is responsible for consuming AttackConfig data and converting it into runtime combat behavior.

---

# 9. Target Selection Types

Target selection determines how towers choose enemies.

Recommended first-version types:

| Type          | Description |
|---------------|---|
| Nearest       | Closest enemy |
| HighestHealth | Enemy with highest HP |
| LowestHealth  | Enemy with lowest HP |
| Random        | Random enemy within range |

Future expansion:

- FirstInPath
- LastInPath
- HighestThreat

Target selection may not be required by every attack archetype.

Archetype usage:

| AttackArchetype | Uses TargetSelectionType |
|---|---|
| Direction Projectile | Yes |
| Arc Projectile | Yes |
| Magic Orb | No |
| Drone | Yes |

Examples:

| Tower | Target Selection Usage |
|---|---|
| Archer | Selects one target before firing |
| Cannon | Selects one target or target position before firing |
| Magic | Does not need single-target selection for first-version orbiting Magic Orb behavior |
| Drone | Selects one target for Drone hover and projectile fire behavior |

---

# 10. Effect and Buff Relationship

Tower attacks may reference effect or buff systems, but only when the tower behavior actually requires them.

First-version recommendation:

| Tower | Effect/Buff Usage |
|---|---|
| Archer | No buff/effect required; projectile collision applies direct damage |
| Cannon | Projectile impact may trigger an AreaDamageEffect; no buff required |
| Magic | No buff required; Magic Orb contact damage is owned by attack entity behavior |
| Drone | No buff required; Drone-fired projectile damage follows projectile attack entity rules |

Guideline:

- Use Projectile runtime logic for projectile movement and collision.
- Use Effect logic for instant gameplay events such as explosion damage or area damage calculation.
- Use Buff logic only when a gameplay state is attached to a unit over time, such as poison, slow, burn, weaken, or armor reduction.

Cannon Tower explosion should be treated as an instant area damage effect rather than a buff.

---

# 11. Related Systems

## Draft System

Uses TowerDefinition to generate Tower Draft choices.

---

## Tower Placement System

Uses TowerDefinition to instantiate tower prefabs.

---

## Tower Runtime Combat System

Uses TowerDefinition and AttackConfig to determine attack archetypes and combat behavior.

---

## Tower Upgrade System

Uses TowerDefinition per-level config data to process tower level-up requests.

Uses Tower Upgrade System-owned upgrade definitions to define and apply tower upgrades.

---

## Buff and Effect System

May be referenced by tower upgrade content and runtime combat systems to apply effect or buff-related gameplay behavior.

The first version should keep direct tower damage, projectile behavior, instant area damage effects, and persistent buffs clearly separated.

---

# 12. First Version Scope

Included:

- TowerDefinition structure
- Tower categories
- Attack Entity concepts and archetypes
- Target selection types
- Runtime system references
- Basic relationship between tower attacks, effects, and buffs
- Optional AttackConfig VFX and Attack Entity prefab references for projectile release, Magic Orb, and Drone attacks

Excluded:

- Runtime combat execution
- Projectile implementation
- Buff implementation
- Upgrade application logic
- Draft generation rules
- Runtime VFX spawning, binding, playback, and cleanup
- Final VFX prefab authoring and particle polish
- Particle collision driven combat logic

---



# Change Log

## 2026-06-22

- Updated first-version tower categories to Archer, Cannon, Magic, and Drone.
- Removed Watch Tower from the current first-version tower lineup.
- Redesigned Magic Tower documentation from ChannelBeam to orbiting Magic Orb attack entity behavior.
- Added Drone Tower as an autonomous Attack Entity that may spawn Projectile Attack Entities.
- Added Attack Entity responsibility boundaries and projectile flight behavior direction: Direction, Arc, and Tracking.
- Updated AttackConfig field guidance and target selection usage for Magic Orb and Drone behavior.

## 2026-06-18

- Added TowerDefinition-owned per-level config direction for tower level base stat growth and visuals.
- Updated Draft System relationship wording from New Tower Draft to Tower Draft.
- Clarified that TowerUpgradeSystem consumes TowerDefinition level config for tower level-up requests while owning upgrade definitions separately.

## 2026-06-12

- Clarified that monster-side hit/reference positions are provided by the Monster System and consumed by attack patterns, projectile target snapshots, projectile hit checks, and ChannelBeam VFX binding.

## 2026-06-10

- Added AttackConfig VFX reference fields for projectile release, ChannelBeam, and PeriodicArea attacks.
- Clarified that AttackConfig VFX references are optional presentation data and must not own gameplay logic.
- Added VFX expectations for Archer, Cannon, Magic, and Watch Tower attack patterns.
- Clarified that projectile travel and impact VFX should follow projectile runtime and impact timing.
- Clarified that ChannelBeam VFX should be runtime-controlled between AttackOrigin and the current target.
- Clarified that PeriodicArea VFX should be authored at a scale that visually matches the tower's configured attackRange in the first version.
- Clarified that particle collision should not drive combat hit detection or damage in the first version.


## 2026-06-08

- Clarified StraightProjectile behavior as direction-based independent projectile flight rather than guaranteed target locking or target-position snapshot impact.
- Updated Archer Tower attack pattern to specify that the selected target only determines launch direction.
- Clarified that StraightProjectile may hit any valid monster encountered during flight, not only the originally selected target.
- Clarified that StraightProjectile hit detection belongs to the Projectile System and should use hit distance threshold plus maximum lifetime handling.


## 2026-05-30

- Updated AttackConfig damage type from float to int to align with MonsterBehaviour.TakeDamage(int).
- Removed damagePerSecond from first-version AttackConfig documentation and clarified that ChannelBeam uses damage with channelDamageInterval.
- Replaced projectileConfigId with direct ProjectileConfig reference.
- Added AttackConfig animator parameter name fields for attack Trigger and continuous attacking Bool presentation.
- Updated first-version tower categories to Archer Tower, Cannon Tower, Magic Tower, and Watch Tower.
- Replaced generic Projectile/Beam archetype list with StraightProjectile, ArcProjectile, ChannelBeam, and PeriodicArea.
- Added detailed attack pattern descriptions for Archer Tower, Cannon Tower, Magic Tower, and Watch Tower.
- Added Effect and Buff Relationship section.
- Clarified that Cannon Tower explosion may be represented as an instant area damage effect, not a buff.
- Clarified that Watch Tower periodic area damage is owned by tower runtime combat logic rather than enemy-attached damage-over-time buffs.
- Standardized TowerCategory values to Archer, Cannon, Magic, and Watch.
- Removed upgradeConfigId from first-version TowerDefinition recommended fields and clarified that upgrade configuration references belong to future Tower Upgrade System work.
- Clarified that unused AttackConfig fields should be hidden in the Inspector whenever practical.
- Removed explosionRadius and areaTickInterval from AttackConfig.
- Added arcHeight for ArcProjectile configuration.
- Added channelDamageInterval for ChannelBeam damage timing.
- Clarified which AttackConfig fields are consumed by each AttackArchetype.
- Clarified that PeriodicArea uses attackInterval and does not use TargetSelectionType.

## 2026-05-29

- Created Tower Framework System.
- Renamed the document from Tower Definition System to Tower Framework System to better reflect ownership of tower architecture, structure, categories, attack archetypes, and upgrade concepts.
- Extracted TowerDefinition ownership from Tower Placement System.
- Added tower categories.
- Added attack archetype definitions.
- Added target selection definitions.
- Added Tower Structure ownership including TowerPrefab, TowerAnchorSet, Center Anchor, Occupied Anchors, and footprint definitions.
- Moved tower upgrade layer ownership to Tower Upgrade System.
