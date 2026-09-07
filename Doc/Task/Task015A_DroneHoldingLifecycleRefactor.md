# Task015A - Drone Holding Lifecycle Refactor

Status: Completed

Accepted: 2026-09-02

Depends on: Completed Task006 Drone Burst Elemental Opportunity Refactor;
completed Task007B Primary-only Elemental Opportunity Boundary Refactor

Unblocks: Task015 Stage6 Wave Calibration continuation

## 1. Goal

Make Battery end the only ordinary gameplay completion boundary for an
initialized Drone. Temporary loss of a valid target must move the Drone into a
targetless Holding state instead of destroying it, without changing accepted
Drone damage values, Burst topology, Elemental opportunity ownership, Attack
Range, capacity, Battery duration, or Final Dive result order.

This is a small global Drone runtime correction discovered during Task015. It
is not Stage6 balance tuning and must complete before Stage6 values are judged.

## 2. Source Contracts And Supersession

The durable behavior owner is `Doc/System/11_TowerRuntimeCombatSystem.md`.
`Doc/System/10_TowerFrameworkSystem.md` provides the framework summary, while
`Doc/System/12_ProjectileSystem.md` continues to own already released
Drone-fired Projectiles.

This Task supersedes only the historical rule that an initialized ordinary
Drone ends when target loss yields no immediate in-range replacement. It
preserves Task006 Burst identity and retarget invariants and Task007B's
primary-only Elemental opportunity boundary. Historical Task004, Task006,
Task007, and Task013 documents and records remain unchanged evidence.

## 3. Problem Statement

The current runtime validates an active Drone target against the source Tower's
current Attack Range. When the target resolves or leaves range, it attempts one
normal in-range retarget. If no replacement exists, the Drone immediately uses
the same aerial-despawn presentation used by Battery completion.

In multi-Tower combat this creates anti-cooperation: another Tower can resolve
the locked target a few seconds after Drone release, causing most of the
Drone's authored Battery and the source Tower's committed Attack Cycle to
produce no further work. The shared despawn presentation also cannot identify
whether the cause was target loss, Battery end, or technical cleanup.

## 4. Accepted Drone Holding Contract

### 4.1 Release And Launch

- Tower scheduling, capacity, Windup, release confirmation, Attack Cycle, and
  initial valid in-range target requirements remain unchanged.
- Launching does not consume Battery.
- While a target remains valid and in range, the Drone continuously retains its
  last valid locked-target Hit Reference position.
- If launch movement completes without a valid target or replacement, the Drone
  enters Holding and begins the active Battery phase. It does not despawn.

### 4.2 Target Loss And Holding

- A target that resolves, otherwise becomes invalid, or leaves the source
  Tower's current refreshed Attack Range triggers normal in-range reselection.
- A valid replacement uses the existing target-selection category and ordinary
  orbit-entry movement.
- With no replacement, the Drone enters Holding around the frozen last valid
  locked-target position. It does not follow the old target, query candidates
  around its own position, or extend the Tower's range.
- Holding keeps the Drone in its active capacity slot, continues Battery
  consumption, continuously checks the source Tower's current range for valid
  candidates, and releases no Projectiles.
- A later valid candidate leaves Holding through the existing target-selection
  category, becomes the current target, and receives normal orbit-entry
  movement before firing resumes.

### 4.3 Burst And Elemental Invariants

- Target loss, Holding entry, and Holding exit preserve Burst identity, phase,
  remaining shots, remaining timer, and consumed opener state.
- Burst timing is frozen while Holding or travelling to a new orbit path.
- A shot slot is consumed only by successful Projectile creation.
- Retargeting never reloads a Burst, bypasses Inter-Burst Cooldown, transfers a
  missed opener, or invents a new Elemental opportunity.
- Only the primary Drone's successfully released opening Projectile of a
  genuinely new Burst may carry ordinary Elemental authorization. Additional
  Drones, later shots, Blast Rounds results, and Final Dive results remain
  unauthorized as already accepted by Task007B.
