# Task004 - Elemental, Effect, And Buff Baseline

Status: Complete; Elemental baseline v0.4 accepted

Depends on: Completed Task003 Tower growth and non-Elemental Upgrade baseline

## 1. Goal

Calibrate the four ElementTypes, their Tower-family application opportunities, lifecycle Effects, shared stacking, Overload, and Protection on top of the accepted Task003 Tower baseline.

The result must support Stage5 single-core learning and Stage6 matching-Element cooperation without making either Elemental value or Overload depend on pixel-perfect Tower placement.

Task004 is the sole calibration owner for Elemental power targets. Task003 supplies accepted non-Elemental builds but does not test Elemental Cores, matching sources, or Overload.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/System/15_BuffSystem.md`
- `Doc/Task/Task003_TowerGrowthAndNonElementalUpgrades.md`

`01_TowerGrowthAndUpgradeIdentity.md` remains the qualitative source of truth for Core investment and Elemental cooperation. This Task owns concrete fixtures, builds, diagnostics, measured results, and accepted values.

## 3. Preconditions

- Task003 remains accepted and its Base, Basic, and Behaviour values are frozen during the first Task004 pass.
- Stage5 and Stage6 Elemental pool authoring may remain unresolved during isolated content tests.
- Final Stage acceptance cannot proceed until Task007 authors and validates those pools.
- A dedicated Task004 Elemental cooperation Map must satisfy Section 7 before matching-source results are accepted.
- The Elemental diagnostic output in Section 8 must exist before lifecycle counts are treated as acceptance evidence. Manual visual observation remains valid for initial Map calibration and presentation checks.

## 4. In Scope

- Tower-family Elemental UpgradeDefinitions
- Elemental application opportunities
- Burning, Cold, ElectricShock, and Windcut
- Active Duration, Maximum Stacks, Source Apply Cooldown, Periodic Tick Interval, and Overload Protection Duration
- StackApplied, Overload, EnteredProtection, and removal lifecycle output
- Single-source and dual-source stacking comparisons
- Cross-TowerFamily contribution to one shared Elemental Buff
- Pre-Elemental Reference Controls
- Single-Source Elemental Reference Cores
- Representative Upper-Stress Elemental Builds
- Matching-source pair, mismatched-source control, and non-overlap control
- A dedicated diagnostic Map and read-only Elemental recorder output

## 5. Out Of Scope

- Final Stage5 or Stage6 Wave pressure
- Final Elemental Draft pool decision or candidate-solvability rules
- Player Progress Requirements
- Non-Elemental Base, Basic, or Behaviour redesign
- Treating an all-compatible-Upgrades debug Tower as the normal player Reference Build
- Using a one-Tower full-Draft run as Stage clear or Elemental balance acceptance

An all-compatible-Upgrades Tower may receive one optional runtime-stability smoke test after the required runs. Its damage is not a Task004 calibration target because it is position-dependent, may be unavailable in the final Stage pool, and can saturate the fixture.

## 6. Named Test Builds

### 6.1 Terminology

| Term | Exact Construction | Purpose |
|---|---|---|
| Pre-Elemental Reference Control | One natural Level 2 Tower with exactly one accepted Basic and one accepted Behaviour Upgrade; no Elemental Upgrade | Actual pre-Elemental baseline before the Level 3 Draft |
| Single-Source Elemental Reference Core | The same build after its normal Level 3 transition plus exactly one Elemental Upgrade; no second Tower applies that ElementType | Normal Stage5-style Elemental value |
| Representative Upper-Stress Elemental Build | One accepted Task003 upper-stress build plus exactly one Elemental Upgrade | Realistic strong-Core ceiling and interaction check |
| Matching-source pair | Two Single-Source Elemental Reference Cores using the same ElementType and sharing targets in overlapping effective coverage | Shared stacks and Overload |
| Mismatched-source control | The same two Towers and placements using different ElementTypes | Prove matching Element identity matters |
| Non-overlap control | The same two matching Cores placed so the first Buff expires before the second source can affect that Monster | Prove shared-target opportunity matters |

The unqualified term `Elemental Core` means one Level 3 Tower with one Basic, one Behaviour, and one Elemental Upgrade. The non-Elemental comparison is always called a `Pre-Elemental Reference Control` and remains at its natural Level 2 state. Tower Level has no direct combat-stat or combat-logic bonus, so Task004 does not create an artificial Level 3 Tower without an Elemental Draft merely to match the Core's level number.

### 6.2 Frozen Pre-Elemental Reference Controls

| TowerFamily | Basic | Behaviour | Accepted Task003 HP120 L Effective Damage | Selection Reason |
|---|---|---|---:|---|
| Archer | Quick Draw | Scatter Arrow | `3830` | Cadence plus additional Attack Entities exercises Source Apply Cooldown |
| Cannon | Faster Reload | Twin Shells | `3570` | Clear projectile opportunities with comparable headroom |
| Magic | Arcane Recovery | Twin Orbs | `3816` | Cadence plus persistent additional attack member |
| Drone | Optimized Burst Module | Blast Rounds | `3704` | Burst and area opportunities exercise per-target applications |

These builds are fixed measurement controls, not a claim that they are the best or only valid Basic-plus-Behaviour pairing. Each current TowerFamily has three accepted Basic and three accepted Behaviour Upgrades, producing nine such pairings. The four selected Controls were already tested, avoid the HP120 ceiling, have comparable Effective Damage, and expose each family's application cadence clearly.

The Task003 HP120 results identify the builds only. Every Task004 comparison must remeasure its own non-Elemental Control on the Task004 fixture.

### 6.3 Frozen Representative Upper-Stress Builds

| TowerFamily | Accepted Task003 Upper-Stress Build |
|---|---|
| Archer | Quick Draw + Explosive Arrow + Scatter Arrow |
| Cannon | Faster Reload + Explosive Shell + Twin Shells |
| Magic | Faster Orbit + Arcane Field + Twin Orbs |
| Drone | Optimized Burst Module + Blast Rounds + Double Drones |

For each family, first measure this build without an Elemental Upgrade on the Task004 fixture, then add one selected Elemental Upgrade without changing any other condition. Select the Element after the single-source matrix identifies that family's highest-risk or most opportunity-dense candidate. Do not replace these builds with every compatible Upgrade.

## 7. Fixed Fixture And Diagnostic Map

### 7.1 Task004 Elemental Reference v0.1

| Fixture Field | First-Pass Value |
|---|---:|
| Monster Count | `40` |
| Monster Health | `240` |
| Monster Speed | `0.25` |
| Spawn Interval | `2.5s` |
| Route | Dedicated Task004 straight diagnostic route |
| Position Set | Fixed Position 1, Position 2, and Position 3 anchors |

Keep the readable `2.5s` Spawn Interval. If a ceiling still hides gain, raise Monster Health and give the revision a new fixture version; do not silently compress spawn presentation or mix fixture versions inside one comparison.

Every run label records Task, fixture version, Map, TowerFamily, exact Upgrades, ElementType, and occupied positions.

### 7.2 Buff-Distance Calibration Before Map Acceptance

Before freezing the dedicated Map, use existing Maps to observe how many Grid intervals a speed-`0.25` Monster travels after the last successful refresh before the Elemental Buff disappears naturally.

- Start the distance count at the last successful application or refresh, not the first application.
- Ensure no Tower or secondary Attack Entity can refresh that Buff during the measured interval.
- Record both elapsed time and approximate Grid distance.
- Repeat when visual ambiguity or route geometry makes the final refresh point unclear.
- This observation validates Map spacing; it does not itself revise Active Duration.

The frozen pre-calibration Buff-parameter v0.3 baseline values are:

| Parameter | Baseline Value | Meaning |
|---|---:|---|
| Active Duration | `5s` | Time since the latest successful application or refresh before natural expiry; at normal speed this is approximately `1.25` route Grids |
| Periodic Tick Interval | Burning `2s`; other first-version Elemental Buffs `0s` | Burning lifecycle damage cadence; it does not control stacking, and the other Buffs have no PeriodicTick binding |
| Maximum Stacks | `10` | Overload threshold |
| Source Apply Cooldown | `2s` | Minimum successful contribution interval for one source Tower against one Monster and BuffDefinition |
| Overload Protection Duration | `10s` | Same-definition application block for every source after Overload |

`PeriodicTickInterval` is lifecycle periodic timing and is not the stack interval. With v0.3 values, one continuously successful source has a theoretical earliest Overload approximately `18s` after its first application. Two synchronized sources have a theoretical earliest Overload of approximately `8s`; actual timing remains attack- and target-opportunity-dependent. Tower A must never block Tower B merely because Tower A applied first.

The `5s` Active Duration is the v0.3 handoff candidate. With Node Size `1` and normal Monster Speed `0.25`, one Grid takes approximately `4s`; the extra second prevents Cannon's `3.5s` cycle plus ordinary impact timing from causing accidental expiry during otherwise continuous exposure. This Grid-distance conversion is a normal-speed calibration guide, not a separate spatial Buff rule. Chilled still uses the same time contract even though its movement reduction changes the distance travelled during those five seconds.

Primary, additional, piercing, area, contact, bounce, and persistent application opportunities produced by one Tower all use that Tower's one source cooldown entry against a given Monster and BuffDefinition. For example, Scatter Arrow may produce three application attempts, but simultaneous hits on the same Monster do not contribute three stacks: the first successful attempt starts the Archer Tower's cooldown and the remaining same-source attempts are blocked until it expires.

Every successful application refreshes Active Duration. A Source Apply Cooldown-blocked or Protection-blocked attempt does not refresh Active Duration, add a stack, or execute StackApplied. Reaching Maximum Stacks is the only Overload condition; Task004 adds no Primed state or distinct-source gate.

Keep these values for the first diagnostic pass. Revise them only after the Recorder distinguishes insufficient opportunity from blocked applications and natural expiry.

An exploratory Inspector override does not revise this baseline by itself. A BuffDefinition-parameter result becomes calibration evidence only when its schema-v3-or-later JSON parameter snapshot identifies the exact runtime values. A lifecycle Effect parameter such as Chilled movement multiplier requires schema-v4-or-later output. Historical schema-v1 reports and filenames may still support functional diagnosis, but they are not self-describing evidence for accepting a parameter revision.

### 7.2.1 Accepted Elemental Baseline v0.4

The completed first-pass matrix accepts a narrow v0.4 revision rather than a global Elemental rewrite:

| Parameter | v0.3 Baseline | Accepted v0.4 | State |
|---|---:|---:|---|
| Shared Active Duration | `5s` | `5s` | Frozen |
| Shared Maximum Stacks | `10` | `10` | Frozen |
| Shared Overload Protection Duration | `10s` | `10s` | Frozen |
| Burning Periodic Tick Interval | `2s` | `2s` | Restored after an unaccepted `1s` Inspector exploration |
| Burning Source Apply Cooldown | `2s` | `2s` | Frozen |
| Chilled movement multiplier | `0.5x` | `0.7x` | Accepted by schema-v7 single-source and matching-source reruns |
| Chilled Source Apply Cooldown | `2s` | `1.5s` | Accepted by schema-v7 single-source and matching-source reruns |
| Electrified Source Apply Cooldown | `2s` | `2s` | Frozen |
| Windcut Source Apply Cooldown | `2s` | `2s` | Frozen |
| WindVortex movement speed | `0.3` | `0.1` | Accepted; HP960 lifecycle diagnostic passed |

The accepted Cold revision changes only movement multiplier and Source Apply Cooldown. Schema-v7 Archer, Cannon, and matching-source reports freeze the exact `0.7x / 1.5s` runtime parameters. The earlier named Magic and Drone schema-v3 reruns remain supporting cross-family value evidence rather than the sole parameter-identity proof. Fire, Electric, and Wind damage Effects remain unchanged.

### 7.3 Dedicated Map Contract

The dedicated Map uses one straight route with three fixed Tower anchors:

```text
Spawn
  -> Shared Cooperation Zone: Position 1 and Position 2 cover the same route segment
  -> Neutral No-Hit Gap: longer than `5s` between possible applications
  -> Isolated Zone: Position 3 covers a later route segment
  -> Destination
