# Task003B - Combat Value Scale And Secondary Attack Member Damage Revision

Status: Completed; source/assets and System contracts are implemented, Unity import/reserialization and Play Mode feature checks passed, Task002 `Base Combat v0.4` is accepted, and Task003 v0.2 calibration is complete

Depends on: Completed Task003A; accepted Task002 `Base Combat v0.3`; first complete L-route single-Upgrade Draft screen for all four TowerFamilies

## 1. Goal

Increase the combat integer scale so deterministic Basic Damage deltas can be calibrated without probability, give every Behaviour package that adds Attack Entities an explicit additional prefab/count/Basic Damage contract, and install the approved first calibration candidates before the next full regression.

## 2. Contract Revision

- Multiply the reference Monster health and every established base/fixed damage value by `10` before applying named Task003B candidate revisions.
- Keep range, cadence, speed, radius, duration, count, and other non-damage values unscaled unless a named candidate changes them.
- High-Caliber Rounds becomes deterministic shared `DamageBonus`; the Drone-only probability stat and projectile bonus channel are removed without compatibility aliases.
- Scatter Arrow, Multi Shells, Multi Orbs, and Multi Drones reuse one serialized additional Attack Entity shape: prefab, positive additional count, and positive integer Basic Damage.
- Shared authoring does not create a shared runtime generator. Archer, Cannon, Magic, and Drone retain their existing confirmation, scheduling, active reconciliation, and cleanup authority.
- Primary entities use the Tower template and resolved Attack Damage. Additional entities use package Basic Damage plus resolved Basic Damage Bonus.
- Released Arrow/Shell additional-member damage is immutable. Active Magic Orb and Drone members retain their own base identity and accept later shared Damage Bonus refresh.

## 3. Installed First-Round Candidates

| Tower | Upgrade | Candidate |
|---|---|---|
| Archer | Eagle Sight | Range `+1`, unchanged |
| Archer | Quick Draw | Cycle `-0.15s` |
| Archer | Sharpened Arrows | Damage `+5` |
| Archer | Explosive Arrow | Effect Damage `10`, Radius `0.5` |
| Archer | Piercing Arrow | Max Hit Count `3`, unchanged |
| Archer | Scatter Arrow | `2` additional Arrows, Basic Damage `10`, angle `15` |
| Cannon | Extended Barrel | Range `+1`, unchanged |
| Cannon | Faster Reload | Cycle `-1s`, unchanged |
| Cannon | Reinforced Shells | Damage `+20` |
| Cannon | Bouncing Shell | Bounce Damage `30` |
| Cannon | Explosive Shell | Effect Damage `30`, Radius `1` |
| Cannon | Multi Shells | `1` additional Shell, Basic Damage `30` |
| Magic | Arcane Charge | Damage `+6` |
| Magic | Arcane Recovery | Cycle `-2s` |
| Magic | Faster Orbit | Rotation Speed `+20` |
| Magic | Arcane Detonation | Effect Damage `100`, Radius `0.75` |
| Magic | Arcane Field | Tick Damage `4`, Interval `0.5s`, Radius `1.5` |
| Magic | Multi Orbs | `1` additional Orb, Basic Damage `12` |
| Drone | Expanded Patrol | Range `+1`, unchanged |
| Drone | High-Caliber Rounds | deterministic Damage `+2` |
| Drone | Optimized Burst Module | Burst Cooldown `-0.5s` |
| Drone | Blast Rounds | Effect Damage `4`, Radius `0.5` |
| Drone | Multi Drones | `1` additional Drone, Basic Damage `6` |
| Drone | Final Dive | Effect Damage `120`, Radius `1` |

These values were the installed starting point for post-scale Play Mode calibration. Task003 owns the accepted revisions and final evidence. The final changes from this table are: Archer Explosive Arrow `Damage 5 / Radius 0.75`; Scatter Arrow additional Basic Damage `5 / angle 16`; Cannon Explosive Shell Damage `25`; Magic Arcane Recovery `-3s`; Arcane Detonation `Damage 200 / Radius 1`; Arcane Field Tick Damage `1`; and Drone Blast Rounds Radius `0.75`.

## 4. Scale Migration

The new Task002 reference fixture candidate is `40` Monsters, `120` Max Health, `0.25` Move Speed, and `2.5s` Spawn Interval. Base Tower Damage candidates are Archer `20`, Cannon `60`, Magic `30`, and Drone `10`. Damage-producing Effect and Buff assets are scaled consistently so unchanged mechanics preserve their relative meaning; the named candidates above then replace the affected values.

This migration created the Task002 `Base Combat v0.4` baseline. It does not retroactively change the historical evidence recorded for Task002 `Base Combat v0.3` or Task003 v0.1.

## 5. Validation And Acceptance

Source acceptance requires a successful C# build, serialized-field drift scans, deterministic semantic checks for all four additional-entity packages, and clean path-scoped diffs. Unity then imported and reserialized the changed assets without missing references.

The completed Play Mode handoff was:

1. Scatter, Multi Shells, Multi Orbs, and Multi Drones spawned the configured prefab/count and preserved configured Basic Damage identity.
2. High-Caliber produced deterministic Damage Bonus behavior and active Drone damage refresh remained valid.
3. The complete Task002 route matrix under HP `120` preserved the four Base Tower identities and became accepted `Base Combat v0.4`.
4. All Task003 Basic/Behaviour definitions received an L-route single screen; route-sensitive cases received targeted Straight/U diagnostics; reviewed pair and upper-stress combinations completed the regression.

Final accepted values, measurements, route exceptions, ceiling handling, and Keep/Revise decisions are recorded in `Task003_TowerGrowthAndNonElementalUpgrades.md`.
