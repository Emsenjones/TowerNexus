# Task002 - Base Tower And Reference Monster Baseline

Status: Completed; `Base Combat v0.2` frozen on 2026-08-11

Depends on: Completed Task001 Stage1 greybox Map

## 1. Goal

Establish and preserve `Base Combat v0.2` for the four Level 1 Towers and one Reference Monster.

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
- A durable freeze point for `Base Combat v0.2`

## 4. Out Of Scope

- L2/L3 growth
- Basic, Behaviour, or Elemental Upgrade balance
- Exact first-hit delay or single-Monster TTK timing
- Permanent recording of each test deployment Grid
- Effect and Buff balance
- Monster roster variants
- Player Progress Requirements
- Stage Wave tuning

Task003A later approved removal of gameplay Magic Orb Maximum Hit Count and replacement of Lingering Orbit. Those changes are not retroactively written into this frozen v0.2 record. After Task003A implementation, Task002 must establish `Base Combat v0.3` before Task003 Upgrade calibration begins.

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
| L-shaped | Primary acceptance route | Represents the likely core-Tower placement around a strong corner and is the authoritative `Base Combat v0.2` comparison. |
| Straight | Low-exposure diagnostic | Shows how each family performs with limited route coverage and exposes reliance on corner geometry. It is not tuned to match the L-shaped result. |
| U-shaped | Premium-position stress | Measures the additional exposure available at a deliberately strong deployment position and reveals route-overload behavior. It is not required to produce the most kills for every family. |

All four Towers must run on all three route structures. Route results are compared as a suite, but they are not interchangeable samples and are not flattened into one route-independent outcome.

If a U-shaped result is unusually strong, first treat it as placement identity evidence. Do not change frozen Base Tower values merely to erase legitimate geometry advantage; only revise the baseline when the same family becomes structurally dominant across the route suite.

### 5.3 Measurement Priority

1. `Effective Damage` and `Damage Coverage` are the primary cross-family power measures.
2. `Killed`, `Final Player Health`, and `Average Leaked Remaining HP` describe damage conversion and Tower identity.
3. `Peak Alive`, battle duration, targeting observations, and misses are supporting diagnostics.

Kill counts are intentionally not equalized. A Tower that concentrates damage and a Tower that spreads damage may deliver comparable total value while producing different kill counts against the same fixture.

## 6. Frozen Base Combat v0.2

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
| Rotation Speed | `180` |
| Orbit Radius | `1` |
| Contact Distance | `0.25` |
| Same Target Hit Cooldown | `0.5s` |
| Max Hit Count | `14` |
| Max Lifetime | `18s` |

The Magic Attack Cycle begins when a complete Orb group is successfully activated and runs concurrently with that group. Early Max Hit Count exhaustion therefore produces a longer inactive remainder before the next fixed release boundary. Per-target contact cooldown remains unchanged; no route-shape detection or shared Global Target Cooldown is used.

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

| Tower | Accepted Runs | Killed | Average Effective Damage | Average Damage Coverage | Average Leaked Remaining HP |
|---|---:|---:|---:|---:|---:|
| Archer | `1` | `5 / 40` | `232` | `48.33%` | `7.086` |
| Cannon | `2` | `10 / 40` | `204` | `42.50%` | `9.200` |
| Magic | `2` | `2 / 40` | `238.5` | `49.69%` | `6.356` |
| Drone | `2` | `7 / 40` | `203.5` | `42.40%` | `8.379` |

The L-shaped effective-damage range is `203.5` to `238.5`. Cannon's Base Attack Damage revision from `5` to `6` moved it from the superseded `161` damage diagnostic to an accepted repeatable `204` damage result without changing its Range or Attack Cycle Duration.

### 7.2 Straight Low-exposure Diagnostic

| Tower | Killed | Effective Damage | Damage Coverage | Average Leaked Remaining HP | Terminal State |
|---|---:|---:|---:|---:|---|
| Archer | `2 / 40` | `216` | `45.00%` | `6.947` | Victory |
| Cannon | `8 / 40` | `180` | `37.50%` | `9.375` | Victory |
| Magic | `0 / 40` | `216` | `45.00%` | `6.600` | Defeat |
| Drone | `4 / 40` | `186` | `38.75%` | `8.167` | Victory |

Magic's zero-kill Straight result is accepted identity evidence rather than zero output: it delivered `216` Effective Damage, equal to Archer, but distributed that damage without completing a kill. This is why Effective Damage remains the primary comparison measure.

### 7.3 U-shaped Premium-position Stress

| Tower | Killed | Effective Damage | Damage Coverage | Average Leaked Remaining HP | Effective Damage vs L |
|---|---:|---:|---:|---:|---:|
| Archer | `7 / 40` | `254` | `52.92%` | `6.848` | `+9.5%` |
| Cannon | `4 / 40` | `228` | `47.50%` | `7.000` | `+11.8%` |
| Magic | `4 / 40` | `252` | `52.50%` | `6.333` | `+5.7%` |
| Drone | `9 / 40` | `222` | `46.25%` | `8.323` | `+9.1%` |

