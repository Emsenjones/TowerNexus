# Task002 - Stage Free Re-roll System

Iteration: Re-roll System
Status: In Progress; approved optimization implemented and static/managed validation passed; schema-27 native integration/export acceptance pending.
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
| Re-roll controls | Existing ButtonPressFeedback on both controls; exhausted control has feedback only |
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
- Precommit failure preserves the old set and balance while the session remains live.
  Lifecycle cancellation discards preparation and cannot reopen the outgoing
  window, spend its budget, or restore expired authority.
- Old-set input is rejected even when the same reward appears in the new set.
  Re-roll keeps the original Draft ordinal/token and pause, does not modify held
  rewards/reservations, and never completes Initial Draft or starts Waves.
- The last successful Re-roll leaves new cards selectable at zero balance.
- Pointer down provides Press feedback; valid release/click requests the action.
  Release outside cancels it. Exit, cancellation, disable, and closure restore
  label position without accumulating offsets.
- Positive balance shows `Button_Reroll_Active` only; zero shows
  `Button_Reroll_Inactive` only. Update `Text_RerollCountRemain` inside Active.
  Remove the separate fixed remaining label/count contract.
- Both buttons reuse `ButtonPressFeedback`. Inactive stays interactable but
  has no gameplay listener and shows no Toast. Fixed sequence mode disables
  the available button's action; protected transactions reject gameplay input.
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

- [x] Stage1-Stage6 budgets match the approved starting values.
- [x] DraftUI exposes both Button references and the Active child TMP count.
- [x] Existing ButtonPressFeedback references/normal-pressed images are preserved.
- [x] No separate fixed remaining-count label is needed.
- [x] DraftUI owns the Inspector-configurable No Other Choices Message;
  ToastUI receives that string at play time. Preview Message is only a preview.
- [x] DraftUI has Task001 Toast prefab and explicit local container references.
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

## 9. Historical Schema-26 Implementation And Evidence

The user authorized Task002 after reporting correct DamageNumber visuals and
calibrating the Toast prefab; integrated Toast acceptance is explicitly deferred
to this feature's Unity test. This does not declare Task001 fully accepted.

Implementation decisions:

- StageDefinition passes the budget through Stage composition and battle binding.
  Stage1-6 use `1/1/2/3/4/5`; Default explicitly uses zero.
- DraftSystem uses session token plus integer set revision. Initial is revision 1;
  replacement advances only on commit. Card callbacks capture their revision.
- DraftUI prepares inactive views under a temporary container, retaining old
  views until ownership commits. Closure/disable invalidates preparation.
  Protected model commit/observation precedes exposing new input.
- Sampling retains System.Random and the existing category/weight/backfill/shuffle
  path. Failed generated preparations advance the stream without spending budget;
  rejected requests and No Other Candidates do not sample.
- Existing Prefab_GameRuntime button layouts and ButtonPressFeedback data are
  retained. Only DraftUI references/message are added. Window_DraftUI's root is
  the explicit Toast container, so Toast ordering is above the window content.
  UiPrefab_ToastItem animation parameters remain user-authored and unchanged.
- Recorder schema 26 keeps `attempts` as opportunities. Top-level generation
  fields describe the original set; `choiceSets` contains every exposure with
  revision and budget transition. `selectedSetRevision` names the final set.
  Separate request outcomes, original/Re-roll exposure totals, Initial/Level-Up
  Re-roll totals, and terminal balance support reconciliation. Generation contract
  is `CategorySlotV1RerollV1`; random algorithm remains `SystemRandomV1`.
- Existing selected reward/consumption/investment consumers still use one attempt
  per opportunity. The generation integrity consumer checks all sets and balance.
  Archived Task003 Pending assertions and baseline sampling oracle are retained;
  only its fixture callback signature/new phase were updated.

Validation on 2026-09-10:

| Check | Evidence | Boundary |
|---|---|---|
| `Tests/Reroll/Task002/run.py` | 489 Editor / 470 player assertions passed | Complete DraftSystem with native HUD doubles; production Recorder capture, summary and integrity methods in Editor |
| `Tests/Reroll/Task002/ui.py` | 18 assertions passed | Complete DraftUI with native/component boundaries doubled |
| `Tests/Reroll/Task002/assets.py` | Passed | Seven budgets and actual prefab references/feedback configuration |
| `Tests/Reroll/Task002/build.py` | Passed; Editor build 0 errors/warnings, full player assembly 126 sources | Installed Unity/TMP/DOTween references; no native Unity run |
| `Tests/Task003/run.py` | 14 Pending contracts and 300 fixed-seed baseline traces passed | Original no-Re-roll sampling oracle retained |
| `Tests/Task004/run.py` | 23 submission contracts passed | Deployment/level/upgrade ownership and consumption regression |
| `Tests/Reroll/Task001/run.py`, `assets.py` | 92 assertions and asset checks passed | Animation regression; preserves reviewed user layout and Toast parameters |
| Scoped `git diff --check` | Passed for scripts, Stage configs, documents and tests | Unrelated user/plugin and Unity-authored whitespace is outside this check |

No Unity/EventSystem/Canvas acceptance, device run or fresh schema-26 battle
export is inferred from these checks.

Unity handoff: inspect the serialized references on Window_DraftUI, then test
last-count switching, exhausted press feedback, repeated No Other Candidates
Toast while paused, selection after refresh, retry, and Stage transition. Export
schema-27 runs with zero/one/multiple Re-rolls and verify generation integrity and
terminal budget/Pending reconciliation. Native EventSystem press/release,
Canvas layout, Unity serialization, and fresh battle exports remain user-owned.
Do not mark Task002 Completed or begin Task003 calibration based solely on managed
or compilation evidence.

