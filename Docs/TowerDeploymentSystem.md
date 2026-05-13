# Tower Nexus - Tower Deployment System Design Document

---

# 1. System Overview

The Tower Deployment System is one of the core gameplay systems in Tower Nexus.

The system is responsible for:

- Managing player level progression
- Managing experience accumulation
- Triggering tower draft selection
- Managing tower deployment workflow
- Managing tower footprint occupation
- Managing tower placement validation
- Updating map node walkability
- Preventing invalid path blocking
- Supporting future tower recycle and redeployment workflow

The Tower Deployment System works closely with:

- Map System
- Pathfinding System
- Combat System
- Future Tower Upgrade System

The system is designed around dynamic battlefield manipulation.

Players continuously reshape the battlefield by deploying towers that alter monster movement paths.

---

# 2. Core Design Philosophy

The Tower Deployment System is built around the following principles:

## 2.1 Draft-Based Tower Progression

Players do not freely build towers from a static build menu.

Instead:

- Players gain experience during gameplay
- Players level up
- Level-ups trigger randomized tower draft selections
- Players choose one tower from multiple options

This creates:

- Build variety
- Replayability
- Strategic adaptation

---

## 2.2 Battlefield Manipulation

Towers are not only combat units.

Towers also function as:

- Terrain modifiers
- Path blockers
- Spatial control tools

Tower placement directly changes monster movement routes.

This creates gameplay where:

- Positioning matters
- Space management matters
- Path manipulation becomes part of combat strategy

---

## 2.3 Runtime Dynamic Deployment

The battlefield is continuously modified during gameplay.

During runtime:

- Towers may be deployed
- Towers may be removed
- Walkable nodes may change
- Monster paths may update dynamically

The deployment system must support fast and stable runtime updates.

---

## 2.4 Grid-Aligned Placement

Tower placement is fully grid-aligned.

All tower placement logic is based on:

- Grid Nodes
- Tower footprint anchors
- Grid snapping

This ensures:

- Consistent placement
- Predictable pathfinding
- Stable occupancy updates
- Simplified runtime validation

---

# 3. Player Level System

The Player Level System controls player progression during gameplay.

Players gain experience by eliminating monsters.

When accumulated experience reaches the required threshold:

- The player levels up
- The Tower Draft UI is opened

---

## 3.1 Experience Configuration

The first implementation uses Unity-based configuration instead of external configuration tables.

Recommended implementation methods:

- ScriptableObject
- Serialized Inspector configuration

Example structure:

```csharp
PlayerLevelConfig
{
    List<int> expRequiredPerLevel;
}
```

Example:

| Level | Required EXP |
|---|---|
| 1 → 2 | 10 |
| 2 → 3 | 20 |
| 3 → 4 | 40 |

---

## 3.2 Runtime Data

Example runtime data:

```csharp
PlayerLevelSystem
{
    int currentLevel;
    int currentExp;
}
```

Core functionality:

```csharp
AddExp(int amount);
TryLevelUp();
```

---

# 4. Tower Draft System

The Tower Draft System provides randomized tower choices during gameplay progression.

Each level-up triggers a new draft selection.

The first implementation uses:

- 3-choice tower draft
- Random selection from Tower Pool

---

## 4.1 Tower Pool

The Tower Pool defines which towers may appear in draft selections.

Example structure:

```csharp
TowerPool
{
    List<TowerDefinition> towerList;
}
```

Future versions may support:

- Weighted probability
- Rarity tiers
- Conditional unlocks
- Synergy-based draft influence

---

## 4.2 Draft Workflow

The draft workflow is:

1. Player gains enough EXP
2. Player levels up
3. Draft UI opens
4. System generates 3 tower choices
5. Player selects one tower
6. Selected tower enters placement mode

The draft UI should temporarily block normal gameplay interaction.

---

# 5. Tower Structure

Each tower is represented by:

- Tower configuration data
- Tower prefab
- Footprint anchor definitions

The tower footprint determines:

- Which Grid Nodes are occupied
- Which nodes become unwalkable
- Which shape the tower occupies on the battlefield

---

## 5.1 Tower Prefab Structure

Recommended prefab structure:

```text
TowerPrefab
├── VisualRoot
├── Collider
├── Anchors
│   ├── CenterAnchor
│   ├── OccupyAnchor_01
│   ├── OccupyAnchor_02
│   └── OccupyAnchor_03
└── TowerBehaviour
```

---

## 5.2 Center Anchor

Each tower contains one Center Anchor.

The Center Anchor is responsible for:

- Snap positioning
- Grid alignment
- Placement reference point

During placement:

- The Center Anchor snaps onto a target GridNode center position

---

## 5.3 Occupied Anchors

