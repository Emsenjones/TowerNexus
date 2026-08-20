# Task006 - Monster Route Reprojection And Lane Movement

Status: Completed; System contracts synchronized; Phases A-D runtime, static, and Play Mode validation accepted

Depends on: Task001 Stage1 To Stage6 Greybox Map Prefabs; Task005 Monster Roster Baseline; existing Map, Monster, Pathfinding, and Tower Placement runtime contracts

Blocks: Task007 Monster Movement Identity And Wave Composition regression; Task008 Per-Stage Progression And Stage Skeleton pacing regression; Task009-Task014 Stage calibration

## 1. Goal

Make Tower deployment a reliable way to reshape the active Monster route without letting live Monster instances veto an otherwise legal placement, while replacing exact grid-center queues with deterministic lane movement that remains compatible with the existing grid A* authority.

Task006 does not replace the Map grid or A* pathfinding. It strengthens the movement state and defines an atomic placement-time route-reprojection transaction.

## 2. Source Documents

- `Doc/Task/Task001_Stage1ToStage6GreyboxMapPrefabs.md`
- `Doc/Task/Task005_MonsterRosterBaseline.md`
- `Doc/System/05_MapSystem.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/09_TowerPlacementSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/12_ProjectileSystem.md`

## 3. Current Problem

The current placement validator checks not only whether the simulated Tower footprint preserves a Spawn-to-Target path, but also whether every live Monster's logical current node remains unblocked and can still reach the Target. A Monster moving between two nodes still reports the previous node as its current node. These rules make placement timing depend on transient Monster positions and can reject a globally legal route modification until the Monster leaves a particular Grid.

After a successful placement, the current recalculation also starts each Monster from its logical current node. It does not define what happens when the footprint covers that node, the next node, or a branch that has just become disconnected.

All Monsters currently move toward the exact center of each route Grid. A populated Wave therefore forms a visually rigid single-file queue even though Grid occupancy is not exclusive.

## 4. Design Decisions

### 4.1 Grid A* remains authoritative

- `GridNodeBehaviour` walkability and the Map's four-neighbor graph remain the route topology.
- A* continues to answer connectivity and provide ordered Grid routes.
- Task006 does not introduce NavMesh baking, free-space navigation, local obstacle avoidance, flocking, RVO, or a third-party pathfinding plugin.
- Tower footprints continue to modify runtime walkability through the existing occupancy authority.

### 4.2 Placement legality is independent of live Monster positions

A new Tower placement is legal when all existing placement constraints pass and the simulated post-placement topology contains at least one Spawn-to-Target route.

Live Monster instances do not veto that result. In particular, placement must not fail merely because a Monster currently occupies, is approaching, or is logically attached to a Grid covered by the new footprint, or because the Monster entered an old branch that the new footprint disconnects.

If the simulated topology has no Spawn-to-Target route, placement fails before any Tower or Monster state changes.

### 4.3 One authoritative projection route

After simulating the footprint, Task006 calculates one ordered authoritative route from the Map Spawn Grid to the Target Grid under the new topology. This route is the only projection domain for affected Monsters.

The transaction must use one stable A* result for the placement. It must not independently select different equivalent Spawn-to-Target routes or calculate per-Monster route tails during the same deployment.

### 4.4 Affected Monster definition

A living, unresolved Monster is affected when at least one condition is true:

- its logical current node is covered by the simulated Tower footprint;
- its active next node is covered by the simulated Tower footprint;
- its remaining route crosses the simulated Tower footprint;
- its movement state cannot be continued safely after the topology revision.

Because current placement only adds blockers, an existing remaining route that does not intersect the new footprint remains valid. Unaffected Monsters therefore preserve their current world position, active segment, route index, and complete remaining route without another A* query or prepared movement revision. They are not snapped to the authoritative route merely because another Monster was reprojected.

### 4.5 Closest-Grid reprojection

For each affected Monster:

1. Capture its pre-placement world position and pre-placement remaining-route distance.
2. Iterate every Grid in the authoritative post-placement Spawn-to-Target route except the Target Grid.
3. Select the Grid whose center is closest to the captured world position.
4. If multiple Grids are equally close, prefer the candidate whose remaining distance to Target changes least from the Monster's pre-placement remaining distance.
5. If a tie remains, prefer the candidate that does not advance the Monster for free.
6. If a tie still remains, use stable route order and Grid coordinates so the result is deterministic.

The first rule is spatial continuity. Avoiding free forward progress is a best-effort tie-breaker rather than a reason to reject placement or invent a complex fairness solver. A small forward or backward correction caused by deployment is acceptable.

