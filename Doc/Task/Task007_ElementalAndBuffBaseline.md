# Task007 - Elemental And Buff Baseline

Status: Completed; CombatMathV2 Elemental package values accepted on 2026-08-27
with schema-20 Phase F-H evidence and the named progression-sentinel waiver

Depends on: Completed Task005 Elemental Stack Contribution Damage Authority;
completed Task006 Drone Burst Elemental Opportunity Refactor; Task007A
Elemental Stack Contribution implementation checkpoint; completed Task007B
Primary Elemental Opportunity Boundary Refactor; accepted Task007C Shared
Elemental Hit Reaction implementation checkpoint

Unblocks: Task010-Task017

## 1. Goal

Calibrate Elemental application, shared normal Buff value, stacking, Overload,
Protection, and matching-source cooperation on top of the accepted Tower and
non-Elemental baselines. Task007C's implementation checkpoint now supplies the
current Electric/Wind contract: contributor-owned StackApplied damage has been
replaced by shared cooldown-bounded FixedBuff Elemental hit reactions. Its
remaining edge-transaction smoke is explicitly deferred and does not block this
calibration.

## 2. Required Screens

- every TowerFamily and ElementType application boundary;
- one source applying each Elemental Buff;
- two matching source Towers contributing to one shared Buff;
- source-scoped apply cooldown;
- StackApplied lifecycle, Elemental hit reactions, Overload, Protection, expiry,
  and re-entry;
- Burning periodic and Overload FixedDamage;
- Electric Elemental hit-reaction FixedDamage on the Buff owner and shared-state
  LightningStrike FixedDamage;
- Wind one-secondary-target Elemental hit-reaction FixedDamage and shared-state
  WindVortex FixedDamage;
- Cold slow and Frozen movement lock;
- Behaviour plus Elemental opportunity combinations;
- no recursive Elemental application from reaction damage.

## 3. Interpretation

- A single Elemental Upgrade provides useful normal application value.
- Matching sources and Overload provide the higher conditional ceiling.
- Normal Elemental value is an absolute shared-package result, not a percentage
  multiplier on the carrying Tower's Effective Damage.
- FixedBuff damage is independent from later source-Tower Level or Damage Bonus.
- Elemental stack contribution changes Overload frequency but does not scale
  normal Electric/Wind reaction damage.
- Fire and Cold are source-independent normal-value packages. Electric and Wind
  are intentionally hit-frequency-sensitive shared-state packages: any
  authorized Tower damage may trigger an already-active reaction, while the
  Buff-owned `towerHitReactionCooldown` bounds repeated reactions on that Buff
  instance. TowerFamily cadence, multi-hit Behaviour, and overlapping fire may
  therefore create legitimate Electric/Wind Build synergy.
- Behaviour damage may trigger an active Electric/Wind reaction but remains
  ineligible to apply Elemental stacks. `sourceApplyCooldown` continues to own
  stack-application cadence; it is not a reaction-frequency control.
- Elemental is not required to outperform Behaviour in every geometry or target
  state.

## 4. Acceptance

- All TowerScaled DamageScale and FixedBuff FixedDamage values are positive,
  intentional, and Recorder-visible.
- All stack, cooldown, lifecycle, and Protection transitions are deterministic.
- Matching-source cooperation is stronger than isolated application without
  becoming an automatic result independent of coverage.
- Elemental opportunity counts match reviewed attack boundaries.
- Electric/Wind normal reactions, shared lifecycle, Overload, Protection, and
  persistent reaction damage do not read triggering or contributing Tower
  BasicDamage.

### 4.1 Current Calibration Contract

Task002 through Task004 accepted Level, Basic, and Behaviour value by comparing
the owning Tower's controlled output. Task007 uses a different primary measure:

```text
Normal Elemental Yield
    = ordinary shared Buff damage or control value
      during one fixed measurement window

Matching Upgrade Dividend
    = Two-Source Overlap Elemental Package Yield
      - One-Source Overlap Control Elemental Package Yield

Coverage Dividend
    = Two-Source Overlap Elemental Package Yield
      - Two-Source Isolated Elemental Package Yield
```

