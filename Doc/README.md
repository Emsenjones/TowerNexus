# Tower Nexus Documentation

This directory separates durable runtime contracts from iterative balance design and bounded implementation work.

## Document Sets

| Directory | Purpose | Authority |
|---|---|---|
| `System/` | Stable gameplay rules, ownership, authoring contracts, and runtime invariants | Source of truth for how the game works |
| `Balance/` | Campaign experience, learning goals, and Reference Builds | Source of truth for approved Stage design intent |
| `Task/` | Bounded implementation contracts | Temporary execution guidance |
| `99_Temps.md` | Unstructured notes and future ideas | Not an approved source of truth |

## Reading Order

1. Read `System/00_ProjectOverview.md` for the game and system model.
2. Read `Balance/00_StageDesignBlueprint.md` for the campaign Stage design blueprint.
3. Read the owning System Document for the mechanic being tuned.
4. Return to the Stage Design Blueprint when a Task result cannot realize the approved Stage intent.
5. Use a Task Document only when implementing a bounded change.

## Authority And Drift

- System Documents answer what a mechanic means and which system owns it.
- The Stage Design Blueprint answers what each Stage should teach, require, and make the player build.
- Unity assets contain the currently authored executable values.
- Task Documents derive implementation values from the Blueprint and owning System contracts.
- An authored value is not automatically an approved target.
- When documents and assets disagree, record the drift and reconcile it explicitly.

## Main Index

- [System Documents](System/00_ProjectOverview.md)
- [Stage Design Blueprint](Balance/00_StageDesignBlueprint.md)
- [Current Task Documents](Task/)