The Target Grid is never a reprojection candidate for a living unresolved Monster. Even when it is spatially closest, the Monster must project to an earlier route Grid and then reach the exact Target center through ordinary movement. Spawn remains a valid candidate.

The Monster is placed on the selected route Grid's resolved movement position and continues toward the next route node. Multiple Monsters may project to the same Grid because Monster Grid occupancy is not exclusive.

Remaining-route continuity uses a centerline-only comparison. `RemainingCenterlineDistance` is measured in the active Map's local XZ frame from the Monster's logical progress along its current centerline segment, followed by Grid-center-to-Grid-center distance for every later segment. Phase D lane offsets and their slightly longer physical traversal are never included in this fairness tie-break. The movement snapshot exposes `HasComparableRemainingDistance` separately; zero distance, snapshot validity, or proximity to Target must not be overloaded to mean that the comparison is unavailable.

### 4.6 Reprojection preserves Monster identity and combat state

Reprojection is movement correction, not combat resolution. It must:

- preserve current and maximum Health;
- preserve active Buffs, Effects, Slow state, durations, and source attribution;
- preserve Monster runtime identity and registration;
- preserve existing Tower target references when the Monster remains alive and targetable;
- avoid dealing damage, healing, killing, leaking, or granting Player progress;
- avoid duplicate registration, deregistration, or resolution events; and
- leave already released Projectile direction, landing-position snapshot, lifetime, and hit rules unchanged. Reprojection does not destroy, recreate, redirect, or guarantee a hit for an in-flight Projectile; the Projectile may miss after the Monster moves.

### 4.7 Placement is one atomic transaction

The deployment flow is:

1. Resolve the candidate footprint and simulate its post-placement walkability.
2. Complete Tower and held-Draft preflight, including Tower definition, Level 1 configuration and model, runtime visual and combat requirements, required runtime owners, and Pending Draft ownership.
3. Find one authoritative Spawn-to-Target route in the simulated topology.
4. Capture living Monster movement state, identify affected Monsters, and precompute every affected Monster's projection Grid, movement target, and authoritative-route slice.
5. Create and initialize the Tower to a ready state without committing occupancy, moving Monsters, registering it as deployed, activating combat, or consuming the Draft. Ready means the Tower is initialized, its required Level model and visual state exist, and combat ownership is valid, while its combat runtime remains inactive.
6. If any pre-commit validation, preparation, or Tower-readiness step fails, destroy any temporary Tower and abort without modifying occupancy, Monster state, or held-Draft ownership.
7. Commit Tower occupancy.
8. Apply the complete prepared Monster revision batch synchronously through one non-failing state-write path; this step performs no pathfinding or new validation.
9. Register the Tower as deployed and activate its battle runtime.
10. Consume the held Tower Draft as part of the same gameplay commit.
11. Refresh runtime Tile presentation and request Tower success presentation after the gameplay commit.

Steps 7 through 10 are one gameplay commit. Every step is a synchronous, prevalidated state write without an ordinary failure result. Tile refresh and success presentation are post-commit presentation: failure is diagnosed but does not roll back the accepted Tower, occupancy, Monster state, registration, combat activation, or Draft consumption.

Because a valid global route is the acceptance boundary, an affected Monster cannot make the placement fail. Every affected Monster has a fallback projection candidate on that validated route.

Revision-batch preparation may reject only a technical precondition that invalidates the placement transaction as a whole, including an invalid topology plan, a missing required runtime owner, or an authoritative route with no eligible non-Target projection Grid. It must not reject because a Monster occupies the footprint, has an invalid movement snapshot, is near Target, or cannot continue its old route. An invalid snapshot uses deterministic fallback preparation. If the captured world position is not comparable, the first non-Target Grid in stable authoritative-route order is selected and diagnostics record the fallback; this does not reject a topology-legal placement.

No observer may see a committed Tower footprint while affected Monsters still follow an invalid old route for a frame.

### 4.8 Movement state must represent the active segment

Monster movement state must expose enough information to reason about physical position between nodes. At minimum it distinguishes:

- previous or segment-start node;
- next or segment-end node;
- current ordered route;
- current route index;
- progress or physical position along the active segment; and
- the Monster's stable per-instance lane identity.

`GetCurrentNode()` alone is not sufficient for placement-time impact or reprojection decisions.

The complete route representation obeys all of the following invariants while a Monster has an active segment:

- `CurrentRoute[RouteIndex] == ActiveNextNode`;
- `RouteIndex > 0`;
- `CurrentRoute[RouteIndex - 1] == ReachedNode`; and
- `CurrentRoute[last] == TargetNode`.

