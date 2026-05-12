# Task_002_MapGenerator

## 1. Task Goal

Create the basic map generation system for the Tower Nexus Map System.

This task focuses on generating a grid-based map in Unity Scene using `GridNodeBehaviour`.

The generator should automatically create nodes based on width and height settings.

Do not implement pathfinding, tower placement, or advanced visual refresh logic in this task.

---

## 2. Related Design Documents

- `Docs/01_MapSystem.md`

---

## 3. Dependency

This task depends on:

```text
Task_001_GridNodeDataStructure
```

`GridNodeBehaviour.cs` must already exist before implementing this task.

---

## 4. Background

In Tower Nexus, maps are built using a prefab-based workflow.

Designers should be able to:

1. Create an empty GameObject
2. Attach a Map Generator component
3. Configure map size
4. Click Generate
5. Automatically generate the full grid map

Generated nodes should become child objects of the map root object.

Each node should:

- Store grid position
- Store walkable state
- Have proper world position
- Be accessible for future systems

---

## 5. Implementation Scope

Create a new C# script:

```csharp
MapGeneratorBehaviour.cs
```

Suggested folder:

```text
Assets/Scripts/Map/
```

---

## 6. Required Serialized Fields

The component should expose the following fields in Inspector:

| Field Name | Type | Description |
|---|---|---|
| width | int | Map width |
| height | int | Map height |
| nodeSize | float | World size between nodes |
| nodePrefab | GridNodeBehaviour | Prefab used to generate nodes |
| generatedNodesParent | Transform | Optional parent container for generated nodes |

Example style:

```csharp
[SerializeField] private int width = 10;
[SerializeField] private int height = 10;
[SerializeField] private float nodeSize = 1f;
[SerializeField] private GridNodeBehaviour nodePrefab;
[SerializeField] private Transform generatedNodesParent;
```

---

## 7. Map Generation Rules

When generation happens:

- Create a full rectangular grid
- Width controls X axis count
- Height controls Y axis count
- All generated nodes should default to:
    - `isWalkable = true`

Node world position should follow:

```csharp
new Vector3(x * nodeSize, y * nodeSize, 0f)
```

---

## 8. Generated Object Structure

Generated node objects should:

- Be instantiated from `nodePrefab`
- Become child objects of:
    - `generatedNodesParent`
      OR
    - current map root transform if parent is null

Recommended hierarchy:

```text
MapRoot
└── Nodes
    ├── Node_0_0
    ├── Node_1_0
    ├── Node_2_0
```

Generated node naming format:

```text
Node_X_Y
```

Example:

```text
Node_3_5
```

---

## 9. Required Public Methods

Implement:

```csharp
public void GenerateMap()
```

Purpose:

- Generate the entire map grid
- Instantiate nodes
- Initialize node data
- Position nodes correctly

---

Implement:

```csharp
public void ClearMap()
```

Purpose:

- Remove all previously generated node objects
- Prevent duplicate generation

---

## 10. Generation Workflow

Inside `GenerateMap()`:

### Step 1

Validate required references:

- `nodePrefab` must not be null

If missing:

- Output warning/error log
- Stop generation

---

### Step 2

Call:

```csharp
ClearMap()
```

---

### Step 3

Loop through:

```csharp
x = 0 → width - 1
y = 0 → height - 1
```

---

### Step 4

Instantiate node prefab.

---

### Step 5

Assign:

```csharp
gridPosition
```

using:

```csharp
new Vector2Int(x, y)
```

---

### Step 6

Set node world position.

---

### Step 7

Set node object name:

```text
Node_X_Y
```

---

### Step 8

Initialize node:

```csharp
Initialize(gridPosition, true)
```

---

## 11. ClearMap Rules

`ClearMap()` should:

- Remove all generated child nodes
- Work in both:
    - Play Mode
    - Edit Mode

Recommended approach:

```csharp
DestroyImmediate()
```

for editor-time cleanup.

Avoid memory leaks or duplicate nodes.

---

## 12. Unity Editor Requirements

The generator should support editor-time generation.

Add:

```csharp
[ExecuteAlways]
```

Optional:

```csharp
[ContextMenu("Generate Map")]
```

Optional:

```csharp
[ContextMenu("Clear Map")]
```

This allows quick generation directly from Inspector.

---

## 13. Out of Scope

Do not implement:

- Pathfinding
- Neighbor query system
- Runtime occupancy
- RuleTile refresh logic
- Obstacle placement
- Tower placement validation
- Spawn node logic
- Target node logic
- Save/load system
- Custom editor tools

These belong to later tasks.

---

## 14. Expected Result

After this task is completed:

- A map root object can generate a grid map
- Nodes are automatically instantiated
- Nodes are correctly positioned
- Nodes become child objects
- Nodes are initialized with grid positions
- Unity scene displays a complete grid structure

---

## 15. Acceptance Criteria

The task is complete when:

- `MapGeneratorBehaviour.cs` exists
- Script compiles successfully
- Width and height can be configured in Inspector
- Clicking Generate creates a grid map
- All nodes use `GridNodeBehaviour`
- Node names follow:
    - `Node_X_Y`
- Node positions are correct
- Re-generating does not create duplicates
- `ClearMap()` removes generated nodes successfully
- Unity project compiles without errors