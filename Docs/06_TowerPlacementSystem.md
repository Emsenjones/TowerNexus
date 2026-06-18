# Tower Nexus - Tower Placement System Design Document

---

# 1. System Overview

The Tower Placement System is one of the core runtime gameplay systems in Tower Nexus.

This system is responsible for converting a dragged Tower Draft item into either a placed battlefield tower or a target-intent request for an existing tower.

The Tower Placement System focuses only on tower placement-related responsibilities:

- Tower drag placement interaction
- Tower placement preview
- GridNode snapping
- Tower footprint validation
- Placement legality checking
- Runtime GridNode occupation
- Runtime walkability update
- Path-blocking validation integration with Monster System pathfinding
- Detecting whether a dragged Draft item targets a deployment tile or an existing tower
- Reserved support for future tower recycle and redeployment

The Tower Placement System is designed around dynamic battlefield manipulation.

Players continuously reshape the battlefield by placing towers that occupy GridNodes and alter monster movement paths.

---

# 2. Responsibility Boundary

The Tower Placement System should only own placement workflow and placement validation.

It should not own:

- Player level
- Player ResolvedMonsterCount progress
- Player HP
- Player death or battle failure logic
- Draft generation
- Draft result rules
- Tower level-up rules
- Tower upgrade application rules
- Draft Window UI
- Battle HUD layout
- Monster movement
- Monster damage logic
- Map data ownership
- Tower combat logic

Recommended ownership boundary:

| System | Owns |
|---|---|
| Player System | Level, ResolvedMonsterCount progress, HP, battle failure |
| Draft System | Draft generation and draft result workflow |
| Battle HUD UI System | Runtime UI display and interaction entry points |
| Tower Placement System | Tower placement, target intent detection, validation, occupation, and walkability update |
| Map System | GridNode data, node query, walkability state, and visual refresh |
| Monster System | Monster spawning, movement, path recalculation, death flow, arrival flow, and monster resolution reporting |

---

# 3. Core Design Philosophy

## 3.1 Placement as Runtime Battlefield Editing

Tower placement is not only a build action.

It is runtime battlefield editing.

When a tower is placed:

- The tower occupies one or more GridNodes.
- Occupied nodes become unwalkable.
- Monster movement paths may change.
- Future pathfinding systems may need to recalculate paths.

This creates gameplay where:

- Positioning matters.
- Space management matters.
- Path manipulation becomes part of combat strategy.

---

## 3.2 Grid-Aligned Placement

Tower placement is fully grid-aligned.

All placement logic is based on:

- GridNodes
- Tower center anchor
- Tower occupied anchors
- Grid snapping

This ensures:

- Consistent placement
- Predictable occupancy updates
- Stable validation
- Future pathfinding compatibility

---

## 3.3 Runtime Dynamic Placement

The battlefield may change during gameplay.

During runtime:

- Towers may be placed.
- Towers may be removed in future versions.
- Walkable nodes may change.
- Monster paths may update dynamically.

The Tower Placement System must support fast and stable runtime updates.

---

# 4. Related Runtime Systems

## 4.1 Player System

Player System owns player level, ResolvedMonsterCount progress, player HP, and battle failure conditions.

Tower Placement System may be reached indirectly after Player System triggers level-up progression and Draft System creates a draggable Draft item.

Tower Placement System should not read or modify player progression data directly.

---

## 4.2 Draft System

Draft System owns draft generation and draft result handling.

Draft System may generate Tower Draft and Tower Upgrade Draft results.

After the player selects a Tower Draft result, the selected tower becomes a draggable Tower Draft item.

Tower Placement System detects whether the Tower Draft item is dropped onto a valid deployment tile or onto an existing same-type tower.

Tower Placement System should not:

- Generate draft choices.
- Read tower pools for draft logic.
- Decide tower draft availability.
- Handle future draft type rules.
- Decide tower level-up rules.
- Apply tower upgrades.

---

## 4.3 Battle HUD UI System

Battle HUD UI System owns runtime battle UI display and interaction entry points.

For Tower Placement System, Battle HUD UI System provides:

- Draft Item Interaction Area display
- Draft item drag interaction entry
- Draft item removal after successful placement, accepted tower level-up, or accepted upgrade application
- Placement feedback display when requested by placement logic
- Valid target highlight presentation for Tower Upgrade Draft items

