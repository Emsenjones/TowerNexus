# Task002 - Base Tower And Reference Monster Baseline

Status: In progress; Magic, Cannon, and Archer Level 1 candidates are frozen, while Drone calibration and the final cross-Tower stress pass remain pending

Depends on: Completed Task001 Stage1 greybox Map

## 1. Goal

Establish `Base Combat v0.1` using four Level 1 Towers and one Reference Monster on a fixed Stage1 Map setup.

The result must make the four attack strategies visibly different while keeping their same-cost combat value within one comparable baseline range.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/12_ProjectileSystem.md`

## 3. In Scope

- Fixed Stage1 test Map, route, Tower positions, and Monster setup
- Archer, Cannon, Magic, and Drone Level 1 attack identity
- Attack Range
- Attack Cycle Duration or release cadence
- Projectile or Attack Entity speed and lifetime
- Targeting behavior
- Magic contact behavior, active-group duration, and release-to-release Cycle cadence
- Lingering Orbit maximum-hit capacity as the explicit Magic calibration exception
- Drone launch, battery, pursuit, and burst cadence
- Reference Monster maximum health and movement speed
- Base Tower damage and measured TTK
- A documented freeze point for `Base Combat v0.1`

## 4. Out Of Scope

- L2/L3 growth
- Basic, Behaviour, or Elemental Upgrade balance beyond the explicit Lingering Orbit capacity restoration
- Effect and Buff balance
- Monster roster variants
- Player Progress Requirements
- Stage Wave tuning

## 5. Calibration Sequence

1. Fix one Stage1 Map version and one legal test position per Tower.
2. Fix one Reference Monster and one repeatable spawn path.
3. Adjust how each Tower attacks before adjusting final damage.
4. Use the same Reference Monster to compare first-hit delay and TTK.
5. Adjust Tower damage and Reference Monster health together.
6. Run a normal-route comparison to include range, pursuit, contact, and downtime.
7. Record accepted values and freeze the baseline.

### Fixed Reference Run

- One Wave with `40` Reference Monsters
- Spawn Interval `2.5s`
- Reference Monster Max Health `5` and Move Speed `0.25`
- One Level 1 Tower with no Upgrades
- Fixed Straight, L-shaped, and U-shaped test routes with one recorded legal placement per Tower

Wave count, Monsters per Wave, Wave Delay, route shape, and placement are part of the test condition. Results from different Wave structures or placements are not interchangeable.

### Current Level 1 Calibration Checkpoint

- Magic uses Attack Cycle Duration `20s`, Orb Max Lifetime `18s`, and base Orb Max Hit Count `14`. Straight results were `30 / 33 / 32`, L-shaped result was `37`, and U-shaped result was `39` kills from 40.
- The Attack Cycle begins only after a complete Orb group is successfully activated. It runs concurrently with the group, so early Max Hit Count exhaustion creates a longer inactive remainder without advancing the next release boundary.
- Per-target contact cooldown remains unchanged. No shared or global contact cooldown and no route-shape detection enters this refactor.
- Lingering Orbit is restored as a Basic maximum-hit capacity Upgrade. Its first Play Mode candidate is `+5`; the final delta remains subject to the fixed-condition Upgrade run.
- Cannon uses Attack Cycle Duration `3.5s`, Damage `5`, and Highest Health targeting. Two Straight runs each released and hit `31` Shells for `31` kills. Two L-shaped runs each killed `29`; each gained additional release opportunities but missed six captured positions after Monsters moved away.
- Archer uses Attack Cycle Duration `0.85s`, Damage `2`, and Lowest Health targeting. Straight results were `29 / 28`, and the L-shaped result was `36` kills from 40. Baseline Direction Projectiles had no observed misses in the recorded runs.
- Drone remains under calibration. Its current diagnostic results are `17` Straight kills and `25` L-shaped kills; these values are not accepted as the frozen baseline.

## 6. Required Measurements

- First attack or first-hit delay
- TTK against the Reference Monster
- Number of releases, contacts, or bursts
- Effective route coverage
- Target-loss or entity-limit downtime
- Magic active-group duration, completion cause, and release-to-release Cycle duration
- Whether any Tower dominates range, cadence, damage, and flexibility simultaneously
- Short player-facing description of each Tower's strength and weakness

## 7. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Campaign experience intent, not combat implementation |
| Tower content | Base authoring and Level 1 identity |
| Tower Runtime Combat | Attack execution and measured runtime behavior |
| Monster content | Reference health, movement, and presentation |
| Task002 | Fixed test, comparison, and accepted baseline record |

## 8. Execution Collaboration

- The user owns the fixed Unity test setup, Tower and Monster asset authoring, repeated Play Mode runs, and the final subjective judgment that the four attack identities feel sufficiently distinct.
- Codex prepares the comparison table, proposes first-pass parameter relationships, checks broad same-cost value, and identifies universal dominance or unclear identity from the user's observations.
- The user may stop the identity pass when the differences are clearly readable. Accepted values then become `Base Combat v0.1` and remain fixed unless later evidence opens an explicit baseline revision.

## 9. Unity Authoring Checklist

- Disable all Tower Upgrades in the fixed test.
- Use one Level 1 Tower at a time.
- Use one fixed Reference Monster definition and route.
- Record all authored values used by the run.
- Repeat each comparison under the same simulation conditions.
- Preserve the Stage1 Map version during the comparison.

## 10. Acceptance Criteria

- The four Tower attack strategies are recognizable without reading raw values.
- Same-cost Level 1 combat value is in one broad comparable range.
- No Tower is universally superior.
- Reference Monster health and movement produce a useful test window.
- The accepted baseline is recorded clearly enough to reproduce.

## 11. Validation

- Fixed-condition Play Mode runs
- Repeat TTK observations
- Route-coverage comparison
- Static validation of changed assets
- Regression run after the final accepted change

## 12. Review Note

Tower damage and Reference Monster health remain one coupled calibration problem. This Task must not approve either side in isolation.
