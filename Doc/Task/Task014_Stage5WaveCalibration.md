# Task014 — Stage 5 Wave Calibration

> Status: **Completed**
>
> Accepted: **2026-09-03**
>
> Historical acceptance: **2026-09-01**
>
> Depends on: Task013 Stage 4 Wave Calibration
>
> Unblocks: Task015 Stage 6 Wave Calibration, Task016 Fast-Monster Wave
> Substitution, and Task017 Natural Draft Offer Probability Calibration

---

## 1. Goal And Accepted Interpretation

Freeze Stage 5 as the first executable Stage contract that requires one
coherent Elemental Core without requiring two-source Elemental cooperation.

The accepted positive envelope contains a reproducible Fire Reference and a
coherent Cold alternative. Both use one Magic L3 Core with one Basic, one
Behaviour, and one Elemental Upgrade, plus four L1 Supports representing all
four TowerFamilies. The Reference is a positive control rather than the only
valid package.

The primary Anti-pattern spends the same ten-Draft budget across all four
TowerFamilies but never completes an L3 Elemental Core. Its failure measures
fragmented investment rather than illegal placement, missing Drafts, or an
artificially weakened opening.

All comparisons use Fixed Draft sequences. Task017 separately owns natural
offer frequency, Build accessibility, and player-choice interpretation.

The 2026-09-01 Stage5 V5 acceptance remains the historical comparison
baseline. Task014 was reopened on 2026-09-03 because Task007 increased the
accepted normal and Overload value of the Elemental package used by the
Reference and coherent alternative. The unchanged Fire Reference first proved
that V5 pressure had become too low. Calibration then changed only the
Stage5-specific Skeleton Lv6 and Orc Lv7 HP before rerunning the full positive
and negative Build envelope. The accepted V9 records restore the intended
separation without changing Stage assets, Player Health, Progress, Wave order,
counts, timing, Fixed Drafts, or locked placement.

### 1.1 Authority And Scope

This Task derives the Stage-specific values from
`Doc/Balance/00_StageDesignBlueprint.md` and consumes the stable ownership
contracts in Stage, Player, Monster, Draft, Tower Upgrade, Effect, and Buff
System documents. Task014 owns the exact Stage5 asset composition, Fixed
fixtures, calibration evidence, and downstream acceptance boundary; it does
not redefine those systems' runtime rules.

Natural Draft probability, fast-Monster substitution, and two-source matching
Element cooperation remain out of scope. They are owned by Task017, Task016,
and Task015 respectively. No runtime code change is accepted through this
calibration Task.

---

## 2. Accepted Stage 5 V9 Contract

| Field | Accepted value |
|---|---|
| Stage asset | `StageDefini_Lv5` |
| Map | `Prefab_Map_Stage5` |
| Player Max Health | `6` |
| Total Draft opportunities | `10` |
| Initial Draft node | `0` |
| Post-initial Draft nodes | `3 / 6 / 10 / 14 / 18 / 26 / 34 / 44 / 54` |
| Progress Requirements | `[3, 3, 4, 4, 4, 8, 8, 10, 10]` |
| Tower pool | Archer / Cannon / Magic / Drone |
| Upgrade pool | All `40` unlocked definitions: `12` Basic, `12` Behaviour, and `16` Elemental |
| Monster count | `76` |
| Total authored Monster HP | `47,760` |
| Move Speed | `0.25` for every Profile |
| Spawn Interval | `2.5s` for every Wave |
| Wave delays | `4 / 6 / 5 / 4 / 4 / 4 / 4 / 4 / 4 / 4 / 4` |
| Final Draft boundary | Node `54`, leaving `22` Monster resolutions for completed-Core verification |
| Positive acceptance | Reference and coherent alternative reach Victory with `0–5` leaks |
| Negative boundary | Fragmented four-family investment reaches Defeat on the sixth leak |

Player Health was selected only after the Build envelope was measured with a
temporary `38`-Health ceiling. It is the accepted execution margin, not the
source of the power difference: the final HP6 positives clear W10 and survive
W11, while the Anti-pattern begins leaking in W10 and cannot finish W11.

### 2.1 Draft Pools And Continuous L3 Reachability

`StageDefini_Lv5` contains all four TowerDefinitions. For each TowerFamily the
Upgrade pool contains:

- three Basic definitions requiring L1;
- three Behaviour definitions requiring L2;
- four Elemental definitions requiring L3, one each for Fire, Cold, Electric,
  and Wind.

This produces `10` UpgradeDefinitions per family and `40` total. The `16`
Elemental definitions are introduced by Stage 5; no new TowerFamily is
introduced.

Every represented TowerDefinition authors L1, L2, and L3 Tower Level configs.
A deployed Tower can therefore receive two same-family Tower Draft results to
advance continuously from L1 to L2 to L3. Basic content is eligible at L1,
Behaviour content at L2, and Elemental content at L3 for Archer, Cannon, Magic,
and Drone. The accepted Fixed sequences exercise this normal Tower-only
Initial Draft, level, Pending, placement, Upgrade, exclusivity, and consumption
flow without adding a calibration-only eligibility path.

### 2.2 Accepted Unity Authoring Checklist

- `StageDefini_Lv5` authors HP `6`, the nine accepted Progress Requirements,
  `Prefab_Map_Stage5`, `Config_MonsterWave_Lv5`, four TowerDefinitions, all
  `40` unlocked UpgradeDefinitions, and the `16` Stage5 introductions.
- `Config_MonsterWave_Lv5` authors the exact eleven-Wave table in Section 3.
- Evil Mage Lv5, Skeleton Lv6, and Orc Lv7 author HP `520 / 1100 / 1700` and
  Move Speed `0.25` through independent prefab identities.
- Recorder Fixed Draft configuration authors the three ten-step fixtures in
  Section 5 and uses the locked placement in Section 4.
- `Config_MonsterWave_Default`, `Config_MonsterWave_Lv4`, and the accepted
  Stage4 Orc Lv5 remain unchanged from their upstream contracts.

---

## 3. Accepted Monster Profiles And Wave Table

Stage 5 reuses these Task010-accepted fixed-speed Profiles unchanged:

- Slime Lv1: HP `60`;
- Monster Plant Lv3: HP `180`;
- Turtule Shell Lv4: HP `400`.

Task014 first accepts these additional formal Profiles, all at Move Speed
`0.25`:

- Evil Mage Lv5: HP `520`, an equal-HP Lv5 peer rather than a mutation of the
  accepted Stage4 Orc Lv5;
- Skeleton Lv6: HP `1100`;
- Orc Lv7: HP `1700`.

The Stage4 Orc Lv5 remains HP `520` in `Config_MonsterWave_Lv4`. The Stage5
Evil Mage Lv5 and Orc Lv7 use independent prefab identities, so Stage5 roster
growth does not rewrite accepted upstream Wave assets or Profiles.

| Wave | Profile | HP | Count | Spawn interval | Delay | Cumulative resolutions |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Slime Lv1 | `60` | `3` | `2.5s` | `4s` | `3` |
| 2 | Slime Lv1 | `60` | `3` | `2.5s` | `6s` | `6` |
| 3 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `5s` | `10` |
| 4 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `14` |
| 5 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `18` |
| 6 | Monster Plant Lv3 | `180` | `8` | `2.5s` | `4s` | `26` |
| 7 | Turtule Shell Lv4 | `400` | `8` | `2.5s` | `4s` | `34` |
| 8 | Turtule Shell Lv4 | `400` | `10` | `2.5s` | `4s` | `44` |
| 9 | Evil Mage Lv5 | `520` | `10` | `2.5s` | `4s` | `54` |
| 10 | Skeleton Lv6 | `1100` | `10` | `2.5s` | `4s` | `64` |
| 11 | Orc Lv7 | `1700` | `12` | `2.5s` | `4s` | `76` |

The authored `Config_MonsterWave_Lv5` and final schema-24 fixture snapshots
match this table.

---

## 4. Locked Placement Contract

All final Build comparisons use the same strategically reviewed placement.
The listed cells are the complete occupied logical-grid footprints recorded by
the Recorder:

| Role | Tower | Occupied cells |
|---|---|---|
| Core | Magic | `(4,5)` |
| Support | Cannon | `(9,5), (10,4), (10,5)` |
| Support | Archer | `(6,4), (6,5)` |
| Support | Drone | `(6,8), (6,9), (7,8), (7,9)` |
| Support | Magic | `(9,6)` |

