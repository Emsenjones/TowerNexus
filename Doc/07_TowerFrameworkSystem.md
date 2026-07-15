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
- Tower visual VFX anchor contract
- Tower model presentation contract
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

TowerUpgradeDefinition content should stay outside TowerDefinition and AttackConfig. TowerDefinition owns tower identity, base prefab/config references, and per-level base stat and presentation data. AttackConfig owns immutable default attack configuration. Tower upgrade runtime state tracks per-instance upgrade state, damage bonuses, stat deltas, and behaviour package activation.

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
├── TowerSpawnRefreshVfxAnchor
├── TowerUpgradeAppliedVfxAnchor
├── PreviewRenderer
└── AttackOriginFallback
```

Future versions may add additional runtime combat components.

VisualRoot owns every tower-local visual object.

TowerBaseVisualRoot contains the permanent tower base or platform visual. Tower level changes should not replace TowerBaseVisualRoot.

TowerPrefabSpawnPoint is the spawn parent for the current tower level model. Tower level changes replace only the spawned tower model.

PreviewRenderer is a compatibility placeholder for preview-related authoring or transforms. It should not decide placement validity, and it should not be the main visual feedback for Tower Placement Preview.

AttackRangePreview is an optional tower-local visual child used by TowerVisualController during Draft item drag operations. It should contain a circular mesh whose radius is 1 when local scale is 1. TowerVisualController scales it uniformly to the tower's configured attackRange and toggles it on or off. AttackRangePreview may include lightweight prefab-authored looping presentation while enabled.

TowerSpawnRefreshVfxAnchor is an optional tower-local anchor for deploy success and tower level-up model refresh VFX.

TowerUpgradeAppliedVfxAnchor is an optional tower-local anchor for TowerUpgradeDefinition application success VFX.

Tower Placement Preview should treat TowerBaseVisualRoot and the spawned tower model as one ghost visual. Valid or invalid placement feedback should be applied through whole-preview material tint and alpha.

AttackOriginFallback is a runtime safety fallback. Each Tower Level Model Prefab is expected to provide its own correctly positioned AttackOrigin. If the current tower model does not provide AttackOrigin, runtime must log a warning and then use AttackOriginFallback.

Missing model AttackOrigin is an authoring or configuration error, not a normal runtime path.

TowerBehaviour owns TowerVisualController.

TowerVisualController owns tower-local visual rendering operations requested by gameplay systems, including model spawn or replacement, current tower model presentation resolution, preview material tint, preview transparency, attack range preview visibility, valid-target highlight presentation, tower-side success feedback playback, and current AttackOrigin resolution.

TowerPlacementSystem may request preview or range visual changes, but should not directly manipulate VisualRoot, TowerPrefabSpawnPoint, renderer materials, or tower model instances.

TowerUpgradeSystem may request visual refresh after an accepted tower level-up, but should not directly manipulate tower model hierarchy.

TowerRuntimeCombatSystem consumes the current active AttackOrigin and tower model presentation entry resolved by the owning tower runtime. It should not resolve level model hierarchy, model presentation hierarchy, or fallback references itself.

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

For Drone Tower, the current active AttackOrigin from the spawned tower level model represents the Drone release point.

Released Drones should capture the release position they need at launch and should not depend on tower model child Transforms after launch. Drone Tower does not need a separate DroneParkingAnchor in the first version.

The Drone prefab itself may define its own internal combat anchors:

```text
DronePrefab
├── VisualRoot
├── DroneBehaviour
├── FireAnchor
└── PropellerVisual
```

FireAnchor represents where Drone-fired projectile Attack Entities and optional attack release VFX should spawn.

FireAnchor belongs to the Drone prefab or DroneBehaviour because it moves with the Drone.

Drone prefab presentation convention:

- Drone model forward should use local +Z for active flight facing.
- Runtime should rotate the Drone root Transform so local +Z faces the current planar movement direction whenever the Drone is moving.
- Imported model orientation differences should be corrected inside the Drone prefab's VisualRoot, not by runtime offset compensation.
- Drone prefab may include a local propeller visual Transform.
- Runtime may spin the propeller visual while Drone is launched and moving.

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

Each Tower Level Model Prefab should provide its own AttackOrigin and may provide a tower model presentation entry for model-local presentation hooks.

Recommended structure:

```text
TowerLevelModelPrefab
├── ModelPresentation
├── VisualRoot
└── AttackOrigin
```

The model presentation entry should be reachable from the Tower Level Model Prefab root.

The model presentation entry owns model-local attack presentation hooks for the current tower model.

The model presentation entry should expose the model-local AttackOrigin reference when available. This lets the tower runtime consume the current model's presentation contract instead of searching model hierarchy by name.

Rules:

- A Tower Draft deployment result should carry or resolve the tower level it represents.
- Deploying a new tower spawns the tower model for the deployment result's resolved tower level.
- Current first-version Tower Draft configuration may resolve all newly deployed towers to Lv1, but the framework should not hardcode new deployment as Lv1-only.
- Upgrading a tower replaces only the spawned tower model.
- TowerBaseVisualRoot remains unchanged across tower levels.
- Runtime resolves the active AttackOrigin from the current tower model presentation when available.
- If the current tower model lacks AttackOrigin, runtime logs a warning and uses AttackOriginFallback.
- Runtime resolves the active model presentation entry from the current tower model when available.
- Missing model presentation should not block combat execution; attacks that require animation release may fall back to immediate release when no presentation entry is available.

Tower model presentation responsibilities:

- Expose model-local runtime anchors such as AttackOrigin.
- Receive attack presentation requests from runtime combat.
- Apply model-local attack presentation parameters.
- Forward attack animation release signals to the owning runtime combat component.

Tower model presentation does not decide attack timing, target selection, cooldowns, damage, projectile creation, or tower upgrade behavior.

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
- Resolve current tower model presentation entry.
- Apply preview material state to preview instances using per-renderer material instances.
- Clear preview transparency from deployed instances when required.
- Manage AttackRangePreview visibility and scale.
- Show attack range preview.
- Hide attack range preview.
- Update attack range preview.
- Present valid-target highlight feedback when a gameplay system requests it.
- Present tower-side success feedback after deploy, level-up model refresh, or upgrade application succeeds.
- Use tower-local VFX anchors for tower-related one-shot VFX when available.

TowerVisualController does not decide:

- Placement validity.
- Upgrade validity.
- Tower level.
- Tower upgrade logic.
- Whether deploy, level-up, or upgrade application succeeded.
- Attack range values.
- Attack timing.
- Animator parameter selection.
- Draft item consumption.

Gameplay systems decide whether a visual should be shown and which data should be rendered. TowerVisualController renders the requested tower-local visual state.

---

# 6. Tower Categories

Current first-version tower categories:

| Category | Description |
|---|---|
| Archer | Fires fast straight-line arrows toward enemies |
| Cannon | Launches arcing explosive shells toward enemy positions |
| Magic | Releases orbiting Magic Orb attack entities that contact enemies and consume hit count |
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

Attack Entity is a design and runtime concept. It does not require every implementation stage to have a dedicated code class with that name.

Current code may still use an attack archetype enum to select the first-version execution branch. The long-term conceptual split is:

```text
Attack Entity kind
    -> Projectile
    -> Magic Orb
    -> Drone

