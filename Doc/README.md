# Tower Nexus Documentation

This directory separates durable runtime contracts from iterative balance design and bounded implementation work.

## Document Sets

| Directory | Purpose | Authority |
|---|---|---|
| `System/` | Stable gameplay rules, ownership, authoring contracts, and runtime invariants | Source of truth for how the game works |
| `Balance/` | Cross-Stage growth experience, campaign learning goals, and Reference Builds | Source of truth for approved balance and Stage design intent |
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
- [Task Workspace](Task/)
- [ArchitectureRefactor Historical Evidence](History/ArchitectureRefactor_Closeout.md)
- [CombatMathV2 Historical Evidence](History/CombatMathV2_Closeout.md)

## Active Task Iteration: Re-roll System

The following contracts execute in dependency order. Task001 has user-reported
DamageNumber visual acceptance; Task002 optimization (schema 27) is implemented pending native Draft/Toast
integration and Recorder export acceptance. Task003 remains planned:

1. [Task001 - Reusable UI Animation And Toast](Task/Task001_ReusableUIAnimationAndToast.md)
2. [Task002 - Stage Free Re-roll System](Task/Task002_StageFreeRerollSystem.md)
3. [Task003 - Re-roll Accessibility Calibration](Task/Task003_RerollAccessibilityCalibration.md)

These numbers are local to this iteration. Historical Task contracts and
existing `Tests/TaskNNN` harnesses retain their original identities.