## 10. Approved Optimization Revision (2026-09-10)

The user authorized implementation after review in “评审 Task002 实现优化点”.
This revision uses an independent prepared choice set and one eligibility snapshot,
HUD-owned single-use view handles, and schema 27 with choiceSets as the sole
candidate history. Existing probabilities, seeded sampling, free budgets, dual
buttons, fixed prefab text, and failed-preparation RNG advancement remain unchanged.

Three mandatory clarifications:

1. Committed presentation failure is reported by DraftSystem with exact Battle/session
   authority to BattleRuntimeCoordinator. Initial and Level-Up uniformly terminate
   the current battle as a technical failure. Expired operations only clean up;
   they must never terminate a replacement battle.
2. Model commitment and presentation outcome are independent. Precommit failure or
   cancellation creates no replacement and spends nothing. Postcommit cancellation
   or presentation failure retains exactly one replacement and one spend; successful
   Re-roll totals count commitments, not completed animations/presentation.
3. A gameplay operation guard covers preparation, commit, presentation, final request
   notification and cleanup. Observation scopes do not supply that guard. Only the
   still-current Battle/session may regain input after completion. Candidate-set and
   final-request notification reentry must both reject selection, Re-roll and Pending
   mutation. Rejected nested calls must not recursively notify without bound.

Each observed request gets an independent ID and start registration under the captured
Battle observation identity, and exactly one final result from exception-safe cleanup.
The outer observation scope drains those results before freezing the old report,
while gameplay termination and terminal Pending/budget capture remain immediate.
Committed set/request joins and Natural identity/weight membership are verified.
Start registration and final results are separate sources for missing-result checks.

Prior validation in Section 9 describes the pre-optimization implementation only.

## 11. Optimization Implementation And Validation

- DraftSystem now passes an explicit DraftChoiceSet into generation helpers; current
  committed results and observation data are never used as preparation scratch.
  Eligibility is gathered once and sampled with the original algorithms/RNG order.
- A HUD-owned DraftViewPreparation handle separates final validation, callback-free
  model/view ownership transfer, and lifecycle-sensitive presentation. It is single
  use and disposal is idempotent. DraftSystem owns the orchestration.
- BattleRuntimeCoordinator binds explicitly to DraftSystem and validates the source
  plus live Draft token for committed presentation failures. Expired-token failure
  reports cannot affect a replacement session. Both Draft sources use this route.
- The gameplay guard survives both observation notifications and cleanup. Calls
  arriving while that guard is held reject at ingress without recursively emitting
  more request notifications. Those rejected recursive calls are outside the observed
  request boundary; all admitted observed requests have independent starts/results.
- The production CombatDiagnosticScope encloses the whole operation. Starts and
  immutable final outcomes use captured subscribers and Battle identity. Terminal
  budget/Pending snapshots stay immediate; old results drain before report freeze.
- Schema 27 adds start count/IDs and final request commitment, sampling-started,
  committed revision, budget transition, and presentation outcome. Replacement sets
  reference request IDs. Request identity is diagnostic only and does not gate gameplay.
- Attempt JSON owns opportunity/selection/consumption; choiceSets alone owns generation
  history. Start/result completeness, unique joins, committed spend, Natural membership
  and weight, and existing generation/selection summaries reconcile together.
- Unity layouts, sprites, button wiring, Stage counts and calibrated Toast parameters
  were preserved during this revision. No new general transaction framework was added.

| Check | Result | Evidence boundary |
|---|---|---|
| `Tests/Reroll/Task002/run.py` | 535 Editor / 484 player assertions passed | Complete DraftSystem; production Recorder methods/DTOs, actual observation scope, extracted production Coordinator failure route; remaining combat/HUD boundaries doubled |
| `Tests/Reroll/Task002/ui.py` | 19 assertions passed | Complete DraftUI with card/Toast/native boundaries doubled |
| `Tests/Reroll/Task002/real_cards.py` | 22 assertions passed | Complete DraftUI + TowerContentUIItem; effective activation/graphics are managed doubles, not native Unity |
| `Tests/Reroll/Task002/assets.py` | Passed | Seven Stage budgets and unchanged prefab wiring/feedback |
| `Tests/Reroll/Task002/build.py` | Passed | Editor 0 warnings/errors; complete 126-source runtime assembly also compiles without UNITY_EDITOR |
| `Tests/Task003/run.py` | 14 contracts / 300 original sampling comparisons passed | Original baseline unchanged; fixture adapters account for explicit prepared-set parameter |
| `Tests/Task004/run.py` | 23 contracts passed | Existing ownership and investment/consumption regressions |
| Task001 `run.py` / `assets.py` | 92 assertions / asset checks passed | Animation and serialized-asset regression |
| Scoped `git diff --check` | Passed | Scripts, Stage configs, docs and tests |

Fault injection covers missing/duplicate results, false request/set joins, out-of-pool
identities, wrong weights, precommit cancellation, both Draft-source presentation
failures, stale failure reports, both notification reentry sites, terminal draining,
and old/new Battle observation separation.

Unity acceptance still required: actual pointer press/release, paused Toast replay and
input pass-through, final-count selection, retry/Stage transitions, and fresh schema-27
zero/one/multiple Re-roll exports. No native/device or fresh battle-export acceptance
is claimed by these managed checks. Task003 remains pending feature acceptance.
