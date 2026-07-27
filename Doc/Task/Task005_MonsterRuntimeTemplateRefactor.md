# Task005 - Monster Runtime Template Refactor

Status: Not started

Depends on: Task001 Battle Result Authority and the existing Monster runtime, Wave authoring, status UI, damage-number, Effect, and Buff foundations

## 1. Goal

Remove the MonsterDefinition ScriptableObject layer and make each authored
Monster prefab the complete reusable runtime template for one unique demo
Monster type.

MonsterWaveConfig Spawn Entries must reference a MonsterBehaviour prefab
directly. Gameplay baselines and Monster-local presentation parameters move to
the relevant behaviors already attached to that prefab.

This task is a clean ownership refactor. It must preserve Wave order, spawn
timing, Monster resolution, Player consequences, Task001 spawning-completion
facts, technical spawning failure, and authoritative Battle results.

The demo does not require multiple health, level, or balance profiles sharing
one visual Monster template. That content model remains deferred.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_GameFlowSystem.md`
- `Doc/03_PlayerSystem.md`
- `Doc/04_BattleHUDUISystem.md`
- `Doc/06_MonsterSystem.md`
- `Doc/13_EffectSystem.md`
- `Doc/14_BuffSystem.md`
- `Doc/Task/Task001_BattleResultAuthority.md`

## 3. Pre-Implementation State

- MonsterSpawnEntry stores a MonsterDefinition reference.
- MonsterDefinition stores the Monster prefab, display name, movement and
  health baselines, animation identities, death delay, hit-feedback values,
  status UI offset, and damage-number offset.
- Each Monster prefab already contains one root MonsterBehaviour and one
  MonsterHitFeedback behavior.
- Each Monster prefab also references its corresponding MonsterDefinition,
  creating a two-way authored relationship.
- Ten MonsterDefinition assets currently correspond one-to-one with ten Monster
  prefabs.
- The current Monster Wave asset references MonsterDefinition assets rather
  than Monster prefabs.
- MonsterBehaviour copies runtime baselines from MonsterDefinition during
  initialization.
- MonsterStatusUIItem reads maximum health through
  `MonsterBehaviour.Definition`.
- MonsterHitFeedback retains a MonsterDefinition reference after
  initialization.
- Task001 already requires route validation, successful instance creation,
  successful Monster registration, exactly-once spawning completion, and
  result-neutral technical spawning failure.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| MonsterWaveConfig | Retain ordered Waves and validate every direct Monster runtime-template reference |
| MonsterSpawnEntry | Reference one root MonsterBehaviour prefab plus Count and Spawn Interval |
| MonsterBehaviour | Own gameplay baselines, movement and death presentation parameters, Monster presentation offsets, and per-instance runtime state |
| MonsterHitFeedback | Own hit-animation and hit-flash authoring plus its transient feedback runtime |
| MonsterSpawner | Validate and instantiate the direct runtime template while preserving Task001 execution semantics |
| MonsterStatusUIItem | Read current and maximum health directly from MonsterBehaviour |
| MonsterDefinition | Removed after all serialized references and values are migrated |

MonsterBehaviour owns one spawned Monster's runtime state. Authored baseline
values on the prefab are read-only during play and must not be mutated to store
per-instance state.

MonsterHitFeedback remains presentation-only. Missing or invalid hit feedback
must not change damage, death, arrival, resolution, or Battle results.

## 5. Direct Runtime Template Model

Use one direct authored relationship:

```text
MonsterWaveConfig
    -> Ordered Wave
        -> Ordered Spawn Entry
            -> Root MonsterBehaviour Prefab
            -> Count
            -> Spawn Interval
```

One MonsterBehaviour prefab represents one unique Monster type in the demo.
Spawn Entries may reuse that same prefab reference wherever that type should
appear.

Do not add:

- A replacement Monster data ScriptableObject
- A Monster database
- A stable hand-authored Monster identifier
- A generic stats-profile abstraction
- Per-Wave Monster stat overrides
- Compatibility lookup from removed MonsterDefinition assets

If future content requires one visual Monster with multiple independent stat
profiles, that is a separate reviewed content-model task.

## 6. Authored Field Ownership

Move the existing MonsterDefinition values as follows:

| Existing MonsterDefinition Data | New Owner |
|---|---|
| Display Name | MonsterBehaviour |
| Monster Prefab | Removed; the Spawn Entry directly references the MonsterBehaviour prefab |
| Move Speed | MonsterBehaviour |
| Maximum Health | MonsterBehaviour |
| Walking Animator Boolean | MonsterBehaviour |
| Death Animator Trigger | MonsterBehaviour |
| Death Delay | MonsterBehaviour |
| Health Bar Offset | Rename to Status UI Offset on MonsterBehaviour |
| Damage Number Offset | MonsterBehaviour |
| Get-Hit Animator Trigger | MonsterHitFeedback |
| Enable Hit Flash | MonsterHitFeedback |
| Hit Flash Color | MonsterHitFeedback |
| Hit Flash Duration | MonsterHitFeedback |

MonsterBehaviour exposes read-only authored values required by consumers,
including at minimum:

- DisplayName
- MaxHealth
- StatusUiOffset
- DamageNumberOffset

An empty authored Display Name may fall back to the prefab instance name for
debug or presentation text. Do not add a separate identity field.

StatusUiOffset positions the combined health and active-Buff status display.
The old HealthBarOffset name must not remain in active runtime code or migrated
prefab authoring.

## 7. Runtime Initialization

MonsterBehaviour initialization no longer accepts MonsterDefinition.

Use this boundary:

```text
Instantiate Authored MonsterBehaviour Prefab
    -> Reset Current Health From Authored Maximum Health
    -> Reset Effective Movement From Authored Move Speed
    -> Clear Movement Modifiers And Locks
    -> Clear Buff Runtime
    -> Clear Terminal And Cleanup Guards
    -> Initialize Monster-Local Presentation Behaviors
