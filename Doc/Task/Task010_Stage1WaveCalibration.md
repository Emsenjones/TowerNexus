# Task010 - Stage1 Wave Calibration

Status: Completed; Stage1 V4 accepted on 2026-08-27 with schema-21
Reference, coherent-alternative, horizontal-edge, and concentration Anti-pattern
evidence

Depends on: Accepted Task007 Elemental And Buff Baseline; accepted Map and Draft runtimes

Unblocks: Task010A Monster Placement Route Continuity Refactor; Task011 resumes
after Task010A preserves the accepted Stage1 Reference result under the revised
movement contract

## 1. Goal

Calibrate Stage1 so a developed Core plus one undeveloped Support coverage point
is the reliable low-leak path, while Builds that omit required route coverage
are not stable clears.

## 2. Fixed Design Intent

- Archer-only Stage pool;
- five-Draft candidate budget;
- Reference: one L2 Archer Core with one Basic and one Behaviour Upgrade plus
  one L1 Archer Support;
- horizontal stress fixture: spend every Tower Draft opportunity on undeveloped
  L1 Archers and measure whether route coverage can compensate for missing Core
  development;
- concentration Anti-pattern: omit the required second coverage point.

## 3. Stage Composition And Fixed Build Fixtures

- Derive exactly five Drafts from two Tower placements, one Core Level Up, and
  two Core Upgrade applications.
- Author the exact Archer Tower Draft Pool and every currently unlocked Archer
  Basic and Behaviour UpgradeDefinition required by the cumulative Stage policy.
- Prove continuous Archer Level 2 reachability with at least one newly eligible
  Upgrade at the required level.
- Select and record the exact Reference Upgrade pair, one coherent alternative,
  the horizontal undeveloped-expansion stress fixture, the missing-coverage
  Anti-pattern, and their legal placement assumptions.
- Author one five-step Fixed Draft sequence for each required Build: one Tower-
  only Initial step plus four Player level-up steps. Every configured choice must
  be naturally eligible at that step and must follow normal Pending, placement,
  Level Up, Upgrade, and consumption rules.

Fixed displayed choices prove Build constructibility only. They are not natural
offer-frequency evidence and do not determine whether a player preferred a
different Build; Task017 owns those questions.

### 3.1 Frozen Reference Build v1

The first calibration anchor uses `Quick Draw + Explosive Arrow`. Task004 has
already verified this Basic/Behaviour combination as a coherent Archer package;
Stage1 uses it as a reproducible Reference, not as the only correct answer.

| Draft | Resolution node | Fixed result | Commit target | Resulting Build state |
|---:|---:|---|---|---|
| 1 | `0` | Archer Tower | Deploy Core | Core L1 |
| 2 | `3` | Archer Tower | Level Core | Core L2 |
| 3 | `6` | Archer Tower | Deploy Support | Core L2 + Support L1 |
| 4 | `10` | Quick Draw | Apply to Core | L2 Core with Basic + L1 Support |
| 5 | `14` | Explosive Arrow | Apply to Core | Complete Reference Build |

Fixed placement footprints are Core `(4,4), (4,5)` and Support `(5,1), (5,2)`.
Both footprints are calibration inputs whose intended route exposure is fixed
across Reference comparisons.

### 3.2 Accepted StageDefinition V4

- Tower pool: Archer only.
- Upgrade pool: all three Archer Basic and all three Archer Behaviour
  definitions; no Elemental content.
- Player Max Health: `4`.
- Progress Requirements: `[3, 3, 4, 4]`, producing Draft nodes
  `3 / 6 / 10 / 14`.
- Wave table:

| Wave | Profile | HP | Count | Spawn Interval | Wave Delay | Cumulative Monsters | Intended milestone |
|---:|---|---:|---:|---:|---:|---:|---|
| 1 | Slime Lv1 | `60` | `3` | `2.5s` | `4s` | `3` | Level Core to L2 |
| 2 | Slime Lv1 | `60` | `3` | `2.5s` | `6s` | `6` | Deploy Support |
| 3 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `5s` | `10` | Apply Quick Draw |
| 4 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `14` | Apply Explosive Arrow |
| 5 | Turtule Shell Lv4 | `400` | `7` | `2.5s` | `4s` | `21` | Seven-Monster post-Build test |

