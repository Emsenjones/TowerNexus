# Task004 - Tower Placement Responsibility Separation

Series: ArchitectureRefactor
Status: Implemented - Stage1 Play Mode And Recorder Passed; Targeted Acceptance Pending
Branch: `codex/architecture-refactor`
Depends on: Task001-Task003 contracts. Task003 core Play Mode smoke passed; its remaining targeted acceptance stays explicitly pending and is not waived by this plan.

## 1. Problem And Goal

TowerPlacementController currently coordinates pointer polling, previews,
Tower-target highlighting, deployment/Level-Up/Upgrade submission, a deployed
Tower collection, and Battle cleanup. Its deployed collection is also queried
by Draft generation. Separate these responsibilities while keeping transaction
orchestration readable and behavior unchanged.

## 2. Proposed Ownership

| Responsibility | Proposed owner |
|---|---|
| Pointer gesture, active drag, preview, highlighting, cancellation | Existing TowerPlacementController |
| Accept deployment/Level-Up/Upgrade intents | One local submission coordinator using existing validators and owners |
| Deployed Tower membership and stable read-only queries | One Battle-local collection owned by the submission coordinator |
| Tower readiness/instantiation | Existing TowerDeployController |
| Topology legality | Existing TowerPlacementValidator and Map/pathfinding owners |
| Monster movement revision | MonsterManager |
| Level/Upgrade eligibility | TowerUpgradeSystem |
| Held reward identity/consumption | Draft domain from Task003 |
| Battle stop/release authority | BattleRuntimeCoordinator |

Use the smallest helper structure that realizes these boundaries. The collection
may be a plain owned class; it does not need a new globally discoverable Manager.
The revised plan uses two plain C# helpers, `TowerPlacementSubmission` and
`DeployedTowerCollection`, without adding scene components. BattleRuntimeCoordinator
owns one Submission for its own lifetime, available before consumer binding.
The same instance survives Stage transitions; Battle data and bindings are reset.
Queries never create or replace this instance. Submission binds DraftSystem directly
for Pending authority, rather than resolving it through Controller/HUD.

## 3. Contract To Preserve

- Submission accepts a current reward identity and domain target/footprint data,
  not a Pending UI component. Preview may provide a candidate; final submission
  performs fresh validation and Monster preparation.
- Deployment, Level Up, and Upgrade each have a single preflight/commit path,
  preserving Task001 consumption and required refresh semantics.
- Task002 cache reuse remains confined to preview. A prepared gameplay transaction
  is synchronous and never stored for a later frame or reused after a callback.
- Membership changes occur only after accepted deployment and during release.
  Draft queries do not rebuild or mutate authority as a property-read side effect.
- After terminal evidence capture, Stop closes gates and stops tracked combat while
  retaining members. Stage release detaches members into a local snapshot and clears
  authoritative membership before invoking per-Tower disable/destruction callbacks.
  Preserve the existing result-screen lifetime.
- Cancellation restores presentation and retains the reward. Pointer release over
  the Pending area still cancels before validation; Camera cannot steal the drag.
- Post-commit visual failures cannot undo gameplay or leave an interactive reward.
- Preserve deployment/investment event identity, ordering, and Recorder route data.

Task002 preservation requirement: preserve Map and pathfinding binding identity,
structure/walkability revisions, exact resolved footprint equality, and cache
clearing on gesture cancellation/rebinding. Unresolved footprint/configuration
failures do not hit the cache. Final submission still creates a fresh topology
plan and Monster revision batch, even after a valid preview cache hit.

Task001 preservation requirement: the ordinary Upgrade core currently lives in
TowerUpgradeSystem. Relocate orchestration only after review; preserve its explicit
combat refresh, Debug authority, evidence-before-release, and complete outer
interaction guard. Extracting submission must not reopen nested acceptance.

## 4. Scope And Documentation

Main scope: `Assets/Scripts/TowerDeployment/TowerPlacementController.cs`,
`TowerDeployController.cs`, `TowerPlacementValidator.cs`, new local helpers,
DraftSystem's deployed-Tower query and Pending binding, BattleRuntimeCoordinator
lifecycle calls, TowerUpgradeSystem binding/result/guard changes, Editor Debug entry
adaptation, actual Submission integration tests, and the minimum Recorder
subscriptions/reference wiring required by the move.

