# Tower Nexus - Project Overview

---

# 1. Project Introduction

Tower Nexus is a strategy tower defense game focused on terrain manipulation and random build creation.

The core gameplay inspiration comes from games such as Underdark: Defence and Wittle Defender. The game combines traditional tower defense mechanics with roguelike drafting systems and terrain-based strategic decision making.

Unlike traditional tower defense games where towers are only used for combat, towers in Tower Nexus also function as physical obstacles that directly affect monster pathfinding and battlefield structure.

The battlefield is not static. Players continuously reshape navigable space during runtime through tower placement, tower repositioning, and terrain occupation changes.

In Tower Nexus, towers are not only combat units, but also runtime battlefield editing tools that dynamically influence map topology and monster movement behavior.

Players are expected to continuously make strategic decisions during gameplay, including:

- Tower placement positioning
- Path manipulation
- Build drafting
- Tower upgrading
- Synergy construction
- Resource distribution

The core experience of the game is to allow players to gradually reshape the battlefield through their own decisions and ultimately create unique combat builds during each run.

---

# 2. Core Gameplay Loop

The gameplay loop of Tower Nexus is structured as follows:

1. The level starts by loading a configured map prefab containing:
    - A Monster Spawn Point
    - A Monster Target Point
    - A grid-based map composed of multiple tiles

2. Monsters continuously spawn from the spawn point based on wave configuration.

3. Monsters use Monster System pathfinding functionality together with Map System data to calculate valid paths from Spawn Nodes to the Target Node and dynamically recalculate paths whenever battlefield walkability changes.

4. Players place towers onto the grid map to:
    - Attack and eliminate monsters
    - Occupy grid nodes
    - Block and alter monster movement paths

5. Eliminating monsters grants experience points to the player.

6. When enough experience is accumulated:
    - The player levels up
    - A new Draft selection is triggered

7. Through repeated drafting, placement, repositioning, upgrading, and path manipulation, the player gradually strengthens their build and survives increasingly difficult monster waves.

8. If monsters reach the target point:
    - Player HP is reduced

9. If player HP reaches 0:
    - The run fails

10. If all monster waves are eliminated:
    - The run succeeds

---

# 3. Core Gameplay Features

## 3.1 Terrain Manipulation Tower Defense

One of the core mechanics of Tower Nexus is that towers physically occupy map tiles and influence monster movement routes.

Each tower has a unique footprint shape similar to Tetromino shapes from Tetris.

Different tower prefabs may contain different occupied anchor layouts, allowing highly asymmetric placement patterns.

Different towers may occupy different tile patterns, such as:

- Line Shapes
- L Shapes
- T Shapes
- Square Shapes
- Cross Shapes

This creates a gameplay experience where players are not only building offensive structures, but are also dynamically reshaping the battlefield itself.

To prevent invalid gameplay states, the system prevents players from placing towers in positions that would completely block all valid monster paths between Spawn Nodes and the Target Node.

Tower placement is validated based on tower footprint anchors. Each tower prefab defines a center anchor and multiple occupied anchors. During placement, the center anchor snaps to a target grid node, while all occupied anchors must match valid walkable grid nodes. If any occupied anchor cannot find a corresponding grid node, overlaps an unwalkable node, or causes the monster path to become fully blocked, the tower cannot be placed.

When tower placement or removal changes map walkability, all alive monsters dynamically recalculate their paths from their current nodes toward the Target Node.

---

## 3.2 Draft-Based Build System

The Draft System is the core progression mechanic of Tower Nexus.

At the beginning of the game, the player may receive an initial 3-choice tower draft.

Each time the player levels up, another draft selection becomes available.

When the player selects a tower from the Draft Window, the selected tower is added to the Pending Tower Deployment Area instead of being placed immediately. The player may then drag a pending tower from this area onto the map for placement.

The first version currently supports two draft categories:

### Tower Draft

Allows players to:

- Obtain a new tower from a configured Tower Pool
- Add the selected tower into the Pending Tower Deployment Area
- Place pending towers onto the battlefield by dragging them from the pending area to the map
- Reposition placed towers through a recycle or redeployment flow
- Merge the tower with an existing identical tower for upgrading

### Upgrade Draft

Allows players to enhance existing tower types through stat or mechanic modifications.

Examples include:

- Increased attack speed
- Multi-projectile attacks
- Range enhancement
- Additional attack effects
- Mechanic transformations

The goal of the Draft System is to create deep build diversity and encourage players to adapt their strategy during each run.

---

## 3.3 Monster Counter System (Planned)

Future monster and boss designs may contain counter relationships against specific tower types or build strategies.

Examples may include:

- High armor enemies
- Fast movement enemies
- Flying monsters
- Anti-projectile enemies
- Split-type enemies
- Path-breaking or terrain-modifying bosses

The purpose of this system is to continuously encourage strategic adaptation and prevent static gameplay solutions.

---

## 3.4 Level and Stage Configuration Pipeline (Planned)

