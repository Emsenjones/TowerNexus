# Task002 - Base Tower And Reference Monster Baseline

Status: Completed; `Base Combat v0.3` frozen on 2026-08-12 after the Task003A structural regression

Depends on: Completed Task001 Stage1 greybox Map

## 1. Goal

Establish and preserve the current `Base Combat v0.3` baseline for the four Level 1 Towers and one Reference Monster while retaining prior accepted versions as historical recovery points.

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
- A durable freeze point for `Base Combat v0.3`

## 4. Out Of Scope

- L2/L3 growth
- Basic, Behaviour, or Elemental Upgrade balance
- Exact first-hit delay or single-Monster TTK timing
- Permanent recording of each test deployment Grid
- Effect and Buff balance
- Monster roster variants
- Player Progress Requirements
- Stage Wave tuning

Task003A removed gameplay Magic Orb Maximum Hit Count and replaced Lingering Orbit. This Task therefore completed a named `Base Combat v0.3` revision before Task003 Upgrade calibration. The superseded v0.2 record remains in Section 11 rather than being retroactively rewritten.

## 5. Fixed Reference Run

### 5.1 Shared Fixture

- One Wave with `40` Reference Monsters
- Spawn Interval `2.5s`
- Reference Monster Max Health `12`
- Reference Monster Move Speed `0.25`
- Player Max Health `40`
- One Level 1 Tower at a time
- No Upgrades
- The same authored Tower placement within each route comparison

Player Final Health equals the number of Monsters killed because every leak removes one Player Health. It is a convenient conversion signal, not the primary power metric.

### 5.2 Route Roles

| Route | Test Role | Interpretation |
|---|---|---|
| L-shaped | Primary acceptance route | Represents the likely core-Tower placement around a strong corner and is the authoritative `Base Combat v0.3` comparison. |
| Straight | Low-exposure diagnostic | Shows how each family performs with limited route coverage and exposes reliance on corner geometry. It is not tuned to match the L-shaped result. |
| U-shaped | Premium-position stress | Measures the additional exposure available at a deliberately strong deployment position and reveals route-overload behavior. It is not required to produce the most kills for every family. |

All four Towers must run on all three route structures. Route results are compared as a suite, but they are not interchangeable samples and are not flattened into one route-independent outcome.

If a U-shaped result is unusually strong, first treat it as placement identity evidence. Do not change frozen Base Tower values merely to erase legitimate geometry advantage; only revise the baseline when the same family becomes structurally dominant across the route suite.

### 5.3 Measurement Priority

1. `Effective Damage` and `Damage Coverage` are the primary cross-family power measures.
2. `Killed`, `Final Player Health`, and `Average Leaked Remaining HP` describe damage conversion and Tower identity.
3. `Peak Alive`, battle duration, targeting observations, and misses are supporting diagnostics.

Kill counts are intentionally not equalized. A Tower that concentrates damage and a Tower that spreads damage may deliver comparable total value while producing different kill counts against the same fixture.

## 6. Frozen Base Combat v0.3

### 6.1 Shared Tower Values

| Tower | Base Attack Damage | Attack Range | Attack Cycle Duration | Target Selection |
|---|---:|---:|---:|---|
| Archer | `2` | `2` | `0.85s` | Lowest Health |
| Cannon | `6` | `3` | `3.5s` | Highest Health |
| Magic | `3` | `1.5` | `20s` | Baseline contact behavior does not require a release target |
| Drone | `1` | `4` | `10s` | Lowest Health |

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
| Archer | `1` | `5 / 40` | `228` | `47.50%` | `7.200` | Victory |
| Cannon | `1` | `10 / 40` | `204` | `42.50%` | `9.200` | Victory |
| Magic | `2` | `0 / 40` | `216` | `45.00%` | `6.600` | Defeat |
| Drone | `1` | `7 / 40` | `205` | `42.71%` | `8.333` | Victory |

The L-shaped effective-damage range is `204` to `228`, so the four naked Towers remain in one broad primary-acceptance band. The first post-Task003A Magic diagnostic retained Rotation Speed `180` and produced `444` Effective Damage. That structural outlier led to the isolated `180 -> 90` revision; two subsequent L-shaped runs both produced `216` Effective Damage and established the accepted Magic baseline.

### 7.2 Straight Low-exposure Diagnostic

| Tower | Killed | Effective Damage | Damage Coverage | Average Leaked Remaining HP | Terminal State |
|---|---:|---:|---:|---:|---|
| Archer | `2 / 40` | `212` | `44.17%` | `7.053` | Victory |
| Cannon | `8 / 40` | `180` | `37.50%` | `9.375` | Victory |
| Magic | `0 / 40` | `111` | `23.13%` | `9.225` | Defeat |
| Drone | `4 / 40` | `186` | `38.75%` | `8.167` | Victory |

Magic's zero-kill Straight result is accepted identity evidence rather than zero output: it delivered `111` Effective Damage but distributed that damage without completing a kill. Together with its L and U results, this identifies Magic as highly dependent on sustained corner coverage rather than universally strong.

### 7.3 U-shaped Premium-position Stress

