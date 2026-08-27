# Task010A - Monster Placement Route Continuity Refactor

Status: Implemented on 2026-08-27; focused Play Mode fixtures, schema-22
evidence review, and Stage1 Reference movement regression remain pending

Depends on: Completed Task010 Stage1 Wave Calibration; current Map,
Pathfinding, Monster Movement, Tower Placement, Draft transaction, Projectile,
and CombatBalanceRunRecorder contracts

Blocks: Task011-Task017. Task011 resumes only after this Task's implementation,
schema-22 evidence, and Stage1 Reference movement regression are accepted.

## 1. Goal

Replace placement-time mass projection onto the closest Grid of a newly
generated route with one deterministic placement-route revision for every
living targetable Monster.

After a candidate Tower footprint leaves at least one Spawn-to-Target route,
placement remains valid regardless of live Monster positions. The accepted
transaction produces one authoritative new Spawn-to-Target Route. A Monster
already on that Route continues without displacement; a Monster that can reach
that Route from its current Grid rejoins it through ordinary Grid movement; a
Monster covered or disconnected by the new topology is relocated to the
nearest eligible recovery Grid and then rejoins the Route.

This Task changes a shared movement contract. It does not tune Stage1 or Stage2
Monster HP, Wave counts, Wave delays, Progress Requirements, Player Health, or
Tower/Upgrade values.

## 2. Observed Problem And Current Authority

The first Task011 Stage2 Reference attempt deployed a Cannon across the old
route. Every Monster whose remaining route intersected that footprint became
affected and visibly jumped at the same instant to a nearby Grid on the shared
post-placement Spawn-to-Target route.

The runtime matches the superseded contract:

- `TowerPlacementValidator` simulates the candidate footprint and accepts it
  when at least one Spawn-to-Target route survives;
- `MonsterManager.IsAffected` classifies reached-node, active-next-node,
  remaining-route, and invalid-snapshot intersections;
- every affected Monster selects one closest eligible Grid directly on the
  authoritative post-placement route;
- `MonsterBehaviour.ApplyPreparedMovementRevision` immediately assigns that
  Grid's movement position to the Monster transform.

The result is a shared placement-route contract problem revealed by Stage2,
not a Stage2 balance failure. No Record from the interrupted run is Wave-
calibration evidence.

## 3. Fixed Placement And Route Contract

### 3.1 Placement Legality And Authoritative Route

Tower Placement remains the sole placement authority:

1. Resolve the complete candidate footprint.
2. Simulate every footprint Grid as Runtime Occupied without mutation.
3. Query one deterministic authoritative Spawn-to-Target Route under that
   simulated topology.
4. Reject without mutation when no Spawn-to-Target Route exists.
5. Otherwise capture Monster state and prepare the complete placement-route
   revision batch.

"The Tower blocks a Monster's old route" and "a Monster currently occupies the
footprint" are not rejection conditions. Once ordinary placement constraints
and the global Spawn-to-Target topology pass, live Monster timing cannot add a
second placement gate.

The authoritative new Route is the common route that every living targetable
Monster must continue on or rejoin after the transaction. Target is never a
join or relocation destination for a living unresolved Monster; it remains the
exact final movement destination. Spawn remains eligible.

### 3.2 Physical Current Grid

Monster classification uses the gameplay movement root's actual position in
the active Map's local XZ frame. A finite position resolves to one physical
current Grid using the Map's cell-boundary ownership rule. It does not use
colliders, render bounds, Tower attack range, nearest-route projection, or the
previously reached logical node as a substitute.

If the Transform is non-finite or a finite position cannot resolve to a valid
Map Grid, the Monster uses `ForcedRelocation` with an explicit reason. Multiple
Monsters may share one movement or recovery Grid because Monster occupancy is
non-exclusive.

### 3.3 Exactly One Classification Per Monster

| Revision mode | Condition | Immediate world-position rule |
|---|---|---|
| `AlreadyOnNewRoute` | The physical current Grid belongs to the authoritative new Route and is not covered by the candidate footprint | Preserve exactly; install the valid Route continuation without rejoin movement |
| `ReachableRouteRejoin` | The physical current Grid is not on the new Route, remains effectively walkable, and can reach at least one eligible new-Route Grid under the simulated topology | Preserve exactly; move through a prepared Grid connector to the selected join Grid, then follow the Route suffix |
| `ForcedRelocation` | The Transform is non-finite, the finite position has no physical Grid, the physical Grid is covered by the candidate footprint, or that Grid cannot reach the new Route | Install the nearest eligible recovery position during commit, then use a prepared connector and Route suffix |

