# Task015 - Stage6 Wave Calibration

Status: In Progress; resumed after Task015A Drone Holding lifecycle acceptance

Resumed: 2026-09-02

Depends on: Accepted Task014 Stage5 Wave Calibration

Resumed after: Completed Task015A Drone Holding Lifecycle Refactor

Blocks: Task016 and Task017

## 1. Goal

Calibrate Stage6 so two developed Towers with the same ElementType and
overlapping effective coverage can reliably produce the intended cooperation
and Overload result.

## 2. Fixed Design Intent

- all TowerFamilies and Elemental content available;
- fifteen-Draft candidate budget;
- two L3 Cores, each with one Basic, one Behaviour, and the same Elemental type;
- three L1 Supports complete the four-family roster;
- Anti-patterns stop at one Elemental Core, use unmatched Elements, or separate
  matching Cores so they do not share targets.

## 3. Stage Composition And Fixed Build Fixtures

- Derive exactly fifteen Drafts from five Tower placements, four Core Level Ups,
  and six Core Upgrade applications.
- Author the exact cumulative four-family Tower pool and all Stage-unlocked
  Basic, Behaviour, and Elemental UpgradeDefinitions. Prove continuous Level 3
  reachability for every family permitted to reach it.
- Select the two reproducible Core families, shared ElementType, exact matching
  Reference packages, coherent matching alternative, single-source, unmatched,
  and non-overlapping Anti-patterns, plus legal overlapping placements.
- Prove at least one legal matching-Element path and author one fifteen-step
  naturally eligible Fixed Draft sequence for every required Build. Preserve the
  Tower-only Initial step and all normal Draft, Pending, placement, Level Up,
  Upgrade, Elemental exclusivity, and consumption rules.

Fixed sequences prove matching-source Build efficacy and constructibility. They
do not establish how often natural Drafts expose that path; Task017 owns the
availability evidence.

### 3.1 Phase A Reference Build

The first-pass Reference uses Fire because Task014 already accepts the Magic
Fire package as a single-Core baseline. Stage6 adds a different-family Cannon
Fire Core so the first record can measure whether both source identities reach
the same Monsters, how quickly their contributions combine, and whether the
current provisional Wave leaves enough shared-target lifetime.

| Role | Tower | Final state |
|---|---|---|
| Core A | Magic | L3; Arcane Charge; Twin Orbs; Blazing Orbs |
| Core B | Cannon | L3; Faster Reload; Explosive Shell; Blazing Shells |
| Support | Archer | L1; no Upgrades |
| Support | Drone | L1; no Upgrades |
| Support | Magic | L1; no Upgrades |

The five Towers represent all four TowerFamilies. Matching TowerFamily is not
required. The repeated Magic Support spends only its deployment Draft and must
not receive a Core Upgrade.

The Phase A Fixed Draft sequence is:

| Draft | Provisional node | Fixed result | Intended commit |
|---:|---:|---|---|
| 1 | `0` | Archer Tower | Deploy Archer Support |
| 2 | `4` | Magic Tower | Deploy Magic Core |
| 3 | `8` | Magic Tower | Magic Core to L2 |
| 4 | `12` | Cannon Tower | Deploy Cannon Core |
| 5 | `16` | Magic Tower | Deploy Magic Support |
| 6 | `21` | Drone Tower | Deploy Drone Support |
| 7 | `26` | Arcane Charge | Apply to Magic Core |
| 8 | `31` | Twin Orbs | Apply to Magic Core |
| 9 | `36` | Magic Tower | Magic Core to L3 |
| 10 | `41` | Blazing Orbs | Apply to Magic Core |
| 11 | `46` | Cannon Tower | Cannon Core to L2 |
| 12 | `52` | Faster Reload | Apply to Cannon Core |
| 13 | `58` | Explosive Shell | Apply to Cannon Core |
| 14 | `64` | Cannon Tower | Cannon Core to L3 |
| 15 | `70` | Blazing Shells | Apply to Cannon Core |

All Tower and Upgrade identities are already members of the Stage6 cumulative
pools, and every Upgrade is applied only after its Required Tower Level is
reachable. The sequence preserves the Tower-only Initial Draft and normal
Pending placement, Level Up, Upgrade, exclusivity, and consumption rules.

### 3.2 Phase A Placement And Record Contract

The `_02` record freezes the Reference deployment order and occupied cells:

| Deployment | Tower role | Occupied cells |
|---:|---|---|
| 1 | Archer Support | `(7,8)`, `(7,9)` |
| 2 | Magic Core | `(8,6)` |
| 3 | Cannon Core | `(6,5)`, `(7,4)`, `(7,5)` |
| 4 | Drone Support | `(5,8)`, `(5,9)`, `(6,8)`, `(6,9)` |
| 5 | Magic Support | `(8,3)` |

Use this exact deployment order and placement for subsequent Reference
comparisons. Forced relocation remains diagnostic rather than a pass/fail
requirement.

The unoccupied Stage6 Map begins with a seven-node shortest path:
`(8,8) -> (7,8) -> (6,8) -> (5,8) -> (5,7) -> (5,6) -> (5,5)`. Because Tower
footprints can materially redirect this short path, record the logical route
after each deployment and judge Core overlap against the committed route rather
than only the unoccupied Map.

Use Recorder Run Name
`Task015_PhaseA_Stage6V1_Reference_MagicFire_CannonFire_Node70_HP45_ProvisionalWave_Schema23_01`.
The first record must be inspected for both Core source identities, shared
Monster targets, stack contribution order, source-scoped cooldown blocks,
Overloads and their source counts, first leak Wave, post-node-70 resolutions,
damage distribution, route changes, placement transactions, and integrity.

### 3.3 Provisional Measurement Scaffold

Phase A deliberately leaves the existing Stage6 balance skeleton unchanged:

- Player Health `45` is a measurement ceiling, not an accepted margin;
- Progress Requirements are
  `[4,4,4,4,5,5,5,5,5,5,6,6,6,6]`, producing Draft nodes
  `4/8/12/16/21/26/31/36/41/46/52/58/64/70`;
- the current seven Waves contain `12/12/12/12/11/11/19` Slime Lv1 Monsters,
  use `2.5s` Spawn Interval and `10s` Wave Delay, and total `89` resolutions;
- node `70` therefore leaves `19` provisional Monsters after both Elemental
  Cores are complete.

These values provide structure for the first Build measurement only. They are
not accepted Task015 Progress, Player Health, Profile order, Count, or Wave
timing, and they must be replaced or explicitly accepted from Recorder evidence
before Phase A can close.

### 3.4 Phase B V2 HP6 Diagnostic

Phase B V2 replaced the provisional scaffold with twelve Waves and `92`
Monsters while retaining final Draft node `70`. It reused the fixed-speed
HP `60/180/400/520/900/1300` Profiles, used Wave counts
`3/3/4/4/4/8/8/10/10/10/12/16`, Wave Delays
`4/6/5/4/4/4/4/4/4/4/4/4`, and `2.5s` Spawn Interval. Player Health was
reduced to `6` for the first strict-margin diagnostic.

Recorder Run
`Task015_PhaseB_Stage6V2_Reference_Placement02_MagicFire_CannonFire_Node70_HP45_W12_M92_ProfileRamp_Schema23_01`
captured the HP6 fixture despite the stale `HP45` label. All integrity checks
passed, but the run ended in defeat during Wave 6 with only five Drafts opened,
four investment commits, and final observed resolution node `16`.

The decisive stall occurred between Player Levels 2 and 3. The first two Waves
contained only six Slimes while the third Draft required resolution node `8`.
The first two Wave 3 leaks therefore unlocked that Draft; by the time the Magic
Core committed from L1 to L2, the remaining Wave 3 Plants were already too far
downstream for the upgrade to recover them. Wave 3 recorded zero kills and four
leaks with only `5/35/40/25` HP remaining. This is Progress-to-Wave timing
evidence, not evidence that the HP400-1300 Profiles are too durable: Waves
7-12 never started. Do not use extra Player Health or a higher-HP Profile to
mask this early constructibility failure.

### 3.5 Task015A Pause And Resume Boundary

Stage6 multi-Tower overlap exposed an independent Drone lifecycle defect: an
initialized Drone with no valid replacement target inside its source Tower's
Attack Range ends before Battery depletion. Task015 does not compensate for
that global runtime behavior through Wave, Progress, Player Health, placement,
or Monster HP changes.

Task015 implementation and new calibration runs paused until
`Task015A_DroneHoldingLifecycleRefactor` completed its accepted lifecycle and
regression route. Existing Phase A and Phase B records remain diagnostic
history. Task015A completed on 2026-09-02 without reopening Task004, Task007B,
or Task013. Task015 now resumes from the frozen `_02` deployment order and
placement, reruns its Reference under the accepted Drone contract, and then
continues Stage6 Progress and Wave calibration.

