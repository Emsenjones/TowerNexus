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
- Tower prefab structure contract
- Tower visual structure contract
- TowerVisualController ownership direction

The Tower Framework System does not own:

- Runtime tower placement
- Runtime tower attack logic
- Runtime projectile movement
- Runtime buff execution
- Runtime draft generation
- Runtime tower upgrades
- Runtime VFX spawning, binding, playback, or cleanup
- VFX prefab authoring, material setup, shader setup, or particle tuning
- Placement preview validation
- Tower level-up validation

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
| towerFamily | TowerFamily | Tower upgrade compatibility family |
| displayName | string | Tower display name |
| description | string | Tower description |
| icon | Sprite | UI icon |
| towerPrefab | GameObject | Runtime tower prefab |
| attackConfig | AttackConfig | Attack configuration reference |
| towerLevelConfigs | List<TowerLevelConfig> | Optional per-level base stat and presentation data consumed by Tower Upgrade System |

Tower level base stat growth should be configured in TowerDefinition through per-level config data.

Upgrade definition references are owned by the Tower Upgrade System and should not be mixed with basic TowerDefinition attack configuration unless a later implementation explicitly requires a shared lookup.

Suggested TowerLevelConfig fields include:

| Field | Type | Description |
|---|---|---|
| level | int | Tower level represented by this config entry |
| basicDamage | int | Basic damage value for this tower level |
| towerModelPrefab | GameObject | Optional visual/model replacement for this level |
| displayIcon | Sprite | Optional UI icon for this level |

---

# 5. Tower Structure

The Tower Structure defines the runtime prefab organization and placement-related anchor data used by gameplay systems.

A tower structure provides:

- Runtime visual representation
- Runtime visual ownership path
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
│   ├── TowerBaseVisualRoot
│   └── TowerPrefabSpawnPoint
├── Collider
├── TowerAnchorSet
│   ├── CenterAnchor
│   ├── OccupyAnchor_01
│   ├── OccupyAnchor_02
│   └── OccupyAnchor_03
├── AttackRangePreview
├── PreviewRenderer
└── AttackOriginFallback
```

Future versions may add additional runtime combat components.

VisualRoot owns every tower-local visual object.

TowerBaseVisualRoot contains the permanent tower base or platform visual. Tower level changes should not replace TowerBaseVisualRoot.

TowerPrefabSpawnPoint is the spawn parent for the current tower level model. Tower level changes replace only the spawned tower model.

PreviewRenderer is a compatibility placeholder for preview-related authoring or transforms. It should not decide placement validity, and it should not be the main visual feedback for Tower Placement Preview.

AttackRangePreview is an optional tower-local visual child used by TowerVisualController during Draft item drag operations. It should contain a circular mesh whose radius is 1 when local scale is 1. TowerVisualController scales it uniformly to the tower's configured attackRange and toggles it on or off.

Tower Placement Preview should treat TowerBaseVisualRoot and the spawned tower model as one ghost visual. Valid or invalid placement feedback should be applied through whole-preview material tint and alpha.

AttackOriginFallback is a runtime safety fallback. Each Tower Level Model Prefab is expected to provide its own correctly positioned AttackOrigin. If the current tower model does not provide AttackOrigin, runtime must log a warning and then use AttackOriginFallback.

Missing model AttackOrigin is an authoring or configuration error, not a normal runtime path.

TowerBehaviour owns TowerVisualController.

TowerVisualController owns tower-local visual rendering operations requested by gameplay systems, including model spawn or replacement, preview material tint, preview transparency, attack range preview visibility, and current AttackOrigin resolution.

TowerPlacementSystem may request preview or range visual changes, but should not directly manipulate VisualRoot, TowerPrefabSpawnPoint, renderer materials, or tower model instances.

TowerUpgradeSystem may request visual refresh after an accepted tower level-up, but should not directly manipulate tower model hierarchy.

TowerRuntimeCombatSystem consumes the current active AttackOrigin resolved by the owning tower runtime. It should not resolve level model hierarchy or fallback references itself.

Drone Tower consumes the current active AttackOrigin differently from projectile-only towers.

Drone Tower still follows the same TowerPrefab visual structure direction:

```text
DroneTowerPrefab
├── VisualRoot
│   ├── TowerBaseVisualRoot
│   └── TowerPrefabSpawnPoint
├── Collider
├── TowerAnchorSet
├── AttackRangePreview
├── PreviewRenderer
└── AttackOriginFallback
```

For Drone Tower, the current active AttackOrigin from the spawned tower level model represents where the Drone rests, launches from, returns to, and recharges when inactive.

This keeps Drone parking behavior aligned with the existing tower attack-origin convention and avoids adding a separate DroneParkingAnchor in the first version.

The Drone prefab itself may define its own internal combat anchors:

```text
DronePrefab
├── VisualRoot
├── Animator
├── DroneBehaviour
└── FireAnchor
```

FireAnchor represents where Drone-fired projectile Attack Entities and optional attack release VFX should spawn.

FireAnchor belongs to the Drone prefab or DroneBehaviour because it moves with the Drone.

Drone prefab presentation convention:

- Drone model forward should use local +Z for active flight facing.
- Runtime should rotate the Drone root Transform so local +Z faces the current planar movement direction whenever the Drone is moving.
- Imported model orientation differences should be corrected inside the Drone prefab's VisualRoot, not by runtime offset compensation.
- When resting or recharging at AttackOrigin, Drone should face local -Z relative to the AttackOrigin orientation in the first version.
- Drone prefab may include an Animator with Bool parameter `IsFlying`.
- Runtime should set `IsFlying = false` while Drone is Resting or Recharging.
- Runtime should set `IsFlying = true` while Drone is Launching, Orbiting, or Returning.

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

## 5.6 Tower Level Model Prefab

Tower level models come from TowerDefinition per-level config data.

Example:

```text
TowerDefinition
└── towerLevelConfigs
    ├── Lv1
    │   └── towerModelPrefab
    ├── Lv2
    │   └── towerModelPrefab
    └── Lv3
        └── towerModelPrefab
