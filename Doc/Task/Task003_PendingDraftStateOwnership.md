# Task003 - Pending Draft State Ownership

Series: ArchitectureRefactor
Status: Implemented - Core Play Mode Smoke Passed; Targeted Acceptance Pending
Branch: `codex/architecture-refactor`
Depends on: Accepted Task001. Task002 is independent of this ownership migration.

## 1. Problem And Goal

`BattleHUDUI` owns a collection of `PendingDraftUIItem`; each view holds its reward,
Draft token, and consumed flag. `DraftSystem.CountPendingReservedCapacityForUpgrade`
reads those views as gameplay data. Initial completion also retains a view reference.

Move held-reward identity and consumption into the Draft domain so presentation
can be rebuilt without altering reward ownership or reservation capacity.

## 2. Approved Contract

- DraftSystem owns one Battle-local Pending collection implemented by a small
  data class. Each entry has an immutable identity, source DraftAttemptToken,
  DraftResult, and semantic held/consumed state. View objects are not identities.
- A consumed identity cannot be accepted again; previous-Battle identities are
  rejected. Clearing a Stage invalidates all entries and prepared consumption.
- Consumers query read-only data and ask the owner to prepare/commit consumption.
  Collection mutation has one authority; no publicly mutable list is exposed.
- Task001's acceptance boundary remains synchronous: accepted gameplay and exact
  consumption occur before optional notifications. A view checks model ownership
  before all input, even if its destruction or model-change callback fails.
- Draft candidate weighting and Elemental-family reservation count held model
  entries using precisely the current reservation rules. UI visibility cannot
  create capacity, release a reservation, or alter sampling order.
- Preserve the Initial Draft requirement that a usable held interaction has been
  prepared before selection is accepted and spawning is authorized. Initial view
  preparation failure creates no committed reward and follows existing failure
  routing. Model/view preparation must not leave a half-accepted selection.
- Rebuilding an already committed view reuses its identity and neither consumes
  another reward nor repeats Initial completion. Stage release clears the model;
  ordinary presentation teardown does not silently discard held rewards.
- DraftSystem continues to own pause and session tokens. Hide/rebuild is not an
  alternate pause owner or a reason to authorize spawning.

Task001 preservation requirement: retain the exactly-once committed investment
flush, pre-cleanup terminal Pending capture, explicit required-refresh outcome,
and input guard through outer cleanup when replacing HUD consumption with the
Draft owner. Do not move evidence behind public terminal subscribers.

## 3. Scope And Source Pointers

`Assets/Scripts/TowerDeployment/DraftSystem.cs`, `DraftAttemptToken.cs`,
`BattleHUDUI.cs`, `PendingDraftUIItem.cs`, the consuming portions of
`TowerPlacementController.cs`, a new local Pending data model/collection, `TowerUpgradeSystem.cs`,
`BattleRuntimeCoordinator.cs`, `TowerUpgradeDraftDebugWindow.cs` and managed tests.
Adapt Recorder Pending snapshots and investment correlation to the model in this
task so reports stay usable; broad Recorder decomposition remains Task006.

No inventory framework, persistence, rerolls, new reward types, probability
retuning, input redesign, or full placement-class decomposition. Remove obsolete
UI authority and duplicate collections after migration.

## 4. Documentation And Review Decisions

This changes an explicit existing ownership contract. The approved implementation updates
[HUD](../System/04_BattleHUDUISystem.md), [Draft](../System/08_DraftSystem.md),
[Placement](../System/09_TowerPlacementSystem.md), and the ownership summary in
[Overview](../System/00_ProjectOverview.md) before implementation and reconciles the
[Upgrade](../System/13_TowerUpgradeSystem.md) consumption wording as well.

Approved: retain interactive-readiness gating while separating accepted state
from presentation. Identity, preparation and release details are recorded in Section 8.

## 5. Implementation Sequence

- Capture fixed-seed candidate, reservation, selection, and consumption traces.
- Introduce model queries and prepared single-use consumption.
- Migrate Draft generation and initial held-item confirmation to model identities.
- Adapt HUD/drag views, all three investment paths, and Recorder consumers together.
- Remove view-based gameplay ownership and verify rebuilding/cancellation paths.

## 6. Acceptance

| Case | Required evidence |
|---|---|
| Hold several rewards then rebuild views | Same identities, results, reservations, and ordering |
| Deploy, Level Up, ordinary Upgrade | Exact identity consumed once; Task001 failure cases preserved |
| Rejected/cancelled/returned drag | Entry and capacity reservation retained |
| Duplicate or old-Battle request | Rejected without gameplay mutation |
| Initial view preparation failure | No completion or spawning; no committed orphan reward |
| Optional view callback or destruction failure | Consumed reward remains unusable |
| Fixed seed and same Tower/Pending state | Same candidate weights, displayed order, and RNG consumption |
| Terminal result then Stage cleanup | Recorder captures terminal Pending model before clearing |

Use model-level tests for identity and reservation plus Play Mode tests for
view rebuilding, initial spawning, pause restoration, and all investment types.
Recorder consumption reconciliation must pass. Do not infer whole-combat
bitwise determinism from the Draft-only seed.

## 7. Completion And Handoff

Record model API and owner decisions for Task004, and terminal snapshot changes
for Task006. Historical schema-25 traces are comparison sources, not fresh
acceptance. Implementation and validation evidence will be recorded below.

## 8. Approved Review Supplements

- Identity validation includes the exact collection owner and entry reference;
  equal generation/sequence numbers from different owners do not confer authority.
- Stop invalidates prepared mutations but retains Held entries for terminal capture.
  Release and a new Battle invalidate the old generation. Tickets are synchronous,
  single-use and must be revalidated before the first semantic write.
