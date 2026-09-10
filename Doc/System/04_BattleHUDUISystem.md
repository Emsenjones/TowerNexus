# Tower Nexus - Battle HUD UI System

Document Set: System

---

# 1. Purpose And Ownership

Battle HUD UI System owns battle-local presentation and player interaction surfaces.

It presents:

- Player level and level progress
- Current player health
- Draft choices
- Free Re-roll control, remaining count, and Draft-local Toast feedback
- Selected but unconsumed Draft items
- Drag, placement, and Tower-target feedback

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

The Draft Window remains an authored part of the battle UI while closed. Opening a Draft creates transient choice items; closing it removes those items and any Draft-owned Toast, resets press feedback, and returns the window to its closed state. Closing presentation does not itself reset the gameplay-owned Re-roll balance.

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

Draft System supplies one active set of choices and the remaining free Re-roll balance. Battle HUD UI System presents them and forwards selection or Re-roll intent for the exact current session and choice set.

```text
Draft Choices Supplied
    -> Validate And Open Draft Window
    -> Report Successful Opening
    -> Present Distinct Choices
        -> Player Requests Re-roll
            -> Draft Accepts Replacement: Refresh Choices And Remaining Count
            -> Draft Reports No Other Candidates: Present Toast Without Spending
            -> Continue Awaiting One Selection
    -> Player Selects One Choice
    -> Return Selection Intent
    -> Owning Draft Session Commits Or Rejects The Selection
    -> Close Draft Window After Accepted Commit Or Lifecycle Cleanup
```

The UI requests Re-roll but cannot generate, weight, validate, or independently
replace Draft candidates or decrement the balance. Draft System decides the
outcome and commits the replacement together with its budget cost. Old choices
remain available if replacement preparation fails while the session is live;
new choices become interactive only after acceptance. Superseded views cannot
submit selection or Re-roll intent, even when a reward also appears in the new set.

The same Draft Window presents both the required Initial Tower Draft and later Player level-up Drafts. When fewer distinct eligible choices exist than the configured display count, the window presents only the available choices; one-choice and two-choice Initial Drafts are valid authored outcomes and do not require placeholder entries.

The Initial Draft is complete only after one valid selection has been returned and the held Tower Draft item has been created. Technical closure, Stage cleanup, or disabled presentation must not be treated as Initial Draft completion or authorize Monster Wave execution.

While the Draft Window is open, it owns the active interaction surface. Pointer input must not pass through it to Camera pan, Tower placement, or other battlefield interaction.

Draft System owns the battle-simulation pause associated with an open Draft Window. Battle HUD UI System remains interactive while simulation is paused and must not acquire, restore, or infer pause ownership from visibility alone. Presentation timing required for Draft interaction must continue independently from paused battle simulation.

A held Draft item enters the pending-item collection only after its presentation and interaction references have been validated and initialization has completed. Failed or partial creation leaves the collection unchanged and does not report a committed Draft result.

Battle HUD authoring supplies two explicit pending-item roots inside the current HUD presentation hierarchy:

- The pending-item container stores inactive held items and owns their layout positioning.
- The drag-visual root temporarily contains the one active dragged item, owns drag ordering and pointer-coordinate conversion, and covers the required battlefield drag space.

Both roots use the same UI coordinate space. The drag-visual root has no layout or content-size authority, is not clipped by a masking ancestor, renders above ordinary Battle HUD content, and introduces no additional input-blocking surface. Pending items receive both roots explicitly and do not discover either root or a Canvas through hierarchy or fallback lookup. Both roots remain Battle HUD presentation ownership and do not become Draft, placement, or shared battle-root services.

## 4.1 Re-roll Presentation And Input

Draft Window authoring supplies:

- A Re-roll button with its child label `Re-roll`.
- Active, Press, and Inactive button images, plus the label's authored press offset.
- A separate numeric remaining-count text presentation.
- The fixed label `Free re-rolls remaining: `, arranged with that number by authored layout.
- A reusable Toast template and an explicit Draft-owned presentation container.

The numeric text reads Draft-owned balance when the window opens and after a
Re-roll request is resolved. No Other Candidates keeps the same number. Layout,
button placement, typography, and the Toast's authored location belong to UI
authoring rather than Draft gameplay rules.

| State | Contract |
|---|---|
| Active | Positive balance and a current Natural Draft awaiting input; no effective press. Use the Active image and the label's resting position. |
| Press | An actionable button is effectively held by the pointer inside it. Use the Press image and apply the offset from the resting position. |
| Inactive | Balance is zero, a refresh or selection commit is in progress, or current Draft interaction is unavailable, including Fixed Draft sequence mode. Use the Inactive image, restore the label, and reject input. |

