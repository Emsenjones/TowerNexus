# Task002 - Map Runtime Query Caching

Series: ArchitectureRefactor
Status: Completed - Remaining Targeted Unity Acceptance Waived
Branch: `codex/architecture-refactor`
Depends on: No new-series task; recommended after Task001 for review order.

## 1. Problem And Goal

At the draft baseline, `MapGeneratorBehaviour.EnsureNodeDictionaryValid` obtained
every hierarchy node and checked the complete index even when it was not dirty.
`GetNeighborNodes` invoked it inside A* expansion. Placement preview repeatedly
called `CanPlaceTower` while dragging, including for an unchanged footprint.

Separate structural validation from runtime lookup and avoid redundant preview
queries while preserving exact path selection and placement acceptance. Managed
baseline comparison confirms reduced repeated work; native Unity and device
performance remain unmeasured.

## 2. Approved Contract

- Map preparation builds and validates the node index once. Normal runtime reads
  use that index without hierarchy enumeration or full-map consistency scans.
- Map identity/index revision covers node addition/removal, coordinate or role
  changes, NodesRoot replacement, and map regeneration/rebinding.
- Walkability revision covers effective occupancy, base walkability, and runtime
  reset. A committed topology batch invalidates dependent previews synchronously,
  before any observer can consume stale results. Reads never advance revisions.
- Explicit mutation entry points notify the owning Map or delegate mutations to
  it. Audit `Initialize`, `SetGridPosition`, `SetNodeType`, `SetBaseWalkable`,
  `SetRuntimeOccupied`, and `ResetRuntimeState`; a dirty flag only in OnValidate
  does not cover these current APIs.
- Retain full authoring validation for edited hierarchies, inactive nodes,
  duplicate coordinates, ownership, and missing Spawn/Target. Editor mutation
  detection must not become a scan performed by every runtime query.
- Preview topology results may be reused only for the same active Map, structure
  and walkability revisions, and exact candidate footprint. New drag/map binding
  invalidates cached state. Live Upgrade/Level target eligibility is not cached
  behind a topology-only key.
- Final placement still performs fresh preflight and Monster route preparation.
  Moving Monsters must never reuse a cached prepared Monster revision batch.

## 2.1 Accepted Review Clarifications

- Map explicitly binds node owners, invalidates both owners on transfer, and
  detaches references on clear/release. Notifications only dirty state and advance
  revisions; they never scan or rebuild during occupancy commitment.
- Index readiness checks identity, coordinates and ownership independently of
  full Stage readiness. An all-Normal authoring scaffold remains queryable;
  missing Spawn/Target still prevents Battle readiness. A malformed index is
  rejected wholesale and cannot expose stale or partially indexed nodes.
- Preview order is binding/index readiness, full current footprint resolution,
  exact key comparison, then reuse/computation. Unresolved anchors, out-of-bounds
  footprints and configuration failures are never cached as topology results.
- Editor checks cover inactive nodes, cross-Map reparent, Undo/Redo, duplicate
  coordinates and repair. Tile/Feature-only refresh must not advance topology
  revisions or rebuild the index. Cache hits do not imply zero allocation.
- Final placement always prepares a fresh plan and Monster revision batch.
  Task001's targeted runtime waiver does not apply to this task.

## 3. Scope And Source Pointers

`Assets/Scripts/Map/MapGeneratorBehaviour.cs`, `GridNodeBehaviour.cs`,
`Assets/Scripts/Pathfinding/AStarPathfindingService.cs`,
`Assets/Scripts/TowerDeployment/TowerPlacementValidator.cs`, and the preview-query
portion of `TowerPlacementController.cs`. MapRuntimeNodeIndex.cs contains the owned lookup storage. StageCompositionController
checks index readiness at the existing Stage preparation boundary. No Monster
movement code was changed.

Keep A* neighbor order, tie-breaking, temporary blocked-node semantics, lane
identity, route rejoin/relocation policy, and numerical authoring unchanged.
Full Tile visual regeneration, spatial partitioning, object pooling, and a new
pathfinding algorithm are outside this task.

