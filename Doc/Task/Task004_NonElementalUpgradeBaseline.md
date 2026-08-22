# Task004 - Non-Elemental Upgrade Baseline

Status: Completed; accepted on 2026-08-22 with Phase A-D Play Mode evidence
and a reviewed Phase E manual-test waiver

Depends on: Accepted Task003 Tower Level Damage Curve

Blocks: Task005-Task016

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
- duplicate, Required Level, package capacity, rejected-item atomicity, and live
  refresh contract review, with any waived manual runtime cases named at closeout.

## 5. Implementation Phases

### Phase A - Level 1 Basic Single-Upgrade Screen

- Record one fresh L-route no-Upgrade control for each TowerFamily.
- Test all twelve Basic Upgrades at Level 1, one Upgrade per run.
- Compare only measurement-Wave Effective Damage against the matching fresh
  control; the HP60 setup Wave exists only to open the Upgrade Draft.
- Review Damage Bonus, cadence, range, Magic rotation, and Drone burst-cooldown
  value without changing the accepted Level 1 Tower baseline.

### Phase B - Level 2 Behaviour Single-Upgrade Screen

- Record one fresh L-route Level 2 no-Upgrade control for each TowerFamily.
- Test all twelve Behaviour packages at Level 2, one package per run.
- Verify every additional-member DamageScale and TowerScaled Effect source,
  along with package-specific trigger ownership and visible strategy change.

### Phase C - Targeted Route Diagnostics

- Use Straight and U only for Upgrades whose value materially depends on
  range, alignment, completion position, route exposure, or local density.
- Keep Tower level, placement role, Monster fixture, and Upgrade state fixed
  within each route comparison.

### Phase D - Combination And Upper-Stress Matrix

- Run selected Basic/Behaviour and Behaviour/Behaviour pairs.
- Run one representative three-Upgrade Core build per TowerFamily.
- Confirm bounded composition, additional-member ownership, and no unexplained
  repeated trigger or multiplication.

### Phase E - Eligibility, Refresh, And Final Regression

- Review duplicate rejection, Required Tower Level, package capacity,
  composition, rejected-item atomicity, and live stat/package refresh against
  the implemented System contracts.
- Run the smallest final family regression needed after accepted value edits.
- Require Recorder combat, deployment, Wave attribution, and damage-diagnostic
  integrity before closing the Task.

## 6. Phase A Configuration

### 6.1 Shared Fixture

| Setting | Value |
|---|---|
| Map | `Prefab_Map_Default_LRoute` |
| Player Max Health | `100` |
| Progress Requirements | `[2, 99]` |
| Draft Mode | Fixed; exactly `3` Steps |
| Wave 1 - setup | Delay `5s`; `2` x `Prefab_MonsterCandidate_Dragon`; HP `60`; Move Speed `0.25`; Spawn Interval `2.5s` |
| Wave 2 - measurement | Family delay: Archer `10s`, Cannon `20s`, Magic `60s`, Drone `60s`; `40` x `Prefab_MonsterCandidate_EvilMage`; HP `480`; Move Speed `0.25`; Spawn Interval `2.5s` |
| Tower state at measurement | Exactly one Level 1 Tower at its family P1, with either no Upgrade or exactly one named Basic Upgrade |
| Result authority | Wave 2 `effectiveDamage`; never the two-Wave run total |

The family-specific second-Wave delays are the tested Phase A controls. Each
gives that family enough time to complete the Upgrade Draft before measurement
exposure without adding unnecessary idle time. Step 3 is the unreachable
terminal-sentinel configuration required by the Fixed Draft validator.

### 6.2 Fixed Draft Sequence

For an upgraded run:

1. Step 1: the tested TowerDefinition; deploy it at the matching P1.
2. Step 2: exactly one tested Basic Upgrade; apply it to the deployed Tower.
3. Step 3: the same TowerDefinition as a sentinel; it should not open because
   the second Progress Requirement is `99`.

For a no-Upgrade control, Step 2 is the same TowerDefinition. Select it to
complete the Draft but leave the resulting Pending Tower undeployed. The final
combat state must still contain exactly one Level 1 Tower and no Upgrade.

### 6.3 Family Placements

