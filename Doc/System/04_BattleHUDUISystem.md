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
- World-space Monster dashed-line path presentation
- TowerInfoWindow for inspecting one deployed Tower's current information and acquired Upgrades

It observes gameplay state and forwards player intent. It does not own Player state, Draft generation or reward ownership, Draft-driven simulation pause, placement validation, Tower Upgrade rules, Map topology, Monster runtime, combat results, or Game Flow transitions.

The Tower inspection session owns its target, presentation, and request/release
of its Battle-owned modal pause. It never writes the simulation rate directly.
The shared pause contract is defined in Stage System Section 3.2; Draft retains
ownership of its own session and pause request.

An ordinary Upgrade commits the exact held reward consumption together with its
accepted Upgrade state before optional callbacks or presentation. Required combat
refresh is distinct from notification. A consumed view stays non-interactive even
if refresh, notification, or destruction fails. A new drag is rejected until the
accepting interaction has completed its outer cleanup; Battle stop/release remains
permitted. Terminal reward snapshots precede destructive Stage cleanup.

---

# 2. Battle UI Composition

One authored Battle UI layer may group the battle HUD, Monster status presentation, Tower level status presentation, and damage-number presentation.

The layer is a composition boundary, not a runtime owner or gameplay service locator. Gameplay systems communicate only with the presentation capability they require.

The Draft Window remains an authored part of the battle UI while closed. Opening a Draft creates transient choice items; closing it removes those items and any Draft-owned Toast, resets press feedback, and returns the window to its closed state. Closing presentation does not itself reset the gameplay-owned Re-roll balance.

TowerInfoWindow is another authored, initially hidden window under the UI Canvas.
Its stable presentation references are assigned explicitly by the content author;
opening populates and activates the existing window rather than creating a new
window. Section 4.2 defines its content and interaction contract.

Monster status displays and damage numbers remain owned by Monster System even when rendered on the same UI surface. Tower level status remains owned by Tower Framework System, using a dedicated status container in the Battle UI layer. Its items do not intercept battlefield input; their fixed prefix and numeric level share an authored UI-space offset. See Tower Framework Section 6.1 for display and lifecycle rules.

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

Draft System owns the pause request associated with its exact active session,
using the shared Battle pause authority. Draft presentation remains interactive
while simulation is paused and must not acquire, restore, or infer pause ownership
from visibility alone. Presentation timing required for Draft interaction must
continue independently from paused battle simulation.

A held Draft item enters the pending-item collection only after its presentation and interaction references have been validated and initialization has completed. Failed or partial creation leaves the collection unchanged and does not report a committed Draft result.

Battle HUD authoring supplies two explicit pending-item roots inside the current HUD presentation hierarchy:

- The pending-item container stores inactive held items and owns their layout positioning.
- The drag-visual root temporarily contains the one active dragged item, owns drag ordering and pointer-coordinate conversion, and covers the required battlefield drag space.

Both roots use the same UI coordinate space. The drag-visual root has no layout or content-size authority, is not clipped by a masking ancestor, renders above ordinary Battle HUD content, and introduces no additional input-blocking surface. Pending items receive both roots explicitly and do not discover either root or a Canvas through hierarchy or fallback lookup. Both roots remain Battle HUD presentation ownership and do not become Draft, placement, or shared battle-root services.

## 4.1 Re-roll Presentation And Input

Draft Window authoring supplies:

- Separate available and exhausted Re-roll controls, each with normal/pressed feedback.
- The numeric remaining count inside the available control. No separate remaining-count label is required.
- A reusable Toast template, an explicit Draft-owned presentation container, and
  the message `No other draft choices available.`

The count reads Draft-owned balance when the window opens and after a request
resolves. Positive balance shows only the available control; zero balance shows
only the exhausted control. This visibility rule is separate from permission to
request a Re-roll. Fixed Draft sequence mode and protected refresh/selection
transactions do not accept gameplay requests even with positive balance.

