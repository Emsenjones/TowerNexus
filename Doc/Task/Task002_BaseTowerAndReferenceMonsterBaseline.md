# Task002 - Base Tower And Reference Monster Baseline

Status: Ready for implementation; Task001 v0.2 greybox Maps are accepted and Stage1 can now be frozen as the fixed Reference test setup

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
- Attack Interval or release cadence
- Projectile or Attack Entity speed and lifetime
- Targeting behavior
- Magic contact behavior, active-group duration, and post-completion recovery cadence
- Drone launch, battery, pursuit, and burst cadence
- Reference Monster maximum health and movement speed
- Base Tower damage and measured TTK
- A documented freeze point for `Base Combat v0.1`

## 4. Out Of Scope

- L2/L3 growth
- Basic, Behaviour, or Elemental Upgrades
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

### Current Magic Calibration Checkpoint

- The last Straight-route candidate is restored to Attack Interval `3s`, Orb Max Lifetime `17s`, and Orb Max Hit Count `15`.
- A three-sided U-shaped route is treated as an intentional Magic-favored stress case, not as the neutral baseline.
- An L-shaped route test remains pending before any decision to replace per-target contact cooldown with a shared Orb contact cadence.
- No contact-cooldown mechanism change is accepted by this checkpoint.

## 6. Required Measurements

- First attack or first-hit delay
- TTK against the Reference Monster
- Number of releases, contacts, or bursts
- Effective route coverage
- Target-loss or entity-limit downtime
- Magic active-group duration and post-completion recovery downtime
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