Use the accepted Task002 L-route placement footprints:

| TowerFamily | P1 occupied Grids |
|---|---|
| Archer | `(2,3)`, `(2,4)` |
| Cannon | `(2,4)`, `(3,3)`, `(3,4)` |
| Magic | `(2,3)` |
| Drone | `(2,3)`, `(2,4)`, `(3,3)`, `(3,4)` |

### 6.4 Required Runs

Each family uses one control as the denominator for its three Basic runs:

| TowerFamily | Control | Basic Upgrades |
|---|---|---|
| Archer | `NoUpgrade` | `EagleSight` Range `+1`; `QuickDraw` Cycle `-0.15s`; `SharpenedArrows` Damage Bonus `+5` |
| Cannon | `NoUpgrade` | `ExtendedBarrel` Range `+1`; `FasterReload` Cycle `-1s`; `ReinforcedShells` Damage Bonus `+20` |
| Magic | `NoUpgrade` | `ArcaneCharge` Damage Bonus `+6`; `ArcaneRecovery` Cycle `-3s`; `FasterOrbit` Rotation Speed `+20` |
| Drone | `NoUpgrade` | `ExpandedPatrol` Range `+1`; `HighCaliberRounds` Damage Bonus `+2`; `OptimizedBurstModule` Burst Cooldown `-0.5s` |

Run Name format:

```text
Task004_PhaseA_LRoute_HP480_<Family>_L1_<UpgradeOrNoUpgrade>_<NN>
```

Phase A therefore contains `16` required runs: four controls plus twelve
single-Basic runs. Repeat only mislabeled, integrity-failing, materially
variable, or decision-threshold results.

### 6.5 Phase A Review Gate

- Wave 2 resolves `40 / 40`; setup and measurement Wave attribution match.
- Final deployed Tower count is `1`, Tower Level is `1`, and its family,
  placement, BasicDamage, resolved stats, and Upgrade list match the Run Name.
- No Behaviour or Elemental Upgrade, TowerScaled Effect damage, or FixedBuff
  damage appears.
- `ResolutionCountsMatch`, `MonsterRuntimeCountsMatch`,
  `WaveMonsterAttributionMatches`, `TowerDeploymentCoverageMatches`, and
  `DamageDiagnosticsCountsMatch` are true.
- Ordinary damage/cadence Basics are reviewed against `1.15x-1.30x`; range or
  spatial results may be retained outside that band only with named evidence.

### 6.6 Phase A Results And Decisions

Magic uses three-run means because random initial Orbit phase materially varies
contact count. Other Phase A entries use their valid first controlled run.

| TowerFamily | Basic Upgrade | Control ED | Upgrade ED | Gain | Phase A decision |
|---|---|---:|---:|---:|---|
| Archer | Eagle Sight | `2320` | `2500` | `1.078x` | Keep as spatial exception using existing route evidence |
| Archer | Quick Draw | `2320` | `2700` | `1.164x` | Keep |
| Archer | Sharpened Arrows | `2320` | `2855` | `1.231x` | Keep |
| Cannon | Extended Barrel | `2040` | `2220` | `1.088x` | Keep as spatial exception using existing route evidence |
| Cannon | Faster Reload | `2040` | `2700` | `1.324x` | Keep at reviewed upper edge |
| Cannon | Reinforced Shells | `2040` | `2720` | `1.333x` | Keep at reviewed upper edge |
| Magic | Arcane Charge | `2283` mean | `2769` mean | `1.213x` | Keep |
| Magic | Arcane Recovery | `2283` mean | `2608` mean | `1.142x` | Keep; `22s -> 19s` scheduler identity |
| Magic | Faster Orbit | `2283` mean | `2833` mean | `1.241x` | Keep |
| Drone | Expanded Patrol | `2090` | `2240` | `1.072x` | Keep as spatial exception using existing route evidence |
| Drone | High-Caliber Rounds | `2090` | `2520` | `1.206x` | Keep |
| Drone | Optimized Burst Module | `2090` | `2610` | `1.249x` | Keep |

All accepted runs resolved the HP480 measurement Wave `40 / 40`, matched the
named Tower and Upgrade state, and passed combat, Monster, Wave attribution,
deployment, and damage-diagnostic integrity. The fixed sentinel produces the
known progression-only expected-count mismatch and does not invalidate combat
evidence.

