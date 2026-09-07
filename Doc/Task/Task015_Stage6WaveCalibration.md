# Task015 - Stage6 Wave Calibration

Status: Completed

Completed: 2026-09-03

Depends on: Accepted Task014 Stage5 Wave Calibration

Resumed after: Completed Task015A Drone Holding Lifecycle Refactor

Unblocks: Task017 fixed-speed natural Draft-offer calibration

Task016 Fast-Monster substitution remains a separately deferred follow-up. Any
future Task016 substitution must rerun the affected Stage6 combat fixtures and
the affected Task017 probability cohort; it does not invalidate this accepted
fixed-speed baseline by omission.

## 1. Goal And Result

Task015 accepts Stage6 as the first Stage where two developed Elemental Towers
can contribute to the same Buff lifecycle. It calibrates the complete Stage
contract: Map pressure, Player Progress, Player Health, cumulative Draft pools,
five-Tower Builds, fixed-speed Monster Profiles, all twelve Waves, placement,
and positive and negative Build boundaries.

Matching Elemental sources and overlapping effective coverage create
shared-stack and multi-source Overload value, but matching Elements are not the
only valid strategy. A tested Archer Cold plus Magic Fire Build cleared through
complementary control and damage and is accepted as a coherent alternative, not
an Anti-pattern.

All Build comparisons use Fixed Draft sequences. Task017 separately owns how
often natural Draft offers expose a viable path.

## 2. Accepted Stage Contract

| Property | Accepted value |
|---|---|
| Stage | `StageDefini_Lv6` / `Level6` |
| Map | `Prefab_Map_Stage6`, `14x14` |
| Player Health | `6` |
| Progress Requirements | `[3,3,4,4,4,4,4,4,4,4,4,4,5,5]` |
| Draft count | `15`: one Initial plus fourteen Level Up Drafts |
| Draft resolution nodes | `0/3/6/10/14/18/22/26/30/34/38/42/46/51/56` |
| Final-Draft position | node `56` of `75` resolutions, or `74.7%` |
| Post-final-Draft pressure | `19` Monster resolutions |
| Tower pool | Archer, Cannon, Magic, Drone |
| Upgrade pool | all `12` Basic, `12` Behaviour, and `16` Elemental identities |
| Monster count | `75` across `12` Waves |
| Total Monster HP | `50,060` |
| Move Speed | `0.25` for every accepted Profile |
| Spawn Interval | `2.5s` for every Wave |
| Positive margin | Victory with `0-4` leaks |
| Negative boundary | Defeat on the sixth leak after the intended weakness is exposed |

The fifteen-Draft budget supports five deployments, four Core Level Ups, and
six Core Upgrade applications. The cumulative pools preserve continuous
L1-to-L2-to-L3 reachability for every family. Fixed authoring preserves normal
eligibility, Pending reservation, placement, Level Up, Upgrade, Elemental
exclusivity, and consumption rules.

## 3. Accepted Monster And Wave Fixture

| Wave | Runtime template | HP | Count | Interval | Delay | Cumulative |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Slime Lv1 | `60` | `3` | `2.5s` | `4s` | `3` |
| 2 | Slime Lv1 | `60` | `3` | `2.5s` | `6s` | `6` |
| 3 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `10` |
| 4 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `14` |
| 5 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `18` |
| 6 | Monster Plant Lv3 | `180` | `8` | `2.5s` | `4s` | `26` |
| 7 | Turtle Shell Lv4 | `400` | `8` | `2.5s` | `4s` | `34` |
| 8 | Turtle Shell Lv4 | `400` | `10` | `2.5s` | `4s` | `44` |
| 9 | Evil Mage Lv5 | `520` | `10` | `2.5s` | `4s` | `54` |
| 10 | Skeleton Lv6 | `1100` | `8` | `2.5s` | `4s` | `62` |
| 11 | Orc Lv7 | `1700` | `9` | `2.5s` | `4s` | `71` |
| 12 | Golem Lv8 | `2400` | `4` | `2.5s` | `20s` | `75` |

