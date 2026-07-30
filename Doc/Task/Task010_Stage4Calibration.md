# Task010 - Stage4 Calibration

Status: Planned; Stage4 Reference Build v0.1 is defined and execution waits for upstream implementation inputs

Depends on: Task009 Stage3 Calibration

## 1. Goal

Calibrate Stage4 so Drone pursuit answers expanded route pressure and larger placement footprints without making fixed Towers irrelevant.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/03_PlayerSystem.md`
- `Doc/System/05_MapSystem.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/09_TowerPlacementSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`

## 3. Preconditions

The Stage Design Blueprint must approve the Stage4 Reference Build, Required Capability, Expected Anti-pattern, and Notes.

Task001 must provide an accepted Stage4 greybox with Reference placement and range/route evidence. Task006 must provide the derived Draft count, exact Draft pools, and Stage4 skeleton. A viable alternative is proposed and tested inside this Task.

First-pass inputs:

| Input | v0.1 Value |
|---|---|
| Map Size | 14x14 |
| Reference Towers | 5 |
| Core | One L3 Drone with one Basic and one Behaviour |
| Support | Four L1 Towers with no Upgrades; Archer, Cannon, and Magic are required and the repeated family is resolved by Task006 |
| Total Draft Opportunities | 9 |
| Elemental | Not available |

## 4. In Scope

- Stage4 MonsterWaveConfig
- Drone launch, pursuit, battery, and coverage opportunity
- Expanded route pressure
- Large-footprint placement usability
- Monster composition and timing
- Local Stage4 Map refinement
- Reference, proposed-alternative, and Anti-pattern runs
- Prior-Stage regression

## 5. Out Of Scope

- Elemental tuning
- Per-Stage Progress Requirements
- Global Drone redesign solely for Stage4
- New placement mechanics

## 6. Calibration Sequence

1. Fix the approved Reference Build and legal placements.
2. Confirm Task001 Reference and reasonable alternative placements against Task006's exact pools.
3. Tune route exposure and Wave timing for meaningful Drone pursuit.
4. Preserve useful roles for Archer, Cannon, and Magic.
5. Test a Codex-proposed viable alternative.
6. Test the local-only fixed-firepower Anti-pattern.
7. Regress Stage1-Stage3 after any global revision.

## 7. Required Measurements

- Drone active time and pursuit coverage
- Large-footprint placement availability
- Draft and Wave timing
- Peak Monsters, leaks, and final Player Health
- Reference and alternative result
- Anti-pattern failure reason

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Approved Stage4 experience, Reference Build, required capability, and Anti-pattern |
| StageDefinition | Stage4 composition and maximum health |
| MonsterWaveConfig | Stage4 Monster order, count, and timing |
| Map Prefab | Stage4 route and large-footprint placement geometry |
| Task010 | Derived Wave and alternative-build proposal, Stage-local calibration, and prior-Stage regression evidence |

## 9. Required Calibration Table

Codex prepares:

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Expected Draft Milestone | Drone Pursuit Test | Fixed-Tower Test | Observed Result |
|---|---|---:|---:|---:|---|---|---|---|
| Proposed Wave | Proposed roster entry | First-pass count | First-pass timing | First-pass timing | Expected cumulative Draft | Intended expanded-route pressure | Expected retained fixed-Tower value | Filled after Play Mode |

## 10. Execution Collaboration

- The user supplies or approves the Stage4 experience and Reference Build, then owns Unity authoring and Play Mode observations.
- Codex derives the first Wave table and alternative Build from the Blueprint plus accepted upstream assets.
- Codex revises Stage-local route exposure or Wave pressure from Drone active time, placement usability, Draft timing, leaks, and final health.

## 11. Unity Authoring Checklist

- Confirm the Stage4 Blueprint contract and Task001/Task006-derived inputs.
- Assign the approved Stage4 Map, Wave, and Draft pools.
- Author the Stage4 MonsterWaveConfig.
- Place the Reference, alternative, and Anti-pattern builds.
- Record Drone launch, pursuit, and active time.
- Validate large-footprint placement and route preservation.
- Rerun Stage1-Stage3 after global changes.

## 12. Acceptance Criteria

- The Reference Build clears reliably.
- Drone pursuit solves a recognizable long-map problem.
- Fixed Towers retain meaningful value.
- Large footprints remain legally deployable.
- The Anti-pattern fails because of insufficient spatial coverage.
- Stage1-Stage3 remain accepted.

## 13. Validation

- Reference, alternative, and Anti-pattern Play Mode runs
- Drone lifecycle observation
- Large-footprint Reference and alternative placement validation
- Draft-count review
- Stage1-Stage3 regression

## 14. Review Note

If Drone is required only because fixed-Tower placement is invalid, repair the Map before increasing Drone power.