```

Initialization must not rewrite the authored prefab baselines.

MonsterBehaviour removes:

- Its serialized MonsterDefinition reference
- The public Definition property
- All null-definition fallback branches

MonsterStatusUIItem initializes health presentation from
`MonsterBehaviour.CurrentHealth` and `MonsterBehaviour.MaxHealth`.

MonsterHitFeedback initializes from its own serialized authoring. It must not
receive or retain MonsterDefinition.

## 8. Spawn And Wave Execution

MonsterSpawnEntry changes from a MonsterDefinition reference to a typed
MonsterBehaviour prefab reference.

Successful processing of one configured Monster count remains:

```text
Validate Direct Monster Runtime Template
    -> Resolve Required Spawn And Target Data
    -> Establish A Usable Initial Route
    -> Instantiate The MonsterBehaviour Prefab
    -> Initialize Runtime State
    -> Supply Runtime Manager And Node References
    -> Register With MonsterManager
    -> Create Status UI Using MonsterBehaviour.StatusUiOffset
    -> Assign The Established Route
```

The direct prefab must contain MonsterBehaviour on its root. Do not perform
scene discovery or accept a child MonsterBehaviour as the Spawn Entry identity.

Any failure before successful registration cleans up the partial instance. Any
failure after registration unregisters and force-cleans the partial Monster.
The current Wave execution then terminates through the Task001 result-neutral
technical failure path.

Spawn Interval remains only between adjacent instances inside one Spawn Entry.
This task does not change Wave Delay, entry order, count semantics, or coroutine
terminal-state rules.

## 9. Validation

MonsterBehaviour owns reusable runtime-template validation.

Validation must reject at minimum:

- A missing direct MonsterBehaviour prefab in a Spawn Entry
- A MonsterBehaviour reference that is not on the prefab root
- Non-positive Maximum Health
- Negative Move Speed
- Negative Death Delay
- Missing required Animator
- Missing required MonsterHitFeedback
- Invalid hit-flash duration
- Missing or unusable runtime dependencies already required by Monster spawning

MonsterHitFeedback validates its own authored values and references.

MonsterWaveConfig must include the concrete Wave and Spawn Entry indices when a
referenced Monster template fails owner validation.

Validation reports errors without mutating authored prefab values or silently
substituting another Monster type.

## 10. Serialized Asset Migration

Migrate all existing Monster content as one explicit serialized transaction:

```text
Capture Each Definition GUID, Prefab GUID, Component File ID, And Value Set
    -> Write Gameplay And Presentation Values To Its Existing Monster Prefab
    -> Write Hit Feedback Values To Existing MonsterHitFeedback
    -> Replace Every Wave MonsterDefinition Reference With The Matching
       Root MonsterBehaviour Prefab Reference
    -> Verify Every Existing Monster Prefab Is Complete
    -> Verify No Runtime Or Serialized Reference Uses MonsterDefinition
    -> Delete MonsterDefinition Assets And Meta Files
    -> Delete The Empty Dedicated MonsterDefini Folder And Meta File
    -> Delete MonsterDefinition Script And Meta File
