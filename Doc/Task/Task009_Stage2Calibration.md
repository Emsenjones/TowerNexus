# Task009 - Stage2 Calibration

Status: Planned; Stage2 Reference Build v0.2 is defined and execution waits for upstream implementation inputs

Depends on: Task008 Stage1 Calibration

## 1. Goal

Calibrate Stage2 so Archer and Cannon role complement produces the intended clear capability while both unfocused horizontal growth and one-Tower over-concentration without role complement fail.

Stage2 introduces a second TowerFamily without erasing the Stage1 lesson of concentrated investment.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/03_PlayerSystem.md`
- `Doc/System/05_MapSystem.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`

## 3. Preconditions

The Stage Design Blueprint must approve the Stage2 Reference Build, Required Capability, Expected Anti-pattern, and Notes.

Task001 must provide an accepted Stage2 greybox with Reference placement and range/route evidence. Task007 must provide the derived Draft count, exact Draft pools, and Stage2 skeleton. A viable alternative is proposed and tested inside this Task rather than approved in the Blueprint.

First-pass inputs:

| Input | v0.2 Value |
|---|---|
| Map Size | 10x10 |
| Reference Towers | 3 |
| Core | One L2 Cannon with one Basic and one Behaviour |
| Support | Two L1 Towers with no Upgrades; Archer is required and the repeated family is selected and recorded by Stage2 calibration |
| Total Draft Opportunities | 6 |
| Elemental | Not available |

## 4. In Scope

- Stage2 MonsterWaveConfig
- Archer/Cannon exposure and role complement
- Monster identities, counts, and order
- Profile-derived Spawn Interval and Map-specific Wave Delay
- Local Stage2 Map refinement
- Reference, proposed-alternative, and Anti-pattern runs
- A Task007-confirmed legal one-Tower concentration run
- Stage1 regression after any global revision

## 5. Out Of Scope

- New Tower or Monster mechanics
- Per-Stage Progress Requirements
- Magic, Drone, or Elemental calibration
- Silent modification of the Stage Design Blueprint

## 6. Calibration Sequence

1. Fix the approved Stage2 Reference Build and placements.
2. Confirm the Task007-derived Draft count.
3. Tune Wave content and Map-specific Wave Delay to expose range and cadence complement while preserving the Task006 standard spatial gap.
4. Let Codex propose and test at least one coherent alternative build.
5. Test the approved undeveloped horizontal-growth Anti-pattern.
6. Test a legal one-Tower concentration that omits the Archer/Cannon complement.
7. Adjust one Stage-local pressure axis at a time.
8. Rerun Stage1 if any global value changes.

## 7. Required Measurements

- Draft timing
- Tower coverage by family
- Peak simultaneous Monsters
- Leaks and final Player Health
- Reference and alternative build result
- Each Anti-pattern's failure reason and missing role
- Last Draft to Stage end

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Approved Stage2 experience, Reference Build, required capability, and Anti-pattern |
| StageDefinition | Stage2 composition and maximum health |
| MonsterWaveConfig | Stage2 Monster order, count, and timing |
| Map Prefab | Stage2 route and placement geometry |
| Task009 | Derived Wave and alternative-build proposal, Stage-local calibration, and Stage1 regression evidence |

## 9. Required Calibration Table

Codex prepares:

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Expected Draft Milestone | Archer/Cannon Test | Observed Result |
|---|---|---:|---:|---:|---|---|---|
| Proposed Wave | Proposed roster entry | First-pass count | Task006 Profile-derived interval | Map-calibrated delay | Expected cumulative Draft | Intended role-complement pressure | Filled after Play Mode |

The same proposal records one coherent alternative Build and its expected margin.

## 10. Execution Collaboration

- The user supplies or approves the desired Stage2 experience and Reference Build, then owns Unity authoring and Play Mode observations.
- Codex derives the first Wave table and alternative Build from the Blueprint plus accepted upstream assets.
- Codex revises Stage-local Wave or Map pressure from measured Draft timing, coverage, leaks, health, and failure reason.

## 11. Unity Authoring Checklist

- Confirm the Stage2 Blueprint contract and Task007-derived inputs.
- Assign the approved Stage2 Map, Wave, and Draft pools.
- Author the Stage2 MonsterWaveConfig.
- Place the Reference, alternative, horizontal-growth, and one-Tower concentration builds.
- Record Draft timing, leaks, health, and Stage duration.
- Revalidate route and placement after Map changes.
- Rerun Stage1 after global changes.

## 12. Acceptance Criteria

- Stage2 reaches its Task007-derived Draft count.
- The Reference Build clears reliably.
- Archer and Cannon have understandable complementary value.
- At least one viable alternative exists.
- Every standard Wave preserves the Task006 reference spatial gap.
- The Anti-pattern fails for the intended capability gap.
- Concentrated investment receives no hidden penalty; its test fails only when the missing TowerFamily role matters.
- Stage1 remains accepted.

## 13. Validation

- Reference, alternative, horizontal-growth, and one-Tower concentration Play Mode runs
- Stage1 regression
- Draft-count review
- Map route validation
- StageDefinition and MonsterWaveConfig validation

## 14. Review Note

Task009 is expected to propose Stage2 numeric values. They remain provisional until the user's fixed Unity runs confirm the intended Blueprint experience.
