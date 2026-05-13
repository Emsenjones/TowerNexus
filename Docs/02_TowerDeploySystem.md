# Tower Nexus - Tower Deploy System Design Document

---

# 1. System Overview

The Tower Deploy System is one of the core gameplay systems in Tower Nexus.

The system is responsible for:

- Managing player level progression
- Managing experience accumulation
- Triggering tower draft selection
- Managing Battle HUD level and EXP display
- Managing Tower Pending Deployment Area
- Managing tower deploy workflow
- Managing tower footprint occupation
- Managing tower placement validation
- Updating map node walkability
- Reserving future support for path blocking validation
- Supporting future tower recycle and redeployment workflow

The Tower Deploy System works closely with:

- Map System
- Future Pathfinding System
- Future Combat System
- Future Tower Upgrade System

The system is designed around dynamic battlefield manipulation.

Players continuously reshape the battlefield by deploying towers that occupy GridNodes and alter monster movement paths.

---

# 2. Core Design Philosophy

The Tower Deploy System is built around the following principles:

## 2.1 Draft-Based Tower Progression

Players do not freely build towers from a static build menu.

Instead:

- Players gain experience during gameplay
- Players level up
- Level-ups trigger randomized tower draft selections
- Players choose one tower from multiple options
- The selected tower is added to the Tower Pending Deployment Area
- Players manually drag pending towers from the pending area onto the map

This creates:

- Build variety
- Replayability
- Strategic adaptation
- A foundation for future tower inventory, recycle, and upgrade systems

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
- Towers may be removed in future versions
- Walkable nodes may change
- Monster paths may update dynamically in future versions

The deploy system must support fast and stable runtime updates.

---

## 2.4 Grid-Aligned Placement

Tower placement is fully grid-aligned.

All tower placement logic is based on:

- GridNodes
- Tower footprint anchors
- Grid snapping

This ensures:

- Consistent placement
- Predictable occupancy updates
- Stable validation
- Future pathfinding compatibility

---

# 3. Player Level System

The Player Level System controls player progression during gameplay.

Players gain experience by eliminating monsters or through other future gameplay rewards.

When accumulated experience reaches the required threshold:

- The player levels up
- The Tower Draft Window is opened

---

## 3.1 Experience Configuration

The first implementation uses Unity-based configuration instead of external configuration tables.

Recommended implementation methods:

- ScriptableObject
- Serialized Inspector configuration

Example structure:

```csharp
public class PlayerLevelConfig : ScriptableObject
{
    public List<int> expRequiredPerLevel;
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
public class PlayerLevelSystem : MonoBehaviour
{
    private int currentLevel;
    private int currentExp;
}
```

Core functionality:

```csharp
public void AddExp(int amount);
private void TryLevelUp();
```

The Player Level System should expose events for future UI and draft systems.

---

# 4. Battle HUD System

The Battle HUD is the always-visible UI shown during tower defense gameplay.

The first version should include:

| UI Element | Description |
|---|---|
| Current Level Text | Displays the current player level |
| EXP Progress Bar | Displays current EXP progress toward the next level |
| Tower Pending Deployment Area | Stores towers that have been selected from draft but not yet deployed |

The Battle HUD displays runtime information but should not own gameplay validation logic.

The Tower Pending Deployment Area stores deployable tower entries selected from the draft system. It is not responsible for placement validation, GridNode updates, or deployment legality checks.

Future systems may add more always-visible battle information into the Battle HUD.

---

# 5. Tower Draft System

The Tower Draft System provides randomized tower choices during gameplay progression.

Each level-up triggers a new draft selection.

After the player chooses one tower, the selected tower is added to the Tower Pending Deployment Area instead of entering placement mode immediately.

The first implementation uses:

- 3-choice tower draft
- Random selection from Tower Pool

---

## 5.1 Tower Pool

The Tower Pool defines which towers may appear in draft selections.

Example structure:

```csharp
public class TowerPool : ScriptableObject
{
    public List<TowerDefinition> towerList;
}
```

Future versions may support:

