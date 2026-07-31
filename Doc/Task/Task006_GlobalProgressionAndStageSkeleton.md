# Task006 - Global Progression And Stage Skeleton

Status: Planned; v0.1 Draft inputs are derived, while execution still depends on accepted Task001-Task005 baselines

Depends on: Task001-Task005

## 1. Goal

Translate the six approved Stage Reference Builds into one shared Player progression axis and six complete StageDefinition content skeletons ready for individual calibration.

This Task derives Draft budgets, exact Draft pools, Progress requirements, and structural Wave budgets. It does not finalize Stage difficulty or add those implementation numbers back into the Stage Design Blueprint.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/03_PlayerSystem.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`

## 3. Preconditions

The Stage Design Blueprint must approve for every Stage:

- Reference Build
- New Content
- Required capability
- Expected Anti-pattern
- Any non-negotiable Stage Notes

Task001-Task005 must provide accepted Map and Stage-independent combat baselines. Reference Build Draft costs and total Draft opportunities are already derivable from Blueprint v0.1. Exact Draft pools, Progress nodes, Monster budgets, repeated support families, UpgradeDefinitions, ElementTypes, and candidate rules remain outputs of this Task.

## 4. Required Derived Planning Table

Before Unity authoring, Codex prepares one reviewable row per Stage containing:

| Derived Field | Derivation |
|---|---|
| Reference Build Draft Cost | Count deployments, Level-ups, and applied Upgrades in the Reference Build |
| Total Draft Opportunities | Match the Reference Build Draft cost |
| Required Player Level-Ups | Total Draft Opportunities minus the Initial Draft |
| Exact Tower Draft Pool | Derive from the campaign unlock line and intended Stage build space |
| Exact Tower Upgrade Draft Pool | Derive from required build access and Stage lesson |
| Rough Monster Resolutions | Solve against the shared Progress sequence |
| Last-Draft Combat Reserve | Preserve enough combat for the final choice to affect the result |
| Candidate Solvability Rule | Add only when a required build could otherwise become unavailable through candidate generation |

The table begins as a proposed numeric pass. It becomes accepted only after structural validation and user Play Mode evidence.

Current Blueprint-derived v0.1 inputs:

| Stage | Reference Towers | Core Towers | Reference Build Draft Cost | Total Draft Opportunities | Required Player Level-Ups After Initial Draft | Elemental Build Requirement |
|---|---:|---:|---:|---:|---:|---|
| Stage1 | 2 | 1 | 6 | 6 | 5 | None |
| Stage2 | 3 | 1 | 7 | 7 | 6 | None |
| Stage3 | 4 | 1 | 8 | 8 | 7 | None |
| Stage4 | 5 | 1 | 9 | 9 | 8 | None |
| Stage5 | 5 | 1 | 10 | 10 | 9 | One Elemental Core |
| Stage6 | 5 | 2 | 15 | 15 | 14 | Two overlapping Cores with the same ElementType |

Stage6 costs 15 Drafts: five Tower deployments plus two Cores, each requiring two Level-ups, one Basic Upgrade, one Behaviour Upgrade, and one Elemental Upgrade.

Stage4-Stage6 intentionally share five Reference Towers and the same proposed Map scale, but their Total Draft Opportunities remain 9, 10, and 15 because their Reference Builds require different vertical and Elemental investment.

These values are the first trajectory estimate. Task006 does not treat the Progress sequence, Monster totals, Draft pools, or candidate availability as accepted until the derived table has been structurally tested.

## 5. In Scope

- Reference Build Draft-cost calculation
- Exact Tower and Tower Upgrade Draft pools
- Rough total Monster-resolution budget per Stage
- One global `progressRequiredPerLevel` sequence
- Cumulative Draft nodes
- Stage1-Stage6 StageDefinitions
- Map references
- MonsterWaveConfig references and initial Wave skeletons
- Stage maximum-health authoring
- Post-final-Draft validation segment targets
- Candidate solvability where required by the Blueprint
- Revalidation of Task001 Reference and reasonable alternative placements against exact Draft pools

## 6. Out Of Scope

- Final Spawn Interval, Wave Delay, or Stage duration
- Final Monster composition
- Final local Map route refinement
- Rebalancing frozen Tower, Monster, Effect, or Buff baselines
- Writing derived implementation values back into the Stage Design Blueprint

## 7. Progression Contract

```text
Total Draft Opportunities
    = One Initial Tower Draft
    + Player Level-Up Drafts

