# Tower Nexus - Tower Placement System

Document Set: System

---

# 1. Purpose And Ownership

Tower Placement System converts a dragged Draft item into either:

- A request to place a new Tower on the battlefield
- A Tower level-up intent targeting an existing Tower
- A Tower Upgrade intent targeting an existing Tower
- A cancelled drag that preserves the held item

It owns drag-state coordination, Grid snapping, footprint resolution, placement preview state, Monster dashed-line path state and displayed-route selection, placement validation, existing-Tower intent detection, runtime occupancy commit, and the resulting Map topology refresh request.

It does not own Draft generation, held-item presentation, Tower level or upgrade eligibility, Tower-local visual rendering, Map data, pathfinding execution, Monster movement, or Tower combat.

Tower placement is runtime battlefield editing: accepted occupancy changes effective walkability and may change Monster routes.

Ordinary Upgrade acceptance preflights the exact currently held Upgrade identity,
target membership, eligibility, consumption, and required combat readiness. Its
synchronous semantic commit records the Upgrade, applies the prepared baseline,
and consumes the reward without optional callbacks. Necessary runtime refresh
follows once. An unrecoverable required refresh failure closes the Battle through
result-neutral Technical Failure; it does not refund or replay accepted investment.
Committed investment facts and terminal observations are retained before Stage
release, independent of presentation and flow-subscriber ordering. Acceptance,
notification, and outer cleanup share one interaction guard, while stop/release
remains legal. Optional notification or presentation failure cannot undo commitment.

---

# 2. Placement Input Contract

Tower Placement System receives drag intent from Battle HUD UI System and validates the associated Held entry with Draft ownership.

| Draft Item | Valid Intent |
|---|---|
| Tower Draft | Place a new Tower or target a same-TowerFamily Tower for level-up |
| Tower Upgrade Draft | Target an eligible existing Tower for upgrade application |

Only one drag operation and one active Tower placement preview may exist at a time.

The held item is consumed only after the receiving gameplay system accepts the requested result. Semantic consumption removes the exact entry from Draft ownership and records it as consumed in the same non-failing commit, so every pointer and drag handler rejects it immediately. Destruction of the consumed view is later presentation cleanup. Rejection or cancellation preserves the item.

An accepted Draft-item drag owns its pointer gesture until release or cancellation. Camera System must not begin or continue a pan from that gesture, including after the pointer moves from the UI into the battlefield.

---

## 2.1 Preview Query Reuse

Preview may reuse a topology result only after validating the current Map binding
and index and resolving the complete current footprint. Reuse requires identical
Map/binding identity, structure and walkability revisions, and footprint node
identities. Unresolved footprints or unavailable configuration do not reuse cached
results. Occupied and route-blocking results for resolved footprints may be cached.
New gestures, cancellation, and rebinding clear preview reuse. Tower Level-Up and
Upgrade eligibility remain live checks.

Final submission creates a fresh topology plan and prepares current living-Monster
movement revisions. Preview reuse never authorizes commitment or reuses a prepared
Monster batch. Path order and movement continuity retain their existing contracts.

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
2. Revalidate the final footprint and obtain one authoritative new Spawn-to-Target Route from the simulated post-placement topology.
3. Capture every living Monster's physical current Grid and prepare its new-Route continuation, reachable Grid rejoin, or nearest-eligible forced relocation without runtime mutation.
4. Create and initialize the Tower to a ready but inactive state without committing occupancy or consuming the Draft. Its required level model and visual state exist, its combat ownership is valid, and combat has not begun.
5. Commit Runtime Occupied state, apply the complete prepared Monster revision batch, register the deployed Tower, activate its battle runtime, and consume the held Tower Draft as one gameplay change.
6. Request Tile topology refresh without regenerating static Map features.
7. Request Tower-local placement success presentation.

If any required step before commit fails, any temporary Tower is removed and no occupancy, Monster state, Tower registration, combat activation, or held-item state is consumed. After readiness succeeds, gameplay-commit steps are synchronous prevalidated state writes without ordinary failure results. Prepared Monster application performs no pathfinding, join selection, relocation search, classification, or new validation during commit.

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
    -> Query One Authoritative New Spawn-To-Target Route
    -> No Route: Reject Candidate
    -> Route Exists: Candidate May Continue