Read and synchronize [Placement](../System/09_TowerPlacementSystem.md),
[Draft](../System/08_DraftSystem.md), [HUD](../System/04_BattleHUDUISystem.md),
[Upgrade](../System/13_TowerUpgradeSystem.md),
[Overview](../System/00_ProjectOverview.md), and
[Tower Framework](../System/10_TowerFrameworkSystem.md) where ownership changes.
Engine helper names belong here, not in the System contract.

No new input system, selling/moving Towers, general command bus, combat controller
refactor, or repository-wide folder move. Preserve serialized GUIDs and references
for any approved component or file migration. Scene-wide dependency discovery
cleanup outside moved owners belongs to Task005.

## 5. Implementation And Review

Review the submission boundary and deployed collection lifetime first. Inventory
all callers of DeployedTowerInstances, StopTrackedTowerCombat, DestroyTrackedTowers,
and deployment/investment events. Extract the operations without duplicating their
rules, migrate every caller, then remove obsolete orchestration paths. Report any
required Inspector wiring as a separate handoff.

## 6. Acceptance

- All three intent types work through the same production entry used by UI.
- Invalid target, blocked route, missing readiness, cancelled drag, and old reward
  preserve pre-commit state. Accepted operations consume once.
- Multi-cell placement, Level preview, Upgrade highlights, and drag-area cancellation
  behave as before. Mouse/touch behavior currently supported remains unchanged.
- Draft queries see exactly the committed Tower membership in stable order.
- Stop during drag, Victory/Defeat cleanup, retry, and next-Stage entry leave no
  combat recovery, stale members, old reward identities, or duplicated callbacks.
- Task001 transaction fault checks and Task002 cache/final-preflight checks pass.
- Recorder investment and route integrity remain valid in representative Stage runs.

Use focused transaction/lifecycle tests and Play Mode interaction checks. Do not
replace these with a line-count target or a Stage-clear-only test.

## 7. Completion

Record final ownership, migrated callers, wiring validation, test evidence, and
any remaining limitations. Task005 consumes the explicit coordinator/collection
references; Task006 may reorganize observation without changing their authority.
Implementation and validation evidence is recorded below.


## 8. Revised Implementation Contracts

Feedback source: Codex task `01a08605-521a-7de1-a607-67de3a5f3b77`,
"Review Task004 implementation plan". Incorporated as implementation requirements;
the implementation below follows these requirements.

### 8.1 Stable Owner And Binding

- BattleRuntimeCoordinator constructs its sole Submission before consumer binding,
  independently of MonoBehaviour enable order. Construction performs no scene lookup.
- Draft queries, Upgrade membership/evidence and Recorder subscriptions use that
  stable instance. Recorder unsubscribe uses the exact instance originally subscribed;
  repeated enable/disable or Stage transitions do not duplicate subscriptions.
- Stage bind/release changes a binding revision and closes acceptance during transition.
  Every prepared submission captures the current Battle, Map and Draft owner context;
  Stop/rebind invalidates its authority before the first semantic write.
- Collection and Submission state are Battle-local even though their objects survive
  Stage transitions. Old entries/targets cannot become valid in the next Stage.

### 8.2 Coherent Deployment Candidate

- Capture a candidate at final submission from one resolution of the Tower definition,
  occupied anchors and pose. Copy the ordered/deduplicated footprint into an immutable
  snapshot; do not retain a mutable caller list or pass Preview into the commit core.
- Preserve Center Anchor offset and the existing world position/rotation and local
  scale semantics, including different preview/deployed parents.
- Construction must enforce the definition/shape/pose relationship. Independent lists
  of legal nodes and unrelated transforms are not a valid candidate. Validate exact
  Pending definition identity and candidate provenance/current binding at submission.
- Explicitly reject empty/unresolved footprints, foreign-Map nodes and mismatched
  definition/shape/pose. Reuse the current anchor resolution rules rather than adding
  a second divergent footprint algorithm. Do not silently reinterpret scale or rotation.
