# Task 005 - Tower Placement Validation and Walkable Update

---

# 1. Task Overview

This task implements the final placement validation and runtime walkability update workflow for the Tower Deploy System.

This task turns the placement preview created in Task 004 into an actual deployed tower.

The system is responsible for:

- Validating occupied anchors
- Finding all GridNodes occupied by the selected tower
- Checking GridNode walkability
- Confirming final placement on input release
- Spawning the final tower instance
- Updating occupied GridNodes to unwalkable
- Removing the deployed tower from the Tower Draft Area
- Returning the tower to the Tower Draft Area if placement fails
- Preparing future support for tower removal and redeployment

This task does not implement pathfinding-based placement validation.

---

# 2. Related System Document

Reference document:

```text
Docs/02_TowerDeploySystem.md
```

Relevant sections:

- `# 8. Tower Placement Workflow`
- `# 9. Placement Validation`
- `# 10. Runtime Occupancy`
- `# 11. Future Tower Recycle System`
- `# 13. First Version Scope`

---

# 3. Goals

The goal of this task is to complete the first playable Tower Deploy loop.

After this task is completed:

- The player can drag a drafted tower onto the map
- The system can validate all occupied anchors
- The system can check whether all occupied GridNodes are walkable
- A valid tower placement creates a final tower instance
- Occupied GridNodes become unwalkable
- The drafted tower entry is removed after successful deployment
- Invalid placement cancels deployment and keeps the drafted tower available
- Future tower removal and redeployment can reuse occupied node data

---

# 4. Implementation Scope

This task includes:

- Final placement validation
- Occupied anchor to GridNode resolution
- Walkability validation
- Final tower instance spawning
- GridNode walkability update
- Drafted tower entry removal on success
- Placement failure handling
- Tower instance occupied node cache
- Future path blocking validation hook

This task excludes:

- Runtime pathfinding validation
- Monster Spawn Node and Target Node configuration
- Tower recycling UI
- Tower redeployment from recycle area
- Tower combat behavior
- Tower upgrade behavior
- Save/load system
- Multiplayer synchronization

---

# 5. Suggested Runtime Responsibilities

This task should provide the following runtime responsibilities:

- Final placement validation
- Occupied anchor to GridNode resolution
- Walkability validation
- Final tower deployment execution
- Runtime deployed tower data storage
- GridNode walkability state update
- Drafted tower entry removal on successful deployment
- Placement failure handling
- Runtime occupied node caching for future recycle systems
- Future path blocking validation hook support

Codex should first inspect the existing project structure before implementation.

The implementation may:

- extend existing placement or deployment systems
- reuse existing map query systems
- reuse existing runtime tower systems
- create new scripts if necessary

Avoid creating duplicate deployment controllers, validators, runtime tower data systems, or GridNode update handlers if equivalent responsibilities already exist.

Recommended script names:

```text
TowerPlacementValidator
TowerDeployController
TowerInstance
```

These names are recommendations only.
If a different architecture fits the existing project better, explain the reasoning before implementation.

---

# 6. Runtime Deployment Workflow

The final deployment workflow is:

1. Player drags a drafted tower from the Tower Draft Area
2. Tower placement preview snaps to a GridNode
3. Player releases input
4. System resolves all occupied anchors to GridNodes
5. System validates all occupied GridNodes
6. If validation succeeds:
    - Spawn final tower instance
    - Mark occupied GridNodes as unwalkable
    - Remove drafted tower entry
    - Destroy placement preview
7. If validation fails:
    - Destroy placement preview
    - Keep tower in Tower Draft Area

This completes the first playable deployment loop.

---

# 7. TowerPlacementValidator

## 7.1 Overview

`TowerPlacementValidator` is responsible for gameplay placement rules.

It should be separated from input and preview logic.

It validates whether a tower can be placed at the current preview position.

---

## 7.2 Required Runtime Capability

The implementation must provide a way to:

- resolve occupied anchors into GridNodes
- validate runtime tower placement
- prevent duplicate occupied node references
- validate GridNode walkability
- expose runtime-safe placement validation behavior

Recommended API:

```csharp
public bool TryGetOccupiedNodes(
    TowerPlacementPreview preview,
    out List<GridNode> occupiedNodes
)

public bool CanPlaceTower(
    TowerPlacementPreview preview,
    out List<GridNode> occupiedNodes
)
```

Expected behavior:

### TryGetOccupiedNodes

