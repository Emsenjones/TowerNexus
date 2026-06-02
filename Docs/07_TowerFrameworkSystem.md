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

The Tower Framework System does not own:

- Runtime tower placement
- Runtime tower attack logic
- Runtime projectile movement
- Runtime buff execution
- Runtime draft generation
- Runtime tower upgrades

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

Upgrade-related configuration references are reserved for the future Tower Upgrade System and are not required by the first-version Tower Framework data implementation.

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
├── ProjectileSpawnPoint
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
| Magic | Locks onto one enemy and channels a continuous magic beam for a limited attack duration |
| Watch | Periodically damages all enemies within its attack radius |

Future categories may include:

- Support Tower
- Trap Tower
- Summon Tower
- Resource Tower

---

# 7. Attack Archetypes

Attack archetypes define the fundamental attack behavior of a tower.

Recommended first-version archetypes:

| Archetype | Description |
|---|---|
| StraightProjectile | Fires a projectile in a straight trajectory toward a target |
| ArcProjectile | Launches a projectile in an arcing trajectory toward a target position |
| ChannelBeam | Locks onto one target and continuously damages it during a limited attack duration |
| PeriodicArea | Periodically applies damage to all valid enemies within the tower attack radius |

Examples:

| Tower | Archetype | Core Behavior |
|---|---|---|
| Archer Tower | StraightProjectile | Fires low-damage arrows with short range and high attack speed |
| Cannon Tower | ArcProjectile | Fires slow arcing shells with long range; shells explode on impact and deal area damage |
| Magic Tower | ChannelBeam | After cooldown, selects one target and channels a beam for up to the configured attack duration; if the target dies early, the tower enters cooldown immediately |
| Watch Tower | PeriodicArea | While enemies are within range, periodically damages all enemies inside its attack radius |

---

## 7.1 Archer Tower Attack Pattern

Archer Tower uses straight projectile attacks.

Design intent:

- Low damage per projectile
- Short attack range
- High attack speed
- Projectile has its own collider
- Damage is applied when the projectile collides with a valid enemy

Archer Tower does not require buff or effect configuration in the first version.

---

## 7.2 Cannon Tower Attack Pattern

Cannon Tower uses arcing projectile attacks.

Design intent:

- Low attack speed
- Long attack range
- Projectile travels toward the selected enemy position
- Projectile explodes when it reaches the target position or impact point
- Explosion deals area damage to enemies within the explosion radius

The cannon projectile itself should be handled by projectile runtime logic.

The explosion may be represented as an impact effect or area damage effect, but it should not be treated as a buff in the first version because it does not persist on enemies over time.

---

## 7.3 Magic Tower Attack Pattern

Magic Tower uses channel beam attacks.

Design intent:

- After cooldown ends, the tower selects one valid target
- The tower locks onto that target and channels a magic beam
- The beam deals continuous damage during the attack duration
- Damage per second may increase the longer the same target is exposed to the beam
- If the target dies before the attack duration ends, the tower immediately enters cooldown
- If the attack duration ends and the target is still alive, the tower enters cooldown
- After cooldown ends, the tower selects a target again

Magic Tower does not require buff configuration in the first version. Its damage is owned by tower runtime attack logic.

---

## 7.4 Watch Tower Attack Pattern

Watch Tower uses periodic area attacks.

Design intent:

- The tower checks for valid enemies inside its attack radius
- If enemies are inside the radius, the tower periodically deals damage to all valid enemies in range
- Example: if attack interval is 1 second, the tower deals damage once per second to all enemies currently within range
- Enemies entering or leaving the radius are naturally included or excluded by the next periodic damage tick

Watch Tower should not apply a damage-over-time buff in the first version. The damage source is the Watch Tower itself, not a buff attached to each enemy.

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
- Channel attack parameters

AttackConfig does not contain runtime state.

Runtime state belongs to Tower Runtime Combat System.

Examples of runtime state:

- Current target
- Cooldown timer
- Channel timer
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
| damage | float | Base damage value |
| projectileConfigId | string | Projectile configuration reference |
| arcHeight | float | Arc projectile trajectory height |
| damagePerSecond | float | Continuous damage value |
| channelDamageInterval | float | Damage application interval during channeling |
| maxChannelDuration | float | Maximum channel duration |

Not every attack archetype requires every field.

Unused fields should be hidden in the Inspector whenever practical.

Editor tooling may use Odin Inspector conditional display features to show only fields relevant to the selected AttackArchetype.

---

## 8.3 AttackConfig Usage By Archetype

Different attack archetypes consume different AttackConfig fields.

### StraightProjectile

Typically uses:

- attackRange
- attackInterval
- damage
- projectileConfigId
- targetSelectionType

---

### ArcProjectile

Typically uses:

- attackRange
- attackInterval
- damage
- projectileConfigId
- arcHeight
- targetSelectionType

Notes:

- Explosion radius and area damage behavior belong to the Projectile System and Buff And Effect System, not AttackConfig.

---

### ChannelBeam

Typically uses:

- attackRange
- attackInterval
- damagePerSecond
- channelDamageInterval
- maxChannelDuration
- targetSelectionType

Notes:

- attackInterval controls how often the tower starts a new channel attack.
- channelDamageInterval controls how frequently damage is applied while channeling.

---

### PeriodicArea

Typically uses:

- attackRange
- attackInterval
- damage

PeriodicArea attacks do not require:

- projectileConfigId
- targetSelectionType

TargetSelectionType is not used by PeriodicArea because the tower applies damage to all valid monsters within attackRange.

attackInterval controls how often area damage is applied.

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

- LowestHealth
- FirstInPath
- LastInPath
- HighestThreat

Target selection may not be required by every attack archetype.

Examples:

| Tower | Target Selection Usage |
|---|---|
| Archer | Selects one target before firing |
| Cannon | Selects one target or target position before firing |
| Magic | Selects one target before channeling |
| Watch | Does not need single-target selection; it damages all enemies within range |

---

# 10. Effect and Buff Relationship

Tower attacks may reference effect or buff systems, but only when the tower behavior actually requires them.

First-version recommendation:

| Tower | Effect/Buff Usage |
|---|---|
| Archer | No buff/effect required; projectile collision applies direct damage |
| Cannon | Projectile impact may trigger an AreaDamageEffect; no buff required |
| Magic | No buff required; channel damage is owned by tower runtime combat logic |
| Watch | No buff required; periodic area damage is owned by tower runtime combat logic |

Guideline:

- Use Projectile runtime logic for projectile movement and collision.
- Use Effect logic for instant gameplay events such as explosion damage, impact visuals, or area damage calculation.
- Use Buff logic only when a gameplay state is attached to a unit over time, such as poison, slow, burn, weaken, or armor reduction.

Cannon Tower explosion should be treated as an instant area damage effect rather than a buff.

---

# 11. Related Systems

## Draft System

Uses TowerDefinition to generate New Tower Draft choices.

---

## Tower Placement System

Uses TowerDefinition to instantiate tower prefabs.

---

## Tower Runtime Combat System

Uses TowerDefinition and AttackConfig to determine attack archetypes and combat behavior.

---

## Tower Upgrade System

Uses TowerDefinition and upgrade configuration references to define and apply tower upgrades.

---

## Buff and Effect System

May be referenced by tower upgrade content and runtime combat systems to apply effect or buff-related gameplay behavior.

The first version should keep direct tower damage, projectile behavior, instant area damage effects, and persistent buffs clearly separated.

---

# 12. First Version Scope

Included:

- TowerDefinition structure
- Tower categories
- Attack archetypes
- Target selection types
- Runtime system references
- Basic relationship between tower attacks, effects, and buffs

Excluded:

- Runtime combat execution
- Projectile implementation
- Buff implementation
- Upgrade application logic
- Draft generation rules

---

# Change Log

## 2026-05-30

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