The complete Build matrix accepts these values. Four timely or normal-
interaction Phase D runs resolved `21 / 21` with zero leaks and material Support
contribution. Two separate delayed-Core runs committed Core L2 at resolution
node `6` rather than `3`, leaked two Monsters, and still reached Victory; they
are retained as execution-latency evidence rather than Reference repeats or a
reason to reduce the accepted Player Health.

### 3.3 Alternative Build Matrix

The shared Fixed Draft matrix preserves the first three Reference steps and
offers all three legal Archer Basics at Step 4 plus all three legal Archer
Behaviours at Step 5:

| Draft | Resolution node | Fixed displayed choices | Required action |
|---:|---:|---|---|
| 1 | `0` | Archer Tower | Deploy Core |
| 2 | `3` | Archer Tower | Level Core to L2 |
| 3 | `6` | Archer Tower | Deploy Support |
| 4 | `10` | Quick Draw / Sharpened Arrows / Eagle Sight | Apply the named Basic to Core |
| 5 | `14` | Explosive Arrow / Scatter Arrow / Piercing Arrow | Apply the named Behaviour to Core |

The first Alternative matrix uses three representative packages while keeping
the same Tower levels, placement, Draft nodes, and Stage fixture:

| Build | Basic | Behaviour | Measurement axis |
|---|---|---|---|
| Alternative A | Quick Draw | Scatter Arrow | Cadence plus additional attack entities |
| Alternative B | Sharpened Arrows | Piercing Arrow | Damage plus aligned multi-target penetration |
| Alternative C | Eagle Sight | Explosive Arrow | Coverage duration plus area conversion |

Each run must select only its named pair. Recorder `selectedChoice`, investment
commits, final Tower upgrades, and Tower-by-Wave attribution are acceptance
evidence; the shared displayed matrix is not permission to mix another pair
under the same Run Name.

Run Names:

```text
Task010_PhaseE_Stage1V4_AlternativeA_ArcherL2_QuickDraw_ScatterArrow_ArcherL1_Lv3HP180_Lv4HP400_Core44-45_Support51-52_Schema21_01
Task010_PhaseE_Stage1V4_AlternativeB_ArcherL2_SharpenedArrows_PiercingArrow_ArcherL1_Lv3HP180_Lv4HP400_Core44-45_Support51-52_Schema21_01
Task010_PhaseE_Stage1V4_AlternativeC_ArcherL2_EagleSight_ExplosiveArrow_ArcherL1_Lv3HP180_Lv4HP400_Core44-45_Support51-52_Schema21_01
```

### 3.4 Anti-pattern Build Fixtures

Anti-pattern acceptance measures deliberate investment-structure mistakes, not
arbitrary placement failure or delayed Pending-item execution. Every deployed
Tower must use a deliberate route-covering position, and every selected item is
committed normally. A run does not qualify merely because the player placed a
Tower where its Attack Range cannot meaningfully intersect the Monster route.

The Horizontal stress fixture spends all five Drafts on distinct undeveloped
Towers:

| Draft | Resolution node | Fixed result | Required action |
|---:|---:|---|---|
| 1 | `0` | Archer Tower | Deploy L1 Archer 1 |
| 2 | `3` | Archer Tower | Deploy L1 Archer 2; do not Level Up |
| 3 | `6` | Archer Tower | Deploy L1 Archer 3 |
| 4 | `10` | Archer Tower | Deploy L1 Archer 4 |
| 5 | `14` | Archer Tower | Deploy L1 Archer 5 |

Use five deliberate best-effort coverage footprints. The first valid run freezes
their exact Recorder-observed positions for any required repeat; obviously
non-covering placements invalidate the comparison.

```text
Task010_PhaseF_Stage1V4_AntiPatternHorizontal_FiveArcherL1_BestReasonableCoverage_Lv3HP180_Lv4HP400_Schema21_01
```

Concentration / missing-coverage Anti-pattern spends the Support Draft on a
second Basic and leaves only the accepted Core footprint `(4,4), (4,5)`:

| Draft | Resolution node | Fixed result | Required action |
|---:|---:|---|---|
| 1 | `0` | Archer Tower | Deploy Core |
| 2 | `3` | Archer Tower | Level the same Core to L2 |
| 3 | `6` | Quick Draw | Apply to Core |
| 4 | `10` | Sharpened Arrows | Apply to Core |
| 5 | `14` | Scatter Arrow | Apply to Core; deploy no Support |