Classification is based on the authoritative new Route for this placement. The
superseded rule that preserved an old remaining route merely because it avoided
the new footprint no longer applies. Apply this strict decision tree:

1. A non-finite Transform uses `ForcedRelocation.NonFiniteTransform`.
2. A finite position without a physical Grid uses
   `ForcedRelocation.UnresolvablePhysicalGrid`.
3. A physical Grid inside the candidate footprint uses
   `ForcedRelocation.CoveredByNewFootprint`.
4. A physical Grid on the authoritative new Route uses `AlreadyOnNewRoute`.
5. A physical Grid in the new Route's connected component uses
   `ReachableRouteRejoin`.
6. Every remaining physical Grid uses
   `ForcedRelocation.DisconnectedFromNewRoute`.

An invalid old logical route does not force relocation when a finite physical
Grid can establish a valid new continuation or rejoin.

### 3.4 Already-On-Route Continuation

`AlreadyOnNewRoute` produces no visible placement correction:

- the captured finite world position is preserved exactly;
- the Monster continues toward the next applicable Grid of the authoritative
  Route from its current physical Grid;
- lane identity remains stable;
- reaching the physical Target cell does not itself resolve the Monster.

A living unresolved Monster already inside the Target cell receives the
internal prepared flag `RequiresExactTargetApproach`. Applying that prepared
state does not install Target as the reached node. The Monster keeps its world
position and moves to the exact Target movement position under a phase-specific
approach invariant. Only physical arrival sets Target as reached and reports
resolution. Placement commit never resolves this subcase.

### 3.5 Reachable Route Rejoin

Preparation builds one immutable connected-component query for the simulated
topology. Because the graph is undirected and the authoritative Route is
continuous, membership in that component proves that the physical current Grid
can reach every eligible Grid on the Route.

The selected join Grid is the non-Target Route candidate whose lane-resolved
movement position is closest in Map-local XZ distance to the captured Monster
position. Equal-distance candidates use stable Route order and then stable Grid
order. Preparation executes at most one ordinary path query for the final join;
identical current-Grid/join-Grid pairs may share a batch-local cached connector.

Preparation supplies:

- the captured world position and physical current Grid;
- one complete orthogonal Grid connector from the current Grid to the selected
  join Grid;
- the selected join Grid and its lane-resolved movement position;
- the authoritative Route suffix from the join Grid to the exact Target.

The Monster starts the connector from its current world position without a
placement-time transform assignment. Ordinary movement controls, Slow,
Frozen, movement locks, Move Speed multipliers, and walking presentation remain
authoritative. Reaching the join Grid installs the authoritative Route suffix;
it is not Target arrival and grants no Progress.

### 3.6 Forced Relocation

`ForcedRelocation` is an explicit placement-state correction and the only mode
that may change a finite Monster position during placement commit.

Its reason is recorded as one of:

- `CoveredByNewFootprint`;
- `DisconnectedFromNewRoute`;
- `UnresolvablePhysicalGrid`;
- `NonFiniteTransform`.

An eligible recovery Grid:

- is Base Walkable and not Runtime Occupied under the simulated topology;
- is outside the complete candidate footprint;
- is not Target;
- can reach at least one eligible non-Target Grid on the authoritative new
  Route.

The recovery Grid does not need to belong directly to the authoritative Route.
One deterministic multi-source distance/join map starts from all eligible Route
Grids and records each connected Grid's minimum connector path cost, selected
join, and next step. Equal-length joins use Route index and then Grid order.

Among eligible recovery candidates, select the lane-resolved recovery position
nearest to the captured pre-relocation position in Map-local XZ. Equal-distance
candidates minimize connector path cost and then use stable recovery-Grid order.
There is no remaining-distance or free-forward-progress tie-break. When the
captured XZ position is not finite, spatial distance is explicitly unavailable
and connector path cost plus stable Grid order select the recovery.

Recovery Y is resolved independently: preserve captured Y when that component
is finite; otherwise use the recovery Grid's world Y. Lane resolution consumes
that explicit finite Y and never reads a non-finite Transform component.

Commit may assign only the prevalidated finite recovery position. From that
Grid, the Monster follows the prepared connector to its selected join Grid and
then the authoritative Route suffix. The relocation itself does not clear or
override movement locks; after relocation, ordinary movement controls decide
whether and when the Monster advances.

Because the accepted Spawn-to-Target Route contains at least one eligible non-
Target Grid, degraded Monster state remains total and does not create a second
placement-legality gate.

### 3.7 Repeated Placement During Rejoin

A Monster may still be following a connector from an earlier placement when a
new Tower is released. The new preflight captures its current physical position,
active connector, selected join, and prepared continuation.

