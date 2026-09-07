# Task003 - Tower Level Damage Curve

Status: Completed; accepted on 2026-08-21 with Straight-route HP480 Play Mode
evidence for all four TowerFamilies; Magic BaseCycle22 regression accepted on
2026-08-22

Depends on: Accepted Task002 Level 1 Base Tower Baseline

Unblocks: Task004-Task007 and Task010-Task017

## 1. Goal

Calibrate each TowerFamily's L1-L3 BasicDamage so every Level Up provides a
readable damage step and greater nominal marginal damage than deploying another
undeveloped Level 1 Tower of the same family.

Task003 accepts the initial family-specific BasicDamage candidates. No
BasicDamage asset revision was required after the controlled comparisons.

## 2. Pure-Damage Guardrail

For each family:

```text
B2 - B1 > B1
B3 - B2 > B1
```

The initial `1.0 / 2.2 / 3.4` curve remains a useful first candidate rather
than a required global multiplier. Archer, Cannon, and Drone retain that curve.
Magic retains its stronger family-specific curve because orbit-contact
conversion is less deterministic than ordinary direct attacks.

## 3. Accepted BasicDamage

| TowerFamily | L1 | L2 | L3 | L1 -> L2 Marginal | L2 -> L3 Marginal | Decision |
|---|---:|---:|---:|---:|---:|---|
| Archer | `20` | `44` | `68` | `24 > 20` | `24 > 20` | Keep |
| Cannon | `60` | `132` | `204` | `72 > 60` | `72 > 60` | Keep |
| Magic | `25` | `66` | `102` | `41 > 25` | `36 > 25` | Keep |
| Drone | `10` | `22` | `34` | `12 > 10` | `12 > 10` | Keep |

Every family satisfies the nominal marginal guardrail. The accepted values are
the authoritative Task004 Level controls.

## 4. Controlled Fixture

Both accepted comparison fixtures use:

- Straight Route;
- one measurement Wave of `40` HP480 Monsters;
- Move Speed `0.25` and Spawn Interval `2.5s`;
- Player Health `100`;
- no Tower Upgrades;
- fixed P1/P2/P3 placements, with horizontal arrangements using separated
  positions so additional coverage remains a legitimate deployment benefit.

Two-Draft comparisons use a setup Wave of `2` HP60 Monsters, one active
Progress Requirement `2`, and a terminal sentinel requirement. They compare
one L2 at P1 with two L1 Towers at P1/P2 and resolve `42 / 42` Monsters.

Three-Draft comparisons use a setup Wave of `4` HP60 Monsters, Progress
Requirements `[2, 2, 99]`, and compare one L3 at P1, one L2 plus one L1 at
P1/P2, and three L1 Towers at P1/P2/P3. They resolve `44 / 44` Monsters.

The measurement-Wave delay is `25s` for Archer, Cannon, and Drone. Magic clean
acceptance runs use `90s` because one low-level Magic Tower cannot resolve the
setup Wave before the shorter delay. In every accepted run, the requested
Tower arrangement is committed before measurement exposure begins.

The HP60 setup Wave exists only to resolve the one or two required Level-Up
Drafts. Accepted Effective Damage comes from the HP480 measurement Wave.

## 5. Accepted Play Mode Evidence

### 5.1 Two-Draft Comparison

| TowerFamily | Two L1 Effective Damage | One L2 Effective Damage | L2 Difference | Decision |
|---|---:|---:|---:|---|
| Archer | `4320` | `4748` | `+428` (`+9.9%`) | Keep |
| Cannon | `3780` | `4044` | `+264` (`+7.0%`) | Keep |
| Magic | `3025` | `3102` | `+77` (`+2.5%`) | Keep |
| Drone | `3730` | `4142` | `+412` (`+11.0%`) | Keep |

One L2 outperformed two separated L1 Towers for every family despite the
horizontal arrangement's additional coverage.

### 5.2 Three-Draft Comparison

Repeated near-threshold observations use their accepted mean. Single clear
observations retain the measured value.

| TowerFamily | Three L1 | L2 + L1 | One L3 | L3 vs L2 + L1 | Decision |
|---|---:|---:|---:|---:|---|
| Archer | `6380` | `6896` | `7280` | `+384` (`+5.6%`) | Keep |
| Cannon | `5520` | `5952` | `5988` | `+36` (`+0.6%`) | Keep; exact repeat |
| Magic | `4200` mean | `4983` mean | `5032` mean | `+49` (`+1.0%`) | Keep; orbit-phase variance |
| Drone | `5460` | `5866` mean | `5984` mean | `+118` (`+2.0%`) | Keep; stable repeat |