```

Each Tower Level Model Prefab should provide its own AttackOrigin.

Recommended structure:

```text
TowerLevelModelPrefab
├── Model
└── AttackOrigin
```

Rules:

- A Tower Draft deployment result should carry or resolve the tower level it represents.
- Deploying a new tower spawns the tower model for the deployment result's resolved tower level.
- Current first-version Tower Draft configuration may resolve all newly deployed towers to Lv1, but the framework should not hardcode new deployment as Lv1-only.
- Upgrading a tower replaces only the spawned tower model.
- TowerBaseVisualRoot remains unchanged across tower levels.
- Runtime resolves the active AttackOrigin from the current tower model.
- If the current tower model lacks AttackOrigin, runtime logs a warning and uses AttackOriginFallback.

---

## 5.7 TowerVisualController

TowerVisualController is a reusable runtime component owned by TowerBehaviour.

Its purpose is to perform tower-local visual rendering requested by gameplay systems.

Responsibilities:

- Manage TowerBaseVisualRoot reference.
- Manage TowerPrefabSpawnPoint reference.
- Spawn tower level model.
- Replace tower level model.
- Destroy previous tower level model.
- Resolve current active AttackOrigin.
- Apply preview material state to preview instances using per-renderer material instances.
- Clear preview transparency from deployed instances when required.
- Manage AttackRangePreview visibility and scale.
- Show attack range preview.
- Hide attack range preview.
- Update attack range preview.

Example API direction:

```text
SetTowerVisual()
GetCurrentAttackOrigin()
SetPreviewMaterialState(Color tint, float alpha)
ShowAttackRangePreview()
HideAttackRangePreview()
UpdateAttackRangePreview()
ClearCurrentTowerModel()
```

TowerVisualController does not decide:

- Placement validity.
- Upgrade validity.
- Tower level.
- Tower upgrade logic.
- Attack range values.
- Draft item consumption.

Gameplay systems decide whether a visual should be shown and which data should be rendered. TowerVisualController renders the requested tower-local visual state.

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
| Drone Tower | Drone | Launches an autonomous drone that orbits selected target monsters, fires projectile bursts, consumes battery, returns, and recharges |

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

- May play an attack release VFX at AttackOrigin when the arrow is released
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

- May play an attack release VFX at AttackOrigin when the shell is released
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
- The same monster cannot be hit again by the same Magic Orb until sameTargetHitCooldown has elapsed
- When hit count reaches zero, the Magic Orb disappears
- Cooldown starts after the active Magic Orb ends
- After cooldown, the tower generates a new Magic Orb
- May play an attack release VFX at AttackOrigin when the Magic Orb is generated


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

- When Drone Tower is deployed, the Drone prefab should be spawned immediately and rest on the tower while inactive
- When monsters enter tower AttackRange, the Drone launches
- The Drone rises vertically from AttackOrigin to configured flight height
- The Drone selects a target inside the source tower AttackRange
- The Drone orbits around the selected target monster
- The Drone model local +Z faces the current planar movement direction while moving; during stable orbit this is the orbit tangent
- The Drone fires straight projectile bursts while orbiting
- Drone flight consumes battery
- When battery is depleted or no valid monsters remain inside source tower AttackRange, the Drone first returns to the point above AttackOrigin at configured flight height, then descends vertically to AttackOrigin for recharge
- Drone cooldown or recharge timing starts after the Drone returns to the tower, not when it launches
- Once recharged and monsters exist, the Drone launches again

Drone is an Attack Entity which may spawn Projectile Attack Entities.

Drone Tower does not require buff configuration in the first version. Projectile damage fired by Drone should follow projectile attack entity rules.

Anchor expectation:

- Drone Tower should use AttackOrigin as the Drone rest position, launch point, return target, and recharge position in the first version.
- Drone Tower runtime should instantiate and initialize the Drone at AttackOrigin when the tower is deployed, not only when the first monster enters range.
- Drone prefab should define its own FireAnchor for Drone-fired projectiles.
- Drone-fired attack release VFX should play from the Drone FireAnchor when configured and face the selected target Monster direction.
- AttackOrigin and FireAnchor are runtime/prefab references; they should not decide target selection, battery rules, projectile hit detection, or damage.

VFX expectation:

- The Drone visual should support resting, launching, orbiting, returning, and recharging states
- Drone active flight animation may be driven by an Animator Bool parameter named `IsFlying`
- `IsFlying` should be false for resting and recharging states, and true for launching, orbiting, and returning states
- Drone model active facing assumes local +Z points forward along the current planar movement direction.
- Runtime should rotate the Drone root Transform immediately toward movement direction, without model-specific rotation offsets or first-version smoothing.
- During stable Orbiting, Drone model local +Z should face the current orbit tangent rather than the target center.
- Drone resting and recharging pose should face local -Z relative to AttackOrigin in the first version
- Projectile travel and impact VFX should follow projectile runtime and impact timing
- Attack release VFX may be reused for Drone-fired projectiles, but should spawn from the Drone FireAnchor rather than the tower AttackOrigin
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
- Attack behavior damage multiplier
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
| attackInterval | float | Time between projectile-style attacks, or Magic Orb respawn cooldown after an active orb ends |
| targetSelectionType | TargetSelectionType | Target selection rule |
| damageMultiplier | float | Default attack-behavior damage multiplier applied to TowerLevelConfig.basicDamage |
| projectileConfig | ProjectileConfig | Direct projectile configuration reference |
| arcHeight | float | Arc projectile trajectory height |
| magicOrbRotationSpeed | float | Rotation speed for Magic Orb behavior |
| magicOrbMaxHitCount | int | Maximum number of successful hits before a Magic Orb disappears |
| sameTargetHitCooldown | float | Cooldown before the same Magic Orb may hit the same monster again |
| magicOrbOrbitRadius | float | Orbit radius used by Magic Orb movement |
| magicOrbContactDistance | float | Contact distance used by Magic Orb hit detection |
| droneBatteryDuration | float | Maximum active flight duration before Drone must return |
| droneRechargeDuration | float | Recharge time before Drone may launch again |
| droneOrbitRadius | float | Orbit radius around the selected target monster |
| droneFlightSpeed | float | Drone movement speed for launch, approach, orbit, and return |
| droneFlightHeight | float | Height offset above AttackOrigin maintained during active Drone flight |
| droneBurstCount | int | Number of projectiles fired in one Drone burst |
| droneBurstInterval | float | Time between projectiles within one Drone burst |
| droneBurstCooldown | float | Cooldown between Drone bursts |
| droneProjectileConfig | ProjectileConfig | Projectile configuration used by Drone-fired projectiles |
| attackAnimatorTriggerName | string | Animator Trigger parameter used by projectile-based attacks |
| attackingAnimatorBoolName | string | Animator Bool parameter used by continuous attack archetypes |
| attackReleaseVfxPrefab | GameObject | Optional one-shot VFX prefab spawned when a tower attack or attack entity release is confirmed |
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
- damageMultiplier
- projectileConfig
- targetSelectionType
- attackAnimatorTriggerName
- attackReleaseVfxPrefab

Notes:

- targetSelectionType is used by the tower to select an initial target before firing.
- The selected target's monster-side hit/reference anchor provides the launch direction for the projectile.
- After launch, direction projectile hit detection belongs to the Projectile System.
- The projectile may hit any valid monster encountered during flight, not only the originally selected target.
- Final damage is calculated from the source tower's current TowerLevelConfig.basicDamage and the active projectile damage multiplier.
- Attack cooldown starts immediately after the projectile is released.
- The projectile should be destroyed by projectile runtime logic when it exceeds its maximum lifetime.

VFX notes:

- attackReleaseVfxPrefab may be played at AttackOrigin when the projectile is released and should face the projectile launch direction.
- Projectile travel VFX should usually live on the projectile prefab or ProjectileConfig.
- Projectile impact VFX should usually be handled by projectile impact logic.

---

### Arc Projectile

Typically uses:

- attackRange
- attackInterval
- damageMultiplier
- projectileConfig
- arcHeight
- targetSelectionType
- attackAnimatorTriggerName
- attackReleaseVfxPrefab

Notes:

- The selected target's monster-side hit/reference anchor provides the target position snapshot.
- Final damage is calculated from the source tower's current TowerLevelConfig.basicDamage and the active projectile damage multiplier.
- Explosion radius and area damage behavior belong to the Projectile System and Buff And Effect System, not AttackConfig.
- Attack cooldown starts immediately after the projectile is launched.

VFX notes:

- attackReleaseVfxPrefab may be played at AttackOrigin when the projectile is released and should use the VFX prefab's default direction.
- Projectile travel VFX should usually live on the projectile prefab or ProjectileConfig.
- Explosion or impact VFX should follow projectile impact timing.

---

### Tracking Projectile

Typically uses:

- attackRange
- attackInterval
- damageMultiplier
- projectileConfig
- targetSelectionType
- attackAnimatorTriggerName
- attackReleaseVfxPrefab

Notes:

- Tracking Projectile follows projectile-style cooldown timing.
- Final damage is calculated from the source tower's current TowerLevelConfig.basicDamage and the active tracking projectile damage multiplier.
- Attack cooldown starts immediately after the projectile is released.
- Tracking projectile runtime is reserved for later projectile-style attack implementations.

VFX notes:

- attackReleaseVfxPrefab may be played when the projectile is released. Until Tracking Projectile defines its own orientation rule, it should use the VFX prefab's default direction.
- Projectile travel and impact VFX should follow Projectile System timing.

---

### Magic Orb

Typically uses:

- attackRange
- attackInterval
- damageMultiplier
- magicOrbRotationSpeed
- magicOrbOrbitRadius
- magicOrbContactDistance
- magicOrbMaxHitCount
- sameTargetHitCooldown
- attackingAnimatorBoolName
- attackReleaseVfxPrefab
- magicOrbPrefab

Notes:

- magicOrbRotationSpeed controls how quickly the Magic Orb rotates around the tower.
- magicOrbOrbitRadius controls the Magic Orb attack path around the tower.
- magicOrbContactDistance controls Magic Orb contact hit detection.
- magicOrbMaxHitCount controls how many successful hits the active Magic Orb can perform before disappearing.
- sameTargetHitCooldown controls how soon the same Magic Orb may hit the same monster again.
- attackInterval controls the cooldown before a new Magic Orb is generated after the active orb ends.
- The active Magic Orb's lifetime is part of the attack process, so cooldown does not start when the orb spawns.
- Final damage is calculated from the source tower's current TowerLevelConfig.basicDamage and the active Magic Orb damage multiplier.
- Magic Orb behavior does not require targetSelectionType in the first version.
- Magic Orb combat parameters should be configured on AttackConfig so MagicOrbBehaviour remains a runtime executor.
- attackReleaseVfxPrefab may be played at AttackOrigin when the Magic Orb is generated and should use the VFX prefab's default direction.

VFX notes:

- magicOrbPrefab may contain visual references for orbiting and contact feedback.
- Magic Orb VFX should not own damage, target search, orbit hit rules, or hit validation.

---

### Drone

Typically uses:

- attackRange
- damageMultiplier
- targetSelectionType
- droneBatteryDuration
- droneRechargeDuration
- droneOrbitRadius
- droneFlightSpeed
- droneFlightHeight
- droneBurstCount
- droneBurstInterval
- droneBurstCooldown
- droneProjectileConfig
- attackingAnimatorBoolName
- attackReleaseVfxPrefab
- dronePrefab

Notes:

- targetSelectionType is used by DroneBehaviour when the Drone chooses a target.
- droneBatteryDuration controls how long the Drone can remain active away from the tower.
- droneRechargeDuration controls how long the Drone must recharge after returning.
- droneOrbitRadius controls the Drone's orbit distance around the selected target monster.
- droneFlightSpeed controls Drone launch, approach, orbit, and return movement.
- droneFlightHeight controls the height offset above AttackOrigin maintained during active Drone flight.
- Drone orbit direction should be chosen at runtime when entering Orbiting or retargeting, based on which tangent direction is closer to the Drone's current local +Z forward direction.
- Drone orbit direction should not be configured on AttackConfig.
- Whenever the Drone is moving, Drone model local +Z should face the current planar movement direction. During stable Orbiting this is the selected orbit tangent / flight direction.
- Drone-fired projectiles and attack release VFX should still aim at the selected monster hit position from the Drone FireAnchor, independent of Drone body movement-facing.
- droneBurstCount, droneBurstInterval, and droneBurstCooldown control Drone-specific burst fire timing.
- Drone uses attackRange as the tower detect and launch range in the first version. Dedicated Drone engage or leash ranges may be added later if needed.
- Drone target selection only considers valid monsters inside the source tower attackRange.
- Drone is an Attack Entity which may spawn Projectile Attack Entities.
- Projectiles fired by Drone should use droneProjectileConfig and Projectile System behavior.
- Final Drone-fired projectile damage is calculated from the source tower's current TowerLevelConfig.basicDamage and the active Drone projectile damage multiplier.
- Drone battery, return, and recharge behavior should belong to Drone attack entity runtime logic.
- Drone Tower should use AttackOrigin as the Drone rest, launch, return, and recharge anchor in the first version.
- Drone FireAnchor should come from the Drone prefab or DroneBehaviour, not AttackConfig.

Drone does not use attackInterval. Drone attack cycle timing is controlled by droneBatteryDuration and droneRechargeDuration, while Drone projectile fire timing is controlled by droneBurstCount, droneBurstInterval, and droneBurstCooldown.
Drone recharge or tower cooldown timing starts after the Drone returns to the tower, not when it launches.

VFX notes:

- dronePrefab may contain visual references for launch, orbit, return, and recharge presentation.
- attackReleaseVfxPrefab may be reused for Drone-fired attack release VFX, should spawn at the Drone FireAnchor, and should face the selected target Monster direction.
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
| Drone | Selects one target for Drone orbit and projectile burst fire behavior |

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

Requests TowerBehaviour-owned TowerVisualController to update placement preview, tower level-up preview, preview transparency, and attack range preview presentation.

---

## Tower Runtime Combat System

Uses TowerDefinition and AttackConfig to determine attack archetypes and combat behavior.

Consumes the current active AttackOrigin resolved through the owning tower runtime and TowerVisualController path.

---

## Tower Upgrade System

Uses TowerDefinition per-level config data to process tower level-up requests.

Uses Tower Upgrade System-owned upgrade definitions to define and apply tower upgrades.

After accepting a tower level-up request, requests the owning tower runtime to replace the spawned tower model and refresh the active AttackOrigin.

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
- Tower prefab visual structure contract
- TowerVisualController ownership and API direction
- Optional AttackConfig VFX and Attack Entity prefab references for attack release, Magic Orb, and Drone attacks

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

## 2026-06-25 (Tower Visual Foundation Sync)

- Updated TowerPrefab visual structure direction with TowerBaseVisualRoot, TowerPrefabSpawnPoint, PreviewRenderer, and AttackOriginFallback.
- Added Tower Level Model Prefab contract requiring each level model to provide its own AttackOrigin.
- Added TowerVisualController as a TowerBehaviour-owned runtime component responsible for tower-local visual rendering.
- Clarified that TowerPlacementSystem and TowerUpgradeSystem request visual changes instead of directly manipulating tower visual hierarchy.
- Clarified that TowerRuntimeCombatSystem consumes the current active AttackOrigin resolved by the owning tower runtime.
- Clarified that Tower Placement Preview feedback should use whole-preview tint and alpha, with PreviewRenderer remaining only as a compatibility placeholder.

## 2026-06-24

- Updated damage framework ownership: TowerLevelConfig owns per-level basicDamage, while AttackConfig owns attack-behavior damageMultiplier.
- Removed undecided attack interval and range modifiers from TowerLevelConfig guidance.
- Updated Drone design from hover-distance firing to target orbit and Drone-specific projectile burst fire.
- Clarified Drone orbit direction as runtime-derived from current forward direction, not AttackConfig data.
- Clarified Drone active orbit facing: model local +Z follows orbit tangent, while Drone-fired projectiles still aim at the selected monster hit position.
- Clarified Drone deployment and return presentation: Drone spawns at AttackOrigin when Drone Tower is deployed, launches by rising vertically, and returns by moving above AttackOrigin before descending.
- Clarified Drone movement-facing presentation: runtime rotates the Drone root local +Z toward planar movement direction whenever it moves, without model-specific offsets or first-version smoothing.
- Clarified that Drone does not use attackInterval; battery/recharge and burst timing own Drone combat timing.
- Clarified attackReleaseVfxPrefab orientation: Direction Projectile and Drone-fired projectile release VFX face launch direction, while other archetypes use prefab default direction.

## 2026-06-23

- Clarified cooldown timing for projectile-style attacks, Magic Orb, and Drone.
- Added Tracking Projectile first-version cooldown guidance.

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
- Standardized TowerFamily values to Archer, Cannon, Magic, and Drone.
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
