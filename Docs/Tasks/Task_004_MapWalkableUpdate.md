# Task_004_MapWalkableUpdate

## 1. Task Goal

Create the runtime walkable state update system for the Tower Nexus Map System.

This task focuses on allowing gameplay systems to dynamically modify node walkability during runtime.

Examples include:

- Tower deployment
- Tower removal
- Runtime obstacle creation
- Future gameplay mechanics

This task only focuses on runtime node state modification and map update APIs.

Do not implement tower placement logic, pathfinding validation, or advanced visual systems in this task.

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
```

The following scripts must already exist:

- `GridNodeBehaviour.cs`
- `MapGeneratorBehaviour.cs`

---

## 4. Background

Tower Nexus is built around dynamic battlefield modification.

During gameplay:

- Towers may occupy nodes
- Towers may release nodes
- Runtime gameplay mechanics may modify map structure

The Map System must support safely updating node walkability during runtime.

The Map System itself does not understand gameplay semantics such as:

- Tower logic
- Combat logic
- Placement rules

It only manages node state changes.

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

## 6. Required Public Methods

Inside `MapGeneratorBehaviour.cs` implement:

```csharp
public bool SetNodeWalkable(Vector2Int gridPosition, bool value)
```

Purpose:

- Update node walkable state using grid position
- Return true if successful
- Return false if node does not exist

---

Implement:

```csharp
public bool SetNodeWalkable(int x, int y, bool value)
```

Purpose:

- Convenience overload for coordinate input

Internally:

```csharp
return SetNodeWalkable(new Vector2Int(x, y), value);
```

---

## 7. Walkable Update Rules

When updating node walkability:

### Step 1

Check whether node exists.

If node is missing:

- Return false
- Do not throw exception

---

### Step 2

Retrieve node using existing query system.

---

### Step 3

Call:

```csharp
node.SetWalkable(value);
```

---

### Step 4

Trigger map visual refresh hook.

(Current version may use simple full refresh.)

---

### Step 5

Return true.

---

## 8. GridNodeBehaviour Update Requirement

Inside:

```csharp
GridNodeBehaviour.cs
```

Ensure:

```csharp
SetWalkable(bool value)
```

properly updates internal state.

Current version only needs:

```csharp
isWalkable = value;
```

Do not implement gameplay callbacks yet.

---

## 9. Required Visual Refresh Hook

Inside:

```csharp
MapGeneratorBehaviour.cs
```

Implement:

```csharp
private void RefreshMapVisual()
```

Purpose:

- Temporary visual refresh entry point
- Called whenever walkable state changes

Current version may:

- Contain only debug logs
  OR
- Stay empty with TODO comment

Example:

```csharp
private void RefreshMapVisual()
{
    // TODO:
    // Implement RuleTile visual refresh in future task.
}
```

This method exists to establish future system architecture.

---

## 10. Runtime Safety Requirements

The system must safely handle:

- Invalid coordinates
- Missing nodes
- Repeated updates
- Runtime updates during gameplay

Avoid:

- Exceptions
- NullReference errors
- Invalid dictionary access

---

## 11. Debugging Support

Optional:

Add temporary debug logs:

```csharp
Debug.Log(...)
```

Example:

```csharp
Debug.Log($"Set Node {gridPosition} Walkable = {value}");
```

Keep logs lightweight.

---

## 12. Out of Scope

Do not implement:

- Pathfinding recalculation
- Tower placement validation
- Complete path blocking checks
- RuleTile sprite refresh logic
- OccupyType system
- Event systems
- Runtime gameplay callbacks
- Visual optimization
- Neighbor-based refresh optimization

These belong to future tasks.

---

## 13. Expected Result

After this task is completed:

- Gameplay systems can modify node walkability during runtime
- Nodes update safely
- Future systems have a unified API for node modification
- Runtime battlefield modification becomes possible

---

## 14. Acceptance Criteria

The task is complete when:

- `SetNodeWalkable(Vector2Int, bool)` exists
- `SetNodeWalkable(int, int, bool)` exists
- Node walkability can be updated during runtime
- Invalid coordinates safely return false
- `GridNodeBehaviour.SetWalkable()` updates state correctly
- `RefreshMapVisual()` exists
- Unity project compiles successfully
- Runtime updates do not throw exceptions