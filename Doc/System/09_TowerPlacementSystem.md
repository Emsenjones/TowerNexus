# Tower Nexus - Tower Placement System

Document Set: System

---

# 1. Purpose And Ownership

Tower Placement System converts a dragged Draft item into either:

- A request to place a new Tower on the battlefield
- A Tower level-up intent targeting an existing Tower
- A Tower Upgrade intent targeting an existing Tower
- A cancelled drag that preserves the held item

It owns drag-state coordination, Grid snapping, footprint resolution, placement preview state, placement validation, existing-Tower intent detection, runtime occupancy commit, and the resulting Map topology refresh request.

It does not own Draft generation, held-item presentation, Tower level or upgrade eligibility, Tower-local visual rendering, Map data, pathfinding execution, Monster movement, or Tower combat.

Tower placement is runtime battlefield editing: accepted occupancy changes effective walkability and may change Monster routes.

---

# 2. Placement Input Contract

Tower Placement System receives one active held Draft item from Battle HUD UI System.

| Draft Item | Valid Intent |
|---|---|
| Tower Draft | Place a new Tower or target a same-TowerFamily Tower for level-up |
| Tower Upgrade Draft | Target an eligible existing Tower for upgrade application |

Only one drag operation and one active Tower placement preview may exist at a time.

The held item is consumed only after the receiving gameplay system accepts the requested result. Rejection or cancellation preserves it.

An accepted Draft-item drag owns its pointer gesture until release or cancellation. Camera System must not begin or continue a pan from that gesture, including after the pointer moves from the UI into the battlefield.

---

# 3. Placement Structure Contract

Tower Framework System defines the reusable Tower structure. Placement consumes only:

- The Tower runtime template referenced by TowerDefinition
- A Center Anchor used as the snap reference
- Occupied Anchors defining the Tower footprint

The footprint is resolved generically:

```text
Candidate Grid Node
    -> Align Center Anchor
    -> Resolve Occupied Anchor Positions
    -> Map Positions To Grid Nodes
    -> Validate Complete Footprint
```

Tower-specific footprint shapes are authored through anchors rather than hardcoded placement rules.

---

# 4. New Tower Placement Flow

```text
Begin Tower Draft Drag
    -> Create One Active Tower Preview
    -> Follow Input Position
    -> Snap To Candidate Grid Node
    -> Resolve Footprint
    -> Continuously Present Validity
    -> Release Input
        -> Cancel, Reject, Or Commit
```

An accepted placement performs one transaction:

1. Revalidate the final footprint and route rule.
2. Create the deployed Tower at the resolved placement.
3. Record the occupied Grid Nodes on the Tower instance.
4. Commit Runtime Occupied state through Map System.
5. Request Tile topology refresh without regenerating static Map features.
6. Request Monster path recalculation.
7. Request Tower-local placement success presentation.
8. Consume the held Tower Draft item.

If any required step before commit fails, no occupancy or held-item state is consumed. Cleanup of presentation after commit does not undo an otherwise accepted gameplay result.

---

# 5. Grid Snap And Footprint Validation

Input position is converted to a Grid coordinate and resolved through Map System. Placement does not depend on physics overlap to identify Grid Nodes.

A candidate new-Tower placement is valid only when:

1. Every occupied anchor resolves to a Grid Node.
2. Every resolved node is effectively walkable and available for placement.
3. The simulated occupancy preserves every required Spawn-to-Target route.
4. No candidate node is the current node of an alive Monster.
5. Every alive Monster retains a route from its current node to the Target.
6. The Tower and active Map references remain valid at final confirmation.

An invalid candidate remains visible as invalid feedback but cannot commit occupancy or consume the item.

## 5.1 Route Preservation

Route validation uses the same topology rules as Monster pathfinding:

```text
Simulate Candidate Nodes As Occupied
    -> Query Required Spawn-To-Target Route
    -> Query Each Alive Monster's Current-Node-To-Target Route
    -> Restore Simulation
    -> All Required Routes Exist: Candidate May Continue
    -> Any Required Route Missing: Reject Candidate
```

Simulation does not change authored Base Walkable state, active Runtime Occupied state, or active Monster paths.

An alive Monster's current node is not a valid placement node even when another route query would otherwise succeed. This prevents a committed Tower from occupying the Monster's current gameplay position.

The current one-Spawn/one-Target Map requires one surviving route. Multiple Spawn Routes will require an explicit route-validation contract before expanding this rule.

---

# 6. Existing Tower Target Intent

Existing Tower targeting uses deployed Towers' recorded occupied Grid Node membership. It does not require colliders, nearest-position thresholds, or physics selection.