Occupied Anchors define which Grid Nodes the tower occupies.

Requirements:

- Anchor Y position should remain 0
- Anchor X/Z offsets should use integer grid offsets
- All occupied anchors must correspond to valid GridNodes

Example footprint:

```text
[X][X]
[ ][X]
```

Different towers may have different footprint shapes.

---

# 6. Tower Placement Workflow

The Tower Placement System handles drag placement and deployment validation.

---

## 6.1 Placement Flow

The placement workflow is:

1. Player selects tower from Draft UI
2. Placement preview object is created
3. Tower preview follows cursor or touch position
4. Center Anchor snaps to nearest GridNode
5. Placement validity is continuously evaluated
6. Player releases input
7. System validates final placement
8. Tower is either deployed or placement is rejected

---

## 6.2 Grid Snap Rules

The placement preview continuously snaps to the nearest GridNode.

Snap behavior uses:

- Center Anchor
- GridNode center position

The system should not rely on physics overlap for node detection.

Recommended workflow:

```text
World Position
    ↓
Grid Coordinate
    ↓
GridNode Lookup
```

---

## 6.3 Placement Preview

During placement:

- Valid placement displays valid visual feedback
- Invalid placement displays invalid visual feedback

Recommended examples:

| State | Visual |
|---|---|
| Valid | Green highlight |
| Invalid | Red highlight |

---

# 7. Placement Validation

Placement validation ensures gameplay integrity and prevents invalid tower deployment.

---

## 7.1 Validation Rules

A tower can only be deployed if:

1. All occupied anchors can find corresponding GridNodes
2. All corresponding GridNodes are walkable
3. Placement does not fully block monster paths

If any rule fails:

- Placement becomes invalid

---

## 7.2 Occupied Node Validation

Validation process:

1. Center Anchor snaps to target GridNode
2. System calculates occupied anchor positions
3. System finds corresponding GridNodes
4. System checks node walkability

If any occupied node is invalid:

- Placement fails

---

## 7.3 Path Blocking Validation

Tower placement must never completely block all valid monster paths.

Validation workflow:

1. Temporarily mark occupied nodes as unwalkable
2. Run pathfinding validation
3. Check whether valid path still exists
4. Restore temporary state
5. Return validation result

If no valid path exists:

- Placement fails

This prevents deadlock gameplay situations.

---

# 8. Runtime Occupancy

During gameplay:

- Towers may occupy nodes
- Towers may release nodes when removed

When occupancy changes:

- GridNode walkability updates
- Pathfinding data updates
- Map visuals refresh

The Tower Deployment System interacts with the Map System through runtime occupancy APIs.

---

# 9. Future Tower Recycle System

Future versions of the system will support tower recycling and redeployment.

Players may remove towers from the battlefield and place them into a recycle area UI.

The stored towers may later be dragged back onto the battlefield.

---

## 9.1 Planned Workflow

Future recycle workflow:

1. Player removes tower
2. Occupied nodes become walkable again
3. Tower enters recycle storage UI
4. Player may redeploy tower later

This allows dynamic battlefield restructuring during gameplay.

---

# 10. Runtime State Management

Recommended runtime states:

| State | Description |
|---|---|
| Normal | Standard gameplay |
| DraftSelection | Draft UI active |
| TowerDragging | Tower placement preview active |
| PlacementValidation | Placement legality checking |
| TowerPlaced | Deployment completed |

Explicit runtime states help prevent:

- Duplicate draft opening
- Invalid UI interaction
- Multiple simultaneous placements
- Deployment conflicts

---

# 11. First Version Scope

The first implementation focuses on the core deployment loop.

Included features:

- Player EXP gain
- Player level-up logic
- 3-choice tower draft
- Tower Pool
- Tower anchor structure
- Tower drag placement
- Grid snapping
- Placement validation
- Path blocking validation
- Runtime walkability updates
- Valid/invalid placement feedback

Excluded from first implementation:

- Tower upgrading
- Weighted draft system
- Tower rarity
- Tower recycle system
- Redeployment inventory
- Tower rotation
- Multiplayer synchronization

---

# 12. Future Expansion Possibilities

Potential future features include:

- Tower rarity system
- Weighted drafting
- Tower upgrade branching
- Tower evolution
- Redeployment inventory
- Tower rotation
- Dynamic footprint changes
- Tower synergy mechanics
- Runtime tower transformation
- Special placement restrictions

These features are not required for the first playable version.

---

# Change Log

## 2026-05-13

- Initial Tower Deployment System Design Document created.
- Defined player level and tower draft workflow.
- Defined tower footprint anchor structure.
- Defined tower drag placement and validation workflow.
- Defined path blocking validation rules.
- Defined future recycle and redeployment direction.