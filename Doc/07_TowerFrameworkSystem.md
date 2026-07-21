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
- Tower Base Prefab combat-component contract
- Common and archetype-specific combat authoring fields
- Static attack VFX configuration references on combat components
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
| towerLevelConfigs | List<TowerLevelConfig> | Optional per-level base stat and presentation data consumed by Tower Upgrade System |

Tower level base stat growth should be configured in TowerDefinition through per-level config data.

Upgrade definition references are owned by the Tower Upgrade System and should not be mixed with TowerDefinition identity or per-level presentation data.

TowerDefinition owns tower identity, the Tower Base Prefab reference, and per-level base stat and presentation data. The Tower Base Prefab's concrete TowerCombatBehaviour component owns immutable base combat authoring. TowerUpgradeDefinition owns package-specific authoring and upgrade runtime prefab references, while tower upgrade runtime state tracks per-instance upgrades, damage bonuses, stat deltas, and behaviour package activation.

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
| Cannon | Launches arcing shells toward captured enemy positions and resolves a nearby direct target on arrival |
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

Tower combat authoring and orchestration use one concrete TowerCombatBehaviour subtype per first-version attack archetype:

```text
TowerCombatBehaviour
    -> DirectionProjectileCombatBehaviour
    -> ArcProjectileCombatBehaviour
    -> MagicOrbCombatBehaviour
    -> DroneCombatBehaviour
```

The concrete component type is the Tower Base Prefab's attack-runtime identity. `AttackArchetype` remains useful for Projectile flight identity such as Direction, Arc, and Tracking, but Tower authoring does not require a separate serialized archetype selector that can disagree with the component type.

Projectile-style Attack Entities share Projectile System runtime behavior, while Magic Orb and Drone use their own Attack Entity runtime behavior.

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
| Cannon Tower | Shell | Fires slow arcing shells with long range toward captured positions; the baseline arrival resolves at most one nearby direct target |
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
- Before attack presentation begins, the selected enemy reference position is captured as an immutable target-position snapshot
- Projectile travels toward that captured position and does not require the original Monster to remain valid after release
- Reaching the captured position always produces Position Impact
- On arrival, projectile runtime searches for the nearest valid Monster around the impact position within ProjectileConfig.hitDistanceThreshold
- When a Monster is found, the landing also produces Monster Hit and resolves direct damage against that Monster
- When no Monster is found, there is no Monster-targeted direct result, but impact feedback and projectile cleanup still occur
- Area explosion is not part of the baseline Cannon attack; Explosive Shell Behaviour upgrade content adds that result
- Attack cooldown starts immediately after the shell is launched, not after impact

The cannon projectile itself should be handled by projectile runtime logic.

Position Impact and Monster Hit are independent facts. One Cannon landing may produce both, while a miss near the captured position may produce Position Impact only. Documents should preserve that semantic distinction without requiring a particular serialized Effect field layout.

When Explosive Shell is active, its explosion is an instant Position Impact Effect rather than a Buff. It may execute even when the baseline landing does not resolve a direct Monster Hit.

VFX expectation:

- May play an attack release VFX at AttackOrigin when the shell is released
- May use optional projectile travel VFX on the projectile prefab, such as smoke, fire, sparks, or trail particles
- Should play impact VFX when the projectile reaches its captured position
- Explosive Shell may add explosion VFX at the same gameplay impact timing

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
- The Magic Arcane Field TowerUpgradeDefinition provides one VFX prefab whose root contains MagicArcaneFieldBehaviour
- The Arcane Field prefab is instantiated as a child of the owning tower only while the Arcane Field package is active
- The Arcane Field VFX prefab uses local X/Z scale `1` as authored radius `1`; runtime sets local X/Z scale to the applied Arcane Field radius while preserving local Y scale
- Nested particle, mesh, material, and shader content remains presentation-only; MagicArcaneFieldBehaviour owns field timing and gameplay execution

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
- Launching does not consume battery; battery consumption begins after the Drone enters active combat flight
- If the Drone's current target becomes invalid after launch, the Drone should retarget to another valid monster inside the source tower AttackRange when possible
- If no valid monster remains inside AttackRange after launch, the Drone should end its task by exploding in the air and disappearing
- When battery is depleted without Final Dive, the Drone plays aerial explosion feedback and disappears
- Final Dive Behaviour content may replace battery-end despawn with a target-locked dive. Its Position Impact performs one local nearest-valid-Monster direct query before the additive impact explosion
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

- The Drone visual should support launching, orbiting, firing, optional Final Dive, battery-end explosion, and despawn states
- Drone active flight presentation may include a lightweight local propeller spin
- Propeller spin should be active while the Drone is launched and moving
- Drone model active facing assumes local +Z points forward along the current planar movement direction.
- Runtime should rotate the Drone root Transform immediately toward movement direction, without model-specific rotation offsets or first-version smoothing.
- During stable Orbiting, Drone model local +Z should face the current orbit tangent rather than the target center.
- Projectile travel and impact VFX should follow projectile runtime and impact timing
- Attack release VFX may be reused for Drone-fired projectiles, but should spawn from the Drone FireAnchor rather than the tower AttackOrigin
- Drone battery and battery-end explosion presentation should remain visual feedback only unless explicitly connected to gameplay state by runtime logic

Drone VFX is presentation-only and must not own target selection, battery rules, projectile hit detection, or damage.

# 8. Tower Combat Component Authoring

Static tower combat data is configured once on the concrete TowerCombatBehaviour component attached to each Tower Base Prefab. TowerDefinition references the prefab; it does not reference a separate AttackConfig asset.