## 7. Phase B Configuration

### 7.1 Shared Fixture

| Setting | Value |
|---|---|
| Map | `Prefab_Map_Default_LRoute` |
| Player Max Health | `100` |
| Progress Requirements | `[2, 2, 99]` |
| Draft Mode | Fixed; exactly `4` Steps |
| Wave 1 - setup | Delay `5s`; `4` x `Prefab_MonsterCandidate_Dragon`; HP `60`; Move Speed `0.25`; Spawn Interval `2.5s` |
| Wave 2 - measurement | Archer/Cannon/Drone Delay `25s`; Magic Delay `90s`; `40` x `Prefab_MonsterCandidate_EvilMage`; HP `480`; Move Speed `0.25`; Spawn Interval `2.5s` |
| Tower state at measurement | Exactly one Level 2 Tower at its family P1, with either no Upgrade or exactly one named Behaviour Upgrade |
| Result authority | Wave 2 `effectiveDamage` plus package-specific signatures and trigger diagnostics |

The longer Magic delay allows the setup Wave to resolve and the Behaviour
Draft to commit before its HP480 measurement Wave begins. Other families reuse
the accepted Task003 `25s` three-Draft delay.

### 7.2 Fixed Draft Sequence

For a Behaviour run:

1. Step 1: the tested TowerDefinition; deploy it at the matching P1.
2. Step 2: the same TowerDefinition; apply it to level the P1 Tower to Level 2.
3. Step 3: exactly one tested Behaviour Upgrade; apply it to the Level 2 Tower.
4. Step 4: the same TowerDefinition as the unreachable sentinel.

For a no-Upgrade control, Step 3 is the same TowerDefinition. Select it to
complete the Draft but leave the resulting Pending Tower undeployed. The final
combat state must still contain exactly one Level 2 Tower and no Upgrade.

### 7.3 Required Runs And Expected Damage Identities

Each family requires one fresh Level 2 control plus its three Behaviour runs:

| TowerFamily | Behaviour | Expected identity at Level 2 |
|---|---|---|
| Archer | Piercing Arrow | Primary Damage `44`; finite unique-hit capacity `3` |
| Archer | Scatter Arrow | Primary `44`; two additional Arrows at DamageScale `0.25` -> `11` each |
| Archer | Explosive Arrow | Primary `44`; direct-hit area Effect at DamageScale `0.25` -> `11` per valid target |
| Cannon | Explosive Shell | Primary `132`; Position Impact area Effect at accepted DamageScale `0.20` -> `26` per valid target |
| Cannon | Twin Shells | Primary `132`; one additional Shell at DamageScale `0.5` -> `66` |
| Cannon | Bouncing Shell | Primary `132`; one bounce at DamageScale `0.5` -> `66` |
| Magic | Twin Orbs | Primary Orb `66`; one additional Orb at DamageScale `0.4` -> `26` |
| Magic | Arcane Detonation | Contact `66`; normal-completion Effect at DamageScale `6.6666667` -> `440` per valid target |
| Magic | Arcane Field | Contact `66`; `0.5s` field ticks at DamageScale `0.0333333` -> `2` per valid target |
| Drone | Double Drones | Primary Drone `22`; one additional Drone at DamageScale `0.6` -> `13` |
| Drone | Blast Rounds | Projectile `22`; direct-hit area Effect at accepted DamageScale `0.25` -> `6` per valid target |
| Drone | Final Dive | Projectile `22`; battery-end impact Effect at accepted DamageScale `8` -> `176` per valid target |

Run Name format:

```text
Task004_PhaseB_LRoute_HP480_<Family>_L2_<BehaviourOrNoUpgrade>_<NN>
```

Phase B contains `16` initial runs: four Level 2 controls and twelve
single-Behaviour runs. Repeat only integrity failures, mechanism ambiguity,
material variance, or decision-threshold results.

### 7.4 Phase B Review Gate

- The final deployed state is exactly one Level 2 Tower at the named P1 with
  either no Upgrade or exactly one named Behaviour package.
