# Tower Nexus - Map System Design Document

---

# 1. System Overview

The Map System is one of the core foundational systems in Tower Nexus.

The map is composed of multiple 2D grid-based tiles arranged into a rectangular battlefield. Each tile is represented by a Node object.

The Map System is responsible for:

- Managing grid nodes
- Managing walkable states
- Providing runtime occupancy updates
- Supporting monster pathfinding
- Supporting tower deployment validation
- Refreshing map visual tiles
- Supporting prefab-based map editing workflow

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
- Pathfinding support

### Visual Layer

Responsible for:

- Tile sprites
- RuleTile visual refresh
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
| Is Walkable | bool | Whether monsters can move through the node |
| Visual Object Reference | GameObject / SpriteRenderer | Visual tile object reference |

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
- Tower deployment
- Runtime gameplay mechanics

The system should support runtime state switching.

---

# 5. Map Visual System

The map visual layer is composed of 2D sprite tiles.

The visual tile system follows Rule Tile style connection rules.

A predefined tile sprite set will be used to automatically determine tile appearance based on neighboring node states.

Current plan:

- Approximately 13 tile sprites
- Automatic edge/corner connection visuals
- Runtime visual refresh support

---

# 6. Visual Refresh Rules

Whenever node walkability changes:

- The map visual system should refresh tile visuals
- Tile sprites should update according to neighboring node states

Initial implementation strategy:

## Full Map Refresh

The first version of the system may refresh the entire map whenever node states change.

Reason:

- Easier implementation
- Faster iteration
- Current map scale is small
- Gameplay validation is prioritized over optimization

Future versions may optimize this into partial local refresh.

---

# 7. Map Generation Workflow

Maps are created using a prefab-based workflow.

## Step 1

Create an empty GameObject in Unity Scene.

---

## Step 2

Attach Map Generator script.

---

## Step 3

Input:

- Width
- Height

Click Generate button.

---

## Step 4

The system automatically generates:

- Grid Nodes
- Tile visuals
- Node child objects

Each node becomes a child object of the map root object.

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
- Pathfinding System

will use the Map System APIs.

---

# 9. Monster Movement Rules

Monsters move using grid-based pathfinding.

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
- Supporting runtime occupancy updates
- Managing map visual refresh
- Supporting prefab-based map workflow

The Map System is NOT responsible for:

- Monster AI
- Tower gameplay logic
- Combat logic
- Pathfinding algorithm implementation
- Draft system logic

---

# 12. Future Expansion Possibilities

Potential future features include:

- Multiple terrain types
- Slow zones
- Damage zones
- Dynamic terrain destruction
- Runtime generated maps
- Multi-layer terrain
- Interactive environment mechanics
- Advanced visual optimization

These features are not required for the first playable version.

---

# Change Log

## 2026-05-10

- Initial Map System Design Document created.
- Defined grid-based map structure.
- Defined walkable state workflow.
- Defined RuleTile visual refresh workflow.
- Defined prefab-based map editing pipeline.

```