Ordinary route assignment accepts a tail beginning at `ActiveNextNode` and constructs this complete representation internally. Callers do not choose whether a supplied route includes `ReachedNode`. Phase A adds one narrow Monster-owned movement-target seam, `ResolveMovementTargetPosition(GridNodeBehaviour destinationNode)`, initially resolving the destination Grid center. It is not a public general-purpose Map-position API; Phase D extends the same seam with deterministic lane resolution.

### 4.9 Deterministic per-instance lane movement

Each Monster instance owns a stable runtime lane seed or lane identity. Shared serialized Monster configuration may define the permitted XZ offset range, but it must not serialize one fixed offset that every instance shares.

For each traversed non-terminal destination Grid, the movement target is derived deterministically from exactly:

- the Monster's stable instance lane identity;
- the destination Grid Position; and
- the authored maximum XZ offset.

The authored maximum is symmetric in the Map-local frame: local X resolves within `[-maxX, +maxX]` and local Z resolves within `[-maxZ, +maxZ]`.

The first version does not include segment identity or any optional hash input. The same Monster resolving the same destination Grid must resolve the same offset instead of visibly jittering. Offset generation must remain within the safe walkable corridor and must not change which Grid owns the route.

Monster System resolves the offset in the active Map's local XZ frame using the Map's Node Size and Nodes Root. Grid Node state remains topology-only and does not own Monster lane configuration or lane resolution.

The gameplay root follows the resolved offset position so the visual model, targeting, Projectile destination, range checks, and area effects agree on the Monster's actual position. Task006 therefore requires focused combat-position regression rather than separating a visual child from gameplay coordinates.

### 4.10 Prepared-data boundaries

Task006 introduces at most four focused runtime data types:

- `MonsterMovementSnapshot`: pure copied movement state with no Monster reference and no live mutable route collection;
- `TowerPlacementTopologyPlan`: one copied candidate footprint and one copied authoritative projection route; the footprint is also the temporary blocker set and is not duplicated;
- `MonsterRouteRevisionEntry`: one Monster reference plus its already validated final movement state; and
- `MonsterRouteRevisionBatch`: copied revision entries plus read-only diagnostics data.

Every route, footprint, and entry collection is copied when its owner is constructed. Wrapping an existing mutable `List` as `IReadOnlyList` does not satisfy this boundary.

Prepared batch application does not reconstruct route slices, projection choices, or affected/unaffected classification data. Ordinary collection growth is not a correctness failure and Task006 does not introduce pooling or a zero-allocation requirement.

Phase A does not add speculative route-application APIs. Phase C introduces one state-write path, `ApplyPreparedMovementRevision`, consumed by every prepared revision entry. Ordinary movement assignment and reprojection do not receive separate low-level mutation paths.

### 4.11 Spawn and Target remain exact

- A Monster spawns at the Spawn Grid's exact world position with zero lane offset.
- A Monster must reach the Target Grid's exact world position before resolution.
- A living unresolved Monster cannot be reprojected directly onto the Target Grid.
- No offset may resolve a Monster early or leave it visually beside the Target when it is counted as leaked.

## 5. In Scope

- Placement validation independent of live Monster occupancy
- Simulated post-placement topology and one authoritative route result
- Affected-Monster detection using active movement state
- Closest-route-Grid reprojection with deterministic tie-breaking
- Atomic Tower occupancy, affected-Monster revision, and held-Draft consumption commit
- Richer between-node movement state
- Deterministic per-instance XZ lane offset
- Spawn and Target zero-offset handling
- Affected-only prepared movement revision while unaffected Monsters retain their valid existing routes
- Copied topology, snapshot, revision-entry, and revision-batch boundaries
- Range, Projectile, area-effect, Buff, Effect, and resolution regression
- System-document synchronization required by the accepted implementation

## 6. Out Of Scope

- Replacing grid A* or authoring NavMesh bake data
- Dynamic local avoidance, collision separation, crowd simulation, or physical Monster blocking
- Exclusive Monster ownership of a Grid
- Tower removal, relocation, selling, or footprint resizing after deployment
- Damage, stun, knockback, or Player-progress reward caused by reprojection
- Final lane-offset values for every Monster visual size beyond the minimum safe first pass
- Monster Health, Move Speed, role, Wave Count, Spawn Interval, Wave Delay, Draft probability, or Stage difficulty tuning
- The Fodder Profile; owned and now accepted by Task007 after Task006

## 7. Ownership

