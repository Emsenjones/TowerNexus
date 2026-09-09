# Task004 - Tower Placement Responsibility Separation

Series: ArchitectureRefactor
Status: Draft - Pending Review
Branch: `codex/architecture-refactor`
Depends on: Accepted Task001-Task003.

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
Names and Unity component placement are review decisions, not approved wiring.

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
- Stop closes gates first and stops tracked combat; Stage release then destroys
  tracked Towers and clears membership. Preserve existing result-screen lifecycle.
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
DraftSystem's deployed-Tower query, BattleRuntimeCoordinator lifecycle calls,
and the minimum Recorder subscriptions/reference wiring required by the move.

Read and synchronize [Placement](../System/09_TowerPlacementSystem.md),
[Draft](../System/08_DraftSystem.md), [HUD](../System/04_BattleHUDUISystem.md),
and [Tower Framework](../System/10_TowerFrameworkSystem.md) if ownership changes.
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
No implementation or evidence exists yet.
