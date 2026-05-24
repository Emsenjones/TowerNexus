

# 01 Player System

## 1. Overview


Player System defines the player's core runtime state in battle.

Player System acts as the runtime player state authority.

This system is responsible for player level, player experience, player health, and battle failure conditions related to the player.

Player System should be treated as an independent core system. Other systems may read player state or listen to player events, but they should not own or modify player data directly unless explicitly allowed by this document.

## 2. Design Goals

1. Provide a clear runtime data structure for player level, experience, and health.
2. Separate player-related logic from Tower Placement System.
3. Allow Draft System to react to player level-up events.
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
5. Tower draft generation.

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

These events are intended for runtime observation by systems such as BattleHUDUISystem, DraftSystem, and future gameplay systems.

Player System should remain the owner of runtime player state.

## 10. Related Systems

### 10.1 Draft System

Draft System may subscribe to OnPlayerLevelUp.

When the player levels up, Draft System can open the draft workflow and allow the player to choose a new tower.

Player System only sends the level-up event. It does not decide which tower options are generated or how the draft UI is displayed.
Draft generation ownership belongs to Draft System.

### 10.2 Tower Placement System

Tower Placement System should not own player level, experience, or health data.

Tower Placement System is only responsible for placement-related logic, including tower dragging, snapping, placement validation, walkability update, path blocking validation, and successful placement handling.

### 10.3 Monster System

Monster System should notify Player System when a monster reaches the target node.

Monster System should not directly decide battle failure. Battle failure should be triggered by Player System after health calculation.

### 10.4 Battle HUD UI System

Battle HUD UI System may subscribe to player runtime events and update runtime battle UI.

Example UI elements:

1. Player level text.
2. Player experience bar.
3. Player health display.
4. Battle failure panel.
5. Runtime draft window.
6. Pending tower deployment area.

Battle HUD UI System is responsible only for display and interaction.

Player runtime data ownership still belongs to Player System.

## 11. Runtime Ownership Boundary

Recommended runtime ownership:

| System | Owns |
|---|---|
| Player System | Level, EXP, HP, death state |
| Draft System | Draft generation and draft workflow |
| Battle HUD UI System | Runtime UI display and interaction |
| Tower Placement System | Placement validation and placement |
| Monster System | Monster runtime behavior |

The purpose of this ownership separation is to reduce system coupling and improve long-term maintainability.

Player System should remain the single owner of runtime player progression and survival state.

## 12. Future Extensions

Future player-related features should be extended in this document.

Possible future extensions:

1. Player attributes.
2. Player passive abilities.
3. Player skill system.
4. Player base upgrade system.
5. Player talent system.
6. Player damage resistance or shield system.

---

# Change Log

## 2026-05-24 (Naming Sync)

- Updated references from Tower Draft System to Draft System.
- Updated references from Tower Deploy System to Tower Placement System.
- Updated runtime observer references to BattleHUDUISystem.
- Clarified Player System as the runtime player state authority.

## 2026-05-24

- Clarified ownership boundaries between Player System, Tower Draft System, Battle HUD UI System, Tower Deploy System, and Monster System.
- Updated Battle HUD references to the independent Battle HUD UI System.
- Clarified that Tower Draft generation belongs to Tower Draft System.
- Added runtime ownership boundary section.