- Initial selection prepares the entire usable, non-interactive view before a
  callback-free registration. Revalidate Battle/session after preparation.
- HUD rebuild cancels drag, prepares all replacements before switching bindings,
  and preserves identities, ordering and reservations if preparation fails.
- Debug Pending batches prepare every candidate before one atomic registration;
  failed preparation never consumes or rolls back an accepted reward.
- Deployment and Level Up capture immutable investment facts before commit and
  publish evidence before any external lifecycle notification. Preserve Upgrade's
  pre-cleanup flush and required-refresh TechnicalFailure handling.
- Scope explicitly includes TowerUpgradeSystem, BattleRuntimeCoordinator,
  TowerUpgradeDraftDebugWindow and managed contract tests. Test the actual Pending
  collection connected to the actual Upgrade commit core.

## 9. Implementation And Managed Evidence — 2026-09-09

- `PendingDraftCollection` owns read-only Held entries, immutable generation/sequence
  and source tokens, exact owner/reference checks, lifecycle invalidation and
  single-use prepared grants/consumption. Source Draft tokens remain attribution,
  not Pending ownership IDs. Stop retains Held state; release/new Battle invalidates it.
- HUD binds entries to current views and owns visual cleanup only. Rebuild cancels
  drag, prepares every replacement, validates usable presentation again, and switches
  bindings without consuming or granting anything. Replaced, hidden and consumed
  views cannot submit input. Existing serialized references are reused.
- Initial confirmation uses a committed model entry. Normal selection and Editor
  Pending batches share all-or-nothing preparation/registration. Direct Debug
  Upgrade remains separate from reward consumption.
- Deployment, Level Up and Upgrade consume through the same model. Deployment and
  Level Up capture immutable investment facts before commit and publish evidence
  before external lifecycle callbacks. Upgrade retains its pre-cleanup flush and
  required-refresh TechnicalFailure boundary; outer placement still guards view cleanup.
- Recorder reads terminal Pending from Draft data; source-token attribution and
  schema-25 semantics remain unchanged. No fresh native Recorder result is claimed.
- `python3 Tests/Task001/run.py`: **30 passed**, actual Upgrade core connected to
  actual Pending collection/result/token and production investment observation.
- `python3 Tests/Task003/run.py`: **14 passed**, including Initial/preparation failure,
  atomic Debug-style batches, rebuild/current-view gates, Stop/old-generation/owner
  rejection and synchronous Stage release from LevelChanged and all three deployment
  callback categories. The latter executes production commit/notification segments
  after preflight against native boundary doubles.
- Draft baseline at `7bec5ca`: **300 traces match** across 100 seeds and three Pending
  states, comparing reservations, displayed order and subsequent RNG state. Controlled
  candidate eligibility is not a whole-game deterministic replay.
- `python3 Tests/Task002/run.py`: **32,192 baseline/current observations match**.
- Runtime and Editor managed compilation: **0 warnings, 0 errors**.
  `git diff --check`: **passed**. These checks do not replace Unity import or
  Play Mode acceptance.

For Task004: submission should carry `PendingDraftEntry` and resolve the current
Draft owner, while pointer interactions also validate their current view binding.
For Task006: preserve model-based terminal capture before Stage clear and immutable
investment evidence before external lifecycle notifications.

## 10. Native Acceptance Handoff — Partially Verified

1. **Stage1 opening:** before Initial selection, simulation remains paused and Waves
   have not started. Select one Tower: one Pending item appears, pause restores and
   spawning starts once. Observe the Unity Console throughout.
2. **Three consumption paths:** deploy a new Tower, use a same-family Tower Draft
   for Level Up, and apply an ordinary Upgrade. Each acceptance removes exactly one
   item. Invalid targets, return-to-container and cancelled drags retain the item.
3. **Held presentation rebuild:** keep at least two Pending items (Debug grants may
   be used). From the DraftSystem component menu choose **Rebuild Pending Draft Views**.
   Check count, reward types and order remain unchanged, and the rebuilt items still
   work. For drag cancellation, pause the Editor during a drag, invoke rebuild, then
   resume. Also hide/show HUD and verify rewards are retained and input recovers.
4. **Terminal/restart:** finish or lose with a reward still Held, inspect the fresh
   Recorder result for retained Pending attribution and no missing/duplicate investment
   consumption, then start a fresh Stage and confirm old rewards do not carry over.
5. **Targeted fault acceptance:** native Initial prefab/readiness failure, later-item
   Debug batch preparation failure, late view destruction and synchronous Stage release
   callbacks remain separate native checks. Managed injection covers their contracts;
   it is not a waiver of native evidence. Any later waiver must be recorded explicitly.

### User-Reported Play Mode Evidence

- Played two Stages without noticing issues.
- With two Draft items in the Pending area, invoked **Rebuild Pending Draft Views**
  on DraftSystem and observed no visible change.
- After rebuilding, successfully consumed the Tower Upgrade Draft to upgrade an
  existing Tower and consumed the Tower Draft to deploy a new Tower in the scene.

This passes the reported ordinary-play smoke and post-rebuild interaction/consumption
for Deployment and ordinary Upgrade. It is user-reported native evidence; model
identity/order invariants retain the managed evidence listed in Section 9.

Dedicated Level Up, rejected/cancelled drag, rebuild during drag, HUD hide/show,
Initial pause/spawn counting, terminal Recorder reconciliation and the targeted
native failure cases above have not been separately confirmed. No waiver is inferred
from the successful smoke test. Keep these acceptance items pending rather than
marking the entire Task Completed. No Task003 commit/push performed.