## 4. Documentation And Review Decisions

Read [Map](../System/05_MapSystem.md), [Monster](../System/07_MonsterSystem.md),
and [Placement](../System/09_TowerPlacementSystem.md).

Review which owner receives node-mutation notifications and the exact editor
invalidation mechanism. Keep engine callback details in this Task; update System
contracts only if accepted mutation authority or observable validity changes.

## 5. Implementation Sequence

- Capture baseline query counts, allocations, and timing on current authored maps.
- Inventory every structural/walkability mutation and map binding/clear path.
- Establish explicit revision/invalidation and replace runtime index scans.
- Add footprint-based preview reuse with independent live target validation.
- Compare paths and placement results before and after on identical fixtures.

## 6. Acceptance

| Case | Required evidence |
|---|---|
| Repeated GetNode/neighbor queries | Same results; no repeated hierarchy scan on a valid index |
| Identical stationary preview | No repeated topology search after the first valid computation |
| Changed footprint, occupancy, reset, or active Map | Correct immediate invalidation |
| Coordinate/role change or node addition/removal in supported authoring flow | Index refresh or explicit validation failure; no silent stale index |
| Route blocking, boundaries, and multi-cell Towers | Same accept/reject decisions and exact paths |
| Placement during Monster movement | Fresh route batch; continuity rules and ownership preserved |
| Stage retry/release | No previous-Map cache or node reference reused |

Use focused correctness tests for invalidation and path equivalence. Profile the
same 8x8 and 14x14 maps, same pointer sequence, build configuration, and recording
mode; report CPU time, GC allocation, and search/scan counts. Do not invent a
percentage speed target or claim an unmeasured device improvement.

Historical Task010A's dedicated movement matrix was not completed. Execute the
relevant route-continuity cases touched here and identify the remaining historical
gap honestly. Ordinary Stage clears alone do not establish movement acceptance.

## 7. Implementation And Measured Evidence

- Node owners and MapRuntimeNodeIndex now provide explicit structural/walkability
  invalidation. Read queries rebuild only after dirty state; neither lookup nor
  rebuilding advances revisions. Valid warm queries do not enumerate hierarchy.
  Index rejection clears the whole lookup; all-Normal authoring is queryable.
- Node Initialize/setters/reset notify the owner. Transfer invalidates both owners;
  Map disable/release detaches recorded owners. Generate/Clear detach removed nodes
  before deferred Destroy so immediate queries cannot resurrect old nodes.
- Runtime structural authoring must notify every affected Map. Call
  `GridNodeBehaviour.RefreshMapOwnership()` after explicit inactive-node transfers;
  detach deleted nodes before deferred Destroy and notify the previous Map.
  Raw hierarchy edits without the explicit boundary are not a supported player API.
- Editor hierarchy/Undo notifications reconcile topology snapshots outside runtime
  hot queries. Play Mode hierarchy churn is ignored; explicit mutation boundaries
  still work in Play Mode. Snapshot comparison ignores visual children. Node
  authoring revisions avoid double invalidation of already-notified changes.
- Preview cache lives in the existing Validator, called by existing preview
  orchestration. Binding/index/endpoints and full footprint are checked before
  matching Map identity, structure/walkability/binding revisions and node set.
  Configuration/footprint failures clear reuse; resolved occupied/blocked outcomes
  can be reused. Cancel/new drag and map initialization clear cache state.
- Final topology plans and Monster batches are never cached. A* neighbor order,
  tie-breaking and connector algorithms are unchanged. Editor-only counters expose
  hierarchy query/index build/search/preview query/cache-hit counts.
- `python3 Tests/Task002/run.py`: PASS. Stage1/Stage4 node data produced 32,192
  identical old/new route, connector and placement observations (including 260
  footprint preview/final/path comparisons). Current invalidation/repair/ownership
  checks passed. See the test README for exactly which Map code is extracted and
  which native APIs are doubled.
