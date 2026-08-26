# Task007A - Elemental Stack Contribution Refactor

Status: Implementation complete on 2026-08-25; Unity Editor authoring validation
and schema-18 gameplay acceptance are deferred to Task007 calibration

Depends on: Completed Task005 Elemental Stack Contribution Damage Authority;
Task006 Drone Burst Elemental Opportunity Refactor completion or explicit
regression waiver; Task007 Phase B schema-17 one-source, overlapping-source,
and isolated-source evidence

Blocks: Task008-Task016. Task007 Phase B may resume after this static
implementation checkpoint and owns the deferred gameplay acceptance.

## Downstream Replacement Status

Current effective downstream application contract: Task007B Primary Elemental
Opportunity Boundary Refactor.

Task007A historically and intentionally left Elemental opportunity topology
unchanged while implementing contribution magnitude and schema-18 unit
accounting. Completed Task007B later replaced that topology with its primary-only
application boundary; this does not rewrite what Task007A implemented at its own
checkpoint. Task007C's accepted implementation checkpoint now owns current
Electric/Wind normal-value execution and preserves Task007A's authored
contribution solely as stack magnitude and Overload-frequency authority. Its
remaining edge-transaction smoke is explicitly deferred.

## 1. Goal

Make the number of stack units produced by one successful Elemental application
an explicit property of the contributing Elemental TowerUpgradeDefinition.

This refactor preserves one shared BuffDefinition and one MaximumStacks threshold
for an ElementType while allowing Tower Upgrade content with different attack
cadence and coverage topology to reach that threshold at an intentional rate.
It does not infer contribution from TowerFamily, current attack speed, BasicDamage,
or the number of Towers already participating in the stacking cycle.

## 2. Entry Gate And Workspace Protection

Before gameplay implementation:

- capture `git status --short`, hashes or scoped diffs for protected files, and
  the complete untracked Record list;
- preserve `Assets/Scenes/Main.unity`, the active StageDefinition,
  `Doc/.DS_Store`, Task007, and all existing gameplay Records except for an
  explicitly reviewed documentation edit;
- synchronize Task006 status and evidence after its remaining four regression
  groups are either executed or explicitly waived; neither outcome is inferred
  by Task007A;
- synchronize the stable cross-engine contracts in
  `Doc/System/13_TowerUpgradeSystem.md`, `14_EffectSystem.md`, and
  `15_BuffSystem.md` before runtime code changes.

System documents describe only authoring ownership, explicit request data,
atomic mutation, and lifecycle invariants. Concrete C# files, migration values,
Recorder implementation details, and test fixtures remain in this Task.

## 3. Evidence And Decision

Task007 Phase B used Recorder schema 17, MaximumStacks `10`, ActiveDuration `5s`,
Protection `10s`, and one stack unit per successful application. The accepted
Straight-Multiple findings are:

| TowerFamily | One-source maximum / Overloads | Overlapping two-source maximum / Overloads | Isolated two-source maximum / Overloads |
|---|---:|---:|---:|
| Archer | `10 / 1` | `10 / 2` | `10 / 3` |
| Cannon | `4 / 0` | `10 / 1` | `4 / 0` |
| Magic | `2 / 0` | `4 / 0` | `2 / 0` |
| Drone | `8 / 0` | `10 / 1` | `8 / 0` |

Successful application gaps remained within ActiveDuration during active
coverage. Lowering the shared threshold would make high-opportunity single
sources overload too readily while still failing to express the intended
family-specific cadence. Task007 therefore keeps MaximumStacks `10` and reopens
the previously deferred configurable stack-contribution design.

## 4. Authoring Contract

TowerUpgradeDefinition owns one serialized positive integer
`ElementalStackContribution`:

- default value is `1`;
- minimum valid value is `1`;
- the Inspector field is grouped under `Elemental Layer` and shown only when
  UpgradeLayer is `Elemental`;
- Basic and Behaviour Upgrade runtime never reads or applies this value;
- Elemental content validation rejects a non-positive authored value rather
  than silently repairing it at runtime;
