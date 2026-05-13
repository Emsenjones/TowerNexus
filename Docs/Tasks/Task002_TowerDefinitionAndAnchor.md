# Task 002 - Tower Definition and Anchor System

---

# 1. Task Overview

This task implements the tower data definition and footprint anchor structure for the Tower Deployment System.

The Tower Definition and Anchor System is responsible for:

- Defining tower basic data
- Defining tower prefab references
- Defining tower icon references
- Defining tower footprint anchors
- Defining tower center anchor
- Providing occupied anchor data for future placement validation

This task does not implement tower draft UI, tower dragging, tower placement, map walkability update, or pathfinding validation.

---

# 2. Related System Document

Reference document:

```text
Docs/02_TowerDeploymentSystem.md
```

Relevant sections:

- `# 5. Tower Structure`
- `# 6. Tower Placement Workflow`
- `# 7. Placement Validation`
- `# 11. First Version Scope`

---

# 3. Goals

The goal of this task is to create reusable tower definition data and prefab-side anchor structures.

After this task is completed:

- A tower can be defined as a ScriptableObject
- A tower can reference its runtime prefab
- A tower can reference its draft UI icon
- A tower prefab can expose a center anchor
- A tower prefab can expose multiple occupied anchors
- Future placement systems can query tower footprint anchor data

---

# 4. Implementation Scope

This task includes:

- `TowerDefinition`
- `TowerAnchorSet`
- Tower prefab anchor validation helpers
- Basic editor/runtime safety checks

This task excludes:

- Player Level System changes
- Tower Draft UI
- Tower Pool random selection
- Tower dragging
- Tower placement
- Grid snapping
- Map System changes
- Walkability updates
- Pathfinding validation
- Tower combat behavior

---

# 5. Required Scripts

Create the following scripts:

```text
Assets/Scripts/TowerDeployment/TowerDefinition/TowerDefinition.cs
Assets/Scripts/TowerDeployment/TowerDefinition/TowerAnchorSet.cs
```

If the project already has a different folder convention, follow the existing project structure while keeping the same logical separation.

---

# 6. TowerDefinition

## 6.1 Script Type

`TowerDefinition` should be implemented as a `ScriptableObject`.

Recommended asset menu path:

```csharp
[CreateAssetMenu(
    fileName = "TowerDefinition",
    menuName = "Tower Nexus/Tower Deployment/Tower Definition"
)]
```

---

## 6.2 Data Fields

| Field | Type | Description |
|---|---|---|
| towerId | string | Unique tower id used by gameplay systems |
| displayName | string | Display name shown in UI |
| description | string | Short description shown in UI |
| icon | Sprite | Icon used by Tower Draft UI |
| towerPrefab | GameObject | Runtime tower prefab used for placement and deployment |

---

## 6.3 Required Properties

Expose read-only public properties:

```csharp
public string TowerId { get; }
public string DisplayName { get; }
public string Description { get; }
public Sprite Icon { get; }
public GameObject TowerPrefab { get; }
```

---

## 6.4 Validation Rules

`TowerDefinition` should be considered valid only if:

- `towerId` is not null or empty
- `towerPrefab` is assigned
- `towerPrefab` has a `TowerAnchorSet` component
- `TowerAnchorSet` contains a valid center anchor
- `TowerAnchorSet` contains at least one occupied anchor

Implement:

```csharp
public bool IsValid()
```

Expected behavior:

- Return `true` if required data is valid
- Return `false` if required data is missing
- Log warning messages when validation fails

---

# 7. TowerAnchorSet

## 7.1 Script Type

`TowerAnchorSet` should be implemented as a `MonoBehaviour`.

It should be attached to each tower prefab.

Recommended prefab structure:

```text
TowerPrefab
├── VisualRoot
├── Anchors
│   ├── CenterAnchor
│   ├── OccupyAnchor_01
│   ├── OccupyAnchor_02
│   └── OccupyAnchor_03
└── TowerAnchorSet
```

---

## 7.2 Data Fields

