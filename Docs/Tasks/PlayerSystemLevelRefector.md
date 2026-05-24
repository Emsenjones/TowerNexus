# Task001 Player System Level Refactor

## Goal

Refactor and integrate the existing player level logic into the new Player System architecture.

This task is based on:

- Docs/01_PlayerSystem.md
- Existing PlayerLevelSystem.cs
- Existing PlayerLevelConfig.cs
- Existing TowerDraftSystem related scripts
- Existing BattleHUDUI related scripts

## Important

Do not implement immediately.

First inspect the existing codebase and generate an implementation plan.

## Required Review

Before writing code, inspect:

1. PlayerLevelSystem.cs
2. PlayerLevelConfig.cs
3. TowerDraftSystem related scripts
4. BattleHUDUI related scripts
5. Any existing level-up event subscriptions
6. Any existing EXP gain calls from monster death or reward logic

## Implementation Plan Must Include

1. Current PlayerLevelSystem structure
2. Current PlayerLevelConfig structure
3. Current level-up to TowerDraftSystem flow
4. Whether PlayerLevelSystem should be:
    - kept as the level module
    - renamed to PlayerSystem
    - wrapped by a new PlayerSystem
    - refactored directly into PlayerSystem
5. Files expected to modify
6. Files expected to create, if needed
7. Event flow after refactor
8. Risks and assumptions
9. Verification plan in Unity

## Required Final Behavior

Player System should own:

- currentLevel
- currentExp
- EXP gain logic
- level-up logic
- player level-up event

TowerDraftSystem may listen to player level-up events.

BattleHUDUI may display player level and EXP progress.

## Constraints

- Preserve the existing working level-up to Tower Draft flow.
- Do not break TowerDraftSystem.
- Do not implement player HP in this task.
- Do not implement battle failure in this task.
- Do not implement unrelated future features.
- Do not let BattleHUDUI own player data.
- Do not let TowerDraftSystem own player progression.