- the public property returns the authored raw value and does not use a runtime
  clamp such as `Mathf.Max(1, value)`;
- the value belongs to the concrete Upgrade asset, not TowerFamily runtime,
  BuffDefinition, EffectDefinition, or Monster Buff state.

The first migration uses these provisional Task007 calibration anchors for all
four current Elemental Upgrade assets in each TowerFamily:

| TowerFamily | Provisional ElementalStackContribution |
|---|---:|
| Archer | `1` |
| Cannon | `4` |
| Magic | `3` |
| Drone | `2` |

These values are implementation and smoke-test anchors. Task007 owns their final
balance acceptance and may revise individual Elemental Upgrade assets later.

## 5. Application Transaction

One Elemental application opportunity remains one application request. The
source Tower's currently applied Elemental Upgrade supplies the requested stack
contribution for that request.

The contribution data flow is explicit:

```text
TowerUpgradeDefinition
    -> ElementalApplication validation
    -> EffectTriggerContext
    -> EffectExecutor forwarding
    -> BuffApplyRequest.RequestedStackUnits
```

EffectExecutor does not derive contribution from SourceUpgrade. A legacy or
non-Elemental request constructor may default RequestedStackUnits to `1`, but
non-ordinary Elemental and lifecycle Effect contexts never inherit a retained
Elemental source Upgrade's contribution. One target-specific opportunity still
creates exactly one request.

The transaction is:

1. Validate the source Tower, source Elemental Upgrade, target Monster, direct
   Elemental Apply Effect, and positive contribution.
2. Put the validated value into EffectTriggerContext, execute the existing
   Effect path, and submit one BuffApplyRequest carrying the exact source Tower,
   source Upgrade, and requested stack units.
3. Buff runtime evaluates definition identity, Protection, and source-scoped
   apply cooldown before committing stack units.
4. Invalid, cooldown-blocked, or Protection-blocked requests commit zero stack
   units, refresh no duration, execute no lifecycle binding, and cannot Overload.
5. A successful first application creates the Buff in Stacking phase with
   `min(requested units, MaximumStacks)` stacks, refreshes ActiveDuration, records
   source cooldown, and executes Applied exactly once. It does not execute
   StackApplied, preserving the Task005 event boundary. If the first application
   reaches MaximumStacks, Applied completes before one Overload; StackApplied
   still does not execute.
6. A successful reapplication refreshes ActiveDuration, records source cooldown,
   and atomically adds up to the remaining capacity. It executes StackApplied
   exactly once when at least one stack unit is committed, regardless of whether
   that request committed one or multiple units.
7. If a successful reapplication crosses or reaches MaximumStacks, its single
   StackApplied completes first with the exact contributor context; Overload
   then executes exactly once from shared Buff state, followed by the existing
   Protection or removal transaction.
8. Requested units beyond MaximumStacks are discarded. They never survive the
   transaction, create a second Overload, or carry into the post-Protection
   stacking cycle.

BuffApplyOutcome immutably reports eligible requested units, applied units, and
discarded units. Invalid, cooldown-blocked, and Protection-blocked outcomes
report zero for all three values. Non-stackable Buff outcomes and observations
also report zero stack units; the request constructor's compatibility default
does not imply an actual stack mutation. Every early-invalid return, including
the Monster-facing boundary, constructs the same explicit zero-unit outcome.

Committed state precedes read-only observation publication, snapshot/UI refresh,
VFX, and other presentation. Those consumers cannot affect the accepted unit
mutation or lifecycle order.

One application therefore remains one source-cooldown event, at most one
StackApplied event, and at most one Overload. Stack units are state magnitude,
not repeated application opportunities or repeated lifecycle events.

## 6. Damage And Source Authority

- StackApplied TowerScaled damage continues to execute once per successful
  reapplication and reads the exact Tower that produced that application.
- A contribution of `4` does not execute four StackApplied damage resolutions.
- Applied, PeriodicTick, Overload, EnteredProtection, removal, and persistent
  reaction results retain their existing damage modes and ownership.
