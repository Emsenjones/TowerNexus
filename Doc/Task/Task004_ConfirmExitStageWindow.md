# Task004 - Exit Stage Confirmation Presentation And Session

Document Set: Task

Status: Implemented after chat plan review and feedback; managed/runtime-compilation
evidence recorded below. Production wiring and native acceptance remain pending.

## 1. Objective And Authority

Provide the separate exit-confirmation view and Battle HUD-owned local session:
open with an exclusive pause, Cancel without losing progress, and expose Restart
and Main Menu intent for later Game Flow integration.

Authoritative contracts:

- [Battle HUD Section 4.3](../System/04_BattleHUDUISystem.md#43-confirmexitstagewindow)
- [Stage Section 3.2](../System/02_StageSystem.md#32-battle-modal-pause-lifetime)
- [Game Flow Section 7.2](../System/01_GameFlowSystem.md#72-player-requested-retry-and-exit-during-battle)

Dependencies: existing shared modal pause and inspection capabilities from
Task001-Task003. New feature order: Task004 ->
[Task005](Task005_ExitStageGameFlowTransitions.md) ->
[Task006](Task006_ExitStageInteractionIntegration.md).

This is a bounded task contract. Review it first, then discuss this task's
implementation plan in chat before authorizing implementation.

## 2. Scope And Deliverables

- Dedicated view with explicit authored presentation references.
- One HUD-owned session with exact Battle identity, local busy/interaction
  ownership, exclusive pause acquisition, and exactly-once cleanup.
- Open, Cancel, failed-opening rollback, and external presentation-loss behavior.
- Exact-session Restart/Main Menu intent handoff with accepted/rejected outcomes
  for Task005 and Task006 to consume.
- Extend the existing pause capability to admit exit confirmation while preserving
  Draft/inspection behavior and a single simulation-rate writer.
- Local validation, focused automated evidence, and dependent-task handoff.

Task004 does not bind production Battle lifecycle, connect the HUD Exit entry,
change Game Flow transitions, or wire the live Prefab. Those are Task005/Task006.
No result-window abstraction, nested modal, or additional dismissal is included.

## 3. Authored Content And Observed Drift

Inspected asset: `Assets/Art/Prefab/Main/Prefab_GameRuntime.prefab`.
Existing root: `Canvas/BattleUIRoot/BattleHUDUI/Window_ExitStageConfirm`.

| Existing element under the root | Observed value | Required behavior |
|---|---|---|
| `Window/Image_Title/Text_Title` | `EXIT STAGE` | Preserve authored title |
| `Window/Text_Des` | `Restart or return to the main menu?` | Preserve description |
| `Window/Button_Cancel/Text_Cancel` | `CANCEL` | Cancel this session |
| `Window/Button_Restart/Text_Restart` | `RESTART` | Restart-current-Stage intent; user correction verified on disk |
| `Window/Button_MainMenu/Text_MainMenu` | `MAIN MENU` | Request existing main menu |

The root itself has a full-stretch raycast-target background Image; no additional
Mask child is required. It is currently saved active, with three distinct Buttons
and empty persistent onClick lists. No dedicated exit view is attached, and no
separately named HUD Exit entry was found. These are static observations, not
proof of native layout/input correctness. Reinspect the latest asset before wiring.

Keep hierarchy, style, and fixed text author-owned. Opening must not replace text
with an earlier suggested warning or Victory/Defeat content. Task006 owns final
hidden/locked startup setup and wiring; do not edit the user's
Prefab in Task004. Keep the result window and its view independent.

## 4. Required Local Behavior

| Operation | Required result |
|---|---|
| Eligible open | Validate supplied Battle/session and view; establish exclusion and pause before exposing interaction |
| Repeated/overlapping open | Reject without replacing a modal or changing its saved rate |
| Cancel | Hide/lock, release only this session's pause, preserve Stage progress and Camera view |
| Restart/Main Menu | Forward exact-session intent; prevent repeated dispatch while acceptance resolves |
| Rejected request | Keep choices usable only if the same session and Battle remain live |
| Accepted request | Lock actions; allow transition revocation/cleanup without ordinary Cancel/resume |
| Failed opening or unexpected live-view disable | Clean owned local state without choosing a transition or publishing a result |
| Stale callback or repeated cleanup | No effect on another session, Battle, view, or simulation rate |

Local admission rejects busy Draft/inspection and protected placement/investment
work using supplied authoritative capabilities. Visibility is not pause ownership.
Background taps do not dismiss. UI remains responsive during pause.

## 5. Lifecycle And Dependent-Task Handoff

Provide explicit current-Battle binding, local cancellation, binding clear, busy
state, and exact-session intent validation capabilities. Task006 supplies live
binding and outgoing lifecycle calls. Keep local cleanup safe before first
activation, after revocation, during callback reentry, and when accepted synchronous
intent releases/replaces the Stage before the request returns.

Task005 consumes current-session intent, not an unqualified click. Task006 consumes
busy state and cleanup guarantees. Record reviewed interfaces after implementation
without declaring production integration complete. No downstream view owns time.

## 6. Plan Review Requirements

The later chat implementation plan must specify view/reference requirements,
session identity and phases, pause extension, inactive-root handling, callback
binding, opening rollback, Cancel/disable distinction, request handoff, reentrant
cleanup order, and dependent-task interfaces. Choose exact APIs and files there;
this task contract is not an approved implementation plan.

## 7. Acceptance And Evidence

Focused checks cover pause-before-interaction, non-default rate restoration,
overlap, stale close, failed validation/show, disable/reentry, text preservation,
duplicate requests, accepted/rejected handoff, and cleanup after revocation.
Use actual view/session/pause behavior where feasible; synthetic Battle/flow
capabilities establish local evidence only.

Native checks verify mask coverage, initial visibility, button response at zero
rate, and unchanged authored content. Task006 owns production input and Stage
switching acceptance. Record automated/build, Play Mode, device, and user visual
evidence separately. Local completion does not imply a connected in-game Exit
entry or end-to-end Restart/Main Menu behavior.

## 8. Implemented Interfaces And Review Handoff

The chat review required distinct request acceptance versus transition success,
exact validation during AwaitingTransition, exception recovery based on ownership,
separate visible/blocking/interactive state, and independent modal lifetime helpers.
Those corrections are implemented without production flow or Prefab wiring.

`ConfirmExitStageWindow` references only Content Root and three Buttons. Static
Title/Description and button labels remain authored text objects, with no view
text references or runtime text validation. Visual acceptance checks their copy
and layout. The view validates the root Image directly. Attach initializes hidden/locked;
Show exposes the blocking surface with actions locked. Only the HUD enables
actions after revalidation. Authored text is never assigned by the view.

| HUD capability | Contract for dependent tasks |
|---|---|
| `BindExitConfirmation(battle, members, pause, transition, out reason)` | Explicit idle-only binding; missing transition safely rejects requests |
| `TryOpenExitConfirmation(out reason)` | Protected opening, pointer invalidation, validation, exclusive pause, activation revalidation |
| `CancelExitConfirmation()` | Cancel local session while retaining Battle dependencies |
| `ClearExitConfirmationBinding()` | Revoke local binding, cancel session, detach callbacks, clear dependencies |
| `IsExitConfirmationOpen` | Only the Open phase; not AwaitingTransition |
| `IsExitConfirmationBusy` | Opening/Open/AwaitingTransition/Closing and protected operations |
| `IsCurrentExitStageRequest(request)` | Exact currently submitting unaccepted request, including identity, revision and action |
| `TryAcceptExitStageRequest(request)` | Accept once under the future Game Flow transition guard, before outgoing revocation |

The single request delegate is
`Func<ExitStageTransitionRequest, ExitStageRequestDisposition>`.
The request carries immutable Source, Battle, Session, BindingRevision and Action;
Accepted records ownership transfer. `Accepted` disposition requires the explicit
acceptance handshake, and outgoing authority must be revoked before the delegate
returns or throws. A later execution failure is not a Rejected admission outcome.
Returning without revocation after acceptance reports an exact-Battle failure.

Before acceptance, rejection/exception restores buttons only if the same live
session still owns its pause and presentation. After acceptance, old callbacks
never restore or mutate a replacement session. LateUpdate calls both window
helpers independently with exception isolation, including when no inspection
session exists. HUD disable/destroy isolate each modal's binding cleanup.

Task006 still owns production bind/unbind and terminal/deferred cleanup sites,
Exit entry, complete bidirectional input guards, required-reference preparation
validation, and Prefab wiring. This local session does not provide an in-game entry.

## 9. Verification Evidence And Pending Acceptance

See `Tests/ExitStageConfirmation/Presentation/README.md` for commands and scope.
Managed tests execute the complete exit view/session, shared pause, Battle binding,
and both HUD modal lifetime helpers, with native hierarchy/rendering and transition
execution doubled. Existing inspection assertions remain unchanged. No Task005
Stage transition or Task006 end-to-end gameplay behavior is claimed.

Unity layout/raycast behavior, Play Mode, iPad touch, real Stage cleanup and
user-confirmed visual acceptance remain pending. Runtime compilation and managed
assertions do not establish those results.
