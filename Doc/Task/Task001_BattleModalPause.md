# Task001 - Battle Modal Pause

Document Set: Task

Status: Task contract generated; implementation plan and review pending.

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
