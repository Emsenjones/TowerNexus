# Task008 - Stage1 Calibration

Status: Planned; Stage1 Reference Build v0.1 and both capability Anti-patterns are defined, while execution waits for Task007 inputs

Depends on: Task007 Global Progression And Stage Skeleton

## 1. Goal

Calibrate Stage1 so its approved Reference Build clears reliably while both legal extremes fail for their intended reasons: over-expanding into undeveloped Level 1 Archers, and over-concentrating every available investment into one Archer while omitting the required second coverage point.

Stage1 establishes the first complete Stage calibration method used by later Stage Tasks.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/03_PlayerSystem.md`
- `Doc/System/05_MapSystem.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/09_TowerPlacementSystem.md`

## 3. Derived Stage Inputs

| Input | Value | Source |
|---|---|---|
| Implementation Map Size | 8x8 | Task001 v0.2 |
| Derived Total Draft | 6 | Reference Build cost, formalized by Task007 |
| Reference Build | Two Archers; one L3 with one Basic and one Behaviour, one L1 | Stage Design Blueprint v0.1 |
| Required Capability | Concentrated growth plus a second coverage point | Stage Design Blueprint |
| Anti-patterns | Over-expand with undeveloped L1 Archers; or fully concentrate into one Archer without the second coverage point | Stage Design Blueprint |

## 4. In Scope

- Stage1 MonsterWaveConfig
- Monster identities, counts, and order
- First Wave delay, Wave delay, and Spawn Interval
- Stage1 maximum health if required
- Local Stage1 route or placement refinement
- Reference Build and both legal Anti-pattern runs
- Stage duration and Draft timing

## 5. Out Of Scope

- Global Tower, Monster, Upgrade, Effect, or Buff redesign
- Per-Stage Progress Requirements
- Stage2-Stage6 tuning
- New runtime systems

## 6. Calibration Sequence

1. Fix the approved Stage1 Reference Build and placements.
2. Confirm the Task007 skeleton provides exactly six total Draft opportunities.
3. Tune Monster order, count, delay, and interval.
4. Preserve combat after the sixth Draft.
5. Confirm the Reference Build clears with deliberate margin.
6. Run the legal L1 Archer over-expansion Anti-pattern.
7. Run the Task007-confirmed legal single-Archer concentration Anti-pattern using the same total Draft budget.
8. Adjust Stage-local Wave or Map pressure one axis at a time.
9. Record the accepted Stage1 baseline.

## 7. Required Measurements

- Draft resolution node and opening time
- Wave start and end
- Peak simultaneous Monsters
- Monster leaks
- Final Player Health
- Last Draft to final resolution time
- Reference Build result
- Each Anti-pattern's failure point and capability gap

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Approved Stage1 experience, Reference Build, required capability, and Anti-pattern |
| StageDefinition | Stage1 composition and maximum health |
| MonsterWaveConfig | Stage1 Monster order, count, and timing |
| Map Prefab | Stage1 route and placement geometry |
| Task008 | Derived Wave proposal, Stage-local calibration evidence, and accepted result |

## 9. Required Calibration Table

Codex prepares the first Stage calibration table:

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Expected Draft Milestone | Expected Pressure | Observed Result |
|---|---|---:|---:|---:|---|---|---|
| Proposed Wave | Proposed roster entry | First-pass count | First-pass timing | First-pass timing | Expected cumulative Draft | Intended Reference Build test | Filled after Play Mode |

## 10. Execution Collaboration

- The user owns Unity authoring, fixed Reference and Anti-pattern runs, and reporting Draft timing, leaks, final health, and Stage duration.
- Codex fills the first MonsterWaveConfig table from the Blueprint, Task007 Draft skeleton, accepted Map, and global combat baselines.
- Codex compares the observed run with the expected result and changes one Stage-local pressure axis at a time.

## 11. Unity Authoring Checklist

- Assign the approved Stage1 Map, Wave, and Draft pools.
- Author the Stage1 MonsterWaveConfig.
- Place the fixed Reference Build.
- Record every Draft opportunity and its timing.
- Run the Reference Build and both legal Anti-patterns.
- Record final health, leaks, Stage duration, and failure reason.
- Revalidate the route after any Map change.

## 12. Acceptance Criteria

- Stage1 grants exactly six total Drafts.
- The approved Reference Build clears consistently.
- The legal L1 Archer over-expansion Anti-pattern fails because of insufficient developed output.
- The legal single-Archer concentration Anti-pattern fails because it omits the required second coverage point, not because concentrated investment receives a hidden penalty.
- Failure is not caused by an invalid route or missing placement.
- The final Draft has enough remaining combat to matter.
- No global baseline is changed solely to repair Stage1.

## 13. Validation

- Reference Build Play Mode runs
- Over-expansion and single-Archer concentration Play Mode runs
- Draft-count and timing review
- Map route validation
- StageDefinition and MonsterWaveConfig validation

## 14. Review Note

Any accepted global-value revision returns to its owning Task and requires a fresh Stage1 regression before Stage2 calibration begins.