- Added stack units never create recursive Elemental application opportunities.
- Contributor count does not gate Overload. One or many Towers may complete the
  threshold; matching sources improve rate and reliability through their
  authored contributions and overlapping coverage.

## 7. Recorder Schema 18

The implementation bumps CombatBalanceRunRecorder schema `17` to `18` in the
same checkpoint. Schema 18 preserves all existing combat, damage, source, Drone,
cycle, timing, Protection, histogram, and wave-attribution fields and adds:

- requested stack units;
- applied stack units;
- discarded stack units;
- maximum requested stack units by one successful application;
- maximum applied stack units by one successful application.

For diagnostic accounting, requested units are recorded only after an
application has passed validation, cooldown, and Protection gates. Blocked or
invalid attempts request and apply zero units. For every aggregate, per-source,
and per-wave scope:

```text
requestedStackUnits = appliedStackUnits + discardedStackUnits
```

The equality is checked independently in all four exported aggregation scopes:

- Buff overall;
- Buff overall per-source;
- per-wave Buff summary;
- per-wave nested source.

The report exposes a top-level `buffStackUnitAccountingConsistent` boolean. It
is true only when all four scopes pass. Existing `buffDiagnosticsConsistent`
continues to cover application, lifecycle, source, and aggregate consistency;
the new accounting result is not hidden inside that existing flag.

Existing `stackUnitsAdded` is renamed or replaced by the unambiguous applied
unit field in schema 18; no duplicate field with competing meaning remains.
Application-result counts remain application counts and must not be multiplied
by contribution.

Each exported Tower Upgrade record also exposes its authored Elemental stack
contribution when its layer is Elemental, allowing report identity to prove the
fixture without reading the asset separately.

## 8. Validation And Migration

The expected implementation surface is:

- authoring: `TowerUpgradeDefinition`;
- explicit request propagation: `ElementalApplication`,
  `EffectTriggerContext`, `EffectExecutor`, and `BuffApplyRequest`;
- mutation and outcome: `MonsterBuffInstance`, `MonsterBuffRuntime`,
  `BuffApplyOutcome`, and the Monster-facing early-invalid boundary;
- observation and export: `BuffRuntimeObservation`,
  `ElementalBuffRunAccumulator`, `CombatBalanceRunJsonReport`, and
  `CombatBalanceRunRecorder`;
- content: the sixteen active Elemental TowerUpgradeDefinition assets.

EffectTriggerContext carries stack units only for the explicitly authorized
ordinary Elemental application path. Lifecycle and other non-Elemental contexts
carry zero and EffectExecutor forwards that value without inspecting
SourceUpgrade. This Task adds no TowerFamily switch, runtime inference,
compatibility layer, or parallel stack subsystem.

- TowerUpgradeDefinition authoring validation checks a positive contribution
  only when the Upgrade is Elemental and reports the Upgrade asset identity on
  failure. Basic and Behaviour runtime ignore the hidden serialized field.
- All sixteen active Elemental Upgrade assets explicitly serialize their
  provisional contribution; Basic and Behaviour assets are not mass-edited.
- Existing Upgrade asset GUIDs and references remain unchanged.
- BuffDefinition MaximumStacks, ActiveDuration, source cooldown, and Protection
  values remain unchanged by this Task.
- Elemental application opportunity boundaries, including Drone Burst opener,
  Behaviour composition, and no-recursion rules, remain unchanged.

## 9. Required Evidence

Static acceptance requires:

- `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` builds with zero
  errors;
- all sixteen Elemental Upgrade assets reporting the expected positive value;
- schema-18 and JSON mapping checks, including all four accounting scopes;
- path-scoped `git diff --check`;
- comparison with the Phase 0 baseline proving no new unrelated scene, Stage,
  Wave, protected-document, or existing-Record changes.

Unity Editor authoring acceptance separately runs
`Tools/Tower Nexus/Validate Combat Damage Authoring` and records its result as
Editor evidence rather than a dotnet static check.