- Already released Direction Projectiles retain their frozen direction,
  identity, lifetime, hit behavior, and ownership independently of later Drone
  target or state changes.

### 4.4 Battery End And Cleanup

- Battery is static entity authoring and is never reset by Holding, retargeting,
  Tower Level, or Upgrade application.
- Battery advances during Orbiting and Holding but not Launching.
- At Battery end, a valid current target may enter the existing Final Dive
  branch when that package is enabled.
- Battery end without a valid target completes through aerial retirement with
  no direct Hit, Position Impact, package Effect, or Elemental opportunity. A
  stale Holding center never authorizes Final Dive gameplay.
- Final Dive remains one-way and retains its accepted target, last-valid-
  position, local direct-target query, explosion, and result-order Contract.
- Owning-Tower cleanup, combat-session cleanup, invalid initialization, and
  external destruction remain technical cleanup exceptions to Battery-only
  ordinary completion.

## 5. Ownership And Implementation Scope

### 5.1 Drone Runtime

The Drone runtime owns:

- an explicit Holding state;
- continuous last-valid locked-position capture;
- target-loss classification and in-range reselection;
- Holding orbit, Battery advancement, and reacquisition;
- Burst preservation across Holding;
- Battery-end versus technical-cleanup completion reasons.

The Tower runtime continues to own scheduling, active capacity, release, live
Attack Range and Burst Cooldown refresh, active-Drone registration, and cleanup.

### 5.2 Recorder Observability

Extend the current Recorder schema from `23` to `24` with per-Drone lifecycle
observations sufficient to distinguish:

- initialization and launch completion;
- target loss because the target became invalid versus left range;
- successful immediate retarget;
- Holding entry and exit;
- Battery depletion;
- Final Dive entry and completion;
- ordinary aerial retirement;
- technical cleanup.

Record per-Drone active time, Holding time, Holding-entry count, reacquisition
count, immediate-retarget count, Battery-end branch, and completion reason.
`activeTime` measures Initialization through Completion and includes Launching,
Orbiting, Holding, and Final Dive. `holdingTime` is the sum of all completed
Holding intervals. `reacquisitionCount` includes immediate retargets and Holding
exits, while `immediateRetargetCount` records the former subset. Add one
integrity result that reconciles lifecycle transitions and proves every
initialized Drone completes exactly once. Recorder observations remain
read-only and never decide gameplay.

### 5.3 Unity Authoring Checklist

- Reuse current primary and additional Drone prefabs, Battery duration, flight
  speed, orbit radius, Burst values, Fire Anchor, and presentation assets.
- Add no new balance knob, Upgrade identity, migration alias, or compatibility
  path.
- Holding should reuse normal orbit presentation around its frozen center;
  presentation must not create targeting, damage, or lifecycle authority.
- Any Inspector serialization caused by the implementation must preserve
  existing prefab references and GUIDs.

## 6. Out Of Scope

- Changing Tower or Drone Attack Range values or allowing unlimited pursuit of
  a target outside the source Tower range;
- changing Battery duration, Tower Attack Cycle, Drone capacity, flight speed,
  orbit radius, Burst Count, Burst Interval, or Burst Cooldown;
- changing target-selection categories;
- changing direct damage, DamageScale, Blast Rounds, Final Dive, or Elemental
  stack/reaction values;
- changing Projectile flight, collision, fallback, or lifetime behavior;
- Stage6 Progress, Wave, Player Health, Monster Profile, or placement tuning;
- rewriting historical Task acceptance records.

## 7. Implementation Phases

### Phase A - Lifecycle State And Completion Ownership

Add Holding and one explicit normal/technical completion classification. Keep
Battery and target authority in the Drone runtime and preserve Tower-owned
registration and cleanup.

### Phase B - Target Loss, Holding, And Reacquisition

