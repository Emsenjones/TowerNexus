# Task003 - Tower Growth And Non-Elemental Upgrades

Status: Planned

Depends on: Task002 `Base Combat v0.1`

## 1. Goal

Calibrate L2/L3 Tower growth plus Basic and Behaviour Upgrades on top of the frozen Base Combat baseline.

Each growth choice should create visible value without erasing the owning TowerFamily's base identity.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`

## 3. In Scope

- L2 and L3 base-stat growth
- Required Tower Level gates
- Basic Upgrade deltas
- Behaviour Upgrade packages
- Per-Tower upgrade capacity and duplicate rules
- Upgrade-before and upgrade-after combat comparisons
- Reference Build cost accounting for non-Elemental Stages
- Regression against the four Base Tower identities

## 4. Out Of Scope

- Elemental Layer
- Elemental Buff stacking, Overload, and Protection
- Final Monster roster
- Player Progress Requirements
- Stage MonsterWaveConfig tuning

## 5. Calibration Sequence

1. Keep Task002 Map, Reference Monster, and Base Tower values fixed.
2. Validate L1-to-L2 and L2-to-L3 growth for each TowerFamily.
3. Calibrate Basic Upgrades one at a time.
4. Calibrate Behaviour Upgrades one package at a time.
5. Test supported combinations without changing the frozen base to repair one Upgrade.
6. Record expected gain, measured result, and keep/revise decision.
7. Recheck Stage1 Reference Build feasibility.

## 6. Required Measurements

- Pre-upgrade and post-upgrade TTK
- Change in range, cadence, uptime, or coverage
- Multi-target or persistent-entity gain where relevant
- Required Tower Level and Draft cost
- Whether the Upgrade creates a meaningful choice
- Whether the TowerFamily identity remains visible

## 7. Ownership

| Owner | Responsibility |
|---|---|
| TowerDefinition | Ordered Level data |
| Tower Upgrade System | Eligibility, application, and accepted upgrade state |
| Tower Runtime Combat | Runtime interpretation of accepted growth |
| Task003 | Comparative calibration and regression evidence |

## 8. Required Proposal Table

Codex prepares the first-pass table from the accepted Task002 baseline:

| Content | Current Value | Proposed Value | Intended Experience | Fixed Test | Observed Result | Decision |
|---|---:|---:|---|---|---|---|
| Tower level or Upgrade | Record from asset | First-pass estimate | Expected visible gain or specialization | Fixed Map, Monster, and placement | Filled after Play Mode | Keep or revise |

## 9. Execution Collaboration

- The user describes the intended growth or Upgrade feeling and owns Unity asset authoring plus Play Mode observation.
- Codex fills the first-pass Level, Basic, and Behaviour proposal table, predicts the expected change, and revises one value group at a time from the returned measurements.
- The user's Play Mode result, not the initial calculation alone, determines acceptance.

## 10. Unity Authoring Checklist

- Force one known Upgrade at a time for controlled comparison.
- Keep Map, Monster, placement, and simulation conditions fixed.
- Validate all four TowerFamilies at L1, L2, and L3.
- Validate each Basic and Behaviour definition.
- Validate package-capacity and duplicate rejection.
- Record every accepted authoring change.

## 11. Acceptance Criteria

- L2 and L3 create useful but understandable growth.
- Basic Upgrades produce coherent numerical specialization.
- Behaviour Upgrades change attack strategy visibly.
- No Upgrade is required merely to repair an unusable Base Tower.
- Stage1 approved Reference Build remains feasible.
- Task002 Base Tower identities survive the full regression.

## 12. Validation

- Fixed-condition A/B Play Mode runs
- Eligibility and application validation
- Combination regression
- Static asset validation
- Stage1 Reference Build regression

## 13. Review Note

Any proposed change to Task002 frozen values must be treated as a Base Combat revision and must rerun the complete Task002 comparison.
