# Task006 - Combat Diagnostics Isolation

Series: ArchitectureRefactor
Status: Draft - Pending Review
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

- Capture fresh pre-extraction reports on the post-Task005 baseline, including
  Draft consumption, route revision, Elemental reaction, and terminal cleanup.
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
commit for eventual phase retirement. No implementation or evidence exists yet.