Fire normal yield is Burning periodic FixedBuff damage. Electric normal yield
is hit-reaction FixedBuff damage committed to the Buff owner. Wind normal yield
is secondary-target hit-reaction FixedBuff damage. Their Overload signatures
are measured separately. Cold has no direct FixedDamage equivalent; its normal
control value uses the absolute Effective Damage, leaked-health, and
battle-duration dividend against the matching no-Element control, while Frozen
counts and outcomes are reported separately as Overload value.

Task004's twelve accepted Behaviour packages produced a median absolute
measurement-Wave gain of approximately `2376`. Task007 rounds this to one
Elemental Reference Unit:

```text
1 ERU = 2400 fixed-value units
```

ERU is a Task007 decision aid for the controlled fixture, not a runtime stat,
damage type, global formula, or promise that every trigger and target has equal
value. The accepted reference profile is:

| Result | Accepted reference |
|---|---:|
| One-source Fire normal Elemental Yield | Typical `2240-2310`, approximately `0.9-1.0 ERU`; Cannon retains the named `1320` coverage exception |
| One-source Cold normal Elemental Yield | Authored `0.6` Move Speed Multiplier; compare control value, leaked health, and battle duration rather than FixedDamage |
| One-source Electric/Wind ordinary yield | No cross-family absolute band; shared per-trigger potency, cooldown bounding, and Build-synergy guardrails apply |
| One-source Overloads per forty measurement Monsters | `0-2`; occasional, not required for base value |
| Second matching source cooperation dividend | Observed `3352-3900`, approximately `1.4-1.6 ERU` |
| Two-source damaging Elemental package | Observed `4500-6000` fixed-value units |
| Overlap Overloads per forty measurement Monsters | Observed `8-15`, with `0` in all accepted isolated references |
| One Overload result in the standard density fixture | approximately `240-480` fixed-value units or the reviewed Cold control equivalent |

For Fire, and for directly comparable Cold control outcomes, the four
TowerFamily normal-yield results should remain in one bounded range. Their
initial outlier gate is `maximum / minimum <= 1.30` when both values are
positive and directly comparable.

Electric and Wind use the approved hit-frequency-sensitive exception. Their
cross-family accumulated normal yield is not subject to the `1.30` gate because
the result intentionally includes each TowerFamily's authored hit cadence and
Behaviour/AoE reaction opportunities. Acceptance instead requires:

- one shared FixedDamage potency per Element, independent from triggering Tower
  BasicDamage, Level, or Damage Bonus;
- deterministic Buff-instance reaction cooldown accounting, with high-frequency
  sources visibly capped and no recursive Elemental application;
- Wind target availability and no-target recovery to remain Recorder-visible;
- no family-and-Element combination to become an unexplained automatic answer
  in the final Core and stress fixtures;
- matching-source Overload to provide the dominant conditional reward above an
  otherwise identical one-source shared-reaction Build.

This exception accepts meaningful Build synergy, not unlimited scaling. The
goal is to bound high-frequency and AoE combinations through the reaction
cooldown while retaining their strategic identity.

For Phase G, define two separate cooperation measures:

```text
Matching Upgrade Dividend
    = Two-Source Overlap Elemental Package Yield
      - One-Source Overlap Control Elemental Package Yield

Coverage Dividend
    = Two-Source Overlap Elemental Package Yield
      - Two-Source Isolated Elemental Package Yield
```

The accepted matching Upgrade dividend is approximately `1.4-1.6 ERU`, and
Overload is its largest identifiable component for directly damaging Elements.
This keeps a
high-frequency or AoE reaction companion useful without allowing it to replace
the reward for completing two matching Elemental Upgrades.

Percentage uplift over Tower output remains a secondary gameplay guardrail. It
must not be used to tune a shared Element independently for Archer, Cannon,
Magic, or Drone. Ratios remain appropriate when comparing the same two Towers
and same two matching Upgrades in Overlap and Isolated layouts, because that
comparison measures build cooperation rather than Tower-family power.

## 5. Implementation Phases

