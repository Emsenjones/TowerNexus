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

1. Complete final Tower and held-Draft preflight, including Tower definition, Level 1 configuration and model, runtime visual and combat requirements, required runtime owners, and Pending Draft ownership.
2. Revalidate the final footprint and obtain one authoritative projection route from the simulated post-placement topology.
3. Capture living Monster movement state, preserve unaffected Monsters, and prepare every affected-Monster reprojection without runtime mutation.
4. Create and initialize the Tower to a ready but inactive state without committing occupancy or consuming the Draft. Its required level model and visual state exist, its combat ownership is valid, and combat has not begun.
5. Commit Runtime Occupied state, apply the complete prepared Monster revision batch, register the deployed Tower, activate its battle runtime, and consume the held Tower Draft as one gameplay change.
6. Request Tile topology refresh without regenerating static Map features.
7. Request Tower-local placement success presentation.

If any required step before commit fails, any temporary Tower is removed and no occupancy, Monster state, Tower registration, combat activation, or held-item state is consumed. After readiness succeeds, gameplay-commit steps are synchronous prevalidated state writes without ordinary failure results. Prepared Monster application performs no pathfinding, projection, classification, or new validation during commit.

Tile refresh and success feedback are post-commit presentation. Their failure is diagnosed as an accepted result with a presentation warning; it does not roll back an otherwise accepted Tower, occupancy state, Monster revision, registration, combat activation, or Draft consumption.

---

# 5. Grid Snap And Footprint Validation

Input position is converted to a Grid coordinate and resolved through Map System. Placement does not depend on physics overlap to identify Grid Nodes.

A candidate new-Tower placement is valid only when:

1. Every occupied anchor resolves to a Grid Node.
2. Every resolved node is effectively walkable and available for placement.
3. The simulated occupancy preserves every required Spawn-to-Target route.
4. The Tower and active Map references remain valid at final confirmation.

An invalid candidate remains visible as invalid feedback but cannot commit occupancy or consume the item.

## 5.1 Route Preservation

Route validation uses the same topology rules as Monster pathfinding:

```text
Simulate Candidate Nodes As Occupied
    -> Query One Authoritative Spawn-To-Target Projection Route
    -> No Route: Reject Candidate
    -> Route Exists: Candidate May Continue
```

Simulation does not change authored Base Walkable state, active Runtime Occupied state, or active Monster paths.

Live Monster instances do not participate in placement legality. A candidate is not rejected because a Monster occupies or approaches its footprint, or because the placement disconnects the old branch that Monster entered. After topology acceptance, Monster System preserves every unaffected Monster unchanged and prepares affected-Monster reprojections before commit.

The current one-Spawn/one-Target Map requires one surviving route. Multiple Spawn Routes will require an explicit route-validation contract before expanding this rule.

## 5.2 Monster Route Revision

All living Monster movement state is captured after final topology validation and before Runtime Occupied changes. The accepted authoritative Spawn-to-Target projection route is shared by every affected Monster in that placement transaction.

Current placement only adds blockers. A Monster is affected when its reached node, active next node, or remaining route intersects the candidate footprint, or its captured movement state is invalid. A Monster whose remaining movement state does not intersect the footprint is unaffected: it preserves its world position, active segment, route position, and complete remaining route without another path query or prepared movement revision.

An affected Monster is mapped to the spatially closest eligible Grid on that route. Remaining-route continuity, avoidance of free forward progress, and stable route order break ties. Target is not eligible for a living unresolved Monster; Spawn remains eligible. Multiple Monsters may share the same projection Grid.

Reprojection is movement correction only. It preserves Monster gameplay state and does not cause damage, healing, death, Target arrival, progress, registration, or deregistration. Already released Projectiles keep their release-time direction or landing position and may hit or miss under their existing rules.

Monster route preparation cannot create a second placement-legality gate. Once the global route and ordinary placement constraints pass, live Monster positions cannot delay or reject deployment. Prepared affected-Monster revisions apply through one non-failing state-write boundary and do not pathfind during gameplay commit.

An invalid Monster movement snapshot, unavailable spatial comparison, proximity to Target, or inability to continue an old route uses deterministic projection fallback. Only transaction-wide technical preconditions such as an invalid topology plan, missing required runtime owner, or authoritative route without an eligible non-Target Grid may fail preparation.

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
- Unaffected Monsters retain their complete valid routes without another path query.
- The prepared batch reprojects affected Monsters before gameplay advances to another frame.

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
- Placement preflight that mutates occupancy or Monster state
- Incomplete Tower definition, Level 1 model, visual, combat, runtime-owner, or Pending Draft ownership preflight
- An additional path query or prepared revision for an unaffected Monster
- Prepared revision application that performs pathfinding or exposes an ordinary commit-time failure
- A committed placement without its complete prepared Monster route revision
- A committed Tower or Monster revision whose held Draft was not consumed
- A living unresolved Monster projected directly onto Target
- Reprojection that causes combat, resolution, registration, or progress side effects
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
- Placement legality independent of live Monster positions
- Atomic occupancy, affected-Monster reprojection, and held-Draft consumption commit
- Runtime occupancy and Tile-only topology refresh
- New-Tower, level-up, attack-range, and eligible-target feedback
- General return-to-area drag cancellation

Deferred topics include Tower recycling, redeployment inventory, Tower rotation, dynamic footprint changes, multiplayer synchronization, traps, and temporary non-Tower blockers.
