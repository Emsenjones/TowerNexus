# Task007 - Elemental And Buff Baseline

Status: Planned

Depends on: Completed Task005 Elemental Stack Contribution Damage Authority; completed Task006 Drone Burst Elemental Opportunity Refactor

Blocks: Task008-Task016

## 1. Goal

Calibrate Elemental application, contributor-owned StackApplied TowerScaled
damage, shared-state FixedBuff damage, stacking, Overload, Protection, and
matching-source cooperation on top of the accepted Tower and non-Elemental
baselines.

## 2. Required Screens

- every TowerFamily and ElementType application boundary;
- one source applying each Elemental Buff;
- two matching source Towers contributing to one shared Buff;
- source-scoped apply cooldown;
- StackApplied, Overload, Protection, expiry, and re-entry;
- Burning periodic and Overload FixedDamage;
- Electric contributor-owned StackApplied TowerScaled damage and shared-state
  LightningStrike FixedDamage;
- Wind contributor-owned StackApplied TowerScaled damage and shared-state
  WindVortex FixedDamage;
- Cold slow and Frozen movement lock;
- Behaviour plus Elemental opportunity combinations;
- no recursive Elemental application from reaction damage.

## 3. Interpretation

- A single Elemental Upgrade provides useful normal application value.
- Matching sources and Overload provide the higher conditional ceiling.
- FixedBuff damage is independent from later source-Tower Level or Damage Bonus.
- StackApplied TowerScaled damage reads the exact Tower that contributed that
  successful stack.
- Elemental is not required to outperform Behaviour in every geometry or target
  state.

## 4. Acceptance

- All TowerScaled DamageScale and FixedBuff FixedDamage values are positive,
  intentional, and Recorder-visible.
- All stack, cooldown, lifecycle, and Protection transitions are deterministic.
- Matching-source cooperation is stronger than isolated application without
  becoming an automatic result independent of coverage.
- Elemental opportunity counts match reviewed attack boundaries.
- Only the approved contributor-owned StackApplied damage reads current Tower
  BasicDamage; shared lifecycle, Overload, Protection, and persistent reaction
  damage do not.

## 5. Implementation Phases

### Phase A - Level 3 Single-Element Application Screen

- Record one fresh L-route Level 3 no-Upgrade control for each TowerFamily.
- Test all sixteen Elemental Upgrades at Level 3, one Upgrade per run.
- Compare only measurement-Wave Effective Damage against the matching family
  control; the setup Wave exists only to complete the required Drafts.
- Verify the named source Tower instance, Elemental Upgrade, shared Buff,
  application results, contributor TowerScaled signatures, and FixedBuff
  signatures.

### Phase B - Shared Runtime And Matching-Source Cooperation

- Compare a fixed pair of Level 3 Towers with unchanged families, placements,
  and direct TowerScaled output between runs. The pair may use different
  TowerFamilies because matching cooperation is defined by ElementType and the
  shared BuffDefinition, not by one TowerUpgradeDefinition identity.
- In the control, only one Tower owns the tested Element. In the treatment,
  both Towers own matching Elemental Upgrades for that ElementType.
- Verify independent source cooldown entries, two successful source Tower
  identities, faster or more reliable overload, Protection, expiry, and
  re-entry.
- Use the authored `MaximumStacks 10`, `ActiveDuration 5s`, and Protection
  duration as the first candidate. Review first-application-to-overload timing,
  successful-application gaps, natural-expiry stack distribution, distinct
  overloaded Monsters, and Protection re-entry before changing those values.
- Use one representative three-source screen to confirm that additional
  matching sources accelerate the first Overload but remain bounded by
  Protection.
- Multiple-stack contribution per successful application is deferred. Reopen
  it only if schema-17 evidence shows that realistic matching-source coverage
  cannot reach Overload reliably with one stack unit per application.

### Phase C - Element-Specific Reactions

