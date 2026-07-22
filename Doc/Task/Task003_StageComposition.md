# Task003 - Stage Composition

Status: Ready for review

Depends on: Task001, Task002

## 1. Goal

Implement StageDefinition-driven battle composition so one selected Stage supplies its Map template, MonsterWaveConfig, Tower Draft pool, and Tower Upgrade Draft pool before battle runtime begins.

The demo must support at least five independently authored StageDefinitions, while one battle runtime composes exactly one selected StageDefinition.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_StageSystem.md`
- `Doc/02_PlayerSystem.md`
- `Doc/04_MapSystem.md`
- `Doc/05_MonsterSystem.md`
- `Doc/06_DraftSystem.md`
- `Doc/07_TowerPlacementSystem.md`
- `Doc/11_TowerUpgradeSystem.md`
- `Doc/Task/Task001_MapAuthoringRefactor.md`
- `Doc/Task/Task002_MonsterResolutionAndBattleStop.md`

## 3. Current State

- The scene owns one fixed Map instance and one fixed MonsterWaveConfig reference.
- DraftSystem reads global TowerDefinitionDatabase and TowerUpgradeDatabase components.
- Pathfinding, MonsterSpawner, placement, and deployment hold serialized or scene-discovered Map references.
- There is no StageDefinition, selected-Stage validation, Active Map establishment, composition-ready boundary, or Stage-owned cleanup.

## 4. StageDefinition

Create one `StageDefinition` ScriptableObject containing:

- Optional Display Name
- One Map template containing the Map owner component from Task001
- One MonsterWaveConfig
- TowerDefinition Draft pool
- TowerUpgradeDefinition Draft pool

Do not add StageId. The asset reference/name is sufficient until persistence or external lookup requires a stable identifier.

StageDefinition stores authored references only. It must never retain the instantiated Active Map, current Player state, spawned Monsters, deployed Towers, pending Drafts, or other battle runtime state.

## 5. Composition Transaction

Implement one explicit Stage composition transaction:

```text
Receive Selected StageDefinition
    -> Validate Complete Composition
    -> Clear Prior Stage-Composed Runtime
    -> Instantiate Selected Map Under Stage Runtime Root
    -> Establish Active Map
    -> Initialize Map Consumers
    -> Supply MonsterWaveConfig
    -> Supply Tower And Upgrade Draft Pools
    -> Initialize Fresh Player Battle State
    -> Mark Composition Ready
    -> Begin Battle Runtime