### 9.1 Deferred Schema-18 Gameplay Smoke

The user explicitly chose to execute these smoke cases during Task007 gameplay
calibration rather than as a separate pre-calibration Task007A session. They
remain required runtime evidence and are not implied by static implementation
acceptance.

Fresh schema-18 smoke evidence verifies:

- Archer contribution `1` preserves one-unit application behavior;
- Cannon `4`, Magic `3`, and Drone `2` produce the authored requested-unit value;
- a successful multi-unit first Apply executes Applied once and StackApplied
  zero times;
- a successful multi-unit reapplication executes StackApplied once;
- a state at `8` stacks receiving a request for `4` records requested `4`,
  applied `2`, and discarded `2`, then executes one StackApplied before one
  Overload;
- a temporary contribution of `12` on a first application records requested
  `12`, applied `10`, and discarded `2`, then executes Applied before Overload
  with zero StackApplied events;
- blocked attempts record no requested or applied units;
- non-stackable Buff stack-unit totals remain zero;
- source Tower and source Upgrade attribution remain exact;
- Drone application-attempt counts still match Burst opportunities rather than
  stack units;
- all existing integrity flags and `buffStackUnitAccountingConsistent` pass.

The contribution-`12` case uses a temporary smoke fixture only. No production
asset saves that value.

### 9.2 Deferred Task007 Provisional Value Acceptance

Task007 combines implementation smoke with provisional value acceptance. The
original three-condition comparison remains required for every TowerFamily:

- one-source control;
- P1/P2 overlapping two-source treatment;
- P1/P3 isolated two-source reference.

Task007 therefore uses twelve fresh schema-18 Records before accepting the
provisional values or continuing to element-specific reaction calibration.

## 10. Completion And Evidence Record

- Static implementation is complete when both dotnet builds, asset/schema
  scans, scoped diff checks, and Phase 0 workspace protection checks pass.
- Unity Editor authoring validation, schema-18 smoke, and Task007 numeric
  acceptance remain explicitly pending until the combined Task007 sessions.
- Evidence records keep static dotnet builds, Unity Editor authoring validation,
  Recorder-derived Play Mode evidence, manual checks, and waived or unrun checks
  as separate categories.
- Task007 names the exact accepted Records, closes the deferred smoke matrix,
  and updates its Phase B continuation gate. Any gameplay or Recorder defect
  reopens Task007A implementation rather than being treated as balance noise.

Implementation evidence recorded on 2026-08-25:

- Runtime and Editor dotnet builds passed with zero warnings and zero errors.
- All sixteen Elemental Upgrade assets explicitly serialize the provisional
  values: Archer `1`, Cannon `4`, Magic `3`, and Drone `2`.
- Schema-18 declarations and all four overall/source/wave/nested-source JSON
  mappings contain requested, applied, discarded, and both maximum unit fields;
  the schema-17 `stackUnitsAdded` fields are absent from runtime code.
- Scoped `git diff --check` passed. `Main.unity`, the active StageDefinition,
  `Doc/.DS_Store`, and the pre-existing schema-17 Record set have no new Task007A
  implementation changes relative to the captured Phase 0 baseline. Task007's
  documentation change is the intentional continuation-gate synchronization.
- Unity Editor authoring validation was not run in this static implementation
  session. Schema-18 Play Mode smoke and the twelve-run value comparison were
  also not run; both are explicitly deferred to Task007 as described above.
- Final workspace review compares protected Scene, Stage, documents, and old
  Records against the Phase 0 baseline and reports every intentional difference.

## 11. Out Of Scope

- changing MaximumStacks, ActiveDuration, source cooldown, or Protection;
- requiring a minimum number of contributor Towers;
- deriving contribution dynamically from Tower stats, attack speed, Buff state,
  placement, source count, or target state;
- changing direct damage, StackApplied DamageScale, Overload FixedDamage, radius,
  target count, or movement-control values;
- changing attack cadence or Elemental opportunity boundaries;
- final Task007 balance acceptance.
