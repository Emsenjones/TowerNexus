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
| attackArchetype | AttackArchetype | Attack archetype |
| attackConfigId | string | Attack configuration reference |
| upgradeConfigId | string | Upgrade configuration reference |

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
| ArcherTower | Fires arrows toward enemies |
| CannonTower | Launches explosive projectiles |
| LaserTower | Emits a continuous beam attack |

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
| Projectile | Fires a projectile toward a target |
| Beam | Continuously damages a target with a beam |

Examples:

| Tower | Archetype |
|---|---|
| Archer Tower | Projectile |
| Cannon Tower | Projectile |
| Laser Tower | Beam |

---

# 8. Target Selection Types

Target selection determines how towers choose enemies.

Recommended first-version types:

| Type | Description |
|---|---|
| Nearest | Closest enemy |
| HighestHealth | Enemy with highest HP |
| Random | Random enemy within range |

Future expansion:

- LowestHealth
- FirstInPath
- LastInPath
- HighestThreat

---

# 9. Related Systems

## Draft System

Uses TowerDefinition to generate New Tower Draft choices.

---

## Tower Placement System

Uses TowerDefinition to instantiate tower prefabs.

---

## Tower Runtime Combat System

Uses TowerDefinition to determine attack archetypes and combat behavior.

---

## Tower Upgrade System

Uses TowerDefinition and upgrade configuration references to define and apply tower upgrades.

---

## Buff System

May be referenced by tower upgrade content and runtime combat systems to apply buff-related gameplay effects.

---

# 10. First Version Scope

Included:

- TowerDefinition structure
- Tower categories
- Attack archetypes
- Target selection types
- Runtime system references

Excluded:

- Runtime combat execution
- Projectile implementation
- Buff implementation
- Upgrade application logic
- Draft generation rules

---

# Change Log

## 2026-05-29

- Created Tower Framework System.
- Renamed the document from Tower Definition System to Tower Framework System to better reflect ownership of tower architecture, structure, categories, attack archetypes, and upgrade concepts.
- Extracted TowerDefinition ownership from Tower Placement System.
- Added tower categories.
- Added attack archetype definitions.
- Added target selection definitions.
- Added Tower Structure ownership including TowerPrefab, TowerAnchorSet, Center Anchor, Occupied Anchors, and footprint definitions.
- Moved tower upgrade layer ownership to Tower Upgrade System.