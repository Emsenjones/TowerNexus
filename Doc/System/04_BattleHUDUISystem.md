# Tower Nexus - Battle HUD UI System

Document Set: System

---

# 1. Purpose And Ownership

Battle HUD UI System owns battle-local presentation and player interaction surfaces.

It presents:

- Player level and level progress
- Current player health
- Draft choices
- Selected but unconsumed Draft items
- Drag, placement, and Tower-target feedback
- Battle notifications approved by future designs

It observes gameplay state and forwards player intent. It does not own Player state, Draft generation or reward ownership, Draft-driven simulation pause, placement validation, Tower Upgrade rules, Map topology, Monster runtime, combat results, or Game Flow transitions.

An ordinary Upgrade commits the exact held reward consumption together with its
accepted Upgrade state before optional callbacks or presentation. Required combat
refresh is distinct from notification. A consumed view stays non-interactive even
if refresh, notification, or destruction fails. A new drag is rejected until the
accepting interaction has completed its outer cleanup; Battle stop/release remains
permitted. Terminal reward snapshots precede destructive Stage cleanup.

---

# 2. Battle UI Composition

One authored Battle UI layer may group the battle HUD, Monster status presentation, and damage-number presentation.

The layer is a composition boundary, not a runtime owner or gameplay service locator. Gameplay systems communicate only with the presentation capability they require.

The Draft Window remains an authored part of the battle UI while closed. Opening a Draft creates transient choice items; closing it removes only those transient items and returns the window to its closed state.

Monster status displays and damage numbers remain owned by Monster System even when rendered on the same UI surface.

Main menu, Stage Introduction, Stage Victory, and Stage Defeat presentation belong to Game Flow System. Sharing one visual canvas or screen with battle-local UI does not make those surfaces part of Battle HUD UI System.

---

# 3. Player Runtime Display

The first-version HUD displays:

| Information | Source |
|---|---|
| Current Player Level | Player System |
| Progress Toward Next Level | Player System |
| Current Health | Player System |

Presentation updates when the owning gameplay state changes. The HUD must not derive level progression, calculate damage, or decide defeat.

---

# 4. Draft Window

Draft System supplies one active set of choices. Battle HUD UI System presents that set and returns one player selection.

```text
Draft Choices Supplied
    -> Validate And Open Draft Window
    -> Report Successful Opening
    -> Present Distinct Choices
    -> Player Selects One Choice
    -> Return Selection Intent
    -> Owning Draft Session Commits Or Rejects The Selection
    -> Close Draft Window After Accepted Commit Or Lifecycle Cleanup
```

The UI cannot create, replace, reroll, weight, or validate Draft candidates unless a future Draft rule explicitly grants that action.

The same Draft Window presents both the required Initial Tower Draft and later Player level-up Drafts. When fewer distinct eligible choices exist than the configured display count, the window presents only the available choices; one-choice and two-choice Initial Drafts are valid authored outcomes and do not require placeholder entries.

The Initial Draft is complete only after one valid selection has been returned and the held Tower Draft item has been created. Technical closure, Stage cleanup, or disabled presentation must not be treated as Initial Draft completion or authorize Monster Wave execution.

While the Draft Window is open, it owns the active interaction surface. Pointer input must not pass through it to Camera pan, Tower placement, or other battlefield interaction.

Draft System owns the battle-simulation pause associated with an open Draft Window. Battle HUD UI System remains interactive while simulation is paused and must not acquire, restore, or infer pause ownership from visibility alone. Presentation timing required for Draft interaction must continue independently from paused battle simulation.

A held Draft item enters the pending-item collection only after its presentation and interaction references have been validated and initialization has completed. Failed or partial creation leaves the collection unchanged and does not report a committed Draft result.

Battle HUD authoring supplies two explicit pending-item roots inside the current HUD presentation hierarchy:

- The pending-item container stores inactive held items and owns their layout positioning.
- The drag-visual root temporarily contains the one active dragged item, owns drag ordering and pointer-coordinate conversion, and covers the required battlefield drag space.

Both roots use the same UI coordinate space. The drag-visual root has no layout or content-size authority, is not clipped by a masking ancestor, renders above ordinary Battle HUD content, and introduces no additional input-blocking surface. Pending items receive both roots explicitly and do not discover either root or a Canvas through hierarchy or fallback lookup. Both roots remain Battle HUD presentation ownership and do not become Draft, placement, or shared battle-root services.

---

# 5. Draft Item Interaction Area

The Draft Item Interaction Area displays selected rewards that have not yet been consumed.

Draft owns the read-only collection of Held reward entries. HUD binds each entry to its current view. Before input, the view must still be the current binding and its exact entry must remain consumable. Deployment, Level Up and Upgrade consume the model entry inside their gameplay commit. Replaced or consumed views immediately lose interaction authority; later destruction is presentation cleanup. Rejection and cancellation preserve the entry.

