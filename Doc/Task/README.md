# Tower Nexus Architecture Refactor Tasks

Active Series: `ArchitectureRefactor`
Status: Completed - Task001–Task006 implemented; remaining acceptance explicitly waived for this iteration (2026-09-10).
Branch: `codex/architecture-refactor`
Draft baseline: `10eaf6f` (documentation/evidence retirement complete).

## Review Order

| Task | Scope | Implementation dependency | Status |
|---|---|---|---|
| [Task001 - Upgrade Commit Consistency](Task001_UpgradeCommitConsistency.md) | Exact reward consumption and required combat refresh | Current runtime | Completed - Targeted Unity Acceptance Waived |
| [Task002 - Map Runtime Query Caching](Task002_MapRuntimeQueryCaching.md) | Runtime index and preview-query invalidation | Independent; reviewed after Task001 | Completed - Remaining Targeted Unity Acceptance Waived |
| [Task003 - Pending Draft State Ownership](Task003_PendingDraftStateOwnership.md) | Draft-owned reward data with independent views | Task001 | Completed - Remaining Targeted Acceptance Waived For This Iteration |
| [Task004 - Tower Placement Responsibility Separation](Task004_TowerPlacementResponsibilitySeparation.md) | Input/preview, submission, and deployed membership boundaries | Task001-Task003 | Completed - Remaining Targeted Acceptance Waived For This Iteration |
| [Task005 - Battle Dependency Binding](Task005_BattleDependencyBinding.md) | Explicit gameplay query scope and lifetime | Task004 | Completed - Remaining Targeted Acceptance Waived For This Iteration |
| [Task006 - Combat Diagnostics Isolation](Task006_CombatDiagnosticsIsolation.md) | Optional producer cost and Recorder responsibility separation | Task001-Task005 | Completed - Remaining Targeted Acceptance Waived For This Iteration |

Task numbers are local to this series. Use `ArchitectureRefactor Task001` when
contrasting with the retired `CombatMathV2 Task001`; the identical numeric suffix
does not imply the same task. Suggested new run-name prefix: `AR_TaskNNN_`.

## Review And Execution Rules

- Review each document's proposed ownership, exception behavior, scope, dependencies,
  and acceptance before implementation. Draft text does not supersede System truth.
- After an ownership change is approved, synchronize the relevant durable System
  contracts before implementing it. Keep engine callbacks, method names, and test
  mechanics in Task/tooling documents rather than the cross-engine System contract.
- Implement and validate one bounded task at a time. Reconcile downstream drafts
  with accepted upstream APIs before marking them ready; helper names here are
  proposals rather than permission to add speculative frameworks.
- Preserve numerical assets, balance goals, Draft probabilities, attack timing,
  and route rules unless a separately reviewed design change explicitly owns them.
- Scope directory moves to the task that owns the code, preserve .meta GUIDs, and
  validate serialized references. No separate repository-wide reorganization.
- Record static checks, Editor/player compilation, automated tests, Play Mode,
  and Profiler evidence separately. Document validation alone is not runtime proof.
- Unity Inspector wiring/import and Play Mode remain the user's handoff unless
  explicitly delegated. Record pending work rather than claiming acceptance.
- Historical waivers are not automatic exemptions for new changes touching those
  boundaries. Record relevant fresh checks or an explicitly reviewed waiver.
- Choose the smallest controlled fixture for the changed contract. Fixed Draft
  efficacy and Natural offer accessibility remain separate. Inspect actual fixture
  fields and final records; do not infer acceptance from filenames or Victory alone.
- Completion requires the approved scope, document synchronization, and recorded
  acceptance. Store implementation commits and new report locations at closeout.

## History And Deferred Work

The previous Tasks, code, assets, and reports are preserved at
`milestone/combatmath-v2-2026-09-09`. See the
[historical evidence index](../History/CombatMathV2_Closeout.md) for retrieval,
known limits, and unexecuted acceptance checks. This series does not erase them.

Retired CombatMathV2 Task016 Fast-Monster/Wave substitution remains deferred.
It is not a dependency of this refactor series. Natural Draft assistance such as
rerolls/pity and new gameplay content remain separate future work.


## ArchitectureRefactor Iteration Closeout — 2026-09-10

The user accepts this iteration as complete and elects not to run the remaining
targeted tests, including Task006 Recorder on/off profiling. Those checks are
explicitly waived for this iteration, not passed. Future bugs or unexpected behavior
will be diagnosed and fixed as concrete follow-up work; no additional tests block
this closeout. Each Task retains its actual evidence and historical coverage limits.
No measured Unity performance gain or exhaustive lifecycle correctness is claimed.

Implementation history:

- `7bec5ca`: Task001–Task002.
- `c621362`: Task003–Task004.
- `630fcad`: Task005 and its four native acceptance reports.
- Task006 implementation and its three new native reports are in the current
  worktree, not yet committed. Completion does not imply commit, push or merge.

Task004, Task005 and Task006 JSON evidence is under `Doc/GamePlayRecord` with the
corresponding `TaskNNN_` prefixes. See each Task's evidence sections for exact runs.
Task006 Section 9.5 records normal Victory/Defeat, retained Piercing Arrow Pending,
Elemental/entity observations and Recorder partial-cancel/next-Battle recovery.
