# Task007B - Primary Elemental Opportunity Boundary Refactor

Status: Completed on 2026-08-26 with static implementation, user-confirmed
Unity Editor authoring validation, and thirteen schema-19 Play Mode evidence
Records; three sentinel-progression integrity fields are accepted only through
the named requirement-99 fixture waiver below

Depends on: Completed Task006 Drone Burst Elemental Opportunity Refactor;
Task007A Elemental Stack Contribution Refactor implementation checkpoint;
Task007 Phase D schema-18 Behaviour plus Elemental evidence

Blocks: Task007 Phase D reacceptance and Task010-Task017

## 1. Goal

Make the baseline primary attack path the only path that may produce ordinary
Elemental application opportunities within one Tower attack topology.

Behaviour Upgrades continue to add their authored Attack Entities, contacts,
bounces, explosions, completion results, persistent ticks, and damage, but those
Behaviour-added or Behaviour-extended results do not apply an Elemental Buff and
request no Elemental stack units. This keeps Basic, Behaviour, and Elemental
growth as distinct tuning axes while preserving matching-Element Towers as the
reliable way to accelerate shared stacking and Overload.

This Task supersedes the earlier Behaviour-opportunity portions of Task006,
Task007A, and the current System documents. It preserves Task006's Drone Burst
opener identity and Task007A's explicit positive contribution transaction for
the one opportunity that remains eligible.

## Current Effective Contract And Downstream Replacement

Task007B's primary-only Elemental application topology is the current effective
application contract. Task007C's accepted implementation checkpoint replaced
only Electric and Wind normal-value execution with shared FixedBuff Elemental
hit reactions from successful Tower-owned damage while preserving this Task's
primary-only permission to create, stack, refresh, or overload an Elemental
Buff. Task007C's remaining edge-transaction smoke is explicitly deferred and
does not change Task007B application authority.

## 2. Evidence And Decision

The first Task007 Phase D schema-18 screen allowed reviewed Behaviour results to
create ordinary Elemental applications. The resulting application attempts and
Overloads were:

| TowerFamily | Behaviour package | Application attempts | Overloads |
|---|---|---:|---:|
| Archer | Piercing Arrow | `143` | `1` |
| Archer | Scatter Arrow | `313` | `2` |
| Archer | Explosive Arrow | `417` | `4` |
| Cannon | Bouncing Shell | `61` | `1` |
| Cannon | Twin Shells | `59` | `2` |
| Cannon | Explosive Shell | `129` | `9` |
| Magic | Twin Orbs | `91` | `0` |
| Magic | Arcane Detonation | `45` | `0` |
| Magic | Arcane Field | `698` | `40` |
| Drone | Double Drones | `91` | `10` |
| Drone | Blast Rounds | `153` | `10` |
| Drone | Final Dive | `66` | `6` |

The Arcane Field run is the clearest boundary failure: `652` field damage
resolutions plus `46` primary Orb contacts produced `698` application attempts,
and all `40` measurement Monsters overloaded from one source Tower. The run
recorded `464` Protection-blocked attempts and an average first-application-to-
Overload time of approximately `2.28s`. This makes Elemental outcome frequency
primarily a property of the Behaviour package's coverage topology instead of
the Elemental Upgrade's authored contribution and matching-Tower coverage.

Arcane Detonation did not execute a recorded detonation damage signature in its
first Phase D run. Its old result therefore does not prove the completion
boundary either way; Task007B acceptance must explicitly exercise normal
completion and prove that detonation damage remains while Elemental application
does not originate from that result.

The accepted design response is a subtraction rather than another contribution
parameter: Behaviour results have zero Elemental opportunity. This is a runtime
eligibility rule, not an authored numeric `0` stored on a Behaviour Upgrade or
EffectDefinition.

### 2.1 Entry Gate And Upstream Baseline

Task007B begins from a dirty but intentional upstream workspace. The review that
created this contract observed Task007A runtime and Recorder changes, System and
Task documentation edits, balance assets, scene and fixture edits, and the
existing schema-17/schema-18 Records in the same worktree. A clean Git checkout
is not assumed and no pre-existing change may be normalized, reverted, or
silently absorbed into Task007B.

