# Task002 - Stage Free Re-roll System

Iteration: Re-roll System
Status: Planned; implementation and native acceptance have not started.
Dependencies: [Task001 - Reusable UI Animation And Toast](Task001_ReusableUIAnimationAndToast.md).
Next: [Task003 - Re-roll Accessibility Calibration](Task003_RerollAccessibilityCalibration.md).

## 1. Goal And Sources

Deliver a complete Stage-local free Re-roll feature: configuration, Draft-owned
balance and replacement, usable controls and Toast feedback, and trustworthy
Recorder evidence. One Draft still produces at most one selected reward.

Sources:

- [Stage System](../System/02_StageSystem.md), StageDefinition and composition.
- [Draft System](../System/08_DraftSystem.md), Sections 3.4 and 8-10.
- [Battle HUD UI System](../System/04_BattleHUDUISystem.md), Sections 4.1 and 6.3.
- [Stage Blueprint](../Balance/00_StageDesignBlueprint.md), Section 2.1.
- [Game Flow System](../System/01_GameFlowSystem.md), retry and Stage release.

## 2. Scope And Ownership

| Owner | Responsibility |
|---|---|
| StageDefinition / Stage composition | Non-negative initial budget and distribution |
| DraftSystem | Battle-local remaining count, eligibility, sampling, current choice-set identity, replacement commit, pause, and observations |
| DraftUI / BattleHUDUI | Referenced controls, current-set input, prepared presentation, balance display, and local Toast instance/container |
| Re-roll button | Active/Press/Inactive images, valid click, label offset and restoration |
| Recorder | Per-set exposure and final reward evidence, without controlling gameplay |

No new Draft reward, progression event, budget carryover, paid Re-roll, pity,
cross-set exclusion, guaranteed new card, per-card lock, or global Toast routing.
Do not change category probabilities, content pools, combat, or Stage pressure.

## 3. Configuration And Gameplay Contract

Author Stage1-Stage6 initial counts as `1 / 1 / 2 / 3 / 4 / 5`, matching the
Blueprint's initial test baseline. Zero is valid; reject negative authoring.
Explicitly review the Default/debug StageDefinition and any other authored
Stage assets during migration; they must also have a valid non-negative value.

Each fresh battle, including Retry, starts at the configured count. Initial and
Level-Up Drafts share the budget and allow repeated use in one window. UI
reopening does not refill it; spending does not mutate StageDefinition.

Initial Re-roll remains Tower-only. Natural mode with a fixed seed permits
Re-roll; Fixed Draft sequence mode does not. Use existing probability, weights,
distinct identities, backfill, and shuffle. Cross-set repetition, including the
entire same set, is valid and costs one successful Re-roll.

Check distinct identities actually eligible for the current source and Pending
reservations, not raw Stage lists or multiplicity. If every eligible identity is
already displayed, return No Other Candidates, keep choices/count, and show
`No other draft choices available.` The otherwise actionable button stays Active.

## 4. Replacement And Input Contract

- Accept only the current live battle, Draft session, and choice-set identity in
  Awaiting Selection with owned pause, open presentation, and positive balance.
- Claim refresh before preparing replacement. Reject concurrent selection,
  duplicate/stale refresh, and nested commits.
- Prepare all new choices and usable views without destroying old choices or
  exposing new input. Revalidate authority before atomically replacing the set,
  advancing its identity, and decrementing the count exactly once.
- Failure preserves the old set and balance while the session remains live.
  Lifecycle cancellation discards preparation and cannot reopen the outgoing
  window, spend its budget, or restore expired authority.
- Old-set input is rejected even when the same reward appears in the new set.
  Re-roll keeps the original Draft ordinal/token and pause, does not modify held
  rewards/reservations, and never completes Initial Draft or starts Waves.
- The last successful Re-roll leaves new cards selectable at zero balance.
- Pointer down provides Press feedback; valid release/click requests the action.
  Release outside cancels it. Exit, cancellation, disable, and closure restore
  label position without accumulating offsets.
