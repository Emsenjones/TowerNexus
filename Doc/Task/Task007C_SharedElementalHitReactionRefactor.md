# Task007C - Shared Elemental Hit Reaction Refactor

Status: Implementation checkpoint accepted for Task007 continuation on
2026-08-26; Unity Editor authoring validation and Phase 7A schema-20 gameplay
smoke passed, while the remaining edge-transaction smoke is explicitly
deferred and may be resumed later

Depends on: Task007A Elemental Stack Contribution Refactor implementation
checkpoint; Task007B Primary Elemental Opportunity Boundary Refactor static
implementation and thirteen schema-19 evidence Records; Task007 Phase C and
Phase E evidence

Blocks: Task007 final Elemental and Buff value acceptance and Task010-Task017

## 1. Goal

Replace Electric and Wind's contributor-owned `StackApplied` TowerScaled damage
with source-independent shared Buff reactions that occur when a Tower-owned
damage result hits a Monster carrying the corresponding active Elemental Buff.

The new model separates three tuning axes:

```text
Normal Elemental value
    -> shared Buff behavior and FixedBuff reaction values
Overload frequency
    -> primary-only Elemental application and authored stack contribution
Overload potency
    -> source-independent Overload Effect values
```

Fire remains persistent FixedBuff periodic damage. Cold remains a persistent
movement-speed modifier. Electric becomes a vulnerable state that adds fixed
damage when eligible Tower-owned damage hits its owner. Wind becomes a reactive
state that uses an eligible hit on its owner to damage at most one other nearby
Monster for a fixed amount.

This Task does not restore Behaviour-origin Elemental application. Task007B's
primary-only application boundary remains effective: Behaviour results may use
an existing Electric or Wind Buff, but they never create that Buff, add stack
units, refresh Active Duration, advance Overload, or consume Source Apply
Cooldown.

## 2. Evidence And Decision

Task007B successfully removed Behaviour-produced Elemental applications and
made application provenance auditable. The later Task007 Phase E Electric Core
screen then showed that correct application topology did not by itself provide
a stable Elemental value curve:

| TowerFamily | Pair control ED | One-source ED / uplift / Overloads | Two-source ED / uplift / Overloads |
|---|---:|---:|---:|
| Archer | `29257` | `30746 / +5.1% / 1` | `33312 / +13.9% / 4` |
| Cannon | `27509` | `27932 / +1.5% / 1` | `29237 / +6.3% / 4` |
| Magic | `15509` | `15642 / +0.9% / 0` | `20713 / +33.6% / 7` |
| Drone | `24442` | `26058 / +6.6% / 6` | `27609 / +13.0% / 10` |

The old Electric and Wind normal value was coupled to both the number of
successful stack reapplications and the exact contributor Tower's current
resolved BasicDamage. Changing a contribution value therefore changed Overload
frequency while attack cadence and Tower BasicDamage also changed normal
reaction value. One global DamageScale could not create a stable cross-family
baseline.

The approved response is to remove damage from Electric and Wind
`StackApplied`. Stack contribution remains state magnitude used only to advance
the shared MaximumStacks threshold. Electric and Wind normal reaction damage
becomes shared-state FixedBuff content with an authored per-Buff-instance
cooldown.

## 3. Entry Gate And Workspace Protection

Task007C begins from a dirty but intentional upstream workspace containing
Task007A and Task007B runtime, Recorder, System-doc, Task-doc, scene, fixture,
balance-asset, and gameplay-Record changes. No existing modification or
untracked file may be normalized, reverted, overwritten, or silently absorbed.

Before Task007C System-doc, runtime, Recorder, or asset edits:

- store a complete `git status --short`, binary-safe tracked diff, HEAD
  identity, and hashes for every modified or untracked file in a task-specific
  temporary directory outside the repository;
- treat the complete existing worktree as the protected upstream baseline;
- run Runtime and Editor baseline builds and pause implementation if either
  fails;
- reconcile Task007B's twelve family/package schema-19 Records and focused
  Arcane Detonation Record without claiming blanket integrity success: all
  thirteen currently pass combat, damage, Elemental-opportunity, Buff, and
  stack-unit accounting, while `levelUpCountMatches`,
  `levelUpResolutionNodesMatch`, and `finalPlayerLevelMatches` are false because
  the fixture intentionally uses requirement `99`; either record that named
  fixture-specific progression waiver or replace the evidence with fresh
  all-integrity-true runs;
- add the Unity Editor authoring evidence to Task007B using the exact menu and
  output, `Tools/Tower Nexus/Validate Combat Damage Authoring` and `Combat
  damage authoring validation passed: 75 assets checked.`; recover and record
  the execution date from the Unity log or mark the date unavailable rather
  than inventing one;
