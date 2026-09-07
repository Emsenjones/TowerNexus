# Task001 - Combat Damage Formula Refactor

Status: Completed; all phases accepted through Unity runtime, schema-v14 Recorder evidence, and Combat Damage Authoring validation

Depends on: Accepted current Tower, Upgrade, Projectile, Effect, Buff, Recorder, and Tower placement runtimes; archived CombatMathV1 evidence

Blocks: Task002-Task007 and Task010-Task017

## 1. Goal

Replace the current runtime-template and independently authored Tower-damage
model with one Level-owned BasicDamage foundation while preserving every
reviewed attack topology, trigger order, package timing, target rule, lifecycle,
and Elemental opportunity.

Tower-owned direct and Behaviour damage must derive from the source Tower's
current resolved BasicDamage. Buff lifecycle and Elemental-reaction damage must
remain independently authored FixedDamage.

This Task establishes structure and migration correctness. It does not accept
final Level, Upgrade, Buff, Monster, or Stage balance values.

## 2. Source Contracts

- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/04_BattleHUDUISystem.md`
- `Doc/System/09_TowerPlacementSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/12_ProjectileSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/System/15_BuffSystem.md`

## 3. Authoritative Formula

```text
CurrentResolvedBasicDamage
    = Current TowerLevelConfig.BasicDamage
    + Sum Of Applied Basic Damage Bonus Deltas

TowerOwnedDamage
    = RoundToInt(CurrentResolvedBasicDamage At Damage Boundary * Stable DamageScale)

BuffLifecycleDamage
    = Authored FixedDamage
```

Exactly one final integer rounding occurs for each damage result. No caller may
pre-round one operand and then round the result again.

Basic Damage Bonus remains whole-number authoring in Task001. Resolution still
preserves its raw accumulated value and performs no intermediate rounding.
DamageScale, CurrentResolvedBasicDamage, and their raw product must all be
finite. A missing source Tower, non-positive CurrentResolvedBasicDamage,
non-positive DamageScale, or non-finite input rejects that complete Tower-owned
gameplay result; it is never clamped into a valid zero-damage result.

## 4. Data Authority

### 4.1 Tower Level

`TowerLevelConfig` owns:

- unique positive Level;
- usable Tower Level Model;
- positive integer BasicDamage.

The Tower runtime combat component no longer owns a second base-damage field.
The same TowerDefinition may therefore author a different BasicDamage for L1,
L2, and L3 without duplicating non-damage archetype data.

### 4.2 Tower-Owned Damage

The following carry a stable DamageScale and source Tower identity:

- Archer primary, Scatter, Piercing, and Explosive Arrow results;
- Cannon primary, additional, bounce, and Explosive Shell results;
- Magic primary/additional Orb contacts, Arcane Detonation, and Arcane Field;
- Drone primary/additional direct shots, Blast Rounds, and Final Dive.

Primary direct attacks use scale `1`. Additional members, bounce children, and
Behaviour Effects own their explicit positive scale.

### 4.3 Fixed Buff Damage

The following use positive integer FixedDamage and ignore current Tower
BasicDamage:

- Burning PeriodicTick and Overload;
- Electric StackApplied and Overload LightningStrike;
- Windcut StackApplied secondary attack;
- WindVortex ticks.

ApplyBuff, movement, wrapper, and spawn actions do not author damage.

## 5. Timing And Live Resolution

- DamageScale and package identity follow their existing Windup, release,
  pre-impact, group, or persistent-entity timing contract.
- Final Tower-owned integer damage reads the source Tower's current resolved
  BasicDamage when that hit, contact, explosion, field tick, or dive impact
  actually resolves.
- A Level or Basic Damage Bonus change affects later unresolved Tower-owned
  results but never replays an earlier result.
- Targets, landing positions, directions, hit history, bounce history, consumed
  capacity, elapsed lifetime, and completed results remain unchanged.
- Missing source Tower for TowerScaled damage is invalid runtime state, not a
  reason to reinterpret the action as FixedDamage. Normal battle teardown uses
  technical cleanup and produces no gameplay result.
- An invalid Tower-owned damage source rejects the complete owning result: it
  deals no direct damage, executes no Tower-owned Behaviour Effect, creates no
  Elemental opportunity, and records no successful damage. An already accepted
  FixedBuff lifecycle remains independent from later source-Tower validity.

## 6. Package Timing Preserved

| Package | Preserved Boundary |
|---|---|
| Piercing Arrow | May add capacity delta to an eligible active Arrow without clearing history |
| Scatter Arrow | Future Windup only |
| Explosive Arrow | Future release only; never retrofit an active Arrow |
| Explosive Shell | May affect an unresolved airborne initial Shell before impact |
| Multi Shells | Future Windup only |
| Bouncing Shell | May affect an initial Shell before first Position Impact; active chain identity remains stable |
| Multi Orbs | May atomically add missing active-group members |
| Arcane Detonation | May grant future normal-completion eligibility to an incomplete group |
| Arcane Field | Reconcile one Tower-owned field immediately |
| Multi Drones | Change capacity without batch launch |
| Blast Rounds | Future shots and eligible unresolved airborne Drone projectiles |
| Final Dive | Active Drones only before battery-end branch resolution |

Damage migration must not broaden any row in this table.

## 7. Effect Schema

`DealDamage` has one explicit DamageMode:

| Mode | Required Authoring | Forbidden Authoring |
|---|---|---|
| TowerScaled | positive DamageScale; valid source-Tower execution path | FixedDamage |
| FixedBuff | positive integer FixedDamage | DamageScale dependency on source Tower |

There is no compatibility fallback to the old authored amount or trigger-context
resolved damage. Invalid assets fail validation and are migrated in the same
clean break.

Behaviour Effect identity is the EffectDefinition identity plus the authored
action ordinal. Owner-side validation must prove that Behaviour packages refer
only to TowerScaled damage and that Buff lifecycle, reaction, and WindVortex
paths refer only to FixedBuff damage. Nested Effect validation is recursive; a
revisited definition is an invalid cycle rather than a successful early exit.

## 8. Level-Up Transaction

Level Up is prepared before gameplay state changes:

- the exact held Draft identity and Battle HUD ownership are valid;
- the target, next TowerLevelConfig, and combat runtime are ready;
- the next combat baseline is fully prepared.

The non-failing semantic commit is ordered:

```text
Commit Level
    -> Apply Prepared Combat Baseline
    -> Remove Exact Held Draft From Semantic Ownership
    -> Mark Pending Draft View Consumed