- Use dense high-health fixtures for Burning, Cold/Frozen, Electric multi-target
  execution, Wind StackApplied, and WindVortex behavior.
- Review FixedBuff values separately from target count, radius, duration,
  cooldown, and movement-control value.

### Phase D - Behaviour Elemental Opportunities

- Test each of the twelve Behaviour packages once with a representative
  Elemental Upgrade.
- Match Elemental application attempts to its reviewed primary, additional,
  area, bounce, contact, persistent, completion, or Drone Burst-opener
  boundaries.
- Confirm Buff and reaction damage never creates recursive Elemental attempts.

### Phase E - Eligibility, Presentation, And Final Regression

- Review Required Level, second-Element rejection, rejected-item atomicity,
  status icon and stack display, Protection pulse, persistent Buff VFX, and
  technical cleanup.
- Run the smallest final four-family regression after accepted value edits.

## 6. Recorder Preparation

Task007 Phase A uses Recorder schema `16`, including the Task006 Drone Burst
diagnostic export. Phase B and later fresh reports use schema `17`.

Schema `17` preserves schema-16 combat, damage, source, and Drone diagnostics
and adds Buff calibration evidence for:

- stacking cycles started, overloaded, and naturally expired;
- distinct Monsters overloaded and Protection-to-stacking re-entry;
- first application to Overload timing range and sample count;
- successful application gap timing range and sample count;
- Protection expiry to reapplication timing range and sample count;
- stack count distribution at natural expiry;
- distinct source count distribution at Overload;
- stack units added separately from stacked application count;
- per-source-Wave Buff summaries and Buff diagnostic integrity flags.

The stack-unit fields make one-stack applications explicit without authorizing
multiple-stack contribution. A future gameplay change must separately define
requested, applied, and discarded stack units and the one-Overload-per-
application boundary.

Each Buff source record includes:

- source Tower instance ID;
- TowerFamily;
- Elemental Upgrade asset name;
- application, cooldown, Protection, overload, and lifecycle counts.

This source identity is diagnostic only. It does not create Buff state or
participate in cooldown authority.

Before Phase A, the Cold/Frozen authoring baseline is repaired to match the
System contract:

- Chilled clears its speed multiplier on both EnteredProtection and Removed;
- Frozen has no PeriodicTick interval because it owns no PeriodicTick behavior.

The six `Task005_PhaseA_LRoute_HP480_Archer_L3_*` reports recorded before
Task005 and Task006 were introduced remain pre-revision diagnostic evidence.
They identified the cooldown and damage-ownership problems but cannot satisfy
Task007 acceptance. Task007 begins with fresh `Task007_*` reports after both
upstream implementations are accepted.

## 7. Phase A Configuration

### 7.1 Shared Fixture

| Setting | Value |
|---|---|
| Map | `Prefab_Map_Default_LRoute` |
| Player Max Health | `100` |
| Progress Requirements | `[2, 2, 2, 99]` |
| Draft Mode | Fixed; exactly `5` Steps |
| Wave 1 - setup | Delay `5s`; `6` x `Prefab_MonsterCandidate_Dragon`; HP `60`; Move Speed `0.25`; Spawn Interval `2.5s` |
| Wave 2 - measurement | Family delay: Archer/Cannon/Drone `25s`, Magic `90s`; `40` x `Prefab_MonsterCandidate_EvilMage`; HP `480`; Move Speed `0.25`; Spawn Interval `2.5s` |
| Tower state at measurement | Exactly one Level 3 Tower at its family P1, with no Upgrade or exactly one named Elemental Upgrade |
| Result authority | Wave 2 `effectiveDamage`, Buff observations, and damage diagnostics |

The Stage Upgrade pool contains all current Basic, Behaviour, and sixteen
Elemental definitions so every TowerFamily resolves a Level 3 cap. Fixed Draft
choices remain the authority for the actual run content.

### 7.2 Fixed Draft Sequence

