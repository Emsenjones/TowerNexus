# Task 004 - Tower Drag and Snap Placement

---

# 1. Task Overview

This task implements the drag placement and grid snapping workflow for the Tower Deploy System.

The system is responsible for:

- Selecting a pending tower from the Tower Pending Deployment Area
- Creating a runtime tower preview object
- Dragging the preview object using cursor or touch input
- Snapping the tower preview to GridNodes
- Updating preview valid/invalid visual state
- Providing placement preview runtime behavior

This task focuses only on placement preview interaction.

This task does not implement final deployment validation, GridNode walkability updates, or actual tower deployment.

---

# 2. Related System Document

Reference document:

```text
Docs/02_TowerDeploySystem.md
```

Relevant sections:

- `# 7. Tower Pending Deployment Area`
- `# 8. Tower Placement Workflow`
- `# 9. Placement Validation`
- `# 12. Runtime State Management`
- `# 13. First Version Scope`

---

# 3. Goals

The goal of this task is to create the first interactive tower placement preview workflow.

After this task is completed:

- The player can select a pending tower
- A tower preview object appears
- The tower preview follows cursor or touch input
- The preview snaps to GridNodes
- The preview displays valid/invalid placement feedback
- The system exposes runtime preview data for future placement validation tasks

---

# 4. Implementation Scope

This task includes:

- Pending tower drag interaction
- Runtime tower preview creation
- Placement preview movement
- Grid snapping
- Preview state updates
- Placement preview visual feedback
- Runtime placement state management
- Basic map query integration

This task excludes:

- Final tower deployment
- GridNode walkability updates
- Occupied anchor validation
- Pathfinding validation
- Formal tower spawning
- Tower combat logic
- Tower recycle system
- Save/load system

---

# 5. Suggested Runtime Responsibilities

This task should provide the following runtime responsibilities:

- Pending tower placement start flow
- Runtime placement preview creation and cleanup
- Placement input tracking
- Screen-to-world position conversion
- GridNode snapping integration
- Placement preview state updates
- Preview valid/invalid visual feedback
- Runtime placement state exposure for future deployment validation

Codex should first inspect the existing project structure before implementation.

The implementation may:

- extend existing placement or UI scripts
- reuse existing map query systems
- create new scripts if necessary

Avoid creating duplicate placement controllers, input handlers, or preview systems if equivalent responsibilities already exist.

Recommended script names:

```text
TowerPlacementController
TowerPlacementPreview
```

These names are recommendations only.
If a different architecture fits the existing project better, explain the reasoning before implementation.

---

# 6. Runtime Placement Workflow

The first version placement workflow is:

1. Player selects or drags a tower from the Tower Pending Deployment Area
2. A runtime placement preview object is created
3. The preview follows cursor or touch position
4. The preview snaps to nearest GridNode
5. Placement validity state is updated continuously
6. Player releases input
7. Placement preview state is exposed for future deployment validation

This task does not deploy towers onto the battlefield.

---

# 7. TowerPlacementController

## 7.1 Overview

`TowerPlacementController` manages:

- Current dragging state
- Current preview object
- Input tracking
- Grid snapping
- Placement preview updates

Only one placement preview may exist at a time.

---

## 7.2 Required Fields

| Field | Type | Description |
|---|---|---|
| currentPreview | TowerPlacementPreview | Current active placement preview |
| currentTowerDefinition | TowerDefinition | Tower currently being previewed |
| currentTargetNode | GridNode | Current snapped GridNode |
| isDragging | bool | Whether tower dragging is active |
| placementCamera | Camera | Camera used for screen-to-world conversion |

---

## 7.3 Required Runtime Capability

The implementation must provide a way to:

- begin placement preview from a tower definition
- cancel current placement preview safely
- prevent multiple active previews
- expose current placement preview state for future systems

Recommended API:

```csharp
public void BeginPlacement(TowerDefinition towerDefinition)
public void CancelPlacement()
```

Expected behavior:

### BeginPlacement

- Cancels existing placement if already dragging
- Validates tower definition
- Instantiates or prepares placement preview object
- Enters dragging state

### CancelPlacement

- Destroys or hides current preview object
- Clears runtime references
- Exits dragging state

Equivalent implementations are acceptable if they better match the existing project architecture.

---

# 8. Input Handling

## 8.1 First Version Input Rules

The first version only needs basic mouse support.

Recommended behavior:

| Input | Result |
|---|---|
| Left Mouse Hold | Drag preview |
| Left Mouse Release | End preview state |
| Right Mouse Click | Cancel placement |

Touch support may be added later.

---

## 8.2 World Position Conversion

The placement system should convert screen position into world position.

Recommended workflow:

```text
Mouse Position
    ↓
Raycast To Ground Plane
    ↓
World Position
    ↓
GridNode Query
```

The implementation should avoid physics overlap for GridNode lookup.

---

# 9. GridNode Query Integration

The placement system should integrate with the Map System.

The implementation must integrate with existing map query behavior if available.

Recommended query behavior:

```csharp
bool TryGetNodeByWorldPosition(
    Vector3 worldPosition,
    out GridNode gridNode
)
```

Expected behavior:

- Returns nearest valid GridNode
- Returns false if no GridNode exists
- Used continuously during placement preview updates

This task assumes the Map System already provides runtime node query support.

Do not modify the Map System in this task unless required for integration.

