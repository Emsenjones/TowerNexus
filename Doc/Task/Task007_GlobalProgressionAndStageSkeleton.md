# Task007 - Per-Stage Progression And Stage Skeleton

Status: Completed; Stage-owned progression, six StageDefinitions, cumulative Draft pools, Normal-only Wave skeletons, Reference Player Health, and structural Play Mode validation were accepted on 2026-08-19

Depends on: Task001-Task006

## 1. Goal

Translate the six approved Stage Reference Builds into six independently authored Player progression sequences and six complete StageDefinition content skeletons ready for individual calibration.

This Task derives Draft budgets, exact Draft pools, per-Stage Progress requirements, structural Wave budgets, and theoretical minimum active-Wave duration. It does not finalize Stage difficulty or add those implementation numbers back into the Stage Design Blueprint.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
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

Task001-Task006 provide accepted Map and Stage-independent combat, Monster-HP, movement-identity, and Wave-composition baselines. Reference Build Draft costs, total Draft opportunities, and the cumulative content-unlock policy are approved by Blueprint v0.2. Task007 materializes the exact Stage asset lists and derives Progress nodes, Monster budgets, candidate-solvability evidence, and the six Stage skeletons. Exact repeated support families, Reference-run Upgrade identities, and ElementTypes remain downstream calibration choices unless required for a specific solvability check.

## 4. Required Derived Planning Table

Before Unity authoring, Codex prepares one reviewable row per Stage containing:

| Derived Field | Derivation |
|---|---|
| Reference Build Draft Cost | Count deployments, Level-ups, and applied Upgrades in the Reference Build |
| Total Draft Opportunities | Match the Reference Build Draft cost |
| Required Player Level-Ups | Total Draft Opportunities minus the Initial Draft |
| Exact Tower Draft Pool | Materialize every cumulatively unlocked TowerFamily as Stage asset references |
| Exact Tower Upgrade Draft Pool | Materialize every currently unlocked UpgradeDefinition for the represented families |
| Per-Family Stage Level Cap | Highest Required Tower Level in that family's Stage Upgrade pool, with a continuous unlock path |
| Player Progress Requirements | Author one positive ordered sequence whose length equals the required Player Level-Ups for this Stage |
| Rough Monster Resolutions | Sum this Stage's Progress Requirement sequence, then add any intended post-final-Draft reserve |
| Theoretical Minimum Active-Wave Duration | Derive from authored Wave Delay, Spawn Intervals, and Counts under immediate Monster resolution |
| Last-Draft Combat Reserve | Preserve enough combat for the final choice to affect the result |
| Candidate Solvability Rule | Measure remaining opportunities and practical sampling access without guaranteeing the next offer |
| Legal Calibration Builds | Confirm that the Reference, viable alternative, and planned Anti-pattern allocations can actually be formed from the authored pools and Draft budget |

The table begins as a proposed numeric pass. It becomes accepted only after structural validation and user Play Mode evidence.

Current Blueprint-derived v0.2 inputs:

| Stage | Reference Towers | Core Towers | Reference Build Draft Cost | Total Draft Opportunities | Required Player Level-Ups After Initial Draft | Elemental Build Requirement |
|---|---:|---:|---:|---:|---:|---|
| Stage1 | 2 | 1 | 5 | 5 | 4 | None |
| Stage2 | 3 | 1 | 6 | 6 | 5 | None |
| Stage3 | 4 | 1 | 7 | 7 | 6 | None |
| Stage4 | 5 | 1 | 8 | 8 | 7 | None |
| Stage5 | 5 | 1 | 10 | 10 | 9 | One Elemental Core |
| Stage6 | 5 | 2 | 15 | 15 | 14 | Two overlapping Cores with the same ElementType |

Stage1-Stage4 each use one L2 Core. Their Draft costs equal the number of Tower deployments plus one Core Level-up, one Basic Upgrade, and one Behaviour Upgrade.

Stage5 costs 10 Drafts: five Tower deployments plus two Level-ups and one Basic, Behaviour, and Elemental Upgrade for its Core.