Capture the last valid locked position, route invalid/out-of-range target loss
through immediate reselection, enter Holding when no candidate exists, and
resume through normal selection and orbit entry when a candidate appears.

### Phase C - Burst, Upgrade, And Final Dive Preservation

Preserve Burst state and successful-release shot consumption, verify live Range
and Burst Cooldown refresh, and keep the existing Battery-end Final Dive branch
and result order.

### Phase D - Recorder Schema 24

Add lifecycle observations, report fields, reconciliation integrity, and
focused Run Names without turning Recorder data into gameplay authority.

### Phase E - Static And Play Mode Acceptance

Pass compilation and scoped diff gates, then execute the focused lifecycle and
calibration-impact regression matrix below.

## 8. Original Focused Lifecycle Matrix

The implementation plan proposed the following deterministic one-scenario
fixtures. During runtime acceptance, the user approved a smaller evidence route:
the live Stage6 Reference exercised repeated Holding entry, exit, reacquisition,
Battery end, and terminal cleanup, and the retained Task004, Task007B, and
Task013 fixtures audited the affected numerical and topology contracts. The
individual L1-L11 Run Names below were therefore not executed and are not
claimed as passed. They remain a focused diagnostic catalog if a later defect
requires one of these edge transactions to be isolated.

| ID | Scenario | Required result |
|---|---|---|
| L1 | Initial target resolves during Launching; no replacement | Launch completes into Holding; no early completion; Battery begins |
| L2 | Target resolves Between Shots; no replacement, then a later target enters | Holding preserves Burst identity, remaining shots, timer, and opener state; reacquisition resumes the same Burst |
| L3 | Live target exits source Tower Attack Range | Drone freezes the last valid in-range position and Holds; it neither pursues out of range nor completes |
| L4 | Expanded Patrol is applied while Holding and exposes a valid target | Live Range refresh permits normal reacquisition without Battery or Burst reset |
| L5 | Battery ends while Holding without Final Dive target | One ordinary aerial retirement; zero direct Hit, Position Impact, package Effect, or Elemental opportunity |
| L6 | Battery ends with Final Dive and a valid target | Existing one-way Final Dive direct/explosion order remains unchanged |
| L7 | Final Dive target invalidates after dive entry | Drone reaches the retained last valid position and completes the accepted Position Impact contract |
| L8 | Primary plus additional Drone lose targets independently | Each owns Holding, Battery, Burst, reacquisition, and exactly one completion |
| L9 | Tower or combat cleanup occurs during Holding | Technical cleanup completes once, unregisters capacity, and produces no ordinary Battery-end gameplay |
| L10 | Battery reaches zero on the same frame the engaged target leaves Attack Range | Battery end wins over reacquisition, but the out-of-range target cannot authorize Final Dive; one aerial retirement and zero impact results |
| L11 | A Drone Projectile fails runtime initialization | No shot is consumed, opener authorization is not transferred, one technical cleanup occurs, and no per-frame retry is attempted |

Suggested focused Run Names:

```text
Task015A_PhaseA_LaunchTargetResolved_HoldingReacquire_Schema24_01
Task015A_PhaseA_BetweenShotsTargetResolved_BurstPreserved_Schema24_01
Task015A_PhaseA_TargetOutOfRange_HoldingReacquire_Schema24_01
Task015A_PhaseA_ExpandedPatrol_HoldingRangeRefresh_Schema24_01
Task015A_PhaseA_HoldingBatteryEnd_NoImpact_Schema24_01
Task015A_PhaseA_FinalDiveBatteryEnd_ValidThenInvalidTarget_Schema24_01
Task015A_PhaseA_DoubleDrones_IndependentHolding_Schema24_01
Task015A_PhaseA_HoldingTechnicalCleanup_Schema24_01
Task015A_PhaseA_BatteryEnd_TargetCrossesRange_NoFinalDive_Schema24_01
Task015A_PhaseA_ProjectileInitializationFailure_TechnicalCleanup_Schema24_01
```

