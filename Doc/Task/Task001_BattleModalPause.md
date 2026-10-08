# Task001 - Battle Modal Pause

Document Set: Task

Status: Implemented with managed/build verification; native Unity/device acceptance pending.

## 1. Objective And Authority

Establish one exclusive Battle-scoped modal pause authority for Draft and
TowerInfoWindow. Preserve existing Initial Draft, Level-Up Draft, selection,
Re-roll, and failure behavior while removing independent modal simulation-rate writers.

Authoritative contracts:

- [Stage System Section 3.2](../System/02_StageSystem.md#32-battle-modal-pause-lifetime)
- [Draft System Section 3.3](../System/08_DraftSystem.md#33-draft-simulation-pause)
- [Battle HUD TowerInfoWindow](../System/04_BattleHUDUISystem.md#42-towerinfowindow)
- [Game Flow System](../System/01_GameFlowSystem.md)

Implementation order: first. Task002 consumes its owner/handle contract; Task003
uses its modal availability and lifecycle guarantees.

## 2. Scope And Deliverables

- One pause authority owned and explicitly bound by Battle runtime coordination.
- Exact Battle/session identity and a single-use ownership handle for each acquisition.
- Capture, pause, normal release, and outgoing-Battle cancellation behavior.
- Migrate Draft acquisition, ownership checks, and release to this authority.
- Expose read-only modal availability for inspection and gameplay input admission.
- Bounded automated regression evidence and a native pause-verification procedure.

No pause stack, queue, generic time-effects framework, drag slow motion, or player
pause menu is required. This task does not create TowerInfoWindow or add map tap input.

## 3. Existing Integration Points

Inspect live code again before planning:

- `Assets/Scripts/TowerDeployment/DraftSystem.cs`: existing `TryAcquirePause`,
  `OwnsPause`, `ReleasePause`, provisional opening, Re-roll, and cancellation paths.
- `Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs`: fresh Battle
  binding, activation, authority revocation, stop, deferred release, terminal
  observation/publication, and physical runtime release.
- `Assets/Scripts/TowerDeployment/BattleHUDUI.cs`: Draft presentation lifetime.
- Simulation-driven systems and presentation that consume scaled/unscaled time.

These are current seams, not a requirement for a particular new class name.
Runtime owners must receive explicit dependencies; UI hierarchy lookup is not binding.

## 4. Required Behavior

| Operation | Required Result |
|---|---|
| Acquire with a valid active Battle and session, no owner | Capture the current rate once, set simulation rate to zero, return an exact handle |
| Acquire while another owner exists | Reject with no rate, owner, or saved-value mutation; do not queue |
| Query ownership | Match authority, Battle generation, session, and acquisition identity |
| Release the current handle | Clear ownership and restore the captured rate exactly once |
| Duplicate, stale, foreign, or outgoing release | No effect on current ownership or rate |
| Revoke outgoing Battle | Reject new acquisitions immediately; clean outgoing ownership before fresh binding |

Do not assume the previous rate was `1`. Successful opening must establish pause
before interactive modal content is exposed. Failed opening must not retain a pause.
Invalid identities or unusable captured rates produce an explicit failure rather
than silently adopting a default rate.

Draft retains its attempt identity, selection authority, completion record, and
failure rules. Re-roll retains the same pause continuously. Selection closes its
presentation and releases pause before completion publication. A rejected overlap
cannot close or resume the current modal session. Existing Initial Draft failure
must still prevent Wave start through the existing Battle failure/start contract.

Visibility is not pause ownership. Disabling a view or owner must release only its
valid handle. Clear handle state before operations that may reenter cleanup or
publish callbacks so repeated cancellation cannot restore the rate twice.

## 5. Lifecycle And Cleanup

Terminal acceptance, stop, release (including deferred physical release), retry,
replacement, disable, and preparation rollback revoke incoming gameplay authority
before outgoing modal cleanup. Restoration of rate does not reopen combat,
placement, Monster, or Wave gates. Outgoing pause state is cleared before a fresh
Battle receives authority; delayed callbacks cannot change the new Battle.

Preserve terminal evidence ordering and result-neutral Technical Failure behavior.
Cleanup failures must not prevent remaining Battle cleanup or leave the next
Battle paused. Exact cleanup ordering and exception containment belong in the
implementation plan review.

## 6. Plan Review Requirements

The later implementation plan must specify:

- Authority lifetime, handle identity, API, and ownership transitions.
- Every existing Draft rate-write/release path and its migration.
- Opening rollback and reentrant close/disable ordering.
- Revocation versus physical release, terminal evidence, and fresh-Battle reset.
- Which scene simulation clocks already stop at zero and any required corrections.
- How Task002 and Task003 consume pause availability without writing the rate.

Generate and review that plan before implementation; this document is the task
contract, not evidence that the plan or implementation has passed.

## 7. Acceptance And Evidence

Automated checks should cover rates `1` and a non-default positive rate, rejected
overlap in both ownership directions, repeated release, wrong owner, stale Battle,
opening rollback, disable, stop, deferred release, and fresh-Battle acquisition.
Use synthetic inspection ownership until Task002 exists rather than a dummy UI.

Draft regressions include Initial/Level-Up opening, accepted selection, Re-roll
without resume/reacquire, failed opening, asynchronous technical failure, and
stop/retry. Required Initial completion still precedes first Wave Delay.

Native Unity checks verify paused Wave timing, Monster movement, Tower attack
scheduling, released attack entities, Effects, Buff timers, and simulation-driven
presentation while UI remains responsive. Existing Draft/Re-roll interaction must
work after migration. Do not infer native behavior from a managed harness alone.

Record build/static, automated, Unity Play Mode, and device evidence separately;
unexecuted checks remain pending. Completion requires the shared pause contract and
Draft migration to pass, with implementation APIs documented for dependent tasks.

## 8. Implementation Evidence And Handoff

The implementation follows the reviewed chat plan, including retained attack
release facts, shared permission checks before reward commit, phase-specific
presentation-loss routing, and corrected Draft System pause contracts.
The implementation plan remains in the conversation rather than this document.

Implemented capabilities:

- Coordinator-owned `BattleModalPauseAuthority`, bound to the fresh combat-binding
  identity, opened at Battle start, revoked before evidence, and cancelled after
  outgoing gameplay gates and modal cleanup close.
- Immutable acquisition handle, exclusive modal kind/session ownership, exact
  rate capture/restoration, invalid-rate rejection, and no reopening a revoked Battle.
- Draft pause acquisition before UI activation, exact-attempt ownership checks,
  finally-based restoration, failed-opening rollback, and continuous Re-roll pause.
- Explicit root/component/HUD presentation-loss cancellation. Opening loss returns
  synchronous failure; live session loss cleans up before validated Battle failure;
  normal close and outgoing lifecycle cancellation do not report another failure.
- Failed committed presentation close restores pause and terminates the exact
  live Battle instead of stranding Initial completion.
- Paused simulation entry guards, retained attack release and projectile contact
  facts, and zero-delay spawning guards. Cleanup remains permitted during pause.

Task002/Task003 internal API handoff:

| Capability | Usage |
|---|---|
| `BattleRuntimeCoordinator.ModalPause` | Explicit shared authority supplied by runtime binding |
| `TryAcquire(battle, kind, session, out handle, out reason)` | Tower inspection uses `TowerInspection` and its own unique session identity |
| `Owns(handle)` | Interactive ownership; false immediately after Battle revocation |
| `Release(handle)` | Exact retained-owner restoration, including cleanup after revocation |
| `HasRetainedPause` | True while an outgoing modal still holds pause awaiting cleanup |
| `CanAcquire` | True only for a live binding without a retained owner |
| `CurrentKind` | Retained modal kind, even when interactive ownership has been revoked |
| `IsBattleOpen(battle)` | Validate exact active Battle identity at reentrant boundaries |

Do not infer availability from `Owns == false` or view visibility. No window or
Tower tap implementation is included in Task001. The authored Draft root receives
a runtime-bound `DraftPresentationLifetime`; no Canvas layout or prefab reauthoring
is required by this task.

Verification performed:

- Actual pause authority: 81 managed assertions, including both overlap directions,
  non-default/zero/invalid rates, revoked ownership, stale release, and fresh binding.
- Exact production attack and spawning method boundaries: five cases each, including
  paused one-shot release/resume/Stop and same-frame zero-delay spawning cancellation.
- Whole production DraftSystem: 552 Editor and 501 player assertions, including
  shared revocation during Pending preparation, Opening loss, live presentation loss,
  and throwing committed view close. External UI and terminal/native boundaries are doubled.
- Whole production DraftUI: 24 native-boundary assertions; with real card code: 27.
- Existing Submission: 31 contracts; Pending/sampler: 14 cases with 300 baseline traces;
  TowerStateUI: 14 cases plus five extracted coordinator cases; dashed-path integration: 18 cases.
- Editor build and complete non-Editor runtime compilation passed. Managed tests and
  method extraction do not establish native physics, Animator, hierarchy, or coroutine behavior.

Pending native acceptance: Play Mode/device pause/resume at ordinary and non-default
rates, root/component/HUD disable, retained attack/contact recovery, Wave freeze,
Monster/projectile/Orb/Drone/field/Buff behavior, responsive UI, and retry/Stage
transitions. Record these results separately before feature acceptance/merge.