```

No Wave spawning, Draft generation, placement, or combat may begin before the transaction completes successfully.

If validation or initialization fails, do not leave a partially active Stage. Report the failing Stage and configuration slice, clean up only objects created by the failed composition, and keep battle gameplay stopped.

## 6. Runtime Distribution

The Stage composition owner supplies only the dependency slice each consumer owns:

| Consumer | Stage-Supplied Data |
|---|---|
| Map System | Instantiated Map and Active Map role |
| A* Pathfinding | Active Map query source |
| MonsterSpawner | Active Map and selected MonsterWaveConfig |
| Tower Placement/Deployment | Active Map |
| DraftSystem | Selected Tower and Tower Upgrade pools |
| Battle Runtime | Fresh-battle start after all dependencies are ready |

Use explicit initialization or replacement methods. Remove scene-wide Map discovery and stale serialized fallback paths that could bind a different Map from the selected Stage.

Stage composition is coordination, not a general service locator. Runtime consumers continue to own their domain behavior.

## 7. Draft Pool Migration

- DraftSystem reads the active Stage's Tower and Tower Upgrade pools.
- Preserve existing candidate weighting, deployed-Tower eligibility, pending reservation, and displayed-choice deduplication.
- Remove TowerDefinitionDatabase and TowerUpgradeDatabase only after DraftSystem and serialized prefab references have migrated.
- Do not copy Stage pools into another global database or mutate the StageDefinition at runtime.
- A null or duplicate definition in either pool is invalid Stage authoring.
- A Tower Upgrade whose TowerFamily cannot be represented by the Stage Tower pool is invalid when that family relationship is required.

## 8. Demo Bootstrap

Game Flow and Stage Selection UI are deferred. For the current demo, the Stage composition owner may expose one serialized initial StageDefinition and compose it at the existing battle entry point.

This bootstrap must use the same composition transaction as any future selector. Do not create a second scene-only initialization path.

## 9. Replacement And Cleanup

- At most one Active Map exists for one battle runtime.
- Replacing a Stage stops the current battle before releasing Stage-composed content.
- Destroy or release only the Active Map and runtime references created by Stage composition.
- Domain owners clean their Monsters, Towers, Attack Entities, pending Drafts, placement state, and subscriptions through their existing technical-cleanup boundaries.
- Recomposition must not retain prior Player health/progress, Draft pools, Wave state, occupied nodes, or Active Map references.
- Repeating initialization must not duplicate subscriptions or runtime roots.

## 10. Validation

Stage authoring validation must report:

- Missing or invalid Map template
- Missing or invalid MonsterWaveConfig
- Null or duplicate TowerDefinition references
- Null or duplicate TowerUpgradeDefinition references
- Referenced definitions that fail owner validation
- Incoherent TowerFamily coverage between Tower and Upgrade pools
- Map composition that fails Map validation

Validation must not repair referenced assets or silently substitute global content.

## 11. Unity Authoring Checklist

- Create an initial StageDefinition for the existing demo content.
- Convert the existing authored Map into a reusable Map template after Task001 validation.
- Assign the existing MonsterWaveConfig and the desired Tower/Upgrade pools.
- Configure the Stage runtime root and initial StageDefinition on the demo bootstrap.
- Remove old fixed Map, Wave, and Database references from the GameManager prefab only after runtime injection works.
- Author at least five StageDefinition assets when the five Stage contents are ready; do not duplicate placeholder gameplay content merely to satisfy an asset count.

Unless explicitly handed over, Codex owns scripts and documentation; the user owns final Stage asset composition, prefab/scene wiring, content selection, and Unity Play Mode acceptance.

## 12. Out Of Scope

- Stage selection UI
- Stage ordering or unlock rules
- Victory detection and next-Stage flow
- Defeat presentation or restart flow
- Save data and persistent progress
- Scene-transition strategy
- Multiple Spawn Routes or route-specific Waves
- Procedural Map generation
- Changes to Draft weighting or Tower Upgrade eligibility
- New Tower, Monster, Upgrade, Effect, or Buff content

## 13. Acceptance Criteria

- One StageDefinition completely identifies one playable battle composition.
- A separate StageId is not required.
- The selected Map template is instantiated once and becomes the only Active Map.
- Pathfinding, MonsterSpawner, placement, and deployment use that Active Map explicitly.
- MonsterSpawner uses the selected Stage's MonsterWaveConfig.
- DraftSystem uses only the selected Stage's Tower and Tower Upgrade pools.
- Global TowerDefinitionDatabase and TowerUpgradeDatabase runtime ownership is removed.
- Fresh Player and battle-active state are established before Wave execution begins.
- Invalid Stage composition never begins partial gameplay.
- Replacing/recomposing a Stage leaves no stale Map, Wave, pool, Player, or runtime references.
- The demo bootstrap and future Stage selectors share one composition path.
- The authoring model supports at least five independent StageDefinition assets.

## 14. Validation And Handoff

- Run targeted compilation for the main Unity assembly.
- Search for remaining runtime reads of TowerDefinitionDatabase, TowerUpgradeDatabase, fixed MonsterWaveConfig, scene-wide Map discovery, and stale Active Map references.
- Compose two controlled StageDefinitions sequentially and confirm Map, Wave, Draft pool, Player state, and cleanup isolation.
- Validate missing Map, invalid Map, missing Wave, duplicate pool entries, invalid definition, and family-coherence failures.
- Confirm no spawning, Draft, placement, or combat occurs before composition ready.
- Run `git diff --check` and review prefab/scene serialized-reference migration separately from script behavior.