Before any Task007B System-doc or runtime edit:

- save the complete `git status --short` output;
- save the complete tracked diff in binary-safe form as the upstream baseline;
- inventory every untracked path together with a content hash;
- hash every currently modified tracked file as well as every untracked file;
- store the baseline outside the repository in a task-specific temporary
  directory so the baseline does not modify its own subject;
- run runtime and Editor baseline builds before Task007B changes, so an inherited
  compile failure is distinguishable from a Task007B regression;
- pause implementation if either baseline build fails;
- close the mandatory schema decision gate by freezing schema-19 provenance,
  candidate/eligible/dispatched semantics, Drone field replacements, and
  integrity equations before changing gameplay producers;
- treat every existing tracked modification and untracked file as protected
  upstream work, not only the specifically named scene, Stage, asset, or Record
  paths;
- freeze the new schema-19 Record names before Play Mode and never overwrite an
  existing `Task007_PhaseD_*` Record.

The implementation closeout compares the whole resulting worktree with this
baseline and reports only Task007B-owned deltas.

## 3. Layer Responsibilities

### 3.1 Basic Layer

Basic Upgrades modify deterministic Tower combat stats through the existing
resolved-stat contract. They may raise the damage of later primary or Behaviour
results, but they create no attack topology and own no Elemental contribution.

### 3.2 Behaviour Layer

Behaviour Upgrades own attack topology, target coverage, additional Attack
Entities, hit continuation, area damage, persistent entities, and completion
mechanics. Their added or extended results remain damage-capable but never
create ordinary Elemental application opportunities.

### 3.3 Elemental Layer

The equipped Elemental Upgrade owns the positive stack-unit contribution used
when the baseline primary result reaches its one authorized Elemental boundary.
Buff application, shared stacking, StackApplied contributor damage, lifecycle
effects, Overload, and Protection remain under the existing Effect and Buff
contracts.

One Tower may still reach Overload through repeated eligible primary attacks.
There is no minimum contributor count. Two or more Towers with matching
Elemental types improve opportunity rate, coverage, and reliability rather than
unlocking a separate permission.

## 4. Primary Opportunity Rule

An ordinary Elemental application requires all of the following:

1. the result belongs to the baseline primary member of the Tower's normal
   attack topology;
2. it reaches that member's reviewed normal application boundary: the first
   direct hit for Arrow and Shell, each valid primary-Orb contact, or the primary
   Drone's Burst-opener direct hit;
3. the source Tower currently owns a valid Elemental Upgrade and direct apply
   Effect;
4. the resolved Monster remains gameplay-targetable when application is
   dispatched.

The eligible primary result submits at most one target-specific application
request and carries the exact positive contribution authored by the equipped
Elemental Upgrade. Existing source cooldown, Protection, stack capacity, event
ordering, and one-Overload-per-application rules decide the outcome.

An ineligible result submits no Elemental application request. It does not pass
`0` through the normal Buff application transaction, create a blocked attempt,
consume source cooldown, refresh duration, or publish stack-unit accounting.
Eligibility is not transferred when an eligible entity misses, is cleaned up,
or loses its target. Damage, Effect, VFX, and completion may still resolve under
their existing contracts.

Elemental Buff damage, StackApplied damage, Overload, persistent reactions, and
all other downstream results remain non-recursive.

## 5. Tower-Family Boundaries

| TowerFamily | Eligible primary result | Elemental-ineligible Behaviour results |
|---|---|---|
| Archer | Center primary Arrow's first valid Monster Hit | Side/additional Arrows; later Piercing hits; Explosive Arrow area targets |
| Cannon | Primary initial Shell's baseline direct Monster result | Additional initial Shells; all bounce children; Explosive Shell area targets |
| Magic | Primary Orb contacts | Additional Orb contacts; Arcane Detonation completion targets; Arcane Field tick targets |
| Drone | Primary Drone's Burst-opening Projectile direct hit | Later Burst shots; every Projectile from an additional Drone; all Blast Rounds area targets; Final Dive direct and explosion targets |

### 5.1 Archer

