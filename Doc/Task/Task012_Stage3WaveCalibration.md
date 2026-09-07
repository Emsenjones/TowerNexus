# Task012 - Stage3 Wave Calibration

Status: Completed on 2026-08-28; Stage3 V4 Progress, fixed-speed Wave,
Player Health, Magic contact correction, Build envelope, placement boundary,
and schema-23 route-damage coverage evidence are accepted

Depends on: Accepted Task011 Stage2 Wave Calibration

Unblocks: Task013 Stage4 Wave Calibration, Task016 fixed-speed substitution,
and Task017 natural Draft offer probability calibration

Task010A's dedicated ten-fixture movement acceptance and Stage1 Reference
movement regression remain separate technical debt. Task012 records prove the
movement cases encountered during Stage3 calibration; they do not substitute
for Task010A's focused acceptance suite.

## 1. Goal And Accepted Interpretation

Calibrate Stage3 around one developed Magic Core, persistent route-adjacent
contact, and useful coverage across more than one route zone.

The Reference Build is a reproducible positive control, not the unique correct
answer. Stage3 accepts a legal, internally coherent Build that spends its Draft
budget purposefully, uses strategically reasonable placement, and clears with
`0-3` leaked Monsters. A coherent alternative is not weakened merely because it
differs from or occasionally matches the Reference result.

The same tolerance does not make every legal allocation effective. Concentrated
coverage with an explainable route gap and extreme horizontal expansion without
Level or Upgrade growth remain meaningful failure boundaries. Fixed Drafts prove
Build efficacy under named conditions; Task017 separately owns natural offer
frequency, player choice, and coherent-Build accessibility.

## 2. Accepted Stage3 V4 Configuration

| Field | Accepted value |
|---|---|
| Player Max Health | `6` |
| Progress Requirements | `[3, 3, 4, 4, 4, 4]` |
| Post-initial Draft nodes | `3 / 6 / 10 / 14 / 18 / 22` |
| Total Draft opportunities | `7` |
| Total Monsters | `40` |
| Resolutions after final Draft node | `18` |
| Standard Move Speed | `0.25` |
| Standard Spawn Interval | `2.5s` |
| Wave Delays | `[4s, 6s, 5s, 4s, 4s, 4s, 4s, 4s]` |
| New Monster Profiles | None |

The authored Stage pools are cumulative Archer, Cannon, and Magic pools. Their
Basic and Behaviour Upgrades are available without Elemental content. Each
represented family retains continuous Stage-authored Level reachability.

## 3. Accepted Monster Wave

Task012 reuses the fixed-speed Profiles accepted by Task010 and Task011 without
changing HP or Move Speed:

| Wave | Profile | HP | Count | Spawn Interval | Wave Delay | Cumulative count | Reference milestone |
|---:|---|---:|---:|---:|---:|---:|---|
| 1 | Slime Lv1 | `60` | `3` | `2.5s` | `4s` | `3` | Level Magic Core to L2 |
| 2 | Slime Lv1 | `60` | `3` | `2.5s` | `6s` | `6` | Deploy Archer Support A |
| 3 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `5s` | `10` | Deploy Cannon Support |
| 4 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `14` | Deploy Archer Support B |
| 5 | Monster Plant Lv3 | `180` | `4` | `2.5s` | `4s` | `18` | Apply Faster Orbit |
| 6 | Turtule Shell Lv4 | `400` | `4` | `2.5s` | `4s` | `22` | Apply Arcane Field |
| 7 | Turtule Shell Lv4 | `400` | `8` | `2.5s` | `4s` | `30` | First completed-Build window |
| 8 | Turtule Shell Lv4 | `400` | `10` | `2.5s` | `4s` | `40` | Second completed-Build window |

The earlier `42`-Monster V1/V2 candidates established the rejected upper
pressure bound. V3 reduced Wave 7 from `10` to `8`; V4 retained the resulting
Wave table and accepted Player Health `6`.

## 4. Accepted Reference Fixture

Reference final Build:

| Role | Count | TowerFamily | Final state |
|---|---:|---|---|
| Core | `1` | Magic | L2, Faster Orbit, Arcane Field |
| Support | `2` | Archer | L1, no Upgrades |
| Support | `1` | Cannon | L1, no Upgrades |

Reference Fixed Draft sequence:

| Draft | Resolution node | Fixed result | Commit target |
|---:|---:|---|---|
| 1 | `0` | Magic Tower | Deploy Core |
| 2 | `3` | Magic Tower | Level Core to L2 |
| 3 | `6` | Archer Tower | Deploy Support A |
| 4 | `10` | Cannon Tower | Deploy Support B |
| 5 | `14` | Archer Tower | Deploy Support C |
| 6 | `18` | Faster Orbit | Apply to Magic Core |
| 7 | `22` | Arcane Field | Apply to Magic Core |

Accepted multi-zone placement cells:

| Tower | Footprint cells |
|---|---|
| Magic Core | `(4,3)` |
| Archer Support A | `(6,1) / (6,2)` |
| Cannon Support | `(6,3) / (7,2) / (7,3)` |
| Archer Support B | `(4,0) / (4,1)` |

The placement-relocation expectation is `Ignore`. Recorder still captures route
revision diagnostics, while Task010A alone owns strict movement acceptance.

## 5. Accepted Build Envelope And Negative Boundaries

| Fixture | Final allocation | Placement role | Decision |
|---|---|---|---|
| Reference Control | Magic L2, Faster Orbit, Arcane Field; Archer L1 x2; Cannon L1 | Accepted multi-zone cells | Positive control |
| Coherent Alternative A | Replace Reference Upgrades with Arcane Recovery and Twin Orbs | Same accepted cells | Accepted Behaviour alternative |
| Coherent Alternative B / Basic-only | Faster Orbit and Arcane Recovery; no Behaviour Upgrade | Same accepted cells | Accepted Basic-only alternative |
| Single-zone Concentration | Reference final Build | Four Towers concentrated around one local route zone | Accepted placement Anti-pattern |
| Horizontal Expansion | Magic L1; Archer L1 x3; Cannon L1 x3; no Levels or Upgrades | Two reviewed multi-zone candidates | Accepted underinvestment boundary |

Alternative A applies Arcane Recovery at node `18` and Twin Orbs at node `22`.
Alternative B applies Faster Orbit at node `18` and Arcane Recovery at node
`22`. Both preserve the Reference Towers, Draft nodes, and placements.

Horizontal Expansion uses every Draft to deploy a Tower. Its second placement
candidate gave all seven Towers positive damage contribution, yet both reviewed
placements leaked three HP400 Monsters in Wave 6 and three more in Wave 7. The
failure demonstrates the timing and output cost of completely abandoning
vertical growth; it is not evidence that ordinary horizontal support is invalid.

## 6. Magic Orb Planar-Contact Correction And Regression

Stage3 exposed a cross-Profile contact defect. Magic Orb contact previously used
full three-dimensional point distance to the Monster hit anchor. Vertical visual
offset could therefore consume the complete authored Contact Distance even when
an Orb visibly intersected a Monster on the movement plane.

Magic Orb contact now measures X/Z planar distance. The hit anchor still owns
the committed damage and feedback position after contact succeeds. The fix
changes no Monster HP, Magic damage, Orbit Radius, Contact Distance, Same Target
Cooldown, rotation speed, Attack Cycle, or Orb lifetime. Accepted Stage3 records
show positive Magic contribution against both Slime and Monster Plant Waves.

The smallest affected naked-Magic regression reused Task004's surviving-target
shape: Player Health `100`, Progress Requirements `[2, 999]`, one two-Monster
HP60 setup Wave, one forty-Monster measurement Wave, Spawn Interval `2.5s`, and
Wave Delays `5 / 90`. Each run deployed exactly one Magic L1 Tower with Damage
`25`, Range `1.5`, Attack Cycle `22s`, Contact Distance `0.3`, and no Upgrade.
The second fixed Magic Draft remained Pending throughout the measurement Wave.
Straight, L, and U use dedicated regression StageDefinitions with explicit
`Prefab_Map_Default_Straight`, `Prefab_Map_Default_LRoute`, and
`Prefab_Map_Default_URoute` templates rather than mutating one shared map field.

The three immutable Run Names retain the historical `HP480` comparator label.
The live `Prefab_MonsterCandidate_EvilMage` had since changed to HP4800, which is
recorded explicitly by schema 23. This identity mismatch is accepted for this
contact-only regression because all forty measurement Monsters survived in both
the historical and current fixtures, so target HP did not truncate contacts,
damage applications, or Effective Damage. The reports, rather than the label,
remain authoritative about live HP.