Preparation is read-only. Failure preserves the complete old connector state.
Only a successful new placement commit atomically supersedes the old connector
with exactly one new classification and prepared movement state. Old and new
connector routes never accumulate or remain active together.

## 4. Preserved Gameplay State

Every revision mode preserves:

- current and maximum Health;
- Buff definitions, stacks, phases, durations, protection, source/cooldown
  state, movement multipliers, and movement locks;
- Monster registration, spawn ordinal, Wave attribution, lane identity, and
  resolution state;
- valid Tower and attack-entity target ownership while the Monster remains
  targetable;
- hit-reference ownership and damage eligibility;
- released Projectile direction, landing-position snapshot, lifetime, target
  identity, and hit rules;
- Draft, Player Progress, Player Health, and resolved-Monster state.

Placement-route revision never deals damage, heals, kills, leaks, resolves,
grants Progress, registers, deregisters, duplicates, or destroys a Monster.
Reaching a connector or recovery join is not reaching the gameplay Target.

## 5. Atomic Placement Transaction

Preserve the existing transaction boundary:

```text
Validate Candidate And Held Draft
    -> Prepare Authoritative Route And All Monster Revisions
    -> Prepare Tower Ready But Inactive
    -> Commit Runtime Occupied
    -> Apply Prepared Monster Revision Batch
    -> Register And Activate Tower Combat
    -> Consume Held Draft
    -> Publish Immutable Placement And Revision Facts
    -> Refresh Tile And Placement Presentation
```

All failure-capable topology, route, relocation, owner, Draft, and Tower-
readiness work occurs before commit. Commit remains a synchronous non-failing
set of prevalidated gameplay-state writes. Observers are exception-isolated and
cannot roll back or influence the accepted result. Presentation failure does
not roll back the accepted gameplay result.

## 6. Recorder Schema 22

CombatBalanceRunRecorder schema 22 adds one placement-route transaction event
for each committed new Tower and immutable lifecycle observations supplied by
gameplay runtime.

Each transaction records:

- deployment ordinal, active time, complete footprint, and authoritative Route;
- living and per-mode Monster counts;
- for every living Monster: spawn ordinal, Wave identity, physical current
  Grid, classification, captured position, immediate post-commit position,
  immediate displacement, and immutable pre/post gameplay-state fingerprint;
- for `ReachableRouteRejoin`: connector Grid path, join Grid, Route suffix, and
  planned connector distance;
- for `ForcedRelocation`: reason, recovery Grid and position, relocation
  distance, connector Grid path, join Grid, and Route suffix;
- whether Health, Buff state, movement controls, lane identity, target ownership,
  registration, resolution, Player Health, Player Progress, or resolved-Monster
  count changed during revision application.

Connector lifecycle uses stable revision identity and records:

- `Started` when a rejoin connector is installed;
- `Joined` when the Monster reaches the selected new-Route Grid and installs the
  authoritative suffix;
- `Superseded` when a later committed placement replaces an active connector;
- `MonsterResolvedBeforeJoin` with `Killed`, `Leaked`, or `TechnicalCleanup`
  when normal Monster resolution ends an active connector;
- `ActiveAtRunEnd` when a connector remains unfinished when recording ends.

`RelocationApplied` is a separate commit fact rather than a connector terminal
state. If the recovery Grid already belongs to the authoritative Route, the
zero-length connector emits no `Started`; the same commit records
`RelocationApplied` and `Joined` with `joinedAtCommit = true`.

Completion and supersession facts come from gameplay runtime. Recorder only
accumulates immutable observations and never decides Monster movement.

The Recorder fixture declares a forced-relocation expectation:

| Expectation | Integrity meaning |
|---|---|
| `RequireZero` | No forced relocation is permitted in this fixture |
| `RequireDiagnosed` | At least one correctly diagnosed forced relocation is required |
| `Ignore` | Forced relocation count does not decide fixture integrity |

Schema-22 integrity requires:

- per-mode counts reconcile with the complete living-Monster snapshot;
- `AlreadyOnNewRoute` and `ReachableRouteRejoin` have exactly zero immediate
  displacement for every finite captured position;
- `ForcedRelocation` is the only mode allowed to report finite immediate
  displacement, and its destination is the selected nearest eligible recovery
  position;
- every connector is valid under the simulated topology, reaches its recorded
  join Grid, and every Route suffix ends at exact Target;
- every `Started` connector has exactly one terminal fact: `Joined`,
  `Superseded`, `MonsterResolvedBeforeJoin`, or `ActiveAtRunEnd`;
- zero-length recovery connectors have no `Started` and exactly one
  `joinedAtCommit` fact;
