# Task003 - TowerInfoWindow Interaction Integration

Document Set: Task

Status: Task contract generated; implementation plan and review pending.

## 1. Objective And Authority

Connect mouse/touch taps on committed deployed Towers to Task002, preserve Camera
drag behavior, enforce modal input exclusion, and integrate inspection cancellation
with all Battle lifecycle paths. Verify the complete first-version feature.

Authoritative contracts:

- [Battle HUD Section 4.2](../System/04_BattleHUDUISystem.md#42-towerinfowindow)
- [Camera System](../System/06_CameraSystem.md)
- [Tower Placement System](../System/09_TowerPlacementSystem.md)
- [Battle Modal Pause](../System/02_StageSystem.md#32-battle-modal-pause-lifetime)
- [Game Flow System](../System/01_GameFlowSystem.md)

Dependencies: [Task001](Task001_BattleModalPause.md),
[Task002](Task002_TowerInfoWindowPresentation.md), and
[Task002-1](Task002-1_TaskInfoWindowContentRefinment.md), including their reviewed APIs.
Start implementation only after Task002-1's runtime and presentation changes are
complete. Execution order: Task001 -> Task002 -> Task002-1 -> Task003.

## 2. Scope And Deliverables

- One coherent primary-pointer classification path for battlefield tap versus pan.
- Target hit detection using the same rendered battlefield Camera as placement/pan.
- Current Battle membership and availability validation at press and release.
- Explicit production binding to the window/session and shared pause capabilities.
- Guards for Camera, pending-item interaction, placement, and investment submissions.
- Immediate authority revocation and safe cancellation on every outgoing lifecycle path.
- Native mouse/touch, lifecycle, and existing gameplay regression evidence.

No Camera focus, movement speed, return framing, pinch/zoom, click-to-switch while
open, upgrade details, or additional inspection surfaces are included.

## 3. Existing Integration Points

- `Assets/Scripts/Camera/CameraPanController.cs`: primary pointer, UI raycast checks,
  press/release, active Map binding, pan baseline, and rendered Camera reconciliation.
- `Assets/Scripts/TowerDeployment/TowerPlacementSubmission.cs`:
  `OwnsDeployedTower`, `IsBusy`, acceptance guards, and Battle gate/revision.
- `Assets/Scripts/TowerDeployment/TowerPlacementController.cs`: held-item drag,
  placement Camera, preview, final submission, and close-gate handling.
- `Assets/Scripts/TowerDeployment/BattleHUDUI.cs` and pending-item input handlers.
- `Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs`: prepare/start,
  authority revocation, terminal observations, stop, and deferred/physical release.
- Tower prefab/Collider and model replacement authoring, and Task002's manual UI setup.

Inspect these live seams before planning. Do not infer deployed membership from a
tag or build a competing Tower registry. Collider hit mapping must resolve the
owning TowerInstance even when level models or hit children change.

## 4. Pointer State And Admission

Use one primary mouse pointer or touch identity per gesture. Suppress touch-emulated
mouse duplication and ignore extra touches while a gesture is owned. A release
without an eligible press cannot open a window.

| State/Event | Required Behavior |
|---|---|
| Eligible battlefield press | Record pointer, press position, Battle/Map identity, and optional deployed target; do not pan or open yet |
| Movement within threshold | Retain tap candidate; do not pan |
| Movement beyond threshold | Permanently cancel tap; enter pan using existing Map-plane/boundary behavior |
| Release within threshold | Revalidate current identity, modal/placement availability, target membership, and same Tower under pointer; request Task002 opening |
| Release after pan or pointer cancellation | End gesture without opening |
| Modal opening, Battle/Map revocation, focus/input loss | Cancel gesture and baseline; no delayed replay |

The threshold is a finite nonnegative authored screen-space distance with an
explicit unit. Validate it and define the transition sample's pan baseline in the
plan so threshold crossing does not cause a Camera jump or hidden overscroll.
Preserve direct-manipulation bounds and reversal behavior.

UI rejection checks the exact pressing pointer against the current event-routing
authority. Release over UI rejects inspection. Only a valid active Battle and its
committed deployed target qualify; previews, prepared Towers, result-screen Towers,
and outgoing Towers cannot open the window. Movement exceeding threshold remains
a drag even if the pointer returns to its starting position.

## 5. Modal Input And Investment Guards

Draft and inspection cannot overlap. Task001 pause acquisition is the final
exclusive check, with preflight rejection before content work where possible.
Inspection also rejects during held-item dragging, placement submission, and
protected outer acceptance cleanup.

While TowerInfoWindow is open, its mask intercepts pointer interaction and runtime
guards reject Camera pan, pending-item gestures, deployment, Tower level-up/Upgrade
requests, and taps on another Tower. Direct/debug investment paths capable of
changing the displayed Tower must obey the same modal restriction. Rejection
does not consume or rearrange pending rewards or change occupancy or Upgrade state.

Only Close dismisses normally. A background click, Escape/back shortcut, or another
Tower tap does not introduce an unapproved dismissal. Closing does not replay
blocked gestures or begin dragging an underlying control on the same release.

## 6. Battle Binding And Lifecycle

Explicitly bind the inspection owner to the current Battle, membership authority,
Task001 pause authority, Task002 view, and output Camera. Fresh binding cannot reuse
an outgoing session or pointer candidate. Integrate missing-reference validation
with the existing Battle preparation contract rather than silently discovering UI.

Terminal acceptance, stop, technical failure, release (including deferred physical
release), retry, Stage replacement, target removal, and relevant owner/view disable
cancel inspection. Invalidate input and opening authority immediately, then clean
the view, content, target, and owned pause. Physical cleanup may finish later;
outgoing callbacks cannot reopen a window or change a newer Battle's rate.

Preserve terminal evidence ordering, existing result-neutral failure routing,
deployed Tower retention on result screens, Tower level-status lifetime, and
pending-item/placement cleanup. Inspection closes at Battle stop even if Towers
remain visible. Closing never changes Game Flow state or reactivates combat.

## 7. Plan Review Requirements

Specify the single pointer authority and how existing pan adapts, hit query and
Collider authoring, touch/mouse de-duplication, threshold units, and cancellation.
List each raw-input and investment path needing a modal guard and its exact owner.
Specify lifecycle revocation before deferred cleanup and the binding/validation
changes needed for user-authored UI. Keep Task001/Task002 responsibilities intact.

Review the plan against live code and dependent APIs before implementation.

## 8. Acceptance And Evidence

Automated checks should exercise tap success, movement just within/beyond threshold,
drag-return-to-start, release over another Tower/UI, extra touch, cancelled pointer,
modal admission between press/release, target removal, stale Battle/Map, and busy
placement rejection. Verify rejected investment paths preserve rewards and Tower state.

Native acceptance includes:

- Mouse and actual touch taps open the correct Tower's latest values and Kill count,
  including current resolved Attack Cycle Duration displayed in seconds,
  with acquired Upgrade names/icons and the correct layer backgrounds; Description
  is absent. Verify kills earned through actual direct and Buff/Effect damage.
- Dragging from a Tower pans without opening; ordinary empty-map pan and bounds work.
- UI presses, Draft, Pending Item drag, and placement cleanup cannot open inspection.
- The full-screen mask blocks Camera, pending items, investment, and target switching.
- Close restores the previous rate; background taps do nothing; Camera view is preserved.
- Battle simulation is frozen while window UI remains responsive.
- Repeated close/reopen and target switching do not retain old icons or values.
- Stop, terminal failure, external disable, deferred release, retry, and Stage replacement
  leave no outgoing view, target, gesture, or pause; result-screen Towers cannot open it.
- Initial Draft, Level-Up Draft, Re-roll, deployment, level-up, ordinary Upgrade,
  route feedback, and Tower level-status behavior retain their existing contracts.

Managed tests/build and static inspection do not replace Unity/device checks.
Record actual device and user layout acceptance explicitly; leave unexecuted cases
pending. Feature completion requires Task001, Task002, Task002-1 and Task003 acceptance evidence and all
required manual UI references to be assembled and verified before merge to main.
