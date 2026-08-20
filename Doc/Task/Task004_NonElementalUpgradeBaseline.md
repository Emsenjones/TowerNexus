# Task004 - Non-Elemental Upgrade Baseline

Status: Planned

Depends on: Accepted Task003 Tower Level Damage Curve

Blocks: Task005-Task014

## 1. Goal

Recalibrate every current Basic and Behaviour Upgrade on the accepted Level
BasicDamage curve. Each Upgrade must create coherent value without repairing an
unusable Base Tower or relying on accidental repeated triggers.

## 2. Comparison Contract

- Test each Upgrade at its minimum Required Tower Level.
- Compare against the same family, same level, same route, and no-Upgrade
  control.
- Reuse L as the primary route.
- Use Straight/U only for material range, alignment, route-exposure, completion,
  or density dependencies.
- Treat current CombatMathV1 values as first candidates only.

## 3. Review Bands

| Content | Initial Review Guide |
|---|---|
| Basic | Reliable numerical or spatial value; ordinary damage/cadence cases approximately `1.15x-1.30x` |
| Behaviour | Visible strategy change; intended-condition cases approximately `1.35x-1.65x` |
| Range/alignment exception | May fall outside ordinary bands when spatial value is demonstrated |
| Pair or three-Upgrade build | Bounded composition, correct trigger ownership, and no unexplained multiplication |

These are decision aids rather than universal formulas.

## 4. Required Coverage

- all Basic definitions;
- all twelve current Behaviour packages;
- every additional-member DamageScale;
- TowerScaled Effect damage;
- selected Basic/Behaviour and Behaviour/Behaviour pairs;
- representative three-Upgrade Core builds;
- duplicate, Required Level, package capacity, and live refresh checks.

## 5. Acceptance

- Every Upgrade has a reviewed same-level control and decision.
- Basic value is reliable and Behaviour value is conditionally stronger where
  its mechanic is intended to work.
- Higher current BasicDamage naturally strengthens TowerScaled results.
- No FixedBuff damage appears in this Task's gain calculation.
- Combinations remain bounded and preserve TowerFamily identity.