- Weighted probability
- Rarity tiers
- Conditional unlocks
- Synergy-based draft influence

---

## 5.2 Draft Workflow

The draft workflow is:

1. Player gains enough EXP
2. Player levels up
3. Tower Draft Window opens
4. System generates 3 tower choices from Tower Pool
5. Player selects one tower
6. Tower Draft Window closes
7. Selected tower is added to the Tower Pending Deployment Area

The Tower Draft Window should temporarily block normal gameplay interaction while it is open.

---

## 5.3 Tower Draft Window Structure

The Tower Draft Window is shown when the player levels up.

Recommended UI structure:

| UI Element | Description |
|---|---|
| Window Title | Displays the purpose of the window, such as Tower Draft |
| Tower Item Container | Holds 3 Tower Draft Items |
| Tower Draft Item | Represents one selectable tower option |
| Tower Icon | Displays the tower icon |
| Tower Name | Displays the tower name |
| Tower Description | Displays a short tower description |

When the player selects a Tower Draft Item:

- The selected tower is added to the Tower Pending Deployment Area
- The Tower Draft Window closes
- The player does not immediately enter placement mode

---

# 6. Tower Structure

Each tower is represented by:

- Tower configuration data
- Tower prefab
- Footprint anchor definitions

The tower footprint determines:

- Which GridNodes are occupied
- Which nodes become unwalkable
- Which shape the tower occupies on the battlefield

---

## 6.1 Tower Definition

A TowerDefinition represents the basic data required by draft and deploy systems.

Recommended fields:

| Field | Type | Description |
|---|---|---|
| towerId | string | Unique tower identifier |
| displayName | string | Display name shown in UI |
| description | string | Short description shown in UI |
| icon | Sprite | Icon used by Tower Draft UI and pending deployment UI |
| towerPrefab | GameObject | Runtime tower prefab used for placement and deployment |

The field list may be simplified later if some data is not needed in the first version.

---