- update Task007B status and downstream replacement wording only after its
  executed evidence and named waiver are synchronized; until Task007C runtime
  acceptance, describe Task007C as an approved pending downstream contract,
  not the current implemented contract;
- preserve Task005, Task007A, and Task007B historical goals, checkpoints, and
  evidence; add downstream supersession notes instead of rewriting history;
- freeze the provisional positive Electric and Wind reaction FixedDamage,
  reaction radius/target-count behavior, Wind target-selection and no-target
  VFX behavior, and reaction cooldown values before migrating assets;
- freeze all new schema-20 Record names before Play Mode and never overwrite
  existing schema-17, schema-18, or schema-19 Records.

The closeout comparison reports only Task007C-owned changes relative to this
external baseline. Upstream files outside Task007C's approved scope must remain
hash-identical. Files intentionally overlapped by Task007C are reviewed against
their saved baseline blobs and binary-safe diff; they are not incorrectly
required to retain the baseline hash.

## 4. Stable Ownership Model

### 4.1 Elemental Application

Elemental application remains owned by Task007B's reviewed baseline-primary
opportunity. One eligible source Tower with one equipped Elemental Upgrade may
submit at most one target-specific Buff request carrying that Upgrade's exact
positive `ElementalStackContribution`.

Behaviour-added or Behaviour-extended results still submit no Elemental Buff
request and carry no stack units. This rule is unchanged by Task007C.

### 4.2 Buff State And Overload

Buff System continues to own Active Duration, stack mutation, Source Apply
Cooldown, MaximumStacks, Overload, Protection, expiry, and re-entry. Multiple
matching Elemental Towers contribute to one shared Buff instance. There is no
minimum contributor count.

`ElementalStackContribution` controls only stack magnitude and Overload
frequency. It never multiplies Electric or Wind normal reaction damage and does
not execute a reaction once per contributed unit.

### 4.3 Tower-Owned Hit Reaction

A successful Tower-owned damage application creates one target-specific hit
fact. If the target is eligible for an active Electric or Wind reaction, that
fact may request the corresponding shared Buff reaction.

The Tower that produced the hit is diagnostic context only for the reaction. It
does not become the Buff contributor, does not select a stack cooldown entry,
does not need an Elemental Upgrade, and does not supply BasicDamage or
DamageScale to the reaction's FixedBuff damage.

## 5. Eligible And Ineligible Damage

All successful Tower-owned damage is eligible to use an active Electric or Wind
Buff. This includes current direct and TowerScaled Behaviour result families:

- Primary direct damage;
- Additional direct damage;
- Bounce direct damage;
- Final Dive direct damage;
- TowerScaled Behaviour Effect damage, including area, persistent-tick, and
  completion results such as explosions, Arcane Field, Arcane Detonation, Blast
  Rounds, and Final Dive explosion.

Eligibility requires one positive Tower-owned damage application against one
Monster that remains gameplay-targetable at the reaction boundary. A resolution
that finds no valid target, applies no damage, or resolves the Monster before
the reaction boundary creates no reaction.

The following are always ineligible:

- FixedBuff damage;
- Burning ticks;
- Electric or Wind hit-reaction damage;
- FlameBurst, LightningStrike, Frozen, WindVortex, or other Overload results;
- persistent Elemental-reaction damage;
- technical, presentation, diagnostic, or rejected damage observations.

FixedBuff damage never recursively triggers Electric or Wind. One reaction may
not trigger itself, the other hit reaction, another Buff lifecycle Effect, or
an Elemental application.

Gameplay authority must not subscribe to Recorder or diagnostic observations.
The Tower-owned damage producer forwards one explicit target-specific hit fact
through the gameplay transaction; diagnostics observe the committed outcome.

## 6. Reactive Buff Authoring

BuffDefinition receives optional hit-reaction authoring:

- one `TowerHitReceived` Effect binding;
- one serialized positive `towerHitReactionCooldown` when that binding exists,
  exposed through `TowerHitReactionCooldown` without silently repairing an
  invalid authored value;
- no cooldown state or reaction behavior when the binding is absent.

`TowerHitReceived` is appended as a stable Buff event identity without
renumbering existing serialized enum values. It is a shared-state reaction
event, not a contributor-owned stack event. Version 1 deliberately accepts only
two closed authoring shapes:

- Electric is a same-owner single-target Effect containing exactly one approved
  `DealDamage` action using `FixedBuff` and no other gameplay action;
- Wind is one radius multi-target wrapper that excludes the trigger owner,
  selects at most one valid secondary target using the existing random
  selection policy, and invokes a child Effect containing exactly one approved
  single-target `DealDamage` action using `FixedBuff` and no other gameplay
  action.