- Resolve fresh topology and Monster movement preparation on final submission;
  preview-cache hits and prior plans never authorize a commit. Revalidate lifecycle
  and binding after readiness operations that may invoke callbacks.

### 8.3 Explicit Submission Result

Use a small result with `Rejected`, `Committed`, and
`CommittedWithTechnicalFailure`, plus a failure reason where applicable.

- `Rejected` means no gameplay commit and no reward consumption.
- Both committed outcomes mean the exact reward was consumed. Controller releases
  its consumed view and never refunds/replays the request on TechnicalFailure.
- Optional presentation failure remains committed. The Upgrade core remains in
  TowerUpgradeSystem, with its required-refresh and pre-cleanup evidence semantics.
  Adapt its result boundary so Submission does not infer commitment from a Boolean.
- Direct Debug application uses the same truthful commit outcome while preserving
  its separate authority and absence of synthetic Pending consumption/evidence.

### 8.4 Guard Entry And Exit

- Controller enters the outer interaction guard before submitting and exits in
  `finally` only after consumed-view cleanup and CancelPlacement finish.
- Submission owns a separate guard around preflight, commit, refresh and notifications,
  also released in `finally`. An authorized internal Upgrade call continues within
  that operation; it must not reject itself merely because Submission is busy.
- All fresh public submissions and Debug applications reject while either guard is
  held. Pending grants and view rebuilds use the same fresh-operation gate.
- Keep internal authorization narrow and bound to the current operation; do not
  introduce a publicly callable bypass or trust an arbitrary caller-supplied flag.
- Stop/release bypass acceptance guards for cleanup and invalidate pending authority.
  Cleanup cannot reset a guard early and reopen acceptance for the outer operation.

### 8.5 Collection Release

Capture terminal evidence -> close acceptance -> detach the release snapshot and
clear authoritative membership -> stop/disable/destroy each snapshot member.

- Plain Stop retains collection contents and stops combat; it does not detach members.
- Release is idempotent. Synchronous callbacks see no investable released members and
  cannot acquire the same release snapshot again.
- Isolate cleanup failures per Tower, and attempt destruction even if its stop or
  disable step throws. One failed member cannot prevent cleanup of the others.
- Read-only queries have no pruning/rebuild side effects. Membership order follows
  accepted deployment order. No new arbitrary Tower removal feature is introduced.

## 9. Revised Execution And Validation

1. Synchronize relevant System ownership contracts before runtime changes; retain
   Task003's actual acceptance status and existing uncommitted implementation.
2. Add the stable Submission instance, explicit binding and owned read-only collection;
   migrate deployed queries and lifecycle ownership without dual authoritative lists.
3. Introduce coherent candidate capture and explicit results; adapt Validator and
   DeployController APIs while preserving current anchor/pose behavior.
4. Move deployment/Level Up coordination; delegate Upgrade to its existing core.
   Wire outer/internal guards and evidence publication before external callbacks.
5. Migrate Draft, Upgrade, Recorder and Debug callers; remove obsolete Controller
   orchestration and event/query authority. Preserve serialized references and GUIDs.
6. Test the actual complete production Submission entry for all three intents, using
   real Pending ownership and collection. Native boundaries may be doubled explicitly,
   but do not replace preflight or reproduce acceptance logic inside a fake Submission.

Required additional cases:

- Accept/reject all three intents; malformed or mismatched candidate, blocked route,
  missing readiness, foreign target, old Pending entry and wrong owner.
- Stop or rebind during readiness: no occupancy, membership, level/Upgrade or reward
  mutation; discard the prepared Tower safely.
- Nested requests during required refresh, notifications and UI cleanup: fresh
  submission, Debug, Pending grant and rebuild are rejected; current authorized core
  completes without self-rejection; Stop/release still works.
- Reentrant/throwing release callbacks: snapshot detached once, remaining members
  cleaned, repeated release harmless; standalone Stop retains members.
- Stage transition and consumer re-enable: exactly one Recorder subscription per
  event, one accepted investment record, stable Submission identity and stale targets
  rejected. Compare pose/footprint for multi-cell and offset-anchor fixtures.