```text
TowerDefinition.towerPrefab
    -> concrete TowerCombatBehaviour component
    -> prefab-authored base combat values and Attack Entity references
    -> Tower Runtime Combat execution
```

This ownership keeps one-off combat configuration beside the runtime that consumes it and prevents unrelated archetype fields from sharing one authoring asset.

## 8.1 Common TowerCombatBehaviour Fields

The abstract TowerCombatBehaviour base owns fields shared by tower attack orchestration:

| Field | Type | Description |
|---|---|---|
| attackRange | float | Base target-detection and launch range before runtime upgrade deltas |
| attackInterval | float | Base time between successful Attack Entity releases before runtime upgrade deltas |
| targetSelectionType | TargetSelectionType | Normal tower/Drone target-selection rule when used by the concrete archetype |
| attackReleaseVfxPrefab | GameObject | Optional one-shot release VFX used at the reviewed release boundary |

The base component also owns shared runtime coordination contracts, but it does not serialize current targets, cooldowns, active-entity histories, or applied upgrades.

Attack animation parameter names remain model-presentation-local and do not belong to the combat component.

## 8.2 Concrete Combat Components

Each Tower Base Prefab must author exactly one compatible concrete component:

| Component | Tower | Additional Authored Fields |
|---|---|---|
| DirectionProjectileCombatBehaviour | Archer | projectileConfig |
| ArcProjectileCombatBehaviour | Cannon | projectileConfig, arcHeight |
| MagicOrbCombatBehaviour | Magic | magicOrbPrefab, magicOrbRotationSpeed, magicOrbOrbitRadius, magicOrbContactDistance, magicOrbMaxHitCount, magicOrbMaxLifetime, magicOrbSameTargetHitCooldown |
| DroneCombatBehaviour | Drone | dronePrefab, droneProjectileConfig, droneBatteryDuration, droneOrbitRadius, droneFlightSpeed, droneFlightHeight, droneBurstCount, droneBurstInterval, droneBurstCooldown |

The component type is authoritative. Tower authoring does not serialize a second AttackArchetype selector that can disagree with it.

TowerDefinition validation should confirm that its Tower Base Prefab contains the expected concrete combat component for the configured TowerFamily. A missing or mismatched component is an authoring error and should not be repaired by adding an untyped base TowerCombatBehaviour at runtime.

## 8.3 Runtime Data Construction

Attack Entity behavior classes do not read the combat MonoBehaviour continuously. At release, the concrete combat component combines its immutable authored base values with the placed tower's current level and upgrade state, then constructs only the typed runtime data relevant to the released entity.

```text
Prefab-authored combat fields
    + TowerLevelConfig
    + applied TowerUpgradeDefinitions
    -> resolved typed runtime data
    -> Projectile / Magic Orb / Drone initialization
```

Owned Attack Entities may later receive selective replacement runtime data when an approved level or upgrade change is Live Refresh. They do not receive the complete TowerUpgradeState and do not inspect unrelated TowerUpgradeDefinitions.

## 8.4 Archetype Notes

Direction Projectile uses the shared range, interval, target selection, release VFX, and its ProjectileConfig. The selected target defines the initial direction; Projectile System owns flight and hit resolution after release.

Arc Projectile uses the same common fields plus ProjectileConfig and the Arc component's authored arcHeight. It captures an immutable landing position. Explosion and bounce behavior remain package-owned rather than base Arc authoring.

Magic Orb uses common range and interval for tower release orchestration. Magic-specific authored fields define its base orbit, contact, hit-budget, lifetime, and prefab rules. Applied upgrades may selectively refresh damage, rotation speed, remaining hit budget by delta, and reviewed Behaviour options on active Orbs.

Drone uses common range, interval, target selection, and release VFX plus its Drone-specific authored fields. Drone-local movement, targeting, battery, burst, and Final Dive state remain owned by Drone runtime after release. Applied upgrades may selectively refresh approved future Drone behavior without replacing already-consumed entity history.

Attack VFX fields are optional and presentation-only. Empty references must not block combat execution or own gameplay decisions.

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
| Cannon | Baseline arrival may resolve one direct Monster; Explosive Shell and similar reviewed Behaviour content may add a Position Impact area Effect |
| Magic | No buff required; Magic Orb contact damage is owned by attack entity behavior |
| Drone | No buff required; Drone-fired projectile damage follows projectile attack entity rules |

Guideline:

- Use Projectile runtime logic for projectile movement and collision.
- Use Effect logic for reviewed instant gameplay events such as Behaviour-owned explosion damage or area damage calculation.
- Use Buff logic only when a gameplay state is attached to a unit over time, such as poison, slow, burn, weaken, or armor reduction.

Position Impact and Monster Hit should remain distinct runtime facts. An eligible tower-owned attack or Behaviour attack extension may request Elemental application independently of its DealDamage result; Buff cooldown and Protection decide the final application outcome.

Cannon Tower explosion Behaviour content should be treated as an instant area damage Effect rather than a Buff.

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

Uses TowerDefinition, the concrete prefab-authored TowerCombatBehaviour component, and resolved tower upgrade state to determine final runtime stats and combat behavior.

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
- Abstract TowerCombatBehaviour authoring contract and four first-version concrete component types
- Optional combat-component VFX and Attack Entity prefab references for projectile, Magic Orb, and Drone attacks
- Magic Arcane Field Behaviour-package VFX/runtime prefab structure contract

Excluded:

- Runtime combat execution
- Projectile implementation
- Buff implementation
- Upgrade application logic
- Draft generation rules
- Generic runtime VFX spawning, binding, playback, and cleanup beyond explicitly reviewed runtime-prefab contracts
- Final VFX prefab authoring and particle polish
- Particle collision driven combat logic

---