```text
Task010_PhaseF_Stage1V4_AntiPatternConcentration_OneArcherL2_QuickDraw_SharpenedArrows_ScatterArrow_NoSupport_Core44-45_Lv3HP180_Lv4HP400_Schema21_01
```

The two paths require separate Fixed Draft configurations because Scatter Arrow
is not naturally eligible in the five-L1 horizontal state. Two Horizontal runs
each reached Victory with `19 / 21` kills, two leaks, and final Player Health
`2`; their Effective Damage was `4320` and `4160`. This reproducible result
proves that broad route coverage can compensate for missing Core development at
a materially worse margin than the Reference. Because five consecutive Archer
Tower selections are a Fixed efficacy fixture rather than natural-offer
evidence, Task010 accepts this as a rare horizontal edge Build instead of
manufacturing its Defeat through lower Player Health. Task017 owns the
probability of reaching it naturally.

The Concentration run reached Defeat at Player Health `0`. Waves 1-4 resolved
`14 / 14`; Wave 5 killed one, leaked four, and retained two unresolved Monsters
when Defeat ended the run. Its complete one-Tower Build committed at resolution
node `14`, received five later resolutions and `47.59s` of post-commit combat,
and passed every schema-21 execution and integrity check. The failure therefore
measures missing route coverage rather than an unfinished Build or invalid
execution.

## 4. Stage Calibration And Monster Authoring

1. Author four positive Player Progress Requirements. Their sum is the resolved-
   Monster node that opens the final Draft.
2. Choose a total Monster budget greater than that sum so the completed Build
   receives a meaningful post-final-Draft pressure window.
3. Begin with existing Monster runtime templates only as candidates. Reuse a
   candidate when its HP can express the required opening, body, or late-pressure
   role. When none can, create the smallest new Stage-needed Profile instead of
   pre-authoring a complete campaign roster.
4. Formal reusable Profiles use `Prefab_Monster_<Visual>_Lv<N>`. `N` is the
   ascending rank of the Profile HP among distinct HP values in the formal
   reusable roster; Profiles with equal HP share one Level. Role labels such as
   Fodder, Normal, Rush, Tough, or Tank are not part of Profile identity.
   Inserting a new HP tier reindexes affected formal prefab filenames and root
   object names while preserving their `.meta` GUIDs. `MonsterCandidate_*`
   assets remain outside this index until promoted to formal Profiles.
5. Every Profile accepted in this Task uses `MoveSpeed = 0.25`; every standard
   Wave authors `SpawnInterval = 2.5s`, preserving the `0.625` spatial gap.
6. Author and record the exact `MonsterWaveConfig`: ordered Wave index,
   `WaveDelay`, Monster runtime template, Count, and `SpawnInterval`.
7. Calibrate the Progress sequence, Profile HP, Wave order and Count,
   `WaveDelay`, Player Health, and Reference/alternative placement together
   until the intended Build envelope is reproducible.

A Monster Profile first accepted here becomes reusable downstream. Later Tasks
must not silently change its HP or MoveSpeed; changing it reopens this Task and
every accepted Stage that references it.

## 5. Calibration Loop And Recorder Evidence

1. Freeze the exact Reference Build, coherent-alternative fixtures, horizontal
   stress fixture, missing-coverage Anti-pattern, placement, and Draft order
   before changing Stage values.
2. Use the Reference Build to establish the first viable Stage candidate. Tune
   Wave/Profile pressure and Progress timing before Player Health; Player Health
   only sets the accepted leak allowance after the combat curve is legible.
3. Run the complete Build matrix against that same candidate. Alternatives define
   the viable Build envelope; Anti-patterns test whether the Stage discriminates
   against the intended mistakes.
4. Change only the smallest Stage-owned variable set supported by the evidence,
   then rerun the Reference and the complete comparison matrix. Repeat until all
   acceptance bands hold. Do not reselect the Reference between iterations unless
   evidence invalidates the original design fixture.
5. If coherent and Anti-pattern Builds remain materially indistinguishable after
   Wave timing, Profile HP/count, Progress timing, placement exposure, and leak
   allowance have been isolated, stop Stage tuning and reopen the upstream Tower
   Build-power task. Player Health must not be used to manufacture a power gap.

Recorder schema 21 is the minimum evidence contract for every acceptance run:

- `fixture` freezes Stage identity, configured Player Health, complete Map node
  topology, and the ordered Wave definitions;
