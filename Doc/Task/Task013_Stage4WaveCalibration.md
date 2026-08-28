# Task013 — Stage 4 Wave Calibration

> Status: **Completed**
>
> Accepted: **2026-08-29**
>
> Depends on: Task012 Stage 3 Wave Calibration
>
> Unblocks: Task014 Stage 5 Wave Calibration

---

## 1. Goal

Freeze Stage 4 as an executable Stage contract: map pressure, progression cadence, Draft pools, fixed-speed Wave composition, Player Health, Reference Build, coherent alternatives, and negative Build boundaries must work together.

The Reference Build is a positive control, not the only valid answer. A legal, internally coherent Build with sensible resource use and placement is accepted when it clears within the Stage-specific leak margin. Task017 separately owns natural Draft-offer accessibility.

---

## 2. Accepted Stage Contract

| Field | Accepted value |
|---|---|
| Stage asset | `StageDefini_Lv4` |
| Map | `Prefab_Map_Stage4` map-gap revision |
| Initial Player Health | `6` |
| Total Draft resolutions | `8` |
| Post-initial Draft progress nodes | `3 / 6 / 10 / 14 / 18 / 26 / 34` |
| Progress requirements | `3 / 3 / 4 / 4 / 4 / 8 / 8` |
| Tower pool | Archer / Cannon / Magic / Drone |
| Upgrade pool | All Basic and Behaviour upgrades for the four Tower families; no Elemental upgrades |
| Monster count | `56` |
| Monster move speed | `0.25` for every Wave |
| Spawn interval | `2.5` for every Wave |
| Wave delays | `4 / 6 / 5 / 4 / 4 / 4 / 4 / 4 / 4 / 4` |
| Final Draft boundary | Progress node `34`, leaving `22` resolutions for Build verification |
| Reference acceptance | Victory with `0–4` leaks |
| Negative boundary | A strategically incomplete Build may fail even when placement is efficient |

The Stage-specific `0–4` leak margin reflects Stage 4's larger 56-Monster pressure envelope. It does not replace the general Stage-design rule that coherent alternatives should not be weakened merely because they differ from the Reference Build.

---

## 3. Map-Gap Contract

The accepted map revision creates an early shortcut that cannot be ignored. The player must use an early Support Tower to close or control that gap, then expand coverage across the longer route.

The Reference placement uses a Cannon footprint at `(8,10) / (8,9) / (7,10)`. This changes the active route from `15` to `23` cells and gives the later Drone pursuit package room to express its cross-zone strength.

Key logical-grid changes relative to the earlier Stage 4 candidate:

| Cell | Earlier candidate | Accepted map-gap revision |
|---|---|---|
| `(6,7)` | Walkable | Blocked |
| `(6,8)` | Blocked | Walkable |
| `(6,9)` | Blocked | Walkable |
| `(7,7)` | Walkable | Blocked |
| `(7,10)` | Blocked | Walkable shortcut |

Legal route-forced relocation may occur during placement and remains valid calibration evidence when Recorder integrity checks pass. It must not be confused with an illegal or strategically careless placement.

---

## 4. Accepted Wave Definition

| Wave | Monster | HP | Count | Delay | Cumulative resolutions |
|---:|---|---:|---:|---:|---:|
| 1 | Slime Lv1 | 60 | 3 | 4 | 3 |
| 2 | Slime Lv1 | 60 | 3 | 6 | 6 |
| 3 | Plant Lv2 | 180 | 4 | 5 | 10 |
| 4 | Plant Lv2 | 180 | 4 | 4 | 14 |
| 5 | Plant Lv2 | 180 | 4 | 4 | 18 |
| 6 | Plant Lv2 | 180 | 4 | 4 | 22 |
| 7 | Plant Lv2 | 180 | 4 | 4 | 26 |
| 8 | Plant Lv2 | 180 | 8 | 4 | 34 |
| 9 | TurtleShell Lv4 | 400 | 10 | 4 | 44 |
| 10 | Orc Lv5 | 520 | 12 | 4 | 56 |

Total authored Monster HP is `15,640`.

Orc is the accepted Stage 4 elite-capstone model at HP `520`. HP `600` produced excessive pressure; HP `480` did not create enough separation from TurtleShell HP `400`.

