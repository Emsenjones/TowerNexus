# Task006 - Combat Diagnostics Isolation

Series: ArchitectureRefactor
Status: Completed - Remaining Targeted Acceptance Waived For This Iteration
Branch: `codex/architecture-refactor`
Depends on: Accepted Task001-Task005; migrate their final observation surfaces.

## 1. Problem And Goal

CombatBalanceRunRecorder is already guarded by UNITY_EDITOR, but placement still
unconditionally captures combat ownership fingerprints before/after commitment.
Monster/Buff snapshots also contain diagnostic fingerprints. The Recorder combines
subscriptions, fixture capture, aggregation, consistency checks, text formatting,
and file output in one large class.

Remove unnecessary diagnostic work when recording is unavailable or disabled,
and separate Recorder responsibilities while preserving its evidence contract.

## 2. Proposed Ownership

- Runtime owners expose observation facts; they do not depend on report DTOs,
  JSON writers, summaries, or acceptance decisions.
- A recorder/session adapter owns subscriptions, run lifetime, and immutable
  snapshots captured at the required boundary.
- Focused accumulators own Draft/investment, damage/Elemental, route, and entity
  aggregates. Reuse existing ElementalBuffRunAccumulator responsibilities.
- Report building and export consume completed snapshots/aggregates. File I/O and
  formatting never participate in gameplay commitment.
- Keep the attachable recorder entry point stable where possible. Split by actual
  responsibility; a partial-class-only file split does not establish ownership.

## 3. Isolation And Timing Contract

- Gate expensive diagnostic-only data production at the producer, not only the
  event subscriber. Cover Tower/Projectile ownership fingerprints and Monster/Buff
  fingerprints; inventory allocations and serialization performed without listeners.
- Retain movement/gameplay snapshots required for actual placement planning.
  Similar-looking diagnostic fields do not justify deleting gameplay inputs.
- Proposed baseline is Editor-only recording, preserving current build behavior.
  Disabling recording in Editor must also skip expensive diagnostic capture.
  Development-player recording is a separate feature unless explicitly approved.
- Recording must not change RNG consumption, eligibility, target choice, event
  authority, pause ownership, or commit ordering. Observation failure is isolated.
- Capture terminal Pending, investment, and other destructive-cleanup-sensitive
  facts synchronously before Stage cleanup. Deferred LateUpdate export cannot
  reconstruct them by reading the already-cleared live world.
- Preserve final Monster resolution attribution, post-commit route facts, and
  exactly-once terminal finalization, including disable/release and queued output.
- Keep schema 25 and field meanings for a structural extraction. If review finds
  a necessary semantic change, explicitly version it and specify comparison
  handling before implementation; do not silently repurpose or omit fields.

Task001 preservation requirement: keep investment evidence and terminal evidence
as phases before public notifications, with terminal Pending captured before
cleanup. Snapshot identity must survive destroyed Towers/views; a deferred exporter
must not replace captured facts with live-world reads.

## 4. Scope And Source Pointers

`Assets/Scripts/TowerDeployment/CombatBalanceRunRecorder.cs`,
`CombatBalanceRunJsonReport.cs`, `ElementalBuffRunAccumulator.cs`, new focused
recording helpers, `Assets/Editor/CombatBalanceRunRecorderMenu.cs`, and the minimal
runtime observation/fingerprint producers in placement, Monster, Buff, and combat.

Use a coherent diagnostics folder if approved, preserving .meta GUIDs and Unity
references. Assembly splitting is optional and must respect runtime/Editor
visibility; moving a MonoBehaviour into an Editor-only assembly requires explicit
serialized-reference and attachability validation. No broad asmdef migration.

No new metrics, schema redesign, automated play policy, probability tuning,
restoration of all archived reports, or wholesale removal of debug APIs.

## 5. Documentation And Review Decisions