- The complete requested arrangement and package are committed before HP480
  measurement exposure begins.
- Primary, Additional, Bounce, and Behaviour Effect signatures use the expected
  Level 2 BasicDamage and stable DamageScale shown above.
- Package-specific completion, impact, unique-hit, capacity, and repeated-trigger
  behavior matches its System contract.
- Wave 2 resolves `40 / 40`, and combat, Monster, Wave attribution, deployment,
  and damage-diagnostic integrity all pass.

### 7.5 Phase B Results And Decisions

All values below use measurement-Wave Effective Damage. Explosive Shell, Blast
Rounds, and Final Dive show their accepted tuned reruns; their original values
remain diagnostic evidence only.

| TowerFamily | Behaviour | Control ED | Upgrade ED | Gain | Decision |
|---|---|---:|---:|---:|---|
| Archer | Piercing Arrow | `5096` | `5360` | `1.052x` | Keep as alignment-dependent exception; diagnose in Phase C |
| Archer | Scatter Arrow | `5096` | `7241` | `1.421x` | Keep |
| Archer | Explosive Arrow | `5096` | `7974` | `1.565x` | Keep |
| Cannon | Bouncing Shell | `4524` | `6768` | `1.496x` | Keep |
| Cannon | Twin Shells | `4524` | `6522` | `1.442x` | Keep |
| Cannon | Explosive Shell | `4524` | `6872` | `1.519x` | Keep after DamageScale `0.4166667 -> 0.20` |
| Magic | Twin Orbs | `5676` | `8398` | `1.480x` | Keep |
| Magic | Arcane Detonation | `5676` | `8808` | `1.552x` | Keep |
| Magic | Arcane Field | `5676` | `8252` | `1.454x` | Keep |
| Drone | Double Drones | `4578` | `6967` | `1.522x` | Keep |
| Drone | Blast Rounds | `4578` | `6966` | `1.522x` | Keep after DamageScale `0.40 -> 0.25` |
| Drone | Final Dive | `4578` | `6942` | `1.516x` | Keep after DamageScale `12 -> 8` |

The tuned signatures were exactly Explosive Shell `132 * 0.20 -> 26`, Blast
Rounds `22 * 0.25 -> 6`, and Final Dive `22 * 8 -> 176`. No unexplained
duplicate Effect execution or damage multiplication appeared.

## 8. Phase C Route Diagnosis

Piercing Arrow was the only Behaviour requiring additional route evidence:

| Route and accepted P1 | NoUpgrade ED | Piercing ED | Gain | Observation |
|---|---:|---:|---:|---|
| Straight | `4748` | `6156` | `1.297x` | Aligned lane materially increases unique hits |
| L | `5096` | `5360` | `1.052x` | Corner exposure gives weak conversion |
| U | `5532` | `12128` | `2.192x` | Premium alignment stress; finite but exceptionally strong |

The route fixtures use their own accepted family placement, so this is a
spatial diagnosis rather than a route-shape-only causal experiment. It is
sufficient to retain Piercing Arrow's finite MaxHitCount `3` as a deliberate
deployment-dependent Upgrade; no damage-value adjustment is required. Existing
Task002 route evidence is sufficient for Eagle Sight, Extended Barrel, and
Expanded Patrol, so no additional range-Basic runs were required.

## 9. Phase D Combination Results

Phase D uses `[2, 2, 2, 99]`, five Fixed Draft Steps, a six-Monster HP60 setup
Wave, and one Level 2 Tower with the named Upgrades before the HP480 measurement
Wave. Archer, Cannon, and Drone use Delay `25s`; Magic uses Delay `90s`.

### 9.1 Basic Plus Behaviour

| TowerFamily | Combination | Control ED | Build ED | Gain |
|---|---|---:|---:|---:|
| Archer | Quick Draw + Explosive Arrow | `5096` | `9257` | `1.817x` |
| Cannon | Faster Reload + Explosive Shell | `4524` | `9538` | `2.108x` |
| Magic | Arcane Charge + Twin Orbs | `5676` | `8945` | `1.576x` |
| Drone | Optimized Burst Module + Blast Rounds | `4578` | `7958` | `1.738x` |