Phases A-E preserve the evidence path that exposed and resolved the Task007A,
Task007B, and Task007C implementation boundaries. They remain historical
mechanism evidence. Phases F-H are the current numerical continuation and own
the final Task007 decision.

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
- The initial Phase B deferred multiple-stack contribution until schema-17
  evidence showed realistic matching-source coverage could not reach Overload
  reliably with one unit per application. That condition was met. Task007A then
  implemented configurable contribution and schema-18 diagnostics. Any current
  transaction defect reopens Task007A instead of being accepted as balance
  noise.

### Phase C - Element-Specific Reactions

- Use dense high-health fixtures for Burning, Cold/Frozen, Electric Elemental
  hit reactions, Wind secondary-target Elemental hit reactions, and WindVortex
  behavior.
- Review FixedBuff values separately from target count, radius, duration,
  cooldown, and movement-control value.

### Phase D - Behaviour Elemental Opportunities

- The original twelve schema-18 runs remain the evidence that motivated
  Task007B; they are not primary-only acceptance evidence.
- Task007B is complete under its explicitly named requirement-99 progression
  waiver. Its twelve package Records and focused Arcane Detonation Record accept
  the primary-only Elemental application topology.
- The twelve Behaviour packages have been rerun with the representative
  Elemental Upgrade. Candidate, eligible, and dispatched counts match the
  primary-only boundary; those schema-19 Records are retained as accepted
  topology evidence rather than repeated merely because Task007C changes normal
  Electric/Wind value execution.
- Confirm Behaviour damage and lifecycle remain functional while additional,
  continuation, area, bounce, persistent, and completion results have zero
  eligible and dispatched Elemental counts.
- Confirm Buff and reaction damage never creates recursive Elemental attempts.
- Task007C's shared Electric/Wind Elemental hit reactions and schema-20
  diagnostics passed the accepted implementation checkpoint and ordinary Phase
  7A gameplay smoke. Its remaining edge-transaction fixtures are deferred. Run
  only the focused value fixtures needed for current balance; Fire/Cold and
  Task007B application topology do not require a blanket rerun.

### Phase E - Eligibility, Presentation, And Final Regression

- Review Required Level, second-Element rejection, rejected-item atomicity,
  status icon and stack display, Protection pulse, persistent Buff VFX, and
  technical cleanup.
- Run the smallest final four-family regression after accepted value edits.

### Phase F - Cross-Family Normal-Value Calibration

- For each ElementType, compare the Archer, Cannon, Magic, and Drone Level 3
  Elemental variants under the same Map, measurement Wave, placement role, and
  no-Basic/no-Behaviour constraint.
- Retain one matching no-Element Level 3 control for each TowerFamily. Damage
  elements use Recorder FixedBuff signatures as primary value; Cold uses its
  absolute control dividend.
- Exclude FlameBurst, Frozen, LightningStrike, and WindVortex from normal-value
  totals. Report their counts and value separately instead of letting an
  accidental Overload conceal weak ordinary Buff value.
- Compare the four family results for one Element before moving to the next
  Element. Adjust shared normal parameters only when that Element is broadly
  high or low. Do not use stack contribution to repair normal potency. Apply
  the `1.30` cross-family gate only to Fire and directly comparable Cold
  outcomes; review Electric/Wind per-trigger potency, cooldown bounding, target
  availability, and Build synergy under their approved exception.
- Investigate a family-only outlier through application uptime, eligible hit
  opportunities, cooldown blocking, target availability, and route exposure.
  Change that family's contribution only when the failure is specifically
  Overload frequency.

### Phase G - Matching Upgrade And Coverage Cooperation

- After Phase F establishes each Element's normal-value contract, compare three
  conditions with the same two Level 3 Towers: one Elemental source plus one
  no-Element reaction companion at overlapping `P1/P2`; two matching Elemental
  sources at overlapping `P1/P2`; and two matching Elemental sources at
  isolated `P1/P3` coverage.
- Use one fixed reference TowerFamily across all four Elements so Element
  differences are not mixed with family cadence. Existing four-family
  contribution evidence remains valid unless the current Records expose an
  Overload-frequency outlier.
