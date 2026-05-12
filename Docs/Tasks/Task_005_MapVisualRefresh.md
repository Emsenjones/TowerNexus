# Task_005_MapVisualRefresh

## 1. Task Goal

Create the basic map visual refresh system for the Tower Nexus Map System.

This task focuses on synchronizing map visuals with node walkable states.

When node walkability changes during runtime, the visual appearance of the map should update automatically.

This task establishes the first version of the map visual layer.

Do not implement advanced RuleTile systems or optimization in this task.

---

## 2. Related Design Documents

- `Docs/01_MapSystem.md`

---

## 3. Dependency

This task depends on:

```text
Task_001_GridNodeDataStructure
Task_002_MapGenerator
Task_003_MapQuerySystem
Task_004_MapWalkableUpdate
```

The following scripts must already exist:

- `GridNodeBehaviour.cs`
- `MapGeneratorBehaviour.cs`

---

## 4. Background

The Tower Nexus battlefield is visually represented using 2D tile sprites.

The map visual system must reflect:

- Walkable nodes
- Unwalkable nodes
- Runtime node changes

The current version focuses on establishing the connection between:

```text
Node State
↓
Map Visual
```

Future versions may expand into:

- RuleTile systems
- Neighbor-based tile selection
- Animated tiles
- Terrain themes
- Visual optimization

---

## 5. Implementation Scope

Modify:

```csharp
MapGeneratorBehaviour.cs
```

Modify:

```csharp
GridNodeBehaviour.cs
```

Do not create additional manager systems yet.

---

## 6. Required Serialized Fields

Inside `MapGeneratorBehaviour.cs` add:

| Field Name | Type | Description |
|---|---|---|
| walkableSprite | Sprite | Sprite used for walkable nodes |
| unwalkableSprite | Sprite | Sprite used for unwalkable nodes |

Example:

```csharp
[SerializeField] private Sprite walkableSprite;
[SerializeField] private Sprite unwalkableSprite;
```

---

## 7. Visual Refresh Rules

Map visuals should update based on:

```text
GridNodeBehaviour.IsWalkable
```

Rules:

| Node State | Sprite |
|---|---|
| Walkable | walkableSprite |
| Unwalkable | unwalkableSprite |

---

## 8. Required Public Methods

Inside `MapGeneratorBehaviour.cs` implement:

```csharp
public void RefreshMapVisual()
```

Purpose:

- Refresh all node visuals
- Synchronize sprite appearance with node walkable state

Current version uses:

```text
Full Map Refresh
```

---

## 9. Refresh Workflow

Inside `RefreshMapVisual()`:

### Step 1

Loop through all nodes inside:

```csharp
nodeDictionary
```

---

### Step 2

Check:

```csharp
node.IsWalkable
```

---

### Step 3

Retrieve node SpriteRenderer.

Skip node safely if renderer is missing.

---

### Step 4

Assign sprite:

```csharp
Walkable:
walkableSprite

Unwalkable:
unwalkableSprite
```

---

## 10. Runtime Integration Requirement

Whenever:

```csharp
SetNodeWalkable(...)
```

is called:

The system should automatically call:

```csharp
RefreshMapVisual()
```

This ensures runtime visual synchronization.

---

## 11. GridNodeBehaviour Requirements

Inside:

```csharp
GridNodeBehaviour.cs
```

Ensure:

```csharp
public SpriteRenderer SpriteRenderer { get; }
```

works correctly.

The visual system depends on this accessor.

---

## 12. Runtime Safety Requirements

The system must safely handle:

- Missing SpriteRenderer
- Missing sprites
- Empty node dictionary
- Runtime updates

Avoid:

- Exceptions
- NullReference errors

If sprites are missing:

- Output warning logs
- Continue safely

---

## 13. Debugging Support

Optional:

Add temporary logs:

```csharp
Debug.Log(...)
```

Example:

```csharp
Debug.Log("Map Visual Refreshed");
```

Keep logs lightweight.

---

## 14. Out of Scope

Do not implement:

- Real RuleTile system
- Neighbor-based tile selection
- Edge/corner tile detection
- Animated tiles
- Terrain types
- Tile blending
- Partial local refresh optimization
- Shader-based terrain visuals
- Visual effects
- Multi-layer terrain

These belong to future tasks.

---

## 15. Expected Result

After this task is completed:

- Walkable nodes display walkable sprite
- Unwalkable nodes display unwalkable sprite
- Runtime node updates refresh visuals automatically
- The battlefield visually reflects gameplay state

---

## 16. Acceptance Criteria

The task is complete when:

- `walkableSprite` exists in Inspector
- `unwalkableSprite` exists in Inspector
- `RefreshMapVisual()` exists
- All nodes refresh visuals correctly
- Runtime walkable updates change sprites automatically
- Missing SpriteRenderer does not throw exception
- Unity project compiles successfully