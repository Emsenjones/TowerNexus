# Task001 - Upgrade Commit Consistency

Series: ArchitectureRefactor
Status: Completed - Targeted Unity Acceptance Waived
Branch: `codex/architecture-refactor`
Depends on: No new-series task. Uses the current HUD-owned Pending model.

## 1. Problem And Goal

At the draft baseline, `TowerPlacementController.CompleteTowerUpgrade` applied an Upgrade,
plays feedback, and then removes the Pending item. `TowerInstance.TryRecordUpgrade`
mutated Upgrade state before invoking `OnUpgradeRecorded`. An exception in a
subscriber or presentation can interrupt consumption after the Upgrade is recorded.
Level Up already separates prepared state writes from later notifications.

Make one accepted ordinary Upgrade consume its exact reward once, with rejection
preserving both Upgrade and reward state. Preserve the approved live-refresh rules.
This is a correctness repair. The pre-change managed failure was reproduced;
normal-play Unity smoke acceptance passed by user report; remaining targeted
Unity checks are waived for this closeout (see Section 8).

## 2. Approved Contract

1. Preflight the Battle gate, exact held reward and result identity, target,
   eligibility, pending reservation, and required combat owner readiness.
2. Prepare the Upgrade mutation, exact Pending consumption, and necessary combat
   revision before exposing any accepted mutation. Prepared data is single-use,
   belongs to the current Battle, and cannot survive a callback or deferred frame.
3. Commit accepted Upgrade state and Pending consumption synchronously without
   presentation or optional observers between those writes. The consumed view
   rejects further interaction immediately; destruction is later cleanup.
4. Perform required owner-controlled combat refresh exactly once, then finalize
   captured investment evidence before isolated optional notifications and presentation.
   Neither an observer nor a failed VFX may restore the item or repeat investment.
5. Preserve Basic stat refresh, scheduler ratios, matching-package refresh,
   released-entity semantics, Elemental eligibility, and duplicate/package limits.

The existing Upgrade event is also a combat-refresh transport. Merely catching
all subscribers and declaring success could leave stale combat state. Separate
required owner refresh from optional notification. Preparation should move all
predictable refresh failures before commit. If required refresh can still fail
after commit, the approved fallback is an explicit result-neutral Battle technical
failure, with no refund or replay; this policy was approved on 2026-09-09.
Do not describe a required refresh failure as a presentation warning.

## 2.1 Accepted Review Clarifications (2026-09-09)

- Capture investment facts at commitment. Success and failure share one exactly-once
  finalization path; investment observation and terminal Pending capture precede
  Stage release. Recorder observation is a distinct phase before public flow
  notifications, with subscriber exceptions isolated.
- Required refresh returns Applied, NotRequired, or TechnicalFailure. Expected
  no-active/terminal-entity cases are not failures; failed required creation or
  initialization is explicit even without an exception.
- Guard the complete acceptance and outer cleanup, including new drag entry.
  Battle stop/release remains permitted; callbacks cannot start a second drag
  that the old operation's cleanup would cancel.
- Establish a pre-change managed failure reproduction, then rerun it after repair.
  Native Unity lifecycle and VFX failure acceptance remains separately recorded.
- Candidate Basic values reuse the existing resolver through read-only candidate
  input. Debug batches reuse the core and stop on technical failure without
  inventing a Pending consumption or Draft investment event.

## 3. Scope And Source Pointers

- `Assets/Scripts/TowerDeployment/TowerPlacementController.cs`
- `Assets/Scripts/TowerDeployment/BattleHUDUI.cs`
- `Assets/Scripts/TowerFramework/TowerInstance.cs`
- `Assets/Scripts/TowerUpgrade/TowerUpgradeSystem.cs` and `TowerUpgradeState.cs`
- `Assets/Scripts/TowerRuntimeCombat/TowerCombatBehaviour.cs` and only subtype
  refresh entry points required to make that ownership explicit
- BattleRuntimeCoordinator failure/terminal-observation routing, Recorder adapters,
  and TowerUpgradeDraftDebugWindow caller migration required by this contract
- PendingDraftUIItem input guard and DroneBehaviour refresh result adapters
- `Tests/Task001` managed boundary checks and baseline reproduction

Keep the current UI-owned Pending collection until Task003. Do not create a generic
transaction framework, migrate all combat lifecycles, alter Upgrade values, or
change Draft generation. Preserve new-Tower and Level-Up transaction behavior.

## 4. Documentation And Review Decisions

Read [HUD](../System/04_BattleHUDUISystem.md),
[Placement](../System/09_TowerPlacementSystem.md),
[Combat](../System/11_TowerRuntimeCombatSystem.md), and
[Upgrade](../System/13_TowerUpgradeSystem.md).

The required-refresh boundary and result-neutral TechnicalFailure route were
approved on 2026-09-09 and synchronized into the owning System documents.
Existing Level-Up semantics remain unchanged.

## 5. Implementation Sequence

- Capture the current failure with a scoped throwing observer/presentation fixture.
- Inventory Upgrade subscribers and subtype refresh side effects.
- Introduce local preparation/commit operations and explicit refresh dispatch.
- Route all ordinary Upgrade acceptance through the same operation; remove any
  bypass that can apply an interactive reward without consuming its identity.
- Keep diagnostics after semantic commitment and remove temporary fault hooks.

## 6. Acceptance

