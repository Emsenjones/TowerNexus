# Tower Nexus - Map System Design Document

---

# 1. System Overview

The Map System is one of the core foundational systems in Tower Nexus.

The map is composed of multiple 2D grid-based tiles arranged into a rectangular battlefield. Each tile is represented by a Node object.

The Map System is responsible for:

- Managing grid nodes
- Managing walkable states
- Providing runtime occupancy updates
- Providing pathfinding-related map queries
- Supporting tower placement validation
- Refreshing map visual tiles
- Supporting prefab-based map editing workflow
- Supporting handcrafted map prefab workflow

The Map System is designed as a reusable and gameplay-neutral system.

The system itself does not care whether a node is occupied by:

- A tower
- A rock
- A tree
- A temporary wall
- Future gameplay mechanics

The system only cares whether a node is currently walkable.

---

# 2. Core Design Philosophy

The Map System is built around the following principles:

## 2.1 Grid-Based Battlefield

The battlefield is composed of multiple square nodes arranged in a 2D grid structure.

Each node represents a gameplay space unit that can:

- Allow movement
- Block movement
- Be occupied
- Be queried by gameplay systems

---

## 2.2 Runtime Battlefield Modification

The battlefield is not static.

During gameplay:

- Towers may dynamically occupy nodes
- Towers may be removed
- Node walkability may change during runtime

This allows players to continuously reshape monster movement paths during combat.

---

## 2.3 Logic Layer and Visual Layer Separation

The map system separates:

### Logic Layer

Responsible for:

- Node data
- Walkability
- Occupancy
- Runtime queries
- Pathfinding-related spatial queries

### Visual Layer

Responsible for:

- Prefab-based tile visuals
- MeshRenderer-based tile presentation
- Direction-based tile connection refresh
- Tile appearance updates

Gameplay logic should never directly depend on visual presentation.

---

## 2.4 Grid Scale and Axis Convention

Current project convention:

- 1 Grid Node = 1 Unity World Unit
- The map grid is placed on the XZ plane
- X axis represents grid width / horizontal direction
- Z axis represents grid height / vertical battlefield direction
- Y axis represents world height / elevation

This convention is used because Tower Nexus uses 3D tower models and 3D scene objects.

Map tiles, monsters, towers, obstacles, and placement logic should use XZ coordinates for ground positioning.

Y should only be used for:

- Object height
- Model vertical offset
- Visual layering
- Projectile height
- VFX positioning
- Future terrain elevation

This convention is used to simplify:

- Grid positioning
- Runtime coordinate calculation
- Pathfinding
- Tower placement
- Range calculation
- Mouse interaction
- Visual alignment

---

# 3. Node Structure

Each map tile is represented by a Grid Node.

Each node contains the following information:

| Field | Type | Description |
|---|---|---|
| Grid Position | Vector2Int | Position index inside the grid |
| World Position | Vector3 | World position on the XZ ground plane, using Y as height |
| Is Walkable | bool | Whether monsters can move through the node |
| Visual Object Reference | GameObject / MeshRenderer Tile Visual | Visual tile object reference |

## 3.1 Grid Position to World Position Mapping

Grid coordinates should be converted to world coordinates using the following convention:

```csharp
worldPosition = new Vector3(gridX * nodeSize, 0f, gridY * nodeSize);
```

This means:

- Grid X maps to Unity world X
- Grid Y maps to Unity world Z
- Unity world Y remains height

The Map System should avoid using Unity world Y as a ground-plane coordinate.

---

# 4. Walkable State

Nodes contain two core states:

## Walkable

Monsters can move through the node.

## Unwalkable

Monsters cannot move through the node.

Unwalkable nodes may be caused by:

- Static obstacles
- Decorative map objects
- Tower placement
- Runtime gameplay mechanics

The system should support runtime state switching.

---

# 5. Map Visual System

The map visual layer is composed of prefab-based 3D tile visuals.

Each visual tile is a prefab containing MeshRenderer-based presentation assets.

The visual tile system follows direction-based connection rules.

