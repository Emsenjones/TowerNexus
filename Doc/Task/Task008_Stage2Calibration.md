# Task008 - Stage2 Calibration

Status: Planned; Stage2 Reference Build v0.1 is defined and execution waits for upstream implementation inputs

Depends on: Task007 Stage1 Calibration

## 1. Goal

Calibrate Stage2 so Archer and Cannon role complement produces the intended clear capability while unfocused horizontal growth fails.

Stage2 introduces a second TowerFamily without erasing the Stage1 lesson of concentrated investment.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/03_PlayerSystem.md`
- `Doc/System/05_MapSystem.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`

## 3. Preconditions

The Stage Design Blueprint must approve the Stage2 Reference Build, Required Capability, Expected Anti-pattern, and Notes.

Task001 must provide an accepted Stage2 greybox with Reference placement and range/route evidence. Task006 must provide the derived Draft count, exact Draft pools, and Stage2 skeleton. A viable alternative is proposed and tested inside this Task rather than approved in the Blueprint.

First-pass inputs:

| Input | v0.1 Value |
|---|---|
| Map Size | 10x10 |
| Reference Towers | 3 |
| Core | One L3 Cannon with one Basic and one Behaviour |
| Support | Two L1 Towers with no Upgrades; Archer is required and the repeated family is resolved by Task006 |
| Total Draft Opportunities | 7 |
| Elemental | Not available |

## 4. In Scope

- Stage2 MonsterWaveConfig
- Archer/Cannon exposure and role complement
- Monster identities, counts, and order
- Wave and Spawn timing
- Local Stage2 Map refinement
- Reference, proposed-alternative, and Anti-pattern runs
- Stage1 regression after any global revision

## 5. Out Of Scope

- New Tower or Monster mechanics
- Per-Stage Progress Requirements
- Magic, Drone, or Elemental calibration
- Silent modification of the Stage Design Blueprint

## 6. Calibration Sequence

1. Fix the approved Stage2 Reference Build and placements.
2. Confirm the Task006-derived Draft count.
3. Tune Wave content to expose range and cadence complement.
4. Let Codex propose and test at least one coherent alternative build.
5. Test the approved Anti-pattern.
6. Adjust one Stage-local pressure axis at a time.
7. Rerun Stage1 if any global value changes.

## 7. Required Measurements

- Draft timing
- Tower coverage by family
- Peak simultaneous Monsters
- Leaks and final Player Health
- Reference and alternative build result
- Anti-pattern failure reason
- Last Draft to Stage end

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Approved Stage2 experience, Reference Build, required capability, and Anti-pattern |
| StageDefinition | Stage2 composition and maximum health |
| MonsterWaveConfig | Stage2 Monster order, count, and timing |
| Map Prefab | Stage2 route and placement geometry |
| Task008 | Derived Wave and alternative-build proposal, Stage-local calibration, and Stage1 regression evidence |

## 9. Required Calibration Table

Codex prepares:

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Expected Draft Milestone | Archer/Cannon Test | Observed Result |
|---|---|---:|---:|---:|---|---|---|
| Proposed Wave | Proposed roster entry | First-pass count | First-pass timing | First-pass timing | Expected cumulative Draft | Intended role-complement pressure | Filled after Play Mode |

The same proposal records one coherent alternative Build and its expected margin.

## 10. Execution Collaboration

- The user supplies or approves the desired Stage2 experience and Reference Build, then owns Unity authoring and Play Mode observations.
- Codex derives the first Wave table and alternative Build from the Blueprint plus accepted upstream assets.
- Codex revises Stage-local Wave or Map pressure from measured Draft timing, coverage, leaks, health, and failure reason.

## 11. Unity Authoring Checklist

- Confirm the Stage2 Blueprint contract and Task006-derived inputs.
- Assign the approved Stage2 Map, Wave, and Draft pools.
- Author the Stage2 MonsterWaveConfig.
- Place the Reference, alternative, and Anti-pattern builds.
- Record Draft timing, leaks, health, and Stage duration.
- Revalidate route and placement after Map changes.
- Rerun Stage1 after global changes.

## 12. Acceptance Criteria

- Stage2 reaches its Task006-derived Draft count.
- The Reference Build clears reliably.
- Archer and Cannon have understandable complementary value.
- At least one viable alternative exists.
- The Anti-pattern fails for the intended capability gap.
- Stage1 remains accepted.

## 13. Validation

- Reference, alternative, and Anti-pattern Play Mode runs
- Stage1 regression
- Draft-count review
- Map route validation
- StageDefinition and MonsterWaveConfig validation

## 14. Review Note

Task008 is expected to propose Stage2 numeric values. They remain provisional until the user's fixed Unity runs confirm the intended Blueprint experience.