Future versions of Tower Nexus will include a LevelConfig or StageConfig driven level pipeline.

Current direction:

- Maps are authored as handcrafted map prefabs
- LevelConfig or StageConfig ScriptableObjects store map prefab references
- Future monster wave configurations will also be stored in level-related configuration assets
- Runtime gameplay loads and initializes configured map prefabs directly

This pipeline is intended to separate map authoring, level configuration, and runtime gameplay initialization.

---

# 4. Configuration Strategy

Tower Nexus uses Unity ScriptableObject assets as the primary configuration solution.

All gameplay configuration data should follow this strategy.

Current and planned configuration assets include:

- TowerDefinition
- TowerAttackConfig
- MonsterDefinition
- ProjectileConfig
- EffectConfig
- BuffConfig
- LevelConfig
- StageConfig

Odin Inspector may be used to improve configuration editing workflows, validation, and editor usability.

The first version does not use:

- Excel export tools
- CSV import pipelines
- JSON configuration generation
- External data table workflows

The goal is to maximize iteration speed during prototype and early production phases.

---

# 5. Core Design Philosophy

The core design philosophy of Tower Nexus is:

"Meaningful decisions create unique battlefield stories."

The game emphasizes:

- Strategic thinking
- Terrain control
- Dynamic adaptation
- Build experimentation
- Spatial planning
- Risk and reward balancing

Players are expected to constantly evaluate:

- Where to place towers
- How to manipulate monster paths
- Which draft option provides the highest value
- When to merge towers
- How to construct synergies
- How to adapt against different enemy types

The ultimate goal is to create a tower defense experience where every run feels strategically unique.

---

# 6. Core Systems Overview

## 6.1 Map System

The Map System provides the grid-based battlefield foundation of Tower Nexus.

It manages:

- Grid nodes
- Walkable and unwalkable states
- Runtime battlefield modification
- MeshRenderer-based tile visual refresh
- Direction-based tile connection visuals
- Handcrafted map prefab authoring workflow

The Map System does not directly handle tower placement rules, monster AI, combat logic, or draft logic. Other systems interact with the Map System through node query and walkability update APIs.

The Map System also acts as the runtime foundation for future LevelConfig or StageConfig driven gameplay loading workflows.

---


## 6.2 Player System

The Player System manages player runtime progression state and player battle survival state.

It is responsible for:

- Player level
- Player EXP accumulation
- Player level-up events
- Player HP management
- Player death and battle failure conditions
- Runtime player state events

The first implementation phase focuses on:

1. EXP gain from monster elimination.
2. Player level-up logic.
3. Player HP management.
4. Monster damage interaction when monsters reach the target node.
5. Battle failure handling when player HP reaches zero.
6. Runtime event broadcasting for level-up and HP changes.

The Player System acts as the owner of player runtime data.

Other systems may react to player state changes through events:

- Draft System may listen to player level-up events.
- Battle HUD UI System may display player EXP and HP.
- Monster System may notify Player System when monsters reach the target node.

The Player System should not directly manage tower placement, draft generation, monster movement, or UI implementation.

---

## 6.3 Battle HUD UI System

The Battle HUD UI System is responsible for displaying runtime battle UI during gameplay.

It is responsible for:

- Displaying player level
- Displaying player EXP progress
- Displaying player HP
- Opening and closing the Draft Window
- Displaying draft choices
- Displaying the Pending Tower Deployment Area
- Providing pending tower drag interaction entry points
- Displaying placement feedback
- Displaying future battle failure UI

The Battle HUD UI System does not own player progression, player HP calculation, draft generation, tower placement validation, monster runtime logic, or map walkability updates.

It acts as the runtime gameplay presentation layer.

---

## 6.4 Draft System

The Draft System manages runtime player choice generation during battle progression.

The first version focuses on tower-related drafting, including New Tower Draft and Tower Upgrade Draft.

It is responsible for:

- Listening to player level-up events
- Generating draft choices
- Managing draft result workflow
- Providing draft choice data to Battle HUD UI System
- Processing player draft selection results
- Creating pending tower entries for later placement

The Draft System does not own player EXP calculation, player level-up logic, Draft Window UI layout, tower placement validation, GridNode occupation, or map walkability updates.

Future versions may extend Draft System to support:

- Additional tower-related draft types
- Global Buff Draft
- Temporary Buff Draft
- Curse Draft
- Utility Draft
- Weighted rarity or synergy-based draft rules

---

## 6.5 Tower Placement System

The Tower Placement System manages the process of previewing, validating, placing, repositioning, recycling, and runtime battlefield reshaping through tower interaction on the grid map.

It is responsible for:

- Tower drag and snap placement from the pending deployment area
- Placement preview
- Placement validity checking
- Grid node walkability occupation and release
- Runtime walkability state management
- Runtime battlefield topology modification
- Runtime monster path reshaping through walkability changes
- Path-blocking validation before final placement
- Future tower recycle and redeployment flow