Execution/presentation metadata may remain on these approved Effects, but the
action graph may not contain `ApplyBuff`, movement, `SpawnWindVortex`, another
multi-target branch, or any action capable of authorizing Elemental application
or a second gameplay result. This closed shape makes committed reaction damage,
cooldown consumption, and Recorder reconciliation unambiguous.

Validation rejects:

- a reaction binding without a positive finite cooldown;
- a positive reaction cooldown without a reaction binding;
- a reaction binding on content other than a stackable Electric or Wind Buff in
  this first implementation;
- TowerScaled damage anywhere under a reaction binding;
- an action graph outside the two approved version-1 shapes, including an
  Effect that can apply an Elemental Buff, move a target, spawn persistent
  content, or recursively authorize Elemental application;
- Electric or Wind content that retains both old StackApplied damage and the
  new hit reaction.

Fire and Cold do not receive hit-reaction bindings in this Task.

## 7. Per-Buff-Instance Cooldown

Reaction cooldown is mutable state owned by the Monster's Buff instance:

- it is scoped by owner Monster and BuffDefinition;
- every Tower shares the same cooldown on that instance;
- it is not per source Tower;
- it is independent from Source Apply Cooldown;
- it is independent from Overload Protection Duration;
- it runs only while the Buff is in Stacking phase;
- entering Protection clears or disables reaction readiness;
- removal destroys the cooldown with the Buff instance;
- a new Stacking cycle begins reaction-ready.

An eligible hit during cooldown causes no Effect execution and does not extend,
restart, or otherwise mutate the cooldown.

Cooldown consumption is based on committed reaction damage, not generic Effect
execution or presentation. Electric consumes cooldown only when its extra
damage successfully applies. Wind consumes cooldown only when it resolves and
successfully damages one eligible nearby Monster. If Wind has no valid
secondary target, it records `NoValidReactionTarget`, does not execute damage,
does not consume cooldown, and does not play the reaction execution VFX; a later
hit may try again immediately. Suppressing no-target VFX is an intentional
Task007C rule so a ready cooldown cannot generate misleading presentation on
every hit while no secondary target exists.

## 8. Atomic Hit Transaction And Deferred Overload

Current `MonsterBuffRuntime.ApplyBuffWithOutcome` resolves Overload and
Protection before returning. Task007C must replace that timing with an explicit
completion protocol; merely adding a reaction after the existing call would
violate the approved ordering.

One Tower-owned target hit follows one semantic transaction:

```text
Commit positive Tower-owned damage
    -> stop if the owner Monster is no longer gameplay-targetable
    -> snapshot reactive Buff opportunities already present for this hit
    -> execute the hit's one authorized Elemental application when applicable
    -> commit stack mutation and Applied/StackApplied lifecycle work
    -> return any newly earned single-use pending-Overload completion
    -> include a newly Applied Electric or Wind Buff in this hit's reaction set
    -> evaluate Electric, then Wind
    -> commit at most one FixedBuff reaction for each Buff
    -> finalize the pending Overload exactly once
    -> enter Protection once when the owner still has a live Buff instance
    -> publish state, presentation, and diagnostic observations
```

The pending completion is gameplay state, not a Recorder token. It snapshots
the immutable BuffDefinition, Overload Effect, contributor context, threshold
position, Buff instance/cycle identity, and whether the threshold came from
first application or reapplication. It is consumed exactly once by
`FinalizeOverload`; a second finalize is rejected. A committed threshold may
never be returned as an ordinary completed application while silently leaving
the Buff at MaximumStacks in Stacking phase.

The default non-hit Buff path finalizes an earned completion immediately, which
preserves existing lifecycle semantics outside the explicit Tower-hit
transaction. The Tower-hit path requests deferred completion and owns it until
the reaction step finishes. Before stack mutation, a failed application may
cancel without a completion. After threshold mutation commits, exceptions and
early returns must execute deterministic finalization in cleanup; they may not
abandon or transfer the completion.

Stable event rules:

- the same hit that first successfully applies Electric or Wind also evaluates
  one normal reaction when its reaction target is valid;
- a hit whose stack application reaches MaximumStacks may execute one normal
  reaction and one Overload, in that order, before Protection becomes the
  externally observable post-transaction state;
- first-application threshold crossing runs `Applied`, then the hit reaction,
  then one Overload; it never runs `StackApplied`;
- reapplication threshold crossing commits stack units, runs the logical
  `StackApplied` event without Electric or Wind damage, then the hit reaction,
  then one Overload;
- a threshold Overload is earned by the committed stack mutation. If the normal
  Electric reaction kills the owner, the pending Overload still executes once
  from its captured threshold position and context; the dead owner does not
  enter Protection and no stale max-stack Buff instance remains;