A predefined tile prefab set is used to automatically determine tile appearance based on neighboring node walkable states.

Current plan:

- 12 tile visual prefabs
- MeshRenderer-based ground tile presentation
- Automatic direction connection visuals
- Runtime visual refresh support

The 12 tile visual prefabs are defined by walkable directions around the current node:

| Walkable Directions | Prefab Meaning |
|---|---|
| Up + Down + Left + Right | Four-direction connected tile |
| None | No-direction connected tile |
| Up + Down | Vertical connected tile |
| Left + Right | Horizontal connected tile |
| Up + Left | Corner connected tile |
| Up + Right | Corner connected tile |
| Down + Left | Corner connected tile |
| Down + Right | Corner connected tile |
| Down + Left + Right | Three-direction connected tile |
| Up + Left + Right | Three-direction connected tile |
| Up + Down + Right | Three-direction connected tile |
| Up + Down + Left | Three-direction connected tile |

---

# 6. Visual Refresh Rules

Whenever node walkability changes:

- The map visual system should refresh tile visuals
- Tile prefabs should update according to neighboring node walkable states
- Each node should contain one generated tile visual child object

Current implementation strategy:

## Full Map Refresh

The first version of the system refreshes the entire map whenever node states change.

Reason:

- Easier implementation
- Faster iteration
- Current map scale is small
- Gameplay validation is prioritized over optimization

During refresh:

1. The system clears the old generated tile visual child object under each node.
2. The system checks whether the neighboring Up, Down, Left, and Right nodes are walkable.
3. The system selects one of the 12 tile visual prefabs based on the walkable direction combination.
4. The selected tile visual prefab is instantiated as a child object of the current node.
5. The generated tile visual is reset to local position zero, local rotation identity, and local scale one.

Direction judgment rule:

- Neighboring node exists and `IsWalkable == true`: this direction is considered walkable
- Neighboring node does not exist: this direction is considered unwalkable
- Neighboring node exists but `IsWalkable == false`: this direction is considered unwalkable
- Map boundary is therefore treated as an unwalkable direction

Current limitation:

- The current 12-prefab set does not include single-direction dead-end tile visuals.
- Map layout should avoid single-direction walkable connection cases for now.
- Future versions may add 4 additional dead-end tile prefabs for Up-only, Down-only, Left-only, and Right-only cases.

Future versions may optimize this into partial local refresh.

---

# 7. Map Authoring Workflow

The current version of Tower Nexus uses a prefab-based handcrafted map workflow.

Maps are manually authored inside the Unity Editor and then saved as reusable map prefabs.

The runtime system loads and uses these map prefabs directly.

The Map System itself does not serialize or generate gameplay map data through ScriptableObjects.

ScriptableObjects are only intended to store references to map prefabs and future level-related metadata.

---

## Step 1

Create an empty GameObject in Unity Scene.

---

## Step 2

Attach Map Editor Behaviour script.

---

## Step 3

Input:

- Width
- Height

Click Generate Map button.

---

## Step 4

The system automatically generates:

- Grid Nodes
- MeshRenderer-based tile visual prefab instances
- Node child objects

Each node becomes a child object of the map root object.

The generated map root GameObject acts as the runtime map entity.

Each GridNodeBehaviour stores gameplay node state data, while visual tile prefabs are generated as child objects under each node.

---

## Step 5

Level designer manually places:

- Rocks
- Trees
- Obstacles
- Decorations

Occupied nodes are manually marked as unwalkable.

---

## Step 6

The completed map GameObject is saved as a Prefab.

Future LevelConfig or StageConfig ScriptableObjects may reference these map prefabs for runtime level loading.

---

# 8. Runtime Occupancy

During gameplay:

- Towers may occupy nodes
- Towers may release nodes when destroyed or removed

When occupancy changes:

- Node walkability updates
- Map visuals refresh
- Pathfinding data updates

The Map System itself only handles node state changes.

Gameplay systems such as:

- Tower Placement System
- Monster System

will use the Map System APIs.

---

# 9. Monster Movement Rules