- Piercing Arrow preserves continuation, damage, history, and capacity, but only
  the center primary Arrow's first valid Monster Hit is Elemental-eligible.
- The Arrow's immutable primary authorization and its mutable consumption state
  are separate. The first valid impact consumes authorization exactly once,
  before damage and application dispatch. If damage removes the Monster and no
  Buff request can be submitted, later Piercing hits remain ineligible.
- Scatter Arrow preserves its side entities and their damage; only the center
  primary Arrow may apply Elemental state.
- Explosive Arrow preserves direct-plus-area ordering and damage. The primary
  direct hit may apply Elemental state; every explosion target is ineligible,
  including the direct Monster if it also appears in the radius query.

### 5.2 Cannon

- The primary initial Shell retains its baseline direct opportunity.
- Additional Shells produced by Twin Shells are Elemental-ineligible.
- Every Bouncing Shell child is Elemental-ineligible, including descendants of
  the primary Shell.
- Explosive Shell preserves Position Impact, target query, and damage. Its area
  targets are Elemental-ineligible even when one is also the direct Monster.

### 5.3 Magic

- Contacts from the primary Orb retain ordinary Elemental eligibility.
- Contacts from additional Orbs produced by Twin Orbs are ineligible.
- Arcane Detonation still executes its normal-completion damage once per
  eligible active member, but all detonation targets have zero Elemental
  opportunity.
- Arcane Field still follows the Tower, ticks, resolves targets, and deals its
  authored TowerScaled damage, but every field-tick target has zero Elemental
  opportunity.

### 5.4 Drone

- Task006 Burst identity, opener creation, retarget preservation, and missed-
  opener non-transfer rules remain in force.
- Only the primary Drone's successfully released Burst opener may carry
  Elemental eligibility, and only its direct hit may dispatch the application.
- Every later Projectile in that Burst is ineligible.
- Every Projectile from an additional Drone is ineligible, including that
  Drone's local Burst opener.
- Blast Rounds preserves its area damage for every valid resolved target, but
  no Blast Rounds target receives an Elemental application. The primary opener's
  surviving direct target remains independently eligible.
- Final Dive preserves battery-end targeting, optional direct damage, Position
  Impact explosion, and completion, but neither the direct nor explosion result
  may apply Elemental state.

These Drone rules supersede Task006 sections 4.1-4.3 only where that Task grants
Blast Rounds area, additional-Drone opener, or Final Dive opportunities. They do
not reopen Burst cadence, projectile release, or retargeting behavior.

## 6. Authoring And Data Contract

- `TowerUpgradeDefinition.ElementalStackContribution` remains visible and valid
  only for Elemental Upgrades and remains positive.
- Basic and Behaviour Upgrades do not expose or consume an Elemental contribution
  field.
- EffectDefinition remains reusable execution data and receives no generic
  Elemental stack-contribution parameter.
- Behaviour damage is not mechanically repackaged into new EffectDefinitions as
  part of this Task. Existing direct runtime damage and existing Effect-backed
  Behaviour damage keep their current ownership.
- No TowerFamily switch derives stack units. No runtime fallback repairs missing
  eligibility or contribution.
- Existing Upgrade, Effect, Buff, Projectile, prefab, and scene GUIDs remain
  unchanged.

The implementation should represent primary-versus-Behaviour provenance at the
earliest stable attack-result boundary and preserve it through later execution.
EffectExecutor only forwards an explicitly authorized Elemental request; it does
not infer eligibility from Effect type, source Upgrade, damage success, target
count, or DamageSource identity after the fact.

## 7. Runtime Scope

The expected runtime work includes the producers and immutable payloads that
currently authorize Elemental application for:

- Archer primary, additional, piercing, and explosion results;
- Cannon primary, additional, bounce, and explosion results;
- Magic primary/additional Orb contacts, Arcane Detonation, and Arcane Field;
- Drone primary/additional identity, Burst opener direct hits, Blast Rounds,
  and Final Dive;
- Effect trigger-context creation for Behaviour-backed Effects;
- Recorder consistency checks needed to distinguish retained primary
  opportunities from prohibited Behaviour opportunities.

