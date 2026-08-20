# Task009 - Stage1 Calibration

Status: In Progress; Candidate v0.1 authors the first Stage1 pacing/Wave Delay hypothesis and begins Reference/Alternative/Anti-pattern evidence collection

Depends on: Task008 Global Progression And Stage Skeleton regression

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
| Derived Total Draft | 5 | Reference Build cost, formalized by Task008 |
| Reference Build | Two Archers; one L2 with one Basic and one Behaviour, one L1; exact Upgrade identities are not mandatory player answers | Stage Design Blueprint v0.2 |
| Required Capability | Concentrated growth plus a second coverage point | Stage Design Blueprint |
| Anti-patterns | Over-expand with undeveloped L1 Archers; or fully concentrate into one Archer without the second coverage point | Stage Design Blueprint |

## 4. In Scope

- Stage1 MonsterWaveConfig
- Monster identities, counts, and order
- First Wave delay, Wave Delay, and Task007-derived Spawn Interval
- Stage1 maximum health if required
- Local Stage1 route or placement refinement
- Reference Build and both legal Anti-pattern runs
- Selection and recording of one reproducible Reference Basic/Behaviour pair without making that pair a mandatory player answer
- At least one legal non-Reference Upgrade combination or adaptive Build check
- Stage duration and Draft timing

## 5. Out Of Scope

- Global Tower, Monster, Upgrade, Effect, or Buff redesign
- Per-Stage Progress Requirements
- Stage2-Stage6 tuning
- New runtime systems

## 6. Calibration Sequence

1. Fix the approved structural Stage1 Reference Build and placements, then record the exact Basic and Behaviour identities used by the reproducible run.
2. Confirm the Task008 skeleton provides exactly five total Draft opportunities after its pacing regression.
3. Tune Monster order, Count, and Map-specific Wave Delay while deriving Spawn Interval from the Task007 standard spatial gap.
4. Preserve combat after the fifth Draft.
5. Confirm the Reference Build clears with deliberate margin without treating its exact Upgrade identities as the only valid answer.
6. Run the legal L1 Archer over-expansion Anti-pattern.
7. Run the Task008-confirmed legal single-Archer concentration Anti-pattern using the same total Draft budget.
8. Adjust Stage-local Wave or Map pressure one axis at a time.
9. Record the accepted Stage1 baseline.

### 6.1 Candidate v0.1 Wave Table

`Wave Delay` is schedule-owned: each value is measured before that Wave begins spawning. For Wave 2 onward, the delay begins after the previous Wave has finished spawning; it does not wait for all Monsters from the previous Wave to Resolve.

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Cumulative Monster Count | Intended Pressure |
|---|---|---:|---:|---:|---:|---|
| 1 | Fodder | 3 | 2.5s | 4s | 3 | Low-pressure Initial Tower check and first Level-Up Draft |
| 2 | Fodder | 3 | 2.5s | 10s | 6 | Preserve a forgiving second coverage/build decision |
| 3 | Normal | 4 | 2.5s | 10s | 10 | First HP pressure step after the opening |
| 4 | Normal | 4 | 2.5s | 10s | 14 | Final Draft acquisition under established pressure |
| 5 | Normal | 8 | 2.5s | 10s | 22 | Post-final-Draft capability validation |

All Spawn Intervals preserve the Task007 centerline spatial-gap baseline of `0.625` for Move Speed `0.25`. Counts and Wave boundaries intentionally align with cumulative Progress nodes `3 / 6 / 10 / 14 / 22`; runtime Draft timing is still determined by Monster resolution rather than spawn completion.

Candidate v0.1 is a measurement fixture, not an accepted Stage1 baseline. In particular, it must reveal whether five undeveloped L1 Archers actually underperform the two-Tower Reference Build. If the horizontal Anti-pattern has structurally higher realized output, Stage-local Wave tuning alone must not be used to manufacture a misleading acceptance.

### 6.2 Candidate v0.1 Build Scripts

| Run | Five-Draft Final Build | Expected Use |
|---|---|---|
| Reference | Two Archers; Core L2 with Quick Draw and Scatter Arrow; Support L1 | Reproducible primary calibration control |
| Coherent Alternative | Two Archers; Core L2 with Sharpened Arrows and Piercing Arrow; Support L1 | Confirm the Stage does not require one named Upgrade pair |
| Horizontal Anti-pattern | Five L1 Archers with no Upgrade | Test whether undeveloped expansion really lacks the required developed output |
| Concentration Anti-pattern | One L2 Archer with Quick Draw, Sharpened Arrows, and Scatter Arrow; no second Archer | Test the missing second-coverage-point lesson |

The order of Draft acquisition must be recorded with each run. The Reference sequence begins with the Core Archer, establishes the Support Archer from the first Level-Up Draft, then acquires Core L2, Basic, and Behaviour investments.

### 6.3 Fixed Draft Sequence Diagnostic Fixture

Stage calibration separates Build efficacy from natural Draft probability. `DraftSystem` therefore exposes two Editor calibration states:

- Natural: generate choices through the existing weighted Draft pool.
- Fixed: use the next configured Fixed Draft Step while preserving the existing session token, pause, Pending, drag, placement, Level-Up, Upgrade, and consumption flow.

The Fixed path still constructs the natural candidate pool first. Every configured item must belong to the active Stage pool and be naturally eligible in the current runtime state; Fixed mode does not bypass Required Level, TowerFamily, duplicate package, or Pending reservation rules.

The serialized Reference sequence is:

| Draft Ordinal | Fixed Choice | Required Player Action |
|---:|---|---|
| 1 | Archer Tower | Deploy Core Archer |
| 2 | Archer Tower | Deploy Support Archer |
| 3 | Archer Tower | Drop onto Core Archer to reach L2 |
| 4 | Quick Draw | Apply to Core Archer |
| 5 | Scatter Arrow | Apply to Core Archer |

`Prefab_GameRuntime` stores this five-Step diagnostic sequence with Fixed mode disabled by default. The current `Main` Stage1 fixture enables it through a scene override. Retry, Stage release, and a fresh Battle reset sequence ownership to Step 1. `TowerUpgradeDraftDebugWindow` remains an independent Editor-only Task003/Task004 helper and is not used by Task009 Fixed Sequence runs.

## 7. Required Measurements

- Draft resolution node and opening time
- Wave start and end
- Peak simultaneous Monsters
- Monster leaks
- Final Player Health
- Last Draft to final resolution time
- Reference Build result
- Each Anti-pattern's failure point and capability gap
- Wave spawn-start and spawn-complete milestones
- Alive, spawned, resolved, killed, leaked, and Player Health snapshots at every Wave and Draft milestone
- Per-Monster spawn and resolution time so simultaneous pressure can be reconstructed
- Per-Monster source Wave number and within-Wave spawn ordinal
- Per-Wave spawned, killed, leaked, unresolved, damage, leaked-health, and resolution-time attribution
- Per-Tower successful deployment ordinal, active time, World position, and occupied Grid positions
- Draft attempt count versus Progression-triggered Draft count, plus committed-selection completeness
- Run duration and elapsed combat after the final Draft
- Configured Tower and Tower Upgrade Draft pools
- Per-Draft natural candidate identities and multiplicities before selection
- Per-Draft generation mode, displayed choices, and committed selection

`CombatBalanceRunRecorder` schema v13 owns these diagnostic fields. Wave attribution follows the Monster's authoritative source Wave rather than the Wave active when that Monster eventually resolves. Tower placement attribution is captured only after a successful gameplay commit and identifies the occupied Map Grid positions used by the calibration fixture. Fixed-mode displayed-choice frequency is not natural probability evidence; the pre-selection natural candidate multiplicities are retained for later Draft probability modelling. The final Tower snapshot remains the authority for the realized Build, and the Recorder does not make gameplay or balance decisions.

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Approved Stage1 experience, Reference Build, required capability, and Anti-pattern |
| StageDefinition | Stage1 composition and maximum health |
| MonsterWaveConfig | Stage1 Monster order, count, and timing |
| Map Prefab | Stage1 route and placement geometry |
| Task009 | Derived Wave proposal, Stage-local calibration evidence, and accepted result |

## 9. Required Calibration Table

Codex prepares the first Stage calibration table:

| Wave | Monster Role | Count | Spawn Interval | Wave Delay | Expected Draft Milestone | Expected Pressure | Observed Result |
|---|---|---:|---:|---:|---|---|---|
| Proposed Wave | Proposed roster entry | First-pass count | Task007 Profile-derived interval | Map-calibrated delay | Expected cumulative Draft | Intended Reference Build test | Filled after Play Mode |

Candidate v0.1 is the concrete table in section 6.1. Observed results are filled from the named schema-v13 JSON runs rather than copied from visual impressions alone.

## 10. Execution Collaboration

- The user owns Unity authoring, fixed Reference and Anti-pattern runs, and reporting Draft timing, leaks, final health, and Stage duration.
- Codex fills the first MonsterWaveConfig table from the Blueprint, Task008 Draft skeleton, accepted Map, and global combat baselines.
- Codex compares the observed run with the expected result and changes one Stage-local pressure axis at a time.

## 11. Unity Authoring Checklist

- Assign the approved Stage1 Map, Wave, and Draft pools.
- Author the Stage1 MonsterWaveConfig.
- Confirm the Main fixture enables Fixed Draft Choices and contains five Reference Steps.
- Place the fixed Reference Build.
- Apply each Fixed choice before the next required runtime eligibility dependency.
- Record every Draft opportunity and its timing.
- Run the Reference Build and both legal Anti-patterns.
- Record final health, leaks, Stage duration, and failure reason.
- Revalidate the route after any Map change.

## 12. Acceptance Criteria

- Stage1 grants exactly five total Drafts.
- Schema-v13 reports five opened Draft attempts, five committed selections, both Draft integrity checks as true, source-Wave attribution totals matching global combat totals, and complete deployment observations for every final Tower.
- The approved Reference Build clears consistently.
- At least one legal non-Reference Upgrade combination or adaptive Build can clear; success does not depend on one uniquely named Basic/Behaviour pair.
- The legal L1 Archer over-expansion Anti-pattern fails because of insufficient developed output.
- The legal single-Archer concentration Anti-pattern fails because it omits the required second coverage point, not because concentrated investment receives a hidden penalty.
- Failure is not caused by an invalid route or missing placement.
- The final Draft has enough remaining combat to matter.
- Every standard Wave preserves the Task007 reference spatial gap; Stage1 difficulty does not silently come from interval-driven density changes.
- No global baseline is changed solely to repair Stage1.

## 13. Validation

- Reference Build Play Mode runs
- Over-expansion and single-Archer concentration Play Mode runs
- Draft-count and timing review
- Map route validation
- StageDefinition and MonsterWaveConfig validation

## 14. Review Note

Any accepted global-value revision returns to its owning Task and requires a fresh Stage1 regression before Stage2 calibration begins.
