# Task_003_MapQuerySystem

## 1. Task Goal

Create the basic node query system for the Tower Nexus Map System.

This task focuses on allowing gameplay systems to query and retrieve map nodes during runtime.

The query system will be used later by:

- Pathfinding System
- Tower Placement System
- Monster Movement System
- Runtime Occupancy System
- Future Gameplay Systems

This task only focuses on node retrieval and neighbor query functionality.

Do not implement pathfinding logic or tower placement validation in this task.

---

## 2. Related Design Documents

- `Docs/01_MapSystem.md`

---

## 3. Dependency

This task depends on:

```text
Task_001_GridNodeDataStructure
Task_002_MapGenerator
```

The following scripts must already exist:

- `GridNodeBehaviour.cs`
- `MapGeneratorBehaviour.cs`

---

## 4. Background

The map in Tower Nexus is composed of multiple grid nodes.

Gameplay systems need reliable ways to:

- Retrieve nodes by grid position
- Check whether coordinates are valid
- Query neighboring nodes
- Access map dimensions
- Access all generated nodes

The query system acts as the core access layer for future gameplay logic.

---

## 5. Implementation Scope

Modify:

```csharp
MapGeneratorBehaviour.cs
```

This task does NOT require creating a separate manager class yet.

The map query functionality should temporarily live inside `MapGeneratorBehaviour`.

Future versions may refactor this into a dedicated Map Manager system.

---

## 6. Required Internal Data Structure

Inside `MapGeneratorBehaviour`, maintain a runtime node container.

Recommended structure:

```csharp
private Dictionary<Vector2Int, GridNodeBehaviour> nodeDictionary;
```

Purpose:

- Fast node lookup
- Runtime query support
- Coordinate-based access

---

## 7. Dictionary Population Rules

During `GenerateMap()`:

After each node is instantiated:

- Add node into dictionary
- Use grid position as key

Example:

```csharp
nodeDictionary[new Vector2Int(x, y)] = node;
```

---

## 8. Required Public Properties

Implement:

```csharp
public int Width => width;
public int Height => height;
```

Implement:

```csharp
public IReadOnlyDictionary<Vector2Int, GridNodeBehaviour> NodeDictionary => nodeDictionary;
```

Purpose:

- Allow future systems to access generated nodes safely

---

## 9. Required Public Methods

Implement:

```csharp
public GridNodeBehaviour GetNode(Vector2Int gridPosition)
```

Purpose:

- Retrieve node by grid position
- Return null if node does not exist

---

Implement:

```csharp
public GridNodeBehaviour GetNode(int x, int y)
```

Purpose:

- Convenience overload for coordinate query

Internally:

```csharp
return GetNode(new Vector2Int(x, y));
```

---

Implement:

```csharp
public bool HasNode(Vector2Int gridPosition)
```

Purpose:

- Check whether node exists

---

Implement:

```csharp
public bool IsInsideBounds(Vector2Int gridPosition)
```

Purpose:

- Check whether coordinate is inside map bounds

Rules:

```csharp
x >= 0 && x < width
y >= 0 && y < height
```

---

## 10. Neighbor Query System

Implement:

```csharp
public List<GridNodeBehaviour> GetNeighborNodes(Vector2Int gridPosition)
```

Purpose:

- Return neighboring walkable or unwalkable nodes
- Used later for pathfinding

Current version should only support:

- Horizontal neighbors
- Vertical neighbors

Diagonal neighbors are NOT supported.

---

## 11. Neighbor Query Rules

The neighbor query should check four directions:

```text
Up
Down
Left
Right
```

Recommended direction list:

```csharp
Vector2Int.up
Vector2Int.down
Vector2Int.left
Vector2Int.right
```

For each direction:

- Calculate neighbor position
- Check bounds
- Retrieve node if valid
- Add to result list

---

## 12. ClearMap Update Requirement

When `ClearMap()` is called:

- Clear node dictionary
- Prevent stale references

Example:

```csharp
nodeDictionary.Clear();
```

---

## 13. Runtime Safety Requirements

Methods should safely handle:

- Invalid coordinates
- Empty dictionary
- Missing nodes

Avoid:

- Exceptions
- Out-of-range errors
- NullReference exceptions

---

## 14. Debugging Support

Optional:

Add temporary debug logs:

```csharp
Debug.Log(...)
```

Optional:

Add Gizmos later in future tasks.

Do not implement heavy debug visualization yet.

---

## 15. Out of Scope

Do not implement:

- A* pathfinding
- Runtime occupancy
- Tower placement validation
- Monster movement logic
- Spawn node management
- Target node management
- Tile refresh optimization
- Diagonal movement
- Weight cost system
- Terrain movement cost

These belong to future tasks.

---

## 16. Expected Result

After this task is completed:

- Nodes can be queried by coordinate
- Systems can retrieve neighbor nodes
- Map bounds can be validated
- Runtime systems can safely access nodes
- Dictionary-based lookup is available

---

## 17. Acceptance Criteria

The task is complete when:

- `MapGeneratorBehaviour` contains a node dictionary
- Nodes are registered during generation
- `GetNode()` works correctly
- `HasNode()` works correctly
- `IsInsideBounds()` works correctly
- `GetNeighborNodes()` returns correct neighbors
- Diagonal neighbors are excluded
- `ClearMap()` clears dictionary correctly
- Unity project compiles successfully