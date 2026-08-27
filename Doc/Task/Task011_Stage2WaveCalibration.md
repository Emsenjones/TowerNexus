# Task011 - Stage2 Wave Calibration

Status: Pending; Reference Build v1 and Stage2 V1 Wave candidate are frozen, but
Phase A is blocked by Task010A Monster Placement Route Continuity Refactor

Depends on: Accepted Task010 Stage1 Wave Calibration; completed and accepted
Task010A Monster Placement Route Continuity Refactor

Blocks: Task012, Task016, and Task017

The first Phase A attempt exposed visible closest-Grid mass reprojection during
Tower placement and is not balance evidence. Preserve the frozen Stage2
Reference, Wave candidate, and placement intent without further calibration.
After Task010A accepts schema 22 and the Stage1 movement regression, return this
Task to `In Progress`, update the Phase A RunName, and execute a fresh run.

## 1. Goal

Calibrate Stage2 to require a coherent Archer/Cannon role-complement build rather
than uniform undeveloped expansion or one over-concentrated local answer.

## 2. Fixed Design Intent

- Archer and Cannon pools;
- six-Draft candidate budget;
- one L2 Cannon Core with one Basic and one Behaviour Upgrade;
- two L1 Supports, including Archer;
- legal alternative may exchange the repeated Support family or Core package;
- Anti-patterns omit role complement through undeveloped spread or excessive
  concentration.

## 3. Stage Composition And Fixed Build Fixtures

- Derive exactly six Drafts from three Tower placements, one Core Level Up, and
  two Core Upgrade applications.
- Author the exact cumulative Archer/Cannon Tower and Upgrade pools and prove
  continuous Level 2 reachability for every family permitted to reach it.
- Select the repeated Support family, exact Reference Core package, coherent
  alternative, undeveloped-spread Anti-pattern, over-concentration Anti-pattern,
  and their legal placement assumptions.
- Author one six-step naturally eligible Fixed Draft sequence for every required
  Build. The first step is Tower-only; later steps preserve the normal candidate,
  Pending, placement, Level Up, Upgrade, and consumption boundaries.

Fixed sequences prove Build efficacy and constructibility, not natural Draft
probability or player preference. Task017 owns natural offer accessibility.

### 3.1 Implementation Kickoff Snapshot

The live Stage2 skeleton already provides the cumulative Archer/Cannon Tower
pool, all six Archer and all six Cannon Basic/Behaviour UpgradeDefinitions, and
continuous Level 2 eligibility for both families. Its remaining values are
provisional inputs rather than accepted calibration:

- Player Max Health: `16`;
- Progress Requirements: `[3, 3, 4, 4, 8]`, producing Draft nodes
  `3 / 6 / 10 / 14 / 22`;
- Wave table: three same-Profile Waves with counts `11 / 11 / 10`, Delay `5s`,
  and Spawn Interval `2.5s`;
- total Monsters: `32`, leaving ten resolutions after the final Draft node.

Task011 will replace the provisional Wave composition with Stage1-accepted
fixed-speed Profiles before judging Stage2 pressure. The existing Progress
sequence and Player Health are candidates only; neither is accepted from the
skeleton.

### 3.2 Frozen Reference Build v1

The accepted first Reference preserves the two Archer coverage points learned
in Stage1 and adds Cannon as the new developed Core:

| Role | Count | TowerFamily | Final state |
|---|---:|---|---|
| Core | `1` | Cannon | L2, Faster Reload, Explosive Shell |
| Support | `2` | Archer | L1, no Upgrades |

`Faster Reload + Explosive Shell` is the Task004 fixed-condition Cannon package
with an accepted `2.108x` measurement-Wave gain over its same-level control. It
combines Cannon cadence improvement with bounded area conversion while leaving
Archer responsible for the faster coverage role. Two Archer Supports make the
campaign transition legible: retain the Stage1 coverage structure, then add and
develop the newly introduced Cannon rather than changing every role at once.

Six-step Fixed Draft sequence:

| Draft | Resolution node | Fixed result | Commit target | Resulting Build state |
|---:|---:|---|---|---|
| 1 | `0` | Cannon Tower | Deploy Core | Cannon Core L1 |
| 2 | `3` | Cannon Tower | Level Core | Cannon Core L2 |
| 3 | `6` | Archer Tower | Deploy Support A | Core L2 + one Archer L1 |
| 4 | `10` | Archer Tower | Deploy Support B | Core L2 + two Archer L1 |
| 5 | `14` | Faster Reload | Apply to Core | Core with Basic + two Supports |
| 6 | `22` | Explosive Shell | Apply to Core | Complete Reference Build |

The first placement-sanity run selects three deliberate route-covering
footprints and freezes their Recorder-observed cells for Reference repeats. The
first coherent Alternative should then exchange either the Cannon package or
the repeated Support family, but not both in the same comparison.