---

## 5. Fixed-Draft Build Fixtures

All fixtures share the accepted Stage, Wave, Player Health, and map-gap configuration. Placement is optimized for the Tower characteristics; poor placement is not used as a substitute for a meaningful Build boundary.

### 5.1 Reference — pursuit and formation control

Draft order:

1. Drone
2. Drone L2
3. Cannon support at the early shortcut
4. Archer
5. Magic
6. Archer
7. Expanded Patrol
8. Double Drones

Final Build: Drone L2 with Expanded Patrol + Double Drones; Archer L1 ×2; Cannon L1 ×1; Magic L1 ×1.

### 5.2 Coherent Alternative A — optimized burst

Use the same first six Drafts and placement responsibilities as the Reference, then select Optimized Burst + Blast Rounds for the Drone core.

### 5.3 Coherent Alternative B — high-caliber dive

Use the same first six Drafts and placement responsibilities as the Reference, then select High-Caliber + Final Dive for the Drone core.

### 5.4 Negative boundary A — horizontal expansion

Draft Drone / Cannon / Archer / Magic twice, producing eight L1 Towers with no levels or upgrades. Even with efficient placement, the undeveloped Build must not automatically pass the elite capstone.

### 5.5 Negative boundary B — no-pursuit Cannon core

Draft order:

1. Cannon core
2. Cannon support at the early shortcut
3. Cannon core L2
4. Archer
5. Magic
6. Archer
7. Faster Reload
8. Explosive Shell

This fixture closes the map gap on time and uses reasonable placement. Its failure therefore measures the missing pursuit/cross-zone package rather than an intentionally bad opening.

---

## 6. Final Acceptance Evidence

All final records use Recorder schema 23, resolve all `56` Monsters, complete all `8` Drafts through node `34`, and pass integrity checks.

| Record | Fixture | Result | Leaks | Final Health | Effective Damage |
|---|---|---|---:|---:|---:|
| `Task013_PhaseG_001` | Reference | Victory | 4 | 2 | 14,829 |
| `Task013_PhaseG_003` | Coherent Alternative A | Victory | 3 | 3 | 15,430 |
| `Task013_PhaseG_004` | Coherent Alternative B | Victory | 4 | 2 | 14,901 |
| `Task013_PhaseG_002` | Horizontal expansion | Defeat | 6 | 0 | 14,785 |
| `Task013_PhaseG_005` | No-pursuit Cannon core | Defeat | 6 | 0 | 14,252 |

The Cannon-core negative control survives Waves 1–8 but fails under the late verification pressure. Its core damages `46` Monsters across `5` route cells, compared with the Reference Drone core damaging `52` Monsters across `23` route cells. This is the intended Stage 4 distinction: deployment quality remains valuable, but the revised map rewards pursuit and cross-zone reach rather than allowing Cannon to perfectly replace Drone.

---

## 7. Acceptance Decision

Stage 4 calibration is accepted because:

- the Reference Build clears within the `0–4` leak margin;
- both coherent Drone alternatives clear within the same margin;
- efficient horizontal L1 expansion fails at the elite capstone;
- a sensibly deployed Cannon fixed-core Build closes the early gap but still fails late;
- all final Recorder integrity and progression checks pass;
- the Stage produces a meaningful formation decision without treating Reference as the only legal solution.

Task014 may use this accepted Stage 4 contract as its progression baseline. Task017 remains responsible for testing whether natural Draft offers expose viable Builds often enough.

---

## 8. Calibration History and Supersession

The initial V1 candidate used Progress requirements `3 / 3 / 4 / 4 / 4 / 4 / 4`, ended Draft progression at node `26`, and relied on TurtleShell-only late Waves. It established the first Stage 4 formation fixture but is not the accepted contract.

Subsequent calibration moved the final Draft node to `34`, replaced Wave 6 TurtleShell with Plant to restore the pressure ramp, introduced Orc as the elite capstone, rejected Orc HP `600`, accepted HP `520`, and finally revised the map to create the early shortcut/gap decision. Phase G records `001–005` supersede earlier V1–V8 balance conclusions wherever their map or elite-capstone conditions differ.