The Tower Placement System is only responsible for placement-related battlefield interaction and runtime map topology modification.

It does not own player progression, player HP, battle failure logic, draft generation, Draft Window UI, or Battle HUD UI layout.

The first implementation focuses on the core placement loop:

1. The player gains EXP through gameplay.
2. The Player System levels up the player.
3. The Draft System receives the level-up event.
4. A 3-choice draft UI is opened through Battle HUD UI System.
5. The player selects one tower from the draft options.
6. The selected tower is added to the Pending Tower Deployment Area.
7. The player drags a pending tower from the pending deployment area onto the map.
8. The tower preview snaps to grid nodes based on its center anchor.
9. The system validates all occupied anchors.
10. If placement is valid, the tower is placed and occupied grid nodes become unwalkable.
11. If placement is invalid, the tower remains in the pending deployment area.

Tower recycling and redeployment are planned as future extensions. When implemented, removed towers should release their occupied grid nodes and enter a UI-based recycle area, allowing players to drag them back onto the battlefield later.

This system transforms tower placement from a one-time build action into a continuous runtime tactical battlefield editing process.

Tower placement is fundamentally treated as runtime map topology editing rather than traditional static tower placement.

Runtime walkability states may differ from the original authored map walkability states due to tower occupation, future temporary obstacles, or future gameplay mechanics.

---

## 6.5.1 Tower Framework System

The Tower Framework System defines the shared tower architecture used by gameplay systems.

It is responsible for:

- TowerDefinition
- TowerAttackConfig
- Tower categories
- Attack archetypes
- Target selection types
- Tower prefab structure
- TowerAnchorSet
- Center Anchor
- Occupied Anchors
- Shared tower combat configuration references

The Tower Framework System defines what a tower is.

Runtime placement, combat, and upgrade behavior are owned by their respective systems.

---

## 6.6 Monster System

The Monster System manages monster spawning, runtime movement, pathfinding, death handling, target arrival reporting, and battlefield pressure generation.

It is responsible for:

- Monster wave spawning
- MonsterDefinition driven monster configuration
- Spawn Node and Target Node integration
- Runtime pathfinding functionality
- Runtime path recalculation
- Monster state management
- Monster movement along node paths
- Monster death handling
- EXP reward generation
- Player damage interaction reporting when monsters reach the target node
- Future monster behavior expansion

The first implementation phase focuses on:

1. Monster spawning through wave configurations.
2. Grid-based A* pathfinding.
3. Dynamic path recalculation when runtime walkability changes.
4. Monster movement toward the Target Node.
5. Monster death and EXP reward flow.
6. Monster target arrival notification to Player System.
7. Providing pathfinding functionality that can be reused by tower placement validation.

The Monster System interacts closely with the Map System and Tower Placement System.

- The Map System provides runtime walkability and node query support.
- The Tower Placement System modifies battlefield topology through tower occupation.
- The Player System receives player damage events when monsters reach the target node.
- The Monster System continuously reacts to runtime battlefield changes and updates movement paths accordingly.

Future versions may extend the Monster System with:

- Elite monsters
- Boss monsters
- Flying monsters
- Crowd control effects
- Special AI behaviors
- Terrain interaction mechanics
- Advanced combat mechanics

---

# 7. Current Runtime Architecture Direction

```text
PlayerSystem
    ↓ OnPlayerLevelUp
DraftSystem
    ↓ Draft Result

New Tower Draft
    ↓
BattleHUDUISystem
    ↓ Pending Tower Entry
TowerPlacementSystem

or

Tower Upgrade Draft
    ↓
TowerUpgradeSystem

TowerPlacementSystem
    ↓ Modify Walkability
MapSystem

MonsterSystem
    ↓ Pathfinding Queries
MapSystem
```

Player progression, drafting, UI interaction, placement, battlefield topology modification, and monster pathfinding are intentionally separated into independent runtime systems.

This separation is intended to:

- Improve long-term maintainability
- Reduce system coupling
- Clarify responsibility ownership
- Simplify future feature expansion
- Improve AI-assisted development workflows

---

# Change Log

## 2026-05-24

- Synced system names after splitting Draft System, Battle HUD UI System, and Tower Placement System into independent documents.
- Replaced Tower Deployment System references with Tower Placement System.
- Replaced Tower Draft System references with Draft System.
- Added Battle HUD UI System and Draft System to Core Systems Overview.
- Updated runtime architecture flow.
- Synced Project Overview with Tower Framework System, Tower Upgrade System, and updated ownership boundaries.

## 2026-05-31

- Added Configuration Strategy section as a project-level configuration guideline.
- Standardized ScriptableObject assets as the primary configuration solution for the first version.
- Documented Odin Inspector as the recommended configuration editing workflow.
- Clarified that the first version does not use Excel export tools, CSV import pipelines, JSON generation workflows, or external data table systems.
- Updated Tower Framework System overview to include TowerAttackConfig ownership.
- Synced Project Overview with the latest Tower Framework System architecture.