### 3.6 Phase B V3 Holding Observation

Before the Task015A regression matrix closed, V3 changed the first Progress
requirements to `3/3` and retained twelve Waves, `92` Monsters, Player Health
`6`, and final Draft node `70`. This fixed the V2 L2-to-L3 opening stall: the run
reached node `46`, completed the Magic Fire Core, and opened the Cannon L2 Draft
before the sixth leak ended the Stage during Wave 8.

The Record is schema `24` despite its stale `Schema23` filename. It observed
`7` Drones, `42` Holding entries, `41` Holding exits, `77` reacquisitions, `6`
Battery depletions, and `7` exactly-once completions with every integrity result
true. Its Drone Support was deliberately placed at `(10,10) / (10,11) /
(11,10) / (11,11)` to make the new Holding behavior visible. Because that is
not the frozen `_02` Reference placement, V3 is lifecycle and progression
diagnostic evidence rather than an accepted Stage6 balance comparison.

### 3.7 Resumed V4 Reference Fixture

The first resumed run changes no Stage6 authored value from V3. It isolates the
accepted Holding runtime by restoring the frozen `_02` deployment order and all
five placements:

| Deployment | Tower role | Occupied cells |
|---:|---|---|
| 1 | Archer Support | `(7,8)`, `(7,9)` |
| 2 | Magic Core | `(8,6)` |
| 3 | Cannon Core | `(6,5)`, `(7,4)`, `(7,5)` |
| 4 | Drone Support | `(5,8)`, `(5,9)`, `(6,8)`, `(6,9)` |
| 5 | Magic Support | `(8,3)` |

V4 uses Player Health `6`, Progress Requirements
`[3,3,5,5,5,5,5,5,5,5,6,6,6,6]`, the existing twelve-Wave `92`-Monster
Profile ramp, `2.5s` Spawn Interval, and Wave Delays
`4/6/5/4/4/4/4/4/4/4/4/4`. The Fixed Draft sequence remains the fifteen-step
Reference result sequence in section 3.1. Its current Draft nodes are
`0/3/6/11/16/21/26/31/36/41/46/52/58/64/70`; the older nodes shown in the
Phase A table remain provisional-history labels only.

Use Recorder Run Name
`Task015_PhaseB_Stage6V4_Reference_Placement02_MagicFire_CannonFire_Node70_HP6_W12_M92_Progress3-3-5-5_ProfileRamp_HoldingAccepted_Schema24_01`.
Forced relocation remains diagnostic, so the Recorder expectation is `Ignore`.

## 4. Stage Calibration And Monster Authoring

Author fourteen positive Player Progress Requirements. Their sum owns the final-
Draft resolution node; the total Monster budget must leave meaningful combat for
the completed matching-Element Build.

Reuse fixed-speed Profiles accepted by Task010-Task014 whenever they can express
the required shared-target, Overload, and late-pressure roles. Add a new Profile
only for an unmet need, recording explicit HP with `MoveSpeed = 0.25` and standard
`SpawnInterval = 2.5s`. Upstream Profiles remain immutable unless their
introducing Stage and affected dependents are reopened.

Record the complete `MonsterWaveConfig` table and jointly calibrate Progress,
Profile order and Count, any new HP, `WaveDelay`, Player Health, and overlapping
Reference placement. Do not weaken Waves to hide an impossible Draft or
placement path.

## 5. Acceptance

- both Elemental Cores contribute to shared targets;
- source-scoped cooldown and shared stacks produce reviewed Overloads;
- Reference and a coherent matching alternative clear within the accepted
  margin;
- unmatched, single-source, or non-overlapping builds fail for the intended
  cooperation gap;
- post-final-Draft pressure validates the completed build;
- all fifteen Drafts occur at reviewed pressure nodes and leave a meaningful
  matching-Core cooperation window;
- exact Stage pools, Level 3 reachability, legal matching paths, Build fixtures,
  and Fixed sequences are accepted inside this Task;
- every Profile is identified as reused or first accepted here and the accepted
  Wave table matches the authored `MonsterWaveConfig`;
- Recorder proves source identities, Buff lifecycle, TowerScaled damage, and
  FixedBuff damage independently.

All Build comparisons use Fixed Draft sequences. Whether natural Drafts make a
coherent matching path sufficiently available is accepted later by Task017.
