# Task002 - TowerInfoWindow Presentation

Document Set: Task

Status: Implemented with managed/build evidence; manual UI assembly and native acceptance pending.

Content amendment: [Task002-1](Task002-1_TaskInfoWindowContentRefinment.md) is
implemented with separate managed/compilation evidence before Task003. The current
implementation removes Description, displays Kill count and uses named/layer-styled
Upgrade items; the final content and assembly contract below includes that amendment. Historical evidence in
Sections 8 and 10 does not establish acceptance of that amendment.

## 1. Objective And Authority

Present the latest committed information for one deployed Tower in the authored
TowerInfoWindow, pause through Task001, and clean the window safely on close or
cancellation. Provide an explicit opening capability for Task003.

Authoritative contracts:

- [Battle HUD Section 4.2](../System/04_BattleHUDUISystem.md#42-towerinfowindow)
- [Tower Framework Section 6.2](../System/10_TowerFrameworkSystem.md#62-tower-inspection-data)
- [Tower Runtime Combat](../System/11_TowerRuntimeCombatSystem.md)
- [Battle Modal Pause](../System/02_StageSystem.md#32-battle-modal-pause-lifetime)

Dependency: [Task001](Task001_BattleModalPause.md). Task003 integrates player entry
and production Battle lifecycle. This task owns local opening rollback, close,
disable, target binding, and generated-content cleanup.
Execution order: Task001 -> Task002 -> Task002-1 -> Task003.

## 2. Scope And Deliverables

- Window script with explicit author-assigned presentation references.
- Read-only current resolved combat-stat query and a coherent inspection snapshot.
- Target/session ownership, pause acquisition/release, and reliable opening rollback.
- Upgrade icon generation in acquisition order and local content cleanup.
- Close-button integration and configuration diagnostics.
- A documented UI assembly checklist for the user's manual layout and binding.

Map hit detection and tap-versus-pan classification belong to Task003. Camera
focus, zoom, upgrade details, unacquired icons, secondary popups, per-frame display-data polling,
Tower editing, and a generic window framework are outside this task.
Presentation-frame lifecycle checks are in scope and remain active during pause.

## 3. Data Contract

| Display | Source |
|---|---|
| Title | Fixed author-edited text; opening does not overwrite it |
| DisplayName | `TowerInstance.TowerDefinition.DisplayName` |
| Icon | `TowerInstance.TowerDefinition.Icon` |
| Level | `TowerInstance.CurrentLevel` |
| AttackRange | Current committed resolved Attack Range from `TowerCombatBehaviour` |
| Attack | Current committed `ResolvedBasicDamage` from `TowerCombatBehaviour` |
| AttackCycleDuration | Current committed `AttackCycleDuration` from `TowerCombatBehaviour`, including Upgrade changes, in seconds |
| Kill count | `TowerInstance.KillCount`, added by Task002-1 |
| Upgrade items | Acquired Upgrade display name, Icon and TowerUpgradeLayer |

Attack includes level BasicDamage and applied Basic Damage Bonus deltas, before
source-specific DamageScale. Do not substitute `LevelBasicDamage`, final damage
for one damage source, or DPS. Range includes applied Upgrade changes.

The existing combat cache is private and resolved-stat access is protected.
Provide a read-only capability that verifies the target binding and an established
committed baseline; reading UI data cannot initialize, mutate, or refresh combat.
Unavailable data rejects opening with diagnostics instead of fabricated zero values.
Use the current Battle's authoritative deployed membership, not component existence.

Capture level, identity, resolved values, and an independent ordered Upgrade list
for one opening. Revalidate target/Battle/session after work that can reenter
lifecycle code. Reopening after accepted level or Upgrade changes displays new values.
No regular per-frame stats polling is needed while simulation is paused.

Numeric formatting must be defined in the plan and applied consistently. Keep
finite valid values accurate; do not convert a float to an integer merely for layout.
Blank optional identity text may remain blank. Specify missing-icon diagnostics and
presentation behavior without dropping an acquired Upgrade or reusing another icon.

## 4. Authored UI Assembly Contract

The user creates and positions the UI and assigns references manually:

- Existing UI Canvas, initially inactive full-screen common TowerInfoWindow root
  containing both mask and content. TowerInfoWindow is attached to this common root;
  the existing BattleHUDUI manages the active session.
- Fixed Title text and Basic Stats parent containing individual text/Image rows.
- References for DisplayName, Icon, Level, AttackRange, Attack, AttackCycleDuration and Kill count.
- Upgrade Info parent using Grid Layout and a TowerUpgradeInfoItem Prefab with
  name text, icon Image, background Image and three layer-background Sprites.
- Close button.
- Full-screen semitransparent black Image mask below window content, above underlying
  HUD surfaces, with Raycast Target enabled.

The script must support opening an initially inactive window through an explicitly
bound active session owner. Do not require the hidden view's Update or OnEnable to
discover dependencies or accept the opening request. Bind Close once and avoid
duplicate listeners across activation cycles.

Generate one named, layer-styled item per acquired Upgrade, in acquisition order. Empty upgrades leave
an empty container. Own and remove only generated icon instances, preserving authored
children/templates. Deferred destruction must not leave old items visible or counted
by layout during immediate close/reopen. Icons provide no click action.

Provide assembly instructions and diagnostics; do not automatically generate or
redesign the user's Canvas layout. Native checks become complete after user assembly.

## 5. Session And Close Contract

Validate eligibility, references, snapshot, and prepared content; acquire Task001's
exclusive pause; then activate the populated window. No partial visible content
or acquired pause survives an opening failure or cancellation during activation.
Only one target/session is current. A second open cannot replace an existing target.

Close hides content, retires generated icons, clears target/session state, and
releases only the exact current pause handle. A mask click has no action. Explicit
Close, disable, disposal, and target invalidation converge on idempotent cleanup;
reentrant OnDisable cannot release twice or clear a newer opening.

Expose current inspection/modal availability to Task003. Window visibility alone
is insufficient authority. Production Battle end, stop, release, retry, and Stage
replacement wiring belongs to Task003 and calls this task's cancellation capability.
Neither opening nor closing moves the Camera or mutates Tower/reward state.

## 6. Plan Review Requirements

Specify view versus active-session owner, the inactive-root opening path, snapshot
and combat-query API, deployed-membership validation, reference failure behavior,
numeric formatting, icon instance ownership, listener lifetime, and reentrant
opening/closing rollback. Describe how target removal and external disable notify
or invalidate the session even while simulation is paused.

Define the Task003-facing opening/cancellation interfaces and which session owns
the Task001 handle. Avoid introducing a second pause writer or duplicate Tower registry.

## 7. Acceptance And Evidence

Automated checks cover a base Tower, changed level, range/damage Upgrades, no
Upgrades, several acquired Upgrades in order, switching targets after close,
repeated open/close, missing required references, invalid/uncommitted target data,
content-generation failure, pause rejection, activation cancellation, and disable.
Every rejected/closed case leaves no generated items, target binding, or pause.

Native Unity checks verify manual references, Title preservation, text/image
correctness, Grid Layout, immediate reopen without duplicate icons, full-screen
mask ordering, Close responsiveness during pause, and unchanged Camera view.
Task003 verifies production input blocking and lifecycle end to end.

Record automated/build evidence separately from Play Mode/device and user-confirmed
layout acceptance. Completion requires the data/view/session capabilities and
assembly checklist; pending manual assembly is reported explicitly, not marked passed.

## 8. Implemented Interfaces And Reviewed Boundaries

This section records the original implementation; Task002-1 extends its snapshot
and item preparation without changing the HUD session/pause interfaces.

`BattleHUDUI.TowerInspection.cs` is part of the existing partial BattleHUDUI
component. It adds one serialized TowerInfoWindow reference, manages Opening/Open/
Closing and an outer operation guard, and uses Task001's actual BattleCombatBinding
identity. No additional Controller scene component is introduced.

| Interface | Meaning |
|---|---|
| `BindTowerInspection(battle, members, pause, out reason)` | Explicit bind while idle; rejects active sessions/outer operations. Finish Cancel/Clear before replacing a binding |
| `TryOpenTowerInfo(target, out reason)` | Validate deployment, operation availability, view references and snapshot; prepare hidden content, acquire pause, activate, revalidate |
| `CloseTowerInfo()` / `CancelTowerInspection()` | End the current session; retain the current Battle dependencies |
| `ClearTowerInspectionBinding()` | Revoke opening permission, cancel session, detach view listeners and clear dependencies |
| `IsTowerInfoOpen` | True after successful activation and freshness checks |
| `IsTowerInspectionBusy` | Includes Opening, Open, Closing and outer operation cleanup |

Task003 must pass the exact coordinator combat binding, `Submission`, and
`ModalPause`, bind before player inspection becomes available, and clear before
outgoing Stage replacement. Production map-input and Coordinator binding wiring
remain Task003 work. Inspector assignment alone does not introduce a click entry.

`TowerCombatBehaviour.TryGetInspectionStats` reads an established committed cache
without recomputing it. Each accepted level/Upgrade baseline and initial baseline
establishment increments a revision. `TowerInspectionSnapshot` captures level,
revision, identity text/icon, and an independent ordered Upgrade/icon sequence;
preparation and activation must preserve them. Stale snapshots roll back without retry.
`BattleCombatBinding.IsOpenForRead` is side-effect-free, unlike failure-reporting
`IsUsable`. Open-window frame checks inspect lifecycle, not displayed combat values.

The view owns one PreparedContent record per opening. Disposal hides/detaches only
generated icon instances before deferred destruction. Missing Sprites leave empty
Image slots with diagnostics. Level is integer text; range/Attack use invariant
`0.##` formatting. Title is neither referenced nor overwritten by the script.

Both mask and content are children of the same root. External root/component
disable notifies HUD; loss of content validity is detected on a presentation
frame. Close disarms callbacks before hide. CanvasGroup suppresses visibility and
raycasts before root deactivation; cleanup always releases the exact pause in finally.
A failed modal hide reports failure through the exact live Battle identity. HUD
disable isolates inspection, subscription, placement, and Draft cleanup exceptions.

## 9. Manual UI Assembly

Create this hierarchy under the existing full-screen Battle UI Canvas:

```text
Window_TowerInfo (inactive; RectTransform; TowerInfoWindow)
  Mask (Image; full stretch; semitransparent black; Raycast Target enabled)
  Content (RectTransform; active locally)
    Title (fixed authored text)
    BasicStats (active parent)
      DisplayName / Level / AttackRange / Attack / AttackCycleDuration / KillCount (TMP_Text)
      Icon (Image)
    UpgradeGrid (active parent; GridLayoutGroup)
    Close (Button)
```

1. Stretch the common root to a full-screen UI parent with zero offsets. Stretch
   Mask to that root with zero offsets; put Mask before Content in sibling order.
   Keep child objects active locally; only the common root starts inactive.
2. Assign Content Root, all six text references including Attack Cycle Duration and Kill count, Tower Icon, Upgrade Container,
   and Close Button on TowerInfoWindow. BasicStats is optional layout organization;
   no container reference is required. Mask has no script reference: manually
   verify full-screen coverage, sibling order, active state and Raycast Target.
3. After Task002-1, save the upgrade UI object as a Prefab with TowerUpgradeInfoItem.
   Assign its icon/background Images, name text and Basic/Behaviour/Elemental
   background Sprites, then assign the typed Prefab reference on TowerInfoWindow.
   Remove the Description row and its obsolete binding; add and bind Kill count
   and Attack Cycle Duration text. Cycle duration uses invariant `0.##` plus ` s`.
4. Assign TowerInfoWindow on the existing BattleHUDUI component. Do not disable the
   HUD with the window. No separate Controller component is required.
5. Configure Grid size/spacing and content layout manually. Do not add a background
   dismissal action. CanvasGroup is acquired/created on the common root at binding;
   avoid other scripts competing for its visibility or modal raycasts.
6. After Task003 binding/entry is connected, run the native checks below. Temporary
   test dependencies do not substitute for the production Battle binding.

## 10. Verification Evidence And Pending Acceptance

The following is historical evidence for Task002 and its reference simplification;
Task002-1 records its new 105 presentation assertions and 33 damage/provenance
assertions separately. Native Kill count and named/layer-styled item acceptance
remains pending.

- 74 managed assertions execute the whole production View, HUD inspection partial,
  Snapshot, Battle binding and pause authority, plus exact production combat query
  and baseline-commit methods. Native activation/graphics and membership are doubles.
- Cases include latest level/resolved stats, ordered/empty/missing icons, Title,
  preserving authored children, immediate reopen, membership rejection, invalid
  baseline/reference, halfway generation failure, preparation/activation mutations,
  reentrant cancellation, Draft overlap, target destruction, pause revocation,
  old callbacks, listener preservation, HUD exceptions, content loss, and hide failure.
- Existing Draft/Re-roll passed 552 Editor / 501 player assertions, DraftUI 24,
  real-card UI 27; modal pause 81 plus five attack/five spawning boundary cases;
  Effect/binding 27 and Submission 31 cases passed. Assertions were preserved.
- Editor build and complete non-Editor runtime compilation passed.

Pending: user UI assembly, native text/Image/Grid and mask ordering, deferred
destruction, Close interaction during actual pause, no Camera movement, mouse/touch
entry and complete Battle lifecycle via Task003. These checks have not run and are
not inferred from managed doubles or builds. Task001's scene pause acceptance remains
separately pending until native evidence is recorded.