- The one-source Overlap control proves the ordinary shared-Buff and
  Electric/Wind reaction value available without the second matching Upgrade.
  The two-source Overlap treatment must produce the stronger high-order result
  through shared stacking and Overload, not merely through the companion
  Tower's ordinary hits or a hidden change in Tower damage.
- The accepted cooperation gate separates two questions. The second matching
  Upgrade must add approximately `1.0-1.6 ERU` over the one-source Overlap
  control, with Overload as the largest identifiable damaging component. The
  Overlap layout must also outperform the two-source Isolated reference and
  produce a clear absolute Overload-count increase. A universal `1.5x` ratio
  against Isolated is not required because Fire periodic uptime and Cold
  control retain legitimate value in both layouts.
- Review distinct Monsters overloaded, first-application-to-Overload timing,
  natural expiry, Protection entry, and re-entry so repeated farming of one
  Monster cannot masquerade as cooperation.

### Phase H - Final Core Regression And Stage Handoff

- After accepted Element and Overload parameter changes, run one final Core
  gameplay regression for each TowerFamily with normal Basic, Behaviour, and
  Elemental content.
- Confirm the fixed Elemental package remains visible without restoring
  Behaviour-origin applications or source-Tower damage scaling.
- Record any retained geometry-dependent exception explicitly.
- Hand the accepted package to the later Stage Tasks. MonsterDefinition,
  MonsterWaveDefinition, Map, and pressure tuning may make matching Elemental
  cooperation strategically necessary in a late Stage, but may not compensate
  for a failed Task007 normal-value or cooperation contract.

## 6. Recorder Preparation

Task007 Phase A historically used Recorder schema `16`, and the first Phase B
evidence used schemas `17` through `19` as the upstream contracts evolved. All
fresh calibration Records after Task007C use schema `20`.

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

Schema `20` is sufficient for the current continuation. Fresh Records must use
its FixedBuff signatures, Buff application and stack-unit accounting, natural
expiry and Overload distributions, per-source and per-Wave summaries,
Elemental opportunity provenance, and Elemental hit-reaction outcomes. Task007
does not require another Recorder schema merely to calculate ERU, cross-family
dispersion, or Overlap/Isolated cooperation.

Before Phase A, the Cold/Frozen authoring baseline is repaired to match the
System contract:

- Chilled clears its speed multiplier on both EnteredProtection and Removed;
- Frozen has no PeriodicTick interval because it owns no PeriodicTick behavior.

The six `Task005_PhaseA_LRoute_HP480_Archer_L3_*` reports recorded before
Task005 and Task006 were introduced remain pre-revision diagnostic evidence.
They identified the cooldown and damage-ownership problems but cannot satisfy
Task007 acceptance. Task007 begins with fresh `Task007_*` reports after both
upstream implementations are accepted.

## 7. Historical Phase A Configuration

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
- A control observes no Elemental Buff, Elemental hit-reaction, or Elemental
  FixedBuff damage.
- An Elemental run observes only the named source Tower instance, named
  Elemental Upgrade, and its shared Buff content.
- Source records contain a nonzero Tower instance ID and no unattributed
  application.
- All FixedBuff signature values equal their authored positive FixedDamage and
  remain independent from Tower Level BasicDamage and Damage Bonus.
- After Task007C, Electric and Wind Elemental hit-reaction signatures use their
  authored FixedDamage and remain independent from the triggering or
  contributing Tower's current resolved BasicDamage.
- Drone application attempts match only the primary Drone Burst opener direct
  boundary; Blast Rounds, additional Drones, and Final Dive remain application-
  ineligible but may trigger an already-active Electric/Wind reaction after
  Task007C.
- Reaction FixedBuff resolutions do not create additional Elemental application
  attempts.
- Resolution, Monster runtime, Wave attribution, Tower deployment, and damage
  diagnostic integrity all pass.

## 8. Current Phase F-H Test Configuration

### 8.1 Shared Schema-20 Fixture

