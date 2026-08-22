# Task002 - Level 1 Base Tower Baseline

Status: Completed; CombatMathV2 Level 1 baseline accepted on 2026-08-21,
with the Magic Attack Cycle revision accepted on 2026-08-22

Depends on: Completed Task001 Combat Damage Formula Refactor

Unblocks: Task003-Task016

## 1. Goal

Calibrate the four Level 1 Towers without any Upgrade so their delivered combat
value remains in one broad comparable range while preserving distinct cadence,
coverage, concentration, conversion, and route-exposure identities.

The accepted values in this Task are the CombatMathV2 `B1` comparison units.
They replace the archived CombatMathV1 baseline and remain fixed by default
during Task003 and later calibration.

## 2. Accepted Fixed Fixture

- One Level 1 Tower at a time
- No Basic, Behaviour, or Elemental Upgrade
- `Config_MonsterWave_Default`: one homogeneous Wave of `40`
- Reference Monster candidate: `Prefab_MonsterCandidate_Dragon`
- Reference Monster Max Health `120` and Move Speed `0.25`
- Spawn Interval `2.5s`
- Player Max Health `40`
- Progress Requirement `[999]` to prevent Level Up during the run
- L route as primary acceptance
- Straight and U routes as low- and high-exposure diagnostics
- The same family-specific placement within each route comparison

Kill counts are not equalized. Effective Damage and recognizable family
identity are the primary cross-family comparison.

## 3. Accepted Level 1 Values

| TowerFamily | BasicDamage | Attack Range | Attack Cycle | Baseline Targeting |
|---|---:|---:|---:|---|
| Archer | `20` | `2` | `0.85s` | Lowest Health |
| Cannon | `60` | `3` | `3.5s` | Lowest Health |
| Magic | `25` | `1.5` | `22s` | Orbit contact; no release target required |
| Drone | `10` | `4` | `10s` | Random |

Magic Orb retains Rotation Speed `90`, Orbit Radius `1`, Same Target Hit
Cooldown `0.5s`, and Max Lifetime `18s`. The accepted `22s` Attack Cycle leaves
a readable `4s` base release gate after normal Orb expiry; Task004's Arcane
Recovery reduces that cycle to `19s` and therefore reduces the post-expiry gate
to `1s`. Contact Distance is accepted at `0.3`:
`0.25` produced visually unsatisfying apparent misses, while `0.4` produced an
unacceptable L-route `4590` Effective Damage and `33 / 40` kills. The accepted
`0.3` distance preserves readable contact while BasicDamage `25` returns L-route
combat value to the reviewed broad band.

Cannon's initial Highest Health run delivered `2040` Effective Damage but
distributed one `60`-damage hit to each of `34` Monsters and killed none.
Lowest Health is accepted because it preserves the same broad damage budget
while restoring deliberate heavy-hit conversion.

## 4. Accepted Play Mode Results

Every accepted report used the fixture in Section 2, recorded exactly one L1
Tower with no Upgrade, and resolved all `40` Monsters.

### 4.1 Straight Route - Low Exposure Diagnostic

| Tower | Killed | Effective Damage | Damage Coverage | Average Leaked Remaining HP | Successful Damage Events |
|---|---:|---:|---:|---:|---:|
| Archer | `2 / 40` | `2160` | `45.00%` | `69.474` | `108` |
| Cannon | `3 / 40` | `1860` | `38.75%` | `79.459` | `31` |
| Magic | `0 / 40` | `1350` | `28.13%` | `86.250` | `54` |
| Drone | `10 / 40` | `1830` | `38.12%` | `99.000` | `183` |

Accepted reports:

- `Task002_StraightRoute_Archer_L1_NoUpgrade_02.json`
- `Task002_StrightRoute_Cannon_L1_NoUpgrade_01.json`
- `Task002_StrightRoute_MagicL1_NoUpgrade_01.json`
- `Task002_StrightRoute_Drone_L1_NoUpgrade_01.json`

The `StrightRoute` spelling in three generated report identities is historical
record-label text only. Route, fixture, Tower identity, and measurements are
unambiguous. The first Archer Straight report is excluded because its run label
contained Magic-only parameter text; the accepted `_02` report reproduced its
combat result exactly with the correct identity.

### 4.2 L Route - Primary Acceptance

| Tower | Killed | Effective Damage | Damage Coverage | Average Leaked Remaining HP | Successful Damage Events |
|---|---:|---:|---:|---:|---:|
| Archer | `5 / 40` | `2320` | `48.33%` | `70.857` | `116` |
| Cannon | `7 / 40` | `2100` | `43.75%` | `81.818` | `35` |
| Magic | `0 / 40` | `2575` | `53.65%` | `55.625` | `103` |
| Drone | `13 / 40` | `2090` | `43.54%` | `100.370` | `209` |

Accepted reports:

- `Task002_LRoute_Archer_L1_NoUpgrade_01.json`
- `Task002_LRoute_Cannon_L1_NoUpgrade_LowestHealth_01.json`
- `Task002_LRoute_Magic_L1_NoUpgrade_Damage25_Contact030_01.json`
- `Task002_LRoute_Drone_L1_NoUpgrade_01.json`