- when Electric and Wind coexist, opportunities are evaluated in stable
  ElementType order: Electric before Wind. If Electric resolves the owner,
  Wind is recorded as invalidated with
  `OwnerResolvedByEarlierElementalReaction` and does not execute;
- one hit produces at most one Electric reaction and at most one Wind reaction,
  regardless of action count, stack units, or source Upgrade;
- multiple targets damaged by one area Effect evaluate their own Buff instance
  and cooldown independently;
- reaction damage is not a Tower-owned hit and cannot recurse;
- Buff mutation, reaction damage, Overload, Protection or terminal cleanup all
  commit before UI, VFX, Recorder, or other read-only publication.

## 9. Element Content

### 9.1 Fire

Burning PeriodicTick and FlameBurst remain source-independent FixedBuff content.
No Fire application, tick, radius, FixedDamage, stack contribution, or Overload
value changes are owned by Task007C.

### 9.2 Cold

Cold slow, Frozen, duration, stack contribution, and Protection behavior remain
unchanged. Neither slow nor Frozen damage/control creates a hit reaction.

### 9.3 Electric

While Electrified is in Stacking phase, every eligible Tower-owned damage hit
may deal one authored FixedBuff amount to the Electrified owner when the shared
per-instance reaction cooldown is ready.

The same hit may both apply Electrified and trigger this normal reaction.
Electric reaction damage does not add stacks, refresh the Buff, read source
Tower stats, apply Electrified, or trigger another reaction.

LightningStrike Overload remains source-independent FixedBuff content and keeps
its existing targeting contract unless Task007 later changes its balance value.

### 9.4 Wind

While Windcut is in Stacking phase, every eligible Tower-owned damage hit may
search around the Windcut owner, exclude that owner, select at most one other
valid Monster using the existing random candidate selection policy, and deal
one authored FixedBuff amount when cooldown is ready.

The same hit may both apply Windcut and trigger this normal reaction. If no
secondary target exists, no reaction damage or reaction execution VFX occurs
and cooldown remains ready. Wind reaction damage does not add stacks, refresh
Windcut, read source Tower stats, apply Windcut, or trigger another reaction.

WindVortex Overload remains source-independent FixedBuff content and keeps its
current lifetime, movement, tick, radius, and damage contract unless Task007
later changes balance values.

## 10. Asset Migration

The Electric and Wind migration is a clean downstream replacement:

- remove Electrified and Windcut `StackApplied` damage bindings;
- retain logical `StackApplied` lifecycle observations and stack mutation;
- create or migrate distinct Electric and Wind `TowerHitReceived` Effects using
  FixedBuff damage;
- preserve Wind's trigger-owner exclusion and maximum secondary target count of
  one;
- preserve Electric Overload, WindVortex, Buff identities, status icons,
  persistent VFX, GUID reference integrity, MaximumStacks, ActiveDuration,
  Source Apply Cooldown, and Overload Protection unless a separately approved
  Task007 balance edit changes them;
- do not leave compatibility execution of the old TowerScaled StackApplied
  damage;
- preserve `.asset` and `.meta` GUIDs when an existing Effect asset is renamed
  or repurposed, and update its serialized identity intentionally.

Task007C accepts only positive implementation/smoke anchors for reaction
FixedDamage and cooldown. Their final balance acceptance belongs to Task007.

## 11. Recorder Schema 20

CombatBalanceRunRecorder advances from schema `19` to schema `20`. Schema 20
preserves schema-19 Elemental application provenance, Buff stack-unit
accounting, damage signatures, Drone diagnostics, and all existing integrity
fields.

Schema 20 adds Electric and Wind hit-reaction diagnostics at four scopes:

- overall per BuffDefinition;
- overall per triggering source Tower and damage-source identity;
- per Wave;
- per Wave with nested source Tower and damage-source identity.

The accounting unit is exactly one `(hit transaction, target Monster,
BuffDefinition)` tuple. One hit on a Monster carrying both Electrified and
Windcut therefore produces two independently accountable opportunities. A Buff
successfully Applied by the same hit is inserted into that hit's observed set
and is counted; it is not hidden as a pre-existing-state exception.

Each scope records at minimum:

- observed reaction opportunities;
- evaluated reaction opportunities;
- reactions with committed damage successfully triggered;
- opportunities blocked by reaction cooldown;
- Wind opportunities with no valid secondary target;
- opportunities invalidated before committed reaction damage, split by the
  closed reason set `OwnerResolvedByEarlierElementalReaction`,
  `BuffInstanceOrCycleChanged`, `ReactionTargetInvalidBeforeCommit`, and
  `ReactionCommitRejected`;
- successful reaction damage target count;
- total reaction FixedBuff damage;
- `reactionGapSampleCount` plus minimum, average, and maximum successful
  reaction gap;