| Setting | Value |
|---|---|
| Map | `Prefab_Map_Default_Multiple` |
| Player Max Health | `100` |
| Progression sentinel | Final requirement `99`; the three named progression-integrity fields remain a fixture-specific waiver |
| Wave 1 - setup | Delay `5s`; `22` x `Prefab_MonsterCandidate_Dragon`; HP `60`; Move Speed `0.25`; Spawn Interval `2.5s` |
| Wave 2 - measurement | Delay `90s`; `40` x `Prefab_MonsterCandidate_EvilMage`; HP `4800`; Move Speed `0.25`; Spawn Interval `2.5s` |
| Recorder | Schema `20`; measurement-Wave values and diagnostics are authoritative |
| Upgrade restriction | No Basic or Behaviour Upgrade during Phase F/G |

The high-health measurement Wave avoids a low-HP damage ceiling and keeps
ordinary Buff, hit-reaction, and Overload signatures visible. Every comparison
keeps Wave, Tower Level, family-specific placement role, Draft sequence, and
non-Element content fixed. Do not use total build percentage uplift as the
primary normal-value decision.

### 8.2 Phase F Matrix

Each run contains exactly one Level 3 Tower at its family P1. An Element run
owns exactly one named Elemental Upgrade; its matching control owns no Upgrade.
Use requirements `[2, 2, 2, 99]` and five Fixed Draft steps. The fifth step is
the unreachable sentinel.

| TowerFamily | Control | Four treatments |
|---|---|---|
| Archer | `NoElement` | `BlazingArrows`, `FrostboundArrows`, `ChargedArrows`, `GaleArrows` |
| Cannon | `NoElement` | `BlazingShells`, `FrostboundShells`, `ChargedShells`, `GaleShells` |
| Magic | `NoElement` | `BlazingOrbs`, `FrostboundOrbs`, `ChargedOrbs`, `GaleOrbs` |
| Drone | `NoElement` | `BlazingDrones`, `FrostboundDrones`, `ChargedDrones`, `GaleDrones` |

Run Name format:

```text
Task007_PhaseF_NormalValue_StraightMultiple_HP4800_<Family>_L3_<ElementOrNoElement>_Schema20_<NN>
```

Review all four TowerFamilies for one Element together. For Fire, Electric, and
Wind, export ordinary and Overload FixedBuff signature totals separately. For
Cold, compare each treatment with its family control and report the absolute
Effective Damage, leaked-health, and battle-duration dividend. A run that
accidentally Overloads remains usable only when its normal and Overload value is
separable in Recorder evidence.

The first Fire screen identified one Drone-specific targeting confound before
Element values are changed: a Drone retained one valid target across successive
Bursts for its full Battery lifetime, concentrating its authored stack
contribution on a small set of Monsters. Task007 therefore adopts the Runtime
Combat System rule that a Burst does not voluntarily rotate away from a valid
target, while the next Burst reevaluates valid candidates through the authored
target-selection category after Inter-Burst Cooldown. The previous target
remains an allowed result. This correction does not change Burst cadence,
Battery, damage, or Elemental-opener eligibility. The schema-20 `01` Drone Fire
Record remains pre-change diagnostic evidence. The accepted post-change `_03`
rerun uses Burst reselection and Burning FixedDamage `10`. Values `11-12`
remain future retuning headroom rather than current authored candidates.

The four-family Cold screen accepts shared Chilled `moveSpeedMultiplier 0.6`.
The stronger `0.5` candidate remains future retuning headroom. `ActiveDuration
5s`, Frozen `ActiveDuration 5s`, MaximumStacks, and Protection remain
unchanged. Magic Frostbound Orbs contribution `2` is the accepted
family-specific Overload-frequency correction.

The Electrical and Wind Phase F screens approve the hit-frequency-sensitive
exception above. In measurement Wave 2, Electrical ordinary reactions produced
`1484 / 420 / 630 / 1260` FixedDamage and Wind produced
`2140 / 600 / 920 / 1800` for Archer / Cannon / Magic / Drone. Their successful
reaction counts were respectively `106 / 30 / 45 / 90` and
`107 / 30 / 46 / 90`; Wind recorded zero no-valid-target outcomes. The matching
family ordering and opportunity counts prove that Tower hit cadence, rather
than DamageScale or Wind spatial failure, owns the accumulated spread. No
shared Electric or Wind FixedDamage candidate is promoted from Phase F.

### 8.3 Phase G Matrix