- Retain Task001-003 regressions and Task002 fresh-final-validation checks. Existing
  Task003 post-preflight segment tests are regression evidence, not full Submission
  integration coverage. Native Play Mode and fresh Recorder acceptance stay separate.


## 10. Implementation And Managed Evidence — 2026-09-09

- BattleRuntimeCoordinator constructs one Submission using a field initializer,
  before enable callbacks. It binds Controller, Draft and Upgrade to that instance;
  Stage preparation configures dependencies and increments binding authority.
  Recorder subscribes to the stable instance and retains it for exact unsubscription.
- Controller now owns only interaction, preview/highlights and outer cleanup. All
  three gameplay paths call Submission with a model entry and domain candidate/target.
  No new scene components or Inspector references were introduced. Existing component
  GUIDs are unchanged; immutable investment observations moved to their own source file.
- `DeployedTowerCollection` maintains ordered read-only behaviour/instance projections.
  Queries perform no reconstruction or pruning. Accepted deployment reserves capacity
  before adding. Stop retains membership; release detaches before per-member callbacks,
  isolates cleanup failures and tolerates repeated/reentrant calls.
- `TowerPlacementCandidate` has a private constructor and a final capture factory.
  It snapshots definition, pose, center/occupied anchors and immutable footprint, with
  validator/Map/path revisions and frame identity. The final validator rejects foreign
  nodes and reruns topology. Prepared Tower geometry must match the capture before
  any gameplay mutation. Existing world position/rotation and local scale are retained.
- Submission returns `Rejected`, `Committed` or `CommittedWithTechnicalFailure`.
  Upgrade core remains in TowerUpgradeSystem and requires the exact current authorized
  operation. Direct Debug uses the same fresh-submission gate with separate authority.
- A single-use interaction lease survives submission return through view destruction
  and CancelPlacement, even if cleanup throws. Submission separately protects its core;
  Stop/release invalidate authority without prematurely disposing the outer lease.
- Deployment/Level Up/Upgrade evidence and route notification ordering are preserved.
  Accepted placement log formatting remains unchanged; post-commit diagnostics cannot
  turn successful acceptance into a rejection. Recorder schema is unchanged.

Managed validation:

- `python3 Tests/Task004/run.py`: **23 cases passed** through complete production
  Submission, collection, Pending, candidate, validator, Upgrade and Tower state.
  Includes production Controller entry/cleanup and Recorder subscription blocks,
  synchronous release from deployment/route callbacks, geometry and lifecycle faults.
- `python3 Tests/Task001/run.py`: **30 Upgrade regressions passed**, now through the
  real Submission entry and real collection, preserving required-refresh fault cases.
- `python3 Tests/Task003/run.py`: **14 Pending/orchestration cases passed**, including
  real Draft grant/rebuild rejection during refresh, notification and outer cleanup;
  **300 baseline Draft traces match**. Prior post-preflight investment segment tests
  are superseded by Task004's complete-entry tests, not treated as equivalent evidence.
- `python3 Tests/Task002/run.py`: **32,192 baseline/current fixture observations match**.
  Task002 supplies resolved candidates to the real validator; real candidate capture
  and geometry checks are covered by Task004.
- Runtime and Editor managed compilation: **0 warnings, 0 errors**.
  `git diff --check`: **passed**. The Task001 historical baseline reproduction also
  remains runnable. Native instantiation, transforms, physics, combat and Recorder
  JSON export remain separate Play Mode acceptance; boundary doubles are documented
  in `Tests/Task004/README.md`.

Task005 receives a stable, explicitly bound Submission and domain queries; its broader
scene-dependency discovery cleanup remains out of scope. Task006 receives Submission's
observation events and existing model-based pre-cleanup Pending capture. Task003's
unconfirmed native acceptance cases remain pending; this Task does not waive them.

## 11. Native Acceptance Handoff — Partially Verified

1. Start Stage1 and confirm Initial Draft selection restores normal simulation and
   permits one normal Tower deployment. Watch the Unity Console throughout.
2. Exercise all three intents: new deployment (include a multi-cell Tower), same-family
   Level Up and ordinary Upgrade. Each consumes one Pending item. Confirm existing
   level preview, Upgrade highlighting and attack-range feedback remain correct.
