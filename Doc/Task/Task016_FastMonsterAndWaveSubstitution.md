# Task016 - Fast Monster And Wave Substitution

Status: Planned

Depends on: Accepted Task010-Task015 fixed-speed Stage calibrations

## 1. Goal

Add the first low-HP, high-MoveSpeed Monster identity and replace a small number
of already accepted fixed-speed Waves to create urgency, Target pressure, and
coverage checks without reopening the entire HP roster.

## 2. Initial Identity

The first candidate uses `Prefab_Monster_Bat_Rush`.

Its exact HP and MoveSpeed are Task outputs. It must have lower survivability
than the same-Stage high-pressure body and higher MoveSpeed than the fixed
`0.25` baseline.

## 3. Formation Contract

Every standard Rush Wave derives its explicit SpawnInterval from the accepted
spatial gap:

```text
SpawnInterval = 0.625 / Authored Rush MoveSpeed
```

Faster movement must not silently create a wider or denser initial formation.
Any deliberate formation exception is separately authored and named.

## 4. Substitution Rules

- Begin with one candidate Rush Profile, not multiple speculative speed roles.
- Replace selected Waves; do not add hidden extra Monster budget by default.
- State the intended pressure change for every substituted Wave.
- Preserve the opening low-pressure formation unless Stage evidence requires a
  later opening-speed lesson.
- Re-run only affected Stages, but include their Reference, coherent
  alternative, and relevant Anti-pattern.

## 5. Required Regressions

- centerline spacing and lane offset;
- Spawn and Target center behavior;
- Direction and Arc projectile interaction;
- Cannon target-position snapshot and potential miss behavior;
- Slow, Frozen, Buff continuity, and movement lock;
- route reprojection and forced displacement;
- per-Wave pressure, leaks, and leading-Monster timing.

## 6. Acceptance

- Rush is readable from speed and lower HP rather than hidden resistance.
- Substituted Waves change urgency without invalidating accepted build roles.
- Standard spatial gap remains approximately `0.625`.
- No affected Reference Build becomes dependent on one exact random Draft.
- Every affected Stage retains its accepted pass/fail build envelope after the
  reviewed substitution.

## 7. Downstream

Natural Draft probability calibration remains a separate later Task. Controlled
Stage reports may inform it, but Fixed-mode displayed choices are not natural
frequency evidence.

