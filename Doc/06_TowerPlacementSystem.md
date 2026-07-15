# Tower Nexus - Tower Placement System Design Document

---

# 1. System Overview

The Tower Placement System is one of the core runtime gameplay systems in Tower Nexus.

This system is responsible for converting a dragged Tower Draft item into either a placed battlefield tower or a target-intent request for an existing tower.

The Tower Placement System focuses only on tower placement-related responsibilities:

- Tower drag placement interaction
- Tower placement preview
- Tower Draft level-up preview request flow
- Attack range preview request flow during Draft item drag
- Current Drag Operation cancellation flow
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

Tower Placement System detects whether the Tower Draft item is dropped onto a valid deployment tile or onto an existing same-TowerFamily tower.

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
- Draft item drag cancellation target when the current drag operation is released back into the Battle HUD interaction area
- Draft item removal after successful placement, accepted tower level-up, or accepted upgrade application
- Placement feedback display when requested by placement logic

Battle HUD UI System should not:

- Validate tower placement.
- Update GridNode walkability.
- Decide placement legality.
- Own tower placement workflow.
- Validate tower level-up or tower upgrade rules.
- Own tower-local valid-target highlight presentation.

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
10. Tower Placement System may request tower-side model spawn feedback through the deployed tower's visual ownership path.
11. Battle HUD UI System removes the consumed Draft item.
12. If placement is invalid, the Draft item remains in the Draft Item Interaction Area.

---

## 7.2 Existing Tower Target Intent

When a Draft item is dragged onto an existing tower, Tower Placement System should detect the target intent and route it to TowerUpgradeSystem.

Tower Draft item on existing tower with the same TowerFamily:

```text
Tower Draft Item
    ↓ Dropped On Existing Same-TowerFamily Tower
TowerPlacementSystem
    ↓ Detect Tower Level-Up Target Intent
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

Tower Placement System should not decide whether the target tower satisfies TowerFamily, TowerLevel, duplicate upgrade, or max-level rules.

During a Tower Upgrade Draft drag, Tower Placement System may resolve current deployed tower candidates and ask TowerUpgradeSystem whether each candidate can receive the dragged TowerUpgradeDefinition.

Tower Placement System may request valid-target highlight visibility from the candidate tower's visual ownership path, but it should not directly modify tower renderers, materials, model hierarchy, or VFX playback.

After a TowerUpgradeSystem upgrade application succeeds, Tower Placement System may request upgrade-applied success feedback through the target tower's visual ownership path.

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

## 7.4 Placement Preview

During any active Tower Draft drag operation, there should be exactly one active Tower Preview.

For a valid empty deployable area, the active Tower Preview should display:

- The tower's permanent base visual.
- The tower model for the dragged Tower Draft item's resolved deployment level.
- Whole-preview material tint and transparency applied to TowerBaseVisualRoot and the spawned tower model.
- Attack range preview based on the tower's AttackConfig attackRange.

Current first-version Tower Draft configuration may resolve all new tower deployment results to Lv1, but Tower Placement System should use the Draft item's resolved deployment level instead of hardcoding Lv1.

Tower Placement System owns when the active preview should exist, where it should snap, and which placement state it represents.

Tower Placement System should request visual updates through the placed or preview tower's TowerBehaviour / TowerVisualController ownership path. It should not directly manipulate tower visual hierarchy, replace tower models, or modify renderer materials.

During placement:

- Valid placement displays the whole Tower Preview with original material color multiplied by valid preview tint plus preview alpha.
- Invalid placement displays the whole Tower Preview with original material color multiplied by invalid preview tint plus preview alpha.
- Invalid placement keeps the active Tower Preview visible in an invalid feedback state.
- Active Tower Preview remains semi-transparent until release or cancel.

Recommended examples:

| State | Visual |
|---|---|
| Valid | Original material color * validPreviewTint color + validPreviewTint alpha |
| Invalid | Original material color * invalidPreviewTint color + invalidPreviewTint alpha |

Preview material rules:

- Treat TowerBaseVisualRoot and the spawned tower model as one ghost visual.
- Apply preview tint color and alpha to all renderers under VisualRoot after the tower model is spawned.
- Preview feedback configuration should live on TowerPlacementPreview:
  - `validPreviewTint`
  - `invalidPreviewTint`
- Apply preview material state per renderer material instance.
- Do not replace all preview renderers with one shared ghost material.
- Support `_BaseColor` and `_Color` material properties.
- Do not use `sharedMaterial`.
- Cache preview material instances per preview object and reuse them when switching valid or invalid state.
- PreviewRenderer may remain for compatibility, but should not be the main placement feedback visual.
- TowerPlacementPreview should not require a serialized PreviewRenderer reference for material feedback.
- PreviewRenderer should be disabled or visually ignored unless existing runtime logic still depends on its transforms.
- Temporary Tower Preview instances are placement visuals and validation helpers only; they should not run TowerCombatBehaviour.

Placement preview is part of placement interaction.

Battle HUD UI System may display additional UI feedback, but validation ownership remains in Tower Placement System.

---

## 7.5 Tower Level-Up Preview

Tower Level-Up Preview currently applies only to Tower Draft items dragged onto an already deployed tower with the same TowerFamily.

It does not describe future Tower Upgrade Draft item effect previews such as attack damage upgrades, extra Magic Orbs, or other upgrade-definition-specific preview behavior.

Tower Level-Up Preview may be entered when:

- The dragged item is a Tower Draft item.
- The target is an existing deployed tower.
- The dragged Tower Draft TowerFamily matches the deployed tower TowerFamily.
- The existing tower is below max tower level.

Tower Level-Up Preview target detection should not require deployed tower Colliders, raycast tower hits, distance thresholds, or nearest spatial matching.

Tower Level-Up Preview target detection should use occupied GridNode identity:

1. The active Tower Preview Center Anchor snaps to the current target GridNode.
2. Tower Placement System searches deployed towers.
3. If the current target GridNode is contained in a deployed tower's TowerInstance.OccupiedNodes, that deployed tower is the hovered tower candidate.
4. If a hovered tower candidate exists, Tower Placement System asks TowerUpgradeSystem whether the dragged Tower Draft can level up that candidate.
5. If the request is valid, the active Tower Preview snaps to the hovered tower's TowerAnchorSet.CenterAnchor and displays the candidate tower's Current Level + 1 ghost model.
6. If the request is invalid, Tower Level-Up Preview is not entered, the active preview remains invalid, and normal grid placement validation should not run for that occupied node.
7. If no hovered tower candidate exists, Tower Placement System falls back to normal grid placement validation.

Current max tower level target:

```text
Max Tower Level = 3
```

When all conditions are satisfied, the active Tower Preview should switch to:

```text
TowerBaseVisualRoot
    +