3. Try an invalid target or blocked route and return/cancel a drag over Pending.
   Reward and gameplay state must remain unchanged. Rebuild two Held views using
   DraftSystem's existing context menu, then consume them normally.
4. End/stop during a drag, reach Victory or Defeat, retry and enter another Stage.
   Result-screen Towers remain stopped until release. There must be no old Towers,
   stale targets/rewards, duplicated effects or resumed combat in the next Battle.
5. Inspect a fresh Recorder run for exactly-once investment consumption and route
   integrity, including terminal Pending attribution. Managed subscription tests do
   not constitute a fresh Recorder export or native lifecycle acceptance.

Targeted native fault injection remains separate from ordinary-play smoke. Record
actual coverage and any explicit scoped waiver before marking Completed. Confirmed
coverage is recorded below; remaining cases are not implicitly waived.


## 12. Stage1 Native Smoke And Recorder Evidence — 2026-09-10

User reported completing Stage1 without observed functional problems. Evidence:
`Doc/GamePlayRecord/Task004_Stage1_SubmissionLifecycle_Acceptance_01.json`
(schema 25, generated `2026-09-10T01:11:31+08:00`, fixture `StageDefini_Lv1`).

- Victory; all 5 Waves started/completed; 21 Monsters spawned/resolved, 20 killed,
  1 leaked, 0 unresolved. Player Health changed from 4 to 3 consistently.
- All 32 Recorder integrity flags pass. Independent JSON reconciliation also
  confirms each of the 5 selected Draft tokens maps to exactly one investment,
  with matching selected asset and consumption after selection.
- Three Submission intents covered: 2 Archer deployments (each occupying 2 cells),
  1 Level Up from L1 to L2, and 2 ordinary Behaviour Upgrades (Scatter Arrow and
  Explosive Arrow). Token `1:3` was held while later rewards were consumed and
  then deployed successfully; consumption order need not equal selection order.
- Both deployments have corresponding route commits. The second revises 7 Monsters:
  1 AlreadyOnNewRoute, 3 ReachableRouteRejoin, 3 ForcedRelocation, each forced move
  caused by CoveredByNewFootprint. Commit health/progress/counts, Monster gameplay
  fingerprints and combat ownership remain unchanged. Connector lifecycle records
  show 4 subsequent joins and 1 Monster killed before joining; no unresolved
  Monsters remain at Victory.
- Terminal Pending is empty and all 5 rewards are Consumed. This verifies empty
  terminal reconciliation, not non-empty Pending capture/attribution.

After run 01, remaining native coverage included non-empty terminal Pending, rejection/cancel and
view-rebuild cases under Task004, drag-time stop, retry/next-Stage release behavior,
and targeted fault scenarios. Visual feedback details and Console Warning/Error
absence were not separately confirmed. This run does not establish those results
or waive the remaining acceptance requirements. Task003 historical evidence remains
separate. No runtime code was changed for this record review.


### Run 02 — Non-empty Terminal Pending

Evidence: `Doc/GamePlayRecord/Task004_Stage1_SubmissionLifecycle_Acceptance_02.json`
(schema 25, generated `2026-09-10T01:17:36+08:00`).

- Defeat; 21 Monsters resolved, 17 killed and 4 leaked; Player Health 4 to 0.
  All 32 integrity flags pass.
- Independent token reconciliation confirms 5 selections = 4 consumed investments
  (2 deployments, 1 Level Up, 1 Sharpened Arrows Upgrade) + 1 terminal Pending item.
  Pending token `1:5` has no investment commit, is marked `StillPending`, and its
  terminal item exactly matches the selected choice.
- The user confirmed the retained item was an Archer `TowerDraft`, matching both
  the selected choice and terminal snapshot. Non-empty terminal Pending attribution
  at Defeat passes acceptance. This check does not require a separate run retaining
  an Upgrade draft; the earlier item-type clarification is resolved.
- Both route commits reconcile; 5 Monster revisions are AlreadyOnNewRoute, with
  no forced relocation in this run. Run 01 remains the relocation evidence.

Other unconfirmed native scenarios listed above remain pending. No runtime change
or waiver follows from this record review.