Slime, Monster Plant, Turtle Shell, Evil Mage, Skeleton, and Orc retain the
fixed-speed identities consumed from Task010-Task014. Task015 accepts Golem Lv8
as the new capstone identity at HP `2400` and Move Speed `0.25`. Skeleton Lv6
remains HP `1100`; Orc Lv7 remains HP `1700`.

Four Golems preserve a visible HP increase over Orc without creating a long
eight-body capstone queue. The `20s` delay gives the last fixed investment time
to commit while keeping the pressure in the Monsters rather than a longer pause.

## 4. Fixed Build Fixtures

### 4.1 Fire Reference

| Role | Final state |
|---|---|
| Magic Core | L3; Arcane Charge; Twin Orbs; Blazing Orbs |
| Cannon Core | L3; Faster Reload; Explosive Shell; Blazing Shells |
| Archer Support | L1; no Upgrade |
| Drone Support | L1; no Upgrade |
| Magic Support | L1; no Upgrade |

| Draft | Node | Fixed result | Intended commit |
|---:|---:|---|---|
| 1 | `0` | Archer Tower | Deploy Archer Support |
| 2 | `3` | Magic Tower | Deploy Magic Core |
| 3 | `6` | Magic Tower | Magic Core to L2 |
| 4 | `10` | Cannon Tower | Deploy Cannon Core |
| 5 | `14` | Magic Tower | Deploy Magic Support |
| 6 | `18` | Drone Tower | Deploy Drone Support |
| 7 | `22` | Arcane Charge | Apply to Magic Core |
| 8 | `26` | Twin Orbs | Apply to Magic Core |
| 9 | `30` | Magic Tower | Magic Core to L3 |
| 10 | `34` | Blazing Orbs | Apply to Magic Core |
| 11 | `38` | Cannon Tower | Cannon Core to L2 |
| 12 | `42` | Faster Reload | Apply to Cannon Core |
| 13 | `46` | Explosive Shell | Apply to Cannon Core |
| 14 | `51` | Cannon Tower | Cannon Core to L3 |
| 15 | `56` | Blazing Shells | Apply to Cannon Core |

Accepted StrategicPlacement01:

| Deployment | Role | Occupied cells |
|---:|---|---|
| 1 | Archer Support | `(7,8)`, `(7,9)` |
| 2 | Magic Core | `(8,6)` |
| 3 | Cannon Core | `(6,5)`, `(7,4)`, `(7,5)` |
| 4 | Magic Support | `(7,2)` |
| 5 | Drone Support | `(10,4)`, `(10,5)`, `(11,4)`, `(11,5)` |

The final route contains `29` cells and required no forced relocation.
Deployment-order variation is not itself a failure when the same legal
strategic formation and final Build are achieved.

### 4.2 Matching Cold Alternative

| Role | Final state |
|---|---|
| Archer Core | L3; Quick Draw; Scatter Arrow; Frostbound Arrows |
| Magic Core | L3; Faster Orbit; Arcane Field; Frostbound Orbs |
| Archer Support | L1; no Upgrade |
| Cannon Support | L1; no Upgrade |
| Drone Support | L1; no Upgrade |

| Draft | Node | Fixed result | Intended commit |
|---:|---:|---|---|
| 1 | `0` | Archer Tower | Deploy Archer Support |
| 2 | `3` | Cannon Tower | Deploy Cannon Support |
| 3 | `6` | Archer Tower | Deploy Archer Core |
| 4 | `10` | Magic Tower | Deploy Magic Core |
| 5 | `14` | Archer Tower | Archer Core to L2 |
| 6 | `18` | Drone Tower | Deploy Drone Support |
| 7 | `22` | Quick Draw | Apply to Archer Core |
| 8 | `26` | Scatter Arrow | Apply to Archer Core |
| 9 | `30` | Archer Tower | Archer Core to L3 |
| 10 | `34` | Frostbound Arrows | Apply to Archer Core |
| 11 | `38` | Magic Tower | Magic Core to L2 |
| 12 | `42` | Faster Orbit | Apply to Magic Core |
| 13 | `46` | Arcane Field | Apply to Magic Core |
| 14 | `51` | Magic Tower | Magic Core to L3 |
| 15 | `56` | Frostbound Orbs | Apply to Magic Core |

