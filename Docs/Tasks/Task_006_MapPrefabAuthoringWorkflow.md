# Task_006_MapAuthoringWorkflow

---

# 1. Task Goal

Implement a handcrafted map authoring workflow for Tower Nexus.

This task focuses on editor-time map creation and visual refresh workflows.

The goal is to allow level designers to:

- Generate grid-based maps inside Unity Editor
- Modify node walkability manually
- Refresh tile visuals based on node walkable states
- Save completed maps as reusable prefabs

This task is editor workflow focused.

This task does NOT implement runtime level loading systems.

---

# 2. Scope

This task includes:

- Map editor workflow
- Grid node generation
- Tile visual prefab refresh workflow
- Runtime node structure support
- Map prefab authoring workflow

This task does NOT include:

- LevelConfig system
- StageConfig system
- Monster wave system
- Runtime level loading
- Runtime map streaming
- Procedural map generation
- Pathfinding algorithm implementation
- Custom EditorWindow tooling
- Partial local visual refresh optimization

---

# 3. Existing Systems

This task extends the existing systems:

- Map System
- GridNodeBehaviour
- Runtime walkability workflow
- Tile visual refresh workflow

The implementation should follow:

- `Docs/01_MapSystem.md`
- `Docs/ProjectOverview.md`

---

# 4. Core Workflow

Target workflow:

```text
Create Empty GameObject
    ↓
Attach MapEditorBehaviour
    ↓
Configure Width / Height
    ↓
Generate Grid Nodes
    ↓
Modify GridNodeBehaviour.IsWalkable manually
    ↓
Refresh Tile Visuals
    ↓
Save as Map Prefab
```

---

# 5. Functional Requirements

## 5.1 Map Root Structure

The generated map should contain:

```text
MapRoot
    ├── Nodes
    │     ├── Node_0_0
    │     ├── Node_0_1
    │     ├── ...
```

Each node should contain:

- GridNodeBehaviour
- Generated tile visual child object

---

## 5.2 Map Editor Behaviour

Implement or refactor a `MapEditorBehaviour`.

Responsibilities:

- Generate map nodes
- Refresh tile visuals
- Clear generated map content
- Store map editor configuration

The script should support:

| Field | Description |
|---|---|
| Width | Map width |
| Height | Map height |
| Node Size | Grid size |
| Node Prefab | Grid node prefab |
| Tile Visual Prefabs | 12 direction-based tile prefabs |

---

## 5.3 Generate Map

The Generate Map workflow should:

1. Clear old generated content
2. Create node parent object
3. Generate grid nodes
4. Assign grid positions
5. Initialize default walkable states
6. Generate tile visuals
7. Build runtime node dictionary support

Generated nodes should use:

```csharp
new Vector3(x * nodeSize, 0f, y * nodeSize)
```

The map uses the XZ plane.

---

## 5.4 Tile Visual Refresh

The visual refresh workflow should support:

- Full map refresh
- Direction-based tile selection
- MeshRenderer-based tile prefabs
- Runtime-safe visual regeneration

Tile visual selection depends on:

- Current node walkability
- Neighbor node walkability

Neighbor directions:

- Up
- Down
- Left
- Right

---

## 5.5 Tile Visual Rules

Current visual rules use 12 tile prefabs:

| Walkable Directions | Visual Type |
|---|---|
| Up + Down + Left + Right | Four-direction |
| None | Closed tile |
| Up + Down | Vertical |
| Left + Right | Horizontal |
| Up + Left | Corner |
| Up + Right | Corner |
| Down + Left | Corner |
| Down + Right | Corner |
| Down + Left + Right | Three-direction |
| Up + Left + Right | Three-direction |
| Up + Down + Right | Three-direction |
| Up + Down + Left | Three-direction |

Current limitation:

Single-direction dead-end tiles are not supported yet.

Maps should currently avoid generating single-direction walkable cases.

---

## 5.6 Refresh Visual Workflow

Refresh Visual workflow should:

1. Remove old generated tile visuals
2. Recalculate neighbor connectivity
3. Select matching tile prefab
4. Instantiate visual prefab
5. Reset local transform

Generated tile visuals should:

- Become child objects of the node
- Use local position zero
- Use local rotation identity
- Use local scale one

---

## 5.7 Walkability Rules

Direction judgment rules:

| Situation | Result |
|---|---|
| Neighbor exists and walkable | Walkable direction |
| Neighbor missing | Unwalkable direction |
| Neighbor exists but unwalkable | Unwalkable direction |

Map boundaries are treated as unwalkable directions.

---

## 5.8 Manual Authoring Workflow

Level designers should be able to:

- Select GridNodeBehaviour objects manually
- Modify `IsWalkable`
- Refresh visuals immediately
- Build handcrafted map layouts

The workflow should prioritize:

- Simplicity
- Fast iteration
- Visual clarity

---

# 6. Technical Requirements

## 6.1 Editor Workflow

The implementation should work directly inside Unity Editor.

Preferred options:

- ContextMenu
- Odin Inspector buttons
- Inspector buttons

Custom EditorWindow is NOT required.

---

## 6.2 Runtime Separation

GridNodeBehaviour should remain gameplay-data focused.

GridNodeBehaviour should NOT:

- Manage tile prefab selection
- Store tile visual prefab references
- Contain visual refresh logic

Map-level visual refresh should be managed by MapEditorBehaviour.

---

## 6.3 Visual Refresh Strategy

Current version uses:

```text
Full Map Refresh
```

Reason:

- Easier implementation
- Easier debugging
- Faster iteration
- Current map size is small

Partial refresh optimization is future work.

---

# 7. Acceptance Criteria

The task is complete when:

- Maps can be generated inside Unity Editor
- Nodes are generated correctly on the XZ plane
- GridNodeBehaviour stores node state correctly
- Tile visuals refresh correctly
- Walkability updates affect visual refresh
- Old tile visuals are cleaned correctly
- Maps can be saved as prefabs
- Generated prefabs can be reused safely

---

# 8. Important Constraints

Do NOT implement:

- LevelConfig system
- StageConfig system
- Runtime level loading
- Runtime map generation
- Procedural generation
- Monster wave logic
- Pathfinding refactor
- Custom editor windows
- Runtime streaming systems

Do NOT redesign unrelated systems.

Do NOT modify gameplay systems outside map authoring scope.

---

# 9. Expected Deliverables

Expected implementation may include:

- MapEditorBehaviour
- Tile visual refresh helpers
- Runtime-safe node generation workflow
- Map cleanup workflow
- Updated map prefab workflow

Existing systems should be reused whenever possible.

---

# 10. Notes

This task focuses on:

```text
Handcrafted Map Prefab Workflow
```

NOT:

```text
Procedural Runtime Map Generation
```

The purpose of this task is to support fast handcrafted level production for the first playable version of Tower Nexus.