- triggering Tower instance, TowerFamily, damage-source type, EffectDefinition
  and action ordinal when applicable;
- whether the trigger source owns the same Elemental Upgrade, another Element,
  or no Elemental Upgrade, for diagnostics only.

`observedReactionOpportunities` is recorded after the hit target is known and
the same-hit application has committed. Every observed tuple is evaluated
exactly once; cooldown and Wind target checks are evaluation outcomes, not a
precondition hidden inside a field named eligible. `triggeredReactions` is
recorded only from a new target-specific committed reaction outcome after
FixedBuff damage succeeds. It is not inferred from the existing aggregate
damage observation.

The top-level boolean `elementalHitReactionDiagnosticsConsistent` validates
every exported scope, including every source Tower, Elemental Upgrade,
damage-source identity, BuffDefinition, stacking-cycle identity, and Wave
partition. At minimum:

```text
observedReactionOpportunities = evaluatedReactionOpportunities

evaluatedReactionOpportunities
    = triggeredReactions
    + blockedByReactionCooldown
    + noValidReactionTarget
    + invalidatedReactionOpportunities

triggeredReactions <= observedReactionOpportunities
successfulReactionDamageTargets == triggeredReactions for Electric v1
successfulReactionDamageTargets == triggeredReactions for Wind v1
```

An unknown invalidation reason, a tuple evaluated twice, a missing same-hit
new-application tuple, or a technical cleanup that leaves an observed tuple
without an outcome makes the consistency flag false.

A reaction gap compares only consecutive successful triggers for the same Buff
instance during the same Stacking cycle. The first success in a cycle produces
no gap sample. Protection, removal, expiry, and re-entry end that sequence; a
new cycle starts with zero gap samples. Scope aggregation derives min/average/
max only from real samples and exports `reactionGapSampleCount = 0` when no
pair exists.

Electric and Wind FixedBuff damage totals must reconcile with the corresponding
Effect damage signatures. FixedBuff and reaction damage must record zero
reaction opportunities. Behaviour-origin reaction triggers may be positive,
but Behaviour-origin Elemental application eligible/dispatched counts remain
zero and schema-19 application reconciliation remains true.

Recorder observations remain read-only and exception-isolated. They consume the
new target-specific committed reaction outcome and never decide damage
eligibility, cooldown, target selection, Buff state, or Effect execution.

## 12. Documentation Synchronization

Before runtime implementation, synchronize the stable target contract in:

- `Doc/System/00_ProjectOverview.md`: shared Elemental state summary and
  downstream replacement of StackApplied TowerScaled normal value;
- `Doc/System/10_TowerFrameworkSystem.md`: layer-level responsibility summary;
- `Doc/System/11_TowerRuntimeCombatSystem.md`: explicit Tower-owned hit fact and
  separation from primary-only application;
- `Doc/System/12_ProjectileSystem.md`: projectile damage result forwarding,
  without projectile-owned Buff authority;
- `Doc/System/13_TowerUpgradeSystem.md`: contribution controls only stack
  magnitude and Overload frequency;
- `Doc/System/14_EffectSystem.md`: TowerHitReceived FixedBuff execution,
  non-recursion, and Wind target-result semantics;
- `Doc/System/15_BuffSystem.md`: reaction authoring, per-instance cooldown,
  transaction ordering, Protection, and Electric/Wind content.

Also synchronize:

- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`, replacing the current
  effective Electric/Wind `StackApplied` TowerScaled value statement while
  preserving the historical Task005/Task007A rationale;
- Task005 with a visible downstream supersession note that preserves its
  historical accepted implementation;
- Task007A with preserved contribution history and current contribution-only
  responsibility;
- Task007B with completed schema-19 evidence and the distinction between
  primary-only Elemental application and broader Tower-owned hit reactions;
- Task007 with the new normal-Buff, Overload-frequency, and Overload-potency
  calibration model;
- this Task and `Doc/Task/README.md` dependency order.

Run a terminology drift scan across System and Balance documentation. In
particular, `Doc/System/07_MonsterSystem.md` already uses "hit reaction" for
non-blocking presentation feedback. The new gameplay mechanism must be called
**Elemental hit reaction** everywhere; the existing Monster presentation term
must remain distinct and need not change unless a clarifying qualifier is
required.

System documents describe cross-engine ownership and invariants. Concrete C#
file lists, migration paths, Recorder field implementation, and smoke fixtures
remain in Task007C.

## 13. Implementation Checkpoints

### Phase 0 - Baseline, Status, And Value Gate

- capture the complete external worktree baseline;
- pass Runtime and Editor baseline builds;
- classify baseline files as Task007C-untouched/hash-identical or approved
  overlap/reviewed against saved baseline blobs and diff;
- synchronize Task007B's thirteen runtime Records, the named requirement-99
  progression waiver or replacement evidence, and the dated-or-date-unavailable
  75-asset Unity authoring validation without claiming blanket integrity;
- retain "pending downstream replacement" wording until Task007C runtime
  acceptance changes it to the current effective contract;
- freeze provisional Electric/Wind reaction FixedDamage and cooldown anchors;
- freeze Wind random selection and the approved no-target rule: no damage, no
  cooldown consumption, and no reaction execution VFX;
- freeze schema-20 field names, equations, and fresh Record names.

### Phase 1 - Stable Document Contracts

- synchronize the System and Task documents listed above;
- synchronize `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md` and scan
  `Doc/System/07_MonsterSystem.md` plus related docs for ambiguous unqualified
  "hit reaction" terminology;
- preserve historical Task conclusions and mark precise downstream replacement;
- keep balance anchors provisional and Task007-owned.

### Phase 2 - Reaction Authoring And Runtime State

- append the reaction event identity without changing serialized values;
- add optional BuffDefinition reaction Effect and cooldown authoring;
- add per-instance cooldown state and validation;
- expose no source-Tower gameplay scaling for reaction damage.

### Phase 3 - Explicit Hit Transaction

- forward target-specific successful Tower-owned damage facts from every direct
  and Effect damage producer;
- split stack mutation/lifecycle commit from Overload/Protection completion;
- return and propagate an immutable single-use pending-Overload completion from
  Buff runtime through the Elemental application/Effect path to the Tower-hit
  transaction;
- finalize immediately for non-hit application paths and after Elemental hit
  reactions for Tower-hit paths, with deterministic cleanup on every exception
  and early return;
- preserve the earned Overload when reaction damage kills the owner, while
  skipping Protection and removing stale Buff state on the resolved owner;
- evaluate coexisting Electric then Wind and record a Wind invalidation if the
  earlier Electric reaction resolves the owner;
- permit all reviewed Tower-owned damage identities;
- exclude every FixedBuff and reaction result by construction;
- avoid diagnostic events as gameplay transport.

### Phase 4 - Electric And Wind Migration

- remove old StackApplied TowerScaled damage bindings;
- author Electric same-target and Wind one-secondary-target FixedBuff reactions;
- preserve Overload content and protected balance assets outside this slice;
- verify asset and GUID reference integrity.

### Phase 5 - Recorder Schema 20

- implement target-specific committed reaction outcomes, the four diagnostic
  scopes, closed invalidation reasons, stacking-cycle gap samples, and the
  top-level consistency flag;
- validate `observed = evaluated` and the complete evaluated outcome partition
  at every nested scope;
- preserve and revalidate every schema-19 field and integrity equation;
- reconcile reaction counts with FixedBuff damage signatures and application
  provenance.

### Phase 6 - Static And Editor Validation

- build Runtime and Editor projects serially;
- run scoped `git diff --check`;
- validate schema/JSON mapping, enum serialization, producer coverage, effect
  references, and protected-file hashes;
- run `Tools/Tower Nexus/Validate Combat Damage Authoring` separately in Unity.

### Phase 7 - Focused Play Mode Smoke

- prove same-hit first application reaction;
- prove first-application threshold crossing produces Applied, reaction, one
  finalized Overload, and no StackApplied;
- prove reapplication threshold crossing produces one StackApplied, reaction,
  one finalized Overload, and Protection;
- prove a reaction that kills its owner still finalizes an earned Overload once
  and leaves no Protection or stale max-stack instance;
- prove Protection expiry/re-entry creates a reaction-ready new Stacking cycle;
- prove Electric and Wind coexistence executes Electric first and accounts for
  Wind invalidation when Electric resolves the owner;
- prove primary, additional, bounce, direct, area, persistent, and completion
  Tower-owned results can trigger without applying stacks;
- prove reaction cooldown blocks high-frequency Arcane Field ticks;
- prove FixedBuff, reaction, Overload, and WindVortex damage cannot recurse;
- prove Electric same-target and Wind secondary-target FixedDamage;
- prove Wind no-target does not consume cooldown and the next valid opportunity
  can trigger, with no misleading no-target reaction VFX;
- compare fixtures with equal hit/reaction counts but different Tower
  BasicDamage and prove identical FixedBuff reaction damage;
- prove a Tower without the matching Elemental Upgrade can use an existing Buff;
- pass all relevant schema-19 combat/application/Buff integrity and schema-20
  integrity together; any requirement-99 progression exception must be the
  exact named fixture-specific waiver recorded in Phase 0, never summarized as
  "all integrity passed."

### Phase 8 - Task007 Continuation

Task007 then calibrates:

- single-Core `0 Element -> 1 Element` normal value;
- pair `1 matching source -> 2 matching sources` Overload reward;
- Fire, Cold, Electric, and Wind normal-value parity in controlled fixtures;
- per-family contribution values and Overload frequency;
- Overload potency and Protection only after normal reaction values pass.

Task007C implementation acceptance does not itself accept final balance values.

### Phase 9 - Completion And Evidence

- record baseline/final Runtime and Editor results separately;
- record Unity authoring validation separately;
- list exact schema-20 smoke Records and Recorder-derived evidence;
- list manual, waived, and unrun checks honestly;
- distinguish all-integrity-true Records from Records accepted with the exact
  requirement-99 progression waiver;
- compare the complete worktree with the Phase 0 baseline;
- permit Task007 calibration to continue after the implemented contract passes
  ordinary gameplay smoke, while keeping unrun edge-transaction fixtures
  explicitly deferred rather than describing them as passed;
- mark Task007C fully Completed only after the deferred focused edge gate is
  either executed or separately accepted as a permanent waiver;
- update Task007's continuation gate and active Task README.

## 14. Acceptance Criteria

Task007C is accepted only when:

- Task007B's primary-only Elemental application remains unchanged; its
  schema-19 combat, damage, Elemental-opportunity, Buff, and stack-unit
  integrity remains true, and the three requirement-99 progression failures
  are either replaced or carried only as the explicit fixture-specific waiver;
- every reviewed successful Tower-owned damage family can use an existing
  Electric or Wind Buff;
- Behaviour damage creates hit reactions but zero Elemental application or
  stack units;
- Electric and Wind reaction damage is FixedBuff and independent from every
  triggering Tower's Level, BasicDamage, Damage Bonus, Elemental Upgrade, and
  DamageScale;
- first-application and threshold-crossing hits execute the approved normal
  reaction ordering;
- committed threshold mutation returns one single-use pending completion;
  normal reactions precede its exactly-once Overload finalization, including
  when Electric reaction damage resolves the owner;
- coexisting Electric and Wind use deterministic Electric-then-Wind ordering;
- one hit triggers at most one reaction per reactive Buff;
- per-instance cooldown bounds high-frequency direct and persistent hits;
- Wind no-target does not consume cooldown;
- FixedBuff and reaction damage never recurse;
- old Electric/Wind StackApplied TowerScaled damage no longer executes;
- Overload, Protection, expiry, re-entry, Fire, Cold, UI, VFX, and persistent
  content remain functional;
- schema-20 reaction accounting passes at every nested scope and uses the exact
  `(hit transaction, target Monster, BuffDefinition)` unit, closed invalidation
  reasons, target-specific committed outcomes, and cycle-scoped gap samples;
- inherited integrity is reported field-by-field; no fixture-specific waiver is
  described as blanket success;
- static, Editor, Play Mode, documentation, asset-reference, and worktree
  protection evidence is recorded without overstating unrun checks.

## 15. Out Of Scope

- final Task007 balance acceptance;
- changing Fire or Cold normal mechanics;
- changing Electric LightningStrike or WindVortex Overload mechanics;
- changing MaximumStacks, ActiveDuration, Source Apply Cooldown, Overload
  Protection Duration, or final FixedDamage values without separate Task007
  evidence;
- restoring Behaviour-origin Elemental Buff application;
- requiring a minimum number of contributor Towers for Overload;
- making hit reactions TowerScaled or source-Tower-owned;
- allowing FixedBuff or reaction damage to trigger hit reactions;
- adding cross-Buff reaction chains;
- introducing a generic damage-type, resistance, critical-hit, vulnerability,
  or global proc framework beyond the explicit Electric/Wind requirement;
- changing Tower, Monster, Stage, Wave, route, or Draft balance to compensate for
  a failed Task007C contract.

## 16. Completion Record

Implementation began on 2026-08-26. The current checkpoint records:

- Phase 0 external baseline:
  `/private/tmp/towernexus-task007c-baseline.JCKcDL`, containing porcelain
  status, binary-safe tracked worktree/index patches, HEAD identity, and SHA-256
  inventory for all `125` modified or untracked baseline files;
- baseline Runtime and Editor builds: passed with `0` warnings and `0` errors;
- Task007B evidence synchronization: thirteen schema-19 Records retained;
  combat, damage, Elemental-opportunity, Buff, and stack-unit diagnostics pass,
  while `levelUpCountMatches`, `levelUpResolutionNodesMatch`, and
  `finalPlayerLevelMatches` remain the explicit requirement-99 fixture waiver;
- prior Unity Editor evidence: user-confirmed `Combat damage authoring validation
  passed: 75 assets checked`; the execution date is unavailable and this is not
  reused as Task007C post-implementation Editor evidence;
- post-implementation Unity Editor authoring validation: user-confirmed on
  2026-08-26, `Combat damage authoring validation passed: 75 assets checked`;
- provisional implementation anchors: Electric reaction `FixedDamage 14`, Wind
  secondary reaction `FixedDamage 20`, and shared per-instance reaction cooldown
  `0.5s` for each Buff; Task007 owns final numeric acceptance;
- Electric and Wind now author `TowerHitReceived` FixedBuff reactions; old
  StackApplied damage execution is removed while logical StackApplied lifecycle
  accounting remains;
- Tower-owned direct and Behaviour damage uses one target-specific hit
  transaction; Elemental application remains Task007B primary-only, reaction
  damage cannot recurse, and earned threshold Overload completion is deferred
  until Electric-then-Wind reaction evaluation finishes;
- Recorder schema advanced to `20` with overall Buff, overall source, per-Wave,
  and per-Wave nested-source reaction accounting, closed invalidation outcomes,
  cycle-scoped reaction gaps, FixedBuff reconciliation, and
  `elementalHitReactionDiagnosticsConsistent`;
- final static Runtime and Editor builds: passed with `0` warnings and `0`
  errors; scoped `git diff --check` passed;
- Phase 0 protected `Main.unity`, active StageDefinition, `Doc/.DS_Store`, and
  all pre-existing gameplay Records remain hash-identical to the captured
  baseline. Approved overlapping runtime and documentation paths were reviewed
  against that baseline.

Phase 7A gameplay evidence accepted on 2026-08-26 consists of these seven
schema-20 Records:

- `Task007C_Phase7A_StraightMultiple_HP4800_Archer_L3_ExplosiveArrow_ChargedArrows_SharedHitReaction_Schema20_01`;
- `Task007C_Phase7A_StraightMultiple_HP4800_Cannon_L3_BouncingShell_ChargedShells_SharedHitReaction_Schema20_01`;
- `Task007C_Phase7A_StraightMultiple_HP4800_Drone_L3_FinalDive_ChargedDrones_SharedHitReaction_Schema20_01`;
- `Task007C_Phase7A_StraightMultiple_HP4800_Magic_L3_ArcaneField_ChargedOrbs_SharedHitReaction_Schema20_01`;
- `Task007C_Phase7A_StraightMultiple_HP4800_Magic_L3_ArcaneField_GaleOrbs_WindNoTargetRecovery_Schema20_01`;
- `Task007C_Phase7A_StraightMultiple_HP4800_Magic_L3x2_ChargedOrbs_ArcaneField_1Element_P1P2_NoElementReactionSource_Schema20_01`;
- `Task007C_Phase7A_StraightMultiple_HP4800_Magic_L3x2_ChargedOrbs_GaleOrbs_P1P2_ElectricWindCoexistence_Schema20_01`.

All seven reached `Victory` and passed combat damage, Elemental opportunity,
Elemental hit-reaction, Buff lifecycle, stack-unit, and Wave-attribution
integrity. Their three progression fields remain the exact requirement-99
fixture waiver already named above. They provide ordinary gameplay evidence for
shared Electric/Wind reactions, Behaviour-owned hit sources, cooldown blocking,
non-matching Towers using an existing Buff, coexistence ordering, FixedBuff
damage signatures, and non-recursive application.

On 2026-08-26 the user explicitly deferred the remaining Phase 7B edge-
transaction smoke so Task007 value calibration can proceed. The following are
unrun or not yet deterministically proven and must not be described as passed:

- first-application MaximumStacks crossing and exactly-once pending Overload
  completion when the normal reaction resolves the owner;
- reapplication overflow ordering, Protection expiry, and same-target re-entry;
- deterministic Wind no-valid-target followed by same-cycle recovery without
  cooldown consumption or misleading VFX;
- Electric resolving an Electric-and-Wind owner before Wind, with the exact
  `OwnerResolvedByEarlierElementalReaction` invalidation outcome;
- an equal-hit/equal-reaction controlled comparison across different
  BasicDamage values.

These fixtures may be resumed if time permits or if Task007 gameplay exposes a
related defect. Their deferral does not claim full Task007C completion, does not
accept final reaction balance, and does not block Task007 calibration under the
implemented shared-hit-reaction contract.

Final completion must additionally record:

- the external baseline location and inventory counts;
- synchronized Task007B runtime evidence and status;
- provisional implementation anchors actually authored;
- all touched System, Task, runtime, Recorder, and asset paths;
- baseline/final Runtime and Editor builds;
- Unity Editor authoring validation;
- exact schema-20 smoke Record names;
- key Electric/Wind trigger, cooldown, no-target, recursion, damage, stack, and
  Overload counts;
- every inherited and new integrity result, including the exact disposition of
  the three requirement-99 progression fields;
- manual, waived, or unrun checks;
- final worktree comparison and any protected upstream differences.