| Case | Required evidence |
|---|---|
| Valid Basic, Behaviour, Elemental | Exactly one Upgrade and one consumption; correct runtime refresh |
| Invalid family/level/duplicate/package/Elemental capacity | No state change or consumption |
| Missing, consumed, wrong-result, or previous-Battle reward | Rejected before mutation |
| Optional subscriber/VFX failure | Committed reward remains consumed; later observers continue |
| Required refresh failure | Reviewed failure route occurs; no silent stale combat or replay |
| Reentrant or repeated acceptance | No second Upgrade, consumption, or investment event |
| Live entities and cooldowns | Matching refresh and timer ratios preserved; no replayed damage |

Use managed transaction tests with fault injection plus focused Unity Play
Mode coverage for all three layers and live combat refresh. Smoke new deployment
and Level Up. Historical CombatMathV2 Task004 Phase E was waived; this task's
consumption/refresh changes need fresh relevant evidence, not that waiver.

## 7. Implementation And Evidence (2026-09-09)

- Ordinary held Upgrade and Editor Debug now share TowerUpgradeSystem's synchronous
  core. Player preflight includes current Battle/deployed membership, exact held
  result, HUD ownership, eligibility, explicit combat owner, and candidate stats.
- Upgrade state, resolved baseline, and prepared HUD consumption commit without
  callbacks. The view is immediately consumed. No public TryRecordUpgrade or
  TryApplyUpgrade bypass remains. Debug does not consume or report a Draft reward.
- Required refresh is explicit. Magic staged-member/field creation and actual
  initialization failures report TechnicalFailure. Existing projectile/Drone
  eligibility and future-release rules remain in their owners.
- Captured investment identity is emitted exactly once to the evidence phase,
  including failure or synchronous stop/release. Recorder captures terminal
  Pending before cleanup and queues terminal evidence before destructive cleanup
  and public Game Flow notifications. Public subscriber and owner cleanup exceptions are isolated.
- The upgrade operation and complete placement cleanup reject nested acceptance
  and new drag entry. Stop/release can still cancel the Battle.
- Pre-change reproduction: `python3 Tests/Task001/run.py --baseline` PASS, reading
  actual historical source from `10eaf6f`. The old observer fault leaves an Upgrade
  recorded while the subsequent consumption is skipped.
- Managed regression: `python3 Tests/Task001/run.py` PASS, 29 cases. Actual commit
  core/instance/state are compiled against boundary doubles. This is not native
  HUD, prefab, combat, Recorder, or Game Flow execution.
- Runtime static build: PASS, 0 warnings/errors, using serialized build with
  `-p:LangVersion=8.0`. The generated default 9.0 is unsupported by the installed
  compiler; no project configuration was changed for this check.
- Editor static build (including runtime dependency): PASS, 0 warnings/errors.
  Final `git diff --check`: PASS.
- User-reported Unity smoke acceptance: played continuously from Stage1 through
  defeat in Stage3 and observed no noticeable problems. Specific Upgrade/package
  coverage, Console counts, and Recorder JSON were not supplied or independently
  verified; this is overall normal-play evidence.
- User accepted closing Task001 on this basis. Remaining targeted native checks
  are waived for this iteration, not recorded as passed. No device or Recorder
  result is claimed. This closeout does not create a Git commit.

## 8. Targeted Unity Acceptance - Waived For This Closeout

The user approved stopping Task001 after the Stage1-to-Stage3 smoke run. The
following detailed matrix remains unverified and is waived for this iteration;
it does not block Task002 review. Retain it as a regression reference when these
paths change or a related defect appears.

If rerun, use existing Stage/Recorder fixtures and names `AR_Task001_<case>`.
Do not reuse retired balance evidence as proof for this transaction change.

1. Apply Basic, Behaviour, and Elemental rewards through Pending drag. Verify one
   Upgrade, one removed/consumed Pending identity, one `investmentCommits` entry,
   and unchanged eligible targets/duplicate rejection. Repeat the pointer release.
2. Reject wrong-family/level/duplicate/package/Elemental-capacity targets and
   stale or consumed rewards without changing tower state or held capacity.
3. Exercise Basic cycle changes mid-cycle; Magic rotation with an active group;
   Drone cooldown during inter-burst cooldown. Verify ratios and no attack replay.
4. Exercise Archer Piercing, Cannon Explosive/Bouncing, Drone Blast/FinalDive,
   Magic MultiOrbs/Detonation/Field with live and absent/terminal entities.
   Verify only eligible entities refresh, and no-active cases are normal.
5. With a temporary Editor-side throwing Upgrade observer, verify later observers
   still run and the reward cannot be replayed. Trigger a second drag/acceptance
   from that observer and verify rejection through outer cleanup. Also exercise
   presentation failure; remove fault injection after the check.
6. Inject a required-refresh exception and an explicit Magic create/init failure
   after valid preflight. Verify result-neutral TechnicalFailure, no refund, one
   investment record, and terminal Pending excluding the consumed item. Force an
   early public failure subscriber to throw; Game Flow and later subscribers must
   still receive termination. Exercise normal synchronous Stage release as well.
7. Smoke new deployment, Level Up, successful/failed Battle exit, and a second
   Stage/run. Inspect final JSON (including terminal Pending) after every run;
   verify the previous run's snapshot cannot overwrite the next one. Debug batch
   must stop after a required failure without synthesizing Draft investments.

Task001 is Completed with the above explicit acceptance waiver. The smoke run
does not establish fault-injection, exact cooldown-ratio, package-by-package, or
Recorder ordering coverage. Task003 preserves the transaction boundary when
migrating Pending ownership; Task004 preserves it when relocating submission;
Task006 preserves the pre-cleanup evidence phases.