The accepted L-route Effective Damage band is `2090-2575`. Magic delivers the
highest distributed damage but no kills, while Drone delivers the lowest total
damage and the most kills. This is accepted coverage-versus-conversion identity,
not a reason to equalize kill counts.

### 4.3 U Route - High Exposure Diagnostic

| Tower | Killed | Effective Damage | Damage Coverage | Average Leaked Remaining HP | Successful Damage Events |
|---|---:|---:|---:|---:|---:|
| Archer | `8 / 40` | `2560` | `53.33%` | `70.000` | `128` |
| Cannon | `9 / 40` | `2160` | `45.00%` | `85.161` | `36` |
| Magic | `5 / 40` | `3650` | `76.04%` | `32.857` | `147` |
| Drone | `16 / 40` | `2180` | `45.42%` | `109.167` | `218` |

Accepted reports:

- `Task002_URoute_Archer_L1_NoUpgrade_02.json`
- `Task002_URoute_Cannon_L1_NoUpgrade_01.json`
- `Task002_URoute_Magic_L1_NoUpgrade_01.json`
- `Task002_URoute_Drone_L1_NoUpgrade_01.json`

Magic resolved `147` successful `25`-damage events for `3675` applied damage.
Its `3650` Effective Damage excludes `25` overkill from five lethal fifth
contacts. This difference is expected and passed recorder damage integrity.
The first Archer U report is excluded because its run label named Cannon; the
accepted `_02` report used the correct identity and reproduced the result within
one normal timing-dependent hit.

## 5. Route And Identity Acceptance

The Section 4 table records the original CombatMathV2 acceptance at the former
`20s` Magic Attack Cycle. After Task004 identified that `20s` combined with the
`18s` Orb lifetime left too little scheduler space for Arcane Recovery to read
clearly, the base cycle changed to `22s` without changing BasicDamage, Range,
contact, rotation, or Orb lifetime.

The smallest affected naked-Magic regression used Task004's HP480 measurement
fixture and the current `22s` cycle:

| Route | Effective Damage observations | Accepted mean |
|---|---:|---:|
| Straight | `1250 / 1100 / 1125` | `1158` |
| L | `2425 / 2150 / 2275` | `2283` |
| U | `3425 / 3375 / 3400 / 3225` | `3356` |

Every run retained Damage `25`, Range `1.5`, Rotation Speed `90`, no Upgrade,
complete Wave attribution, and the route ordering `Straight < L < U`. These
results accept the scheduler revision while preserving the original family
identity; they do not retroactively replace the Section 4 HP120 measurements.

| Tower | Straight ED | L ED | U ED |
|---|---:|---:|---:|
| Archer | `2160` | `2320` | `2560` |
| Cannon | `1860` | `2100` | `2160` |
| Magic | `1350` | `2575` | `3650` |
| Drone | `1830` | `2090` | `2180` |

- Every family satisfies `Straight < L < U` Effective Damage.
- Archer retains fast, reliable finishing with moderate exposure growth.
- Cannon retains heavy-hit concentration without the all-residual-health
  opening failure.
- Magic is weakest on Straight, comparable on the authoritative L route, and
  exceptional only at the premium U position. It is geometry-specialized, not
  universally dominant.
- Drone retains mobile pursuit and the strongest kill conversion in the
  accepted L and U comparisons.

## 6. Recorder Integrity

Every accepted report confirms:

- expected Wave and Monster count, homogeneous HP and Move Speed, and observed
  Spawn Interval;
- L1 BasicDamage and resolved combat values;
- no applied Upgrade;
- primary Tower-owned DamageScale `1`, no Tower-scaled rejection, and no
  FixedBuff damage;
- `ResolutionCountsMatch`, `LeakCountMatchesPlayerHealthLoss`, Monster runtime,
  Wave attribution, deployment coverage, and damage-diagnostic integrity.

The deliberate `[999]` Progress Requirement prevents Level Up during a
40-Monster L1 control. The general recorder derives one expected Level Up from
the one-entry requirement sequence, so `LevelUpCountMatches`,
`LevelUpResolutionNodesMatch`, `FinalPlayerLevelMatches`, and
`PostFinalDraftCombatObserved` are false for every run. These progression checks
are not applicable to this locked-L1 fixture and do not invalidate its complete
combat-resolution evidence.

## 7. Handoff

- Task003 uses `20 / 60 / 25 / 10` for Archer, Cannon, Magic, and Drone as the
  accepted `B1` units when calibrating each TowerFamily's L2/L3 BasicDamage.
- Current Magic Level controls use the accepted `22s` Attack Cycle; the
  BasicDamage curve remains unchanged.
- Task003 keeps this fixture, the route roles, and naked L1 controls fixed unless
  a named Task002 revision is explicitly approved.
- Task008 uses the accepted fixture and L1 outputs as reference pressure input.
- Any later Base Tower, Magic Orb contact, or Reference Monster change requires
  a named baseline revision and the smallest affected Straight/L/U regression.