Projectile flight behavior
    -> Direction
    -> Arc
    -> Tracking
```

Projectile-style Attack Entities share Projectile System runtime behavior, while Magic Orb and Drone use their own Attack Entity runtime behavior.

Future refactors may separate Attack Entity kind from projectile flight behavior in code, but this is not required before Effect System foundation work.

Tower responsibilities:

- Detect monsters
- Select targets when required
- Manage cooldowns
- Decide attack timing
- Release Attack Entities

Attack Entity responsibilities:

- Movement
- Orbit or tracking behavior
- Hit detection
- Damage dispatch
- Lifetime
- Trigger context emission when a hit, contact, or impact should execute reusable Buff And Effect rules

Tower decides when an attack happens.

Attack Entity decides how the attack behaves.

Current first-version Attack Entities:

| Archetype | Description |
|---|---|
| Arrow | Direction projectile attack entity fired by Archer Tower |
| Shell | Arc projectile attack entity fired by Cannon Tower |
| Magic Orb | Orbiting attack entity released by Magic Tower |
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
| Magic Tower | Magic Orb | Releases orbiting magic weapon behavior with contact damage and hit-count lifetime |
| Drone Tower | Drone | Releases an autonomous drone that orbits selected target monsters, fires projectile bursts, consumes battery, air-explodes, and despawns |

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

- The tower releases Magic Orb attack entities on its attack interval
- Each Magic Orb rotates around the release-time center captured from AttackOrigin
- The Magic Orb checks distance to monsters while orbiting
- Contact deals damage
- The Magic Orb has a configurable maximum hit count
- Hit count decreases after each successful hit
- The same monster cannot be hit again by the same Magic Orb until sameTargetHitCooldown has elapsed
- When hit count reaches zero, the Magic Orb disappears
- When maximum lifetime is reached, the Magic Orb disappears even if remaining hit count is greater than zero
- Cooldown starts when the Magic Orb is generated
- After cooldown, the tower may generate a new Magic Orb without checking older released Magic Orbs
- Magic Orb should spawn at a runtime-selected orbit angle
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

- When Drone Tower attacks, it releases a Drone prefab from AttackOrigin
- Attack cooldown starts when the Drone is successfully launched
- If no valid monster exists inside AttackRange at release time, Drone Tower should not launch a Drone and should not start cooldown
- The Drone rises vertically from its release position to configured flight height
- The Drone selects a target inside the source tower AttackRange
- The Drone orbits around the selected target monster
- The Drone model local +Z faces the current planar movement direction while moving; during stable orbit this is the orbit tangent
- The Drone fires straight projectile bursts while orbiting
- Drone flight consumes battery
- If the Drone's current target becomes invalid after launch, the Drone should retarget to another valid monster inside the source tower AttackRange when possible
- If no valid monster remains inside AttackRange after launch, the Drone should end its task by exploding in the air and disappearing
- When battery is depleted, the Drone explodes in the air and disappears
- If attackInterval is shorter than Drone lifetime, multiple released Drones may exist at the same time

Drone is an Attack Entity which may spawn Projectile Attack Entities.

Drone Tower does not require buff configuration in the first version. Projectile damage fired by Drone should follow projectile attack entity rules.

Anchor expectation:

- Drone Tower should use AttackOrigin as the Drone release point in the first version.
- Released Drones should not depend on tower model child Transforms after launch.
- Drone prefab should define its own FireAnchor for Drone-fired projectiles.
- Drone-fired attack release VFX should play from the Drone FireAnchor when configured and face the selected target Monster direction.
- AttackOrigin and FireAnchor are runtime/prefab references; they should not decide target selection, battery rules, projectile hit detection, or damage.

VFX expectation:

- The Drone visual should support launching, orbiting, firing, battery-end explosion, and despawn states
- Drone active flight presentation may include a lightweight local propeller spin
- Propeller spin should be active while the Drone is launched and moving
- Drone model active facing assumes local +Z points forward along the current planar movement direction.
- Runtime should rotate the Drone root Transform immediately toward movement direction, without model-specific rotation offsets or first-version smoothing.
- During stable Orbiting, Drone model local +Z should face the current orbit tangent rather than the target center.
- Projectile travel and impact VFX should follow projectile runtime and impact timing
- Attack release VFX may be reused for Drone-fired projectiles, but should spawn from the Drone FireAnchor rather than the tower AttackOrigin
- Drone battery and battery-end explosion presentation should remain visual feedback only unless explicitly connected to gameplay state by runtime logic

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
- Target selection rules
- Projectile references
- Projectile trajectory parameters
- Attack Entity behavior parameters
- Animator parameter names for attack presentation requests
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

AttackConfig assets are referenced directly by TowerDefinition. They should not maintain a hand-authored attack config id unless a future persistence, external-data, or lookup requirement needs a stable id. Debug output should use the ScriptableObject asset name.

| Field | Type | Description |
|---|---|---|
| attackArchetype | AttackArchetype | Attack behavior type |
| attackRange | float | Maximum attack range |
| attackInterval | float | Time between successful Attack Entity releases |
| targetSelectionType | TargetSelectionType | Target selection rule |
| projectileConfig | ProjectileConfig | Direct projectile configuration reference |
| arcHeight | float | Arc projectile trajectory height |
| magicOrbRotationSpeed | float | Rotation speed for Magic Orb behavior |
| magicOrbMaxHitCount | int | Maximum number of successful hits before a Magic Orb disappears |
| magicOrbMaxLifetime | float | Maximum lifetime before a released Magic Orb disappears, even if remaining hit count is greater than zero |
| sameTargetHitCooldown | float | Cooldown before the same Magic Orb may hit the same monster again |
| magicOrbOrbitRadius | float | Orbit radius used by Magic Orb movement |
| magicOrbContactDistance | float | Contact distance used by Magic Orb hit detection |
| droneBatteryDuration | float | Maximum active flight duration before Drone air-explodes and despawns |
| droneOrbitRadius | float | Orbit radius around the selected target monster |
| droneFlightSpeed | float | Drone movement speed for launch, approach, and orbit |
| droneFlightHeight | float | Height offset above the Drone release position maintained during active Drone flight |
| droneBurstCount | int | Number of projectiles fired in one Drone burst |
| droneBurstInterval | float | Time between projectiles within one Drone burst |
| droneBurstCooldown | float | Cooldown between Drone bursts |
| droneProjectileConfig | ProjectileConfig | Projectile configuration used by Drone-fired projectiles |
| attackReleaseVfxPrefab | GameObject | Optional one-shot VFX prefab spawned when a tower attack or attack entity release is confirmed |
| magicOrbPrefab | GameObject | Optional Magic Orb attack entity prefab |
| dronePrefab | GameObject | Optional Drone attack entity prefab |

AttackConfig should not define a damage multiplier. Final runtime damage is resolved from TowerLevelConfig.basicDamage plus tower upgrade runtime damage bonuses.

Not every attack archetype requires every field.

Unused fields should be hidden in the Inspector whenever practical.

Editor tooling may use Odin Inspector conditional display features to show only fields relevant to the selected AttackArchetype.

Attack VFX fields are optional. Empty VFX references should not block combat execution.

Attack VFX references are static presentation configuration only. They must not define gameplay damage, targeting rules, cooldown logic, projectile hit detection, or buff behavior.

Attack animation parameter names belong to the current tower model presentation entry. Tower Runtime Combat requests attack presentation from the resolved model presentation entry instead of hardcoding animator parameter names.

Recommended naming convention:

| Field | Recommended Value |
|---|---|
| attackAnimatorTriggerName | Attack |

Most tower model presentation entries should follow the same naming convention to simplify animator setup and runtime combat implementation.

However, animator parameter names remain configurable per tower model presentation entry.

---

## 8.3 AttackConfig Usage By Archetype

Different attack archetypes consume different AttackConfig fields.

### Direction Projectile

Typically uses:

- attackRange
- attackInterval
- projectileConfig
- targetSelectionType
- attackReleaseVfxPrefab

Notes:

- targetSelectionType is used by the tower to select an initial target before firing.
- The selected target's monster-side hit/reference anchor provides the launch direction for the projectile.
- After launch, direction projectile hit detection belongs to the Projectile System.
- The projectile may hit any valid monster encountered during flight, not only the originally selected target.
- Final damage is calculated from the source tower's current TowerLevelConfig.basicDamage and resolved runtime damage bonus.
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
- projectileConfig
- arcHeight
- targetSelectionType
- attackReleaseVfxPrefab

Notes:

- The selected target's monster-side hit/reference anchor provides the target position snapshot.
- Final damage is calculated from the source tower's current TowerLevelConfig.basicDamage and resolved runtime damage bonus.
- Explosion radius and area damage behavior belong to the Projectile System and Effect System, not AttackConfig.
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
- projectileConfig
- targetSelectionType
- attackReleaseVfxPrefab

Notes:

- Tracking Projectile follows projectile-style cooldown timing.
- Final damage is calculated from the source tower's current TowerLevelConfig.basicDamage and resolved runtime damage bonus.
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
- magicOrbRotationSpeed
- magicOrbOrbitRadius
- magicOrbContactDistance
- magicOrbMaxHitCount
- magicOrbMaxLifetime
- sameTargetHitCooldown
- attackReleaseVfxPrefab
- magicOrbPrefab

Notes:

- magicOrbRotationSpeed controls how quickly the Magic Orb rotates around its release-time center.
- magicOrbOrbitRadius controls the Magic Orb attack path around its release-time center.
- magicOrbContactDistance controls Magic Orb contact hit detection.
- magicOrbMaxHitCount controls how many successful hits the active Magic Orb can perform before disappearing.
- magicOrbMaxLifetime controls how long a released Magic Orb can remain active before disappearing, even if remaining hit count is greater than zero.
- sameTargetHitCooldown controls how soon the same Magic Orb may hit the same monster again.
- attackInterval controls the cooldown before the tower may release another Magic Orb.
- Magic Orb cooldown starts when the Magic Orb is generated.
- Older Magic Orbs do not block later Magic Orb releases.
- Magic Orb should spawn at a runtime-selected orbit angle.
- Final damage is calculated from the source tower's current TowerLevelConfig.basicDamage and resolved runtime damage bonus.
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
- attackInterval
- targetSelectionType
- droneBatteryDuration
- droneOrbitRadius
- droneFlightSpeed
- droneFlightHeight
- droneBurstCount
- droneBurstInterval
- droneBurstCooldown
- droneProjectileConfig
- attackReleaseVfxPrefab
- dronePrefab

Notes:

- targetSelectionType is used by DroneBehaviour when the Drone chooses a target.
- attackInterval controls how often Drone Tower may release a new Drone after a successful launch.
- droneBatteryDuration controls how long the released Drone can remain active before air-exploding and despawning.
- droneOrbitRadius controls the Drone's orbit distance around the selected target monster.
- droneFlightSpeed controls Drone launch, approach, and orbit movement.
- droneFlightHeight controls the height offset above the Drone release position maintained during active Drone flight.
- Drone orbit direction should be chosen at runtime when entering Orbiting or retargeting, based on which tangent direction is closer to the Drone's current local +Z forward direction.
- Drone orbit direction should not be configured on AttackConfig.
- Whenever the Drone is moving, Drone model local +Z should face the current planar movement direction. During stable Orbiting this is the selected orbit tangent / flight direction.
- Drone-fired projectiles and attack release VFX should still aim at the selected monster hit position from the Drone FireAnchor, independent of Drone body movement-facing.
- droneBurstCount, droneBurstInterval, and droneBurstCooldown control Drone-specific burst fire timing.
- Drone uses attackRange as the tower detect and launch range in the first version. Dedicated Drone engage or leash ranges may be added later if needed.
- Drone target selection only considers valid monsters inside the source tower attackRange.
- If no valid monster is available before Drone release, Drone Tower should remain idle and should not start cooldown.
- If a released Drone loses its current target, it should retarget to another valid monster inside the source tower attackRange when possible.
- If no valid monster remains after release, the released Drone should air-explode and despawn.
- Drone is an Attack Entity which may spawn Projectile Attack Entities.
- Projectiles fired by Drone should use droneProjectileConfig and Projectile System behavior.
- Final Drone-fired projectile damage is calculated from the source tower's current TowerLevelConfig.basicDamage and resolved runtime damage bonus.
- Drone battery and battery-end destruction behavior should belong to Drone attack entity runtime logic.
- Drone Tower should use AttackOrigin as the Drone release point in the first version.
- Released Drones should not depend on tower model child Transforms after launch.
- Drone FireAnchor should come from the Drone prefab or DroneBehaviour, not AttackConfig.

Drone tower cooldown timing starts when the Drone is successfully launched. Drone projectile fire timing is controlled by droneBurstCount, droneBurstInterval, and droneBurstCooldown.
If attackInterval is shorter than Drone lifetime, multiple released Drones may exist at the same time.

VFX notes:

- dronePrefab may contain visual references for launch, orbit, projectile firing, battery-end explosion, and despawn presentation.
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

Uses TowerDefinition, AttackConfig, and resolved tower upgrade state to determine attack archetypes, final runtime stats, and combat behavior.

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