- repeated placement never accumulates more than one active connector;
- exact-Target approach produces no commit-time resolution;
- immutable pre/post fingerprints prove that revision application preserves
  gameplay state and produces no combat, resolution, Progress, or Player-Health
  side effects;
- the fixture's forced-relocation expectation is satisfied.

Console diagnostics remain useful for visual debugging but are not acceptance
evidence without JSON lifecycle and integrity reconciliation.

## 7. Implementation Slices

1. Synchronize `Doc/System/05_MapSystem.md`, `07_MonsterSystem.md`,
   `09_TowerPlacementSystem.md`, and the unchanged released-Projectile boundary
   in `12_ProjectileSystem.md` before runtime changes.
2. Replace closest-route projection data with explicit physical-Grid,
   strict classification, exact-Target approach, Grid-connector, Route-suffix,
   relocation, shared connectivity, and lifecycle contracts.
3. Refactor Monster route preparation to classify every living targetable
   Monster against the authoritative new Route and simulated occupancy.
4. Extend Monster movement with one explicit route-rejoin state that preserves
   ordinary movement controls and supports committed supersession.
5. Preserve Tower Placement's atomic commit ordering and publish immutable
   revision facts only after occupancy, Monster revision, Tower activation, and
   Draft consumption have completed.
6. Upgrade CombatBalanceRunJsonReport and CombatBalanceRunRecorder to schema 22
   with transaction, lifecycle, state-fingerprint, and fixture-expectation
   integrity.
7. Run focused static builds, Unity authoring validation, Play Mode fixtures,
   Stage1 Reference regression, and the renewed Stage2 Phase A run.

## 8. Required Play Mode Fixtures

1. Deploy while a Monster's physical current Grid already belongs to the new
   Route: position is unchanged and it follows the correct Route continuation.
2. Change the Route while an off-route Monster can still reach it: immediate
   displacement is zero, the nearest reachable join is selected, and the
   Monster visibly follows its Grid connector.
3. Cover one Monster's physical current Grid: only that Monster receives a
   diagnosed nearest-eligible forced relocation and then rejoins the Route.
4. Create a pocket whose current Grid cannot reach any new-Route Grid while the
   global Spawn-to-Target Route survives; placement succeeds and the Monster
   receives diagnosed forced relocation. Exercise non-finite spatial fallback
   as a separate technical subcase.
5. Revise several Monsters at different positions: per-Monster modes reconcile
   and no mass closest-route projection occurs.
6. Repeat with active Slow, Frozen/movement lock, Buff stacks and protection,
   damage, lane identity, and Tower/attack-entity target ownership; all state
   survives.
7. Repeat while Archer, Cannon, Magic, and Drone Projectiles are in flight;
   release-time direction, landing snapshots, target identity, lifetime, and
   existing hit-or-miss rules remain unchanged.
8. Place near Target: Grid membership or relocation never resolves early,
   leaks, grants Progress, or changes Player Health.
9. Attempt a footprint that removes every Spawn-to-Target Route: placement and
   Draft consumption are rejected with zero topology, Monster, Tower, or Player
   mutation.
10. Deploy again while a Monster is rejoining: failed preflight preserves the
    old connector, while a successful commit records `Superseded` and installs
    exactly one replacement connector that later joins normally.

Focused Map-query regression additionally proves internal and outer-boundary
ownership, epsilon-outside rejection, non-finite rejection, and unchanged Tower
preview, existing-Tower targeting, and footprint-anchor lookup.

## 9. Acceptance And Downstream Handoff

- Both Runtime and Editor serial builds pass with zero errors.
- System documents, runtime behavior, schema-22 JSON, and integrity rules agree.
- All ten focused fixtures pass with reviewed visual behavior and JSON evidence.
- Every finite `AlreadyOnNewRoute` or `ReachableRouteRejoin` snapshot has zero
  placement-time displacement.
- Every finite forced relocation is diagnosed and resolves to the selected
  nearest eligible recovery position.
- No fixture produces mass closest-route projection or accumulated connectors.
- Stage1 Reference at the accepted placement and Stage1 V4 values retains its
  accepted result; any regression explicitly reopens Task010 rather than being
  hidden by Stage2 values.
- Focused evidence is reviewed first, then Stage1 regression is accepted, then
  Task010A becomes Completed. Recorder file generation alone is not acceptance.
- Task011's first Stage2 attempt remains excluded from balance evidence.
- After acceptance, Task011 returns to `In Progress`, keeps its frozen Reference
  Build and Stage2 V1 Wave candidate, updates its RunName to schema 22, and
  restarts Phase A placement/pressure measurement from a fresh run.