Use one explicit provenance or eligibility value carried by the owning attack
transaction where required. Do not create per-package contribution math,
duplicate Elemental application entry points, or a parallel Buff path.

## 8. Recorder Contract

Task007B requires Recorder schema `19`. Schema 18 preserves Buff application,
source, stack-unit, damage-signature, and Drone diagnostics, but it cannot
attribute an Archer, Cannon, or Magic application attempt to a primary versus
Behaviour result. Aggregate equality between primary damage and Buff attempts is
not accepted as provenance proof.

Schema 19 adds one read-only, Editor/Recorder-only Elemental opportunity
observation before the Buff transaction. It records at least:

- source Tower and equipped Elemental Upgrade identity;
- TowerFamily;
- attack-result provenance;
- attack-member identity (`Primary` or `Additional` where applicable);
- result role;
- an optional hit or result ordinal when multiple results otherwise share the
  same source, member, and role;
- whether a target-specific candidate result occurred;
- whether that candidate was Elemental-eligible;
- whether an ordinary Elemental application request was actually dispatched.

Candidate is observed when target identity and attack-result identity are known
at a potential Elemental boundary. Eligible is observed when topology authority
classifies that candidate; Recorder only observes and cannot decide the result.
Dispatched is observed inside EffectExecutor after the Elemental gate passes and
immediately before BuffApplyRequest is created and submitted. It does not depend
on whether Buff runtime later applies, cooldown-blocks, Protection-blocks, or
otherwise resolves the request.

A killing hit may therefore record candidate `1`, eligible `1`, and dispatched
`0`. Diagnostic provenance may travel through EffectTriggerContext to publish
the dispatch observation, but it is discarded before entering BuffApplyRequest.
These facts are diagnostics only: they never authorize gameplay, alter target
validity, derive stack units, or participate in Buff mutation.

The first schema-19 result roles are:

- `InitialDirect`;
- `PiercingContinuation`;
- `BounceChild`;
- `AreaResult`;
- `PersistentTick`;
- `CompletionResult`;
- `BlastArea`;
- `FinalDiveDirect`;
- `FinalDiveExplosion`.

The report aggregates candidate, eligible, and dispatched counts by provenance,
member identity, result role, source Tower, Elemental Upgrade, and Wave, and
exposes a top-level
`elementalOpportunityDiagnosticsConsistent` result. At minimum it proves:

- in every `sourceTower + elementalUpgrade + wave + provenance + member +
  resultRole` scope, dispatched count never exceeds eligible count and eligible
  count never exceeds candidate count;
- every Behaviour scope has eligible `0` and dispatched `0`;
- only approved primary provenance and result role can be eligible;
- prohibited Behaviour provenance may have positive candidate and damage counts
  but always has zero eligible and dispatched counts;
- for every source Tower, Elemental Upgrade, and Wave, the sum of dispatched
  ordinary Elemental applications equals the corresponding Elemental Buff
  application attempts without duplicating the Buff transaction's source or unit
  accounting;
- non-Elemental Buff requests never participate in this reconciliation.

After the refactor:

- Buff application attempts count only dispatched authorized primary
  opportunities;
- Behaviour damage signatures remain visible even when they create zero Buff
  attempts;
- Archer, Cannon, and Magic fixtures use explicit provenance counts rather than
  inferring origin from aggregate damage and Buff totals;
- Drone opening-slot identity is recorded independently from Elemental
  eligibility. An additional Drone opener remains an opening Projectile, but is
  an ineligible one and is never classified as a later shot;
- schema 19 replaces Drone fields whose schema-18 names combine opening-slot
  identity with eligibility. It separately reports all opening Projectiles, the
  Elemental-eligible primary subset, actual later Projectiles, direct-hit
  opportunities, Blast targets, additional-Drone results, and Final Dive results;
- Blast Rounds target opportunities and Final Dive opportunities are expected to
  be zero; additional-Drone direct opportunities are expected to be zero;
- `droneBurstDiagnosticsConsistent`, `buffDiagnosticsConsistent`, and
  `buffStackUnitAccountingConsistent` must use the new boundary rather than the
  superseded Task006 Behaviour permissions.