- Reads occupied anchors from the preview's `TowerAnchorSet`
- Converts each occupied anchor world position to a GridNode
- Returns all corresponding GridNodes
- Returns false if any anchor cannot find a valid GridNode
- Prevents duplicate GridNodes in the returned list

### CanPlaceTower

- Calls `TryGetOccupiedNodes`
- Checks whether each occupied GridNode is walkable
- Calls future path blocking validation hook
- Returns true only if all checks pass

Equivalent implementations are acceptable if they better match the existing project architecture.

---

## 7.3 Future Path Blocking Hook

The implementation should provide a placeholder hook for future pathfinding validation.

Recommended API:

```csharp
private bool ValidatePathBlocking(List<GridNode> occupiedNodes)
```

Expected behavior in this task:

```csharp
return true;
```

Notes:

- Do not implement actual pathfinding validation in this task
- Do not add Monster Spawn Node or Target Node logic
- This method is reserved for future Pathfinding System integration

---

# 8. TowerDeployController

## 8.1 Overview

`TowerDeployController` is responsible for final deployment execution.

It should be separate from:

- Input tracking
- Preview movement
- Placement visual feedback

---

## 8.2 Required Fields

| Field | Type | Description |
|---|---|---|
| placementValidator | TowerPlacementValidator | Validates final placement |
| deployedTowerRoot | Transform | Parent transform for deployed tower instances |

---

## 8.3 Required Runtime Capability

The implementation must provide a way to:

- execute final tower deployment
- validate deployment before tower creation
- update occupied GridNode walkability state
- create or initialize runtime tower instance data
- remove drafted tower entries only after successful deployment

Recommended API:

```csharp
public bool TryDeployTower(
    TowerPlacementPreview preview,
    TowerDraftItemUI draftItem
)
```

Expected behavior:

1. Validate input references
2. Use `TowerPlacementValidator.CanPlaceTower`
3. If validation fails:
    - Return false
    - Do not modify GridNodes
    - Do not remove drafted item
4. If validation succeeds:
    - Instantiate final tower prefab
    - Align final tower to preview position
    - Add or initialize `TowerInstance`
    - Store occupied GridNodes in `TowerInstance`
    - Mark occupied GridNodes as unwalkable
    - Remove drafted tower item from the Tower Draft System
    - Return true

Equivalent implementations are acceptable if they better match the existing project architecture.

---

# 9. TowerInstance

## 9.1 Overview

`TowerInstance` represents a deployed tower on the map.

It stores runtime deployment data required for future tower removal, recycling, and redeployment.

---

## 9.2 Required Fields

| Field | Type | Description |
|---|---|---|
| towerDefinition | TowerDefinition | Definition data of this deployed tower |
| occupiedNodes | List<GridNode> | GridNodes occupied by this tower |

---

## 9.3 Required Runtime Capability

The implementation must provide a way to:

- initialize runtime tower deployment data
- store occupied GridNode references safely
- expose occupied node data for future recycle systems

Recommended API:

```csharp
public void Initialize(
    TowerDefinition towerDefinition,
    List<GridNode> occupiedNodes
)

public IReadOnlyList<GridNode> GetOccupiedNodes()
```

Expected behavior:

### Initialize

- Stores tower definition
- Stores occupied node list
- Prevents null occupied node entries if possible

### GetOccupiedNodes

- Returns read-only occupied node list
- Used by future removal and recycle systems

Equivalent implementations are acceptable if they better match the existing project architecture.

---

# 10. GridNode Walkability Update

## 10.1 Deployment Update

When tower placement succeeds:

```csharp
gridNode.isWalkable = false;
```

Apply this update to every occupied GridNode.

---

## 10.2 Future Removal Update

Do not implement removal in this task.

However, the design should support future logic:

```csharp
gridNode.isWalkable = true;
```

This will be used when towers are recycled or redeployed.

---

# 11. Integration With Task 004

Task 004 created preview interaction.

This task should connect final release behavior to deployment.

Expected integration:

- `TowerPlacementController` detects input release
- It passes current preview and drafted item to `TowerDeployController`
- `TowerDeployController` attempts final deployment
- If successful:
    - Drafted item is removed
    - Preview is destroyed
- If failed:
    - Drafted item remains
    - Preview is destroyed
    - Player may drag the drafted tower again

Do not make `TowerPlacementController` directly update GridNode walkability.

---

# 12. Drafted Tower Item Handling

Drafted tower entries should only be removed after successful deployment.

Rules:

| Case | Result |
|---|---|
| Valid deployment | Remove drafted tower entry |
| Invalid deployment | Keep drafted tower entry |
| Cancel placement | Keep drafted tower entry |

