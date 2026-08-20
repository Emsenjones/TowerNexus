# Task002 - Level 1 Base Tower Baseline

Status: Planned; waits for accepted Task001 implementation

Depends on: Task001 Combat Damage Formula Refactor

Blocks: Task003-Task014

## 1. Goal

Calibrate the four Level 1 Towers without any Upgrade so their delivered combat
value remains in one broad comparable range while preserving distinct cadence,
coverage, concentration, and conversion identities.

## 2. Fixed Fixture

- One Level 1 Tower at a time
- No Basic, Behaviour, or Elemental Upgrade
- Reference Monster candidate: HP `120`, MoveSpeed `0.25`
- One homogeneous Wave of `40`
- SpawnInterval `2.5s`
- L route as primary acceptance
- Straight and U routes as low- and high-exposure diagnostics
- Same family-specific placement within each route comparison

The current `20 / 60 / 30 / 10` L1 values are migration candidates, not frozen
CombatMathV2 values.

## 3. Measurements

- Effective Damage and Damage Coverage
- Kill count and leak count
- Average leaked remaining HP
- Overkill and target distribution
- Attack count and successful damage-event count
- Route exposure response
- Recorder fixture and integrity flags

Kill counts are not equalized. Effective Damage and identity are the primary
cross-family comparison.

## 4. Acceptance

- No family is structurally unusable or universally dominant.
- L-route Effective Damage lies in one reviewed broad band.
- Straight/L/U results retain recognizable family identities.
- Target selection avoids the known all-residual-health opening failure.
- Every report confirms L1 BasicDamage, scale `1` primary damage, no Upgrades,
  and complete resolution integrity.

## 5. Handoff

The accepted Task002 L1 values become Task003's `B1` comparison unit and
Task006's reference pressure input.