Stage6 costs 15 Drafts: five Tower deployments plus two Cores, each requiring two Level-ups, one Basic Upgrade, one Behaviour Upgrade, and one Elemental Upgrade.

The one-Basic and one-Behaviour Core is the Reference budget unit, not the maximum legal Upgrade stack. Exact pools must also expose whether a player can legally redirect multiple Upgrades into one Tower; later Stage Tasks use that legal concentrated build only when it tests a required coverage, complement, or cooperation gap.

Stage4-Stage6 intentionally share five Reference Towers and the same proposed Map scale, but their Total Draft Opportunities remain 8, 10, and 15 because their Reference Builds require different vertical and Elemental investment.

The cumulative v0.2 Draft-pool policy is:

| Stage | Tower Draft Families | Unlocked Upgrade Content |
|---|---|---|
| Stage1 | Archer | All Archer Basic and Behaviour definitions |
| Stage2 | Archer, Cannon | All Basic and Behaviour definitions for both families |
| Stage3 | Archer, Cannon, Magic | All Basic and Behaviour definitions for all three families |
| Stage4 | Archer, Cannon, Magic, Drone | All Basic and Behaviour definitions for all four families |
| Stage5 | Archer, Cannon, Magic, Drone | All Basic, Behaviour, and Elemental definitions for all four families |
| Stage6 | Archer, Cannon, Magic, Drone | All Basic, Behaviour, and Elemental definitions for all four families |

This policy fixes pool membership without prescribing one exact player Build. Downstream Reference Runs record the specific Upgrade identities they used for reproducibility.

The cumulative pool produces an L2 Stage cap for every represented family in Stage1-Stage4 and an L3 Stage cap for every represented family in Stage5-Stage6. Core is a strategic role selected by the player's investment, not a separately authored Tower identity.

These values are the first trajectory estimate. Task007 does not treat a Stage's Progress sequence, Monster totals, duration estimate, Draft pools, or candidate availability as accepted until the derived table has been structurally tested.

## 5. In Scope

- Reference Build Draft-cost calculation
- Exact Tower and Tower Upgrade Draft pools
- Per-Stage, per-TowerFamily level caps
- Continuous Required-Level unlock paths from Level 1 through each Stage cap
- Rough total Monster-resolution budget per Stage
- One Stage-authored Player Progress Requirements sequence per StageDefinition
- Cumulative Draft nodes
- Theoretical minimum active-Wave duration derived from immediate Monster resolution
- Stage1-Stage6 StageDefinitions
- Map references
- MonsterWaveConfig references and initial Wave skeletons
- Consumption of the Task006 homogeneous-Wave convention, accepted per-Profile Move Speeds, and standard spatial-gap interval rule
- Stage maximum-health authoring
- Post-final-Draft validation segment targets
- Candidate solvability where required by the Blueprint
- Legal constructibility records for every planned Stage calibration and Anti-pattern build
- Revalidation of Task001 Reference and reasonable alternative placements against exact Draft pools

## 6. Out Of Scope

- Final observed Stage duration or combat-duration guarantee
- Final Wave Delay or any intentional departure from the standard spatial-gap interval
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

Each StageDefinition authors one independent ordered Progress Requirement sequence. Its entry count equals that Stage's Required Player Level-Ups, and its sum is the earliest Monster-resolution count at which the final intended level-up Draft can occur.

The Initial Tower Draft is separate and is not represented by an entry. Every entry must be positive. Stage composition supplies the selected sequence to Player System, which validates and snapshots it for the battle without mutating StageDefinition. Retry reloads the same Stage sequence into fresh state; next Stage loads its own sequence. No Player-global fallback or supplementary Progress Requirement sequence remains.

## 8. Authoring Sequence