Accepted concentrated placement:

| Deployment | Role | Occupied cells |
|---:|---|---|
| 1 | Archer Support | `(7,8)`, `(7,9)` |
| 2 | Cannon Support | `(7,5)`, `(8,4)`, `(8,5)` |
| 3 | Archer Core | `(10,4)`, `(10,5)` |
| 4 | Magic Core | `(10,3)` |
| 5 | Drone Support | `(6,1)`, `(6,2)`, `(7,1)`, `(7,2)` |

The final route contains `31` cells and required no forced relocation. Two
exact Node56 repeats both ended at `73` kills, `2` leaks, and Health `4`.

### 4.3 Complementary Mixed-Element Alternative

The mixed alternative uses the Cold fixture unchanged through Draft 14, then
selects Blazing Orbs instead of Frostbound Orbs at Draft 15. Placement is
unchanged. Archer supplies Cold control while Magic independently supplies
Burning and Fire Overload damage.

This Build was originally named `AntiUnmatched`, but it cleared at `74` kills,
`1` leak, and Health `5`. It produced no cross-Element hit reaction. Archer Cold
reached at most nine stacks without Overload; Magic Fire independently produced
ten single-source Overloads and `230` Burning periodic ticks. This evidence
supersedes the provisional assumption that unmatched Elements must fail.

### 4.4 Single-Source Anti-pattern

This fixture uses the Cold sequence through Draft 14, then applies Faster Reload
to the Cannon Support instead of giving Magic an Elemental Upgrade. Archer is
the only Elemental Core; Magic reaches L3 with Faster Orbit and Arcane Field.

It preserves all fifteen investments and the concentrated placement. It clears
Waves 1-10, then fails on the sixth Orc leak in Wave 11. Only one single-source
Cold Overload occurred.

### 4.5 Non-overlap Anti-pattern

This fixture restores the complete matching Cold Build and keeps all placements
except Magic Core, which moves from `(10,3)` to `(11,9)`. Both Cores remain
positioned against different route sections but no longer form the concentrated
firepower network.

Compared with the accepted Cold repeat, shared damage-route cells fall from `8`
to `1`, shared damaged Monsters from `42` to `18`, and two-source Cold Overloads
from `9` to `0`. Archer Core damage remains close (`18,973 -> 18,645`), while
Magic Core damage falls from `20,570` to `13,528` as its coverage falls from
`42` to `20` Monsters. Five Wave 10 Skeletons and one Wave 11 Orc trigger defeat.

This is a spatial package boundary: it demonstrates the combined loss of shared
stacks, multi-source Overload, and concentrated kill conversion. The
Single-source fixture separately isolates the missing second Elemental source
without changing placement.

## 5. Final Acceptance Evidence

All final records use schema `24`, the actual HP6/Node56/75-Monster fixture,
Fixed mode, fifteen selections, fifteen investments, and zero failed integrity
flags.

| Fixture | Result | Kills | Leaks | Unresolved | HP | Damage | Overloads | Two-source |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Fire Reference | Victory | `72` | `3` | `0` | `3` | `49,259` | `13` | `6` |
| Matching Cold `_01` | Victory | `73` | `2` | `0` | `4` | `48,069` | `11` | `9` |
| Matching Cold `_02` | Victory | `73` | `2` | `0` | `4` | `49,173` | `14` | `9` |
| Mixed Cold + Fire | Victory | `74` | `1` | `0` | `5` | `49,783` | `10` Fire | `0` |
| Single-source Cold | Defeat | `66` | `6` | `3` | `0` | `44,000` | `1` | `0` |
| Non-overlap Cold | Defeat | `60` | `6` | `9` | `0` | `41,567` | `9` | `0` |