The exhausted control remains responsive to press feedback, but has no gameplay
action and produces no Toast. Both controls reuse ordinary button feedback;
a Re-roll-specific press-state implementation is unnecessary. Authoring owns
layout, typography, resting/pressed images, content offsets, and Toast placement.

Having no other eligible identities does not disable the available control.
A valid click forwards the request so Draft can return No Other Candidates and
HUD can explain it. Press alone changes feedback; an accepted click after
release inside requests the action. Release outside cancels the click. Pointer
exit restores resting feedback; cancellation, disabling, window closure, or
battle termination clears the press state. Offsets do not accumulate.
Finishing the last Re-roll switches to the exhausted control while leaving the
new cards selectable.

---

## 4.2 TowerInfoWindow

TowerInfoWindow lets the player inspect current deployed-Tower information and
acquired Upgrades while considering later investment decisions. It is read-only
and does not consume rewards or change Tower state.

### Opening And Pointer Admission

- Accept one primary mouse click or touch tap only during an active Battle.
- The target must still be a formally committed deployed Tower owned by that
  Battle. Previews, prepared-but-uncommitted Towers, and outgoing Towers are invalid.
- Confirm the tap on release after a press on that target, with movement within
  the click threshold and the same target still eligible under the pointer.
  Crossing the movement threshold commits the gesture to Camera pan and permanently
  cancels Tower inspection for that gesture, even if the pointer later returns.
- A press over UI, an active Draft, a held-item drag or placement transaction,
  another modal surface, or an already open TowerInfoWindow prevents opening.
  Release rechecks eligibility; cancellation and Battle replacement clear the gesture.
- Prepare current target data and valid presentation, acquire the exclusive modal
  pause, then expose the populated window. Failed opening leaves no visible partial
  window, generated items, retained target, or owned pause.

### Authored Content And Data

One initially hidden common root contains both the full-screen mask and window
content; hiding, disabling, or destroying this root ends their shared visibility.
The authored TowerInfoWindow view lives on that root; the continuously active
Battle HUD owns inspection sessions, target binding, and the pause handle.

The author explicitly supplies a fixed Title, a Basic Stats parent with individual
text/image references, an Upgrade Info parent with Grid Layout, one Upgrade item
UI template containing name text, icon and background Images plus authored Basic,
Behaviour and Elemental background Sprites, a Close button, and a full-screen semitransparent
black mask below the window content. The mask blocks raycasts and covers underlying
battle HUD controls as well as the Map. Window content remains interactive above it.

| Display | Authoritative Source And Meaning |
|---|---|
| Title | Fixed authored window name |
| DisplayName | Target TowerDefinition.DisplayName |
| Icon | Target TowerDefinition.Icon |
| Level | Target TowerInstance.CurrentLevel |
| AttackRange | Current resolved Attack Range supplied by Tower Runtime Combat, including applied Upgrade changes |
| Attack | Current resolved BasicDamage supplied by Tower Runtime Combat: current level BasicDamage plus applied Basic Damage Bonus deltas |
| Kill count | Target TowerInstance.KillCount: cumulative attributed enemy deaths for this runtime Tower |

Description is omitted from this compact window; TowerDefinition.Description
remains available elsewhere. The Basic Stats parent is layout organization and
the mask is manually authored; neither requires a dedicated view-script reference.

Attack is the current Tower attack baseline. Individual attack and Effect results
apply their own DamageScale at the damage boundary; the display is not a DPS or
aggregate damage estimate. UI reads the resolved value without recalculating combat
rules or accessing mutable combat-cache internals.

Every opening reads a coherent snapshot of the target's latest committed level,
resolved values, and acquired Upgrades. Gameplay cannot mutate these through the
window while paused, so continuous per-frame data polling is unnecessary.
Preparation and activation may invoke authored code: before pause acquisition and
after activation, compare level, committed combat-baseline revision, and ordered
Upgrade identities/names/icons/layers and Kill count with the captured snapshot. A changed snapshot cancels
that opening without automatic retries. Presentation-frame lifecycle checks remain
permitted while paused; they do not refresh displayed stats.

