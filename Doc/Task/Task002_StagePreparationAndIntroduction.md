# Task002 - Stage Preparation And Introduction

Status: Not started

Depends on: Existing Stage Composition runtime foundation

## 1. Goal

Split Stage composition into an explicit prepare boundary and a separate begin boundary, then add Stage-authored Introduction content without changing Draft gameplay rules.

Every initial Stage, next Stage, and retry must use the same preparation transaction. Preparation establishes the selected Map, Wave, Draft pools, and fresh Player state while all Battle gameplay remains inactive.

An optional Stage Introduction may hold the prepared Stage before Battle begins. Confirming the Introduction begins the already prepared Stage; it does not compose the Stage again.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_GameFlowSystem.md`
- `Doc/02_StageSystem.md`
- `Doc/03_PlayerSystem.md`
- `Doc/05_MapSystem.md`
- `Doc/06_MonsterSystem.md`
- `Doc/07_DraftSystem.md`
- `Doc/08_TowerPlacementSystem.md`

## 3. Pre-Implementation State

- StageDefinition already owns Player maximum health, Map template, MonsterWaveConfig, Tower Draft Pool, and Tower Upgrade Draft Pool.
- StageCompositionController already validates the selected Stage, creates one Active Map, and prepares Battle runtime dependencies.
- The current composition method immediately begins the prepared Battle.
- StageCompositionController still owns an automatic initial-Stage bootstrap.
- There is no stable prepared-but-not-started Stage boundary available to Game Flow.
- StageDefinition has no Introduction presentation data.
- DraftSystem already consumes plain TowerDefinition and TowerUpgradeDefinition pools and must remain unchanged by Introduction metadata.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| StageDefinition | Author one Stage's composition and optional Introduction content |
| StageCompositionController | Validate, prepare, begin, rollback, and release one selected Stage |
| BattleRuntimeCoordinator | Prepare and begin Battle consumers while preserving Battle gate authority |
| PlayerSystem | Own the fresh runtime level, progress, maximum health, current health, and Defeat state |
| DraftSystem | Continue to consume the existing plain Stage Draft pools |
| Game Flow | Future caller of prepare and begin boundaries; no controller implementation in this task |

StageDefinition remains authored data only. It must not store current Stage index, instantiated UI items, Active Map, runtime Player state, or whether the Introduction has already been acknowledged.

## 5. Stage Introduction Data

Add independent Stage-authored Introduction data:

- `showStageIntroduction`
- `introducedTowers`
- `introducedTowerUpgrades`

Expose read-only access for future Game Flow and presentation consumers.

Do not change the existing serialized Draft pool element types:

```text
Tower Draft Pool = List<TowerDefinition>
Tower Upgrade Draft Pool = List<TowerUpgradeDefinition>
```

Do not wrap Draft pool entries only to add presentation metadata.

Do not add `isNew` to TowerDefinition or TowerUpgradeDefinition. Introduction is relative to one Stage and is not a global property of those reusable definitions.

## 6. Introduction Validation

Stage validation must report:

- Null introduced Tower reference
- Duplicate introduced Tower reference
- Introduced Tower not present in the same Stage Tower Draft Pool
- Null introduced Tower Upgrade reference
- Duplicate introduced Tower Upgrade reference
- Introduced Tower Upgrade not present in the same Stage Tower Upgrade Draft Pool

When `showStageIntroduction` is true and both introduced lists contain no presentable content:

- Report an authoring warning.
- Do not invalidate an otherwise playable Stage.
- Allow future Game Flow to skip the empty Introduction and begin Battle directly.

When `showStageIntroduction` is false, configured Introduction lists remain authored data but are not presented for that Stage entry.

Stage Introduction validation must not change Draft weighting, eligibility, duplicate rules, pending reservation, or candidate generation.

## 7. Stage Preparation Transaction

Replace the current prepare-and-immediately-begin behavior with one explicit preparation transaction:

```text
Receive Selected StageDefinition
    -> Reject Reentrant Preparation
    -> Validate Complete Stage
    -> Release Prior Stage Runtime
    -> Instantiate Selected Map
    -> Establish Active Map
    -> Bind Pathfinding, Monster, Placement, And Draft Consumers
    -> Supply Selected Player Maximum Health
    -> Initialize Fresh Player Battle State
        -> Level = Initial Level
        -> Progress = Zero
        -> Maximum Health = Selected Stage Player Maximum Health
        -> Current Health = Maximum Health
        -> Defeated = False
    -> Validate Prepared Consumer State
    -> Mark Stage Prepared
    -> Keep Every Battle Gate Closed
```

Preparation returns success only after every required dependency is valid and the complete fresh Player state has been established.

No spawning, Draft generation, placement interaction, Tower combat, or Player resolution may begin during preparation.

## 8. Prepared Stage State

StageCompositionController must expose enough read-only state for Game Flow to distinguish:

- No Stage prepared
- Preparation in progress
- Stage prepared and waiting
- Battle begun for the prepared Stage

At minimum, callers must be able to read:

- Active StageDefinition
- Active Map
- Whether composition is ready
- Whether composition is currently in progress

The prepared state is not a pause state. It is a pre-Battle state in which gameplay authority has never opened.

## 9. Begin Prepared Stage

Add one explicit begin operation:

```text
Request Begin Prepared Stage
    -> Reject If Preparation Is In Progress
    -> Reject If No Valid Stage Is Prepared
    -> Reject If Battle Is Already Active
    -> Ask BattleRuntimeCoordinator To Begin Prepared Battle
    -> Start Spawning Last
    -> Confirm Every Consumer Gate Opened
    -> Mark Battle Begun