```text
Current Candidate Grid Node
    -> Find Deployed Tower Whose Occupied Nodes Contain It
    -> Resolve Intent From Dragged Draft Item
    -> Ask Tower Upgrade System For Eligibility
```

## 6.1 Tower Draft Level-Up Intent

A Tower Draft may target an existing Tower when the TowerFamily matches. Tower Upgrade System remains the final authority for level limits and level-up acceptance.

An accepted result consumes the Tower Draft and requests the Tower-owned visual path to refresh its level model and success presentation.

## 6.2 Tower Upgrade Draft Intent

A Tower Upgrade Draft may target an existing Tower only when Tower Upgrade System accepts that TowerUpgradeDefinition for the current Tower state.

An accepted result consumes the held Upgrade item and its pending reservation. A rejected result preserves both.

Tower Placement System detects and forwards intent; it does not duplicate Tower Upgrade eligibility.

---

# 7. Preview And Feedback Contract

Tower Placement System owns when previews exist and which state they represent. Tower visual presentation owns how Tower-local feedback is rendered.

## 7.1 New Tower Preview

The active preview represents:

- Permanent Tower base presentation
- The model for the Draft item's resolved deployment level
- The complete authored footprint
- Current valid or invalid state
- Authored base attack range

The preview is non-gameplay state: it does not attack, occupy nodes, affect pathfinding, or register as a deployed Tower.

Valid and invalid feedback applies to the complete preview rather than one placeholder renderer. Exact material, shader, and animation techniques are implementation choices.

## 7.2 Tower Level-Up Preview

When a Tower Draft hovers an eligible same-TowerFamily deployed Tower, the preview moves to that Tower's center and presents the next-level model without changing the deployed Tower.

If the occupied node identifies a Tower but the level-up is ineligible, the state remains invalid; ordinary empty-grid placement validation does not run on that occupied node.

## 7.3 Attack Range Preview

During an active Draft-item drag:

- Deployed Towers may display their current resolved attack range.
- A new-Tower preview displays its authored base attack range.
- A level-up preview displays the range appropriate to its reviewed preview contract.

Tower Placement System requests visibility. The Tower-owned visual layer renders the authored unit-radius range presentation. Range presentation never changes combat range or placement validity.

## 7.4 Upgrade Target Feedback

During a Tower Upgrade Draft drag, every current deployed Tower may be evaluated by Tower Upgrade System. Eligible Towers receive Tower-local target feedback; ineligible Towers do not.

This feedback indicates target eligibility only. It does not simulate package-specific combat outcomes.

---

# 8. Drag Cancellation

Releasing any dragged Draft item back inside the Draft Item Interaction Area cancels before scene validation.

Cancellation:

- Preserves the held item and pending reservation
- Removes the active placement preview
- Hides all drag-time attack range presentation
- Clears all Tower target feedback
- Clears placement and target state
- Does not invoke placement, level-up, or upgrade application

The same cleanup occurs after accepted or rejected release, with item consumption determined only by the gameplay result.

---

# 9. Runtime Occupancy

Map System owns Base Walkable, Runtime Occupied, and effective Is Walkable state. Tower Placement System requests occupancy changes after an accepted placement result.

When occupancy changes:

- Effective walkability changes.
- Tile connection presentation refreshes from current effective walkability.
- Static Obstacle, Spawn, and Target presentation remains unchanged.
- Alive Monsters request path recalculation.

Tower Placement System never rewrites Base Walkable or converts a placed Tower into an authored Map obstacle.

Tower removal and redeployment are deferred. When designed, they must release recorded occupied nodes through the same Map ownership boundary.

---

# 10. Validation

Placement authoring and runtime validation should report or reject at minimum:

- Missing Tower runtime template
- Missing Center Anchor or Occupied Anchor data
- Empty or duplicate footprint anchors where invalid
- Missing active Map
- Candidate anchors outside the Map
- Candidate nodes that are unwalkable or already occupied
- Candidate occupancy that blocks a required route
- Stale or unavailable target Tower
- A second drag or confirmation competing with the active operation
- Camera pan competing for an active Draft-item drag gesture

Validation must not silently alter Tower footprints, Map authored state, or Tower Upgrade eligibility.

---

# 11. Approved Scope And Deferred Topics

Current scope includes:

- Tower Draft placement and same-family level-up intent
- Tower Upgrade target intent
- One active drag and placement preview
- Grid snapping and anchor-defined footprint validation
- Occupied-node Tower target detection
- Route-preserving placement validation
- Runtime occupancy and Tile-only topology refresh
- New-Tower, level-up, attack-range, and eligible-target feedback
- General return-to-area drag cancellation

Deferred topics include Tower recycling, redeployment inventory, Tower rotation, dynamic footprint changes, multiplayer synchronization, traps, and temporary non-Tower blockers.