---

# 10. Tower Placement Preview

## 10.1 Overview

The placement preview object is a temporary runtime object.

It is not the final deployed tower.

The preview object exists only during dragging.

---

## 10.2 Preview Responsibilities

The preview object is responsible for:

- Visual display
- Snap positioning
- Valid/invalid visual feedback
- Anchor reference access

The preview object is not responsible for:

- Final deployment
- Walkability updates
- Placement validation rules
- Grid occupancy updates

---

## 10.3 Required Fields

`TowerPlacementPreview` should contain:

| Field | Type | Description |
|---|---|---|
| towerDefinition | TowerDefinition | Current tower definition |
| towerAnchorSet | TowerAnchorSet | Anchor references from prefab |
| isPlacementValid | bool | Current preview validity state |

---

# 10.4 Required Runtime Capability

The implementation must provide a way to:

- initialize preview state from tower data
- move preview object in world space
- keep the tower center anchor aligned with the snapped GridNode
- update visual state based on placement validity

Recommended API:

```csharp
public void Initialize(TowerDefinition towerDefinition)
public void SetWorldPosition(Vector3 position)
public void SetPlacementState(bool isValid)
```

Expected behavior:

### Initialize

- Validates tower definition
- Finds or uses anchor references
- Initializes runtime state

### SetWorldPosition

- Moves preview object in world space
- Keeps CenterAnchor aligned to snapped GridNode

### SetPlacementState

- Updates preview valid/invalid visual feedback

Equivalent implementations are acceptable if they better match the existing project architecture.
# 15. Implementation Planning Requirement

Before implementation, Codex should:

1. Inspect the current project structure
2. Identify existing placement, input, UI, tower, and map query systems
3. Decide whether to:
   - extend existing scripts
   - create new scripts
   - refactor small existing structures
4. Explain the implementation plan before writing code

The implementation plan should include:

- scripts expected to change
- new scripts expected to be created
- responsibilities of each modified script
- reasoning for any newly created runtime systems
- how the placement preview will integrate with existing map query behavior

Do not start implementation before presenting the plan.

---

# 11. Preview Visual Feedback

## 11.1 Overview

The preview object should display placement state visually.

The first version only requires simple visual feedback.

Recommended examples:

| State | Visual |
|---|---|
| Valid | Green tint |
| Invalid | Red tint |

---

## 11.2 First Version Validity Rules

The first version only needs simple preview validation.

Preview is considered valid if:

- A GridNode can be found under the placement position

Preview is considered invalid if:

- No valid GridNode can be found

The first version does not validate:

- Occupied anchors
- Walkability
- Path blocking
- Tower overlap

Those systems belong to Task 005.

---

# 12. Runtime State Rules

The placement system should follow these rules:

- Only one preview object may exist at a time
- Placement preview only exists during dragging
- Draft Window cannot remain open during placement
- Cancelling placement destroys preview object
- Releasing input exits placement preview state
- Placement preview does not modify gameplay data

---

# 13. Validation Rules

The implementation must handle:

- Null TowerDefinition
- Missing tower prefab
- Missing TowerAnchorSet
- Missing CenterAnchor
- Invalid GridNode query result
- Missing camera reference
- Invalid world position conversion

If data is invalid:

- Do not crash
- Log warnings where appropriate
- Prevent invalid preview behavior

---

# 14. Integration Notes

This task integrates with:

- BattleHUDUI
- PendingTowerItemUI
- TowerDefinition
- TowerAnchorSet
- MapSystem

However:

- Do not deploy final towers
- Do not update GridNode walkability
- Do not validate occupied anchors
- Do not validate pathfinding
- Do not remove pending tower entries

This task only prepares runtime placement interaction.

---

# 16. Acceptance Criteria

This task is complete when:

- A pending tower can begin placement preview
- Placement preview follows cursor or touch input
- Preview snaps to GridNodes
- Preview updates valid/invalid visual state
- Preview cancels correctly
- Only one preview exists at a time
- Releasing input exits placement preview state
- Unity Console has no compile errors
- No final deployment logic is implemented

---

# 17. Testing Checklist

Use temporary runtime test setup to validate:

1. Begin placement
    - Preview object appears correctly

2. Drag preview
    - Preview follows input correctly

3. Snap preview
    - Preview snaps to nearest GridNode

4. Invalid position
    - Preview becomes invalid visually

5. Release input
    - Preview exits correctly

6. Cancel placement
    - Preview is destroyed correctly

7. Multiple placement attempts
    - Only one preview exists

8. Missing prefab
    - No crash
    - Warning logged

9. Missing anchor set
    - No crash
    - Warning logged

10. Missing camera
    - No crash
    - Warning logged

---

# 18. Out of Scope

Do not implement:

- Final tower deployment
- Walkability update
- Occupied anchor validation
- Pathfinding validation
- Tower overlap detection
- Tower recycle system
- Tower combat logic
- Save/load system
- Multiplayer synchronization

---

# Change Log

## 2026-05-13
- Initial Tower Drag and Snap Placement task document created.
- Defined placement preview workflow.
- Defined runtime dragging behavior.
- Defined GridNode snapping workflow.
- Defined placement preview visual feedback.
- Reserved occupied anchor validation for future tasks.
- Reserved final deployment workflow for future tasks.
- Refactored task structure to support architecture-driven AI workflow.