1. Reconfirm the v0.2 Draft cost for all six approved Reference Builds.
2. Materialize the approved cumulative Tower and Tower Upgrade Draft pools as exact Stage asset lists.
3. Derive every represented TowerFamily's Stage cap and validate each intermediate unlock level.
4. Check that every required Reference Build remains obtainable.
5. Confirm that each planned viable alternative and Anti-pattern is legal under the same pools and opportunities; remove or revise an impossible test rather than manufacturing it.
6. Solve one positive Player Progress Requirements sequence for each Stage.
7. Verify each sequence length, cumulative Draft nodes, and final intended Draft resolution count.
8. Choose rough Monster-resolution totals and reserve combat after the last intended Draft.
9. Reconfirm the Task006 movement identities, homogeneous-Wave convention, and Profile-derived Spawn Intervals.
10. Calculate each Stage's theoretical minimum active-Wave duration under immediate Monster resolution.
11. Author six StageDefinitions and six MonsterWaveConfig skeletons.
12. Assign Map, Player Progress Requirements, Tower pool, Upgrade pool, and Stage maximum health.
13. Revalidate Task001 Reference and reasonable alternative placements against the exact pools.
14. Validate each Stage composition without final difficulty claims.

## 9. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Reference Build, Stage lesson, required capability, Anti-pattern, and non-negotiable constraints |
| Task007 | Derived planning table, cross-Stage constraint solving, and structural validation |
| Player System | Battle-local snapshot, validation, progress consumption, and level transitions for the selected Stage sequence |
| StageDefinition | Map, Wave, Draft pools, maximum health, and reusable Player Progress Requirements authoring |
| MonsterWaveConfig | Ordered Wave skeleton and rough Monster totals |
| Draft System | Candidate generation according to the authored pools and approved solvability contract |
| Tower Upgrade System | Stage-derived Tower level cap and final level-up eligibility |

## 10. Execution Collaboration

- The user supplies or approves the intended Stage experience and Reference Builds.
- Codex calculates the first complete Draft, pool, per-Stage Progress, Monster-budget, theoretical-duration, and candidate-solvability table from those inputs.
- The user authors or confirms the proposed values in Unity and performs the structural Play Mode smoke test.
- Codex compares observed Draft timing and counts against the table, then proposes the smallest next revision.
- No proposed value is treated as accepted before the corresponding Unity validation.

## 11. Unity Authoring Checklist

- Review the six-row derived planning table.
- Author the accepted Player Progress Requirements sequence on each StageDefinition.
- Create or update Stage1-Stage6 StageDefinitions.
- Assign the intended Map and MonsterWaveConfig to each Stage.
- Assign the reviewed Tower and Tower Upgrade pools.
- Confirm every represented TowerFamily's derived Stage cap and continuous Required-Level ladder.
- Author positive Stage maximum health.
- Create structurally valid Wave content with the rough Monster totals.
- Keep each Wave to its single authored Monster Runtime Template.
- Seed each Wave with the Task006 interval derived from its Monster Runtime Template's Profile Move Speed; do not use interval drift as an unstated density adjustment.
- Verify the Initial Draft and first-Wave gate.
- Record cumulative resolution nodes and observed Draft counts.
- Compare the calculated theoretical minimum duration with observed Play Mode timing.
- Recheck Reference and reasonable alternative placements when an exact pool changes the available Tower set.

## 12. Acceptance Criteria

- One reviewed derived planning table covers all six Stages.
- Every StageDefinition has one positive Player Progress Requirements sequence whose length matches its required Player Level-Ups.
- Every Stage skeleton reaches its derived Draft count in resolution-count space.
- Six StageDefinitions reference the intended Maps, Waves, and Draft pools.
- Every Wave skeleton consumes an accepted Task006 Monster identity and composition convention.
- Every standard Wave preserves the Task006 reference spatial gap through its explicit Profile-derived interval.
- Every approved Reference Build is obtainable under its authored Draft structure.
- Every planned calibration and Anti-pattern build is either legally constructible or explicitly replaced before Stage testing.
- Every allowed Tower level transition unlocks at least one Upgrade at the reached Required Tower Level.
- Newly eligible required content has a reviewed practical chance to appear within the remaining Draft opportunities without a guaranteed next offer.
- Stage6 retains at least one achievable matching-Element build path if Overload is mandatory.
- Every fresh Stage begins with one Initial Tower Draft.
- Every Stage retains a post-final-Draft validation segment.
- Player System contains no reusable global Progress Requirement authoring or fallback.
- Every Stage has a reviewed theoretical minimum active-Wave duration estimate.