```

Map acceptance requires:

- Positions 1 and 2 sit on opposite sides of the route and provide a broad, ordinary shared-target segment.
- Their common effective coverage contains at least three consecutive Monster-route Grids. With Node Size `1` and Monster Speed `0.25`, this provisionally represents approximately `12s` of cooperative route travel.
- Moving either shared-zone anchor by approximately one valid Grid position should not eliminate all useful shared-target opportunity. The diagnostic must not depend on a pixel-perfect placement.
- Position 3 has no simultaneous effective-coverage overlap with Positions 1 or 2.
- More importantly, the travel interval from the last possible hit in the shared zone to the first possible hit from Position 3 exceeds `5s`. At normal speed, use at least two complete no-hit Grid intervals between those possible application boundaries as the first robust authoring candidate.
- Separation is measured between possible impact/application boundaries, not Tower anchor centers.
- Position 2 and Position 3 provide comparable solo route exposure. A non-Elemental placement-parity run should keep their Effective Damage within an initial review guide of approximately `5-10%`; revise the anchors when placement strength, not overlap, dominates the comparison.

The Position 3 gap is a deliberate negative control, not a third simultaneously deployed Tower and not a normal gameplay placement recommendation. The non-overlap run moves the same second Tower from Position 2 to Position 3. Ordinary Stage Maps may allow adjacent or sequential matching Towers to cooperate while a Buff persists. Overlap accelerates shared stacking because both independent sources contribute during the same exposure; sequential placement may preserve existing stacks but normally does not provide the same simultaneous contribution rate. The three-Grid overlap is a provisional Stage6 derivation standard rather than a claim that all Maps must use the same geometry.

### 7.4 Fixed Cooperation Pair

Use one cross-family pair for all cooperation comparisons so only Element identity and target sharing change:

| Role | Tower | Build | Position |
|---|---|---|---:|
| Source A | Archer | Quick Draw + Scatter Arrow + tested Elemental | `1` |
| Overlap Source B | Cannon | Faster Reload + Twin Shells + tested Elemental | `2` |
| Non-overlap Source B | The same Cannon | The same build and Elemental | `3` |

Required comparisons are:

1. Archer at Position 1 alone.
2. Cannon at Position 2 alone.
3. Matching Archer Position 1 plus Cannon Position 2.
4. Mismatched Archer Position 1 plus Cannon Position 2, changing only Cannon's ElementType.
5. Matching Archer Position 1 plus Cannon Position 3.

Run matching overlap for every ElementType. One representative mismatched control is sufficient unless Element-specific evidence suggests otherwise. Because all four current Elemental Buffs use `5s`, one representative matching non-overlap control is sufficient; repeat it only if later calibration creates an Element-specific duration or handoff behavior.

## 8. Elemental Diagnostic Contract

Extend the Stage-end `CombatBalanceRunRecorder` output with an Elemental summary while preserving its existing combat summary and integrity checks.

The Recorder consumes read-only Buff lifecycle observations. It must not poll active snapshots and infer historical events. Gameplay ownership remains in Buff System; diagnostics may not change apply, refresh, stack, Overload, Protection, Effect execution, or removal behavior.

The observation payload must retain the current application source from the request and enough before/after state to distinguish:

- BuffDefinition and ElementType
- Source Tower and applied Elemental Upgrade
- Apply result
- Stack count before and after the attempt
- Phase before and after the attempt
- Whether the attempt reached maximum stacks
- Observation time
- Removal reason, including at least natural expiry versus Monster cleanup/resolution

At Stage end, aggregate at minimum by ElementType and source TowerFamily:

- Application attempts
- Applied, Refreshed, and Stacked results
- Source Apply Cooldown-blocked and Protection-blocked attempts
- Overload and EnteredProtection counts
- Natural expiries before Overload
- Average elapsed time from first application to Overload
- Distinct Monsters receiving the Buff
- Distinct source Towers contributing successful applications

The existing human-readable report remains in the Unity Console. Each terminal run additionally writes exactly one JSON report under `Doc/GamePlayRecord/`. A non-empty authored Run Name becomes `<RunName>.json`; a blank Run Name falls back to local completion time `yyyyMMdd_HHmmss.json`. If that filename already exists, the Recorder preserves it and selects the first available numeric suffix such as `_01` or `_02`. The Recorder does not create TXT output or mutable `Latest` aliases.

The accepted Task004 JSON contract used `schemaVersion = 7` and carries generation time, run identity, Monster fixture, combat totals, timing, player and integrity results, Tower, Upgrade, Projectile-runtime snapshots, and the Buff aggregates above. The current schema v8 preserves every schema-v7 field and adds Task005 Monster runtime identity, lifetime, resolution, damage-application, and observation-completeness diagnostics without reinterpreting Task004 evidence. The fixture snapshot obtains its expected count from the `MonsterWaveConfig` actually bound to `MonsterSpawner`, not from a Recorder Inspector field, and carries:

- `waveConfigName`
- `expectedMonsterCountAvailable`
- `expectedMonsterCount`

`expectedMonsterCount` is the sum of every authored Spawn Entry Count across every Wave in that bound config. `MonsterSpawner` publishes the spawning-start boundary immediately before starting its Wave coroutine, and the Recorder freezes the value at that boundary. This intentionally occurs after Stage preparation so a diagnostic Play Mode override made before spawning is captured, while later changes cannot rewrite the recorded expectation. If no valid bound WaveConfig is available, the Recorder retries at finalization, records `expectedMonsterCountAvailable = false`, uses `0` only as the unavailable JSON sentinel, prints `Expected=Unavailable`, and emits a warning. It must not replace the expected value with the observed Spawned count because that would hide incomplete spawn execution.

Each observed Buff aggregate additionally carries `definitionName` plus a nested `parameters` snapshot containing:

- `parameters.usesStacks`
- `parameters.activeDurationSeconds`
- `parameters.periodicTickIntervalSeconds`
- `parameters.maximumStacks`
- `parameters.sourceApplyCooldownSeconds`
- `parameters.overloadProtectionDurationSeconds`
- `parameters.moveSpeedMultiplierAvailable`
- `parameters.moveSpeedMultiplier`

The Recorder freezes this parameter snapshot when the BuffDefinition is first observed during the run. The movement multiplier is resolved from the first `SetMoveSpeedMultiplier` action on that BuffDefinition's `Applied` Effect binding. `moveSpeedMultiplierAvailable = false` and `moveSpeedMultiplier = 0` mean that no such action was authored; zero is not interpreted as a runtime movement multiplier. The Recorder does not reread mutable authoring state at Stage end. This makes each schema-v4-or-later report self-describing even when an exploratory Inspector value differs from the frozen baseline. A failed JSON write emits a diagnostic warning but cannot change the Battle result or suppress the Console report. Manual context snapshots remain Console-only because they are not terminal balance runs.

Each Tower snapshot additionally carries aggregate `projectileRuntime` diagnostics. These observations are not authored balance parameters:

- total, initial, and child Projectile releases;
- Arc Projectile releases;
- Arc impacts that resolved a direct Monster target;
- Arc impacts that reached the authored position without resolving a direct target;
- resolved Arc impacts split between intended-target and fallback-target results;
- position-only Arc impacts split between intended-target invalidation, intended-target out-of-range, and releases without an intended target;
- Arc Projectiles that ended without impact and those still unresolved when the report was written;
- the target-resolution rate across completed Arc impacts.

Schema v7 additionally carries `projectileRuntime.arcTargetRelation`. Only initial Arc members contribute to this relation diagnostic; bounce children remain counted by the existing release/outcome totals but cannot distort the Primary/Additional comparison. It records:

- Primary and Additional initial-Shell outcomes split into intended-target, fallback-target, and position-only counts;
- fallback spawn-order relations split into immediate later spawn (`ResolvedOrdinal - IntendedOrdinal = +1`), immediate earlier spawn (`-1`), non-adjacent, and unavailable identity;
- for a position-only result, the same relation categories for the nearest other gameplay-targetable Monster even when that Monster lies outside `Hit Distance Threshold`;
- observed Hit Distance Threshold;
- minimum, average, maximum, and sample count for confirmation-to-release time, release-to-impact time, planned Arc travel time, landing-to-target distances, and relevant Monster move speeds;
- one ordered sample per completed initial Arc member containing its member type, resolution type, intended/resolved/nearest-other spawn ordinals and deltas, timings, distances, move speeds, and availability flags.

Spawn ordinal is the Recorder's one-based Monster registration order for the current run. On the fixed single-route fixture, `+1` means the immediately later-spawned Monster, which normally follows behind the intended Monster. It is diagnostic identity rather than a targeting input. The nearest-other query is read-only and may observe a candidate outside the authored threshold, but it cannot promote that candidate into a hit.

Arc relation identity, distance, and move-speed values are frozen immediately after impact target resolution and before direct damage, Elemental application, or other impact results execute. A lethal hit or the current Shell's own Chilled application therefore cannot erase or rewrite the observation that explains how that Shell arrived.

For schema compatibility, the JSON metric remains named `confirmationToReleaseSeconds`. After the release-targeting refactor, it measures Windup admission to presentation Release Moment; target identity and position are not captured at the start of that interval.

An Arc position-only impact is not labelled as a generic miss because it may still execute an authored area Effect or release a bounce child. Initial and child releases remain separate so Bouncing Shell children do not distort Twin Shells' initial-release diagnosis. Projectile observation is Editor-only and read-only: it cannot alter targeting, damage, flight, Effect execution, or the Battle result.

The Cannon + Twin Shells + Chilled diagnostic identified repeated position-only arrivals despite unchanged release count and complete projectile resolution. Intended-target priority preserves every captured landing position and authored hit threshold while retaining each initial Shell's intended Monster identity. Its first paired run produced Control `98.77%` versus Cold `88.37%` target resolution and left `10` Cold position-only arrivals, so intended-target priority alone was not accepted as the complete correction. The schema-v6 classification then showed Control `39` intended / `41` fallback / `1` position-only versus Cold `47` intended / `24` fallback / `12` position-only.

The schema-v7 Control/Cold pair confirmed the relationship rather than a generic unresolved-Projectile failure. Control produced `37` intended / `43` fallback / `1` position-only results, and every fallback was the immediately later-spawned Monster. Cold produced `46` intended / `25` fallback / `12` position-only results; every fallback remained the immediately later-spawned Monster, while most position-only arrivals left that same nearby candidate just outside the unchanged `0.4` threshold. Confirmation-to-release timing remained approximately `0.25s`, and release-to-impact remained approximately `1.25s`. The normal Control therefore benefited from repeatedly hitting the following Monster, while Chilled changed that incidental interception geometry.

The accepted correction changes Archer and Cannon to a two-stage attack contract. Windup admission requires one valid target but freezes only release-group/member topology. At the animation Release Moment, the Tower selects current valid in-range targets and freezes Arrow direction or Shell intended identity and landing position. Released Projectiles remain non-tracking. Applying Scatter Arrow or Multi Shells during Windup cannot change the pending member count. A Release Moment with no valid primary target cancels the group and does not start the Attack Cycle.

This correction intentionally does not change Cannon Arc travel time, Projectile speed, Arc height, or `Hit Distance Threshold`. The accepted paired Control/Cold rerun produced `100% / 98.81%` Arc target resolution and removed the earlier negative-Upgrade behavior. Cannon Projectile Speed remains `2` to preserve readable ballistic weight; no flight revision is required by Task004.

Post-refactor acceptance used the existing Straight Route, HP `240`, speed `0.25`, spawn interval `2.5s`, and Position 1:

1. `HP240_P1_Archer_QuickDraw+ScatterArrow_Control_ReleaseTimeTargeting`.
2. `HP240_P1_Archer_QuickDraw+ScatterArrow_Cold_Slow0.7_Cooldown1.5_ReleaseTimeTargeting`.
3. `HP240_P1_Cannon_FasterReload+TwinShells_Control_Projectiles_ReleaseTimeTargeting`.
4. `HP240_P1_Cannon_FasterReload+TwinShells_Cold_Slow0.7_Cooldown1.5_Projectiles_ReleaseTimeTargeting`.

All four reports completed Monster resolution with both integrity flags true. Archer Control/Cold produced `3570 / 3685` Effective Damage with `378 / 384` released Arrows, preserving complete three-member Scatter groups. Cannon Control/Cold produced `3720 / 3780` Effective Damage with `100% / 98.81%` Arc target resolution. A manual cancellation smoke confirmed that a Monster invalidated during Windup produces no Arrow or Shell, starts no Attack Cycle, and allows immediate reacquisition when a valid target returns.

When sharing results, identify the named file or ask Codex to inspect the newest files in `Doc/GamePlayRecord/`; copying the whole Console block is no longer required. Repeated calibration runs may reuse one logical Run Name because the exported filename suffix preserves every result.

The final summary must still report `ResolutionCountsMatch=True` and `LeakCountMatchesPlayerHealthLoss=True` before a balance run is accepted.

## 9. Calibration Sequence

### Phase A - Map And Instrumentation Gate

1. Validate the authored `5s` natural expiry once on the dedicated straight route: after the last successful refresh, the Buff should expire after approximately `1.25` normal-speed Grids and before a two-Grid no-hit handoff completes.
2. Author the dedicated straight diagnostic Map and three fixed anchors.
3. Run the Position 2 versus Position 3 non-Elemental placement-parity check.
4. Validate the implemented read-only Elemental diagnostics in Unity Play Mode and inspect the generated JSON.
5. Confirm that a manual lifecycle sequence and Recorder counts agree on one short run.

### Phase B - Single-Source Functional Pass

1. Measure the four Pre-Elemental Reference Controls on the Task004 fixture.
2. Add exactly one Elemental Upgrade to the same Tower and position.
3. First cover all four Element mechanics with one named TowerFamily each.
4. Confirm initial application, Active Duration refresh, Source Apply Cooldown, Maximum-Stacks ordering, Overload, Protection, and removal.
5. Confirm one Tower's primary, additional, area, contact, and persistent opportunities share its Source Apply Cooldown against one Monster.
6. Confirm a different Tower has an independent Source Apply Cooldown against that same Monster and BuffDefinition.
7. Confirm reaction or lifecycle damage does not recursively apply Elemental Buffs.

Functional observations include:

- Fire: Burning periodic damage and FlameBurst.
- Cold: slow, Frozen, movement lock, and release.
- Electric: StackApplied damage and multi-target Overcharged.
- Wind: secondary Wind attack and persistent WindVortex.

### Phase C - Single-Source Value Matrix

Run every supported TowerFamily with every ElementType using its frozen Pre-Elemental Reference Control. Each of the sixteen Elemental runs compares only against that family's Task004 control at the same position and fixture.

Record Effective Damage, Damage Coverage, realized gain, stacks, Overloads, blocked attempts, natural expiry, and whether the intended Element behavior was visible.

### Phase D - Representative Upper-Stress Runs

For each TowerFamily:

1. Measure its Section 6.3 upper-stress build without an Elemental Upgrade on the Task004 fixture.
2. Add the selected highest-risk Elemental Upgrade.
3. Confirm bounded trigger ownership, no recursive Elemental application, and measurable headroom.

The proposed single-Tower ceiling remains `4.0x` relative to the naked same-family Task004 control. A ceiling result is a lower bound and requires a higher-Health diagnostic before exact acceptance.

### Phase E - Matching-Source Cooperation

1. Run the fixed Archer/Cannon matching overlap comparison for all four ElementTypes.
2. Run the representative mismatched overlap control.
3. Run one representative matching non-overlap control by moving the second Tower from Position 2 to Position 3.
4. Compare Overloads, time-to-Overload, blocked attempts, and total value against the two single-source runs.
5. Revise Active Duration, Maximum Stacks, Source Apply Cooldown, Overload Protection Duration, Periodic Tick Interval, or lifecycle Effect output only after identifying which measurement causes the failure.

### Current Calibration Evidence Status - 2026-08-17

The current `Doc/GamePlayRecord/` inventory contains `83` parseable reports: `43` schema-v1, one schema-v2, `14` schema-v3, two schema-v4, five schema-v5, seven schema-v6, and `11` schema-v7 reports. Every report satisfies `ResolutionCountsMatch=True` and `LeakCountMatchesPlayerHealthLoss=True`. Historical schema-v1 reports remain valid screening evidence but cannot by themselves accept an exact parameter revision.

Structural evidence is complete:

- the `5s` natural-expiry smoke observation, dedicated straight Map, and three authored positions;
- Position 2 `3810` versus Position 3 `3450` Cannon Effective Damage, a `9.45%` placement difference inside the initial `5-10%` guide;
- all four Pre-Elemental Reference Controls and all sixteen single-source TowerFamily-by-ElementType runs;
- the four representative upper-stress controls and Elemental runs, plus HP480 Archer and Cannon ceiling diagnostics;
- all four matching overlap runs, one mismatched overlap control, and one matching non-overlap control;
- schema-v3 Electric and Wind HP960 diagnostics with `Expected=3`, two contributing source Towers, Maximum Stacks `10`, two Overloads, two Protection entries, and internally balanced application counts;
- manual confirmation that Overcharged damages nearby Monsters and that the slower WindVortex preserves its persistent moving-zone identity;
- Fire Overload and Protection, Cold Overload and Frozen lifecycle output, independent source cooldowns, and non-recursive reaction damage;
- release-time target confirmation for Archer and Cannon, immutable released direction/position, and cancellation without Attack Cycle when no release target remains.

Final numerical decisions are:

- Electric gain spans `1.087x-1.333x` and Wind spans `1.186x-1.528x`; keep their normal Effect values.
- Fire single-source screening spans `1.214x-1.790x`, while HP480 upper-stress reruns remain `1.249x-1.332x`. The post-refactor HP960 matching pair produced `1715` versus `1405` Control (`1.221x`), two Overloads, `23` periodic ticks, and no ceiling. Keep Burning Tick Interval `2s`, Source Apply Cooldown `2s`, and current FlameBurst output.
- Cold `0.7x / 1.5s` single-source gains are Archer `1.032x`, Cannon `1.016x`, Magic `1.464x`, and Drone `1.203x`. The post-refactor HP960 matching pair produced `2140` versus `1405` Control (`1.523x`), three Overloads across three Monsters, three Protection entries, and complete Frozen application. Accept the v0.4 Cold revision.
- Electric and Wind require no blanket reruns. All Phase A-E structural, value, stress, and cooperation gates are complete.

## 10. Required Measurements And Decision Rules

- Effective Damage, Damage Coverage, leaks, Peak Alive, and ceiling state
- Time from first application to Overload
- Successful stack additions
- Source Apply Cooldown-blocked and Protection-blocked applications
- Natural expiry before maximum stacks
- Overloads per fixed run
- Overload Protection Duration and observable blocked applications
- Overload Effect result
- Difference between single-source and dual-source stacking
- Control result, Elemental result, and realized-gain ratio
- Matching, mismatched, and non-overlap pair results

Interpretation targets:

- A Single-Source Elemental Reference Core has meaningful normal value and can build stacks before a matching source exists.
- A single source should not reliably Overload every Monster under ordinary Reference exposure.
- The fixed matching pair should reliably trigger Overload while a surviving Monster traverses the accepted three-Grid shared zone, without requiring exact-placement geometry.
- A mismatched pair produces two independent normal Elemental states and no shared stacks.
- The forced non-overlap pair cannot carry shared progress across the accepted neutral gap.
- Protection prevents immediate repeated Overload for the same BuffDefinition.

First numerical review guides:

- Do not force every ElementType or TowerFamily to one identical gain ratio; Element identity and attack opportunity remain material.
- Fire, Electric, and Wind normal-stage results generally use `1.10x-1.60x` as a review guide rather than a hard gate. A ratio outside it requires an absolute-damage, ceiling, route, or attack-archetype explanation.
- Cold is judged as a team-support Element. Archer or Cannon may show little single-Tower damage gain, but persistent-contact Towers should not receive an unexplained near-double multiplier before Frozen cooperation.
- The accepted Cold `0.7x` result produced `1.464x` for Magic and `1.203x` for Drone, while the post-refactor Archer and Cannon comparisons remained non-negative at `1.032x` and `1.016x`.

If a single source Overloads nearly every target, matching-source identity is too weak. If a matching pair rarely reaches Overload despite the accepted broad shared zone, the cooperation requirement is too strict. These observations trigger diagnosis, not an automatic one-parameter revision.

## 11. Ownership

| Owner | Responsibility |
|---|---|
| Tower Upgrade System | Elemental eligibility and accepted Tower state |
| Tower Runtime Combat | Elemental application opportunity |
| Effect System | One-shot lifecycle result execution |
| Buff System | Shared stacks, Source Apply Cooldown, Active Duration, Overload, Protection, removal reasons, and read-only lifecycle observations |
| Combat Balance Run Recorder | Diagnostic aggregation and Stage-end reporting only |
| Task004 | Elemental power budget, named fixed comparisons, and accepted Elemental baseline |

## 12. Result Table

| ElementType | Tower / Test Build | Position Set | Control Result | Elemental Result | Gain | Applied / Stacked / Blocked | Overloads / Avg Time | Ceiling | Decision |
|---|---|---|---:|---:|---:|---|---|---|---|
| Cold | Archer Reference Core | P1 | `3570` | `3685` | `1.032x` | `47 / 136 / 169` | `0 / -` | No | Accept `Slow0.7 / Cooldown1.5` |
| Electric | Archer Reference Core | P1 | `3560` | `4275` | `1.201x` | `42 / 75 / 221` | `0 / -` | No | Keep first-pass values |
| Fire | Archer Reference Core | P1 | `3560` | `5470` | `1.537x` | `46 / 72 / 219` | `0 / -` | No | Accept `Tick2 / Cooldown2 / current FlameBurst` |
| Wind | Archer Reference Core | P1 | `3560` | `5075` | `1.426x` | `42 / 77 / 221` | `0 / -` | No | Keep first-pass values |
| Cold | Cannon Reference Core | P1 | `3720` | `3780` | `1.016x` | `70 / 13 / 0` | `0 / -` | No | Accept `Slow0.7 / Cooldown1.5` |
| Electric | Cannon Reference Core | P1 | `3450` | `3750` | `1.087x` | `31 / 40 / 0` | `0 / -` | No | Keep first-pass values |
| Fire | Cannon Reference Core | P1 | `3450` | `4190` | `1.214x` | `31 / 37 / 0` | `0 / -` | No | Accept `Tick2 / Cooldown2 / current FlameBurst` |
| Wind | Cannon Reference Core | P1 | `3450` | `4090` | `1.186x` | `36 / 35 / 0` | `0 / -` | No | Keep first-pass values |
| Cold | Magic Reference Core | P1 | `1734` | `2538` | `1.464x` | `40 / 77 / 3` | `0 / -` | No | Accept `Slow0.7 / Cooldown1.5` |
| Electric | Magic Reference Core | P1 | `1734` | `2312` | `1.333x` | `40 / 47 / 2` | `0 / -` | No | Keep first-pass values |
| Fire | Magic Reference Core | P1 | `1734` | `3104` | `1.790x` | `40 / 46 / 3` | `0 / -` | No | Keep `Tick2`; low-Control exception |
| Wind | Magic Reference Core | P1 | `1734` | `2650` | `1.528x` | `40 / 44 / 2` | `0 / -` | No | Keep first-pass values |
| Cold | Drone Reference Core | P1 | `4132` | `4972` | `1.203x` | `49 / 157 / 685` | `3 / -` | No | Accept `Slow0.7 / Cooldown1.5` |
| Electric | Drone Reference Core | P1 | `4132` | `5320` | `1.288x` | `41 / 108 / 564` | `0 / -` | No | Keep first-pass values |
| Fire | Drone Reference Core | P1 | `4132` | `5998` | `1.452x` | `40 / 110 / 565` | `0 / -` | No | Accept `Tick2 / Cooldown2 / current FlameBurst` |
| Wind | Drone Reference Core | P1 | `4132` | `5644` | `1.366x` | `38 / 96 / 514` | `0 / -` | No | Keep first-pass values |

Record exact authored values with every accepted result. Schema-v3-or-later BuffDefinition snapshots are sufficient for Buff-only revisions; schema-v4-or-later snapshots are required when accepting a lifecycle Effect parameter such as Chilled movement multiplier. Do not mix runs made before and after an Effect, Buff, Map, or fixture revision under one candidate row.

Final cooperation acceptance:

| ElementType | Build | Control Result | Matching Result | Gain | Overloads | Decision |
|---|---|---:|---:|---:|---:|---|
| Cold | P1 Archer + P2 Cannon | `1405` | `2140` | `1.523x` | `3 / 3 Monsters` | Accept v0.4 cooperation |
| Fire | P1 Archer + P2 Cannon | `1405` | `1715` | `1.221x` | `2 / 3 Monsters` | Accept Tick2 / Cooldown2 / current FlameBurst |

## 13. Execution Collaboration

- The user owns Unity Map and asset authoring, visible lifecycle observation, and Play Mode execution.
- Codex owns the first-pass result table, ratio and timing calculations, evidence review, and smallest justified revision proposal.
- Manual visual notes supplement the Recorder but do not replace its lifecycle counts after the instrumentation gate.
- Exact Stage5 and Stage6 pool composition remains a Task007 output rather than a prerequisite for isolated Task004 tests.

## 14. Unity Authoring Checklist

- Observe and record natural-expiry Grid distance on an existing Map.
- Author the dedicated straight Task004 Map with fixed Positions 1, 2, and 3.
- Validate shared-zone exposure, neutral-gap expiry, and Position 2/3 placement parity.
- Validate all Elemental UpgradeDefinitions and shared BuffDefinition references.
- Keep the first-pass Buff parameters unchanged until diagnostics identify a failure cause.
- Confirm the three-Grid shared route zone at Monster Speed `0.25` before accepting cooperation results.
- Confirm the Position 3 handoff leaves more than `5s`, provisionally at least two complete normal-speed no-hit Grid intervals, between possible applications.
- Confirm Tower A cannot consume or delay Tower B's Source Apply Cooldown.
- Test every named build without substituting an unnamed extra condition.
- Test two different TowerFamilies using the same ElementType.
- Test the mismatched and non-overlap controls.
- Validate StackApplied, Overload, EnteredProtection, natural expiry, and cleanup removal reporting.
- Confirm the JSON WaveConfig identity and automatically derived Expected Monster Count match the bound diagnostic fixture.
- Confirm each terminal run creates one parseable named JSON report, or a timestamp-fallback report when Run Name is blank, and no TXT or `Latest` alias.
- Record accepted Effect and Buff values.

## 15. Acceptance Criteria

- Every ElementType has a readable normal phase and Overload result.
- Shared stacks work across matching TowerFamilies.
- One Tower's Attack Entities share its cooldown entry while a second Tower contributes through an independent entry.
- Stage5 can demonstrate useful Elemental application and stacking without requiring a second source.
- Stage6 can use two matching Elements to reach Overload more reliably through a broad shared-target opportunity.
- A Single-Source Elemental Reference Core has measurable normal value before a second matching source exists.
- Representative Upper-Stress Elemental Builds respect the accepted single-Tower power ceiling.
- Any gain above that ceiling is attributable to matching-source cooperation rather than the Elemental Layer name alone.
- Mismatched and forced non-overlap controls do not imitate matching-source cooperation.
- Protection is observable and prevents immediate reapplication for the same definition.
- Reaction and lifecycle damage do not recursively apply Elemental Buffs.
- No Elemental value change silently rewrites Base Tower identity.
- Recorder Console and JSON output agree with manual lifecycle observations and retain the combat integrity checks.

## 16. Validation

- Dedicated Map geometry and placement-parity runs
- Fixed-condition Pre-Elemental and single-source runs
- Full TowerFamily-by-ElementType value matrix
- Representative Upper-Stress Elemental runs
- Fixed-condition matching, mismatched, and non-overlap pair runs
- Lifecycle ordering and non-recursion checks
- Cross-family shared-stack checks
- Static asset validation
- Path-scoped documentation and implementation diff validation

## 17. Review Note

Task004 is complete. It owns the accepted matching-source and Overload calibration, Required Levels, authored parameters, fixtures, diagnostics, and test evidence. `01_TowerGrowthAndUpgradeIdentity.md` remains the qualitative growth contract. `15_BuffSystem.md` remains the stable lifecycle and ownership contract. Final Stage5 and Stage6 Elemental pool composition is derived and authored by Task007 from the Blueprint's required capabilities.
