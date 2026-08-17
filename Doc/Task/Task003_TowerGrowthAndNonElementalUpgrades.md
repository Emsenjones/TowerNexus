# Task003 - Tower Growth And Non-Elemental Upgrades

Status: Completed on the accepted Task002 `Base Combat v0.4` fixture; all non-Elemental single-Upgrade L-route screens, targeted route diagnostics, and reviewed combination stress builds are accepted

Depends on: Completed Task003A implementation; completed Task003B combat-scale and additional-entity revision; accepted Task002 `Base Combat v0.4`; approved Tower Growth And Upgrade Identity balance contract

## 1. Goal

Calibrate Required Tower Level gates plus Basic and Behaviour Upgrades on top of the frozen Base Combat baseline and the Stage-authorized Tower Level v0.1 contract.

Each growth choice should create visible value without erasing the owning TowerFamily's base identity. Level Up buys future eligibility and model progression; applied Upgrades create combat-stat or attack-strategy growth.

Every Basic and Behaviour Upgrade uses the fixed L route as its shared Primary Acceptance Route. Straight and U are targeted diagnostics for Upgrades whose value is materially shaped by range, projectile direction, route exposure, completion position, or local Monster density. They are not repeated for every ordinary numerical Upgrade.

## 2. Source Documents

- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/12_ProjectileSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/Task/Task002_BaseTowerAndReferenceMonsterBaseline.md`
- `Doc/Task/Task003A_NonElementalUpgradeDefinitionRevision.md`
- `Doc/Task/Task003B_CombatValueScaleAndSecondaryAttackMemberDamageRevision.md`

## 3. Authority Boundary

`Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md` is the qualitative source of truth for growth philosophy, investment horizon, Core/Support intent, and the meaning of Basic, Behaviour, and Elemental growth.

System documents own stable runtime, authoring, eligibility, application, and lifecycle contracts. They do not own Task003 candidate values or Play Mode evidence.

This Task owns the fixed fixture, proposed and accepted numerical values, route results, combination evidence, reviewed exceptions, and Keep/Revise decisions. Unity assets remain the executable source for the accepted authored parameters.

Exact Stage pools, Draft budgets, concentrated-build availability, and Stage sampling feasibility remain Task007 responsibilities.

## 4. Scope

### In Scope

- Required Tower Level gates for current non-Elemental content
- Basic additive stat deltas
- Behaviour package parameters and visible attack-strategy changes
- One fixed L-route single-Upgrade test for every current Basic and Behaviour Upgrade
- Targeted Straight/U diagnostics for position- and route-sensitive Upgrades
- Selected Basic/Behaviour, Behaviour/Behaviour, and three-Upgrade stress builds
- Package composition, additional Attack Entity damage ownership, and ceiling review
- Regression against the accepted four Base Tower identities

### Out Of Scope

- Direct combat-stat growth from Tower Level
- Elemental Layer calibration
- Elemental Buff stacking, Overload, and Protection
- Final Monster roster and Stage MonsterWaveConfig tuning
- Player Progress Requirements
- Exact Stage Tower or Upgrade pools
- Guaranteed post-Level-Up Draft offers
- Exact uncapped magnitude after the fixed HP120 fixture reaches its `4800` total-damage ceiling

## 5. Calibration Sequence

1. Reuse the accepted Task002 `Base Combat v0.4` naked controls.
2. Keep route, placement, Monster, health, count, speed, and Spawn Interval fixed within each comparison.
3. Force exactly one Basic or Behaviour Upgrade and screen all `24` definitions on L.
4. Tune one coherent Upgrade-owned value group at a time and rerun only the changed candidate.
5. Run Straight/U only for selected range, direction, completion-position, or density-sensitive Upgrades.
6. Run selected L-route combinations that can multiply cadence, area effects, additional Attack Entities, completion effects, or chained projectiles.
7. Reuse the already accepted duplicate rejection, Required Tower Level eligibility, and package-capacity checks unless runtime eligibility code changes.
8. Record Effective Damage, realized gain, ceiling state, mechanism observation, and Keep/Revise decision.

One valid fixed-condition run is sufficient when integrity checks pass and the result produces a clear practical decision. Repeat a run when variance, a mislabeled route, a changed candidate, or a near-threshold mechanism requires confirmation. A supplemental higher-Health diagnostic may expose an uncapped stress-build magnitude, but it never replaces the fixed fixture.

## 6. Fixed Fixture And Controls

Task003 v0.2 uses `40` Monsters, `120` Max Health, `0.25` Move Speed, and `2.5s` Spawn Interval. Total Observed HP is `4800`.

All accepted recorder results satisfy:

- `ResolutionCountsMatch=True`
- `LeakCountMatchesPlayerHealthLoss=True`

The accepted Task002 v0.4 Effective Damage denominators are:

| Tower | Straight | L Primary | U Premium |
|---|---:|---:|---:|
| Archer | `2140` | `2280` | `2500` |
| Cannon | `1800` | `2040` | `2280` |
| Magic | `1170` | `2160` | `3240` |
| Drone | `1860` | `2025` average (`2020-2030`) | `2200` |

Realized Gain is `Upgraded Effective Damage / same-family same-route naked Effective Damage`.

L owns the candidate decision. Straight records low-exposure or alignment behavior. U records extreme route exposure. A high or low non-L result causes revision only when it exposes an unbounded mechanic, broken ownership, or realistic Stage risk.

## 7. Calibration Targets

| Test Build | Primary L-Route Interpretation |
|---|---|
| Single Basic | Prefer approximately `1.15x-1.30x`; reviewed range/placement and discrete-value exceptions are allowed |
| Single Behaviour | Prefer approximately `1.35x-1.65x`; reviewed route/direction exceptions are allowed |
| Selected Basic + Behaviour | Compare with the two accepted single gains and inspect ownership or repeated-trigger errors |
| Selected Behaviour + Behaviour | Confirm bounded composition and the intended trigger count; approximately `2.0x-2.3x` is a review guide rather than a universal band |
| Representative three-Upgrade stress build | Confirm the combined mechanism remains coherent; a `4800` result is recorded as a lower-bound Gain rather than an exact multiplier |

These empirical bands are decision aids, not direct stat-multiplier formulas. Range and path-shape Upgrades are allowed to trade fixed-point damage for deployment flexibility or exceptional geometry strength.

## 8. Accepted Authoring Values

| Tower | Upgrade | Required Level | Accepted Value |
|---|---|---:|---|
| Archer | Eagle Sight | `1` | Attack Range `+1` |
| Archer | Quick Draw | `1` | Attack Cycle `-0.15s` (`0.85s -> 0.70s`) |
| Archer | Sharpened Arrows | `1` | Damage Bonus `+5` |
| Archer | Explosive Arrow | `2` | Effect Damage `5`, Radius `0.75` |
| Archer | Piercing Arrow | `2` | Maximum Hit Count `3` |
| Archer | Scatter Arrow | `2` | `2` additional Arrows, Basic Damage `5`, angle offset `16` |
| Cannon | Extended Barrel | `1` | Attack Range `+1` |
| Cannon | Faster Reload | `1` | Attack Cycle `-1s` (`3.5s -> 2.5s`) |
| Cannon | Reinforced Shells | `1` | Damage Bonus `+20` |
| Cannon | Bouncing Shell | `2` | `1` bounce, Search Radius `2`, Bounce Damage `30` |
| Cannon | Explosive Shell | `2` | Effect Damage `25`, Radius `1` |
| Cannon | Twin Shells | `2` | `1` additional Shell, Basic Damage `30` |
| Magic | Arcane Charge | `1` | Damage Bonus `+6` |
| Magic | Arcane Recovery | `1` | Attack Cycle `-3s` (`20s -> 17s`) |
| Magic | Faster Orbit | `1` | Rotation Speed `+20` |
| Magic | Arcane Detonation | `2` | Effect Damage `200`, Radius `1` |
| Magic | Arcane Field | `2` | Tick Damage `1`, Interval `0.5s`, Radius `1.5` |
| Magic | Twin Orbs | `2` | `1` additional Orb, Basic Damage `12` |
| Drone | Expanded Patrol | `1` | Attack Range `+1` |
| Drone | High-Caliber Rounds | `1` | deterministic Damage Bonus `+2` |
| Drone | Optimized Burst Module | `1` | Burst Cooldown `-0.5s` |
| Drone | Blast Rounds | `2` | Effect Damage `4`, Radius `0.75` |
| Drone | Double Drones | `2` | `1` additional Drone, Basic Damage `6` |
| Drone | Final Dive | `2` | Effect Damage `120`, Radius `1`, Hit Threshold `1` |

## 9. Accepted Play Mode Evidence

### 9.1 Single-Upgrade L Screen

| Tower | Upgrade | Effective Damage | Gain | Decision |
|---|---|---:|---:|---|
| Archer | Eagle Sight | `2480` | `1.088x` | Keep as deployment/range exception |
| Archer | Quick Draw | `2580` | `1.132x` | Keep; readable cadence gain |
| Archer | Sharpened Arrows | `2840` | `1.246x` | Accepted |
| Archer | Explosive Arrow | `3735` | `1.638x` | Accepted at upper Behaviour edge |
| Archer | Piercing Arrow | `2320` | `1.018x` | Keep as direction-sensitive Behaviour |
| Archer | Scatter Arrow | `3300` | `1.447x` | Accepted |
| Cannon | Extended Barrel | `2100` | `1.029x` | Keep as deployment/range exception |
| Cannon | Faster Reload | `2700` | `1.324x` | Accepted as high-value Basic |
| Cannon | Reinforced Shells | `2320` | `1.137x` | Keep as discrete-damage exception |
| Cannon | Bouncing Shell | `2970` | `1.456x` | Accepted |
| Cannon | Explosive Shell | `3290` | `1.613x` | Accepted |
| Cannon | Twin Shells | `2790` | `1.368x` | Accepted |
| Magic | Arcane Charge | `2628` | `1.217x` | Accepted |
| Magic | Arcane Recovery | `2490` | `1.153x` | Accepted |
| Magic | Faster Orbit | `2730` | `1.264x` | Accepted |
| Magic | Arcane Detonation | `2820` | `1.306x` | Keep as route/completion-position exception |
| Magic | Arcane Field | `3511` | `1.625x` | Accepted at upper Behaviour edge |
| Magic | Twin Orbs | `3318` | `1.536x` | Accepted |
| Drone | Expanded Patrol | `2100` | `1.037x` | Keep as deployment/range exception |
| Drone | High-Caliber Rounds | `2436` | `1.203x` | Accepted; deterministic damage confirmed |
| Drone | Optimized Burst Module | `2480` | `1.225x` | Accepted |
| Drone | Blast Rounds | `3276` | `1.618x` | Accepted after Radius revision |
| Drone | Double Drones | `3026` | `1.494x` | Accepted; additional Drone identity confirmed |
| Drone | Final Dive | `3120` | `1.541x` | Accepted as concentrated finisher |

Magic's zero-kill or Defeat outcomes in some single tests do not invalidate the Effective Damage comparison. Magic intentionally distributes contact damage across many Monsters; the recorder integrity checks remained valid.

### 9.2 Targeted Route Diagnostics

| Upgrade | Straight Result / Gain | L Result / Gain | U Result / Gain | Decision |
|---|---|---|---|---|
| Archer Eagle Sight | `2300 / 1.075x` | `2480 / 1.088x` | `2680 / 1.072x` | Stable deployment-flexibility Basic; keep `+1` Range |
| Archer Piercing Arrow | `3140 / 1.467x` | `2320 / 1.018x` | `4000-4060 / 1.600x-1.624x` | Intended alignment-sensitive Behaviour confirmed |
| Cannon Extended Barrel | `1860 / 1.033x` | `2100 / 1.029x` | `2340 / 1.026x` | Stable deployment-flexibility Basic; keep `+1` Range |
| Magic Arcane Detonation | `1680 / 1.436x` | `2820 / 1.306x` | `3960 / 1.222x` | Absolute contribution grows with exposure while relative gain follows Magic's route identity |
| Drone Expanded Patrol | `1890 / 1.016x` | `2100 / 1.037x` | `2170 / 0.986x` | Fixed-point damage is neutral; keep as patrol/deployment flexibility |

The second U Piercing run (`4060`) confirms that its high aligned-route value was not a one-run anomaly. No range Upgrade is increased merely to force fixed-position DPS into the ordinary Basic band.

### 9.3 Combination Regression

| Tower | Build | Effective Damage | Gain | Ceiling | Decision |
|---|---|---:|---:|---|---|
| Archer | Quick Draw + Explosive Arrow | `4270` | `1.873x` | No | Expected cadence/AoE composition |
| Archer | Quick Draw + Scatter Arrow | `3830` | `1.680x` | No | Expected cadence/additional-member composition |
| Archer | Sharpened Arrows + Scatter Arrow | `4590` | `2.013x` | No | Side members retain Basic Damage and add shared Damage Bonus |
| Archer | Piercing Arrow + Explosive Arrow | `3910` | `1.715x` | No | Each new unique pierced hit is eligible for its explosion |
| Cannon | Faster Reload + Explosive Shell | `4360` | `2.137x` | No | Expected cadence/AoE composition |
| Cannon | Faster Reload + Twin Shells | `3570` | `1.750x` | No | Expected cadence/additional-member composition |
| Cannon | Bouncing Shell + Explosive Shell | `4725` | `2.316x` | No | Bounded high-value pair; each eligible Position Impact explodes |
| Magic | Faster Orbit + Arcane Field | `4140` | `1.917x` | No | Expected contact/field composition |
| Magic | Arcane Recovery + Twin Orbs | `3816` | `1.767x` | No | Expected cadence/member composition |
| Magic | Arcane Charge + Twin Orbs | `4044` | `1.872x` | No | Additional Orb keeps Basic Damage and receives shared Damage Bonus |
| Magic | Twin Orbs + Arcane Detonation | `4158` | `1.925x` | No | Each active Orb owns its reviewed completion result |
| Drone | Optimized Burst Module + Blast Rounds | `3704` | `1.829x` | No | Final Radius `0.75`; no repeated-trigger anomaly |
| Drone | Double Drones + Final Dive | `4464` | `2.204x` | No | Each Drone owns one bounded battery-end sequence |
| Archer | Quick Draw + Explosive Arrow + Scatter Arrow | `4800` | `>=2.105x` | Yes | Accepted representative upper stress build |
| Cannon | Faster Reload + Explosive Shell + Twin Shells | `4800` | `>=2.353x` | Yes | Accepted representative upper stress build |
| Magic | Faster Orbit + Arcane Field + Twin Orbs | `4612` | `2.135x` | No | Accepted upper stress build with measurable headroom |
| Drone | Optimized Burst Module + Blast Rounds + Double Drones | `4800` | `>=2.370x` | Yes | Accepted representative upper stress build |

The three `4800` results prove fixture saturation, not an exact final multiplier. Their fast clear, low Peak Alive, and shortened battle duration are expected from a selected Core build containing cadence plus multiple Behaviour investments. Single and pair tests retain enough headroom to validate the owning parameters. Exact Stage availability and acquisition pace remain Stage/Draft balance responsibilities.

### 9.4 Final Candidate Revisions

| Upgrade | First Task003B Candidate | Accepted Revision | Reason |
|---|---|---|---|
| Archer Explosive Arrow | Damage `10`, Radius `0.5` | Damage `5`, Radius `0.75` | Make nearby splash visible without preserving excessive per-target payload |
| Archer Scatter Arrow | Side Basic Damage `10`, angle `15` | Side Basic Damage `5`, angle `16` | Record the final tested spread while removing the original ceiling result |
| Cannon Explosive Shell | Damage `30`, Radius `1` | Damage `25`, Radius `1` | Bring the L gain inside the Behaviour band |
| Magic Arcane Recovery | Cycle `-2s` | Cycle `-3s` | Reach the Basic lower band |
| Magic Arcane Detonation | Damage `100`, Radius `0.75` | Damage `200`, Radius `1` | Improve completion-position hit opportunity; retain route-sensitive identity |
| Magic Arcane Field | Tick Damage `4` | Tick Damage `1` | Restore measurable headroom below the fixture ceiling |
| Drone Blast Rounds | Damage `4`, Radius `0.5` | Damage `4`, Radius `0.75` | Make area coverage reliable while remaining within the Behaviour band |

## 10. Functional Acceptance

- Scatter Arrow side damage, Twin Shell additional damage, Twin Orbs additional damage, and Double Drones additional damage use their package-authored Basic Damage rather than copying primary Attack Damage.
- Shared Damage Bonus composes with eligible primary and additional members according to the System contracts.
- High-Caliber Rounds is deterministic.
- Piercing plus Explosive Arrow, Bouncing plus Explosive Shell, Twin Orbs plus Arcane Detonation, Blast Rounds cadence, and Final Dive multi-Drone interactions remain bounded and preserve their reviewed trigger ownership.
- Duplicate application rejection, Level 1 Basic eligibility, Level 2 Behaviour eligibility, package capacity, and incompatible-combination rejection were previously exercised without error and remained reusable because eligibility code did not change during final value tuning.
- Task002 v0.4 naked Tower identities remain the accepted regression controls; no final Upgrade-only parameter revision changes naked Tower or Monster values.

## 11. Acceptance Criteria

- All `24` current Basic and Behaviour definitions have an accepted fixed L result.
- Every final authored value uses the fixed `40 / HP120 / Speed0.25 / Spawn2.5s` fixture.
- Targeted route-sensitive Upgrades have reviewed Straight/U evidence.
- Selected high-risk combinations confirm additional-member ownership, trigger count, and bounded completion behavior.
- Ceiling results are recorded as Gain lower bounds and are not used to claim an exact multiplier.
- Basic Upgrades create coherent numerical or deployment specialization.
- Behaviour Upgrades visibly change attack strategy.
- No Upgrade is required merely to repair an unusable Base Tower.
- Task002 Base Tower identities survive the complete post-scale regression.
- Elemental content and exact Stage/Draft feasibility remain explicitly deferred to their owning tasks.

## 12. Validation

- Task002 v0.4 Straight/L/U naked control suite
- Task003 L-route single-Upgrade screen for all four TowerFamilies
- Targeted Straight/U route diagnostics
- L-route pair and representative upper-stress combination regression
- Recorder integrity checks on every accepted run
- Required-Level, eligibility, duplicate, and package-capacity smoke tests
- Static source, asset, stale-schema, and diff validation from Task003B

## 13. Review Note

Task003 v0.2 accepts the post-Task003B non-Elemental calibration without changing the frozen Task002 v0.4 Base Towers or reference Monster. `01_TowerGrowthAndUpgradeIdentity.md` remains qualitative; this Task retains concrete parameters, fixtures, measurements, and decisions. System documents retain only stable cross-engine behavior and ownership contracts.

Future Stage tuning must not reinterpret a route-sensitive exception as a universal numerical weakness. Any later change to Base Tower or reference Monster values requires a named Base Combat revision and a complete affected Task002 regression. Any later change to an accepted Upgrade parameter requires an affected Task003 single run plus the smallest relevant route or combination regression.