Every family produced more Effective Damage as route exposure increased from Straight to L-shaped to U-shaped. No U-shaped result reached the `40 / 40` kill ceiling or exceeded `52.92%` Damage Coverage.

Cannon nevertheless converted fewer kills on U-shaped than on L-shaped despite dealing more total damage. This confirms that U-shaped placement is a stress condition rather than a universal best-result requirement, and that kill conversion remains family- and geometry-dependent.

### 7.4 Accepted Identity Reading

- Archer retains fast, reliable finishing and scales moderately with added route exposure.
- Cannon retains heavy-hit concentration and long range; its kill conversion changes strongly with when and where captured targets are hit.
- Magic retains broad contact damage and corner-focused coverage; total damage can be competitive even when kills are low.
- Drone retains mobile pursuit, Burst cadence, battery duration, and active-entity limits, with stronger conversion when route exposure creates follow-up opportunities.

No Tower is accepted as universally superior. The route suite establishes comparable delivered damage while preserving different cadence, concentration, coverage, and conversion identities.

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Campaign experience intent, not combat implementation |
| Tower content | Frozen Level 1 authoring and family identity |
| Tower Runtime Combat | Attack execution and semantic timing contracts |
| Monster content | Frozen Reference health, movement, and presentation |
| Task002 | Accepted `Base Combat v0.2` fixture, route structure, parameters, and result record |
| Task003 | Required-Level plus Basic and Behaviour Upgrade calibration on top of this baseline |

## 9. Acceptance

- The four Level 1 attack strategies are recognizable from their runtime behavior.
- L-shaped Effective Damage falls within one broad comparable range without equalizing kill counts.
- Straight, L-shaped, and U-shaped results show the expected increase in exposure without establishing one universally superior Tower.
- Reference Monster Health `12` prevents the accepted route suite from reaching the kill ceiling.
- All accepted Level 1 values, fixture values, route roles, and results are recorded for later recovery and regression.

## 10. Validation Record

- Fixed-condition L-shaped Play Mode runs for all four Towers, including repeated Cannon, Magic, and Drone runs
- Fixed-condition Straight low-exposure Play Mode run for all four Towers
- Fixed-condition U-shaped premium-position Play Mode run for all four Towers
- `CombatBalanceRunRecorder` output for fixture, damage, pressure, timing, integrity, Tower family, Tower level, resolved stats, and Upgrade layers
- Live prefab verification of Reference Monster Health and all four Tower shared values

## 11. Superseded Base Combat v0.1 Record

`Base Combat v0.1` was frozen on 2026-08-06 with `40` Monsters, Health `5`, Move Speed `0.25`, Spawn Interval `2.5s`, and Straight as the authoritative comparison. It is retained as historical evidence but no longer defines the current regression fixture.

The v0.1 shared Tower values matched v0.2 except that Cannon Base Attack Damage was `5`.

| Tower | Straight Route | Straight Average | L-shaped Route | U-shaped Route |
|---|---|---:|---|---|
| Archer | `29 / 40`, `28 / 40` | `28.5` | `36 / 40` | Not required |
| Cannon | `31 / 40`, `31 / 40` | `31` | `29 / 40`, `29 / 40` | Not required |
| Magic | `30 / 40`, `33 / 40`, `32 / 40` | `31.7` | `37 / 40` | `39 / 40` |
| Drone | `32 / 40`, `32 / 40` | `32` | `40 / 40` | Skipped because L-shaped reached the measurement ceiling |

## 12. Revision Policy

Task003 and later Tasks must keep the `Base Combat v0.2` Level 1 baseline fixed by default. Base Attack Damage is authored once by the Tower runtime template rather than repeated in TowerLevelConfig. Required-Level gates, Upgrade deltas, and Behaviour packages are the first calibration levers for later content; the Base Tower must not be changed merely to repair one Upgrade.

A Level Up whose contract and data remain model-and-eligibility-only does not require a repeated combat-output run; static schema and asset validation are sufficient unless runtime combat resolution code changes.

An ownership migration is not a `Base Combat` value revision, but it must rerun the Task002 route suite when the resolved-damage path changes.

If later evidence proves that the Level 1 baseline itself is structurally invalid, the change must be named as a `Base Combat` revision. The revised Tower must rerun L-shaped primary acceptance plus Straight and U-shaped diagnostics, then rerun downstream Upgrade checks before replacing `Base Combat v0.2`.

Task003A is one such named structural revision because normal Magic Orb completion changes from hit-count exhaustion or lifetime expiry to lifetime expiry only. After Task003A implementation:

1. Keep the `40 / HP 12 / Speed 0.25 / Spawn 2.5s` fixture and the existing L, Straight, and U route roles.
2. Run every naked Level 1 Tower on every route, not Magic alone, so the common baseline remains comparable after shared runtime changes.
3. Use Effective Damage and Damage Coverage as the primary comparison; do not force equal kill counts.
4. Adjust a naked-Tower base value only when the revised route suite shows a structural outlier. Upgrade values are not used to repair the baseline.
5. Record and freeze the accepted results as `Base Combat v0.3`, retaining v0.2 as historical evidence.
6. Begin Task003 final Upgrade calibration only after v0.3 is accepted.