```

Applying the prepared baseline is a pure cache assignment. It performs no
validation, virtual callback, active-entity traversal, allocation-dependent
preparation, diagnostics, or event publication. Marking the Pending Draft view
consumed is deterministic state assignment that makes every pointer and drag
handler reject further input immediately.

`OnLevelChanged` and diagnostics are exception-isolated post-commit
notifications. They do not refresh gameplay state and no subscriber is an
authority for Level or combat-baseline correctness. Model replacement, Attack
Origin handoff, VFX, and destruction of the consumed view are best-effort
post-commit presentation; their failure cannot restore Draft ownership or roll
back the accepted gameplay change.

## 9. Implementation Phases

### Phase A - Level Damage Authority

- Add BasicDamage to `TowerLevelConfig` and its validation.
- Resolve current Level BasicDamage through TowerInstance/TowerDefinition.
- Remove the runtime-template base-damage authority.
- Implement the prepared, non-failing Level-Up transaction defined above.
- Use a temporary Level-derived direct-damage projection only as an internal
  Phase-A bridge until every direct carrier is migrated in Phase B. It is not a
  serialized authority, compatibility path, or retained final API.
- In the same phase, write all four TowerDefinitions' provisional L1/L2/L3
  BasicDamage and remove the four runtime-prefab base-damage fields.
- End with compiling source and valid Tower assets.

### Phase B0 - Rounding Audit Before Scale Assets

- Generate the complete provisional Level-by-source rounding matrix before the
  first DamageScale asset is written.
- Record zero results, monotonicity, `.5` threshold sensitivity, and material
  relative error. Known provisional error is evidence, not an implicit balance
  acceptance or reason to change the global combat unit.

### Phase B - Shared Resolution And Every Direct Carrier

- Introduce one narrow resolver that returns one immutable
  `TowerOwnedDamageResolution` containing Tower family, Level, Level
  BasicDamage, raw Damage Bonus, resolved BasicDamage, Damage Source identity,
  DamageScale, raw product, and final damage.
- Gameplay, impact context, and exception-isolated diagnostics consume that same
  result; no observer invokes the resolver or recomputes the formula.
- Convert primary attacks to DamageScale `1` and convert additional-member and
  bounce authoring from absolute damage to positive DamageScale.
- In the same phase, migrate every direct carrier and its assets: Archer and
  Cannon Projectiles, Magic Orbs, Drones, Drone-fired Projectiles, the Final
  Dive carrier/context, and bounce children/chains.
- Preserve source Tower and stable Damage Source identity through every carrier.
- End with compiling source, valid direct-damage assets, and a naked-L1 gate.

### Phase C - Effect And Buff Migration

- Add explicit DealDamage mode validation.
- Migrate the six Tower-owned Behaviour Effect groups to TowerScaled.
- Migrate the six Buff lifecycle/reaction damage groups to FixedBuff.
- Migrate Arcane Field, explosion, and Final Dive Effect consumers together
  with every affected Effect asset; clear stale damage fields from non-damage
  actions.
- Add recursive owner-side mode validation and reject Effect-definition cycles.
- Keep non-damage and composite Effects semantically unchanged.
- Preserve all Elemental eligibility and recursion boundaries.
- End with compiling source and valid Effect, Buff, Upgrade, and runtime-prefab
  assets.

### Phase D - Clean-Break Cleanup And Package Matrix

- Make Level and Basic Damage Bonus changes affect later unresolved Tower-owned
  results without altering stable package payloads or entity history.
- Delete every remaining absolute-damage snapshot, damage-refresh payload, and
  parallel runtime authority from Projectile, Magic, Drone, and package paths.
- `EffectTriggerContext` carries no inherited integer damage or direct-hit
  resolution. Each TowerScaled Effect resolves its own immutable result at its
  actual execution boundary; FixedBuff reads only its authored FixedDamage.
- The stale-authority scan includes TowerCombatBaseStats/ResolvedTowerCombatStats
  AttackDamage, Projectile/Magic/Drone basicDamage or attackDamage snapshots,
  cached damageBonus, LocksDirectDamage, TryRefreshDamage, and every damage
  refresh request or payload.
- Execute the complete package-timing matrix and focused live-resolution
  regression suite.

### Phase E - Recorder And Final Regression Gate

- Upgrade CombatBalanceRunRecorder to schema v14 and aggregate by damage
  signature rather than emitting one JSON row for every hit or tick.
- TowerScaled signatures contain Tower, Level, Level BasicDamage, raw Damage
  Bonus, resolved BasicDamage, Damage Source identity, DamageScale, raw product,
  and final damage.
- FixedBuff signatures contain Effect identity, action ordinal, and FixedDamage;
  source Tower is optional diagnostics and Level/BasicDamage are not required.
- Aggregate resolution, success, invalid-source, and final-damage counts plus the
  rounding audit. Diagnostics remain read-only and never recompute damage.
- Add one Editor-wide asset validator for cross-owner DamageMode and recursive
  Effect checks; it reports invalid authoring but owns no runtime behavior.
- Expose that validator at `Tools/Tower Nexus/Validate Combat Damage
  Authoring`; run it after Unity imports the Phase-E source and assets.
- Preserve GUIDs and references, run complete source/asset validation, and
  build both runtime and Editor assemblies before the final Play Mode regression
  and Task002 handoff.

## 10. Provisional Migration Values

Task001 may author provisional L2/L3 values solely to exercise live Level damage
refresh. They are not accepted balance values. L1 migration should initially
preserve the current candidate identities:

| Tower | L1 | Provisional L2 | Provisional L3 |
|---|---:|---:|---:|
| Archer | 20 | 44 | 68 |
| Cannon | 60 | 132 | 204 |
| Magic | 30 | 66 | 102 |
| Drone | 10 | 22 | 34 |

Existing absolute Tower-owned damage values should be converted to scales that
reproduce or closely approximate their current L1 result. Task004 owns their
final calibration.

## 11. Integer And Rounding Audit

Before choosing a larger global combat unit, generate the complete matrix:

`TowerFamily x Level x DamageSource -> raw product -> rounded integer -> relative error`

Relative error is defined only for positive raw products:

```text
Abs(FinalDamage - RawProduct) / RawProduct
```

With the provisional values, Arcane Field L2 and L3 currently expose about
`9.09%` and `11.76%` relative rounding error. Task001 records and provisionally
accepts this monotonic non-zero result; Task004 owns its later numeric decision.

Acceptance:

- no valid positive Tower-owned result becomes zero;
- damage is monotonic across increasing accepted Level BasicDamage;
- common results avoid unexplained `.5` threshold instability;
- material rounding error is reported before any scale migration decision.

If more precision is required, Tower BasicDamage, Basic Damage Bonus, Monster HP,
and FixedBuff damage must be scaled together. Increasing only Tower damage is a
balance change, not a precision migration.

### 11.1 Provisional Phase-B0 Matrix

The following matrix was completed before writing the first DamageScale asset.
`Result` is `L1 / L2 / L3`; percentages list only non-zero relative rounding
error. All rows are positive, monotonic, and avoid a `.5` threshold.

| Tower | Damage source | Scale | Raw products | Result | Relative error |
|---|---|---:|---|---|---|
| Archer | Primary | 1 | 20 / 44 / 68 | 20 / 44 / 68 | 0% |
| Archer | Additional / Explosive Arrow | 0.25 | 5 / 11 / 17 | 5 / 11 / 17 | 0% |
| Cannon | Primary | 1 | 60 / 132 / 204 | 60 / 132 / 204 | 0% |
| Cannon | Additional / Bounce | 0.5 | 30 / 66 / 102 | 30 / 66 / 102 | 0% |
| Cannon | Explosive Shell | 0.4166667 | 25 / 55 / 85 | 25 / 55 / 85 | 0% at authored precision |
| Magic | Primary | 1 | 30 / 66 / 102 | 30 / 66 / 102 | 0% |
| Magic | Additional Orb | 0.4 | 12 / 26.4 / 40.8 | 12 / 26 / 41 | 0% / 1.52% / 0.49% |
| Magic | Arcane Detonation | 6.6666667 | 200 / 440 / 680 | 200 / 440 / 680 | 0% at authored precision |
| Magic | Arcane Field | 0.0333333 | 1 / 2.2 / 3.4 | 1 / 2 / 3 | 0% / 9.09% / 11.76% |
| Drone | Primary | 1 | 10 / 22 / 34 | 10 / 22 / 34 | 0% |
| Drone | Additional | 0.6 | 6 / 13.2 / 20.4 | 6 / 13 / 20 | 0% / 1.52% / 1.96% |
| Drone | Blast Rounds | 0.4 | 4 / 8.8 / 13.6 | 4 / 9 / 14 | 0% / 2.27% / 2.94% |
| Drone | Final Dive | 12 | 120 / 264 / 408 | 120 / 264 / 408 | 0% |

Phase B migrates the Primary, Additional, and Bounce direct-carrier rows.
Phase C owns the serialized Effect rows; this audit fixes their provisional
scale inputs before that schema migration begins.

Naked L1 with no Damage Bonus must reproduce the migrated pre-refactor primary,
additional-member, and bounce integers. Damage-Bonus combinations intentionally
change under the new multiplicative formula; Task001 records those structural
differences and Task004 owns their numeric acceptance.

## 12. Out Of Scope

- Final L1 balance
- Final L2/L3 BasicDamage
- Final Upgrade DamageScale or Buff FixedDamage
- Monster HP, MoveSpeed, Wave Count, SpawnInterval, or WaveDelay
- Stage Player Health, Progress Requirements, Draft pools, or Draft weighting
- Any change to route reprojection, lane movement, Map layout, or placement
  validity

## 13. Acceptance

- One and only one Level BasicDamage authority exists.
- Every Tower-owned damage source uses current resolved BasicDamage and one
  stable positive DamageScale.
- Every Buff lifecycle/reaction damage source uses positive FixedDamage.
- Level Up refreshes unresolved Tower-owned damage and leaves prior results
  untouched.
- Level and its prepared combat baseline commit before exception-isolated
  notification; the exact Draft becomes non-interactive during the same
  semantic commit.
- Each phase that changes serialized schema also migrates its owning assets and
  ends in a compilable, locally valid checkpoint.
- Every actual Tower-owned damage boundary produces at most one
  TowerOwnedDamageResolution consumed by gameplay, context, and diagnostics.
- Package timing and topology match the preserved table.
- Explosive Arrow remains future-release-only.
- Arc Projectile landing snapshots, Direction flight, target history, Buff
  lifecycle, Elemental opportunities, and placement reprojection regressions
  remain correct.
- Recorder schema v14 distinguishes TowerScaled and FixedBuff signatures and
  identifies the formula inputs needed for Task002-Task007 without per-hit JSON
  expansion.
- Static source, asset, and Play Mode gates pass.

## 14. Handoff

Task002 starts only after Task001 implementation and focused Unity validation are
accepted. Task002 may revise L1 values but must not reopen damage ownership or
package timing without returning to Task001.

Task005 is the approved downstream revision point for one narrow exception to
this completed baseline: immediate Electric and Wind StackApplied damage becomes
TowerScaled from the exact Tower that contributed the current stack. It does not
reopen shared-state Periodic, Overload, Protection, persistent reaction, or
WindVortex FixedBuff ownership.
