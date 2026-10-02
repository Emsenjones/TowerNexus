# Task001 - Monster Path Route Queries

Document Set: Task
Status: Planned; implementation and runtime acceptance have not begun.
Dependencies: None in this task set.
Next: [Task002 - Monster Dashed Path Presentation](Task002_MonsterDashedPathPresentation.md).

## 1. Goal

Make the formal Spawn-to-Target main route and a read-only hypothetical
post-deployment main route available for Monster dashed-line path feedback.
Preserve existing placement legality, A* route ordering, and final deployment
validation. This task delivers route data; drag transitions and rendering are
implemented by Task003 and Task002 respectively.

## 2. Source Documents

- [Monster System](../System/07_MonsterSystem.md), Section 6.3: main-route semantics and route-query ownership.
- [Tower Placement System](../System/09_TowerPlacementSystem.md), Sections 5.1 and 7.5: candidate constraints and dashed-line states.
- [Map System](../System/05_MapSystem.md), Sections 9 and 10.1: spatial data and query validity.
- [Stage System](../System/02_StageSystem.md), Section 3.1: route availability at Battle start.

System documents are the design authority. This Task is a bounded implementation
contract and does not authorize changes to the approved behavior.

## 3. Scope

- Query the formal ordered shortest route from the active Map's Spawn to Target
  without requiring a live Monster or authorization to start Wave timing.
- Resolve every occupied Tower anchor before evaluating a candidate; use the
  complete unique footprint as additional blockers.
- Return the ordered candidate route alongside placement-topology validity.
- Distinguish route available, route blocked, and candidate unavailable.
  Missing bindings or invalid Map data must remain diagnosable technical
  failures rather than being classified as route blocking.
- Extend the existing preview cache to retain the route and its outcome, so
  Tower validity and dashed-line feedback can share the same topology query.
- Preserve fresh final placement preflight and its authoritative route.

## 4. Out Of Scope

Dashed geometry, colors, opacity, drag-state retention, Stage display wiring,
Monster connector or relocation prediction, lane changes, new pathfinding
algorithms, Tower rotation/removal, multiple Spawn/Target support, and broad
Map/query architecture refactoring.

## 5. Ownership And Integration Boundary

Monster System owns pathfinding execution and route semantics. Map System owns
node identity, effective walkability, and topology revisions. Tower Placement
System resolves candidate eligibility and owns its existing preview-query
cache. A query result is data, not permission to commit or display a state.

Current entry points to inspect during implementation:

- `Assets/Scripts/Pathfinding/AStarPathfindingService.cs`: existing formal and temporary-blocker queries.
- `Assets/Scripts/TowerDeployment/TowerPlacementValidator.cs`: complete footprint resolution, preview cache, and final topology planning.
- `Assets/Scripts/TowerDeployment/TowerPlacementCandidate.cs`: final candidate freshness.
- `Assets/Scripts/Map/MapGeneratorBehaviour.cs`: node queries and topology revisions.

Use the existing owners and A* ordering. Add only the route-result capability
needed by this feature; do not introduce a generic route broker or global
service locator.

## 6. Required Implementation Contract

1. Both formal and valid candidate routes include Spawn and Target, traverse
   orthogonal effectively walkable nodes, and preserve existing shortest-route
   tie selection.
2. An otherwise valid complete footprint that leaves no route yields Route
   Blocked. Out-of-Map, occupied, base-unwalkable, or incomplete footprints
   yield Candidate Unavailable. Technical query failures are distinguishable
   from these gameplay outcomes.
3. Published route data cannot be changed by clearing or reusing query buffers.
   Consumers must not mutate cached results. Its validity is bound to the
   originating Map/pathfinding binding and topology.
4. Resolve the complete footprint before cache lookup. Cache identity includes
   the active Map/service binding, structure and walkability revisions, and
   full node identities, independent of occupied-anchor ordering.
5. Repeated equivalent candidates may reuse one result. Changed footprints,
   relevant revisions, rebinding, invalid footprint resolution, or release
   prevent stale-result reuse. Do not cache technical configuration failures.
6. Querying changes no authored terrain, runtime occupancy, Tile/Feature
   presentation, live Monster movement, registration, or Draft ownership.
7. Final deployment independently prepares its topology plan. The preview
   cache must not bypass that preflight. Under unchanged topology and footprint,
   successful placement's authoritative route exactly matches the preview.

## 7. Unity Authoring Checklist

- This task requires no new visual asset, scene object, or Inspector reference.
- Use existing authored Map endpoints and Tower footprint anchors.
- Include real campaign Maps and multi-cell Tower footprints in fixture checks.
- Any discovered invalid authoring is reported separately rather than repaired
  as part of the query change.

Inspector changes, Unity import/reserialization, and Play Mode remain user-owned
unless explicitly delegated.

## 8. Acceptance Criteria

- Formal route data is available before any Monster exists.
- Valid candidate paths match the same A* query used by final placement,
  including cases with equal-length alternatives.
- A complete blocking footprint is distinguished from overlap, partial
  out-of-bounds, malformed anchors, and technical query failure.
- Multi-cell and reordered-anchor candidates use the full footprint.
- Stationary equivalent queries reuse the route result without an additional
  topology search; final preflight still performs a fresh evaluation.
- Occupancy changes, binding replacement, and Map replacement invalidate stale
  results; no candidate query changes gameplay state.

## 9. Validation And Evidence

- Compile the runtime assembly with the repository's established static gate:
  `dotnet build Assembly-CSharp.csproj --no-restore -m:1 -nr:false -p:LangVersion=8.0`.
- Extend relevant executable route/query fixtures. Existing
  `Tests/Task002/run.py` covers the older Map query-caching contract; it is a
  regression harness, not the new Task002 presentation task.
- Verify ordered path equivalence, complete-footprint handling, outcome
  classification, invalidation, read-only behavior, and fresh final preflight.
- Use query-count evidence for cache reuse; do not claim runtime performance
  improvement without measurement.
- Run `git diff --check` on changed files.
- Report static compilation and fixture evidence separately from Unity runtime
  checks. Mark unexecuted checks explicitly.

## 10. Review And Status

Planned contract only. Before coding, present the concrete affected-code plan
and settle the minimal result shape and consumer boundary in review. Completion
requires the acceptance evidence above; document generation is not evidence of
implemented route queries.