Read the [history index](../History/CombatMathV2_Closeout.md) and the owning
[Monster](../System/07_MonsterSystem.md), [Draft](../System/08_DraftSystem.md),
[Placement](../System/09_TowerPlacementSystem.md), [Combat](../System/11_TowerRuntimeCombatSystem.md),
and [Buff](../System/15_BuffSystem.md) contracts.

Review observer activation/lifetime, expensive-field classification, and the
adapter/accumulator/export split. Recorder implementation details stay in Task
or tooling documentation; System documents retain gameplay ownership only.

## 6. Implementation Sequence

- Preserve the four accepted Task005 reports at baseline `630fcad` as integration
  references. Capture old investment reconciliation outputs before extraction;
  replay recorded route inputs against their preserved outputs. These are scoped
  deterministic comparisons, not a complete event-stream replay.
- Inventory producer-side cost and distinguish gameplay data from observations.
- Introduce recorder activation gating and extract responsibilities incrementally.
- Verify report construction using identical captured inputs and canonicalized
  outputs, allowing only timestamps, output paths, and transient IDs to differ.
- Run fresh integration fixtures and profile recording enabled versus disabled.

## 7. Acceptance

| Case | Required evidence |
|---|---|
| Recording off / non-Editor build | No diagnostic fingerprint generation or report I/O |
| Identical captured event/fixture input | Same aggregates, integrity, schema and field meanings |
| Victory, Defeat, technical failure, manual stop | Correct distinct terminal semantics and one finalization |
| Pending at terminal followed by immediate release | Snapshot preserved; no missing consumption attribution |
| Retry/disable/re-enable | No duplicate subscriptions or events from a prior Battle |
| Throwing observer/export failure | Gameplay unaffected; recording error diagnosed |
| Recording on/off under controlled stimuli | Same gameplay outcomes and random draws |

Validate Editor and player compilation, prefab references if moved, meaningful
aggregation tests, and representative Play Mode records. Independent live combat
runs are not assumed byte-identical: compare semantic invariants and fixture
identity, while exact report comparison uses identical captured inputs. Report
measured diagnostic cost; no unmeasured performance claims.

## 8. Completion

Record schema/field comparison, producer gating inventory, lifecycle evidence,
compilation results, and measured cost. Preserve any new evidence and its source
commit for eventual phase retirement. Implementation and managed evidence follow; native acceptance remains open.


## 9. Implemented Contract And Native Handoff

### 9.1 Ownership And Lifecycle

- The attachable `CombatBalanceRunRecorder` keeps its existing path, class,
  serialized fields and .meta GUID. It discovers at most one Battle root and
  derives consumer references from that root; Stage fixture discovery must match
  the same root. Mid-Battle enable/reset waits for the next initialization.
- `CombatRecordingSession` owns one Battle identity and its exact subscriptions.
  `CombatDiagnosticScope` owns Editor observation availability and nested draining;
  it never asks combat `IsUsable` to admit an observation. New Battle identities
  cannot reuse the old session even when their runtime components are reused.
- Terminal pre-cleanup capture stores Pending items, Tower builds and Player values.
  Closing keeps the old session alive through nested resolution, hit-finally,
  entity cleanup and synchronous Retry/disable callbacks. Final counters and time
  are frozen only after the outer producer scope drains. Captured Tower/source
  identity and target route coordinates survive destruction.
- Capture failures mark the recording unusable and suppress its acceptance export;
  a missing fingerprint cannot silently become passing evidence. Gameplay can
  continue. Nonterminal cancellation creates no synthetic ManualStop report.
- Draft/investment, damage, route and entity accumulators have separate state.
  Existing Buff/reaction accumulation is reused with captured Tower identities.
  `CombatReportBuilder` builds from frozen session data; `CombatReportIntegrity`
  holds route/damage checks. `CombatReportExporter` queues detached JSON strings,
  reserves unique pending filenames and isolates file errors. Queue lifetime is
  independent of the component. Manual Snapshot remains diagnostic Console output.
- Schema 25 DTO fields are unchanged. DTO/Buff helper files moved to
  `Assets/Scripts/Diagnostics/CombatBalance` with their original .meta GUIDs.
  No asmdef or Editor-assembly component migration was introduced.