Monsters move using pathfinding systems that query Map System data.

The Map System does not own pathfinding algorithms.

The Map System only provides:

- Node data
- Walkability data
- Spawn node references
- Target node references
- Spatial queries required by pathfinding systems

Current movement rules:

- Horizontal movement allowed
- Vertical movement allowed
- Diagonal movement disabled
- Movement is calculated on the XZ ground plane

The pathfinding system will search for valid paths from:

- Monster Spawn Point
  to
- Monster Target Point

---

# 10. Spawn and Target Nodes

Each map contains:

## Spawn Node

Monster spawn position.

## Target Node

Monster destination position.

The Target Node only defines the destination position on the map.

The Map System does not handle player HP damage, battle failure, or monster arrival consequences.

When a monster reaches the Target Node, the Monster System should detect the arrival result and notify the Player System if player HP damage needs to be applied.

Target arrival may also cause Monster System to report the monster as resolved for ResolvedMonsterCount progression while the run is still active.

Map System does not own monster resolution, player progression, or player HP consequences.

The current version supports:

- Single Spawn Node
- Single Target Node

Future versions may support:

- Multiple spawn points
- Multiple target points
- Dynamic target switching

---

# 11. System Responsibilities

The Map System is responsible for:

- Managing node data
- Managing walkability
- Providing node queries
- Providing pathfinding-related map queries
- Supporting runtime occupancy updates
- Managing map visual refresh
- Supporting prefab-based map workflow
- Supporting handcrafted map prefab workflow

The Map System is NOT responsible for:

- Monster AI
- Monster arrival consequence handling
- Player HP damage
- Battle failure logic
- Tower gameplay logic
- Combat logic
- Pathfinding algorithm ownership
- Draft system logic

---

# 12. Future Expansion Possibilities

Potential future features include:

- Multiple terrain types
- Slow zones
- Damage zones
- Dynamic terrain destruction
- Runtime generated maps
- LevelConfig / StageConfig based level loading
- Custom map editor tooling
- Partial local visual refresh optimization
- Multi-layer terrain
- Interactive environment mechanics
- Advanced visual optimization

Future features that depend on player state should still be owned by Player System or other gameplay systems. Map System should only provide map data, node queries, walkability state, pathfinding-related map queries, and spatial references.

These features are not required for the first playable version.

---

# Change Log

## 2026-06-18 (Monster Resolution Boundary Sync)

- Clarified that Target Node arrival may cause Monster System to report monster resolution.
- Clarified that Map System does not own ResolvedMonsterCount progression or player HP consequences.

## 2026-05-24 (Naming Sync)

- Updated deployment terminology to placement terminology.
- Synced naming with Tower Placement System.
- Clarified ownership boundary between Map System, Monster System, and pathfinding-related functionality.

## 2026-05-22

- Clarified that Target Node only defines map destination position.
- Clarified that monster arrival consequences should be handled by Monster System and Player System, not Map System.
- Added Player HP damage and battle failure logic to Map System non-responsibilities.

## 2026-05-19 (Workflow Update)

- Updated map workflow terminology from Map Generation Workflow to Map Authoring Workflow.
- Clarified that maps are stored as handcrafted map prefabs.
- Clarified that ScriptableObjects only store map prefab references and future level metadata.
- Added runtime map entity description.
- Added future LevelConfig / StageConfig expansion direction.

## 2026-05-19

- Updated map visual system from sprite-based visual refresh to MeshRenderer prefab-based tile visuals.
- Defined 12 tile visual prefabs based on Up, Down, Left, and Right walkable direction combinations.
- Updated visual refresh rules to instantiate tile prefab instances under each node.
- Clarified boundary and missing-node handling as unwalkable directions.
- Documented the current limitation that single-direction dead-end tile visuals are not supported yet.
- Fixed Node Structure table formatting.

## 2026-05-10

- Initial Map System Design Document created.
- Defined grid-based map structure.
- Defined walkable state workflow.
- Defined RuleTile visual refresh workflow.
- Defined prefab-based map editing pipeline.