Schema 19 replaces inaccurate or ambiguous diagnostic fields rather than keeping
schema-18 aliases with competing meanings. It does not add attack provenance to
Buff state or create a second application-accounting system.

## 9. Document Synchronization

Before gameplay code changes, synchronize the stable cross-engine contract with
the following ownership split:

- `Doc/System/00_ProjectOverview.md` and
  `Doc/System/10_TowerFrameworkSystem.md`: layer-level summary only;
- `Doc/System/11_TowerRuntimeCombatSystem.md`: primary-only runtime rule and the
  authoritative family boundary matrix;
- `Doc/System/12_ProjectileSystem.md`: projectile member identity, immutable
  authorization, one-shot consumption, and bounce inheritance;
- `Doc/System/13_TowerUpgradeSystem.md`: Elemental contribution authoring
  ownership, without duplicating the family matrix;
- `Doc/System/14_EffectSystem.md`: explicit authorization forwarding,
  diagnostic-only provenance, and non-recursion;
- `Doc/System/15_BuffSystem.md`: Buff consumes an explicit request and never
  infers attack provenance.

The synchronized System documents state the layer responsibilities, primary
opportunity rule, non-transfer and no-recursion invariants, and each System's
ownership boundary. They do not enumerate C# methods, temporary fixtures, or
implementation checkpoints.

Synchronize the execution and historical relationship in:

- `Task006_DroneBurstElementalOpportunityRefactor.md`, preserving its Goal,
  implementation checkpoint, and evidence, then adding a prominent downstream-
  replacement-pending section that lists Blast Rounds area, additional-Drone
  opener, and Final Dive permissions;
- `Task007A_ElementalStackContributionRefactor.md`, preserving the historically
  correct statement that opportunity boundaries were unchanged by that Task,
  then adding a downstream-replacement-pending section that points to Task007B;
- `Task007_ElementalAndBuffBaseline.md`, revising Phase D and its continuation
  gate to use the primary-only contract;
- `Doc/Task/README.md`, adding Task007B to the active dependency order.

## 10. Migration And Balance Boundary

Task007B changes eligibility only. It does not change:

- the sixteen Elemental contribution values (`Archer 1`, `Cannon 4`, `Magic 3`,
  `Drone 2`);
- BasicDamage, DamageScale, FixedDamage, attack cadence, target count, radius,
  movement, lifetime, or tick interval;
- Buff MaximumStacks, ActiveDuration, Source Apply Cooldown, Overload Protection,
  lifecycle bindings, or reaction values;
- Stage, Wave, Monster, map, placement, or Draft fixtures.

Those existing values are implementation fixtures, not newly accepted balance
values. Task007 resumes calibration after this boundary is implemented and may
then tune Elemental contribution and reaction values against the reduced,
predictable opportunity set.

## 11. Required Evidence

### 11.1 Static And Editor Evidence

- Capture the complete Phase 0 status, tracked diff, and untracked path-plus-hash
  inventory before edits.
- Build `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` once before
  Task007B edits and again after implementation, recording baseline and final
  results separately.
- Run path-scoped `git diff --check`.
- Run `Tools/Tower Nexus/Validate Combat Damage Authoring` and record it
  separately as Unity Editor authoring evidence.
- Prove no Elemental contribution field was added to Behaviour Upgrades or
  EffectDefinitions and no unrelated asset was migrated.
- Scan all `ElementalApplication.TryApplyFromTowerAttack` calls,
  `EffectTriggerContext` authorization producers, `ProjectileRuntimeOptions`
  producers, `BuffApplyRequest` producers, and Elemental Apply Effect execution
  entries.
- Compare the complete worktree with the Phase 0 baseline and prove every new
  delta belongs to Task007B.

### 11.2 Gameplay Evidence

Rerun Task007 Phase D's twelve Behaviour plus Elemental fixtures under schema
19 with unchanged balance values and combat configuration. Each run must prove
both sides of the boundary: the Behaviour package still executes its authored
gameplay result, and Buff attempts originate only from the eligible primary
result.