- Zero balance or unavailable interaction uses Inactive and blocks input;
  refreshing/selection commit temporarily blocks input as well.
- At most one Draft-local Toast exists. Repeated No Other Candidates restarts
  it; Draft closure or battle termination cancels and removes it. It uses
  Task001 playback and never intercepts input.

## 5. Recorder Contract

Preserve initial and every committed replacement set under one Draft attempt.
Record set identity/ordinal, source and mode, seed/generation contract, eligible
identities/weights, requested and realized categories, displayed order, and
budget before/after. Final selection names its exact set, followed by normal
held-item, consumption, and investment evidence.

Distinguish rejected/failed requests from successful replacements. Superseded
sets are exposure history, not lost rewards or extra opportunities. Preserve
terminal budget/history before cleanup. Separate Initial/Level-Up and original/
Re-roll exposures in summaries. Update schema/version and integrity checks
together; inspect all consumers instead of assuming one opened event per reward.
Recorder remains observational and compatible with diagnostics-disabled builds.

## 6. Starting Points And Unity Handoff

Inspect `StageDefinition.cs`, `BattleRuntimeCoordinator.cs`, `DraftSystem.cs`,
`DraftUI.cs`, `BattleHUDUI.cs`, `TowerContentUIItem.cs`, and the Draft observation,
accumulation, JSON, and integrity consumers under `Assets/Scripts/Diagnostics/`.
Inspect the actual Stage assets under `Assets/Configs/StageDefini/`.

Unity authoring checklist:

- [ ] Stage1-Stage6 budgets match the approved starting values.
- [ ] DraftUI exposes Re-roll button and numeric text references in Inspector.
- [ ] Button exposes its three Sprites, child-label target, and press offset.
- [ ] Fixed labels read `Re-roll` and `Free re-rolls remaining: `.
- [ ] DraftUI has Task001 Toast prefab and explicit local container references.
- [ ] Modal behavior and Toast ordering/input pass-through are correct.

The user owns final layout, Inspector wiring, import/reserialization, and native
Play Mode unless delegated. Provide the precise reference/authoring checklist.
Do not overwrite archived `Tests/TaskNNN` harnesses because Task numbers match.

## 7. Acceptance And Validation

- [ ] Retry/next Stage reload the correct budget; opening/refreshing does not.
- [ ] Initial and Level-Up share counts, keep source rules, and preserve one
  reward per opportunity and uninterrupted Draft pause.
- [ ] Cover zero/one/multiple remaining counts; one-to-three eligible identities;
  larger pools; Pending reservations; repeated identical generated sets; and
  Fixed sequence versus fixed-seed Natural mode.
- [ ] No Other Candidates produces one replayable Toast without consuming budget.
- [ ] Inject failed preparation, stale old-set input with matching reward,
  repeated requests, nested selection, and battle cancellation during refresh;
  verify no partial views, double spend, stale authority, or resumed simulation.
- [ ] Final selection, Pending consumption, and Initial Wave authorization work
  after zero, one, and multiple Re-rolls.
- [ ] Same controlled seed/state/request history reproduces results. No-Re-roll
  paths preserve existing sampling behavior; diagnostics on/off do not alter it.
- [ ] Fresh Recorder exports reconcile opportunities, sets, successful Re-rolls,
  remaining balance, selected rewards, and terminal Pending/investment evidence.
- [ ] Relevant compilation and managed lifecycle/sampling/Recorder checks pass;
  Unity button/Toast integration and paused interaction have separate evidence.

## 8. Review And Completion Evidence

Present the implementation plan before coding. Resolve current-view binding,
failed-generation random-state semantics, reentrancy, schema migration, affected
existing test assumptions, and asset authoring in that plan. Preserve existing
regression oracles instead of overwriting them to match the new schema.

Record implementation, static/managed checks, Unity authoring, native interaction,
and fresh report evidence separately. Any waiver requires explicit user approval.
Task003 starts once the feature and its observations are accepted; functional
completion does not claim difficulty calibration.

Current evidence: Design contract only; code, assets, and runtime tests pending.
