# Task_001_GridNodeDataStructure

## 1. Task Goal

Create the basic Grid Node data structure for the Tower Nexus Map System.

This task only focuses on defining the data component used by each map node.

Do not implement map generation, pathfinding, tower placement, or visual refresh logic in this task.

---

## 2. Related Design Document

- `Docs/01_MapSystem.md`

---

## 3. Background

In Tower Nexus, the map is composed of multiple 2D grid-based tiles.

Each tile is represented by a Node object.

Each Node should be able to store:

- Its grid position
- Its world position
- Whether it is walkable
- Its visual object reference

Each Node will be used later by:

- Map Generator
- Map Query System
- Tower Placement System
- Pathfinding System
- Monster Movement System

---

## 4. Implementation Scope

Create a new C# script for the map node component.

Suggested script name:

```csharp
GridNodeBehaviour.cs
```

Suggested folder:

```text
Assets/Scripts/Map/
```

Create the folder if it does not exist.

---

## 5. Required Data Fields

The `GridNodeBehaviour` component should include the following serialized fields:

| Field Name | Type | Description |
|---|---|---|
| gridPosition | Vector2Int | The node position inside the grid |
| isWalkable | bool | Whether monsters can move through this node |
| spriteRenderer | SpriteRenderer | The visual renderer of this node |

The node world position does not need to be stored as a separate serialized field.

Use `transform.position` as the runtime world position.

---

## 6. Required Public Properties

Provide public read-only accessors:

```csharp
public Vector2Int GridPosition { get; }
public Vector3 WorldPosition { get; }
public bool IsWalkable { get; }
public SpriteRenderer SpriteRenderer { get; }
```

`WorldPosition` should return:

```csharp
transform.position
```

---

## 7. Required Public Methods

Implement the following methods:

```csharp
public void Initialize(Vector2Int gridPosition, bool isWalkable, SpriteRenderer spriteRenderer = null)
```

Purpose:

- Set the node grid position
- Set the initial walkable state
- Assign the sprite renderer reference
- If `spriteRenderer` parameter is null, try to get `SpriteRenderer` from the current GameObject

---

```csharp
public void SetWalkable(bool value)
```

Purpose:

- Update the walkable state of this node

---

```csharp
public void SetGridPosition(Vector2Int value)
```

Purpose:

- Update the grid position manually if needed by editor or generator tools

---

```csharp
public void SetSpriteRenderer(SpriteRenderer value)
```

Purpose:

- Assign or replace the visual renderer reference

---

## 8. Unity Inspector Requirements

The component should expose fields in Inspector for debugging.

Use `[SerializeField]` for private fields.

Example style:

```csharp
[SerializeField] private Vector2Int gridPosition;
[SerializeField] private bool isWalkable = true;
[SerializeField] private SpriteRenderer spriteRenderer;
```

Do not make fields public unless necessary.

---

## 9. Validation Requirements

Add basic editor/runtime validation:

```csharp
private void Reset()
```

In `Reset()`:

- Try to automatically assign `spriteRenderer` from the current GameObject

Optional:

```csharp
private void OnValidate()
```

In `OnValidate()`:

- If `spriteRenderer` is null, try to assign it automatically

Do not include heavy logic in `OnValidate()`.

---

## 10. Out of Scope

Do not implement:

- Map generation
- Grid manager
- Node neighbor query
- A* pathfinding
- Tower footprint logic
- Tower deployment validation
- Monster movement
- RuleTile sprite refresh
- OccupyType
- Static obstacle detection

These will be handled in later tasks.

---

## 11. Expected Result

After this task is completed:

- A `GridNodeBehaviour` component exists
- It can be attached to a Unity GameObject
- It stores grid position and walkable state
- It can expose world position through `transform.position`
- It can reference a `SpriteRenderer`
- It can be initialized by future map generation logic
- Unity project should compile without errors

---

## 12. Acceptance Criteria

The task is complete when:

- `GridNodeBehaviour.cs` exists under `Assets/Scripts/Map/`
- The script compiles successfully in Unity
- The component can be added to a GameObject
- Grid position is visible in Inspector
- Walkable state is visible in Inspector
- SpriteRenderer reference is visible in Inspector
- `Initialize()` correctly sets node data
- `SetWalkable()` correctly updates walkable state
- `WorldPosition` returns `transform.position`