The twelve Records use new `Task007B_*_PrimaryOnly_*_Schema19_01` names and never
overwrite the existing `Task007_PhaseD_*_Schema18_01` evidence. The exact frozen
names are:

```text
Task007B_PhaseD_StraightMultiple_HP4800_Archer_L3_PiercingArrow_ChargedArrows_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Archer_L3_ScatterArrow_ChargedArrows_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Archer_L3_ExplosiveArrow_ChargedArrows_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Cannon_L3_BouncingShell_ChargedShells_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Cannon_L3_TwinShells_ChargedShells_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Cannon_L3_ExplosiveShell_ChargedShells_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Magic_L3_TwinOrbs_ChargedOrbs_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Magic_L3_ArcaneDetonation_ChargedOrbs_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Magic_L3_ArcaneField_ChargedOrbs_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Drone_L3_DoubleDrones_ChargedDrones_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Drone_L3_BlastRounds_ChargedDrones_PrimaryOnly_Schema19_01
Task007B_PhaseD_StraightMultiple_HP4800_Drone_L3_FinalDive_ChargedDrones_PrimaryOnly_Schema19_01
```

Arcane Detonation additionally uses one focused completion fixture:

```text
Task007B_Focused_Magic_L3_ArcaneDetonation_ChargedOrbs_NormalCompletion_PrimaryOnly_Schema19_01
```

That fixture deliberately controls surviving-target presence, position, and Orb
normal-completion timing. Any adjusted observation window or target condition is
recorded as boundary-proof authoring and is excluded from balance comparison. It
must prove normal completion, positive Arcane Detonation damage, positive
detonation candidate count, and zero detonation eligible and dispatched counts.

Required focused checks include:

- Archer: side Arrows, later Piercing hits, and explosion damage resolve without
  added Elemental attempts;
- Cannon: additional Shells, bounce children, and explosion damage resolve
  without added Elemental attempts;
- Magic: additional Orb contacts are ineligible; the separate focused Arcane
  Detonation fixture proves normal completion and positive damage with zero
  detonation-origin eligible or dispatched applications;
  Arcane Field records positive tick damage resolutions with zero field-origin
  applications;
- Drone: only the primary Drone opener direct hit is eligible; later shots,
  additional-Drone projectiles, Blast Rounds area targets, and both Final Dive
  result types create zero opportunities;
- source Tower and Elemental Upgrade attribution remain exact;
- every eligible request uses the authored positive contribution and the schema-
  19 unit-accounting equality remains true;
- Buff and reaction damage create no recursive attempts;
- all combat, damage, Buff, unit-accounting, Drone, Wave, and resolution
  integrity flags pass, except intentionally false sentinel-progression flags
  documented by the fixed Task007 fixture.

After those twelve boundary regressions and the focused completion fixture pass,
Task007 owns any new single-source, matching-source, element-reaction, or final-
balance reruns required to accept the retained contribution and Overload values.

## 12. Acceptance

Task007B is accepted when:

- every TowerFamily implements the table in section 5 with no hidden package
  exception;
- all Behaviour damage and lifecycle results remain functional;
- no Behaviour-added or Behaviour-extended result submits an ordinary Elemental
  application request;
- an eligible baseline primary result that remains valid at dispatch submits
  exactly one request with the current Elemental Upgrade's authored contribution;
- missed, invalidated, and technical-cleanup opportunities are not transferred;
- Recorder evidence can distinguish retained primary applications from
  prohibited Behaviour applications without ambiguous accounting;
- System and Task documents describe the same ownership rule;
- schema-19 provenance and Drone opener-slot diagnostics are internally
  consistent and reconcile dispatched requests with Buff attempts;
- the required static, Editor, twelve-run gameplay, and focused Arcane
  Detonation evidence is recorded honestly and Task007's Phase D continuation
  gate is updated.

## 13. Completion And Evidence Record

The thirteen fresh Records have passed the Task007B combat and topology gate.
Task006 and Task007A therefore describe Task007B as their current effective
downstream application contract. Task007C is the current downstream normal-
value contract at its accepted implementation checkpoint; its deferred edge-
transaction fixtures must not be described as passed.

The completion record must separately list:

- the external Phase 0 baseline location and inventory counts;
- baseline and final Runtime/Editor build results;
- Unity Editor authoring validation;
- the twelve family/package schema-19 Records;
- the one focused Arcane Detonation schema-19 Record;
- Recorder-derived integrity and provenance evidence;
- manual, waived, or unrun checks;
- final whole-worktree comparison with the Phase 0 baseline.

The active Task README dependency order is updated during closeout so Task007B
is the accepted gate before Task007 resumes and before Task010-Task017.

Static implementation evidence recorded on 2026-08-25:

- The external baseline is
  `/private/tmp/towernexus-task007b-baseline.KPVXbT`. It contains the binary-safe
  tracked worktree patch, empty index patch, NUL-delimited status, HEAD identity,
  hashes for all `43` modified tracked files, and hashes for all `45` untracked
  files observed before Task007B implementation.
- Pre-change Runtime and Editor builds both passed with zero warnings and zero
  errors.
- System contracts `00`, `10`, `11`, `12`, `13`, `14`, and `15` now describe the
  approved primary-only target contract with one authoritative family matrix.
  Task006 and Task007A retain their historical checkpoint text and explicitly
  mark the downstream replacement as pending Task007B completion.
- Runtime now separates immutable primary authorization from one-way Archer
  consumption, separates Drone opening-slot identity from Elemental eligibility,
  and removes ordinary Elemental dispatch from additional, continuation, bounce,
  area, persistent, and completion results while preserving their damage and
  lifecycle execution.
- Recorder schema `19` exports candidate, eligible, and dispatched counts by
  source Tower, Elemental Upgrade, Wave, provenance, member identity, and result
  role; it includes result-ordinal bounds and
  `elementalOpportunityDiagnosticsConsistent`. BuffApplyRequest and Buff state
  contain no attack provenance.
- Final Runtime and Editor builds both passed with zero warnings and zero errors.
  The complete application-producer scan and scoped `git diff --check` passed.
- The final baseline comparison shows hash changes only in the Task007B code and
  documentation paths. Pre-existing scene, Stage, Wave, map, balance-asset, and
  gameplay-Record hashes remain unchanged from Phase 0.
- Unity Editor authoring validation was subsequently run by the user through
  `Tools/Tower Nexus/Validate Combat Damage Authoring` and reported
  `Combat damage authoring validation passed: 75 assets checked.` The exact
  execution date is not recoverable from repository evidence and is therefore
  recorded as unavailable rather than inferred.
- All twelve frozen family/package schema-19 Records and the focused Arcane
  Detonation Record now exist. Across all thirteen, damage diagnostics,
  Elemental opportunity diagnostics, Buff diagnostics, Buff stack-unit
  accounting, Drone diagnostics, Wave attribution, Monster resolution, and
  combat resolution fields pass. Behaviour damage remains positive where
  required while Behaviour-origin Elemental eligible and dispatched counts are
  zero; focused Arcane Detonation records normal completion and positive
  completion damage with zero completion-origin application permission.
- Every one of the thirteen Records reports
  `levelUpCountMatches = false`, `levelUpResolutionNodesMatch = false`, and
  `finalPlayerLevelMatches = false`. This is the intentional requirement-99
  sentinel fixture used to prevent an additional progression node, not an
  unnoticed gameplay failure. Task007B accepts only this exact named
  fixture-specific waiver; it does not describe the Records as blanket
  all-integrity success.
- Task007B is gameplay-accepted under that explicit waiver. Task006 and Task007A
  now point to its primary-only application topology as the current effective
  contract. Task007C has superseded only Electric/Wind normal-value execution at
  its accepted implementation checkpoint and preserves this topology.

## 14. Out Of Scope

- requiring two or more contributor Towers for Overload;
- changing Elemental contribution values or making them Effect-owned;
- introducing a serialized zero contribution for Behaviour content;
- converting all attack damage into EffectDefinitions;
- changing Behaviour damage or topology to compensate for lost applications;
- changing Buff, Overload, Protection, reaction, Tower, Monster, Stage, or Wave
  balance values;
- accepting final Task007 or campaign balance.