For an Elemental run:

1. Step 1: the tested TowerDefinition; deploy it at the matching P1.
2. Step 2: the same TowerDefinition; apply it to level P1 to Level 2.
3. Step 3: the same TowerDefinition; apply it to level P1 to Level 3.
4. Step 4: exactly one tested Elemental Upgrade; apply it to P1.
5. Step 5: the same TowerDefinition as the unreachable sentinel.

For a no-Upgrade control, Step 4 is the same TowerDefinition. Select it to
complete the Draft but leave the resulting Pending Tower undeployed. The final
combat state must still contain exactly one Level 3 Tower and no Upgrade.

### 7.3 Family Placements

Reuse the accepted Task002/Task004 L-route P1 footprints:

| TowerFamily | P1 occupied Grids |
|---|---|
| Archer | `(2,3)`, `(2,4)` |
| Cannon | `(2,4)`, `(3,3)`, `(3,4)` |
| Magic | `(2,3)` |
| Drone | `(2,3)`, `(2,4)`, `(3,3)`, `(3,4)` |

### 7.4 Required Runs

Each family uses one control as the denominator for its four Elemental runs:

| TowerFamily | Control | Elemental Upgrades |
|---|---|---|
| Archer | `NoUpgrade` | `BlazingArrows`; `FrostboundArrows`; `ChargedArrows`; `GaleArrows` |
| Cannon | `NoUpgrade` | `BlazingShells`; `FrostboundShells`; `ChargedShells`; `GaleShells` |
| Magic | `NoUpgrade` | `BlazingOrbs`; `FrostboundOrbs`; `ChargedOrbs`; `GaleOrbs` |
| Drone | `NoUpgrade` | `BlazingDrones`; `FrostboundDrones`; `ChargedDrones`; `GaleDrones` |

Run Name format:

```text
Task007_PhaseA_LRoute_HP480_<Family>_L3_<ElementalOrNoUpgrade>_<NN>
```

Phase A contains `20` required runs: four controls plus sixteen single-Element
runs. Repeat only mislabeled, integrity-failing, materially variable, or
decision-threshold results.

### 7.5 Prepared First Run

After Task005 and Task006 acceptance, prepare `Assets/Scenes/Main.unity` for:

```text
Task007_PhaseA_LRoute_HP480_Archer_L3_NoUpgrade_01
```

The fixed sequence contains five Archer choices and Step 4 is the undeployed
control Tower. After recording this control, change only the Run Name and Step
4 choice to the tested Archer Elemental Upgrade. When changing family, update
Steps 1-3 and Step 5 to that family, restore Step 4 to the control Tower or
tested Elemental Upgrade as appropriate, and use the family measurement-Wave
delay above.

### 7.6 Phase A Review Gate

- Both Waves resolve all configured Monsters and Wave attribution matches.
- The complete requested Level 3 arrangement is committed before measurement
  exposure.
- Final deployed Tower count is `1`; family, Level 3 BasicDamage, placement,
  and Upgrade list match the Run Name.
- A control observes no Buff, contributor TowerScaled Effect damage, or
  FixedBuff damage.
- An Elemental run observes only the named source Tower instance, named
  Elemental Upgrade, and its shared Buff content.
- Source records contain a nonzero Tower instance ID and no unattributed
  application.
- All FixedBuff signature values equal their authored positive FixedDamage and
  remain independent from Tower Level BasicDamage and Damage Bonus.
- Electric and Wind StackApplied TowerScaled signatures identify the exact
  contributing Tower and use its current resolved BasicDamage with the authored
  positive DamageScale.
- Drone application attempts match Burst opener, eligible Blast Rounds, and
  independent Final Dive boundaries rather than ordinary projectile-hit count.
- Reaction FixedBuff resolutions do not create additional Elemental application
  attempts.
- Resolution, Monster runtime, Wave attribution, Tower deployment, and damage
  diagnostic integrity all pass.