### 3.3 Stage2 V1 Fixed-Speed Wave Candidate

Task010's accepted Stage1 Wave used three formal fixed-speed Profiles, all of
which are reused here without changing HP or Move Speed:

| Profile | HP | Move Speed | Stage1 status | Stage2 V1 role |
|---|---:|---:|---|---|
| Slime Lv1 | `60` | `0.25` | Used and accepted | Opening Cannon one-hit feedback |
| Monster Plant Lv3 | `180` | `0.25` | Used and accepted | Mid-stage developed-Cannon pressure |
| Turtule Shell Lv4 | `400` | `0.25` | Used and accepted | Late Core/Support role-complement pressure |

Bat Lv2 (`HP120`, Move Speed `0.35`) and Orc Lv5 (`HP480`, Move Speed `0.2`)
exist in the formal roster but were not used by the accepted Stage1 Wave. Their
movement identities belong to Task016 and are excluded from Task011's initial
fixed-speed calibration. Stage2 V1 therefore introduces zero new Monster
Profiles.

The first Wave candidate keeps the skeleton's accepted structural budget of 32
Monsters and maps its cumulative counts directly to the five Progress nodes:

| Wave | Profile | HP | Count | Spawn Interval | Wave Delay | Cumulative Monsters | Intended Draft milestone |
|---:|---|---:|---:|---:|---:|---:|---|
| 1 | Slime Lv1 | `60` | `3` | `2.5s` | `4s` | `3` | Level Cannon Core to L2 |
| 2 | Slime Lv1 | `60` | `3` | `2.5s` | `6s` | `6` | Deploy Archer Support A |
| 3 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `5s` | `10` | Deploy Archer Support B |
| 4 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `14` | Apply Faster Reload |
| 5 | Turtule Shell Lv4 | `400` | `8` | `2.5s` | `4s` | `22` | Apply Explosive Shell |
| 6 | Turtule Shell Lv4 | `400` | `10` | `2.5s` | `4s` | `32` | Ten-Monster post-Build measurement |

Phase A retains the skeleton Player Health `16` as a measurement ceiling so a
weak placement or first-pass Wave mismatch does not truncate the per-Monster
record. It is not an acceptance value. After the Reference placement and
pressure shape are readable, Player Health is reduced to the reviewed leak
allowance rather than used to create the role-complement gap.

Phase A Run Name:

```text
Task011_PhaseA_Stage2V1_ReferencePlacementAndPressure_CannonL2_FasterReload_ExplosiveShell_ArcherL1x2_Lv1HP60_Lv3HP180_Lv4HP400_PlacementCandidate_Schema21_01
```

Deploy the Cannon Core at one deliberate high-value Stage2 footprint, apply the
second Cannon Draft to that same Tower, and place both Archer Supports at two
other deliberate route-covering footprints. Normal interaction speed is
required. This first run discovers placement; its Recorder-observed cells are
included in the repeat RunName after review.

## 4. Stage Calibration And Monster Authoring

1. Author five positive Player Progress Requirements.
2. Use their sum as the final-Draft resolution node, then provide a meaningful
   post-final-Draft Monster budget.
3. Test the fixed-speed Profiles accepted by Task010 before adding anything.
   Reuse them when they can express Stage2's opening, role-complement, and late
   pressure. Add a new HP Profile only when those accepted Profiles cannot.
4. Any new Profile uses `MoveSpeed = 0.25`. Standard Waves use
   `SpawnInterval = 2.5s` and the `0.625` spatial gap.
5. Record the exact `MonsterWaveConfig` table and jointly calibrate Progress
   requirements, Profile order and Count, HP for any newly introduced Profile,
   `WaveDelay`, Player Health, and legal placement.

An accepted upstream Profile is immutable here. Changing it explicitly reopens
its introducing Stage and every accepted dependent Stage. A Profile first
accepted here becomes available to Task012-Task015.

## 5. Acceptance

- opening permits the three-Tower route structure to form;
- mid/late pressure gives both Archer and Cannon meaningful work;
- Cannon development matters against higher-HP bodies;
- Reference and coherent alternative clear within the accepted leak margin;
- builds that reject role complement do not reliably clear;
- all six Drafts occur at reviewed pressure nodes with meaningful combat after
  the final Draft;
- the exact Stage pools, Level reachability, legal Builds, placements, and Fixed
  sequences are accepted inside this Task;
- every Profile is recorded as reused or first accepted here, and every Profile
  remains fixed at `0.25` MoveSpeed;
- the accepted Wave table matches the authored `MonsterWaveConfig`;
- per-Wave and per-Tower Recorder attribution supports the decision.

All Build comparisons use Fixed Draft sequences. Natural Tower-versus-Upgrade
offer ratios and player-choice interpretation remain Task017 work.
