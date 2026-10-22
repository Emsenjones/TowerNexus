# Task005 - Exit Stage Game Flow Transitions

Document Set: Task

Status: Implemented after chat implementation-plan review and feedback; managed
and runtime-compilation evidence recorded below. Production wiring is implemented in Task006; see its device evidence and remaining native checks.

## 1. Objective And Authority

Allow an exact current-Battle exit-confirmation session to request full restart
of the same Stage or return to Main Menu through the existing Game Flow owner.

Authoritative contracts:

- [Game Flow Sections 2, 4, 6, and 7.2](../System/01_GameFlowSystem.md)
- [Stage Modal Pause Lifetime](../System/02_StageSystem.md#32-battle-modal-pause-lifetime)
- [Battle HUD Section 4.3](../System/04_BattleHUDUISystem.md#43-confirmexitstagewindow)

Dependency: reviewed/implemented local session and intent contract from
[Task004](Task004_ConfirmExitStageWindow.md). Complete before
[Task006](Task006_ExitStageInteractionIntegration.md) production wiring.
Review this task and discuss its implementation plan in chat before implementation.

## 2. Scope And Deliverables

- Game Flow admission for Restart/Main Menu from the exact active exit session.
- Source validation and explicit accepted/rejected request outcome.
- Same-index complete Stage restart using existing release/preparation ownership.
- Full Stage release and run reset before existing Main Menu presentation.
- Duplicate/stale/conflicting-request protection and safe failure rollback.
- Bounded transition/regression evidence and request-contract handoff to Task006.

No new flow state, result type, scene-loading strategy, persistence, result-view
refactor, or unrestricted Battle retry shortcut is included. Cancel remains
Task004's local operation. Production HUD entry, Prefab binding, and all-system
input/lifecycle integration remain Task006.

## 3. Transition And Admission Contract

`RESTART` is the exit-window label for retry-current-Stage. Existing result-screen
`RETRY` retains its current meaning and legal source state.

| Request / source | Required result |
|---|---|
| Restart from exact active confirmation | Keep Stage index, release outgoing runtime, enter complete preparation |
| Main Menu from exact active confirmation | Release outgoing runtime, clear run, show existing Main Menu |
| Cancel | No Game Flow transition |
| Wrong Battle/session, inactive flow, accepted terminal result, or transition/cleanup busy | Reject without releasing or preparing a Stage |
| Duplicate action or old callback | No second transition |

Validate flow state, Stage/index/composition, active Battle identity and source
ownership. The same coordinator component may survive several Battles; component
identity alone is insufficient. A result-source predicate requiring a stopped
Battle cannot validate voluntary active-Battle requests.

## 4. Restart, Exit, And Cleanup Guarantees

Restart resets Player health/level/progress, Map and Camera framing, Monsters,
Towers, Wave execution, pending Drafts, Re-roll balance, and other Battle-local
state through the existing preparation contract. Honor configured Introduction
and Initial Draft requirements. Do not restart by restoring health, resetting
counters, or reusing outgoing prepared runtime.

Main Menu ends the run; a later new run starts at index zero. Neither voluntary
action claims Victory, Defeat, or Technical Failure or opens a result window.
Preparation failure follows the existing safe Main Menu rollback.

Accepted actions lock confirmation and revoke outgoing gameplay before pause
restoration/destructive cleanup. Do not invoke ordinary Cancel first. An already
accepted terminal transition invalidates exit; there is no second transition.

Synchronous acceptance can release content and prepare a new Battle before the
requester returns. Old callbacks cannot access outgoing state or close/resume
the new Battle. Rejection restores interaction only after exact-session/live-Battle
revalidation. Exceptions follow existing Game Flow containment/rollback rules.

## 5. Existing Owners And Boundary

Inspect GameFlowController's guarded transitions, RetryCurrentStageCore,
EnterMainMenuCore, StageCompositionController release/preparation, coordinator
active/terminal ownership, and Task004's reviewed intent contract before planning.

Game Flow retains transition decisions; Stage retains composition/release;
Battle coordination retains gameplay gates; HUD retains session/pause cleanup.
Task005 supplies the validated transition capability. Task006 connects it to
production ownership without a parallel transition authority or duplicate route.

## 6. Plan Review Requirements

The chat plan must specify request/source identity, acceptance ownership/timing,
result-return semantics, synchronous reentry, duplicate protection, release and
revocation order, existing-core reuse, rollback, and Task004/Task006 interfaces.
Decide the forwarding route, API signatures, and subscriptions in that review;
this contract does not prescribe them in advance.

Task004 implemented handoff: consume `ExitStageTransitionRequest` through the
single `Func<ExitStageTransitionRequest, ExitStageRequestDisposition>` capability.
Validate with `IsCurrentExitStageRequest`, which permits the exact submitting
AwaitingTransition request; do not use `IsExitConfirmationOpen` for that check.
Under the transition guard, call `TryAcceptExitStageRequest` before revocation or
release. Revoke the captured outgoing Battle before returning/throwing. Accepted
means ownership transfer, not preparation success; later rollback stays with
Game Flow and cannot produce a Rejected result that restores the old session.

## 7. Acceptance And Evidence

Automated checks cover same-index complete preparation, configured Introduction,
Main Menu run clear, new-run index zero, wrong source, stale session on a reused
coordinator, repeated/conflicting actions, terminal competition, rejected request,
synchronous replacement, and retry preparation failure. Verify no voluntary
action publishes a result and no outgoing gameplay resumes during switching.

Regress result Retry/Main Menu and victory Next Stage, including final Stage.
Use real transition behavior with bounded Stage/UI doubles as needed and report
their limits. Compilation and automated checks do not establish native cleanup
or timing. Task006 verifies the assembled feature in Unity and on device.

## 8. Implemented Interfaces And Review Handoff

Game Flow's `RequestExitStageTransition(source, request)` returns
`ExitStageRequestDisposition`. Source is the explicitly supplied coordinator;
Task006 injects one delegate from the HUD to this internal entry. There is no new
Inspector reference, multicast request event, or production subscription in Task005.

Admission checks live flow/state/result/Stage composition, the subscribed current
coordinator, Stage lifecycle availability, exact HUD/Battle/session/request identity,
and no accepted terminal claim. Result-screen public entry restrictions are unchanged.

Coordinator capabilities:

- `CanAcceptExitStageRequest(request)` validates the exact active source and no
  prepare/start/release/pending-release operation.
- `TryAcceptExitStageTransition(request)` performs the Task004 acceptance handshake
  and callback-free closure of active admission, combat binding, Submission gate,
  and modal authority. It does not claim a terminal result or clean presentation.
- `IsLifecycleBusy` and `IsRuntimeRetired` distinguish operation availability from
  completed successful runtime release and pause cleanup.

Stage capabilities:

- `CanStartFlowTransition` rejects preparation, begin, release, and pending work.
- `IsRuntimeRetired` requires release completion, no outgoing active/candidate or
  retiring Map objects/bindings, and coordinator retirement. Readiness/begun flags
  alone are insufficient. Maps deactivate before deferred engine destruction.
- Release is protected against reentrant preparation/release and retains interrupted
  Map cleanup references until successful retirement.

`GameFlowController.ExitStageTransitions` holds the transition guard through
acceptance, outgoing release, same-Stage preparation or recovery, and Main Menu
notification. Restart uses existing `PrepareCurrentStageCore` after verified
retirement, retaining Introduction/start/result-buffering rules; it does not reset
individual gameplay fields. Main Menu clears the run after retirement. Existing
result Retry/Next/Menu cores remain in their original admission paths.

Deferred release is issued once and completed by its lifecycle owner. Flow Update
only observes retirement before continuing. If disabled, flow selects cancellation/
recovery; if cleanup is still deferred, the owner retires resources independently,
and re-enable drains the cancelled transaction before admitting a new run.

Exceptions receive one bounded recovery release attempt. A recovery that still
cannot retire resources logs the failure and keeps the transition gate closed;
updates do not spin on repeated release. Once external lifecycle cleanup succeeds,
the waiting transaction can finish. It never presents a falsely safe Main Menu.

Accepted remains ownership transfer even if preparation, callbacks, or recovery
fail. Only rejection before the handshake allows Task004 to restore current choices.
Task006 must clear the outgoing HUD binding after revocation and bind the fresh
session/request capability before admitting the next in-game Exit entry.

## 9. Verification Evidence And Pending Acceptance

`Tests/ExitStageConfirmation/Transitions/run.py` passes 156 assertions executing
the complete production HUD exit session/view, GameFlowController, and
StageCompositionController, plus exact production coordinator acceptance, terminal
claim, lifecycle finalization, revocation, and release methods. Tests explicitly
assemble Task006's HUD bind/clear boundaries; this is not production wiring.
Native hierarchy, Camera, Map creation, and gameplay preparation/start are doubles.

Coverage includes rejection restoring the same paused window, same-index and
nonzero-index restart, fresh health/Re-roll configuration transport, Introduction,
stale requests on reused coordinator/HUD, synchronous new-session survival, no
voluntary result, callback reentry, deferred retirement, bounded failed cleanup,
disable/re-enable, protected Main Menu notification, preparation/callback failures,
new-Battle synchronous result buffering, and existing result/sequence behavior.

Task004 local suite passes 138 assertions. Existing Submission/placement passes
34 contracts; Effect/binding 27 checks; Tower state UI 14 cases plus five coordinator
cases; Draft 556 Editor/505 player assertions; shared pause 81 assertions plus
five spawning/five attack cases. Complete runtime Editor/player compilation passes.
The older Tower-state coordinator extraction harness received the missing Camera
admission endpoint and retirement field; its original assertions remain intact.

Actual Player/Tower/Monster/Wave reset, Initial Draft startup, native Camera/map
retirement, Play Mode/device behavior, and user visual acceptance remain Task006
checks. Configuration-transport assertions and managed doubles do not establish
those native results. No Prefab or production Exit binding was edited in Task005.

## Cross-Task Device Evidence

Task006 records the 2026-10-22 user-reported iPad mini 6 smoke test with no issues
observed after Unity iOS export and Xcode deployment. Earlier pending-native
statements above describe this Task's isolated verification boundary. They do not
negate the later assembled-feature smoke test or establish unreported per-case results.