| Field | Type | Description |
|---|---|---|
| centerAnchor | Transform | Anchor used as the snap reference point |
| occupiedAnchors | List<Transform> | Anchors used to define occupied GridNodes |

---

## 7.3 Required Properties

Expose read-only public properties:

```csharp
public Transform CenterAnchor { get; }
public IReadOnlyList<Transform> OccupiedAnchors { get; }
```

---

# 8. Anchor Rules

Tower anchors must follow these rules:

- The center anchor is required
- At least one occupied anchor is required
- Occupied anchors define the tower footprint
- Center anchor may also be included in occupied anchors
- Anchor local Y should normally be 0
- Anchor local X and local Z should normally be integer grid offsets
- Tower rotation is not supported in the first version

---

# 9. Anchor Validation

Implement the following method in `TowerAnchorSet`:

```csharp
public bool IsValid()
```

Expected behavior:

- Return `false` if `centerAnchor` is missing
- Return `false` if `occupiedAnchors` is null or empty
- Return `false` if any occupied anchor is null
- Return `true` if all required anchor data is valid
- Log warning messages when validation fails

---

## 9.1 Optional Anchor Offset Helper

Implement:

```csharp
public IReadOnlyList<Vector3> GetOccupiedAnchorLocalPositions()
```

Expected behavior:

- Return local positions of all occupied anchors
- Return an empty list if anchor data is invalid
- Do not modify anchor transforms

This method will be used by future placement validation tasks.

---

# 10. Runtime Usage Notes

This task only prepares data and anchor structure.

Future placement tasks will use this data to:

- Spawn tower preview objects
- Snap center anchor to GridNode
- Calculate occupied GridNodes
- Validate deployment legality
- Update GridNode walkability

Do not implement any of the above placement logic in this task.

---

# 11. Acceptance Criteria

This task is complete when:

- `TowerDefinition` can be created as a ScriptableObject asset
- `TowerDefinition` can reference tower id, display name, description, icon, and prefab
- `TowerDefinition.IsValid()` validates required tower data
- `TowerAnchorSet` can be attached to tower prefabs
- `TowerAnchorSet` exposes center anchor and occupied anchors
- `TowerAnchorSet.IsValid()` validates required anchor data
- `TowerAnchorSet.GetOccupiedAnchorLocalPositions()` returns occupied anchor local positions
- Unity Console has no compile errors
- No Tower Draft UI, tower placement, or map logic is implemented

---

# 12. Testing Checklist

Create a temporary tower prefab and test the following cases:

1. Valid tower definition
    - Has tower id
    - Has tower prefab
    - Tower prefab has `TowerAnchorSet`
    - `IsValid()` returns true

2. Missing tower id
    - `TowerDefinition.IsValid()` returns false
    - Warning is logged

3. Missing tower prefab
    - `TowerDefinition.IsValid()` returns false
    - Warning is logged

4. Tower prefab missing `TowerAnchorSet`
    - `TowerDefinition.IsValid()` returns false
    - Warning is logged

5. Missing center anchor
    - `TowerAnchorSet.IsValid()` returns false
    - Warning is logged

6. Empty occupied anchors
    - `TowerAnchorSet.IsValid()` returns false
    - Warning is logged

7. Occupied anchor contains null entry
    - `TowerAnchorSet.IsValid()` returns false
    - Warning is logged

8. Valid occupied anchors
    - `GetOccupiedAnchorLocalPositions()` returns correct local positions

---

# 13. Out of Scope

Do not implement:

- Tower Pool
- Tower Draft UI
- Tower random selection
- Tower dragging
- Tower placement preview
- Grid snapping
- Placement validation
- Map walkability update
- Pathfinding validation
- Tower combat behavior
- Tower upgrading
- Tower recycling

---

# Change Log

## 2026-05-13

- Initial Tower Definition and Anchor System task document created.
- Defined TowerDefinition ScriptableObject.
- Defined TowerAnchorSet prefab component.
- Defined tower center anchor and occupied anchor rules.
- Defined validation requirements.
- Defined first-version testing checklist.