Current Level + 1 Ghost Tower Model
```

The Tower Level-Up Preview:

- Shows the next level appearance as a semi-transparent ghost model.
- Displays attack range preview.
- Does not modify the existing deployed tower before release.
- Remains active until release or cancel.
- Uses tower level-up validation rather than grid occupation validation.

If a hovered tower candidate exists but cannot be upgraded, Tower Level-Up Preview is not entered and the active preview remains in invalid feedback state. Normal placement validation should not run for that occupied node.

On successful release, Tower Placement System forwards the level-up request to TowerUpgradeSystem. TowerUpgradeSystem validates and applies tower level data only. After an accepted level-up, Tower Placement System asks the target TowerBehaviour to refresh visuals through the tower-owned visual path.

After the visual refresh is accepted, Tower Placement System may request tower-side model refresh feedback through the same visual ownership path.

Tower Placement System should not directly replace the deployed tower model, directly refresh AttackOrigin, or play tower-side VFX itself.

---

## 7.6 Attack Range Preview

When a Draft item drag operation begins:

- All deployed towers should display their attack range preview.
- The active Tower Preview should display its own attack range preview.

Attack range preview uses:

```text
TowerDefinition
    ↓
AttackConfig
    ↓
attackRange
```

Runtime rendering direction:

- Each Tower Base Prefab may provide an `AttackRangePreview` child.
- The `AttackRangePreview` child should contain a circular mesh whose radius is 1 when local scale is 1.
- TowerVisualController scales `AttackRangePreview` uniformly to `attackRange`.
- TowerVisualController shows or hides `AttackRangePreview` during Draft item drag lifecycle.
- AttackRangePreview may provide lightweight prefab-authored looping presentation while the preview is visible.
- TowerVisualController should not generate placement validation data or affect combat range logic.

When the drag operation ends or is cancelled:

- Deployed tower attack range previews should be hidden.
- The active Tower Preview should be removed.
- Temporary preview objects should be destroyed.

Tower Placement System may request attack range preview visibility during drag operations, but TowerVisualController owns tower-local range preview rendering.

---

## 7.7 Tower Upgrade Draft Target Feedback

Tower Upgrade Draft target feedback is a drag-time presentation layer for showing which deployed towers can receive the dragged TowerUpgradeDefinition.

Responsibility split:

- Tower Placement System owns the active drag lifecycle and deployed tower candidate lookup.
- TowerUpgradeSystem owns upgrade eligibility checks.
- TowerVisualController owns tower-local highlight presentation.
- Battle HUD UI System owns the UI drag entry and pending Draft item display only.

Valid target feedback should not preview upgrade-definition-specific gameplay effects such as extra projectiles, extra Magic Orbs, or future behaviour package visuals.

The first-version feedback can be a lightweight periodic tower highlight or pulse on valid targets.

When a Tower Upgrade Draft drag begins or updates:

1. Tower Placement System resolves deployed tower candidates from the current battlefield state.
2. Tower Placement System asks TowerUpgradeSystem whether each candidate can receive the dragged TowerUpgradeDefinition.
3. Towers accepted by TowerUpgradeSystem may receive valid-target highlight presentation through their TowerVisualController ownership path.
4. Towers rejected by TowerUpgradeSystem should not display valid-target highlight.

Valid-target highlight must be cleared when:

- Drag is cancelled.
- The dragged item is released back into the Draft Item Interaction Area.
- Upgrade application succeeds.
- Upgrade application fails.
- The pending Draft item is restored.
- The target tower is destroyed or no longer available during drag.

TowerUpgradeSystem must not directly control highlight presentation.

---

## 7.8 Current Drag Operation Cancellation

Canceling the current drag operation is a general Draft item behavior, not only a Tower Draft deployment behavior.

If any currently dragged Draft item is released back inside the Battle HUD Draft Item Interaction Area:

- Cancel the current drag operation immediately.
- Do not perform scene placement validation.
- Do not perform tower level-up validation.
- Do not perform tower upgrade application validation.
- Do not deploy a tower.
- Do not upgrade a tower.
- Return the Draft item to Battle HUD ownership.
- Destroy the active Tower Preview if one exists.
- Hide all deployed tower attack range previews.
- Clear all Tower Upgrade Draft valid-target highlights.
- Clear drag state.

This applies to Tower Draft items, future Tower Upgrade Draft items, and future draggable Draft item types.

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
- One active Tower Preview during Tower Draft drag.
- Deployment-level model placement preview for valid empty deployment areas.
- Current Level + 1 ghost model preview for same-TowerFamily Tower Draft level-up targets.
- Attack range preview request flow during Draft item drag.
- Current Drag Operation cancellation when a dragged Draft item is released back into the Battle HUD Draft Item Interaction Area.
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