```

Preserve:

- Every existing Monster prefab path and GUID
- The existing MonsterWaveConfig asset path and GUID
- Existing prefab hierarchy and presentation references
- Existing authored values for every Monster type

The currently unused Golem template must still receive its authored values
before its MonsterDefinition is removed.

Do not rely on FormerlySerializedAs to convert a ScriptableObject reference into
a prefab-component reference or to move fields between different owners.
Perform the serialized migration explicitly.

## 11. Task001 Preservation Contract

This refactor must preserve the implemented Task001 behavior:

- Spawning running state remains independent from the returned Coroutine
  handle.
- Synchronous normal completion returns success without retaining a stale
  handle.
- Synchronous technical failure returns failure and cannot let prepared Battle
  startup report success.
- Normal spawning completion publishes exactly once only after every configured
  Monster succeeds.
- Invalid template authoring, route failure, instance failure, initialization
  failure, and registration failure publish exactly one concrete,
  result-neutral technical failure per execution.
- Stop, disable, Stage release, and preparation rollback do not publish normal
  completion or technical failure.
- BattleResult publication remains owned only by BattleRuntimeCoordinator.

Do not fold result authority into MonsterBehaviour, MonsterWaveConfig, or the
prefab.

## 12. Cleanup And Failure Rules

- A rejected direct template never enters the alive-Monster registry.
- A registered partial Monster is unregistered before force cleanup.
- ForceCleanup remains callback-free and does not advance Player progress,
  reduce Player health, or report post-resolution completion.
- Status UI or hit-feedback presentation failure does not manufacture a
  semantic Monster resolution or Battle result.
- Technical Wave failure closes Battle authority through the existing
  coordinator path and remains neither Victory nor Defeat.
- Releasing Stage runtime clears all spawned instances and bindings without
  modifying authored Monster prefab values.

## 13. Out Of Scope

- Multiple stat profiles sharing one Monster visual template
- Monster levels, elites, bosses, rarity, or difficulty scaling
- Per-Wave or per-Stage Monster stat overrides
- Addressables or dynamic content loading
- Monster pooling
- New Monster behavior or skills
- Changes to movement, damage, progress, Player health, or resolution formulas
- Changes to Task001 Battle result rules
- Game Flow, UI navigation, or Stage sequencing
- New generic configuration, database, registry, factory, or service-locator
  layers

## 14. Acceptance Criteria

- MonsterDefinition is absent from active runtime code.
- MonsterDefinition ScriptableObject assets and their meta files are removed.
- The empty dedicated MonsterDefini folder and its meta file are removed.
- Every existing Monster prefab remains at the same path with the same GUID.
- Every existing Monster prefab owns its complete gameplay and presentation
  authoring through MonsterBehaviour and MonsterHitFeedback.
- MonsterSpawnEntry directly references a root MonsterBehaviour prefab.
- Every existing Wave reference resolves to the intended Monster prefab.
- MonsterBehaviour initializes without a MonsterDefinition parameter.
- MonsterBehaviour exposes CurrentHealth and MaxHealth directly.
- MonsterStatusUIItem no longer reads through a Definition property.
- MonsterHitFeedback no longer receives or retains MonsterDefinition.
- HealthBarOffset is replaced by StatusUiOffset.
- DamageNumberOffset is owned by MonsterBehaviour.
- Existing authored Monster values are preserved.
- Missing or invalid direct templates fail validation with Wave and Spawn Entry
  context.
- Initial-route and registration failures remain result-neutral technical
  failures.
- Normal spawning completion and BattleResult behavior remain unchanged.
- No compatibility, variant-profile, database, or identity layer is added.

## 15. Static Validation

Run at minimum:

- Main Unity assembly compilation
- `git diff --check`
- Confirm MonsterDefinition.cs is removed from Assembly-CSharp.csproj before
  treating the build as evidence
- Confirm every new or moved source file is included in Assembly-CSharp.csproj
  before treating the build as evidence
- Search active scripts for MonsterDefinition, the Definition property,
  HealthBarOffset, and `Initialize(MonsterDefinition`
- Search assets for the removed MonsterDefinition script GUID and all removed
  MonsterDefinition asset GUIDs
- Confirm every MonsterWaveConfig Spawn Entry contains a direct prefab-component
  reference
- Confirm all ten Monster prefabs retain their original prefab GUIDs
- Compare migrated prefab values against the captured pre-deletion
  MonsterDefinition value manifest
- Review Task001 completion, failure, cancellation, and synchronous-start paths

Static validation does not prove Unity import, prefab deserialization,
MonoBehaviour reference resolution, or runtime event timing.

## 16. Unity Handoff

Validate at minimum:

- Unity imports with no Missing Script components.
- Every Monster prefab shows the migrated MonsterBehaviour values.
- Every Monster prefab shows the migrated MonsterHitFeedback values.
- Every Wave Spawn Entry resolves to the intended MonsterBehaviour prefab.
- All configured Monster types spawn with their prior health, speed,
  animation, hit feedback, status UI placement, and damage-number placement.
- Golem prefab authoring remains complete even though the current Wave does not
  select it.
- Missing direct prefab reference fails validation.
- Invalid Monster prefab authoring fails validation with Wave and Spawn Entry
  context.
- All-zero-delay Waves preserve synchronous completion behavior.
- Synchronous first-spawn failure prevents prepared Battle startup success.
- Asynchronous spawn failure cleans partial Monsters and produces no semantic
  result.
- Final Monster death after normal spawning produces one Victory.
- Final Target arrival at one Player health produces one Defeat and no Victory.
- Manual technical stop and Stage release produce no result.
- A fresh Battle does not retain prior spawning or Battle-result state.

Unless explicitly handed over otherwise, Codex owns runtime scripts, serialized
Wave and prefab migration, asset deletion, and static validation. The user owns
final Unity import inspection and Play Mode acceptance.
