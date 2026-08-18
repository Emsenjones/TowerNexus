# Task006 - Monster Movement Identity And Wave Composition

Status: Completed; four movement Profiles, homogeneous campaign Waves, the standard spatial-gap rule, Stage-local Wave Delay, and final normalized-spacing combat regressions were accepted on 2026-08-18

Depends on: Task005 Monster Roster Baseline

Blocks: Task007 Global Progression And Stage Skeleton

## 1. Goal

Establish readable Monster identities from Maximum Health and Move Speed, bind the first accepted identities to runtime Prefabs, and define the campaign Wave-composition and timing contracts consumed by later Stage authoring.

Task006 owns Stage-independent Monster movement identity and standard formation density. It does not decide final Stage order, Count, Wave Delay, duration, or difficulty.

## 2. Source Documents

- `Doc/Task/Task005_MonsterRosterBaseline.md`
- `Doc/Task/Task004_ElementalEffectAndBuffBaseline.md`
- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/07_MonsterSystem.md`

## 3. Accepted Profiles

| Profile | Runtime Prefab | Maximum Health | Move Speed | Tactical Meaning |
|---|---|---:|---:|---|
| Normal | `Prefab_Monster_Slime1` | `120` | `0.25` | Stable ordinary Wave body and movement reference |
| Rush | `Prefab_Monster_Bat1` | `120` | `0.35` | Short handling window that reduces realized Tower efficiency, especially for slow-cadence or slow-projectile attacks |
| Tough | `Prefab_Monster_TurtuleShell1` | `240` | `0.25` | Normal-speed durability pressure |
| Tank | `Prefab_Monster_Orc1` | `480` | `0.20` | High durability with a slower, longer exposure window |

These four mappings are the first campaign roster. Task006 does not require every future Health and Move Speed combination to receive a separate Prefab. Additional identities are added only when Stage calibration demonstrates a distinct tactical need.

## 4. Campaign Wave Authoring Contracts

### 4.1 Homogeneous Waves

One Wave contains exactly one Monster runtime template, one Count, and one Spawn Interval. `MonsterWaveConfig` enforces homogeneous composition structurally rather than relying on a campaign-only content convention.

Additional timing groups are represented as consecutive Waves with their own Delay, template, Count, and Interval. Mixed-template Waves and nested spawn groups are not part of the approved schema.

### 4.2 Standard Spatial Gap

Standard homogeneous Waves preserve the spatial gap created by the accepted Reference fixture:

```text
Reference Spatial Gap
    = Move Speed 0.25 * Spawn Interval 2.5s
    = 0.625 world units

Profile Spawn Interval
    = 0.625 / Profile Move Speed
```

| Profile | Move Speed | Accepted Derived Spawn Interval | Resulting Gap |
|---|---:|---:|---:|
| Normal | `0.25` | `2.5s` | `0.625` |
| Rush | `0.35` | `1.7857s` | approximately `0.625` |
| Tough | `0.25` | `2.5s` | `0.625` |
| Tank | `0.20` | `3.125s` | `0.625` |

The interval is written with sufficient precision into each Wave; runtime does not derive or mutate it from Prefab Move Speed.

This contract isolates formation density from movement identity. Rush pressure should primarily come from shorter route exposure and harder projectile interception, not from an incidental wider formation that further reduces area, piercing, or multi-target coverage. A deliberately dense or loose Wave requires a separately approved identity rather than silent Stage-local interval drift.

### 4.3 Stage-Local Wave Delay

Wave execution is schedule-driven rather than resolution-gated. After the current Wave's last Monster spawns, execution advances to the next Wave and waits that next Wave's authored Wave Delay. It does not wait for earlier Monsters to die or reach the Target.

Consequently:

- clearing a Wave faster does not make the next Wave spawn early;
- leaked or surviving Monsters do not block the next Wave;
- cross-Wave overlap may occur when the authored schedule and active combat state produce it;
- no generic requirement says that every previous Monster must resolve before the next Wave begins.

Each Stage manually selects Wave Delay from its Map size, route length, Monster Count, outgoing Wave spawn duration, Reference Build, desired rhythm, and acceptable overlap. Wave Delay may minimize unintended chase or deliberately accumulate pressure, but Task006 does not freeze one global value.

For one Wave:

```text
Next Wave First Spawn Time
    = Current Wave First Spawn Time
    + (Current Count - 1) * Current Spawn Interval
    + Next Wave Wave Delay