## 6.2 Tower Prefab Structure

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
└── TowerAnchorSet
```

---

## 6.3 Center Anchor

Each tower contains one Center Anchor.

The Center Anchor is responsible for:

- Snap positioning
- Grid alignment
- Placement reference point

During placement:

- The Center Anchor snaps onto a target GridNode center position

---

## 6.4 Occupied Anchors

Occupied Anchors define which GridNodes the tower occupies.

Requirements:

- Anchor Y position should normally remain 0
- Anchor X/Z offsets should use integer grid offsets
- All occupied anchors must correspond to valid GridNodes during deployment
- Center Anchor may also be included in Occupied Anchors
- Tower rotation is not supported in the first version

Example footprint:

```text
[X][X]
[ ][X]
```

Different towers may have different footprint shapes.

---

# 7. Tower Pending Deployment Area

The Tower Pending Deployment Area is part of the Battle HUD.

It stores towers that have been selected from the Tower Draft Window but have not yet been deployed onto the map.

The Pending Deployment Area is responsible for:

- Displaying pending tower entries
- Allowing players to select or drag pending towers
- Removing a tower entry when it is successfully deployed
- Keeping a tower entry when placement fails

The Pending Deployment Area is not responsible for:

- Grid snapping
- Placement validation
- GridNode walkability updates
- Pathfinding validation
- Tower combat logic

---

# 8. Tower Placement Workflow

The Tower Placement System handles drag placement and deployment validation.

---

## 8.1 Placement Flow

The placement workflow is:

1. Player selects or drags a tower from the Tower Pending Deployment Area
2. Placement preview object is created
3. Tower preview follows cursor or touch position
4. Center Anchor snaps to nearest GridNode
5. Placement validity is continuously evaluated
6. Player releases input
7. System validates final placement
8. If placement is valid, the tower is deployed onto the map
9. If placement is invalid, the tower returns to the Tower Pending Deployment Area

---

## 8.2 Grid Snap Rules

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

## 8.3 Placement Preview

During placement:

- Valid placement displays valid visual feedback
- Invalid placement displays invalid visual feedback

Recommended examples:

| State | Visual |
|---|---|
| Valid | Green highlight |
| Invalid | Red highlight |

---

# 9. Placement Validation

Placement validation ensures gameplay integrity and prevents invalid tower deployment.

---

## 9.1 Validation Rules

A tower can only be deployed if:

1. All occupied anchors can find corresponding GridNodes
2. All corresponding GridNodes are walkable

If any rule fails:

- Placement becomes invalid

---

## 9.2 Occupied Node Validation

Validation process:

1. Center Anchor snaps to target GridNode
2. System calculates occupied anchor positions
3. System finds corresponding GridNodes
4. System checks node walkability

If any occupied node is invalid:

- Placement fails

---

## 9.3 Path Blocking Validation (Future)

Tower placement should eventually prevent players from completely blocking all valid monster paths.

However, path blocking validation is not required for the first implementation of the Tower Deploy System.

This feature depends on future Map System and Pathfinding System support, including:

- Monster Spawn Nodes
- Monster Target Nodes
- Runtime pathfinding query API
- Temporary walkability simulation

Planned validation workflow:

1. Temporarily mark occupied nodes as unwalkable
2. Run pathfinding validation from Monster Spawn Node to Monster Target Node
3. Check whether at least one valid path still exists
4. Restore temporary state
5. Return validation result

If no valid path exists:

- Placement fails

This prevents deadlock gameplay situations in future versions.

---

# 10. Runtime Occupancy

During gameplay:

- Towers may occupy nodes
- Towers may release nodes when removed in future versions

When occupancy changes:

- GridNode walkability updates
- Map visuals refresh
- Future pathfinding data updates

The Tower Deploy System interacts with the Map System through runtime occupancy APIs.

---

# 11. Future Tower Recycle System

Future versions of the system will support tower recycling and redeployment.

Players may remove towers from the battlefield and place them into a recycle area UI.

The stored towers may later be dragged back onto the battlefield.

---

## 11.1 Planned Workflow

Future recycle workflow:

1. Player removes tower
2. Occupied nodes become walkable again
3. Tower enters recycle storage UI
4. Player may redeploy tower later

This allows dynamic battlefield restructuring during gameplay.

---

# 12. Runtime State Management

Recommended runtime states:

| State | Description |
|---|---|
| Normal | Standard gameplay |
| DraftSelection | Draft UI active |
| PendingDeployment | At least one tower is stored in the Tower Pending Deployment Area |
| TowerDragging | Tower placement preview active |
| PlacementValidation | Placement legality checking |
| TowerPlaced | Deployment completed |

Explicit runtime states help prevent:

- Duplicate draft opening
- Invalid UI interaction
- Multiple simultaneous placements
- Deployment conflicts

---

# 13. First Version Scope

The first implementation focuses on the core deploy loop.

Included features:

- Player EXP gain
- Player level-up logic
- Battle HUD with current level and EXP progress display
- 3-choice tower draft
- Tower Pool
- Draft selection adds tower to Tower Pending Deployment Area
- Tower Pending Deployment Area
- Tower anchor structure
- Tower drag placement from pending deployment area
- Grid snapping
- Placement validation based on occupied anchors and walkable nodes
- Reserved path blocking validation design, without first-version implementation
- Runtime walkability updates
- Valid/invalid placement feedback

Excluded from first implementation:

- Tower upgrading
- Weighted draft system
- Tower rarity
- Tower recycle system
- Redeployment inventory
- Tower rotation
- Runtime pathfinding-based placement validation
- Multiplayer synchronization

---

# 14. Future Expansion Possibilities

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
- Runtime pathfinding-based placement validation

These features are not required for the first playable version.

---

# Change Log

## 2026-05-13

- Initial Tower Deploy System Design Document created.
- Defined player level and tower draft workflow.
- Defined Battle HUD requirements for current level, EXP progress, and pending tower display.
- Defined Tower Pending Deployment Area workflow.
- Defined tower footprint anchor structure.
- Defined tower drag placement and validation workflow.
- Clarified that path blocking validation is reserved for future implementation and is not part of the first deploy loop.
- Defined future recycle and redeployment direction.