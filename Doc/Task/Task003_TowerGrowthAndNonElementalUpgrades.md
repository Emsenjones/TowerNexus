# Task003 - Tower Growth And Non-Elemental Upgrades

Status: Planned and blocked by Task003A plus Task002 `Base Combat v0.3`; Power Budget targets, revised Upgrade values, and final Play Mode acceptance remain pending

Depends on: Completed Task003A implementation; accepted Task002 `Base Combat v0.3`; approved Tower Growth And Upgrade Identity balance contract

## 1. Goal

Calibrate Required Tower Level gates plus Basic and Behaviour Upgrades on top of the frozen Base Combat baseline and the Stage-authorized Tower Level v0.1 contract.

Each growth choice should create visible value without erasing the owning TowerFamily's base identity. Level Up buys future eligibility and model progression; applied Upgrades create combat-stat or attack-strategy growth.

Every Basic and Behaviour Upgrade is measured on fixed Straight, L, and U routes. L is the shared Primary Acceptance Route because a strategic player is expected to reserve a strong ordinary bend position for the Core Tower. Straight is a low-exposure diagnostic and U is an extreme-exposure observation; neither is an automatic Task003 acceptance target.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/12_ProjectileSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/Task/Task003A_NonElementalUpgradeDefinitionRevision.md`

## 3. Preconditions

- Task003A revised Upgrade definitions and runtime contracts are implemented and accepted.
- Task002 `Base Combat v0.3` Level 1 values are frozen after the complete naked-Tower route regression.
- Tower runtime templates own Base Attack Damage, Range, Cycle, and targeting authoring.
- TowerLevelConfig owns level identity and model data without combat stats.
- Tower Upgrade System binds the active Stage Upgrade pool and enforces the derived per-TowerFamily level cap.
- Stage validation rejects a level path that does not unlock at least one Upgrade at every reached level.
- Rogue-like sampling does not guarantee that newly eligible content appears in a later Draft.

## 4. In Scope

- Required Tower Level gates for non-Elemental content
- At least one valid newly eligible content path through every calibrated L2/L3 Core progression
- A shared four-Tower naked screening run under Task003 calibration pressure
- Straight, L, and U route matrices for every Basic and Behaviour Upgrade
- Basic Upgrade additive deltas
- Behaviour Upgrade packages
- Single-Upgrade, Reference Core, same-Layer combination, and maximum non-Elemental stress builds
- Per-Tower Upgrade capacity and duplicate rules
- Upgrade-before and Upgrade-after combat comparisons
- Reference Build cost accounting for non-Elemental Stages
- Regression against the four Base Tower identities

## 5. Out Of Scope

- Direct combat-stat growth from Tower Level
- Elemental Layer
- Elemental Buff stacking, Overload, and Protection
- Final Monster roster
- Player Progress Requirements
- Exact Stage Tower or Tower Upgrade pools
- Stage MonsterWaveConfig tuning
- Guaranteed post-Level-Up Draft offers

## 6. Calibration Sequence

1. Preserve the Task002 Map plus all frozen `Base Combat v0.3` Tower and Reference Monster asset values.
2. Reuse the Task002 Straight, L, and U route suite with `40` Monsters, fixed `12` Max Health, `0.25` Move Speed, and `2.5s` Spawn Interval.
3. Run Archer, Cannon, Magic, and Drone at Level 1 with no Upgrades on L first. Record every fixed-fixture result, including zero or `40 / 40`, before deciding whether the fixture can distinguish Upgrade value.
4. Record reusable naked L, Straight, and U controls for the selected TowerFamily while route, placement, Monster, health, count, and timing remain fixed.
5. Select one Basic or Behaviour Upgrade. Its Primary Acceptance Route is L without a per-Upgrade route-selection step.
6. Assign Required Tower Level gates so each reached level unlocks real content; validate TowerLevelConfig and resolved data remain combat-stat-free rather than repeating an expected no-output-change Play Mode run.
7. Force-apply the selected Upgrade alone and run its complete Straight, L, and U matrix.
8. Repeat the single-Upgrade matrix for every Basic and Behaviour UpgradeDefinition.
9. Run the required cumulative builds: combined Basic, combined Behaviour, Reference non-Elemental Core, and maximum non-Elemental stress build.
10. Record expected gain, all route results, realized-gain ratios, ceiling state, and Keep/Revise decision.
11. Recheck the Stage1 Reference Build cost and non-Elemental Core feasibility without finalizing its Task006 pool.

### 6.1 Fixed Fixture And Reuse

- Task003 v0.1 reuses the Task002 `Base Combat v0.3` fixture: `40` Monsters, `12` Max Health, `0.25` Move Speed, and `2.5s` Spawn Interval. The interval is not shortened because denser presentation is visually undesirable.
- Reusing the same naked controls and route roles gives every Upgrade comparison the accepted post-Task003A starting line; Task003 measures the upgraded state rather than redefining the Base Tower fixture.
- The canonical L fixture contains one ordinary 90-degree bend and one strong Core placement. It must not behave like a U fixture by covering multiple separated route segments, or both the entrance and Target zones, from the same Tower position.
- `Naked control` means the same TowerFamily at the same Required Tower Level as the tested build with no applied Upgrades. Using that control isolates Upgrade value and is not a separate claim that Level Up grants combat stats.
- A naked control may be reused by every Upgrade on the same TowerFamily only while route, placement, fixture health, Monster, count, timing, and observation method remain unchanged.
- If a naked result is zero or an upgraded result reaches `40 / 40`, retain that outcome in the Task003 v0.1 record rather than silently changing Health.
- A differently named Health-sensitivity run may be added when an exact ceiling magnitude or measurable control is required. It is supplemental diagnostic evidence and cannot replace the fixed v0.1 comparison.
- If `HP 12` proves fundamentally unable to support acceptance, propose an explicit Task003 fixture revision. After approval, assign a new fixture version and rerun every affected naked control and upgraded build under the new fixed conditions.

### 6.2 Route Matrix And Decision Authority

For each named Upgrade or required combination:

```text
Straight Gain = Upgraded Straight Result / Naked Straight Result
L Gain        = Upgraded L Result / Naked L Result
U Gain        = Upgraded U Result / Naked U Result
```

- L is the shared Primary Acceptance Route and owns the Task003 candidate-target decision.
- Straight is the mandatory low-exposure diagnostic and Task002 identity regression.
- U is a mandatory extreme-exposure observation, but a high U result does not automatically fail the Upgrade.
- Straight, L, and U absolute results are not flattened across TowerFamilies.
- A U result triggers revision only when it reveals an unbounded mechanic, breaks the cumulative single-Tower budget, or represents a realistic Stage risk. Otherwise it is recorded as exceptional route strength.
- Any revision first changes the tested UpgradeDefinition's own coherent parameter or redesigns that Upgrade. Task002 Base Damage, Base Range, Base Attack Cycle Duration, and other frozen naked-Tower values remain unchanged.

## 7. Proposed Power Targets

These are Task003-local calibration hypotheses. They remain proposed until Play Mode acceptance and do not become part of `01_TowerGrowthAndUpgradeIdentity.md`; accepted parameters remain in their owning Task record and Unity assets.

The twenty-four single-Upgrade L-route results collected before Task003A are exploratory definition-review evidence only. They remain useful for identifying weak, dominant, or position-sensitive concepts, but results for replaced definitions and the pre-revision naked Magic runtime cannot satisfy final acceptance. Every revised definition must be retested from the accepted v0.3 naked control.

| Test Build | Exact Construction | Proposed L-Route Target |
|---|---|---:|
| Single Basic | Naked Tower plus exactly one named Basic UpgradeDefinition | `1.15x-1.30x` |
| Combined Basic stress build | Apply every current Basic UpgradeDefinition for that TowerFamily once to the same Tower | Approximately `1.50x-1.65x` maximum |
| Single Behaviour | Naked Tower plus exactly one named Behaviour UpgradeDefinition | `1.35x-1.65x` |
| Combined Behaviour stress build | Apply every current Behaviour UpgradeDefinition for that TowerFamily once to the same Tower, subject to the existing package-type capacity rules | Approximately `2.0x-2.3x` maximum |
| Reference non-Elemental Core | One Tower with exactly one named Basic plus one named Behaviour Upgrade | Approximately `1.8x` first-pass center |
| Maximum non-Elemental stress build | One Tower with the combined Basic and combined Behaviour stress sets | Approximately `3.0x` maximum |

`Single` rows measure one UpgradeDefinition without another Upgrade. `Combined` rows measure cumulative behavior that single-Upgrade tests cannot expose. They are mechanical stress builds; Task006 later decides whether one Stage pool actually offers every included definition together.

These are empirical acceptance bands rather than direct stat-multiplier formulas. One run per naked Tower is sufficient for the initial Straight fixture screen. For each TowerFamily, record at least two fixed-condition naked runs per route at the final fixture health; those controls may be reused. Each final Upgrade value requires two matching upgraded runs per route that support the same practical conclusion. Exploratory values may use one complete Straight/L/U matrix before confirmation.

## 8. Required Measurements

- Pre-upgrade and post-upgrade TTK or resolved-Monster result
- Change in range, cadence, uptime, or coverage
- Multi-target or persistent-entity gain where relevant
- Required Tower Level and Draft cost
- Level at which the Upgrade first becomes eligible
- Whether the Upgrade creates a meaningful current-versus-future choice
- Whether the TowerFamily identity remains visible
- Naked control result and upgraded result under the same fixture
- Straight, L, and U realized-gain calculations using the same named measurement
- Whether either state reaches the `40 / 40` ceiling on each route
- Whether a high U result is accepted as exceptional route strength or escalated for revision

## 9. Ownership

| Owner | Responsibility |
|---|---|
| Tower Growth And Upgrade Identity | Qualitative cross-Stage growth experience, investment interpretation, Core/Support intent, and growth-curve principles |
| TowerDefinition | Ordered level identity and model data |
| Tower Upgrade System | Stage-bound level eligibility, application, and accepted Upgrade state |
| Tower Runtime Combat | Base combat authoring and runtime interpretation of accepted Upgrades |
| Task003 | Required-Level proposals, comparative calibration, and regression evidence |
| Task006 | Exact Stage pools, Draft budgets, and candidate solvability |

## 10. Required Proposal Tables

Codex prepares the Upgrade proposal before Play Mode:

| Upgrade Or Combination | Current Value | Proposed Value | Required Level | Intended Experience | L-Route Target | Decision |
|---|---:|---:|---:|---|---|---|
| Named Basic, Behaviour, or required combination | Record from asset | First-pass estimate | First eligible Tower level | Expected visible gain or specialization | Applicable Task003 target row | Keep or revise |

Each proposal then receives a route-result matrix:

| Upgrade Or Combination | Fixture | Route | Naked Result | Upgraded Result | Realized Gain | Ceiling | Observation / Decision |
|---|---|---|---:|---:|---:|---|---|
| Named test build | Task003 v0.1 or named supplemental diagnostic | Straight, L, or U | Reusable same-family control | Filled after Play Mode | Calculated from the named measurement | Yes or no | Accepted, revise, or exceptional U strength |

## 11. Execution Collaboration

- The user describes the intended growth or Upgrade feeling and owns Unity asset authoring plus Play Mode observation.
- Codex fills the first-pass Required-Level, Basic, and Behaviour proposal table, predicts the expected change, and revises one value group at a time from the returned measurements.
- The user's Play Mode result, not the initial calculation alone, determines acceptance.

## 12. Unity Authoring Checklist

- Force one known Upgrade at a time for controlled comparison.
- Confirm the accepted Task002 `Base Combat v0.3` fixture is `40 / HP 12 / Speed 0.25 / Spawn 2.5s` before recording results.
- Run the four naked Towers under the fixed L fixture before Upgrade tuning.
- Record fixed L, Straight, and U controls for each TowerFamily before its Upgrade matrices.
- Keep Map, Monster, placement, and simulation conditions fixed.
- Validate each Stage-authorized level transition through schema, resolved-state, and eligibility inspection; add a Play Mode output regression only if combat-resolution code changes.
- Validate every Basic and Behaviour definition selected for the pass.
- Validate package-capacity and duplicate rejection.
- Run every single-Upgrade and required combination test on Straight, L, and U.
- Keep supplemental Health-sensitivity results separately named; do not overwrite the fixed v0.1 route matrix.
- After an explicitly approved fixture-version revision, rerun affected controls and upgraded states together.
- Record every accepted authoring change.

## 13. Acceptance Criteria

- Every calibrated level transition unlocks at least one real Stage-allowed Upgrade.
- TowerLevelConfig and resolved Level state contain no direct combat-stat growth.
- Basic Upgrades produce coherent numerical specialization.
- Behaviour Upgrades change attack strategy visibly.
- Every Task003 v0.1 acceptance result uses the fixed `40 / HP 12 / Speed 0.25 / Spawn 2.5s` fixture; supplemental diagnostics do not replace it.
- Fixed comparisons retain measurable headroom; no accepted gain is inferred from two `40 / 40` results.
- Every Basic, Behaviour, and required combination has a complete Straight/L/U result matrix.
- Individual and cumulative L-route realized gains satisfy the accepted Task003 target or record an explicit reviewed exception.
- U-route observations are reviewed without forcing TowerFamilies or route shapes into equal absolute output.
- No Upgrade is required merely to repair an unusable Base Tower.
- At least one non-Elemental L3 Core path is feasible for Task006 pool construction.
- Stage1 approved Reference Build cost remains feasible.
- Task002 Base Tower identities survive the full regression.

## 14. Validation

- Fixed-condition A/B Play Mode runs
- Four-family naked Task003 screening run
- Straight/L/U matrices for every single Upgrade and required combination
- Required-Level, eligibility, and application validation
- Combination regression
- Static asset validation
- Stage1 Reference Build cost regression

## 15. Review Note

Task003 v0.1 reuses the frozen Task002 `Base Combat v0.3` fixture and route roles after Task003A. This Task retains candidate targets, accepted Required Levels and authored parameters, detailed Upgrade route matrices, and decisions; `01_TowerGrowthAndUpgradeIdentity.md` remains qualitative. Any later proposed change to the Task002 Tower or Reference Monster assets must be treated as another Base Combat revision and must rerun the complete Task002 route suite. U-route strength alone does not authorize a frozen Base Tower change. Exact Stage pools, concentrated-build legality, and actual sampling solvability remain Task006 outputs.