Every L3 also exceeded the corresponding three-L1 observation. The small
upper-step margins are intentional: Level investment owns greater nominal
damage while horizontal deployment retains real coverage and concurrency value.

## 6. Family-Specific Interpretation

### 6.1 Archer

Both Level steps converted clearly on the Straight fixture. The direct,
high-cadence identity produced no threshold exception.

### 6.2 Cannon

Cannon L3 versus L2 plus L1 repeated exactly at `5988` versus `5952`.
One-shot, attack-window, and overkill thresholds compress the empirical margin,
but they do not reverse it or invalidate Cannon's concentrated-hit identity.

### 6.3 Magic

Accepted clean L3 observations were `4896`, `5100`, and `5100`. These represent
`48`, `50`, and `50` successful HP480-Wave damage applications at `102` damage.
Magic Orb releases use a random initial orbit phase, so otherwise identical
runs may gain or lose a small number of contacts. The three-run mean remains
above the stable L2-plus-L1 mean, while the nominal guardrail remains the
balance authority.

The variance is a reviewed mechanism result rather than evidence for a global
Magic efficiency change. Task003 does not require every random real-map sample
to outperform every horizontal-deployment sample.

After Task004 revised the shared Magic Attack Cycle from `20s` to `22s`, the
affected Level curve was rerun without changing `25 / 66 / 102` BasicDamage:

| Comparison | BaseCycle22 observations | Mean | Difference |
|---|---:|---:|---:|
| Two L1 | `2675 / 2650 / 2750` | `2692` | - |
| One L2 | `2904 / 2838 / 2838` | `2860` | `+168` (`+6.2%`) |
| Three L1 | `3925 / 4025 / 3800` | `3917` | - |
| L2 + L1 | `4165 / 4504 / 4511` | `4393` | `+476` (`+12.2%`) vs three L1 |
| One L3 | `4692 / 4590 / 4284` | `4522` | `+129` (`+2.9%`) vs L2 + L1 |

The corrected L2-plus-L1 set excludes the `(2,12)` placement observation and
uses only P2 `(2,11)` records. These Task004-scoped regressions used a common
`60s` measurement-Wave delay and remain diagnostic rather than replacements
for the original clean `90s` acceptance records. They nevertheless preserve
both monotonic empirical steps and the nominal marginal guardrail, so Magic
L2/L3 BasicDamage remains `66 / 102`.

### 6.4 Drone

Repeated L3 and L2-plus-L1 results stayed within approximately `1.1%` of their
first observations. The L3 mean remains above the alternative arrangement and
confirms monotonic long-lived-entity damage growth.

## 7. Recorder And Integrity Review

Accepted evidence is stored in `Doc/GamePlayRecord` with the
`Task003_StraightRoute_HP480_*` prefix.

Accepted records confirm:

- `Victory` terminal state;
- `42 / 42` Monsters in two-Draft runs and `44 / 44` Monsters in three-Draft
  runs, with no unresolved Monsters;
- resolution, health-loss, Monster-runtime, Wave-attribution, Tower-deployment,
  and damage-diagnostic counts match;
- final Tower families, levels, BasicDamage, deployment count, and arrangement
  match the named fixture;
- the measurement Wave begins after the final required Tower arrangement is
  committed.

The terminal sentinel requirement intentionally prevents an additional Draft
during the HP480 measurement Wave. Recorder fields that derive an expected
Draft from every serialized requirement may therefore remain false for these
diagnostic fixtures; they do not invalidate combat, deployment, or damage
evidence.

Early Magic records that used manual debug Draft timing before the `90s`
measurement delay remain exploratory records and do not own the accepted
Magic comparison.

## 8. Acceptance

- All four families satisfy the marginal guardrail.
- L2 and L3 steps are visible without requiring a Tower Upgrade.
- Level investment does not erase legitimate deployment value.
- Cannon thresholds and Magic/Drone long-lived entities have reviewed repeat
  evidence.
- Current Level and Tower-owned future damage observe the accepted
  BasicDamage; already resolved damage remains unchanged under the Task001
  transaction contract.
- Damage diagnostics and rounding ownership remain valid for every accepted
  primary source.
- No Tower BasicDamage change is required.

## 9. Handoff

Task004 uses these accepted per-level BasicDamage values as its same-level
control foundation. Task004 must recalibrate Non-Elemental Basic and Behaviour
Upgrades; it must not repair an Upgrade result by reopening Task003 unless a
family-specific Level threshold invalidates the accepted guardrail or Tower
identity.
