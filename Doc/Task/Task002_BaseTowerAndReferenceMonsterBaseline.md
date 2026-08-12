# Task002 - Base Tower And Reference Monster Baseline

Status: Completed; `Base Combat v0.4` frozen on 2026-08-12 after the Task003B combat-value scale migration

Depends on: Completed Task001 Stage1 greybox Map

## 1. Goal

Establish and preserve the current `Base Combat v0.4` baseline for the four Level 1 Towers and one Reference Monster while retaining prior accepted versions as historical recovery points.

The frozen record provides a recovery point for later development: future Level, Upgrade, Stage, or runtime work can compare against the accepted Level 1 values instead of changing them unintentionally or losing the previous baseline.

The four attack strategies must remain visibly different while their same-cost combat value stays within one broad comparable range at an authored L-shaped core position. Straight and U-shaped routes remain required diagnostics for low-exposure and premium-position behavior rather than separate balance targets.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/12_ProjectileSystem.md`

## 3. In Scope

- Archer, Cannon, Magic, and Drone Level 1 attack identity
- Base damage, Attack Range, Attack Cycle Duration, and targeting behavior
- Projectile or Attack Entity movement, lifetime, contact, and cadence values
- Reference Monster maximum health and movement speed
- L-shaped primary acceptance comparison
- Straight low-exposure diagnostic comparison
- U-shaped premium-position stress comparison
- A durable freeze point for `Base Combat v0.4`

## 4. Out Of Scope

- L2/L3 growth
- Basic, Behaviour, or Elemental Upgrade balance
- Exact first-hit delay or single-Monster TTK timing
- Permanent recording of each test deployment Grid
- Effect and Buff balance
- Monster roster variants
- Player Progress Requirements
- Stage Wave tuning

Task003B multiplied the shared combat integer scale by ten so deterministic integer Damage Bonus values have sufficient tuning resolution. This Task therefore completed a named `Base Combat v0.4` regression before Task003 Upgrade calibration. The scaling migration is accepted because the three-route results preserve the v0.3 Tower identities; superseded records remain in Section 11 rather than being retroactively rewritten.

## 5. Fixed Reference Run

### 5.1 Shared Fixture

- One Wave with `40` Reference Monsters
- Spawn Interval `2.5s`
- Reference Monster Max Health `120`
- Reference Monster Move Speed `0.25`
- Player Max Health `40`
- One Level 1 Tower at a time
- No Upgrades
- The same authored Tower placement within each route comparison

Player Final Health equals the number of Monsters killed because every leak removes one Player Health. It is a convenient conversion signal, not the primary power metric.

### 5.2 Route Roles

| Route | Test Role | Interpretation |
|---|---|---|
| L-shaped | Primary acceptance route | Represents the likely core-Tower placement around a strong corner and is the authoritative `Base Combat v0.4` comparison. |
| Straight | Low-exposure diagnostic | Shows how each family performs with limited route coverage and exposes reliance on corner geometry. It is not tuned to match the L-shaped result. |
| U-shaped | Premium-position stress | Measures the additional exposure available at a deliberately strong deployment position and reveals route-overload behavior. It is not required to produce the most kills for every family. |

All four Towers must run on all three route structures. Route results are compared as a suite, but they are not interchangeable samples and are not flattened into one route-independent outcome.

If a U-shaped result is unusually strong, first treat it as placement identity evidence. Do not change frozen Base Tower values merely to erase legitimate geometry advantage; only revise the baseline when the same family becomes structurally dominant across the route suite.

### 5.3 Measurement Priority

1. `Effective Damage` and `Damage Coverage` are the primary cross-family power measures.
2. `Killed`, `Final Player Health`, and `Average Leaked Remaining HP` describe damage conversion and Tower identity.
3. `Peak Alive`, battle duration, targeting observations, and misses are supporting diagnostics.

Kill counts are intentionally not equalized. A Tower that concentrates damage and a Tower that spreads damage may deliver comparable total value while producing different kill counts against the same fixture.

## 6. Frozen Base Combat v0.4

### 6.1 Shared Tower Values

| Tower | Base Attack Damage | Attack Range | Attack Cycle Duration | Target Selection |
|---|---:|---:|---:|---|
| Archer | `20` | `2` | `0.85s` | Lowest Health |
| Cannon | `60` | `3` | `3.5s` | Highest Health |
| Magic | `30` | `1.5` | `20s` | Baseline contact behavior does not require a release target |
| Drone | `10` | `4` | `10s` | Lowest Health |

These are the no-Upgrade values authored by each Tower runtime template. Base Attack Damage is one TowerFamily baseline shared by every Tower level; Tower Level changes model presentation and Upgrade eligibility without replacing this value.

### 6.2 Archer Projectile

| Parameter | Frozen Value |
|---|---:|
| Projectile Speed | `3` |
| Hit Distance Threshold | `0.3` |
| Max Lifetime | `1s` |

### 6.3 Cannon Projectile

| Parameter | Frozen Value |
|---|---:|
| Projectile Speed | `2` |
| Hit Distance Threshold | `0.4` |
| Max Lifetime | `10s` |
| Arc Height | `1` |

### 6.4 Magic Orb

| Parameter | Frozen Value |
|---|---:|
| Rotation Speed | `90` |
| Orbit Radius | `1` |
| Contact Distance | `0.25` |
| Same Target Hit Cooldown | `0.5s` |
| Max Lifetime | `18s` |

The Magic Attack Cycle begins when a complete Orb group is successfully activated and runs concurrently with that group. Normal Orb completion is governed by the shared maximum lifetime; gameplay hit-count exhaustion no longer exists. Per-target contact cooldown remains unchanged; no route-shape detection or shared Global Target Cooldown is used.

### 6.5 Drone And Drone Projectile

| Parameter | Frozen Value |
|---|---:|
| Maximum Active Drones | `1` |
| Battery Duration | `20s` |
| Orbit Radius | `1.5` |
| Flight Speed | `3` |
| Flight Height | `1` |
| Burst Count | `4` |
| Burst Interval | `0.4s` |
| Burst Cooldown | `1.3s` |
| Projectile Speed | `6` |
| Projectile Hit Distance Threshold | `0.2` |
| Projectile Max Lifetime | `4s` |

Ordinary Drone retargeting preserves Burst phase, remaining shots, and timer. Retargeting does not reload a Burst or bypass Inter-Burst Cooldown.

## 7. Accepted Play Mode Results

Every accepted run used the fixture in Section 5. Integrity checks confirmed that all `40` Monsters resolved and that Player Health loss matched the leak count. The incomplete first Archer L-shaped recorder sample with one unresolved Monster is excluded from the accepted table.

### 7.1 L-shaped Primary Acceptance

| Tower | Accepted Runs | Killed | Average Effective Damage | Average Damage Coverage | Average Leaked Remaining HP | Terminal State |
|---|---:|---:|---:|---:|---:|---|
| Archer | `1` | `5 / 40` | `2280` | `47.50%` | `72.000` | Victory |
| Cannon | `1` | `10 / 40` | `2040` | `42.50%` | `92.000` | Victory |
| Magic | `1` | `0 / 40` | `2160` | `45.00%` | `66.000` | Defeat |
| Drone | `2` | `6 / 40`, `7 / 40` | `2025` | `42.19%` | `82.852` | Victory |

The L-shaped average effective-damage range is `2025` to `2280`, so the four naked Towers remain in one broad primary-acceptance band. Archer, Cannon, and Magic reproduce their v0.3 Effective Damage at exactly ten times the former scale. Drone produced `2020` and `2030`; this narrow timing variance preserves its prior practical result and identity.

### 7.2 Straight Low-exposure Diagnostic

| Tower | Killed | Effective Damage | Damage Coverage | Average Leaked Remaining HP | Terminal State |
|---|---:|---:|---:|---:|---|
| Archer | `2 / 40` | `2140` | `44.58%` | `70.000` | Victory |
| Cannon | `8 / 40` | `1800` | `37.50%` | `93.750` | Victory |
| Magic | `0 / 40` | `1170` | `24.38%` | `90.750` | Defeat |
| Drone | `4 / 40` | `1860` | `38.75%` | `81.667` | Victory |

Magic's recorder label was entered as `L Route`, but the user confirmed this run used the Straight route; its `PeakAlive=14` and approximately `132s` battle duration also match the Straight fixture. The zero-kill result is accepted identity evidence rather than zero output: it delivered `1170` Effective Damage but distributed that damage without completing a kill.

### 7.3 U-shaped Premium-position Stress

| Tower | Accepted Runs | Killed | Average Effective Damage | Average Damage Coverage | Average Leaked Remaining HP | Effective Damage vs L |
|---|---:|---:|---:|---:|---:|---:|
| Archer | `1` | `7 / 40` | `2500` | `52.08%` | `69.697` | `+9.6%` |
| Cannon | `1` | `6 / 40` | `2280` | `47.50%` | `74.118` | `+11.8%` |
| Magic | `1` | `1 / 40` | `3240` | `67.50%` | `40.000` | `+50.0%` |
| Drone | `1` | `9 / 40` | `2200` | `45.83%` | `83.871` | `+8.6%` |

Every family produced more Effective Damage as route exposure increased from Straight to L-shaped to U-shaped. Magic's `3240` result lies inside the v0.3-equivalent historical range of `3210-3330`, preserving its repeatable premium-position behavior.

Magic's `1170 -> 2160 -> 3240` route profile is an intentionally strong geometry response: weak on Straight, comparable on the authoritative L-shaped route, and exceptional at the U-shaped premium position. Its U-shaped damage still converted only one kill because contact damage was distributed across the wave. This is accepted specialization evidence, not a reason to equalize kill count or reduce a Base Tower value that remains comparable on L.

### 7.4 Accepted Identity Reading

- Archer retains fast, reliable finishing and scales moderately with added route exposure.
- Cannon retains heavy-hit concentration and long range; its kill conversion changes strongly with when and where captured targets are hit.
- Magic retains broad distributed contact damage and the strongest dependence on sustained corner coverage; it is weak on Straight, comparable on L, and exceptional at the U premium position even when kills remain low.
- Drone retains mobile pursuit, Burst cadence, battery duration, and active-entity limits, with stronger conversion when route exposure creates follow-up opportunities.

No Tower is accepted as universally superior. The route suite establishes comparable delivered damage while preserving different cadence, concentration, coverage, and conversion identities.

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Campaign experience intent, not combat implementation |
| Tower content | Frozen Level 1 authoring and family identity |
| Tower Runtime Combat | Attack execution and semantic timing contracts |
| Monster content | Frozen Reference health, movement, and presentation |
| Task002 | Accepted `Base Combat v0.4` fixture, route structure, parameters, and result record |
| Task003 | Required-Level plus Basic and Behaviour Upgrade calibration on top of this baseline |

## 9. Acceptance

- The four Level 1 attack strategies are recognizable from their runtime behavior.
- L-shaped Effective Damage falls within one broad comparable range without equalizing kill counts.
- Straight, L-shaped, and U-shaped results show the expected increase in exposure without establishing one universally superior Tower.
- Reference Monster Health `120` prevents the accepted route suite from reaching the kill ceiling.
- All accepted Level 1 values, fixture values, route roles, and results are recorded for later recovery and regression.

## 10. Validation Record

- Fixed-condition L-shaped Play Mode runs for all four Towers, including a second Drone run that established a narrow `2020-2030` Effective Damage range
- Fixed-condition Straight low-exposure Play Mode run for all four Towers
- Fixed-condition U-shaped premium-position Play Mode runs for all four Towers, with Magic inside the v0.3-equivalent historical range
- `CombatBalanceRunRecorder` output for fixture, damage, pressure, timing, integrity, Tower family, Tower level, resolved stats, and Upgrade layers
- Every accepted run reported `ResolutionCountsMatch=True` and `LeakCountMatchesPlayerHealthLoss=True`
- Recorder verification of Reference Monster Health `120`, all four resolved Tower values, and no applied Upgrades

## 11. Superseded Base Combat Records

### 11.1 Base Combat v0.3

`Base Combat v0.3` was frozen earlier on 2026-08-12 with `40 / HP 12 / Speed 0.25 / Spawn 2.5s`. It introduced the current Magic Orb lifetime/contact model and Rotation Speed `90`. Task003B retained every non-damage value while multiplying the shared combat integer scale by ten.

| Tower | L-shaped | Straight | U-shaped |
|---|---:|---:|---:|
| Archer | `228` | `212` | `250` |
| Cannon | `204` | `180` | `228` |
| Magic | `216` | `111` | `327` average (`321-333`) |
| Drone | `205` | `186` | `221` |

Values are accepted Effective Damage. The v0.4 regression reproduced these values at the expanded scale within the expected discrete timing variance.

### 11.2 Base Combat v0.2

`Base Combat v0.2` was frozen on 2026-08-11 with the same `40 / HP 12 / Speed 0.25 / Spawn 2.5s` fixture and the same shared Tower stats now used by v0.3. Its Magic Orb used Rotation Speed `180`, Max Hit Count `14`, and Max Lifetime `18s`. Task003A's removal of gameplay hit-count exhaustion made that Magic baseline structurally obsolete and required v0.3.

| Tower | L-shaped | Straight | U-shaped |
|---|---:|---:|---:|
| Archer | `232` | `216` | `254` |
| Cannon | `204` | `180` | `228` |
| Magic | `238.5` | `216` | `252` |
| Drone | `203.5` | `186` | `222` |

Values are accepted Effective Damage. The full v0.2 evidence remains recoverable from repository history at the v0.2 freeze commit.

### 11.3 Base Combat v0.1

`Base Combat v0.1` was frozen on 2026-08-06 with `40` Monsters, Health `5`, Move Speed `0.25`, Spawn Interval `2.5s`, and Straight as the authoritative comparison. It is retained as historical evidence but no longer defines the current regression fixture.

The v0.1 shared Tower values matched v0.2 except that Cannon Base Attack Damage was `5`.

| Tower | Straight Route | Straight Average | L-shaped Route | U-shaped Route |
|---|---|---:|---|---|
| Archer | `29 / 40`, `28 / 40` | `28.5` | `36 / 40` | Not required |
| Cannon | `31 / 40`, `31 / 40` | `31` | `29 / 40`, `29 / 40` | Not required |
| Magic | `30 / 40`, `33 / 40`, `32 / 40` | `31.7` | `37 / 40` | `39 / 40` |
| Drone | `32 / 40`, `32 / 40` | `32` | `40 / 40` | Skipped because L-shaped reached the measurement ceiling |

## 12. Revision Policy

Task003 and later Tasks must keep the `Base Combat v0.4` Level 1 baseline fixed by default. Base Attack Damage is authored once by the Tower runtime template rather than repeated in TowerLevelConfig. Required-Level gates, Upgrade deltas, and Behaviour packages are the first calibration levers for later content; the Base Tower must not be changed merely to repair one Upgrade.

A Level Up whose contract and data remain model-and-eligibility-only does not require a repeated combat-output run; static schema and asset validation are sufficient unless runtime combat resolution code changes.

An ownership migration is not a `Base Combat` value revision, but it must rerun the Task002 route suite when the resolved-damage path changes.

If later evidence proves that the Level 1 baseline itself is structurally invalid, the change must be named as a new `Base Combat` revision. Keep the fixed fixture and route roles, rerun every naked Level 1 Tower on L-shaped primary acceptance plus Straight and U-shaped diagnostics, use Effective Damage and Damage Coverage as the primary measures, and retain v0.4 as historical evidence before replacing it. Downstream Upgrade checks must then be rerun from the new accepted controls.
