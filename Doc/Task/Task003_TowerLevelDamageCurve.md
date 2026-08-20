# Task003 - Tower Level Damage Curve

Status: Planned

Depends on: Accepted Task002 Level 1 Base Tower Baseline

Blocks: Task004-Task014

## 1. Goal

Calibrate each TowerFamily's L1-L3 BasicDamage so every Level Up provides a
readable damage step and greater nominal marginal damage than deploying another
undeveloped Level 1 Tower of the same family.

## 2. Pure-Damage Guardrail

For each family:

```text
B2 - B1 > B1
B3 - B2 > B1
```

The first candidate curve is `1.0 / 2.2 / 3.4` times B1. It is a hypothesis,
not an accepted global multiplier.

## 3. Controlled Comparisons

- one L2 versus two L1 Towers at equal Tower-Draft cost;
- one L3 versus three L1 Towers;
- one L3 versus one L2 plus one L1;
- no Tower Upgrades;
- fixed route, Monster fixture, placements, and elapsed exposure;
- theoretical nominal damage plus empirical Effective Damage.

Nominal damage must favor Level investment. Real-map Effective Damage may favor
horizontal expansion when extra coverage, route shaping, target concurrency, or
conversion explains the result.

## 4. Additional Checks

- Current Level model and BasicDamage change together.
- In-flight and persistent Tower-owned future damage observes the new level.
- Already resolved damage is unchanged.
- Cannon one-shot and overkill thresholds remain intentional.
- Magic/Drone long-lived entities produce monotonic damage growth.
- Rounding audit passes for every primary damage source.

## 5. Acceptance

- All four families satisfy the marginal guardrail.
- L2 and L3 steps are visible without requiring an Upgrade.
- Level investment does not erase legitimate deployment value.
- No family-specific threshold invalidates its Task002 identity.
- Accepted integer BasicDamage is recorded for every family and level.