Use Magic as the fixed reference family because the earlier Phase C evidence
establishes its dense-fixture behavior. Fire, Electric, and Wind contribution
remain `3`; Cold uses the accepted contribution `2` correction. Each run
contains two Level 3 Magic Towers and no Basic or Behaviour Upgrade.

- One-source Overlap control: P1 owns the named Elemental Upgrade; P2 owns no
  Elemental Upgrade. Both Towers overlap at P1/P2. P2 may trigger an existing
  Electric/Wind reaction but cannot contribute stacks.
- Two-source Overlap treatment: P1 and P2 both own the same named Elemental
  Upgrade.
- Two-source Isolated reference: P1 and P3 both own the same named Elemental
  Upgrade.
- Requirements: `[2, 2, 2, 2, 2, 2, 2, 99]`;
- Fixed Draft steps: `9`, including one unreachable sentinel.

Run Name formats:

```text
Task007_PhaseG_Cooperation_StraightMultiple_HP4800_Magic_L3x2_<Upgrade>_1Source_P1P2_Control_Schema20_<NN>
Task007_PhaseG_Cooperation_StraightMultiple_HP4800_Magic_L3x2_<Upgrade>_2Sources_P1P2_Overlap_Schema20_<NN>
Task007_PhaseG_Cooperation_StraightMultiple_HP4800_Magic_L3x2_<Upgrade>_2Sources_P1P3_IsolatedReference_Schema20_<NN>
```

The accepted matrix contains twelve Records. For Electric and Wind, the one-source
control is the direct guard against a high-frequency ordinary-reaction Build
replacing matching-source Overload. An additional Archer high-frequency/AoE
stress pair is conditional, not automatic: add it only when the Magic results
leave the Contract-A upper bound unresolved. The accepted Magic results did not
require that additional stress pair or a four-family cooperation rerun.

### 8.4 Phase H Minimum Regression

After the final value edit, run four schema-20 Core fixtures, one per
TowerFamily. Each includes one accepted Basic, one Behaviour, and one Elemental
Upgrade under normal production values. Rotate ElementTypes so the four Records
cover Fire, Cold, Electric, and Wind once. These Records verify composition and
presentation; they do not replace Phase F normal-value or Phase G cooperation
evidence.

### 8.5 Locked CombatMathV2 Elemental Values

The following values are accepted Task007 production baselines. Unity assets
remain executable authority; this table is the reviewable calibration record.

| Shared Buff value | Accepted value |
|---|---:|
| Burning Active Duration / Tick Interval / Periodic FixedDamage | `5s / 1s / 10` |
| FlameBurst radius / FixedDamage | `1 / 100` |
| Chilled Active Duration / Move Speed Multiplier | `5s / 0.6` |
| Frozen Active Duration | `5s` |
| Electrified Active Duration / reaction cooldown / reaction FixedDamage | `5s / 0.5s / 14` |
| Overcharged radius / target count / per-target FixedDamage | `2 / 4 / 100` |
| Windcut Active Duration / reaction cooldown / radius / target count / FixedDamage | `5s / 0.5s / 1 / 1 / 20` |
| WindVortex Lifetime / Tick Interval / Tick FixedDamage / Damage Radius | `15s / 0.6s / 5 / 1` |
| Elemental Maximum Stacks / Source Apply Cooldown / Overload Protection | `10 / 0.5s / 10s` |

Wind's no-valid-secondary-target result does not consume reaction cooldown.
Frozen remains non-Elemental and non-stackable; its unrelated compatibility
fields do not participate in Elemental stacking.

| TowerFamily | Fire | Cold | Electric | Wind |
|---|---:|---:|---:|---:|
| Archer | `1` | `1` | `1` | `1` |
| Cannon | `4` | `4` | `4` | `4` |
| Magic | `3` | `2` | `3` | `3` |
| Drone | `2` | `2` | `2` | `2` |

These contribution values change only the units requested by one authorized
baseline-primary application. They do not scale normal Buff potency or
Overload damage.

### 8.6 Accepted Phase F-G Results

Phase F accepted the following ordinary damaging yields for Archer / Cannon /
Magic / Drone:

| Element | Ordinary FixedBuff yield | Decision |
|---|---:|---|
| Fire | `2310 / 1320 / 2240 / 2270` | Accept `10` periodic damage; retain Cannon as a named low-cadence coverage exception |
| Electric | `1484 / 420 / 630 / 1260` | Accept FixedDamage `14` and the hit-frequency-sensitive family exception |
| Wind | `2140 / 600 / 920 / 1800` | Accept FixedDamage `20` and the hit-frequency-sensitive family exception |

Cold accepts movement control rather than one universal FixedDamage total. The
strict same-delay Magic comparison increased measurement-Wave Effective Damage
from `4182` to `6222`. Archer, Cannon, and Drone Cold treatments used the later
`90s` Wave delay while their historical no-Element controls used `25s`; those
runs remain mechanism and family-response evidence, not strict quantitative
denominators. This named fixture difference does not change the accepted `0.6`
movement multiplier.

Phase G produced:

| Element | One-source control | Two-source overlap | Two-source isolated | Matching Upgrade Dividend | Overlap overloads | Isolated overloads |
|---|---:|---:|---:|---:|---:|---:|
| Fire fixed package | `2100` | `6000` | `4470` | `3900` (`1.63 ERU`) | `8` | `0` |
| Cold Wave-2 ED | `13464` | `16932` | `12852` | `3468` (`1.45 ERU` equivalent) | `15` | `0` |
| Electric fixed package | `1148` | `4500` | `1260` | `3352` (`1.40 ERU`) | `8` | `0` |
| Wind fixed package | `1740` | `5185` | `1720` | `3445` (`1.44 ERU`) | `10` | `0` |

The median matching dividend is approximately `3457`, or `1.44 ERU`. Overload
provided approximately `62%` of Fire's, `93%` of Electric's, and `89%` of
Wind's incremental dividend. Cold's Frozen control value is not converted into
a fictitious FixedDamage split. Every accepted Overlap run produced Overload;
every accepted Isolated reference produced none. This passes the intended
matching-build reward even though Fire and Cold retain useful ordinary value in
the isolated layout.

### 8.7 Phase H Core Regression

The four final Core Records completed with the named Basic, Behaviour, and
Elemental packages:

| TowerFamily / Element | Wave-2 Effective Damage | Elemental evidence |
|---|---:|---|
| Archer / Electric | `17807` | `205` reactions for `2870`; one Overload resolved three `100`-damage targets |
| Cannon / Fire | `15451` | `1680` periodic damage; one Overload resolved two `100`-damage targets |
| Magic / Cold | `9362` | `62` Chilled application attempts; control remained movement/Frozen-owned |
| Drone / Wind | `15054` | `153` reactions for `3060`; `350` opportunities correctly cooldown-blocked |

Behaviour damage created Electric/Wind reaction opportunities but did not
create additional Elemental applications. FixedBuff values remained independent
from Tower BasicDamage. All schema-20 combat, Monster, Wave, deployment, damage,
Elemental opportunity, hit-reaction, Buff, stack-unit, and Wave-attribution
integrity checks passed.

## 9. Completion Evidence And Handoff

- Unity authoring validation passed with `75` assets checked.
- Phase F locked shared normal values and named the retained Cannon Fire and
  Electric/Wind hit-frequency exceptions.
- Phase G accepted all four matching-source cooperation packages.
- Phase H accepted four final Core composition regressions.
- The fixture's unreachable final requirement `99` intentionally leaves
  `levelUpCountMatches`, `levelUpResolutionNodesMatch`, and
  `finalPlayerLevelMatches` false. This progression-only waiver does not apply
  to combat or Elemental diagnostics.
- Task007C's remaining edge-transaction smoke remains explicitly deferred. It
  is not reported as passed and does not block the accepted Task007 numerical
  baseline.

Task010 and later Stage Tasks may tune Monster, Wave, route, Draft opportunity,
and pressure values against this package. They may not repair a Stage result by
silently changing the accepted Tower Level, Basic, Behaviour, Elemental normal,
or matching-source cooperation contracts. Reopening Task007 requires a named
regression caused by a later gameplay-contract or authored-value change.
