

# 01 Player System

## 1. Overview

Player System defines the player's core runtime state in battle.

This system is responsible for player level, player experience, player health, and battle failure conditions related to the player.

Player System should be treated as an independent core system. Other systems may read player state or listen to player events, but they should not own or modify player data directly unless explicitly allowed by this document.

## 2. Design Goals

1. Provide a clear runtime data structure for player level, experience, and health.
2. Separate player-related logic from Tower Deploy System.
3. Allow Tower Draft System to react to player level-up events.
4. Allow Monster System to damage the player when monsters reach the target node.
5. Provide a clean foundation for future player-related features.

## 3. System Responsibilities

Player System is responsible for:

1. Tracking current player level.
2. Tracking current player experience.
3. Handling experience gain and level-up logic.
4. Tracking current player health and maximum player health.
5. Applying damage to the player when monsters reach the target node.
6. Triggering battle failure when player health reaches zero.
7. Sending player state change events to related systems.

Player System is not responsible for:

1. Tower dragging, snapping, placement validation, or deployment.
2. Tower draft UI selection logic.
3. Monster pathfinding, movement, or spawning.
4. Battle HUD layout and visual implementation.

## 4. Runtime Data

Player System should maintain the following runtime data during battle:

| Field | Type | Description |
| --- | --- | --- |
| currentLevel | int | Current player level. |
| currentExp | int | Current player experience within the current level. |
| currentHealth | int | Current player health. |
| maxHealth | int | Maximum player health. |
| isDead | bool | Whether the player has reached the battle failure state. |

## 5. Player Level System

Player Level System handles experience gain and level-up logic.

When the player gains experience, Player System should add the experience value to currentExp and check whether the player can level up.

If currentExp reaches the required experience of the current level, Player System should:

1. Increase currentLevel by 1.
2. Subtract the required experience from currentExp.
3. Trigger a level-up event.
4. Continue checking level-up conditions if the remaining experience is still enough for the next level.

If the player has reached the maximum level, currentExp should no longer trigger additional level-up logic.

## 6. Player Level Config

Player level configuration defines how much experience is required to level up.

Suggested config asset:

```text
PlayerLevelConfig
```

Suggested fields:

| Field | Type | Description |
| --- | --- | --- |
| levelExpList | List<int> | Experience required to level up from each level to the next level. |

Example:

| Index | Meaning |
| --- | --- |
| 0 | EXP required from Level 1 to Level 2. |
| 1 | EXP required from Level 2 to Level 3. |
| 2 | EXP required from Level 3 to Level 4. |

## 7. Player Health System

Player Health System handles player health changes during battle.

When a monster reaches the target node, Monster System should notify Player System.

Player System should then reduce currentHealth based on the monster's damage value or a default damage rule.

When currentHealth reaches zero or below, Player System should:

1. Clamp currentHealth to zero.
2. Set isDead to true.
3. Trigger a player death or battle failure event.
4. Prevent further health damage from being applied repeatedly.

## 8. Player Health Config

Player health configuration defines the player's starting health and related battle health parameters.

Suggested config asset:

```text
PlayerConfig
```

Suggested fields:

| Field | Type | Description |
| --- | --- | --- |
| startHealth | int | Player health at battle start. |
| maxHealth | int | Maximum player health. |

If startHealth and maxHealth are always the same in the current version, only maxHealth is required.

## 9. Events

Player System should expose events for other systems to react to player state changes.

Suggested events:

| Event | Description |
| --- | --- |
| OnPlayerExpChanged | Triggered when player experience changes. |
| OnPlayerLevelUp | Triggered when player level increases. |
| OnPlayerHealthChanged | Triggered when player health changes. |
| OnPlayerDead | Triggered when player health reaches zero. |

## 10. Related Systems

### 10.1 Tower Draft System

Tower Draft System may listen to OnPlayerLevelUp.

When the player levels up, Tower Draft System can open the tower draft workflow and allow the player to choose a new tower.

Player System only sends the level-up event. It does not decide which tower options are generated or how the draft UI is displayed.

### 10.2 Tower Deploy System

Tower Deploy System should not own player level, experience, or health data.

Tower Deploy System is only responsible for deployment-related logic, including tower dragging, snapping, placement validation, walkability update, path blocking validation, and successful deployment handling.

### 10.3 Monster System

Monster System should notify Player System when a monster reaches the target node.

Monster System should not directly decide battle failure. Battle failure should be triggered by Player System after health calculation.

### 10.4 Battle HUD System

Battle HUD System may listen to player events and update UI display.

Example UI elements:

1. Player level text.
2. Player experience bar.
3. Player health display.
4. Battle failure panel.

## 11. Future Extensions

Future player-related features should be extended in this document.

Possible future extensions:

1. Player attributes.
2. Player passive abilities.
3. Player skill system.
4. Player base upgrade system.
5. Player talent system.
6. Player damage resistance or shield system.