## 9. Calibration-Impact Regression Matrix

Previous numerical Tasks remain accepted by default. These tests audit whether
the new branch changes their actual target-rich measurement conditions; they do
not automatically reopen those Tasks.

| Owner | Regression | Retention gate |
|---|---|---|
| Task004 | L2 naked Drone target-rich control | A complete base-Battery Drone retains the expected eight-Burst cadence and accepted direct-damage signature; Holding count is zero |
| Task004 | Optimized Burst Module + Blast Rounds | A complete Battery retains the expected ten-Burst cadence, stable direct/Blast signatures, and accepted package ordering; Holding count is zero |
| Task004 | Double Drones + Final Dive | Primary/additional lifecycle independence and one Battery-end branch per active Drone remain valid |
| Task007B | L3 Charged Drones primary-only control | Eligible opener/direct-hit accounting remains one primary opener per genuine Burst; no eligibility is created by Holding exit |
| Task013 | Accepted Stage4 Reference repeat | Recorder integrity passes and the accepted Victory with `3-4` leaks remains inside its frozen margin |

Suggested regression Run Names:

```text
Task015A_PhaseB_Task004_DroneL2_NoUpgrade_TargetRichRegression_Schema24_01
Task015A_PhaseB_Task004_DroneL2_OptimizedBurst_BlastRounds_TargetRichRegression_Schema24_01
Task015A_PhaseB_Task004_DroneL2_DoubleDrones_FinalDive_TargetRichRegression_Schema24_01
Task015A_PhaseB_Task007B_DroneL3_ChargedDrones_PrimaryOnlyRegression_Schema24_01
Task015A_PhaseB_Task013_Stage4V9_ReferenceRegression_Schema24_01
```

Task015 resumes only after these gates pass. Its first resumed run must reuse
the frozen `_02` deployment order and placement under the accepted Holding
Contract; Task015 continues to own the Stage6 Run Name and all subsequent
Progress, Wave, Player Health, Monster, and Build-boundary decisions.

### 9.1 Accepted Regression Evidence

All authoritative acceptance Records use Recorder schema `24` and pass every
reported integrity result.

| Evidence | Accepted result |
|---|---|
| Stage6 live Holding observation | The V3 Reference Record observed `7` Drones, `42` Holding entries, `41` Holding exits, `77` reacquisitions, `6` Battery depletions, and `7` exactly-once completions. The seventh completion was terminal technical cleanup after the Stage ended in defeat. The stale `Schema23` filename contains a schema-24 report and is lifecycle evidence only, not accepted Stage6 balance evidence. |
| Task004 L1 naked control | The corrected `_02` Record reproduced Wave-2 Effective Damage `2090`, matching the historical control. |
| Task004 L2 naked control | Wave-2 Effective Damage `4510` remained within `1.5%` of the historical `4578` control. |
| Task004 Basic/Behaviour packages | Optimized Burst repeated at `2520/2560`; Blast Rounds at `7440/7484`; Optimized Burst plus Blast Rounds at `9036/9058`. The target-rich fixtures reached their expected Burst cadence and did not expose an ordinary early-completion path. The retained Blast uplift belongs to the accepted downstream retarget/target-distribution behavior rather than the new Holding branch. |
| Task004 lifecycle packages | Double Drones plus Final Dive produced Wave-2 Effective Damage `11559` versus historical `11432`, with `12` completed Final Dives, one Battery aerial retirement, one terminal technical cleanup, and one observed Holding exit. The three-Upgrade Core repeated at `13926/14045` versus historical `12771`; no numerical Task was reopened. |
| Task007B primary-only topology | The Charged Drones control recorded `48` primary opener candidates, eligible results, and dispatched requests. Additional openers and all later shots retained zero eligibility, Buff attempts reconciled exactly, and L3 primary/additional direct damage remained within `1.33%` of the historical fixture. |
| Task013 Stage4 Reference | The frozen Reference completed with Victory, `3` leaks, Final Health `3`, Effective Damage `14993`, and every integrity result true. The historical Reference had `4` leaks and Effective Damage `14829`, so the accepted `3-4` leak margin remained intact. |

