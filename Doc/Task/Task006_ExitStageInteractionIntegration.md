# Task006 - Exit Stage Interaction And Lifecycle Integration

Document Set: Task

Status: Implemented after chat plan review and feedback, including production
binding and authored Prefab wiring. Managed/compilation/static evidence recorded;
User-reported iPad mini 6 smoke testing passed; detailed native checks remain bounded below.

## 1. Objective And Authority

Connect the authored exit window and HUD entry to Task004's session and Task005's
transitions, enforce input exclusion, and complete production lifecycle and native
acceptance for Cancel, Restart, and Main Menu.

Authoritative contracts:

- [Battle HUD Section 4.3](../System/04_BattleHUDUISystem.md#43-confirmexitstagewindow)
- [Game Flow Section 7.2](../System/01_GameFlowSystem.md#72-player-requested-retry-and-exit-during-battle)
- [Stage Section 3.2](../System/02_StageSystem.md#32-battle-modal-pause-lifetime)
- [Camera](../System/06_CameraSystem.md), [Draft](../System/08_DraftSystem.md),
  and [Placement](../System/09_TowerPlacementSystem.md)

Dependencies: [Task004](Task004_ConfirmExitStageWindow.md) and
[Task005](Task005_ExitStageGameFlowTransitions.md), including implemented/reviewed
interfaces. Order: Task004 -> Task005 -> Task006. Review this task and discuss its
chat implementation plan before authorizing integration.

Task005 handoff is implemented with managed/compilation evidence. Inject the single
HUD request delegate into `GameFlowController.RequestExitStageTransition(coordinator,
request)` using the explicitly bound coordinator. Preserve Task005's acceptance,
callback-free revocation, retirement wait, and protected recovery/notification.
Task005 tests explicitly assemble bind/clear callbacks; add real production lifecycle
bindings here rather than assuming those test hooks exist in the game.

## 2. Scope And Deliverables

- Eligible-Battle Exit button entry under BattleHUDUI.
- Explicit production Battle/session/pause/request binding and validation.
- All-direction modal, pointer, pending-item, and investment input exclusion.
- Immediate outgoing authority revocation and reliable lifecycle cleanup.
- Preserve/wire user-authored Window_ConfirmExitStage and establish hidden/locked
  startup behavior. The user has corrected the Restart label.
- Integration regressions, native acceptance procedure, and evidence report.

No redesign of Task004/Task005 ownership, duplicate transition route, general UI
system, nested modal, extra pause features, result-window changes, or unrelated
Prefab/layout cleanup is included.

## 3. Authoring And Production Binding

Inspect the user's latest Prefab before wiring. Retain the existing root Image
as blocker when valid; do not require another child Mask or rebuild hierarchy.
Preserve `EXIT STAGE`, `Restart or return to the main menu?`, `CANCEL`, and
`MAIN MENU` and the user-corrected `RESTART`. Native label fit and button layout
require visual review.

Confirm final Exit-entry authoring; provide the entry within this task if still
absent. Wire Task004's dedicated view and explicit references without name lookup.
Remove copied result logic/persistent callbacks if found. Startup cannot expose
an unowned interactive window, even though the inspected root was saved active.

Bind exact fresh Battle, submission availability, shared pause, and Task005 request
capability before admitting entry. Clear outgoing bindings before replacement.
Report missing/invalid required authoring through existing preparation validation
instead of bypassing it or silently manufacturing a fallback window.

## 4. Modal And Pointer Exclusion

Exit cannot open during Draft/inspection Opening, Open, Closing, held-item drag,
placement/investment submission, or protected outer cleanup. Draft and inspection
likewise cannot open during exit modal work. No opening is queued.

Cover exit's full busy lifetime before pause acquisition and through cleanup;
retained pause or visibility alone is insufficient. Inspect shared coordinator
gameplay-modal guards and direct Draft/inspection-only guards in HUD, Camera,
pending items, placement, and investment/debug paths.

Reuse battlefield pointer invalidation. Opening retires tap/pan candidates;
input cannot leak to Camera, held-item gestures, another Tower, or placement.
Closing never replays input. Mask interception and runtime admission must both
hold. Rejections preserve rewards, Tower state, occupancy, and Draft/Re-roll balance.

## 5. Lifecycle And Transition Integration

Integrate prepare/start, terminal acceptance, stop, technical failure, release,
deferred release, retry/replacement, HUD/window disable, and preparation rollback.
Cancel preserves a live Battle. Accepted Restart/Main Menu revoke gameplay before
pause restoration or destructive release. Preserve result and terminal-evidence
ordering.

Outgoing input loses authority immediately even when physical cleanup is deferred.
Isolate cleanup failures so other owners still clean up. Late callbacks cannot
reopen old content, change the next Battle's rate, or request another transition.
Window disable is not an implicit Restart/Main Menu.

## 6. Plan Review Requirements

The chat plan must enumerate production bind/unbind sites, raw-input/investment
guards, pointer invalidation, lifecycle cancellation order, deferred cleanup,
subscription lifetime, authoring changes, and test/build/native procedures.
Resolve interface gaps against Task004/Task005 handoff before integration edits.

## 7. Acceptance And Evidence

Focused integration checks cover entry/session/flow routing, bidirectional modal
exclusion including callback reentry, protected drag/investment rejection, pointer
retirement, invalid authoring, duplicate input, terminal competition, disable,
deferred release, and fresh-Battle rebinding. Regress affected existing pause,
Draft/Re-roll, inspection, investment, Camera, and Game Flow behavior.

Native Unity and iPad mini 6 acceptance includes:

- Eligible Battle opens the authored window with correct copy; Preparing,
  Introduction, and result screens cannot open it.
- Simulation freezes, UI responds, touch/multi-touch input is blocked beneath
  the window, and background taps do nothing.
- Cancel restores the captured rate and identical progress/Camera view without
  gesture replay; repeated opening does not accumulate listeners.
- Restart recreates the same Stage with configured Introduction/Initial Draft
  and fresh Player/Tower/Monster/Wave/Draft/Re-roll state.
- Main Menu shows the existing menu without old gameplay; a new run starts at
  the first Stage. Switching leaves no outgoing window or pause ownership.
- Battle end, failure, disable, and ordinary result/Draft/inspection/investment/
  Camera interactions retain their established contracts.

Record static/build, automated, Play Mode, device, and user visual evidence
separately. Unexecuted native checks remain pending. Local Task004/Task005 success
does not establish assembled-feature acceptance or imply merge/push authorization.

## 8. Implemented Integration And Review Handoff

`BattleHUDUI.ExitEntry` owns the author-assigned Exit Button, startup hide/lock,
idempotent button listener, live entry refresh, and `PresentationEnabled` notification.
Root `Window_ConfirmExitStage` and existing content/buttons/text/icon layout are
retained. Prefab assigns HUD Exit Button and coordinator Game Flow owner, keeps
the window initially inactive, and starts the Exit Button non-interactive. Title
and Description remain static text objects without view bindings.

`BattleRuntimeCoordinator.ExitStageIntegration` subscribes/unsubscribes the explicit
HUD on coordinator enable/disable. `TryBindBattleHUDConsumers` binds inspection,
Camera, and exit as one protected group during preparation. The single exit delegate
captures exact Flow/coordinator/Battle ownership and calls Task005's request entry.
`HasExitConfirmationBinding` validates a hidden prepared binding; opening uses
separate live-flow and operation admission. Group readiness opens only after all
bindings succeed, with identity/revision checks and complete partial rollback.

HUD OnEnable reports through the production notification. A same-Battle, non-busy
owner may atomically rebind consumers without reinitializing gameplay or reopening
the old window. Active failure follows result-neutral failure; prepared failure
uses Game Flow's protected retirement/menu recovery. A terminal callback during
rebind invalidates the operation and cannot run stale recovery against its result.

`OwnsExitConfirmationBinding` / `OwnsTowerInspectionBinding` and identity-scoped
clear overloads retain cleanup ownership independently of UI readiness, owner
availability, or destroyed view objects. Stop/release clears both modal bindings
after gameplay revocation and before final pause cancellation. Late cleanup cannot
clear a replacement Battle's bindings.

Shared modal guards cover Exit Opening/Open/AwaitingTransition/Closing plus group
binding. Camera, pending views, placement entry/drag/release, Draft admission, and
Submission/debug investment gates reject conflicting interaction. The existing
`CanStartOperation` and `IsAvailableForInspection` protected-operation queries
remain independent from these new-player-operation restrictions, avoiding self-rejection.

Callback-free permission closure precedes Camera/path cleanup. Terminal state is
claimed before callback work, and observation protection preserves capture ->
evidence -> stop -> final publication. Reentrant release is deferred until evidence
and stop complete; reentrant stop cannot recapture the observation. The public
parameterless StopBattle API is preserved through its private cleanup core.

## 9. Verification Evidence And Native Acceptance

See `Tests/ExitStageConfirmation/Integration/README.md` for commands and boundaries.

- Production entry/bind/rebind/clear/Flow integration: 76 assertions. Tests use the
  real entry/session/inspection/Flow/Stage and coordinator integration partial,
  plus exact production acceptance, terminal, failure, and cleanup method bodies.
  They do not replace production bind/clear with test callbacks.
- Static Prefab validation passes for exact Flow/Stage/coordinator/HUD/window/entry
  references, initial visibility/lock, distinct controls, no persistent actions, and copy.
- Task004 local view/session: 138 assertions; Task005 transitions: 156 assertions.
- Existing inspection presentation: 109; whole Camera input: 138; Draft: 560 Editor /
  509 player; Submission/placement: 34 contracts; Effect/binding: 27; Tower-state UI:
  14 cases plus five coordinator cases; pause: 81 plus five spawning/five attack cases.
- Complete runtime Editor/player compilation passes with installed Unity references.

On 2026-10-22, the user reported a successful Unity iOS export, followed by
TowerNexus testing on iPad mini 6 through Xcode, with no issues observed. The saved
project version is 1.0.4. This is user-reported device smoke-test evidence; no
per-case checklist, device logs, or screenshots were supplied. It does not establish
individual acceptance of every case in section 7.

Pre-commit static validation found the saved confirmation root active. It was
restored to the agreed inactive default; runtime Awake already hides it. The
device report predates this authoring correction; the corrected default was
verified statically and has not been retested on device.

Detailed native lifecycle, Camera/physics, raycast routing, scene freeze/reset,
Initial Draft/Wave sequencing, multi-touch, and layout checks remain unconfirmed
individually. Managed tests explicitly double those boundaries. Production
OnEnable subscription is present in source; integration directly enables the real
subscription helper and exercises the actual HUD OnEnable notification and binder.
Git submission and merge authorization are tracked separately from validation evidence.
