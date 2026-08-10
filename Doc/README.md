# Tower Nexus Documentation

This directory separates durable runtime contracts from iterative balance design and bounded implementation work.

## Document Sets

| Directory | Purpose | Authority |
|---|---|---|
| `System/` | Stable gameplay rules, ownership, authoring contracts, and runtime invariants | Source of truth for how the game works |
| `Balance/` | Cross-Stage growth experience, campaign learning goals, and Reference Builds | Source of truth for approved balance and Stage design intent |
| `Task/` | Bounded implementation contracts | Temporary execution guidance |
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

## Main Index

- [System Documents](System/00_ProjectOverview.md)
- [Stage Design Blueprint](Balance/00_StageDesignBlueprint.md)
- [Tower Growth And Upgrade Identity](Balance/01_TowerGrowthAndUpgradeIdentity.md)
- [Current Task Documents](Task/)