| Owner | Responsibility |
|---|---|
| Map System | Grid topology, node coordinates, Spawn/Target identity, and runtime walkability |
| A* Pathfinding Service | Connectivity query and deterministic ordered Grid route |
| Tower Placement System | Simulated footprint validation and orchestration of the atomic placement transaction |
| Monster Manager | Stable snapshot of living Monsters and batch route/reprojection application |
| Monster Behaviour | Active-segment state, per-instance lane identity, physical movement, and prepared-route application |
| Tower/Projectile/Effect runtimes | Continue using the Monster's real gameplay position and preserve valid target/source relationships |
| Task007 | Regress movement Profiles and spacing, then own Fodder identity acceptance |
| Task008 | Recheck early-Draft cadence and Wave Delay perception after movement changes |
| Task009-Task014 | Stage-local numeric calibration after upstream acceptance |

## 8. Implementation Sequence

1. Task006-A, Movement State Foundation: define reached-node, active-next-node, complete-route, route-index, and active-segment invariants; add a copied movement snapshot, centerline remaining-distance comparability, and a narrow zero-offset target resolver; preserve the active segment during ordinary route assignment; do not add a prepared-revision apply API yet. The primary modification scope is `MonsterBehaviour.cs` plus the focused snapshot type.
2. Task006-B, Simulated Topology Plan: expose one temporary-blocker route query, create the copied footprint plus authoritative projection-route plan, remove Monster Manager and all live-Monster vetoes from placement validation, and begin topology failure diagnostics.
3. Task006-C, Route Revision Transaction: classify only footprint-intersecting or invalid-snapshot Monsters as affected, apply deterministic fallback instead of Monster-specific rejection, prepare copied revision entries and one batch, add the single non-failing prepared movement apply path, close Tower and Draft preflight, and commit occupancy, affected revisions, Tower registration, combat activation, and Draft consumption before presentation. Modification scope includes the existing deployment orchestrators (`TowerDeployController.cs` and `TowerPlacementController.cs`), Monster Manager/Behaviour, Tower definition and Level validation, Tower visual/combat readiness, and Battle HUD held-Draft ownership; do not introduce a second Tower validator or readiness service.
4. Task006-D, Deterministic Lane Movement: derive gameplay-root offsets only from Lane Identity plus Destination Grid Position in the Map-local frame, keep Grid Node topology-only, and apply Spawn/Target zero-offset exceptions.
5. Run diagnostics and acceptance continuously across Task006-B through Task006-D; diagnostics and final acceptance are validation tracks, not a fifth implementation phase.

## 9. Required Diagnostics

For a development build or existing debug channel, one placement record should make the following reviewable without becoming permanent player-facing UI:

- outcome: `Rejected`, `Accepted`, or `AcceptedWithPresentationWarning`;
- failure stage and failure reason, when the transaction does not commit;
- selected Tower footprint;
- whether the simulated Spawn-to-Target route exists;
- authoritative route node count;
- affected Monster count;
- each affected Monster's pre-placement position;
- chosen projection Grid;
- world displacement distance;
- pre/post remaining-route distance delta; and
- whether the tie-breaker produced forward or backward progress.

Diagnostics begin with topology planning in Task006-B and expand with revision preparation and commit in Task006-C. They must not create a second placement or pathfinding implementation.

## 10. Unity Authoring Checklist

- Author one conservative maximum XZ lane-offset range on the relevant Monster runtime Prefabs or shared Monster movement configuration.
- Keep the per-instance lane seed runtime-owned; do not enter a unique manual offset for every spawned Monster.
- Confirm the lane range does not visually leave a Grid corridor on narrow Stage routes.
- Confirm Spawn and Target use zero offset.
- Confirm large-footprint Cannon and Drone placements use the same transaction as small footprints.
- Preserve existing Prefab and serialized references while adding movement configuration.

## 11. Static Validation

- Runtime and Editor assemblies compile.
- Tower placement contains no live-Monster veto after global topology acceptance.
- Reprojection selection iterates the single prepared authoritative route.
- Unaffected Monsters retain their active segment and remaining route without another A* query.
- Every snapshot, topology plan, revision entry, and batch owns copied collections rather than live mutable lists.
- The complete revision batch uses one prevalidated, non-failing apply path and performs no pathfinding during commit.
- Tower occupancy, affected-Monster revisions, Tower registration, combat activation, and Draft consumption commit before Tile or success presentation.
- Existing Monster resolution remains exactly once.
- All targeting, range, Projectile, area-effect, Buff, and Effect code reads the gameplay position consistently.
- Spawn and Target exceptions are explicit and testable.
- No NavMesh or second pathfinding authority is introduced.
- System docs and Task references match implemented ownership.

## 12. Play Mode Validation Matrix