```

Simulation does not change authored Base Walkable state, active Runtime Occupied state, or active Monster paths.

Live Monster instances do not participate in placement legality. A candidate is
not rejected because a Monster occupies or approaches its footprint, because
the placement disconnects the old branch that Monster entered, or because the
Monster cannot reach the new Route from its current Grid. After topology
acceptance, Monster System prepares a complete continuation, rejoin, or forced-
relocation result for every living targetable Monster before commit.

The current one-Spawn/one-Target Map requires one surviving route. Multiple Spawn Routes will require an explicit route-validation contract before expanding this rule.

## 5.2 Monster Route Revision

All living targetable Monster movement state is captured after final topology
validation and before Runtime Occupied changes. The accepted authoritative new
Spawn-to-Target Route is shared by every prepared Monster result in that
placement transaction.

Monster System uses the gameplay movement root's physical current Grid:

- A non-finite Transform, a finite position without a Map Grid, or a Grid
  covered by the new footprint receives forced relocation before other
  classification.
- If that Grid belongs to the new Route and is not covered, preserve the exact
  world position and install the applicable Route continuation.
- Otherwise, if the Grid remains effectively walkable and can reach the new
  Route, preserve the exact world position and prepare an orthogonal connector
  to the nearest reachable non-Target Route Grid.
- If the Grid is covered, disconnected from every eligible new-Route Grid, or
  cannot be resolved from valid state, prepare relocation to the nearest
  eligible non-Target recovery Grid that can reach the new Route.

Reachability is evaluated before distance when selecting a join. A recovery
Grid may be outside the authoritative Route, but it must be effectively
walkable under the simulated topology, outside the complete new footprint, and
connected to an eligible new-Route Grid. Forced relocation is the only result
allowed to change a finite Monster position during commit. Target is never a
join or relocation destination for a living unresolved Monster; Spawn remains
eligible.

One shared connectivity and distance query against the simulated topology
identifies the Route-connected component and stable minimum-path recovery joins.
Reachable Monsters select the spatially nearest eligible Route Grid in Map-local
XZ and require only the final connector path. Recovery candidates first
minimize Map-local relocation distance and then connector path cost.

Route revision preserves Monster gameplay state and does not cause damage,
healing, death, Target arrival, progress, registration, or deregistration.
Already released Projectiles keep their release-time direction, landing
position, target identity, lifetime, and hit rules and may hit or miss under
those existing rules.

Monster route preparation cannot create a second placement-legality gate. Once
the global Route and ordinary placement constraints pass, live Monster state
cannot delay or reject deployment. Prepared revisions apply through one non-
failing state-write boundary and perform no pathfinding or candidate selection
during gameplay commit.

A Monster following an earlier placement connector is captured as current live
state. Failed preflight preserves that connector unchanged. A later accepted
placement atomically supersedes it; old and new connector state never
accumulate.

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

Before an accepted result, the interaction preflights the exact held Tower Draft and Draft-domain ownership together with the target, next valid TowerLevelConfig, combat-runtime readiness, and fully prepared next combat baseline.

The non-failing semantic commit advances the Level, applies that prepared combat baseline through pure state assignment, consumes the exact entry through Draft-domain ownership. It does not call presentation, publish events, traverse active Attack Entities, or repeat validation. Investment evidence is published before `OnLevelChanged` or other external notifications. These notifications are exception-isolated and are not combat-refresh authority.

The Tower-owned visual path then performs best-effort level-model replacement, Attack Origin handoff, VFX, and consumed-view destruction. Presentation failure cannot roll back the accepted Level, damage baseline, or Draft consumption, and a failed cleanup cannot leave an interactive ghost Draft.

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

## 7.5 Monster Dashed-Line Path State

During an active Battle, the Monster dashed-line path has two display states.
Monster System supplies ordered main-route data; Tower Placement System selects
the state and route; Battle HUD UI System renders the requested presentation.

| State | Trigger And Route Selection | Presentation |
|---|---|---|
| Normal | No active drag: formal main route. Active Tower Draft drag: valid candidate main route, or formal main route for fallback cases below | Configured Normal Color, including Alpha; default white at Alpha 0.5 |
| Blocked | A complete otherwise valid new-Tower footprint blocks the Spawn-to-Target route; retain the displayed route from immediately before the blocking result | Configured Blocked Color, including Alpha; default red at Alpha 0.5 |

These are presentation states. Formal and hypothetical route selection remain
separate gameplay responsibilities even though both use Normal appearance.

Beginning a Tower Draft drag immediately displays Normal with the current
formal main route, even before the Tower enters the Map. During ordinary
new-Tower placement, resolve the complete snapped footprint before requesting
its hypothetical main route. Refresh the preview when that footprint or its
Map topology changes. Pointer movement that leaves both unchanged does not
require a different route result.

The formal main route is shown in Normal when the Tower is still in the
Pending area, leaves the Map, has a footprint partially outside the Map, overlaps
unavailable or occupied nodes, or enters existing-Tower level-up targeting.
Existing-Tower targeting leaves occupancy unchanged, including an ineligible
level-up target. Normal path feedback means no route-blocking warning is active;
it does not assert that the Tower is deployable. Tower-local invalid feedback
and final acceptance remain authoritative for other placement constraints.

Route blocking immediately enters Blocked during drag, before input
release. Only color and opacity change: the retained path geometry is not
updated. Continuous movement across multiple blocking candidates preserves the
same retained geometry. Each new drag seeds its retained route from the formal
main route, so blocking before any valid candidate also has a route to retain.

Returning to a valid candidate immediately replaces the route and restores
Normal. Entering a fallback case immediately restores the formal route
in Normal. The next blocking result retains whichever valid-preview
route was displayed immediately before it. Retained candidate geometry belongs
only to the current drag and current Battle/Map topology; it cannot survive a
new drag, topology replacement, or Battle replacement as current preview data.

Successful deployment finishes the drag and displays the committed formal
main route in Normal. Failed release, return-to-Pending cancellation, or other
drag cancellation finishes the drag and displays the unchanged formal main
route in Normal. Blocked appearance never persists after the drag ends. Battle end
or Stage release instead removes the line under the Stage lifecycle contract.

While a Draft modal owns input, retain the route/state but suspend battlefield
movement and deployment input. If the player releases or cancels the held drag
inside the modal, finish it as cancellation when battlefield input returns;
closing the window cannot deploy through that release. A continuously held drag
may resume. Battle cleanup and topology invalidation remain authoritative.

Technical route/presentation failure hides the line and reports a diagnostic,
without being classified as route blocking. Recovery on the same topology may
restore the still-valid retained route, including for a blocked result. A changed
Battle/Map binding or topology instead requires a new formal baseline. Repeated
identical failures do not continuously repeat diagnostics.

The line does not modify placement validity, consume a Draft, commit occupancy,
refresh Tiles, or redirect Monsters. Final placement validation remains fresh
and independent of the preview. Tower Upgrade Draft targeting does not propose
new occupancy and does not initiate these Tower Draft route-preview states.

---

# 8. Drag Cancellation

Releasing any dragged Draft item back inside the Draft Item Interaction Area cancels before scene validation.

Cancellation:

- Preserves the held item and pending reservation
- Removes the active placement preview
- Hides all drag-time attack range presentation
- Clears all Tower target feedback
- Clears placement and target state
- Clears retained dashed-line candidate data and restores the formal route in Normal while Battle remains active
- Does not invoke placement, level-up, or upgrade application

The same cleanup occurs after accepted or rejected release, with item consumption determined only by the gameplay result.

---

# 9. Runtime Occupancy

Map System owns Base Walkable, Runtime Occupied, and effective Is Walkable state. Tower Placement System requests occupancy changes after an accepted placement result.

When occupancy changes:

- Effective walkability changes.
- Tile connection presentation refreshes from current effective walkability.
- Static Obstacle, Spawn, and Target presentation remains unchanged.
- Every living targetable Monster receives the prepared continuation, rejoin,
  or relocation result for the authoritative new Route.
- The prepared batch becomes active before gameplay advances to another frame.

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
- A dashed-line state that treats a non-route placement failure as route blocking
- Blocked that updates retained path geometry or survives drag completion
- Valid candidate preview that disagrees with the authoritative new main route under unchanged topology and footprint
- Retained dashed-line data reused across a new drag, topology replacement, or Battle/Map replacement
- Placement preflight that mutates occupancy or Monster state
- Incomplete Tower definition, Level 1 model, visual, combat, runtime-owner, or Pending Draft ownership preflight
- Prepared revision application that performs pathfinding or exposes an ordinary commit-time failure
- A committed placement without its complete prepared Monster route revision
- A committed Tower or Monster revision whose held Draft was not consumed
- A committed Level Up whose exact Pending Draft remains owned or interactive
- Level Up combat-baseline apply that validates, publishes, invokes callbacks, or traverses active entities during semantic commit
- A living unresolved Monster joined or relocated directly onto Target
- A continuation or reachable rejoin that changes a finite Monster's position during commit
- A forced relocation to an ineligible or non-nearest recovery Grid
- Route revision that causes combat, resolution, registration, or progress side effects
- Repeated placement that accumulates more than one active connector for a Monster
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
- Atomic occupancy, all-living-Monster route revision, and held-Draft consumption commit
- Runtime occupancy and Tile-only topology refresh
- New-Tower, level-up, attack-range, and eligible-target feedback
- Normal and Blocked state selection for the Monster dashed-line path
- General return-to-area drag cancellation

Deferred topics include Tower recycling, redeployment inventory, Tower rotation, dynamic footprint changes, multiplayer synchronization, traps, and temporary non-Tower blockers.

Accepted investments capture immutable consumption facts and publish evidence before external notifications that can end the Stage. Stop retains unconsumed Draft entries for terminal capture; Stage release invalidates them.


### Placement submission and membership contract

Battle coordination owns a stable placement-submission service. Interaction owns
pointer/preview/highlight state; submission owns accepted deployment and investment
coordination and the ordered, read-only deployed membership. Reads never prune or
rebuild membership. Pending authority comes directly from Draft. Deployment candidates
capture one coherent definition, shape and pose; final submission revalidates current
Battle/Map authority and topology. Rejected, committed and committed technical failure
are distinct outcomes. Outer interaction protection lasts through visual cleanup;
internal submission protection permits only its authorized Upgrade core, while new
requests are blocked. Terminal evidence precedes cleanup. Stop retains members;
release detaches the member snapshot before per-Tower callbacks and isolates cleanup
failures. Observers retain one subscription to the stable submission service across
Stage changes. Existing geometry, combat rules and result-screen lifetimes are preserved.