- `investmentRuntime.commits` records every Deployment, Level Up, and Upgrade
  commit with its Draft token, Tower, timing, and Resolution node;
- `timing.secondsAfterFinalBuildCommit` and
  `investmentRuntime.resolutionsAfterFinalBuildCommit` prove the completed Build
  received combat exposure;
- `towerWaveSummaries` attributes successful Tower-scaled applications, effective
  damage, and killing blows by Tower and source Wave;
- `execution` reports whether the authored Stage and Build completed; expected
  Defeat or an unfinished Anti-pattern therefore does not become an integrity
  failure;
- `integrity` is reserved for fixture, event-count, attribution, transaction, and
  diagnostic reconciliation.

## 6. Required Runs

- at least two integrity-valid Reference runs when the margin is near a leak;
- one coherent alternative;
- horizontal undeveloped-expansion stress fixture;
- concentration Anti-pattern;
- placement sanity check at the accepted Reference positions.

All Build-comparison runs use Fixed Draft sequences. Natural offer frequency and
the reasons a player may finish with another Build are outside this Task and
belong to Task017.

## 7. Pressure Acceptance

- opening Lv1 pressure lets the Initial Tower earn early progress;
- the player can form the Core/Support structure before high pressure;
- later Waves give the developed Core plus Support a stronger and more reliable
  clear margin than undeveloped horizontal expansion alone;
- Reference is stable with zero or only explicitly accepted minimal leak;
- coherent alternatives may clear without matching one exact Upgrade pair;
- final Player Health permits only the reviewed leak margin;
- Progress Requirements open all five Drafts at the intended pressure nodes and
  leave reviewed post-final-Draft combat;
- the exact Stage pools, Required-Level reachability, and every required Fixed
  Build sequence are accepted with the Stage rather than by an upstream fixture;
- every used Monster Profile is identified as reused or first accepted here,
  with its HP and fixed-speed values recorded;
- the accepted Wave table exactly matches the authored `MonsterWaveConfig`;
- per-Wave Recorder evidence explains every leak and pressure step.

The archived five-L1-Archer no-damage clear remains the motivating regression.
The accepted fixture reduces it to a reproducible two-leak, low-margin edge
without requiring that every legal horizontal allocation end in Defeat.

## 8. Completion Evidence And Handoff

Stage1 V4 is accepted with Player Health `4`, Progress Requirements
`[3, 3, 4, 4]`, and the Wave table in Section 3.2. Every accepted comparison
formed its named Build, received post-final-Build combat, and passed all
schema-21 execution and integrity checks.

| Fixture | Repeats | Terminal result | Combat result | Decision |
|---|---:|---|---|---|
| Reference, timely Core L2 | `4` | Victory | `21 / 21`, zero leaks | Accepted stable target |
| Reference, delayed Core L2 | `2` | Victory | `19 / 21`, two leaks | Execution-latency evidence; not balance input |
| Alternative A | `1` | Victory | `20 / 21`, one leak, ED `4427` | Accepted coherent alternative |
| Alternative B | `1` | Victory | `20 / 21`, one leak, ED `4485` | Accepted coherent alternative |
| Alternative C | `1` | Victory | `20 / 21`, one leak, ED `4476` | Accepted coherent alternative |
| Five-L1 Horizontal | `2` | Victory | `19 / 21`, two leaks, ED `4320 / 4160` | Accepted rare low-margin edge Build |
| One-Core Concentration | `1` | Defeat | `15` kills, `4` leaks, `2` unresolved, ED `3455` | Accepted primary Anti-pattern failure |

The ideal ordering remains Reference zero-leak, coherent alternatives with an
accepted small leak margin, and strategically incoherent Builds failing. Stage1
does not realize that ordering for every legal allocation: the Horizontal edge
can narrowly clear. This exception is accepted because the Reference is stable,
three distinct coherent packages clear, the Horizontal result pays a repeatable
two-leak penalty, and the primary missing-coverage Anti-pattern fails decisively.
The Stage therefore teaches a meaningful allocation lesson without requiring a
single exact Upgrade answer or using Player Health to force an artificial gap.

Task011 may reuse the Stage1-accepted fixed-speed Monster Profiles and this
calibration workflow. Later Stage Tasks should still pursue the ideal ordering,
but may accept a similarly documented secondary edge case when it has a
materially worse margin, does not erase the Stage's primary strategic test, and
is not confused with natural Draft-offer probability.