- `python3 Tests/Task001/run.py`: PASS, all 29 existing managed cases.
- Runtime static build: PASS, 0 warnings/errors. Editor static build (including
  runtime dependency): PASS, 0 warnings/errors. Builds use serial MSBuild with
  `-p:LangVersion=8.0`; generated project defaults remain Unity-owned.
- Whitespace check: PASS.
- User-reported Unity Stage1 smoke acceptance: Tower deployment followed placement
  rules; Monsters relocated normally after deployment without getting stuck.
  No overall abnormal behavior was observed. This confirms the observed deployment
  and relocation flow, not every route-continuation/rejoin or boundary fixture.
  No Recorder JSON, Console counts, native profiling or device evidence was
  supplied or independently verified with this feedback.

Managed benchmark: 100 stationary preview queries, identical authored node data,
Mono compiler/runtime, warmed fixture, no Recorder, one cold lookup then 99 repeats.
Measurements from the completed comparison run:

| Fixture / version | Hierarchy scans | A* searches | Managed elapsed ms | Allocated bytes |
|---|---:|---:|---:|---:|
| Stage1 8x8 baseline | 7900 | 100 | 84.378 | 14744088 |
| Stage1 8x8 current | 0 | 1 | 0.146 | 39440 |
| Stage4 14x14 baseline | 15500 | 100 | 542.548 | 93951240 |
| Stage4 14x14 current | 0 | 1 | 0.222 | 49728 |

These are managed harness measurements, including allocation/time inside hierarchy
API doubles; they are not Unity CPU/GC or device performance numbers. Residual
footprint allocations remain. No zero-GC claim or percentage speed target is made.

## 8. Targeted Unity Acceptance - Remaining Checks Waived

The user accepted deferring the remaining Editor/performance specialist checks
(the third test group in the conversation) until a bug or unexpected behavior
appears, and requested moving on to Task003. Task002 is closed on the existing
managed/static evidence and user-reported Stage1 deployment/relocation smoke.
Remaining detailed native checks below are waived for this iteration, not passed;
retain them as regression references. No native performance improvement, complete
movement matrix, or lifecycle coverage is inferred from the smoke run.

1. Import/compile in Unity and exercise the all-Normal Generate workflow before
   setting Spawn/Target. Validate and prepare the authored Stage maps. Clear and
   immediately regenerate in Play Mode; no old deferred-destruction nodes may
   appear in the new index. No prefab/scene rewiring is required by this change.
2. In Edit Mode, edit an inactive node, reparent it between two Maps, and perform
   Undo/Redo. Introduce and repair duplicate coordinates. Queries must fail on a
   malformed index and recover after repair, with both affected Maps invalidated.
3. Execute actual Tile/Feature refresh: compare StructureRevision,
   WalkabilityRevision and NodeIndexBuildCount before/after. None may advance
   solely for presentation. The hierarchy event may perform an authoring check;
   this is separate from an index rebuild or per-query runtime scan.
4. Profile the same 8x8/14x14 maps, pointer sequence, build configuration, and
   Recorder setting before/after. Record CPU time, GC allocation and the counters
   on MapGeneratorBehaviour, AStarPathfindingService and TowerPlacementValidator.
   A valid stationary preview should search once until its key changes; changing
   occupancy/footprint/binding must invalidate it. Final pointer release searches
   afresh. Native before measurements require a separate checkout of the baseline.
5. Place while Monsters move: verify route continuation, reachable connector
   rejoin, and forced relocation when current Grid is covered. Inspect fresh
   `AR_Task002_<case>` Recorder output for continuity and unchanged Health/Buff/
   combat ownership. Managed connector equivalence does not execute Monster state.
6. Smoke multi-cell/boundary/blocked-route placement, Level Up/Upgrade target
   eligibility, cancellation/new drag, Stage failure/retry/release and subsequent
   Stage entry. Cached preview results must not survive an invalid binding or
   authorize a stale final plan.

Task004 must preserve these keys, clearing boundaries and fresh final preparation
when moving preview orchestration. The historical Task010A movement acceptance
gap remains; this task does not claim to close it using ordinary Stage clears.