| Tower | Accepted Runs | Killed | Average Effective Damage | Average Damage Coverage | Average Leaked Remaining HP | Effective Damage vs L |
|---|---:|---:|---:|---:|---:|---:|
| Archer | `1` | `7 / 40` | `250` | `52.08%` | `6.970` | `+9.6%` |
| Cannon | `1` | `6 / 40` | `228` | `47.50%` | `7.412` | `+11.8%` |
| Magic | `2` | `1 / 40`, `2 / 40` | `327` | `68.13%` | `3.973` | `+51.4%` |
| Drone | `1` | `9 / 40` | `221` | `46.04%` | `8.355` | `+7.8%` |

Every family produced more Effective Damage as route exposure increased from Straight to L-shaped to U-shaped. Magic's two accepted U-shaped runs produced `321` and `333` Effective Damage, confirming a repeatable premium-position range rather than a one-run spike.

Magic's `111 -> 216 -> 327` route profile is an intentionally strong geometry response: weak on Straight, comparable on the authoritative L-shaped route, and exceptional at the U-shaped premium position. Its U-shaped damage still converted only one or two kills because contact damage was distributed across the wave. This is accepted specialization evidence, not a reason to equalize kill count or reduce a Base Tower value that remains comparable on L.

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
| Task002 | Accepted `Base Combat v0.3` fixture, route structure, parameters, and result record |
| Task003 | Required-Level plus Basic and Behaviour Upgrade calibration on top of this baseline |

## 9. Acceptance

- The four Level 1 attack strategies are recognizable from their runtime behavior.
- L-shaped Effective Damage falls within one broad comparable range without equalizing kill counts.
- Straight, L-shaped, and U-shaped results show the expected increase in exposure without establishing one universally superior Tower.
- Reference Monster Health `12` prevents the accepted route suite from reaching the kill ceiling.
- All accepted Level 1 values, fixture values, route roles, and results are recorded for later recovery and regression.

## 10. Validation Record

- Fixed-condition L-shaped Play Mode runs for all four Towers, including two identical accepted Magic runs after its isolated Rotation Speed revision
- Fixed-condition Straight low-exposure Play Mode run for all four Towers
- Fixed-condition U-shaped premium-position Play Mode runs for all four Towers, including two Magic runs within the accepted `321-333` Effective Damage range
- `CombatBalanceRunRecorder` output for fixture, damage, pressure, timing, integrity, Tower family, Tower level, resolved stats, and Upgrade layers
- Every accepted run reported `ResolutionCountsMatch=True` and `LeakCountMatchesPlayerHealthLoss=True`
- Live prefab verification of Reference Monster Health, all four Tower shared values, and Magic Orb Rotation Speed `90`

## 11. Superseded Base Combat Records

### 11.1 Base Combat v0.2

`Base Combat v0.2` was frozen on 2026-08-11 with the same `40 / HP 12 / Speed 0.25 / Spawn 2.5s` fixture and the same shared Tower stats now used by v0.3. Its Magic Orb used Rotation Speed `180`, Max Hit Count `14`, and Max Lifetime `18s`. Task003A's removal of gameplay hit-count exhaustion made that Magic baseline structurally obsolete and required v0.3.

| Tower | L-shaped | Straight | U-shaped |
|---|---:|---:|---:|
| Archer | `232` | `216` | `254` |
| Cannon | `204` | `180` | `228` |
| Magic | `238.5` | `216` | `252` |
| Drone | `203.5` | `186` | `222` |

Values are accepted Effective Damage. The full v0.2 evidence remains recoverable from repository history at the v0.2 freeze commit.

### 11.2 Base Combat v0.1

`Base Combat v0.1` was frozen on 2026-08-06 with `40` Monsters, Health `5`, Move Speed `0.25`, Spawn Interval `2.5s`, and Straight as the authoritative comparison. It is retained as historical evidence but no longer defines the current regression fixture.

The v0.1 shared Tower values matched v0.2 except that Cannon Base Attack Damage was `5`.

| Tower | Straight Route | Straight Average | L-shaped Route | U-shaped Route |
|---|---|---:|---|---|
| Archer | `29 / 40`, `28 / 40` | `28.5` | `36 / 40` | Not required |
| Cannon | `31 / 40`, `31 / 40` | `31` | `29 / 40`, `29 / 40` | Not required |
| Magic | `30 / 40`, `33 / 40`, `32 / 40` | `31.7` | `37 / 40` | `39 / 40` |
| Drone | `32 / 40`, `32 / 40` | `32` | `40 / 40` | Skipped because L-shaped reached the measurement ceiling |

## 12. Revision Policy

Task003 and later Tasks must keep the `Base Combat v0.3` Level 1 baseline fixed by default. Base Attack Damage is authored once by the Tower runtime template rather than repeated in TowerLevelConfig. Required-Level gates, Upgrade deltas, and Behaviour packages are the first calibration levers for later content; the Base Tower must not be changed merely to repair one Upgrade.

A Level Up whose contract and data remain model-and-eligibility-only does not require a repeated combat-output run; static schema and asset validation are sufficient unless runtime combat resolution code changes.

An ownership migration is not a `Base Combat` value revision, but it must rerun the Task002 route suite when the resolved-damage path changes.

If later evidence proves that the Level 1 baseline itself is structurally invalid, the change must be named as a new `Base Combat` revision. Keep the fixed fixture and route roles, rerun every naked Level 1 Tower on L-shaped primary acceptance plus Straight and U-shaped diagnostics, use Effective Damage and Damage Coverage as the primary measures, and retain v0.3 as historical evidence before replacing it. Downstream Upgrade checks must then be rerun from the new accepted controls.
