# Tower Nexus Documentation

This directory separates durable runtime contracts from iterative balance design and bounded implementation work.

## Document Sets

| Directory | Purpose | Authority |
|---|---|---|
| `System/` | Stable gameplay rules, ownership, authoring contracts, and runtime invariants | Source of truth for how the game works |
| `Balance/` | Cross-Stage growth experience, campaign learning goals, and Reference Builds | Source of truth for approved balance and Stage design intent |
| `Art/` | Visual references, concept prompts, and stabilized asset-authoring guidance | Visual direction and approved art workflow |
| `Task/` | Bounded implementation contracts | Temporary execution guidance |
| `CombatReports/` | Generated combat diagnostic JSON reports | Runtime evidence, not design authority |
| `History/` | Milestone references and acceptance limits | Historical evidence index, not current design authority |
| `99_Temps.md` | Unstructured notes and future ideas | Not an approved source of truth |

## Reading Order

1. Read `System/00_ProjectOverview.md` for the game and system model.
2. Read `Balance/00_StageDesignBlueprint.md` for the campaign Stage design blueprint.
3. Read the relevant additional Balance document for cross-Stage growth or tuning intent.
4. Read the owning System Document for the mechanic being tuned.
5. Return to the Balance sources when a Task result cannot realize the approved intent.
6. Use a Task Document only when implementing a bounded change.

## Authority And Drift

- System Documents answer what a mechanic means and which system owns it.
- Balance Documents answer what growth should feel like and what each Stage should teach, require, and make the player build.
- Unity assets contain the currently authored executable values.
- Task Documents derive implementation values from the Blueprint and owning System contracts.
- An authored value is not automatically an approved target.
- When documents and assets disagree, record the drift and reconcile it explicitly.
- Retire completed or explicitly deferred Task documents after durable decisions
  are synchronized and historical evidence is preserved in a named Git version.
  Preserve known limitations and unexecuted checks in the milestone index;
  retirement does not imply that every planned test passed.
- Gameplay reports may be removed from the current working tree after their
  corresponding code, assets, Tasks, and reports are preserved together in Git.
  New runs recreate `CombatReports/` as needed.

## Main Index

- [System Documents](System/00_ProjectOverview.md)
- [Stage Design Blueprint](Balance/00_StageDesignBlueprint.md)
- [Tower Growth And Upgrade Identity](Balance/01_TowerGrowthAndUpgradeIdentity.md)
- [TowerInfoWindow Design](System/04_BattleHUDUISystem.md#42-towerinfowindow)
- [Battle Modal Pause Contract](System/02_StageSystem.md#32-battle-modal-pause-lifetime)
- [Exit Stage Confirmation Design](System/04_BattleHUDUISystem.md#43-confirmexitstagewindow)
- [Art Concept Prompt](Art/00_SableInspiredConceptPrompts.md)
- [Current Task Workspace: Toon Rendering](Task/README.md):
  [Task001: Two-Tone Shader](Task/Task001_ToonShaderFoundation.md) ->
  [Task002: Dynamic Outline](Task/Task002_ToonOutline.md) ->
  [Task003: Asset Integration And Validation](Task/Task003_ToonAssetIntegration.md).
  These are bounded task contracts; implementation and validation are pending.
- Vertical Slice Start Checkpoint: the prior Battle Modal Pause, TowerInfoWindow,
  and Exit Stage task documents are preserved under `Doc/Task/` in Git commit
  `f54e8a17`. The user declared that work complete and requested task retirement
  on 2026-10-28. Their detailed evidence and remaining per-case validation limits
  remain in that historical version. Exit Stage had a user-reported iPad mini 6
  smoke-test pass on 2026-10-22; retirement does not convert unrecorded native
  cases into verified results. Current Task001-Task003 refer to the new Toon work.
- ArchitectureRefactor and CombatMathV2 historical evidence remains in Git history.

## Re-roll System Demo Closeout

Reusable UI animation, Toast, Stage-local Re-rolls, and the bounded Demo
calibration are complete for the approved scope. The retired
`History/ReRollSystem_Closeout.md` in Git history preserves per-Stage decisions,
named reports, user-confirmed native checks, and deferred balance requirements.
Task documents are retired after preserving their final state in Git.
