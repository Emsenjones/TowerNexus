# Task006 - Fixed-Speed Monster Health Roster

Status: Planned

Depends on: Accepted Task002-Task005 combat baselines

Blocks: Task007-Task014

## 1. Goal

Establish the first CombatMathV2 Monster pressure roster using Maximum Health as
the only tactical-stat variable. MoveSpeed and standard formation spacing remain
fixed so Tower and Upgrade pressure can be understood before speed is added.

## 2. Fixed Movement Contract

```text
MoveSpeed = 0.25
Standard Spatial Gap = 0.625
SpawnInterval = 2.5s
```

Lane movement, route reprojection, Spawn center, Target center, and Resolve
contracts remain unchanged.

## 3. Candidate Profiles

| Role | Runtime Prefab | HP Candidate | Purpose |
|---|---|---:|---|
| Fodder | Prefab_Monster_Slime_Fodder | 60 | Low-pressure opening and early Draft conversion |
| Normal | Prefab_Monster_MonsterPlant_Normal | 120 | Reference ordinary body |
| Tough | Prefab_Monster_TutuleShell_Tough | 240 | Mid-pressure Upgrade check |
| Tank | Prefab_Monster_Orc_Tank | 480 | Late concentrated-build check |

Names identify roles; HP candidates must be reaccepted against CombatMathV2.
The Bat fast identity is explicitly deferred to Task014.

## 4. Tests

- L1 Tower screens against Fodder and Normal;
- accepted L2/L3 controls against Normal, Tough, and Tank;
- representative non-Elemental and Elemental Core builds;
- overkill, TTK, damage coverage, and leaked remaining HP;
- no-Tower spacing and route/lane regression only when needed to confirm the
  shared fixed-speed fixture.

## 5. Acceptance

- Roles are readable from HP alone.
- Fodder enables early low-pressure kills without being trivial to every Tower.
- Normal remains the comparison unit.
- Tough and Tank require increasingly developed output.
- No Stage-specific count, order, WaveDelay, or Player Health is accepted here.