Accepted record basenames:

- `Task015_PhaseD_Stage6V1_ReferenceRevalidation_StrategicPlacement01_MagicFire_CannonFire_Node56_HP6_W12_M75_W12Delay20_Skeleton1100x8_Orc1700x9_Golem2400x4_Progress3x2-4x10-5x2_Schema24_01.json`
- `Task015_PhaseC_Stage6V2_CoherentAlternative_ArcherSupport_ArcherCold_MagicCold_Node56_HP6_W12_M75_W12Delay20_Skeleton1100x8_Orc1700x9_Golem2400x4_Progress3x2-4x10-5x2_Schema24_01.json`
- `Task015_PhaseC_Stage6V2_CoherentAlternative_ArcherSupport_ArcherCold_MagicCold_Node56_HP6_W12_M75_W12Delay20_Skeleton1100x8_Orc1700x9_Golem2400x4_Progress3x2-4x10-5x2_Schema24_02.json`
- `Task015_PhaseE_Stage6V1_AntiUnmatched_ArcherSupport_ArcherCold_MagicFire_Node56_HP6_W12_M75_W12Delay20_Skeleton1100x8_Orc1700x9_Golem2400x4_Progress3x2-4x10-5x2_Schema24_01.json`
- `Task015_PhaseE_Stage6V2_AntiSingleSource_ArcherCold_MagicNoElement_Node56_HP6_W12_M75_W12Delay20_Skeleton1100x8_Orc1700x9_Golem2400x4_Progress3x2-4x10-5x2_Schema24_0.json`
- `Task015_PhaseF_Stage6V1_AntiNonOverlap_SeparatedCores01_ArcherCold_MagicCold_Node56_HP6_W12_M75_W12Delay20_Skeleton1100x8_Orc1700x9_Golem2400x4_Progress3x2-4x10-5x2_Schema24_01.json`

The Single-source basename ends in `Schema24_0`; its internal `runLabel` agrees
and its schema is `24`. This cosmetic suffix does not justify a balance rerun.

## 6. Superseded Calibration History

Phase A used HP45, 89 Slimes, and node70 only as a measurement ceiling. Early
Phase B records explored HP6, the twelve-Wave Profile ramp, Drone Holding,
strategic placement, Burning visibility, Golem HP/count, Wave 12 delay, and
final Draft nodes `64` and `62`.

V16 froze the current 75-Monster Wave body at node62 and produced two Reference
Victories with `73` kills and `2` leaks. The first complete Cold alternative
showed that node62 completed too late: its final package arrived at the Wave 12
boundary and repeatedly failed despite a sound Build and placement. Moving the
final Draft to node56 preserved Wave/HP pressure while leaving nineteen reviewed
resolutions. Earlier values remain diagnostic rather than accepted fixtures.

No global Tower, Upgrade, Buff, Effect, or runtime value is changed by the final
Stage calibration. Task015A remains the owner of the Drone Holding lifecycle
correction discovered during this work.

## 7. Acceptance

- the Fire Reference and two matching Cold repeats clear inside the `0-4` leak
  margin;
- different matching Elemental sources reach shared Monsters and produce
  reviewed multi-source Overloads;
- complementary Cold plus Fire remains a valid alternative rather than being
  weakened to force one preferred solution;
- equal-budget Single-source and Non-overlap fixtures fail on the sixth leak for
  their intended missing-cooperation and spatial-network weaknesses;
- all fifteen Fixed Drafts are eligible under normal Draft rules at their
  authored nodes and leave
  nineteen possible post-final-Draft resolutions in a complete run;
- authored StageDefinition, Profile HP, `MonsterWaveConfig`, placements, and
  Recorder fixture snapshots agree;
- every final Recorder integrity result passes;
- Task017 owns natural-offer accessibility and must not reinterpret Fixed
  outcomes as probability evidence;
- Task016 remains deferred and must consume this fixed-speed Stage as its
  accepted baseline if Fast-Monster substitutions resume.