Rebuild cancels active drag and prepares all replacement views before switching bindings. It preserves entry identities, ordering and reservations, and keeps the old views if preparation fails. Ordinary hide or presentation teardown does not clear rewards.

It supports:

- Tower Draft items
- Tower Upgrade Draft items
- Drag interaction entry
- Return-to-area drag cancellation
- Removal after successful consumption

Unconsumed Tower Upgrade Draft items represent pending upgrade capacity and are readable by Draft System during later candidate generation.

Releasing any currently dragged Draft item back inside the interaction area cancels the current drag operation. Cancellation:

- Returns the item to the held-item flow
- Does not invoke placement validation
- Does not invoke Tower level-up or upgrade validation
- Does not consume the item

This rule applies to all present and future draggable Draft item types.

An active held-item drag owns its pointer gesture until release or cancellation. Camera pan must not compete for that gesture even after the pointer leaves the Draft Item Interaction Area.

An Upgrade Draft drag temporarily reparents its held item from the pending-item container to the drag-visual root and places it above ordinary HUD content. End, cancellation, Battle stop, or Stage release returns the item to its original container and sibling position. When the pending-item container owns layout, that layout resumes position authority; the item does not restore a manually captured authored position.

---

# 6. World Interaction Feedback

Battle HUD UI System presents feedback requested by gameplay owners without deciding validity.

## 6.1 Placement Feedback

The UI may distinguish:

- Valid placement
- Invalid placement
- Route-blocking rejection
- Cancelled placement

Tower Placement System owns the result.

## 6.2 Tower Target Feedback

While a Tower-related Draft item is dragged, the UI may present eligible, ineligible, or neutral Tower targets.

Tower Upgrade System owns TowerFamily, level, duplicate, layer-capacity, and maximum-level eligibility. Tower visual presentation owns Tower-local highlighting when that feedback is rendered on the Tower.

---

# 7. Interaction Results

The HUD reports intent or presentation completion; it does not report gameplay success before the owning system accepts the action.

```text
Drag Tower Draft
    -> Placement Or Existing-Tower Intent
    -> Gameplay Validation
    -> Accepted: Remove Ownership And Mark View Consumed
    -> Rejected Or Cancelled: Keep Item
```

```text
Drag Tower Upgrade Draft
    -> Existing-Tower Intent
    -> Upgrade Validation
    -> Accepted: Remove Ownership And Mark View Consumed
    -> Rejected Or Cancelled: Keep Item
```

---

# 8. Validation

Battle UI authoring validation should report at minimum:

- Missing player information presentation
- Missing Draft Window or Draft choice container
- Missing or non-blocking modal Draft interaction surface
- Invalid Draft choice-item presentation or interaction references
- Missing Draft Item Interaction Area
- Missing or unintended pending-item container
- Missing or unintended pending-item drag-visual root
- Pending item initially instantiated outside the authored pending-item container
- Either root outside the current Battle HUD presentation hierarchy
- Pending-item roots using different UI coordinate spaces
- Layout or content-size authority on the drag-visual root
- Drag-visual root clipped by a masking ancestor
- Drag-visual root unable to render the active item above ordinary Battle HUD content
- Drag-visual root introducing an unintended raycast-blocking surface
- Invalid pending-item presentation or interaction references
- Missing interaction feedback references required by current content
- Missing Monster status or damage-number presentation required by the authored composition
- Modal Draft input passing through to Camera or battlefield interaction
- Camera pan competing with an active held-item drag
- Draft presentation unable to remain interactive while battle simulation is paused
- A partial or invalid held item registered in the pending-item collection
- A semantically consumed Pending Draft view that can still begin, continue, or complete pointer/drag interaction

Validation must not create gameplay state or silently replace authored UI.

---

# 9. Approved Scope And Deferred Topics

Current scope includes player information, pause-independent Draft presentation, held Draft items, atomic pending-item registration, drag cancellation, placement feedback, and Tower target feedback.

Game Flow System owns battle-result and Stage-transition presentation, including distinct Victory and Defeat interactions. Those surfaces are outside Battle HUD UI System rather than deferred Battle HUD features.

Deferred Battle HUD topics include:

- Wave and boss warnings
- Pause flow
- Minimap
- Player skills
- Multiplayer status
- General notification feed

Future UI must preserve the same presentation-versus-gameplay ownership boundary.


### Placement submission and membership

HUD and placement interaction retain view/gesture cleanup. Their operation protection lasts through that cleanup, even when submission has returned. Rejected and committed outcomes are distinct; a committed technical failure does not restore the consumed reward. Pending grants and rebuilds reject during submission or outer cleanup.
