# Task009 - Stage3 Calibration

Status: Planned; Stage3 Reference Build v0.1 is defined and execution waits for upstream implementation inputs

Depends on: Task008 Stage2 Calibration

## 1. Goal

Calibrate Stage3 so Magic's route-adjacent persistent contact and wider multi-zone coverage become necessary parts of a coherent build.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/03_PlayerSystem.md`
- `Doc/System/05_MapSystem.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`

## 3. Preconditions

The Stage Design Blueprint must approve the Stage3 Reference Build, Required Capability, Expected Anti-pattern, and Notes.

Task001 must provide an accepted Stage3 greybox with Reference placement and range/route evidence. Task006 must provide the derived Draft count, exact Draft pools, and Stage3 skeleton. A viable alternative is proposed and tested inside this Task.

First-pass inputs:

| Input | v0.1 Value |
|---|---|
| Map Size | 12x12 |
| Reference Towers | 4 |
| Core | One L3 Magic with one Basic and one Behaviour |
| Support | Three L1 Towers with no Upgrades; Archer and Cannon are required and the repeated family is resolved by Task006 |
| Total Draft Opportunities | 8 |
| Elemental | Not available |

## 4. In Scope

- Stage3 MonsterWaveConfig
- Magic contact-zone exposure
- Pressure across multiple useful route zones
- Monster composition and timing
- Local Stage3 Map refinement
- Reference, proposed-alternative, and Anti-pattern runs
- Stage1-Stage2 regression after any global revision

## 5. Out Of Scope

- Drone or Elemental tuning
- Per-Stage Progress Requirements
- Global Magic redesign solely for one Map
- Silent modification of the Stage Design Blueprint

## 6. Calibration Sequence

1. Fix the approved Reference Build and placement plan.
2. Confirm the Task006-derived Stage3 Draft count.
3. Tune Monster movement and Wave timing through Magic's intended contact zone.
4. Maintain meaningful pressure outside that local zone.
5. Test a Codex-proposed viable alternative.
6. Test the single-zone overconcentration Anti-pattern.
7. Regress earlier Stages after any global revision.

## 7. Required Measurements

- Magic contact duration and uptime
- Draft and Wave timing
- Peak Monsters and leaks
- Coverage outside the primary Magic zone
- Reference and alternative result
- Anti-pattern failure reason
- Final Player Health

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Approved Stage3 experience, Reference Build, required capability, and Anti-pattern |
| StageDefinition | Stage3 composition and maximum health |
| MonsterWaveConfig | Stage3 Monster order, count, and timing |
| Map Prefab | Stage3 contact zones and wider route geometry |
| Task009 | Derived Wave and alternative-build proposal, Stage-local calibration, and prior-Stage regression evidence |

## 9. Required Calibration Table

Codex prepares:

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Expected Draft Milestone | Magic Contact Test | Wider Coverage Test | Observed Result |
|---|---|---:|---:|---:|---|---|---|---|
| Proposed Wave | Proposed roster entry | First-pass count | First-pass timing | First-pass timing | Expected cumulative Draft | Intended contact exposure | Intended secondary-zone pressure | Filled after Play Mode |

## 10. Execution Collaboration

- The user supplies or approves the Stage3 experience and Reference Build, then owns Unity authoring and Play Mode observations.
- Codex derives the first Wave table and alternative Build from the Blueprint plus accepted upstream assets.
- Codex revises Map exposure or Wave pressure from measured Magic contact time, wider coverage, Draft timing, leaks, and final health.

## 11. Unity Authoring Checklist

- Confirm the Stage3 Blueprint contract and Task006-derived inputs.
- Assign the approved Stage3 Map, Wave, and Draft pools.
- Author the Stage3 MonsterWaveConfig.
- Place the Reference, alternative, and Anti-pattern builds.
- Record Magic contact time and wider route coverage.
- Record Draft timing, leaks, health, and Stage duration.
- Rerun Stage1-Stage2 after global changes.

## 12. Acceptance Criteria

- The Reference Build clears reliably.
- Magic creates recognizable route-adjacent value.
- The Stage requires more than one useful coverage zone.
- A coherent alternative remains viable.
- The Anti-pattern fails because of under-coverage.
- Stage1 and Stage2 remain accepted.

## 13. Validation

- Reference, alternative, and Anti-pattern Play Mode runs
- Contact-zone observation
- Map route validation
- Draft-count review
- Stage1-Stage2 regression

## 14. Review Note

Map geometry is the first repair lever when Magic contact opportunity is structurally absent.
