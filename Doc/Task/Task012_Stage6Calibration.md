# Task012 - Stage6 Calibration

Status: Planned; Stage6 Reference Build v0.1 is defined and execution waits for Task006 Elemental solvability inputs

Depends on: Task011 Stage5 Calibration

## 1. Goal

Calibrate Stage6 so the player must coordinate two Towers with the same ElementType and overlapping effective coverage to trigger Overload and clear the Stage.

TowerFamily may differ. Matching ElementType and shared-target opportunity are the required relationship.

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
- The non-negotiable requirement that at least one matching-Element build path remains achievable

Task001 must provide an accepted Stage6 greybox with Reference placement, range/route evidence, and matching-Element overlap geometry. Task006 must provide the derived Draft count, exact Elemental Upgrade pool, candidate-solvability rule, and Stage6 skeleton. A viable alternative is proposed and tested inside this Task.

First-pass inputs:

| Input | v0.1 Value |
|---|---|
| Map Size | 14x14 |
| Reference Towers | 5 |
| Core | Two L3 Towers, each with one Basic, one Behaviour, and one matching Elemental |
| Support | Three L1 Towers with no Upgrades; all four TowerFamilies appear in the complete five-Tower Reference roster |
| Total Draft Opportunities | 15 |
| Elemental Detail | The two Cores share one ElementType; exact Core families and ElementType are resolved by Task006 |

## 4. In Scope

- Stage6 MonsterWaveConfig
- Matching-Element pair timing
- Shared-target overlap geometry
- Overload-required pressure
- Candidate availability under the Task006-authored Draft pool and solvability rule
- Monster composition and timing
- Local Stage6 Map refinement
- Reference, alternative, and Anti-pattern runs
- Full Stage1-Stage5 regression after global revisions

## 5. Out Of Scope

- Requiring matching TowerFamily
- Per-Stage Progress Requirements
- New reroll or guarantee systems without a separate reviewed contract
- Silent modification of Elemental candidate rules

## 6. Calibration Sequence

1. Fix the Blueprint-approved matching-Element Reference Build.
2. Confirm when both Elemental sources become active.
3. Preserve enough remaining combat for repeated shared-stack attempts.
4. Tune Map overlap and Wave exposure before changing global Buff values.
5. Confirm at least one required Overload.
6. Test a Codex-proposed different-TowerFamily matching pair when permitted.
7. Test the one-Elemental-Core, mismatched-Element, and non-overlapping Anti-patterns.
8. Regress all earlier Stages after any global revision.

## 7. Required Measurements

- Draft index and time when the pair becomes active
- Time from first shared application to Overload
- Successful stacks and cooldown-blocked applications
- Overloads per run
- Remaining combat after pair completion
- Leaks and final Player Health
- Reference, alternative, one-Core, mismatched, and non-overlap results

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Approved Stage6 experience, Reference Build, required capability, Anti-pattern, and solvability constraint |
| Task006 | Derived Draft count, Elemental pool, candidate-solvability rule, and Stage skeleton |
| StageDefinition | Stage6 composition and maximum health |
| MonsterWaveConfig | Stage6 Monster order, count, and timing |
| Map Prefab | Matching-pair overlap geometry |
| Buff and Effect Systems | Existing shared-stack, Overload, and Protection behavior |
| Task012 | Derived Wave and alternative-build proposal, final Stage calibration, and full regression evidence |

## 9. Required Calibration Table

Codex prepares:

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Matching Pair Milestone | Expected Overload Window | Observed Result |
|---|---|---:|---:|---:|---|---|---|
| Proposed Wave | Proposed roster entry | First-pass count | First-pass timing | First-pass timing | Expected Draft index and time | Intended shared-stack and Overload opportunities | Filled after Play Mode |

The proposal also records one alternative matching-Element pair, the candidate path that makes it obtainable, and the one-Core, mismatched, and non-overlap Anti-pattern expectations.

## 10. Execution Collaboration

- The user supplies or approves the Stage6 experience and Reference Build, then owns Unity authoring and Play Mode Overload observation.
- Codex derives the first Wave table, candidate-path check, and alternative matching pair from the Blueprint, Task006 pool, and accepted Elemental baseline.
- Codex revises Stage-local overlap, exposure, timing, or Monster pressure from pair activation, stack timing, Overloads, leaks, health, and remaining combat.

## 11. Unity Authoring Checklist

- Confirm the Stage6 Blueprint contract and Task006-derived Elemental solvability inputs.
- Assign the approved Stage6 Map, Wave, and Draft pools.
- Author the Stage6 MonsterWaveConfig.
- Place the matching-Element Reference and alternative builds.
- Run one-Elemental-Core, mismatched-Element, and non-overlap Anti-patterns.
- Record pair activation, stack timing, Overloads, leaks, health, and Stage duration.
- Rerun Stage1-Stage5 after global changes.

## 12. Acceptance Criteria

- The Reference Build can form a matching-Element pair within the Task006-derived Draft structure.
- The pair has meaningful overlapping coverage.
- Overload is required and observable.
- Different TowerFamilies may cooperate through the same ElementType.
- One fully developed Elemental Core is insufficient because Stage6 requires a second matching source, not because the first Core receives a hidden penalty.
- Mismatched Elements or separated matching Towers fail for the intended reason.
- Candidate generation does not silently remove every valid solution path under the Task006 solvability rule.
- Stage1-Stage5 remain accepted.

## 13. Validation

- Matching-pair Reference Run
- Different-family matching-pair run
- One-Elemental-Core Anti-pattern run
- Mismatched-Element Anti-pattern run
- Non-overlap Anti-pattern run
- Elemental lifecycle observation
- Full prior-Stage regression

## 14. Review Note

If the Stage is unwinnable because no valid matching candidate route appears, return to Task006 candidate solvability rather than compensating through Wave weakness.
