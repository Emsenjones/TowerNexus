# Task005 - Exit Stage Game Flow Transitions

Document Set: Task

Status: Awaiting individual task review and chat implementation-plan review;
not implemented. Runtime transition acceptance pending.

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
