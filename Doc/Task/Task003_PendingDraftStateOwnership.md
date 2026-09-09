# Task003 - Pending Draft State Ownership

Series: ArchitectureRefactor
Status: Draft - Pending Review
Branch: `codex/architecture-refactor`
Depends on: Accepted Task001. Task002 is independent of this ownership migration.

## 1. Problem And Goal

`BattleHUDUI` owns a collection of `PendingDraftUIItem`; each view holds its reward,
Draft token, and consumed flag. `DraftSystem.CountPendingReservedCapacityForUpgrade`
reads those views as gameplay data. Initial completion also retains a view reference.

Move held-reward identity and consumption into the Draft domain so presentation
can be rebuilt without altering reward ownership or reservation capacity.

## 2. Proposed Contract

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
`TowerPlacementController.cs`, and a new local Pending data model/collection.
Adapt Recorder Pending snapshots and investment correlation to the model in this
task so reports stay usable; broad Recorder decomposition remains Task006.

No inventory framework, persistence, rerolls, new reward types, probability
retuning, input redesign, or full placement-class decomposition. Remove obsolete
UI authority and duplicate collections after migration.

## 4. Documentation And Review Decisions

This changes an explicit existing ownership contract. After review, update
[HUD](../System/04_BattleHUDUISystem.md), [Draft](../System/08_DraftSystem.md),
[Placement](../System/09_TowerPlacementSystem.md), and the ownership summary in
[Overview](../System/00_ProjectOverview.md) before implementation. Check the
[Upgrade](../System/13_TowerUpgradeSystem.md) consumption wording as well.

Review the model identity shape, initial view preparation/commit sequence, and
UI teardown versus Stage-release policy. Proposed default: preserve current
interactive-readiness gating while separating accepted state from the view.

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
acceptance. No implementation or evidence exists yet.
