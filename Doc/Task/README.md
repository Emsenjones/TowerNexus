# Tower Nexus Architecture Refactor Tasks

Active Series: `ArchitectureRefactor`
Status: Task001 and Task002 completed with recorded targeted Unity acceptance waivers; Task003-Task006 await individual review.
Branch: `codex/architecture-refactor`
Draft baseline: `10eaf6f` (documentation/evidence retirement complete).

## Review Order

| Task | Scope | Implementation dependency | Status |
|---|---|---|---|
| [Task001 - Upgrade Commit Consistency](Task001_UpgradeCommitConsistency.md) | Exact reward consumption and required combat refresh | Current runtime | Completed - Targeted Unity Acceptance Waived |
| [Task002 - Map Runtime Query Caching](Task002_MapRuntimeQueryCaching.md) | Runtime index and preview-query invalidation | Independent; reviewed after Task001 | Completed - Remaining Targeted Unity Acceptance Waived |
| [Task003 - Pending Draft State Ownership](Task003_PendingDraftStateOwnership.md) | Draft-owned reward data with independent views | Task001 | Draft - Pending Review |
| [Task004 - Tower Placement Responsibility Separation](Task004_TowerPlacementResponsibilitySeparation.md) | Input/preview, submission, and deployed membership boundaries | Task001-Task003 | Draft - Pending Review |
| [Task005 - Battle Dependency Binding](Task005_BattleDependencyBinding.md) | Explicit gameplay query scope and lifetime | Task004 | Draft - Pending Review |
| [Task006 - Combat Diagnostics Isolation](Task006_CombatDiagnosticsIsolation.md) | Optional producer cost and Recorder responsibility separation | Task001-Task005 | Draft - Pending Review |

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