The Drone acceptance uses the corrected Delay `25s` rerun. Its original Delay
`90s` measurement produced `8402`, but began Wave 2 in a different point of the
`20s` Drone lifecycle and is excluded from the controlled comparison.

### 9.2 Behaviour Plus Behaviour

| TowerFamily | Route | Combination | Control ED | Build ED | Gain |
|---|---|---|---:|---:|---:|
| Archer | U | Piercing Arrow + Explosive Arrow | `5532` | `18084` | `3.269x` |
| Cannon | L | Bouncing Shell + Explosive Shell | `4524` | `11818` | `2.612x` |
| Magic | L | Twin Orbs + Arcane Detonation | `5676` | `14206` | `2.503x` |
| Drone | L | Double Drones + Final Dive | `4578` | `11432` | `2.497x` |

The Archer result is an intentional U-route alignment stress rather than a
universal expected gain. Cannon produced one explosion per eligible primary or
bounce impact; Magic produced one normal-completion detonation per active Orb;
Drone produced one battery-end branch per active Drone. These signatures show
bounded independent ownership rather than accidental recursion.

### 9.3 Three-Upgrade Core Builds

| TowerFamily | Core Build | Control ED | Build ED | Gain |
|---|---|---:|---:|---:|
| Archer | Quick Draw + Explosive Arrow + Scatter Arrow | `5096` | `17228` | `3.381x` |
| Cannon | Faster Reload + Explosive Shell + Twin Shells | `4524` | `15076` | `3.332x` |
| Magic | Faster Orbit + Arcane Field + Twin Orbs | `5676` | `12370` | `2.179x` |
| Drone | Optimized Burst Module + Blast Rounds + Double Drones | `4578` | `12771` | `2.790x` |

No Core build reached the HP480 measurement-Wave `19200` Effective Damage
ceiling. Every accepted combination matched its requested final Tower state and
passed combat, Monster, Wave attribution, deployment, and complete
TowerScaled-diagnostic integrity.

## 10. Phase E Review And Waiver

No Phase A-D run exposed a package-composition or runtime-refresh defect. Phase
D also supplied runtime evidence that different Behaviour package types coexist
and preserve independent trigger ownership. The current System contracts and
implementation already define:

- per-Tower duplicate-definition and package-type rejection;
- Required Tower Level and TowerFamily eligibility;
- rejected application as a no-mutation, no-consumption transaction;
- multiple different Behaviour packages as compatible by default;
- scheduler-ratio, active-entity, and matching-package live refresh.

The user accepted the residual risk and waived additional manual Phase E
eligibility and mid-entity refresh scenarios on 2026-08-22. This is not recorded
as fresh Play Mode proof of those individual rules. Because Phase A-D already
reran every accepted value edit in single or combination combat, no additional
full-family final regression is required unless a later code change touches
eligibility, Pending consumption, package capacity, or live refresh.

The unreachable `99` terminal requirement intentionally leaves
`LevelUpCountMatches`, `LevelUpResolutionNodesMatch`, and
`FinalPlayerLevelMatches` false in these calibration fixtures. All combat,
Monster, Wave, Draft-attempt, deployment, and damage-diagnostic integrity gates
required for the balance decisions pass.

## 11. Final Accepted Values

- Magic base Attack Cycle is `22s`; Arcane Recovery resolves it to `19s` against
  the unchanged `18s` Orb Max Lifetime.
- Explosive Shell TowerScaled DamageScale is `0.20`.
- Blast Rounds TowerScaled DamageScale is `0.25`.
- Final Dive TowerScaled DamageScale is `8`.
- All other reviewed Basic and Behaviour authoring values remain unchanged.
- Piercing Arrow remains a deployment-alignment exception with MaxHitCount `3`.

## 12. Acceptance

- Every Upgrade has a reviewed same-level control and decision.
- Basic value is reliable and Behaviour value is conditionally stronger where
  its mechanic is intended to work.
- Higher current BasicDamage naturally strengthens TowerScaled results.
- No FixedBuff damage appears in this Task's gain calculation.
- Combinations remain bounded and preserve TowerFamily identity.
- Phase E manual eligibility and live-refresh reruns are explicitly waived with
  their residual risk recorded rather than represented as executed evidence.