The first L1 naked `_01` Record contained fixture drift and is superseded by the
corrected `_02` Record. Supplemental Blast-only and three-Upgrade Records are
retained as supporting evidence without changing the frozen Task004 contract.

## 10. Reopening Policy

Do not change a completed Task status merely because Task015A changes a branch
that its accepted fixture did not exercise.

Reopen only the smallest affected acceptance slice if current evidence shows:

- a target-rich Task004 fixture enters Holding or changes full-Battery Burst,
  direct-damage, Behaviour-damage, or accepted Upgrade-order results;
- Task007B opener eligibility, primary/additional identity, or application
  accounting changes;
- the Task013 Reference leaves its accepted `3-4` leak Victory margin for a
  Drone-lifecycle reason; or
- any retained integrity invariant fails.

If all retention gates pass, append Task015A regression evidence while leaving
Task004, Task007B, and Task013 completed and their historical records intact.

## 11. Acceptance

- Target loss with no replacement always enters Holding rather than ordinary
  completion.
- Holding orbits the frozen last valid locked position, consumes Battery,
  releases no Projectile, remains in capacity, and can reacquire in range.
- No target-loss path resets Battery or Burst state, consumes a nonexistent
  shot, bypasses cooldown, or grants Elemental eligibility.
- Battery end and technical cleanup are distinct, exactly-once completion
  boundaries.
- Final Dive and already released Projectiles preserve their accepted behavior.
- Schema-24 lifecycle and Burst diagnostics reconcile per Drone and all existing
  combat, damage, Buff, placement, movement, and Recorder integrity remains
  true.
- The accepted Stage6 live observation and regression matrix cover ordinary
  Holding, reacquisition, Battery retirement, Final Dive, primary/additional
  identity, Elemental eligibility, and Stage-margin retention. The unexecuted
  deterministic L1-L11 edge fixtures remain explicit waivers rather than passed
  evidence.
- Calibration-impact retention gates pass, or the smallest affected completed
  Task slice is explicitly reopened before Task015 resumes.

## 12. Validation

Static gates:

```bash
dotnet build Assembly-CSharp.csproj --no-restore -m:1 -nr:false -p:LangVersion=8.0
dotnet build Assembly-CSharp-Editor.csproj --no-restore -m:1 -nr:false -p:LangVersion=8.0
git diff --check -- Assets/Scripts/TowerRuntimeCombat Assets/Scripts/TowerDeployment Doc/System/10_TowerFrameworkSystem.md Doc/System/11_TowerRuntimeCombatSystem.md Doc/Task/README.md Doc/Task/Task015A_DroneHoldingLifecycleRefactor.md Doc/Task/Task015_Stage6WaveCalibration.md
```

Unity Play Mode and Recorder evidence are mandatory for runtime acceptance.
Static compilation alone cannot prove Holding position, elapsed Battery,
reacquisition, Burst continuation, exactly-once completion, or unchanged Stage
margin.

## 13. Completion And Evidence Record

The Contract, runtime Holding lifecycle, exactly-once completion funnel,
three-result Projectile release boundary, and schema-24 lifecycle Recorder are
implemented. The accepted live Stage6 observation proves repeated Holding exit
and reacquisition under multi-Tower combat. The Task004, Task007B, and Task013
regressions preserve the reviewed damage, Burst, Final Dive, primary-only
Elemental, and Stage-margin boundaries.

No upstream numerical Task is reopened. Task015A changes no authored Drone
damage, Battery, Range, cadence, Upgrade, Buff, Elemental, Monster, Wave, or
Stage value. Task015 resumes from its frozen `_02` deployment order and
placement. Static build and diff evidence is recorded at the completion commit;
Unity Play Mode evidence remains the schema-24 Records listed above.
