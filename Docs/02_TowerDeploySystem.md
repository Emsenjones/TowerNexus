# Tower Nexus - Tower Deployment System Design Document

The system is responsible for:

- Managing player level progression
- Managing experience accumulation
- Triggering tower draft selection
- Managing Battle HUD level and EXP display
- Managing Tower Pending Deployment Area
- Managing tower deployment workflow
- Managing tower footprint occupation
- Managing tower placement validation
- Updating map node walkability
- Reserving future support for path blocking validation
- Supporting future tower recycle and redeployment workflow


## 2.1 Draft-Based Tower Progression

Instead:

- Players gain experience during gameplay
- Players level up
- Level-ups trigger randomized tower draft selections
- Players choose one tower from multiple options
- The selected tower is added to the Tower Pending Deployment Area
- Players manually drag pending towers from the pending area onto the map


# 4. Tower Draft System

Each level-up triggers a new draft selection. After the player chooses one tower, the tower is added to the Tower Pending Deployment Area instead of entering placement mode immediately.


## 4.2 Draft Workflow

The draft workflow is:

1. Player gains enough EXP
2. Player levels up
3. Draft UI opens
4. System generates 3 tower choices
5. Player selects one tower
6. Draft UI closes
7. Selected tower is added to the Tower Pending Deployment Area

The draft UI should temporarily block normal gameplay interaction while it is open.

---

## 4.3 Tower Draft Window Structure

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

## 4.4 Battle HUD Structure

The Battle HUD is the always-visible UI shown during tower defense gameplay.

The first version should include:

| UI Element | Description |
|---|---|
| Current Level Text | Displays the current player level |
| EXP Progress Bar | Displays current EXP progress toward the next level |
| Tower Pending Deployment Area | Stores towers that have been selected from draft but not yet deployed |

Future systems may add more always-visible battle information into the Battle HUD.


## 6.1 Placement Flow

The placement workflow is:

1. Player selects a tower from the Tower Pending Deployment Area
2. Placement preview object is created
3. Tower preview follows cursor or touch position
4. Center Anchor snaps to nearest GridNode
5. Placement validity is continuously evaluated
6. Player releases input
7. System validates final placement
8. If placement is valid, the tower is deployed onto the map
9. If placement is invalid, the tower returns to the Tower Pending Deployment Area


## 7.1 Validation Rules

A tower can only be deployed if:

1. All occupied anchors can find corresponding GridNodes
2. All corresponding GridNodes are walkable


## 7.3 Path Blocking Validation (Future)

Tower placement should eventually prevent players from completely blocking all valid monster paths.

However, path blocking validation is not required for the first implementation of the Tower Deployment System.

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


# 8. Runtime Occupancy

When occupancy changes:

- GridNode walkability updates
- Map visuals refresh
- Future pathfinding data updates


# 10. Runtime State Management

| State | Description |
|---|---|
| Normal | Standard gameplay |
| DraftSelection | Draft UI active |
| PendingDeployment | At least one tower is stored in the Tower Pending Deployment Area |
| TowerDragging | Tower placement preview active |
| PlacementValidation | Placement legality checking |
| TowerPlaced | Deployment completed |


# 11. First Version Scope

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

Excluded features:

- Tower upgrading
- Weighted draft system
- Tower rarity
- Tower recycle system
- Redeployment inventory
- Tower rotation
- Runtime pathfinding-based placement validation
- Multiplayer synchronization


# Change Log

## 2026-05-13

- Added Battle HUD requirements for current level, EXP progress, and pending tower display.
- Updated Tower Draft flow so selected towers enter the Tower Pending Deployment Area before map placement.
- Updated placement flow to start from the Tower Pending Deployment Area.
- Clarified that path blocking validation is reserved for future implementation and is not part of the first deployment loop.