```

## 5. Accepted Evidence

### 5.1 Traversal Identity

Five one-Monster Straight-route runs confirmed that leaked lifetime changes inversely with authored Move Speed and that `0.20 / 0.25 / 0.35` are visually and measurably distinct.

| Move Speed | Average Leaked Lifetime |
|---:|---:|
| `0.15` | `57.04s` |
| `0.20` | `42.79s` |
| `0.25` | `34.24s` |
| `0.30` | `28.55s` |
| `0.35` | `24.46s` |

### 5.2 Three-Monster Archer And Cannon Screening

The accepted Profile-Prefab mappings were screened on Straight Route, Position 1, Count `3`, and the historical fixed `2.5s` interval.

| Tower | Profile | Effective Damage | Damage Coverage | Killed / Leaked | Successful Damage Applications |
|---|---|---:|---:|---:|---:|
| Archer Base | Normal | `360` | `100%` | `3 / 0` | `18` |
| Archer Base | Rush | `280` | `77.78%` | `1 / 2` | `14` |
| Archer Base | Tough | `360` | `50%` | `1 / 2` | `18` |
| Archer Base | Tank | `420` | `29.17%` | `0 / 3` | `21` |
| Cannon Base | Normal | `360` | `100%` | `3 / 0` | `6` |
| Cannon Base | Rush | `180` | `50%` | `0 / 3` | `3` |
| Cannon Base | Tough | `300` | `41.67%` | `0 / 3` | `5` |
| Cannon Base | Tank | `420` | `29.17%` | `0 / 3` | `7` |

Normal Archer and Rush Cannon repeats reproduced their headline outcomes. Rush established the intended hard Cannon matchup under the historical fixture, while Normal, Tough, and Tank retained explainable survival ordering.

Because this screening used one fixed temporal interval, Rush began with a wider formation and Tank with a denser formation. These runs accept the four identity candidates but do not by themselves quantify final Radius behavior under the new normalized-spacing contract.

### 5.3 Cannon And Cold Regression

Matched Level 3 Cannon controls showed that Frostbound Shells was neutral against Rush and positive against Tank rather than intrinsically negative:

| Profile | Configuration | Effective Damage | Damage Coverage | Successful Damage Applications |
|---|---|---:|---:|---:|
| Rush | L3 Cannon, no Upgrade | `60` | `16.67%` | `1` |
| Rush | L3 Cannon, Frostbound Shells only | `60` | `16.67%` | `1` |
| Tank | L3 Cannon, no Upgrade | `420` | `29.17%` | `7` |
| Tank | L3 Cannon, Frostbound Shells only | `480` | `33.33%` | `8` |

Rush remains an intentional Cannon weakness. Stage-local Rush Count and timing are calibrated against the legal Reference Build before any global speed revision is considered.

All named schema-v8 runs completed with all six Recorder integrity flags true. Normal Archer and Rush Cannon each supplied an exact repeat for the primary comparison.

### 5.4 Final Normalized-Spacing Regression

The final integration fixture replaced the historical fixed temporal interval with the accepted Profile-derived intervals. Straight Route, Position 1, Count `3`, Health `120`, and all other comparison conditions remained fixed.

Matched Level 2 Archer controls and Explosive Arrow-only runs produced:

| Profile | Configuration | Observed Spawn Interval | Effective Damage | Damage Coverage | Killed / Leaked | Projectile Releases | Successful Damage Applications | Average Resolution Lifetime |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Normal | L2 Archer, no Upgrade | `2.5064s` | `360` | `100%` | `3 / 0` | `18` | `18` | `20.03s` |
| Normal | L2 Archer, Explosive Arrow only | `2.5033s` | `360` | `100%` | `3 / 0` | `13` | `33` | `16.90s` |
| Rush | L2 Archer, no Upgrade | `1.7913s` | `260` | `72.22%` | `1 / 2` | `13` | `13` | `20.85s` |
| Rush | L2 Archer, Explosive Arrow only | `1.7865s` | `360` | `100%` | `3 / 0` | `13` | `33` | `14.83s` |

Both Explosive Arrow runs converted `13` Projectile releases into `33` successful Damage Applications and cleared all three Monsters. Combined with the Play Mode formation observation, this confirms that the faster Rush identity remains inside the accepted `0.75` explosion Radius when its Spawn Interval preserves the Reference spatial gap. Normal reached the same total-Health ceiling with fewer releases and a shorter lifetime, while Rush improved from `260` to `360` Effective Damage and from `1 / 2` to `3 / 0` Killed / Leaked.

The first final-spacing Rush Cannon run and its repeat produced the same headline result:

| Run | Observed Spawn Interval | Effective Damage | Damage Coverage | Killed / Leaked | Arc Releases | Intended / Fallback / Position-only | Arc Target Resolution |
|---|---:|---:|---:|---:|---:|---:|---:|
| Final Spacing | `1.7909s` | `240` | `66.67%` | `1 / 2` | `4` | `2 / 2 / 0` | `100%` |
| Final Spacing Repeat | `1.7921s` | `240` | `66.67%` | `1 / 2` | `4` | `2 / 2 / 0` | `100%` |

The final-spacing result supersedes the historical fixed-`2.5s` Rush Cannon result for campaign formation interpretation. Rush still reduces Cannon output relative to Normal because the faster Wave supplies fewer release opportunities, but equalized formation spacing lets following Bats resolve otherwise missed landings as fallback hits. The accepted Rush identity therefore reduces realized Attack Efficiency through its shorter handling window without also receiving an incidental loose-formation advantage.

Every final normalized-spacing report used schema v8, matched its Run identity and authored fixture, completed all expected Monster resolutions, and passed all six Recorder integrity flags.

## 6. Completion Decision

The final normalized-spacing regressions preserve all four accepted Profile identities and validate the campaign spacing contract. No additional no-Tower spacing run, generic Tank-to-Rush catch-up experiment, full Profile matrix, or Cold rerun is required for Task006. Actual cross-Wave overlap remains a Stage-local observation for Task008-Task013.

Task006 completed on 2026-08-18.

## 7. In Scope

- Accepted per-Profile Move Speed
- First Profile-to-Prefab mapping
- Homogeneous campaign Wave convention
- Reference spatial gap and Profile-derived Spawn Interval
- Stage-local, non-resolution-gated Wave Delay contract
- Targeted movement, projectile, Slow, Radius, and multi-target regression

## 8. Out Of Scope

- New Monster armor, resistance, collision, blocking, or mechanics
- Automatic runtime SpawnInterval derivation
- Resolution-gated Wave progression
- Final Stage Monster order, Count, Wave Delay, duration, or difficulty
- Adding every available visual Prefab to the roster
- Unapproved dense, loose, or mixed-template campaign Wave identities

## 9. Ownership

| Owner | Responsibility |
|---|---|
| Task005 | Historical HP120/HP240/HP480 health-only baseline |
| Task006 | Four movement Profiles, homogeneous-Wave convention, and standard spatial-gap contract |
| Monster runtime template | Accepted per-type Maximum Health and Move Speed |
| MonsterWaveConfig | Explicit Wave template, Count, derived Spawn Interval, and Stage-local Wave Delay |
| Task007 | Six structural Wave skeletons consuming accepted identities and the derived interval rule |
| Task008-Task013 | Final Stage-local Monster order, Count, Wave Delay, overlap, duration, and difficulty |

## 10. Acceptance Criteria

- Normal, Rush, Tough, and Tank have distinct and explainable tactical identities.
- The accepted Prefab mappings carry the intended Health and Move Speed values.
- Every campaign Wave contains one Monster runtime template.
- Standard Profile intervals preserve approximately the `0.625` Reference spatial gap.
- Faster movement does not silently gain additional initial formation spacing.
- Wave execution remains schedule-driven and does not wait for Monster resolution.
- Each Stage manually calibrates Wave Delay from its Map and Reference Build.
- Cannon and Cannon plus Cold behavior remains explainable under accepted identities.
- The normalized-spacing Radius or multi-target regression does not invalidate the accepted identities.
- All formal Recorder runs pass their completeness and integrity checks.

## 11. Handoff

Task007 begins from the four accepted Profiles, homogeneous-Wave convention, and Profile-derived interval baselines while authoring six rough Wave skeletons. Task008-Task013 then calibrate Stage-local Monster order, Count, Wave Delay, overlap, duration, and difficulty without redefining global movement identities or silently changing standard formation density.
