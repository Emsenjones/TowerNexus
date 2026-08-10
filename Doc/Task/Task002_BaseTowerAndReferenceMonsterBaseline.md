# Task002 - Base Tower And Reference Monster Baseline

Status: Completed; `Base Combat v0.1` frozen on 2026-08-06

Depends on: Completed Task001 Stage1 greybox Map

## 1. Goal

Establish and preserve `Base Combat v0.1` for the four Level 1 Towers and one Reference Monster.

The frozen record provides a recovery point for later development: future Level, Upgrade, Stage, or runtime work can compare against the accepted Level 1 values instead of changing them unintentionally or losing the previous baseline.

The four attack strategies must remain visibly different while their same-cost Straight-route combat value stays within one broad comparable range.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/12_ProjectileSystem.md`

## 3. In Scope

- Archer, Cannon, Magic, and Drone Level 1 attack identity
- Base damage, Attack Range, Attack Cycle Duration, and targeting behavior
- Projectile or Attack Entity movement, lifetime, contact, and cadence values
- Reference Monster maximum health and movement speed
- Fixed-condition Straight-route comparison
- L-shaped and selective U-shaped route-coverage observations
- A durable freeze point for `Base Combat v0.1`

## 4. Out Of Scope

- L2/L3 growth
- Basic, Behaviour, or Elemental Upgrade balance
- Exact first-hit delay or single-Monster TTK timing
- Permanent recording of each test deployment Grid
- Effect and Buff balance
- Monster roster variants
- Player Progress Requirements
- Stage Wave tuning

Lingering Orbit was restored during Task002 with its current `Magic Orb Max Hit Count +5` authoring value. Its final Upgrade balance belongs to Task003 with the other Basic and Behaviour Upgrades.

## 5. Fixed Reference Run

- One Wave with `40` Reference Monsters
- Spawn Interval `2.5s`
- Reference Monster Max Health `5`
- Reference Monster Move Speed `0.25`
- Player Max Health `40`, allowing final health to report the number of kills directly
- One Level 1 Tower at a time
- No Upgrades
- Straight route as the authoritative same-cost baseline
- L-shaped route as the primary route-coverage comparison
- U-shaped route only when the L-shaped result has not already reached the `40 / 40` measurement ceiling or when a family-specific mechanic still needs coverage

The user held route and placement consistent within each comparison. Exact deployment Grid coordinates are intentionally not part of the permanent record. Results from materially different route shapes or placements remain separate observations rather than interchangeable samples.

## 6. Frozen Base Combat v0.1

### 6.1 Shared Tower Values

| Tower | Base Attack Damage | Attack Range | Attack Cycle Duration | Target Selection |
|---|---:|---:|---:|---|
| Archer | `2` | `2` | `0.85s` | Lowest Health |
| Cannon | `5` | `3` | `3.5s` | Highest Health |
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

| Tower | Straight Route | Straight Average | L-shaped Route | U-shaped Route |
|---|---|---:|---|---|
| Archer | `29 / 40`, `28 / 40` | `28.5` | `36 / 40` | Not required |
| Cannon | `31 / 40`, `31 / 40` | `31` | `29 / 40`, `29 / 40` | Not required |
| Magic | `30 / 40`, `33 / 40`, `32 / 40` | `31.7` | `37 / 40` | `39 / 40` |
| Drone | `32 / 40`, `32 / 40` | `32` | `40 / 40` | Skipped because L-shaped already reached the measurement ceiling |

The Straight-route averages occupy the accepted `28.5` to `32` range. Route results are identity observations rather than a requirement that every bend improve every Tower.

- Archer fired fast Direction Projectiles with no observed baseline misses. Lowest Health targeting improved its ability to finish damaged Monsters, while short route exposure could still leave Monsters at `1` HP.
- Cannon killed the Reference Monster in one successful hit. Its Position Snapshot Arc Projectile could miss at a bend after the captured position became stale; both accepted L-shaped runs recorded six misses.
- Magic used evenly distributed Orb contact damage and gained strongly from corner coverage. Max Hit Count, Max Lifetime, and the fixed Attack Cycle bound its Level 1 output without route-shape-specific logic.
- Drone used low-damage Burst fire and pursuit. Four baseline Burst shots did not kill the `5` HP Reference Monster; the next Burst completed the kill. Longer L-shaped coverage converted those follow-up opportunities into a `40 / 40` ceiling result.

No Tower was accepted as universally superior. Cannon retained one-hit power and long range, Archer retained fast reliable finishing, Magic retained corner-focused contact coverage, and Drone retained mobile pursuit with Burst, battery, and active-entity limits.

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Campaign experience intent, not combat implementation |
| Tower content | Frozen Level 1 authoring and family identity |
| Tower Runtime Combat | Attack execution and semantic timing contracts |
| Monster content | Frozen Reference health, movement, and presentation |
| Task002 | Accepted `Base Combat v0.1` parameter and result record |
| Task003 | Required-Level plus Basic and Behaviour Upgrade calibration on top of this baseline |

## 9. Acceptance

- The four Level 1 attack strategies are recognizable from their runtime behavior.
- Same-cost Straight-route combat value falls within one broad comparable range.
- No Tower is universally superior across damage pattern, cadence, route coverage, and flexibility.
- Reference Monster health and movement produce a useful non-ceiling Straight test window.
- All accepted Level 1 and Reference Monster values are recorded for later recovery and regression.

## 10. Validation Record

- Repeated fixed-condition Straight-route Play Mode runs for all four Towers
- L-shaped route-coverage runs for all four Towers
- Selective Magic U-shaped run before the U-shaped ceiling policy was adopted
- Final Drone regression at Burst Cooldown `1.3s`: two Straight runs at `32 / 40` and one L-shaped run at `40 / 40`
- Static project build and changed-file validation after the final document and asset update

## 11. Revision Policy

Task003 and later Tasks must keep this Level 1 test baseline fixed by default. Base Attack Damage is authored once by the Tower runtime template rather than repeated in TowerLevelConfig. Required-Level gates, Upgrade deltas, and Behaviour packages are the first calibration levers for later content; the Base Tower must not be changed merely to repair one Upgrade.

This ownership migration is not a `Base Combat` value revision, but it must rerun the Task002 Base Combat regression because the resolved-damage path changes.

If later evidence proves that the Level 1 baseline itself is structurally invalid, the change must be named as a `Base Combat` revision. The revised Tower must rerun the Task002 Straight comparison, relevant route regression, and downstream Task003 checks before replacing `Base Combat v0.1`.