### 9.2 Producer Cost Inventory

| Producer | Off / non-Editor behavior |
|---|---|
| Submission Tower/Projectile ownership fingerprints | Calls and string construction skipped |
| Monster placement diagnostic snapshot / Buff fingerprint | Returns no diagnostic snapshot; Buff serialization skipped |
| Projectile nearest-other relation lookup | Diagnostic lookup skipped; actual target selection preserved |
| Damage source and pre-hit target identity snapshot | Editor-only and recording-gated |
| Observation scope | Editor-only value-type scope; no per-scope heap object |
| JSON / report file output | No session means no output |

Actual movement revision snapshots, path/placement planning and required combat
observations retain their gameplay roles. This is not a claim that every debug
observation payload throughout the game has zero cost.

### 9.3 Managed Evidence

- `Tests/Task006/run.py`: 64 investment cases match the old implementation at
  `630fcad` (the original 32 cases were captured before extraction; 32 additional
  selection-attempt cases use the same pinned old source oracle); four preserved route reports replay identically from their committed
  route/lifecycle inputs; 36 additional positive/negative integrity checks pass.
- 17 assertions exercise production observation scopes and exporter with boundary
  doubles: nested terminal draining, synchronous identity switching/restoration,
  duplicate rejection, capture failure isolation, destroyed-source identity,
  disabled capture gating, detached output, filename collision and file failure.
- A 100,000-iteration disabled gate sample varied from approximately 5.2 to 57.8 ms under Mono (including
  concurrent compilation load),
  with zero capture invocations. This is a managed gate measurement, **not** Unity
  frame-time, allocation, gameplay-on/off parity or device profiling evidence.
- Task001–Task005 regression suites pass, preserving their documented limits.
  Task004's subscription extraction now reads the session adapter; its Submission
  harness explicitly uses recording-off doubles.
- The four Task005 records remain historical integration evidence. Full canonical
  report equivalence across all event families, native teardown and fresh export
  are not established by the scoped replay tests above.

### 9.4 Native Acceptance Still Required

1. Let Unity import and compile; check the Recorder remains attached with the
   expected run identity and Battle references, without missing-script errors.
2. Run Stage1: deploy and upgrade Towers, produce route changes, leave a Tower or
   Upgrade Draft in Pending at terminal, then proceed/retry promptly. Inspect the
   fresh JSON for terminal Pending, investment reconciliation, final resolution,
   last-hit damage and exactly one export per terminal Battle.
3. Run a Stage4/5 fixture with one Elemental layer and attack entities. Inspect
   source attribution, Buff/reaction, entity cleanup and route/damage integrity in
   its fresh JSON. Existing Task005 build-accessibility limitations remain separate.
4. Disable Recorder during an active run, re-enable mid-run, then begin another
   Battle. The partial run creates no acceptance JSON; the next full Battle must
   record once. A terminal report already queued must survive component disable.
5. Compare controlled recording-on/off stimuli and Unity Profiler capture costs.
   Verify equivalent gameplay/RNG outcomes and absent diagnostic fingerprint work
   with recording off. Managed gate timing does not replace this check.

Victory/Defeat and technical-failure native evidence should be distinguished from
managed scope tests. Do not force a gameplay failure merely to satisfy a checklist;
record any unexercised terminal path as pending or explicitly waived by the user.
Task003–Task005 prior native coverage limitations are not waived by Task006.

Compilation: Editor and non-Editor conditional C# compilation passed with zero
warnings/errors. The non-Editor check removes UNITY_EDITOR defines from the current
project; it is not an iOS/IL2CPP build or native device acceptance.


### 9.5 Fresh Native Records Reviewed — 2026-09-10