Having no other eligible identities does not disable an otherwise Active button.
A valid click forwards the request so Draft can return No Other Candidates and
the window can explain it. Press alone changes feedback; an accepted click after
release inside the button requests the action. Release outside cancels the click.
Pointer exit restores resting feedback, and release, cancellation, disabling,
window closure, or battle termination clears the press state. Offsets do not
accumulate across presses. Finishing the last Re-roll disables only further
Re-rolls, not selection from the new choices.

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

## 6.3 Draft-local Toast

No Other Candidates displays `No other draft choices available.` using an
authored reusable Toast template. The Draft Window owns its container, the
runtime instance, and cleanup. This is a local feedback surface, not a global
notification service or queued feed.

The Toast fades in, stays briefly visible, fades out, and is removed after its
complete animation. Position motion can run concurrently, such as a small eased
upward movement. The authored outer placement stays separate from the animated
content so playback does not take over layout positioning.

The same window keeps at most one Toast instance. Repeated requests reset it to
its authored animation start values and replay it; they neither stack instances
nor accumulate displacement. The Toast is visible above the relevant Draft
content but does not intercept pointer input. Closing the Draft, ending the
battle, or releasing its presentation stops playback and removes the instance.
Toast completion or cancellation never changes choices, budget, or rewards.

## 6.4 Reusable UI Animation

UI animation is a reusable presentation capability, independent of the gameplay
owner requesting a visual. It operates on an explicitly authored animated
content target and opacity target, and supports Position, Scale, and Fade steps.
Each enabled step defines start and target values, duration, delay, and easing.
Delay is an offset from the start of the whole playback, not a wait after the
previous listed step. Duration and delay must be finite and non-negative.

Different properties may animate concurrently. Steps controlling the same
property must not overlap in time; successive steps may meet at an endpoint.
Ambiguous or conflicting timelines are authoring errors. At playback start,
each animated property takes the start value of its earliest enabled step by
timeline time, rather than the last step encountered in a list. At each later
step's scheduled start, its configured start value applies to that step; gaps
hold the preceding state. Authors use matching end and start values for smooth
continuity. A later Fade-out step must not overwrite Fade-in's initial opacity
before its scheduled time.

Playback completes after the last enabled step's delay plus duration. Replaying
stops the previous playback, restores animation start values, and establishes
one new completion. Cancellation stops further visual updates and does not
report normal completion. Completion reports to the owning presentation, which
decides removal or reuse; shared playback does not own text, gameplay state,
object lifetime, or notification routing.

Reusable UI playback defaults to presentation time independent of battle pause
or battle speed. Draft Toasts use that mode. An explicit simulation-time mode
supports combat-linked visuals; damage numbers retain their existing
simulation-time behavior when sharing playback. UI animation must not write or
release the battle simulation rate. Reusing this capability preserves authored
damage-number content, references, and motion rather than moving Monster
presentation ownership into Draft or HUD.

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
- Missing Re-roll button, state images, press target, or remaining-count presentation
- Remaining-count text disagreeing with Draft-owned balance
- No Other Candidates incorrectly disabling the button or consuming a Re-roll
- Inactive or superseded Draft controls still accepting input
- Press offsets accumulating or surviving cancellation or window closure
- Missing Toast template or unintended presentation container
- Toast intercepting input, stacking on repeated requests, or surviving its Draft
- Toast playback stopping or changing speed with paused or slowed battle simulation
- UI animation with missing required targets, invalid timing, or overlapping steps for one property
- Later animation steps overwriting the earliest start values before their scheduled time
- Cancelled playback reporting completion or replay retaining previous displacement
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

Current scope includes player information, pause-independent Draft presentation,
free Re-roll controls and balance display, Draft-local Toasts, reusable Position,
Scale, and Fade animation, held Draft items, atomic pending-item registration,
drag cancellation, placement feedback, and Tower target feedback.

Game Flow System owns battle-result and Stage-transition presentation, including distinct Victory and Defeat interactions. Those surfaces are outside Battle HUD UI System rather than deferred Battle HUD features.

Deferred Battle HUD topics include:

- Wave and boss warnings
- Pause flow
- Minimap
- Player skills
- Multiplayer status
- Global Toast routing, queued notifications, and a general notification feed

Future UI must preserve the same presentation-versus-gameplay ownership boundary.


### Placement submission and membership

HUD and placement interaction retain view/gesture cleanup. Their operation protection lasts through that cleanup, even when submission has returned. Rejected and committed outcomes are distinct; a committed technical failure does not restore the consumed reward. Pending grants and rebuilds reject during submission or outer cleanup.