```

Beginning must use the exact dependencies established by preparation. It must not:

- Revalidate against a different StageDefinition
- Recreate the Map
- Reinitialize Player state
- Rebind Draft pools
- Re-run Stage Introduction

If begin fails, close any opened gates, release the failed prepared Stage runtime, and report failure. Do not leave a half-active Stage.

## 10. Release And Rollback

Release remains technical and result-neutral:

```text
Close Battle Authority
    -> Stop Spawning And Consumer Gates
    -> Clear Draft And Placement Interaction
    -> Force-Clean Monsters
    -> Stop Tower Combat And Attack Entities
    -> Destroy Runtime-Tracked Towers
    -> Clear Wave, Draft, Placement, Pathfinding, And Map Bindings
    -> Deactivate And Destroy Active Map
    -> Clear Active Stage And Prepared State
```

Cleanup must remain effective when:

- Preparation failed before Battle began.
- A prepared Stage is replaced.
- An active Stage is released.
- The owner is disabled.
- Technical stop already occurred.

Release only runtime objects owned by the composed Stage. Do not discover or capture unrelated scene objects.

## 11. Bootstrap Removal

Retire the automatic initial-Stage startup owned by StageCompositionController.

Remove the second initialization path represented by:

- A serialized initial Stage used only by the old bootstrap
- Automatic composition and Battle begin during StageCompositionController startup

Future Game Flow will select index zero and call the same prepare boundary used by next Stage and retry.

Do not preserve a hidden compatibility path that can begin Battle before Game Flow is ready.

Any obsolete serialized prefab fields are handled through normal Unity reserialization. Do not modify unrelated prefab or scene authoring in this task unless explicitly handed over.

## 12. Reentrancy And Failure Rules

- Reject nested prepare requests.
- Reject begin during preparation.
- Reject a second begin for the same active Battle.
- Reject release during the internal commit portion of preparation; defer or fail safely without partial cleanup.
- Establish state guards before calling consumer code that may publish callbacks.
- A failed prepare leaves no Active Map, Stage binding, fresh Player claim, or open gate.
- A failed begin releases the prepared Stage rather than returning to an ambiguous ready state.
- Repeating prepare after failure starts from a clean technical boundary.

## 13. Out Of Scope

- BattleResult implementation from Task001
- Ordered Stage list or current Stage index
- GameFlowController
- Main menu
- Next-Stage, retry, or return-to-menu decisions
- Stage Introduction UI
- Victory or Defeat UI
- Stage unlocks or persistence
- Changes to Draft generation rules
- New Tower, Upgrade, Monster, Map, Effect, or Buff content
- Dynamic UI loading
- Scene-transition strategy

## 14. Acceptance Criteria

- StageDefinition retains its existing plain Tower and Upgrade Draft pools.
- StageDefinition exposes independent Introduction configuration.
- Introduction content is validated as a subset of the matching current Stage pool.
- Empty requested Introduction content reports a warning without invalidating the Stage.
- One public preparation path establishes the selected Stage without beginning Battle.
- Stage preparation resets Player level, progress, Defeat state, maximum health, and current health.
- Current and maximum health both equal `CurrentStage.PlayerMaxHealth` after preparation.
- Battle gameplay remains inactive while the Stage is prepared.
- One explicit begin operation starts the already prepared Stage without recomposition.
- The old automatic initial-Stage startup path is removed.
- Initial Stage, next Stage, and retry can all use the same prepare and begin operations.
- Failed prepare or begin leaves no partial Stage runtime.
- Release remains idempotent and does not produce Victory or Defeat.

## 15. Static Validation

Run at minimum:

- Main Unity assembly compilation
- `git diff --check`
- Search for remaining automatic initial Stage composition
- Search for Battle begin inside the new preparation method
- Search for direct Player state writes outside PlayerSystem
- Search for Introduction metadata added to TowerDefinition or TowerUpgradeDefinition
- Search for changes to DraftSystem pool element types
- Review prepare, begin, rollback, release, and reentrancy state transitions

Static validation does not prove serialized Inspector migration or Play Mode lifecycle behavior.

## 16. Unity Authoring And Play Mode Handoff

User authoring:

- Configure Introduction content on StageDefinition assets.
- Confirm introduced content belongs to the same Stage pools.
- Reserialize the StageCompositionController prefab after obsolete bootstrap fields are removed.
- Preserve the Stage runtime root and BattleRuntimeCoordinator references.

Play Mode validation:

- Prepare a Stage and confirm no Monsters spawn before begin.
- Confirm Draft, placement, and Tower combat remain inactive while prepared.
- Confirm Player current and maximum health equal the selected Stage maximum.
- Confirm Introduction confirmation can begin the same prepared Stage.
- Prepare two Stages sequentially with different Maps, Waves, pools, and Player maximum health.
- Retry one Stage and confirm no Player, Map, Tower, Monster, Draft, or Wave state carries over.
- Force preparation and begin failures and confirm complete rollback.

Unless explicitly handed over, Codex owns scripts and static checks; the user owns Stage asset content, prefab/scene wiring, Unity reserialization, and Play Mode acceptance.