This ensures failed placement does not consume the player's drafted tower.

---

# 13. Placement Preview State Update

After this task, preview validity should use real placement validation instead of only checking whether a GridNode exists.

Preview valid state should be true only if:

- All occupied anchors resolve to GridNodes
- All occupied GridNodes are walkable
- Future path blocking hook returns true

Preview invalid state should be true if any of the above checks fail.

---

# 14. Runtime State Rules

The deployment system should follow these rules:

- Only deployed towers modify GridNode walkability
- Preview towers never modify GridNode walkability
- Drafted tower entries are removed only after successful deployment
- Invalid placement does not consume drafted tower entries
- Deployment validation should be executed again on input release
- Successful deployment destroys the preview object
- Failed deployment destroys the preview object and keeps the drafted item
- Pathfinding validation remains disabled in this task

---

# 15. Validation Rules

## 15.1 Integration With TowerDraftSystem

This task assumes drafted towers are managed by the runtime `TowerDraftSystem`.

Expected integration behavior:

- Tower draft entries are generated by the Tower Draft workflow
- Dragging begins from a `TowerDraftItemUI`
- Successful deployment removes the drafted tower entry from the draft system
- Failed deployment keeps the drafted tower entry available
- Deployment logic should not directly manage draft generation logic

The deployment system should only consume drafted tower entries after successful placement.

---

# 16. Integration Notes

This task integrates with:

- TowerPlacementController
- TowerPlacementPreview
- TowerPlacementValidator
- TowerDeployController
- TowerInstance
- TowerDraftSystem
- TowerDraftItemUI
- TowerDefinition
- TowerAnchorSet
- MapSystem
- GridNode

However:

- Do not implement pathfinding validation
- Do not implement monster spawn or target nodes
- Do not implement tower recycle UI
- Do not implement tower combat
- Do not implement tower upgrade

---

# 17. Implementation Planning Requirement

Before implementation, Codex should:

1. Inspect the current project structure
2. Identify existing placement, deployment, runtime tower, and map query systems
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
- how deployment validation integrates with existing placement systems
- how occupied node state is stored for future recycle support

Do not start implementation before presenting the plan.

---

# 18. Acceptance Criteria

This task is complete when:

- Placement validation checks all occupied anchors
- All occupied anchors resolve to GridNodes
- Duplicate occupied GridNodes are handled safely
- All occupied GridNodes must be walkable
- Valid placement spawns final tower instance
- Valid placement marks occupied GridNodes as unwalkable
- Valid placement removes drafted tower entry
- Invalid placement does not modify GridNodes
- Invalid placement keeps drafted tower entry
- Preview object is destroyed after release
- `TowerInstance` stores occupied node data
- Path blocking validation exists only as a placeholder
- Unity Console has no compile errors

---

# 19. Testing Checklist

Use temporary runtime setup to validate:

1. Valid placement
    - Tower is spawned
    - Occupied GridNodes become unwalkable
    - Drafted item is removed

2. Invalid placement outside map
    - Tower is not spawned
    - Drafted item remains
    - No GridNodes are modified

3. Invalid placement on unwalkable node
    - Tower is not spawned
    - Drafted item remains

4. Multi-anchor tower placement
    - All occupied nodes are detected
    - All occupied nodes become unwalkable

5. Duplicate anchor node case
    - Duplicate nodes do not cause repeated errors

6. Missing tower prefab
    - No crash
    - Warning logged

7. Missing anchor set
    - No crash
    - Warning logged

8. Missing occupied anchor
    - No crash
    - Deployment fails

9. Cancel placement
    - Drafted item remains
    - No GridNodes are modified

10. Multiple placements
    - Previously occupied nodes prevent overlap placement

---

# 20. Out of Scope

Do not implement:

- Runtime pathfinding validation
- Monster Spawn Node
- Monster Target Node
- Tower recycle UI
- Tower redeployment storage
- Tower combat behavior
- Tower upgrade behavior
- Tower rotation
- Save/load system
- Multiplayer synchronization

---

# Change Log

## 2026-05-13

- Initial Tower Placement Validation and Walkable Update task document created.
- Defined occupied anchor validation workflow.
- Defined GridNode walkability validation workflow.
- Defined final tower deployment workflow.
- Defined TowerInstance runtime data.
- Defined pending tower item removal rules.
- Added placeholder future path blocking validation hook.
- Refactored task structure to support architecture-driven AI workflow.

