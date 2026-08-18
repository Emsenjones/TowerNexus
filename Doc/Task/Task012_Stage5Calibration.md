# Task012 - Stage5 Calibration

Status: Planned; Stage5 Reference Build v0.2 is defined and execution waits for Task007 Elemental inputs

Depends on: Task011 Stage4 Calibration

## 1. Goal

Calibrate Stage5 so the player can build a first Elemental core and understand Elemental application and stacking without requiring Stage6's dual-source cooperation.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/03_PlayerSystem.md`
- `Doc/System/05_MapSystem.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/System/15_BuffSystem.md`

## 3. Preconditions

The Stage Design Blueprint must approve:

- Reference Build
- Required Capability
- Expected Anti-pattern
- Stage Notes

Task001 must provide an accepted Stage5 greybox with Reference placement and range/route evidence. Task007 must provide the derived Draft count, cumulative all-family Elemental Upgrade pool, candidate-access evidence, and Stage5 skeleton. A viable alternative and the reproducible Reference Core family and ElementType are selected and recorded inside this Task.

First-pass inputs:

| Input | v0.2 Value |
|---|---|
| Map Size | 14x14 |
| Reference Towers | 5 |
| Core | One L3 Tower with one Basic, one Behaviour, and one Elemental |
| Support | Four L1 Towers with no Upgrades; all four TowerFamilies appear in the Reference roster |
| Total Draft Opportunities | 10 |
| Elemental Detail | All four TowerFamilies and all currently authored ElementTypes are available; the Reference Core family and ElementType are selected and recorded by Stage5 calibration |

## 4. In Scope

- Stage5 MonsterWaveConfig
- Timing at which the first Elemental core becomes available
- Sustained exposure for Elemental application and stacks
- Elemental Draft availability within the Task007-authored pool
- Monster composition, Profile-derived Spawn Interval, and Map-specific Wave Delay
- Local Stage5 Map refinement
- Reference, alternative, and Anti-pattern runs
- A secondary legal over-concentrated single-Tower coverage run when Task007 can construct it
- Prior-Stage regression

## 5. Out Of Scope

- Requiring two matching Elemental Towers
- Per-Stage Progress Requirements
- New Elemental lifecycle mechanics
- Silent modification of the Stage Design Blueprint or Task007-authored pool

## 6. Calibration Sequence

1. Fix the approved Reference Build and Elemental choice.
2. Confirm the Task007-derived Draft count and Elemental unlock timing.
3. Tune remaining Wave exposure and Map-specific Wave Delay after the Elemental core becomes active while preserving the Task006 standard spatial gap.
4. Confirm application and stacking are readable.
5. Test a Codex-proposed alternative Element or TowerFamily when allowed.
6. Test the primary no-Elemental-core Anti-pattern.
7. When legal, test one fully invested Elemental Tower without the Reference coverage/support structure as a secondary Anti-pattern.
8. Regress earlier Stages after global revisions.

## 7. Required Measurements

- Draft index and time when the Elemental core becomes active
- Successful applications and stacks
- Buff expiry before maximum stacks
- Wave timing and remaining combat after Elemental activation
- Leaks and final Player Health
- Reference, alternative, primary Anti-pattern, and secondary coverage result

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Approved Stage5 experience, Reference Build, required capability, and Anti-pattern |
| Task007 | Derived Draft count, Elemental pool, candidate access, and Stage skeleton |
| StageDefinition | Stage5 composition and maximum health |
| MonsterWaveConfig | Stage5 Monster order, count, and timing |
| Map Prefab | Stage5 Elemental exposure geometry |
| Buff and Effect Systems | Existing Elemental lifecycle behavior |
| Task012 | Derived Wave and alternative-build proposal, Stage-local calibration, and prior-Stage regression evidence |

## 9. Required Calibration Table

Codex prepares:

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Elemental Activation Milestone | Expected Stack Exposure | Observed Result |
|---|---|---:|---:|---:|---|---|---|
| Proposed Wave | Proposed roster entry | First-pass count | Task006 Profile-derived interval | Map-calibrated delay | Expected Draft index and time | Intended applications before resolution | Filled after Play Mode |

The proposal also records one coherent alternative Elemental Build and its expected margin.

## 10. Execution Collaboration

- The user supplies or approves the Stage5 experience and Reference Build, then owns Unity authoring and Play Mode Elemental-state observation.
- Codex derives the first Wave table and alternative Build from the Blueprint, Task007 pool, and accepted Elemental baseline.
- Codex revises Stage-local exposure, timing, or Monster pressure from activation time, stacks, leaks, health, and remaining combat.

## 11. Unity Authoring Checklist

- Confirm the Stage5 Blueprint contract and Task007-derived Elemental inputs.
- Assign the approved Stage5 Map, Wave, and Draft pools.
- Author the Stage5 MonsterWaveConfig.
- Place the Reference, alternative, no-Elemental-core, and legal single-Tower coverage builds.
- Record Elemental activation Draft and remaining combat.
- Record applications, stacks, leaks, health, and Stage duration.
- Rerun Stage1-Stage4 after global changes.

## 12. Acceptance Criteria

- The Reference Build clears reliably.
- One Elemental core is achievable within the approved Draft budget.
- Elemental application and stacking are understandable.
- Stage5 does not require the dual-source Stage6 solution.
- The Anti-pattern fails because it never reaches the required Elemental specialization.
- Stage5 does not punish having only one Elemental source; a secondary single-Tower build may fail only because it abandons required route coverage or support value.
- Stage1-Stage4 remain accepted.

## 13. Validation

- Reference, alternative, no-Elemental-core, and optional single-Tower coverage Play Mode runs
- Elemental state observation
- Draft-pool and eligibility validation
- Map exposure review
- Stage1-Stage4 regression

## 14. Review Note

Stage5 may produce Overload, but dual-source Overload remains the explicit Stage6 experience target.
