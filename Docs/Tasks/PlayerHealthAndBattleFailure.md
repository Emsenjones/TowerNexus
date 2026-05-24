# Task002 Player Health And Battle Failure

## Goal

Implement player HP, monster target arrival damage, and battle failure logic based on:

- Docs/01_PlayerSystem.md
- Result of Task001 Player System Level Refactor
- Existing Monster System
- Existing Map System
- Existing BattleHUDUI

## Important

Do not implement immediately.

First inspect the current codebase and generate an implementation plan.

## Required Review

Before writing code, inspect:

1. Player System / PlayerLevelSystem result from Task001
2. MonsterBehaviour.cs
3. MonsterSpawner related scripts
4. Monster path movement logic
5. Target Node detection logic
6. BattleHUDUI related scripts
7. GameManager or battle runtime manager scripts, if any

## Implementation Plan Must Include

1. Where player HP should live
2. Whether a PlayerConfig asset is needed
3. How maxHealth and currentHealth should be initialized
4. How monsters currently detect arrival at the target node
5. How Monster System should notify Player System
6. How Player System should apply damage
7. How BattleHUDUI should display HP
8. How battle failure should be triggered
9. Files expected to modify
10. Files expected to create, if needed
11. Risks and assumptions
12. Verification plan in Unity

## Required Final Behavior

When a monster reaches the Target Node:

1. Monster System detects target arrival.
2. Monster System notifies Player System.
3. Player System reduces currentHealth.
4. Player System triggers OnPlayerHealthChanged.
5. BattleHUDUI updates HP display.
6. If currentHealth reaches 0:
    - currentHealth is clamped to 0
    - Player System sets isDead to true
    - Player System triggers OnPlayerDead
    - Battle failure flow is triggered
    - repeated death calls are prevented

## Player System Should Own

- currentHealth
- maxHealth
- isDead
- TakeDamage logic
- OnPlayerHealthChanged event
- OnPlayerDead event

## Constraints

- Monster System should not directly decide battle failure.
- Map System should not handle player damage.
- BattleHUDUI should only display player state.
- Preserve existing level-up and Tower Draft behavior.
- Do not implement tower deploy changes in this task.
- Do not implement unrelated future systems.