Required Player Level-Ups
    = Reference Build Draft Cost - 1
```

All six Stages use one ordered Progress Requirement sequence. Stage differences come from Monster totals, Wave content, Draft pools, and Map pressure rather than per-Stage Progress Requirement forks.

## 8. Authoring Sequence

1. Reconfirm the v0.1 Draft cost for all six approved Reference Builds.
2. Propose exact Tower and Tower Upgrade Draft pools.
3. Check that every required Reference Build remains obtainable.
4. Choose rough Monster-resolution totals.
5. Solve one positive global Progress Requirement sequence.
6. Verify cumulative Draft nodes against every Stage.
7. Author six StageDefinitions and six MonsterWaveConfig skeletons.
8. Assign Map, Tower pool, Upgrade pool, and Stage maximum health.
9. Reserve combat after the last intended Draft.
10. Revalidate Task001 Reference and reasonable alternative placements against the exact pools.
11. Validate each Stage composition without final difficulty claims.

## 9. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Reference Build, Stage lesson, required capability, Anti-pattern, and non-negotiable constraints |
| Task006 | Derived planning table, cross-Stage constraint solving, and structural validation |
| Player System | One shared Progress Requirement sequence |
| StageDefinition | Map, Wave, Draft pools, and maximum health composition |
| MonsterWaveConfig | Ordered Wave skeleton and rough Monster totals |
| Draft System | Candidate generation according to the authored pools and approved solvability contract |

## 10. Execution Collaboration

- The user supplies or approves the intended Stage experience and Reference Builds.
- Codex calculates the first complete Draft, pool, Progress, Monster-budget, and candidate-solvability table from those inputs.
- The user authors or confirms the proposed values in Unity and performs the structural Play Mode smoke test.
- Codex compares observed Draft timing and counts against the table, then proposes the smallest next revision.
- No proposed value is treated as accepted before the corresponding Unity validation.

## 11. Unity Authoring Checklist

- Review the six-row derived planning table.
- Author the accepted global Progress Requirement sequence.
- Create or update Stage1-Stage6 StageDefinitions.
- Assign the intended Map and MonsterWaveConfig to each Stage.
- Assign the reviewed Tower and Tower Upgrade pools.
- Author positive Stage maximum health.
- Create structurally valid Wave content with the rough Monster totals.
- Verify the Initial Draft and first-Wave gate.
- Record cumulative resolution nodes and observed Draft counts.
- Recheck Reference and reasonable alternative placements when an exact pool changes the available Tower set.

## 12. Acceptance Criteria

- One reviewed derived planning table covers all six Stages.
- One global Progress Requirement sequence exists and validates.
- Every Stage skeleton reaches its derived Draft count in resolution-count space.
- Six StageDefinitions reference the intended Maps, Waves, and Draft pools.
- Every approved Reference Build is obtainable under its authored Draft structure.
- Stage6 retains at least one achievable matching-Element build path if Overload is mandatory.
- Every fresh Stage begins with one Initial Tower Draft.
- Every Stage retains a post-final-Draft validation segment.
- No Stage-specific Progress Requirement fork exists.

## 13. Validation

- Reference Build cost review
- Draft-pool and candidate-solvability review
- Cumulative Draft-node calculation review
- Task001 Reference-placement and range/route regression
- StageDefinition validation
- MonsterWaveConfig structural validation
- Initial Draft and first-Wave gate check
- Progress and Draft-count Play Mode smoke test
- Static asset-reference review

## 14. Review Note

Task007-Task012 own final Stage experience. If one Stage cannot satisfy its Blueprint without changing a global baseline, return the proposed revision to the owning earlier Task and rerun affected regressions.