## 13. Validation

- Reference Build cost review
- Draft-pool and candidate-solvability review
- Reference, alternative, and Anti-pattern legal-constructibility review
- Per-Family Stage-cap and Required-Level continuity review
- Cumulative Draft-node calculation review
- Per-Stage Progress Requirement length, positivity, sum, and runtime-snapshot review
- Theoretical minimum active-Wave duration calculation review
- Task001 Reference-placement and range/route regression
- StageDefinition validation
- MonsterWaveConfig structural validation
- Initial Draft and first-Wave gate check
- Progress and Draft-count Play Mode smoke test
- Static asset-reference review

## 14. Accepted Baseline And Evidence

The accepted Task007 skeleton is:

| Stage | Total Drafts | Player Progress Requirements | Final Draft Node | Wave Counts | Total Monsters | Post-Final-Draft Resolutions | Reference Player Health | Theoretical Minimum Duration |
|---|---:|---|---:|---|---:|---:|---:|---:|
| Stage1 | 5 | 3 / 3 / 4 / 4 | 14 | 14 / 8 | 22 | 8 | 11 | 1:00 |
| Stage2 | 6 | 3 / 3 / 4 / 4 / 8 | 22 | 11 / 11 / 10 | 32 | 10 | 16 | 1:27.5 |
| Stage3 | 7 | 3 / 3 / 4 / 4 / 8 / 8 | 30 | 10 / 10 / 10 / 12 | 42 | 12 | 21 | 1:55 |
| Stage4 | 8 | 3 / 3 / 4 / 4 / 8 / 8 / 10 | 40 | 10 / 10 / 10 / 10 / 14 | 54 | 14 | 27 | 2:27.5 |
| Stage5 | 10 | 3 / 3 / 4 / 4 / 8 / 8 / 10 / 12 / 12 | 64 | 13 / 13 / 13 / 13 / 12 / 12 | 76 | 12 | 38 | 3:25 |
| Stage6 | 15 | 4 / 4 / 4 / 4 / 5 / 5 / 5 / 5 / 5 / 5 / 6 / 6 / 6 / 6 | 70 | 12 / 12 / 12 / 12 / 11 / 11 / 19 | 89 | 19 | 45 | 4:35 |

The duration estimate includes every authored Wave Delay and every interval between consecutive spawns within a Wave, with each Monster resolving immediately when spawned. Stage6 uses the currently authored 10-second delay on all seven Waves; the other five Stages use 5-second delays. Reference Player Health is the Task007 structural baseline `ceil(Total Monsters / 2)`, not a final difficulty target.

Schema-v9 reports accepted the complete progression path for all six Stages. Each accepted run completed one Initial Draft, reached the intended Level-up and total Draft counts, resolved the configured Monster total, retained combat after the final Draft, and passed every integrity flag. The accepted Stage5 Recorder regression records final Draft node 64 and 12 later Resolutions after the same-frame Resolution bookkeeping fix.

Play Mode also confirmed fresh Retry state and a fresh Stage1-to-Stage2 Next Stage transition. The observed Build evidence includes the Stage1 two-Tower L2 Core structure, a Stage5 L3 Elemental core, and a Stage6 matching-Cold L3 Cannon/Magic pair. The Stage6 report records two contributing source Towers and shared-target Cold stacking, confirming an achievable different-family matching-Element overlap path without introducing a guaranteed candidate offer.

The cumulative pools and successful Stage runtime binding confirm an L2 cap with a continuous Basic-to-Behaviour ladder in Stage1-Stage4 and an L3 cap with a continuous Basic-to-Behaviour-to-Elemental ladder in Stage5-Stage6. Exact reproducible Upgrade identities, candidate success rates, final Wave composition, Reference clear margins, and Anti-pattern outcomes remain owned by Task008-Task013.

## 15. Review Note

Task008-Task013 own final Stage experience. If one Stage cannot satisfy its Blueprint, revise that Stage's Progress Requirements, Wave budget, Draft structure, or accepted build envelope through the smallest owning contract change, then rerun affected regressions.
