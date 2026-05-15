# Task006 - Tower Draft System Refactor

## Goal

Create a lightweight `TowerDraftSystem` and refactor `BattleHUDUI` and `TowerDraftUI` so that tower draft logic and UI presentation are decoupled.

This task is based on:

```text
Docs/02_TowerDeploySystem.md
Docs/ProjectOverview.md
```

## Background

Currently, tower draft related logic appears to be mixed into UI scripts.

The expected responsibility boundary is:

```text
PlayerLevelSystem = handles EXP and level-up events
TowerDraftSystem = handles tower draft generation and draft selection result
BattleHUDUI = handles Battle HUD display and opens draft UI
TowerDraftUI = displays draft choices and returns selected tower
TowerDeploySystem = handles battlefield placement
```

## Required First Step

Before modifying any code, inspect the current implementation and provide an Implementation Plan.

The Implementation Plan should include:

1. Which existing scripts are related to the current draft flow
2. What responsibilities are currently mixed together
3. What new or modified scripts are needed
4. Proposed final data flow
5. Required Unity Inspector assignments
6. Risks or assumptions

Do not start implementation until the plan is reviewed.

## Scope

Implement only the minimum structural refactor needed to:

- Create a lightweight `TowerDraftSystem`
- Move draft generation and draft result handling out of UI scripts
- Keep `BattleHUDUI` focused on HUD-level UI coordination
- Keep `TowerDraftUI` focused on displaying choices and returning selection callbacks
- Preserve the existing pending tower and deployment flow as much as possible

## Out of Scope

Do not implement:

- Weighted random
- Rarity
- Unlock rules
- Synergy logic
- Tower upgrade
- Tower recycle
- Path blocking validation
- Major UI redesign

## Expected Data Flow

```text
PlayerLevelSystem
    ↓ OnLevelUp
TowerDraftSystem
    ↓ Generate draft choices
BattleHUDUI
    ↓ Open draft UI
TowerDraftUI
    ↓ Player selects tower
TowerDraftSystem
    ↓ Handles selected tower
BattleHUDUI / Pending Tower Area
    ↓ Adds pending tower
TowerDeploySystem
    ↓ Handles placement
```

## After Implementation

Summarize:

1. Changed files
2. Final data flow
3. Required Inspector setup
4. Assumptions
5. Any deviations from the task scope