No final fixture uses intentionally poor placement. The Fire Reference records
no forced relocation. The Cold alternative records two legal
`CoveredByNewFootprint` forced relocations and one reachable route rejoin under
the explicit `Ignore` expectation; the Anti-pattern records no forced
relocation under the same expectation. These outcomes are consequences of
placing the accepted footprints while Monsters are alive, not balance penalties
or illegal fixture changes. Forced-relocation usage, placement-route topology,
gameplay-state preservation, combat ownership, and lifecycle integrity all
pass.

---

## 5. Fixed Draft Build Fixtures

The first six Drafts establish the same five-Tower, four-family roster in every
fixture. Positive fixtures then complete one Magic L3 Elemental Core; the
Anti-pattern distributes the remaining investments without reaching L3 or any
Elemental Upgrade.

| Draft | Node | Reference | Coherent Alternative A | Fragmented four-family Anti-pattern |
|---:|---:|---|---|---|
| 1 | `0` | Deploy Magic Core | Deploy Magic Core | Deploy Magic Core |
| 2 | `3` | Magic Core to L2 | Magic Core to L2 | Magic Core to L2 |
| 3 | `6` | Deploy Cannon Support | Deploy Cannon Support | Deploy Cannon Support |
| 4 | `10` | Deploy Archer Support | Deploy Archer Support | Deploy Archer Support |
| 5 | `14` | Deploy Drone Support | Deploy Drone Support | Deploy Drone Support |
| 6 | `18` | Deploy Magic Support | Deploy Magic Support | Deploy Magic Support |
| 7 | `26` | Arcane Charge on Core | Faster Orbit on Core | Faster Orbit on Magic Core |
| 8 | `34` | Twin Orbs on Core | Arcane Field on Core | Cannon Support to L2 |
| 9 | `44` | Magic Core to L3 | Magic Core to L3 | Sharpened Arrows on Archer |
| 10 | `54` | Blazing Orbs on Core | Frostbound Orbs on Core | Optimized Burst Module on Drone |

Final allocations:

| Fixture | Final Build |
|---|---|
| Reference | Magic L3 with Arcane Charge, Twin Orbs, and Blazing Orbs; Cannon, Archer, Drone, and Magic Supports at L1 |
| Coherent Alternative A | Magic L3 with Faster Orbit, Arcane Field, and Frostbound Orbs; the same four L1 Supports |
| Fragmented Anti-pattern | Magic L2 with Faster Orbit; Cannon L2; Archer L1 with Sharpened Arrows; Drone L1 with Optimized Burst Module; Magic Support L1; no Behaviour, Elemental, or L3 Core |

---

## 6. Elemental And Support Evidence

Both positive fixtures complete their Elemental package at node `54` and then
resolve all `22` remaining Monsters.

Reference Fire evidence in the accepted schema-24 V9 run:

- Blazing Orbs applies Burning across all `22` W10-W11 Monsters;
- `192` stack units are applied, maximum observed stacks reach `10`, and `12`
  single-source Overloads occur;
- Burning contributes `12,050` FixedBuff damage, including periodic and
  Overload damage;
- TowerScaled and FixedBuff attribution remain separately recorded.

Coherent Cold evidence in the accepted schema-24 V9 run:

- Frostbound Orbs applies Chilled across all `22` W10-W11 Monsters;
- `300` stack units are applied, maximum observed stacks reach `10`, `19`
  single-source Overloads occur, and Frozen is applied `19` times across `18`
  Monsters;
- the coherent package contributes `38,207` TowerScaled Core damage.

Every recorded Overload has `sourceCount = 1`, and both final positive records
observe zero cross-Element reaction opportunities. Stage5 therefore proves
single-Core application and stacking without depending on the two-source
cooperation reserved for Task015.

Non-Elemental Supports remain materially visible. They contribute `7,509`
TowerScaled damage in the Reference and `7,641` in the coherent alternative,
while the Elemental Magic Core remains the primary late-pressure source.

---

## 7. Final Acceptance Evidence

The final V9 HP6 records are the current acceptance authority:

| Record | Fixture | Result | Killed | Leaked | Unresolved | Final Health | Effective Damage | Damage coverage |
|---|---|---|---:|---:|---:|---:|---:|---:|
| `Task014_PJ_S5V9_RefFire_HP6_N54_W11_M76_Skeleton1100_Orc1700_Burning12_Tick7_FlameBurst250_Schema24_01` | Reference | Victory | `74` | `2` | `0` | `4 / 6` | `47,273` | `98.98%` |
| `Task014_PK_S5V9_AltCold_HP6_N54_W11_M76_Skeleton1100_Orc1700_RelocIgnore_Schema24_02` | Coherent Alternative A | Victory | `71` | `5` | `0` | `1 / 6` | `45,848` | `96.00%` |
| `Task014_PL_S5V9_AntiFrag4Way_HP6_N54_W11_M76_Skeleton1100_Orc1700_RelocIgnore_Schema24_01` | Fragmented Anti-pattern | Defeat | `59` | `6` | `11` | `0 / 6` | `31,826` | `66.64%` at terminal |

Reference and Coherent Alternative A clear W1-W10 without leaks; all positive
leaks are W11 Orcs. The Anti-pattern clears W1-W9, leaks five W10 Skeletons,
then reaches Defeat on the first W11 Orc leak. All `76` Monsters spawn, all ten
Drafts commit, and `11` post-final-Draft resolutions occur before terminal
Defeat; the eleven unresolved W11 Orcs are expected terminal state rather than
missing evidence.

All three final records pass every schema-24 integrity check, including fixture,
resolution, Draft selection, investment commit, Tower deployment, Wave
attribution, damage diagnostics, placement-route, Buff, and Elemental
diagnostics.

---

## 8. Acceptance Decision And Downstream Handoff

Stage5 V9 is accepted under the schema-24 Elemental values because:

- one Fire or Cold Elemental Magic Core has measurable application, stack, and
  package value;
- neither positive Build requires a second Elemental source or cross-Element
  reaction;
- non-Elemental Support damage remains visible;
- the Reference and coherent alternative clear within the accepted HP6 margin;
- equal-budget fragmented investment without an Elemental Core fails on the
  sixth leak under reasonable placement;
- all ten Drafts occur at the reviewed nodes and leave `22` completed-Core
  resolutions;
- the four-family pools provide continuous L3 and Elemental reachability;
- Profile provenance, Wave authorship, TowerScaled damage, and FixedBuff damage
  are explicit and Recorder-consistent.

Task015 may consume the accepted Stage5 contract as its progression and
single-Core baseline. Task015 must add matching-source Elemental cooperation
rather than retuning Stage5 to require it. Task016 owns later fast-Monster
substitutions, and Task017 owns natural Draft-offer accessibility. Any later
change to Stage5 Player Health, Progress, fixed-speed Profiles, Wave table,
placement, or accepted packages explicitly reopens this Task.

The 2026-09-03 schema-24 rerun satisfies the reopening condition. Task014 is
complete again, and Task015 may consume V9 as the current accepted single-Core
baseline.

---

## 9. Calibration History And Supersession

Early Stage5 candidates were intentionally measured under Player Health `38`
while placement, Progress cadence, roster order, and late-wave HP were still
being calibrated. V1-V4 results are superseded wherever their Profile identity,
HP, Wave order, placement, or progression differs from V5.

V5 historically accepted Skeleton Lv6 HP `900`, Orc Lv7 HP `1300`, the
node-`54` progression boundary, and the locked placement in this document. Its
HP38 measurement records and HP6 schema-23 acceptance records remain historical
evidence only.

After Task007 increased the Elemental normal and Overload package, the unchanged
V5 Fire Reference cleared with zero leaks. V6-V8 isolated the late-Wave HP
search: Skeleton Lv6 moved to `1100`, while Orc Lv7 candidates `1600`, `2400`,
and `2000` respectively proved too weak or too strong for the complete Build
envelope. V9 accepts Orc Lv7 HP `1700` together with Skeleton Lv6 HP `1100`.
It preserves W1-W9, all counts and timing, node-`54` progression, Player Health
`6`, Fixed Drafts, and locked placement.

`Task014_PC_S5V5_AntiFrag4Way_N54_HP38_S23_01` recorded actual Player Health
`6` despite its `HP38` label. It is retained as diagnostic evidence only and is
superseded by the correctly identified Phase E Anti-pattern repeat.