| Scenario | Required Result |
|---|---|
| Tower placed away from one Monster's remaining route | Placement succeeds when the simulated global route exists; that unaffected Monster preserves its world position, active segment, and remaining route without another path query |
| Tower placed on a Grid occupied by one Monster | Placement is not delayed; the Monster is reprojected and continues without combat resolution |
| Large footprint covers several Monsters or next nodes | One atomic commit reprojects every affected Monster; none remain inside the footprint |
| Old narrow branch is cut while a distant alternate Spawn-to-Target route remains | Placement succeeds; trapped-branch Monsters map to the closest Grid on the authoritative new route |
| Monster is physically between nodes | Impact and projection use active segment/world position rather than only the stale logical current node |
| Monster is near Target | Reprojection does not resolve early, duplicate a leak, or bypass the exact Target position |
| Multiple Monsters select the same projection Grid | All remain valid and continue; no exclusive-occupancy failure is introduced |
| Monster has active Slow, Buff, or Effect | Health, modifiers, duration, and attribution survive reprojection |
| Projectile is already in flight | Its release-time direction or landing position is unchanged; it may hit or miss under the existing flight and hit rules without destruction, recreation, or redirection |
| The same Monster resolves the same destination Grid repeatedly | Its Lane Identity plus Destination Grid Position produces the same movement target without jitter |
| No-Tower route traversal with lane offsets | Completion time remains acceptably close to the offset-zero baseline without speed compensation |
| Long straight route and ordinary corners | Bounded lane targets do not create obvious left-right zigzag or unstable cornering |
| Fresh spawn and final resolution | Spawn and Target positions remain exact Grid centers |
| Reference Tower range and area attacks | Small gameplay-root offsets do not create unexplained misses or invalid coverage |
| Tile refresh or success presentation fails after commit | Tower occupancy, affected-Monster revisions, and Draft consumption remain committed; the presentation failure is diagnosed without rollback |
| Stage Retry or transition | No lane seed, route state, occupancy, or Monster registration leaks into the next battle |

## 13. Acceptance Criteria

- Any placement that passes footprint rules and preserves a Spawn-to-Target route succeeds regardless of live Monster positions.
- A placement with no simulated Spawn-to-Target route still fails without changing runtime state.
- Every affected Monster is deterministically mapped to the spatially closest Grid on one authoritative new route.
- Every unaffected Monster preserves its existing valid active segment and remaining route without another A* query or revision entry.
- Tie-breaking minimizes remaining-route discontinuity and avoids free forward progress when practical without blocking placement.
- Tower occupancy, all affected Monster routes, and held-Draft consumption become visible as one logical gameplay commit before Tile and success presentation.
- The prepared batch applies through one prevalidated, non-failing movement-state path without pathfinding during commit.
- Reprojection does not damage, heal, kill, leak, resolve, duplicate, or deregister a Monster.
- Active combat state and valid target/source relationships survive reprojection.
- Lane movement visibly reduces exact-center single-file queues without changing grid topology authority.
- Lane movement remains within its symmetric authored bounds, does not create obvious zigzag, and keeps no-Tower traversal time acceptably close to the centerline baseline without speed compensation.
- Each Monster owns stable runtime lane identity; resolving the same destination Grid does not reroll its movement target.
- Gameplay visuals and combat coordinates remain aligned.
- Spawn and Target use exact Grid centers.
- A living unresolved Monster is never reprojected directly onto the Target Grid.
- Reprojection never redirects, recreates, destroys, or guarantees an in-flight Projectile hit.
- The validation matrix passes on at least one small-footprint and one large-footprint Tower case.
- Task007 does not begin its roster/spacing regression until Task006 static and Play Mode acceptance is recorded.

## 14. Handoff

After Task006 acceptance:

1. Task007 accepted Fodder Maximum Health `60`, Move Speed `0.25`, Spawn Interval `2.5s`, and matched Normal controls; its remaining focused handoff regresses Rush, Tough, and Tank under lane movement and confirms that the `0.625` standard spatial-gap rule still produces comparable formation spacing.
2. Task008 reruns early-Stage Draft cadence and Wave Delay perception using the accepted movement/roster result. It records Draft candidate categories before deciding whether DraftSystem weighting needs an independent change.
3. Task009-Task014 begin Stage-local Wave order, Count, Spawn Interval, Wave Delay, Progress, and difficulty calibration only after the Task007 and Task008 handoffs are accepted.

Minor placement-induced forward or backward Monster correction is an accepted tradeoff. If Play Mode reveals an exploitable free-knockback pattern, refine the tie-breaker or cap projection discontinuity inside Task006 before changing any Stage numbers.
