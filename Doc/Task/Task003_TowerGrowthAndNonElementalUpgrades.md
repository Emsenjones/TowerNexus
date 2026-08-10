# Task003 - Tower Growth And Non-Elemental Upgrades

Status: Planned; Tower Level v0.1 code and document prerequisites are complete, while Unity authoring migration and runtime acceptance remain pending

Depends on: Task002 `Base Combat v0.1`; approved Tower Growth And Upgrade Identity balance contract

## 1. Goal

Calibrate Required Tower Level gates plus Basic and Behaviour Upgrades on top of the frozen Base Combat baseline and the Stage-authorized Tower Level v0.1 contract.

Each growth choice should create visible value without erasing the owning TowerFamily's base identity. Level Up buys future eligibility and model progression; applied Upgrades create combat-stat or attack-strategy growth.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`

## 3. Preconditions

- Task002 `Base Combat v0.1` Level 1 values remain frozen.
- Tower runtime templates own Base Attack Damage, Range, Cycle, and targeting authoring.
- TowerLevelConfig owns level identity and model data without combat stats.
- Tower Upgrade System binds the active Stage Upgrade pool and enforces the derived per-TowerFamily level cap.
- Stage validation rejects a level path that does not unlock at least one Upgrade at every reached level.
- Rogue-like sampling does not guarantee that newly eligible content appears in a later Draft.

## 4. In Scope

- Required Tower Level gates for non-Elemental content
- At least one valid newly eligible content path through every calibrated L2/L3 Core progression
- Basic Upgrade additive deltas
- Behaviour Upgrade packages
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

1. Keep Task002 Map, Reference Monster, and Base Tower values fixed.
2. Select one controlled TowerFamily and Stage-allowed Upgrade set.
3. Assign Required Tower Level gates so each reached level unlocks real content.
4. Confirm that Level Up alone does not change combat output.
5. Calibrate Basic Upgrades one at a time.
6. Calibrate Behaviour Upgrades one package at a time.
7. Test supported combinations without changing the frozen base to repair one Upgrade.
8. Record expected gain, measured result, and keep/revise decision.
9. Recheck the Stage1 Reference Build cost and non-Elemental Core feasibility without finalizing its Task006 pool.

## 7. Required Measurements

- Pre-upgrade and post-upgrade TTK or resolved-Monster result
- Change in range, cadence, uptime, or coverage
- Multi-target or persistent-entity gain where relevant
- Required Tower Level and Draft cost
- Level at which the Upgrade first becomes eligible
- Whether the Upgrade creates a meaningful current-versus-future choice
- Whether the TowerFamily identity remains visible

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Tower Growth And Upgrade Identity | Cross-Stage player experience, investment interpretation, Core/Support intent, and Anti-patterns |
| TowerDefinition | Ordered level identity and model data |
| Tower Upgrade System | Stage-bound level eligibility, application, and accepted Upgrade state |
| Tower Runtime Combat | Base combat authoring and runtime interpretation of accepted Upgrades |
| Task003 | Required-Level proposals, comparative calibration, and regression evidence |
| Task006 | Exact Stage pools, Draft budgets, and candidate solvability |

## 9. Required Proposal Table

Codex prepares the first-pass table from the accepted Task002 baseline:

| Content | Current Value | Proposed Value | Required Level | Intended Experience | Fixed Test | Observed Result | Decision |
|---|---:|---:|---:|---|---|---|---|
| Basic or Behaviour Upgrade | Record from asset | First-pass estimate | First eligible Tower level | Expected visible gain or specialization | Fixed Map, Monster, and placement | Filled after Play Mode | Keep or revise |

## 10. Execution Collaboration

- The user describes the intended growth or Upgrade feeling and owns Unity asset authoring plus Play Mode observation.
- Codex fills the first-pass Required-Level, Basic, and Behaviour proposal table, predicts the expected change, and revises one value group at a time from the returned measurements.
- The user's Play Mode result, not the initial calculation alone, determines acceptance.

## 11. Unity Authoring Checklist

- Force one known Upgrade at a time for controlled comparison.
- Keep Map, Monster, placement, and simulation conditions fixed.
- Validate each Stage-authorized level transition without applying an Upgrade.
- Validate every Basic and Behaviour definition selected for the pass.
- Validate package-capacity and duplicate rejection.
- Record every accepted authoring change.

## 12. Acceptance Criteria

- Every calibrated level transition unlocks at least one real Stage-allowed Upgrade.
- Level Up alone does not change resolved combat stats.
- Basic Upgrades produce coherent numerical specialization.
- Behaviour Upgrades change attack strategy visibly.
- No Upgrade is required merely to repair an unusable Base Tower.
- At least one non-Elemental L3 Core path is feasible for Task006 pool construction.
- Stage1 approved Reference Build cost remains feasible.
- Task002 Base Tower identities survive the full regression.

## 13. Validation

- Fixed-condition A/B Play Mode runs
- Required-Level, eligibility, and application validation
- Combination regression
- Static asset validation
- Stage1 Reference Build cost regression

## 14. Review Note

Any proposed change to Task002 frozen values must be treated as a Base Combat revision and must rerun the complete Task002 comparison. Exact Stage pools and actual sampling solvability remain Task006 outputs.