| Route | Map template | Magic cell | Measurement applications | Measurement ED | Historical ED range | Decision |
|---|---|---|---:|---:|---:|---|
| Straight | `Prefab_Map_Default_Straight` | `(4,4)` | `47` | `1175` | `1100-1250` | Pass |
| L | `Prefab_Map_Default_LRoute` | `(2,3)` | `96` | `2400` | `2150-2425` | Pass |
| U | `Prefab_Map_Default_URoute` | `(4,3)` | `137` | `3425` | `3225-3425` | Pass |

All three schema-23 records resolved `42 / 42` Monsters, retained exactly one
deployed naked Magic L1 Tower, passed every exported integrity check, and
preserved `Straight < L < U`. The planar-contact correction is accepted as the
current global Magic contact baseline.

## 7. Final Schema-23 Evidence

Schema 23 adds read-only observed route-damage coverage without becoming
gameplay authority. Tower deployment ordinals provide stable identities;
per-Tower summaries report applications, Effective Damage, killing blows,
distinct damaged Monsters, and route-state cells. Pairwise summaries report
shared route cells, shared route-phase cells, and shared damaged Monsters.
`towerRouteDamageCoverageMatches` reconciles Tower, Wave, damage, kill, cell,
and overlap totals.

| Fixture | Result | Killed | Leaked | Unresolved | Final Health | Effective Damage | Final-phase union | Decision |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Reference Control | Victory | `40` | `0` | `0` | `6 / 6` | `11320` | `10` | Accepted positive control |
| Coherent Alternative A | Victory | `39` | `1` | `0` | `5 / 6` | `11172` | `7` | Accepted alternative |
| Basic-only Alternative B, run 1 | Victory | `38` | `2` | `0` | `4 / 6` | `10936` | `9` | Accepted repeat |
| Basic-only Alternative B, run 2 | Victory | `40` | `0` | `0` | `6 / 6` | `11320` | `11` | Accepted repeat |
| Single-zone candidate | Defeat | `27` | `6` | `7` | `0 / 6` | `9290` | `8` | Accepted discovery |
| Single-zone frozen repeat | Defeat | `22` | `6` | `12` | `0 / 6` | `7828` | `8` | Accepted Anti-pattern repeat |
| Horizontal Expansion candidate 1 | Defeat | `27` | `6` | `7` | `0 / 6` | `8065` | `11` | Rejected Build candidate |
| Horizontal Expansion candidate 2 | Defeat | `28` | `6` | `6` | `0 / 6` | `8175` | `7` | Accepted underinvestment boundary |

The Single-zone runs used identical placements, identical final routes, and the
same eight final-phase damage cells. Pairwise shared route-cell totals rose from
the Reference's `16` to `22 / 23`, while every Tower retained positive damage
contribution. Both runs leaked one Monster in Wave 4, three in Wave 6, and two in
Wave 7. This is an explainable local-overlap failure rather than a hidden family
penalty.

Schema-22 Player Health controls remain supporting evidence: the Reference
cleared `40 / 40`, and Alternative A cleared with `37` kills, `3` leaks, and
`3 / 6` Health. Schema 23 supersedes their coverage diagnostics without
invalidating their combat results.

## 8. Acceptance And Downstream Handoff

- Accepted assets match Player Health `6`, Progress Requirements
  `[3,3,4,4,4,4]`, and the eight-Wave table in this document.
- Reference and two coherent alternatives clear inside the Stage3-specific
  `0-3` leak margin; Reference is not treated as the unique solution.
- Strategically reasonable placement can make a Basic-only package viable,
  while concentrated coverage fails from an observed route gap.
- Pure seven-Tower L1 expansion cannot replace all Level and Upgrade growth.
- Magic planar contact is accepted across affected Stage3 Profiles and the
  focused Straight/L/U geometry regression.
- Every positive control resolves all `40` Stage Monsters; terminal Defeat and
  unresolved-at-terminal Monsters remain distinct from Recorder integrity.
- No Wave depends on speed differentiation before Task016.
- Fixed Drafts prove named Build efficacy only; Task017 owns natural Draft offer
  probability and player-choice interpretation.

Task012 is complete. Task013 may begin Stage4 calibration using the accepted
Task010-Task012 fixed-speed Profiles, the Stage-local coherent-Build margin
workflow, and schema-23 route-damage coverage diagnostics. Any later revision to
Stage3 Player Health, Progress, Monster counts, Wave delays, accepted upstream
Tower/Profile power, or Magic planar-contact behavior explicitly reopens this
Task and its dependent evidence.