- `Task006_Stage1_TerminalSnapshot_Acceptance_01`: schema 25, Victory,
  21 spawned / 21 killed / 0 leaked, Player Health 4 -> 4. All 32 integrity
  flags pass; instance counts and effective damage independently reconcile
  (4,600). Two Archer deployments, one Level Up and two Behaviour upgrades
  produce five consumed Drafts / five investment commits. Final resolution time
  matches run end. Terminal Pending is empty in this run; retained-Pending acceptance is
  covered by the later Recovery run below. Two placement commits observe five monsters already on the new route;
  no connector or forced-relocation lifecycle is exercised.
- `Task006_Stage5_ElementalEntity_Acceptance_01`: schema 25, Defeat,
  76 spawned / 68 killed / 6 leaked / 2 unresolved, Player Health 6 -> 0.
  All 32 integrity flags pass; instance counts and effective damage independently
  reconcile (42,248). Ten selections are consumed and matched by ten investment
  commits. Final leak time matches run end; the two unresolved monsters are
  consistent with termination on health depletion.
- Stage5 builds: Magic L3 with Gale Orbs, Cannon L3 with Frostbound Shells and
  Twin Shells, Drone L1. Windcut records 181 application attempts / 17 overloads;
  Chilled records 13 attempts / one overload, with one Frozen application.
  Wind reactions trigger 230 times; Wind Vortex records 1,156 damage applications.
  Cannon records 64 Arc releases, all resolved as target or position impacts.
  Nine Drone lifecycle records each complete by BatteryAerialRetirement. This
  supports normal entity lifecycle evidence, not terminal cleanup of an active
  Drone, and does not imply isolated single-element balance validation.
- Exactly one file per reviewed RunName is present. These files alone cannot
  establish synchronous Retry timing, Console cleanliness, or the queued-export
  disable race. No claim is made for those paths.
- Recorder toggle/recovery evidence is recorded below. Controlled on/off profiling,
  technical failure and unexercised native cleanup paths remain pending.
  Overall status remains Implemented - Pending Native Acceptance.

- Group-three Partial: user reports disabling the Recorder mid-Battle and enabling
  it again when the last wave appeared; no JSON was generated at terminal. No
  matching Partial JSON is present. This matches partial-session cancellation.
- `Task006_Stage1_RecorderToggle_Recovery_01`: Victory, 21 spawned / 20 killed /
  one leaked / zero unresolved, Player Health 4 -> 3. All 32 integrity flags pass;
  21 registration observations, zero fallback observations, and effective damage
  4,596 reconcile with individual Monster records. Initial Draft is recorded.
  Five selections reconcile as four investments plus token `2:10` StillPending:
  Archer Behaviour upgrade Piercing Arrow. This matches the user's retained item
  and establishes nonempty terminal Pending capture. `finalBuildCommitted=false`
  is expected because this selected Draft was deliberately not consumed.
- User confirmed the Recovery Battle kept Recorder enabled throughout; the earlier
  wording omitted 'did not'. Group-three partial cancellation and next-Battle
  recording recovery are accepted, together with retained-Pending capture. This
  does not establish the separate queued-export disable race or other pending
  native paths listed above.


## Iteration Closeout — 2026-09-10

The user explicitly elected to close the ArchitectureRefactor iteration after the
completed implementation and recorded managed/native evidence, and to investigate
further cases when a concrete bug or unexpected behavior is observed. Remaining
unexecuted acceptance checks are waived for this iteration, not recorded as passed.
This closeout supersedes earlier pending/no-waiver status statements in this file;
the earlier sections remain the historical record of actual coverage.

Task001–Task006 are complete within their approved refactor scope. No new gameplay,
balance tuning or speculative follow-up implementation is included. Known coverage
limits remain available for future diagnosis; completion is not a zero-bug claim.

The explicit waiver includes test group four (controlled Recorder on/off gameplay
and Profiler comparison) and remaining unexercised native lifecycle/fault paths,
including the queued-export disable race. No Unity performance improvement or
complete deterministic gameplay/RNG parity measurement is claimed. Groups one to
three retain only the concrete coverage recorded in Section 9.5. Task006 code and
new acceptance reports remain uncommitted at this closeout; no push or merge is
implied by Completed status.