Generate one display-only item for each entry in TowerInstance.AppliedUpgrades, in
acquisition order, assigning its display name, Icon and TowerUpgradeLayer.
Each item selects the corresponding authored Basic, Behaviour or Elemental
background; its presentation owns no Draft or investment behavior. Missing icons
retain slots with diagnostics. Invalid required item/background configuration
rejects hidden preparation rather than substituting another layer's background.
The Grid Layout controls positioning. No acquired Upgrades means an empty container.
Remove previous generated items before binding another target. Icons are display-only;
unacquired upgrades, upgrade details, and secondary popups are outside this scope.

### Modal Lifetime And Closing

While open, only the window's own UI accepts player interaction. Camera pan,
pending-item interaction, placement, level-up/Upgrade submission, and tapping another
Tower are blocked both by the mask and by runtime admission checks. Draft and Tower
inspection cannot overlap. Seeing another Tower requires closing the current window.

Close is the sole normal player dismissal. Clicking the mask does nothing. Closing
hides the window, removes generated icons, clears the target, and releases exactly
that inspection session's pause, restoring the previously captured simulation rate.
Opening and closing perform no Camera movement, zoom, or framing restoration.

Battle end/stop, release, retry, Stage replacement, target removal, and external
window disable also cancel inspection and clean its content and pause ownership.
Cleanup is idempotent and cannot change a newer session's state, restart a stopped
Battle, or manufacture a Battle result. Visibility alone is never pause authority.

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

### Monster Dashed-Line Path Presentation

Battle HUD UI System renders one dashed line for the ordered main route
requested by Tower Placement System. The line begins at the Spawn Grid center,
ends at the Target Grid center, and follows the intervening Grid centers in
Map space. Its world-space presentation is independent of the screen-space
Pending item layout and uses a separate Map-space presentation object. Battle
HUD ownership describes presentation responsibility, not screen-space layout.
Camera pan changes the view of the line and Map together; it does not change
the route or require screen-coordinate synchronization.

| Requested State | Color | Opacity |
|---|---|---|
| Normal | Configured Normal RGB; default white | Configured Normal Alpha; default 0.5 |
| Blocked | Configured Blocked RGB; default red | Configured Blocked Alpha; default 0.5 |

Blocked changes the tint and opacity of the retained route, with no
path-geometry update. The display layer never substitutes a new route, selects
a shortest path, or infers deployability from color. Monster System owns route
data; Tower Placement System owns the complete state transitions described in
`09_TowerPlacementSystem.md`, Section 7.5.

Reusable line authoring controls width; Material/Texture authoring controls
opaque dash versus transparent gap shape and repetition density. The presenter
owns independently configurable Normal and Blocked colors, including each
color's Alpha, plus surface-relative height. Normal has the same appearance
for formal and valid candidate routes. No separate preview opacity or authored
line color gradient overrides these state colors. Alpha ranges from 0
(transparent) to 1 (opaque); both colors default to Alpha 0.5. Grid
alignment and height respect the Map coordinate frame. The line remains
readable against Map surfaces without changing Grid identity or topology.
Route length changes do not stretch the authored pattern.

The pattern flows from Spawn toward Target in both display states without
moving route geometry. Material authoring controls a finite nonnegative speed;
zero disables motion. Flow advances with battle simulation time, freezes with
Draft-induced simulation pause, and resumes from the retained phase. State and
route replacement preserve the phase; clear or a new Battle resets it. Shared
visual assets are not modified by an individual runtime's motion. The first
version has no directional arrows or smoothed route curves. Exact material,
shader, and geometry-generation techniques remain implementation choices.