Battle HUD UI System should not:

- Validate tower placement.
- Update GridNode walkability.
- Decide placement legality.
- Own tower placement workflow.
- Validate tower level-up or tower upgrade rules.

---

## 4.4 Map System

Map System owns GridNode data, walkability state, node query APIs, runtime occupancy support, and map visual refresh.

Tower Placement System uses Map System to:

- Find target GridNodes.
- Query current walkability.
- Validate occupied nodes.
- Mark occupied nodes as unwalkable after successful placement.
- Restore nodes when future tower recycle or redeployment is implemented.

Map System should not own tower placement rules or placement workflow.

---

## 4.5 Monster System

Monster System owns monster spawning, movement, path recalculation, death flow, monster resolution reporting, and target arrival reporting.

Tower Placement System may affect Monster System indirectly by changing map walkability.

Tower Placement System should not directly control monster state, movement, or damage logic.

---

# 5. Tower Structure Reference

Tower structure ownership belongs to Tower Framework System.

Tower Placement System does not define the full TowerDefinition or the complete tower framework structure.

Instead, Tower Placement System only consumes the placement-related structure provided by Tower Framework System.

During placement, Tower Placement System reads:

- Tower prefab reference from TowerDefinition
- TowerAnchorSet from the tower prefab
- Center Anchor from TowerAnchorSet
- Occupied Anchors from TowerAnchorSet

The placement-related anchor data determines:

- Which GridNodes are occupied
- Which nodes become unwalkable
- Which shape the tower occupies on the battlefield
- Which point should snap to the target GridNode

---

## 5.1 Placement Anchor Usage

Tower Placement System uses placement anchors only for placement workflow and validation.

| Anchor Type | Placement Usage |
|---|---|
| Center Anchor | Used as the snap reference point |
| Occupied Anchors | Used to calculate occupied GridNodes |
| TowerAnchorSet | Provides anchor references to placement logic |

The full definition of TowerDefinition, TowerPrefab structure, Center Anchor, Occupied Anchors, and TowerAnchorSet belongs to Tower Framework System.

---

## 5.2 Footprint Calculation

During placement, the system calculates the tower footprint based on the target GridNode and the occupied anchor offsets.

Recommended workflow:

```text
Target GridNode
    ↓
Center Anchor Snap
    ↓
Occupied Anchor World Positions
    ↓
Corresponding GridNode Lookup
    ↓
Placement Validation
```

Different towers may have different footprint shapes, but Tower Placement System should treat all footprint data generically.

Tower Placement System should not hardcode tower-specific footprint rules.

---

# 6. Draft Item Interaction Area

The Draft Item Interaction Area belongs to Battle HUD UI System.

It stores Draft items that have been selected from Draft System but have not yet been consumed.

The Draft Item Interaction Area is responsible for:

- Displaying draggable Tower Draft and Tower Upgrade Draft items.
- Allowing players to select or drag Draft items.
- Removing a Draft item when placement, tower level-up, or upgrade application succeeds.
- Keeping a Draft item when placement, tower level-up, or upgrade application fails.

The Draft Item Interaction Area is not responsible for:

- Grid snapping.
- Placement validation.
- GridNode walkability updates.
- Pathfinding validation.
- Tower combat logic.
- Tower level-up validation.
- Tower upgrade validation.

Tower Placement System treats Draft items as placement or target-intent input.

---

# 7. Tower Placement Workflow

Tower Placement System handles drag placement and final placement validation.

---

## 7.1 Placement Flow

The placement workflow is:

1. Player selects or drags a Tower Draft item from the Draft Item Interaction Area.
2. Tower Placement System creates a placement preview object.
3. The preview follows cursor or touch position.
4. Center Anchor snaps to the nearest valid GridNode.
5. Placement validity is continuously evaluated.
6. Player releases input.
7. Tower Placement System validates final placement.
8. If placement is valid, the tower is placed onto the map.
9. Occupied GridNodes become unwalkable.
10. Battle HUD UI System removes the consumed Draft item.
11. If placement is invalid, the Draft item remains in the Draft Item Interaction Area.

---

## 7.2 Existing Tower Target Intent

When a Draft item is dragged onto an existing tower, Tower Placement System should detect the target intent and route it to TowerUpgradeSystem.

Tower Draft item on existing tower:

```text
Tower Draft Item
    ↓ Dropped On Existing Tower
TowerPlacementSystem
    ↓ Detect Same-Type Tower Target Intent
TowerUpgradeSystem
    ↓ Validate And Process Tower Level-Up Request
```

Tower Upgrade Draft item on existing tower:

```text
Tower Upgrade Draft Item
    ↓ Dropped On Existing Tower
TowerPlacementSystem
    ↓ Detect Target Tower Intent
TowerUpgradeSystem
    ↓ Validate And Apply Upgrade
```

Tower Placement System should not decide whether the target tower satisfies TowerType, TowerLevel, duplicate upgrade, or max-level rules.

---

## 7.3 Grid Snap Rules

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

## 7.3 Placement Preview

During placement:

- Valid placement displays valid visual feedback.
- Invalid placement displays invalid visual feedback.

Recommended examples:

| State | Visual |
|---|---|
| Valid | Green highlight |
| Invalid | Red highlight |

Placement preview is part of placement interaction.

Battle HUD UI System may display additional UI feedback, but validation ownership remains in Tower Placement System.

---

# 8. Placement Validation

Placement validation ensures gameplay integrity and prevents invalid tower placement.

---

## 8.1 Validation Rules

A tower can only be placed if:

1. All occupied anchors can find corresponding GridNodes.
2. All corresponding GridNodes are walkable.
3. Path-blocking validation passes if enabled by the current implementation stage.

If any rule fails:

- Placement becomes invalid.

---

## 8.2 Occupied Node Validation

Validation process:

1. Center Anchor snaps to target GridNode.
2. System calculates occupied anchor positions.
3. System finds corresponding GridNodes.
4. System checks node walkability.

If any occupied node is invalid:

- Placement fails.

---

## 8.3 Path Blocking Validation

Tower placement should prevent players from completely blocking all valid monster paths.

Path blocking validation is part of the intended placement rule set.

The validation depends on Monster System pathfinding functionality. If the current implementation stage has not enabled this validation inside the placement loop yet, the placement system should still keep its validation structure ready for this rule.

This feature depends on Map System data and Monster System pathfinding functionality, including:

- Monster Spawn Nodes
- Monster Target Node
- Monster System pathfinding queries
- Temporary walkability simulation

Recommended validation workflow:

1. Temporarily mark occupied nodes as unwalkable.
2. Request path validation using Monster System pathfinding functionality.
3. Check whether at least one valid path remains for every spawn path requirement.
4. Restore temporary state.
5. Return validation result.

If no valid path exists:

- Placement fails.

This prevents deadlock gameplay situations.

---

# 9. Runtime Occupancy

During gameplay:

- Towers may occupy nodes.
- Towers may release nodes when removed in future versions.

When occupancy changes:

- GridNode walkability updates.
- Map visuals refresh.
- Future pathfinding data updates.
- Monster paths may be recalculated.

Tower Placement System interacts with Map System through runtime occupancy APIs.

---

# 10. Runtime State Management

Recommended runtime states:

| State | Description |
|---|---|
| Normal | Standard gameplay |
| PendingPlacement | At least one Draft item is available in the Draft Item Interaction Area |
| TowerDragging | Tower placement preview active |
| PlacementValidation | Placement legality checking |
| TowerPlaced | Placement completed |

Explicit runtime states help prevent:

- Invalid UI interaction
- Multiple simultaneous placements
- Placement conflicts
- Duplicate placement confirmation

Draft selection state belongs to Draft System and Battle HUD UI System, not Tower Placement System.

---

# 11. Future Tower Recycle System

Future versions may support tower recycling and redeployment.

Players may remove towers from the battlefield and place them into a recycle or pending area UI.

The stored towers may later be dragged back onto the battlefield.

---

## 11.1 Planned Workflow

Future recycle workflow:

1. Player removes tower.
2. Occupied nodes become walkable again.
3. Tower enters recycle storage UI.
4. Player may redeploy tower later.

This allows dynamic battlefield restructuring during gameplay.

---

# 12. First Version Scope

The first implementation focuses on the core placement loop.

Included features:

- Use Draft items from Battle HUD UI System as placement or target-intent input.
- Tower anchor structure.
- Tower drag placement from Draft Item Interaction Area.
- Placement preview object.
- Grid snapping.
- Placement validation based on occupied anchors and walkable nodes.
- Path-blocking validation integration point, enabled when current runtime pathfinding support is connected.
- Runtime walkability updates.
- Valid / invalid placement feedback.
- Draft item removal after successful placement.
- Existing tower target-intent detection for Tower Draft and Tower Upgrade Draft items.

Excluded from first implementation:

- Draft generation.
- Draft Window UI.
- Player ResolvedMonsterCount / level-up logic.
- Player HP / battle failure logic.
- Tower level-up validation and execution.
- Tower upgrade validation and execution.
- Weighted draft system.
- Tower rarity.
- Additional draft types beyond Tower Draft and Tower Upgrade Draft.
- Tower recycle system.
- Redeployment inventory.
- Tower rotation.
- Multiplayer synchronization.

---

# 13. Future Expansion Possibilities

Potential future features include:

- Redeployment inventory
- Tower rotation
- Dynamic footprint changes
- Tower synergy mechanics
- Runtime tower transformation
- Special placement restrictions
- Advanced runtime pathfinding validation
- Tower recycle and redeployment
- Temporary obstacle placement
- Trap placement

These features are not required for the first playable version.

---

# 14. Naming Note

This document was previously named Tower Deploy System.

After separating Player System, Draft System, and Battle HUD UI System, the remaining responsibility is more accurately described as Tower Placement System.

The term Placement is preferred because this system focuses on:

- Dragging
- Previewing
- Snapping
- Validating
- Occupying GridNodes
- Updating walkability

The broader deploy pipeline is now split across:

```text
PlayerSystem
    ↓ Level Up
DraftSystem
    ↓ Draft Result
BattleHUDUISystem
    ↓ Draggable Draft Item
TowerPlacementSystem
    ↓ Runtime Placement Or Target Intent
MapSystem
```

---

# Change Log

## 2026-06-18 (Draft Item Target Intent Sync)

- Replaced pending tower interaction wording with Draft item interaction wording.
- Clarified that Tower Placement System detects deployment or existing tower target intent.
- Clarified that Tower Draft level-up requests and Tower Upgrade Draft application are routed to TowerUpgradeSystem.
- Updated progression ownership wording from EXP to ResolvedMonsterCount.

## 2026-06-07

- Synchronized path-blocking validation wording with ProjectOverview and Monster System.
- Clarified that path-blocking validation is part of the intended placement rule set and depends on Monster System pathfinding.

## 2026-05-24

- Renamed Tower Deploy System document to Tower Placement System.
- Removed detailed Battle HUD System ownership from this document.
- Removed detailed Draft System ownership from this document.
- Clarified that Draft System owns draft generation and draft result workflow.
- Clarified that Battle HUD UI System owns runtime UI display and pending tower UI.
- Refocused this document on tower placement, preview, snapping, validation, GridNode occupation, and walkability updates.
- Moved TowerDefinition and tower prefab structure ownership to Tower Framework System.
- Updated first-version scope to exclude draft, player progression, and battle HUD ownership.
- Clarified that path blocking validation depends on Map System data and Monster System pathfinding functionality rather than Map System owning pathfinding.

## 2026-05-22

- Moved Player Level System ownership out of Tower Deploy System and into Player System.
- Updated responsibility boundaries.
- Updated references from PlayerLevelSystem to Player System where the placement flow depends on player progression events.
- Clarified that this system does not own player level, EXP, health, or battle failure logic.

## 2026-05-15

- Clarified responsibility boundaries between Player Level System, Tower Draft System, Battle HUD System, and Tower Deploy System.
- Added TowerDefinitionDatabase as the recommended first-version source of tower draft data.
- Clarified that BattleHUDUI should not directly own Tower Pool data or generate draft choices.
- Added recommended TowerDraftSystem runtime structure and draft data flow.
- Clarified TowerDraftUI responsibility as display and selection callback only.

## 2026-05-13

- Initial Tower Deploy System Design Document created.
- Defined player level and tower draft workflow.
- Defined Battle HUD requirements for current level, EXP progress, and pending tower display.
- Defined Tower Pending Deployment Area workflow.
- Defined tower footprint anchor structure.
- Defined tower drag placement and validation workflow.
- Clarified that path blocking validation is reserved for future implementation and is not part of the first deploy loop.
- Defined future recycle and redeployment direction.