The line has no collision or input authority and cannot intercept Tower drag,
Camera pan, or modal Draft input. Opening or closing a Draft Window does not
itself change the underlying line's route or requested state. The modal window
continues to own input, and the current line remains beneath its presentation.
Simulation pause does not clear the current line, change its state, or reset
its flow phase; it freezes flow until simulation resumes. Battle end or Stage release
clears the display and retained presentation data; late outgoing requests
cannot restore the line for that Battle or overwrite a newer Battle's line.

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
Validation reads the raw authored values, including the sum of delay and
duration; it must not silently clamp invalid timing into a valid step. An empty
or fully disabled timeline rejects playback rather than waiting indefinitely.

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

A new play request cancels the previous playback before validating its new
configuration. Failure leaves it stopped with no completion notification; the
caller owns failed-instance cleanup. Zero-duration steps apply their target at
their scheduled time. A valid timeline with total duration zero completes on
the next effective playback update, after the play request returns. Cancellation
or replacement before that update revokes the old completion. Same-property
steps may meet at an endpoint regardless of list order, and an update spanning
several boundaries must produce the state at the resulting timeline time.

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
- Missing TowerInfoWindow content references, Close button, icon template, or full-screen blocking mask
- Inspection of a preview, uncommitted, removed, or outgoing Tower
- A drag also opening TowerInfoWindow, or a stale pointer release opening it after a modal/lifecycle change
- Tower information disagreeing with current committed level, resolved combat values, or acquired Upgrade order
- Failed opening or repeated cleanup leaving generated icons, target bindings, or a pause behind
- TowerInfoWindow input leaking to Camera, pending items, placement, or another Tower
- Draft and TowerInfoWindow active together, or a stale close releasing another session's pause
- Missing Draft Window or Draft choice container
- Missing or non-blocking modal Draft interaction surface
- Invalid Draft choice-item presentation or interaction references
- Missing available/exhausted Re-roll controls, feedback references, or child count presentation
- Remaining-count text disagreeing with Draft-owned balance
- No Other Candidates incorrectly disabling the button or consuming a Re-roll
- Exhausted or superseded Draft controls still accepting gameplay actions
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
- Missing or unusable Monster dashed-line path presentation
- Unusable authored dashed-line width/pattern, missing transparent gaps or invalid repetition density; non-finite state colors or state Alpha outside [0, 1]
- Invalid dashed-line flow speed, flow continuing while simulation is paused, or phase reset on an ordinary route/state update
- Dashed-line presentation that blocks input, shows incorrect state tint/opacity, or replaces retained geometry during Blocked
- A dashed line that remains visible after Battle end or can be restored by an outgoing Battle request
- Missing Monster status, Tower level status, or damage-number presentation required by the authored composition
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
drag cancellation, placement feedback, Tower target feedback, and flowing
world-space Monster dashed-line path presentation, and modal TowerInfoWindow
inspection with current resolved stats and acquired Upgrade icons.

Game Flow System owns battle-result and Stage-transition presentation, including distinct Victory and Defeat interactions. Those surfaces are outside Battle HUD UI System rather than deferred Battle HUD features.

Deferred Battle HUD topics include:

- Wave and boss warnings
- General player pause menu and nested modal windows
- Tower inspection Camera focus and upgrade-icon detail presentation
- Minimap
- Player skills
- Multiplayer status
- Global Toast routing, queued notifications, and a general notification feed

Future UI must preserve the same presentation-versus-gameplay ownership boundary.


### Placement submission and membership

HUD and placement interaction retain view/gesture cleanup. Their operation protection lasts through that cleanup, even when submission has returned. Rejected and committed outcomes are distinct; a committed technical failure does not restore the consumed reward. Pending grants and rebuilds reject during submission or outer cleanup.


## Re-roll Prepared Presentation Ownership

The HUD owns a single-use prepared-view handle. Preparing it does not change the
current cards. Final validation and ownership transfer are separate from presentation;
transfer performs no lifecycle callbacks. Presentation may retire old cards and expose
new ones only while its window ownership remains valid. Disposal is idempotent and
cannot restore an expired window. The exhausted button remains feedback-only.
