# 02 Battle HUD UI System

## 1. Overview

The Battle HUD UI System is responsible for displaying runtime battle UI during gameplay.

This system acts as the primary runtime gameplay interface between the player and the battle systems.

Battle HUD UI System is responsible for displaying gameplay state, draft interaction, Draft item interaction, and player battle information.

The Battle HUD UI System should not own gameplay progression data, tower deployment validation logic, monster runtime logic, or player runtime state.

Instead, it should observe and display runtime data owned by other systems.

---

## 2. Design Goals

1. Provide clear runtime battle information to the player.
2. Separate UI display logic from gameplay ownership logic.
3. Centralize battle-related runtime UI.
4. Support runtime event-driven UI updates.
5. Allow future UI expansion without modifying gameplay systems.

---

## 3. System Responsibilities

The Battle HUD UI System is responsible for:

- Displaying player level.
- Displaying player level progress.
- Displaying player HP.
- Displaying runtime battle state.
- Opening and closing the Draft Window.
- Displaying draggable Draft items.
- Providing Draft item drag interaction entry.
- Displaying placement validation feedback.
- Displaying valid target highlights for Tower Upgrade Draft items.
- Displaying future runtime battle notifications.
- Displaying future battle failure UI.

The Battle HUD UI System is NOT responsible for:

- Player ResolvedMonsterCount calculation.
- Player level-up logic.
- Player HP calculation.
- Player death logic.
- Tower draft generation.
- Tower placement validation.
- Runtime walkability updates.
- Monster runtime logic.
- Pathfinding.
- Combat calculation.

---

## 4. Runtime UI Ownership Boundary

The Battle HUD UI System is a display layer.

Gameplay systems own runtime gameplay data.

The Battle HUD UI System only listens to gameplay events and updates visual state.

Recommended ownership boundaries:

| System | Owns |
|---|---|
| Player System | Level, ResolvedMonsterCount progress, HP, death state |
| Draft System | Draft generation and draft result workflow |
| Tower Placement System | Placement validation and placement |
| Monster System | Monster runtime state |
| Battle HUD UI System | Runtime UI display and interaction |

---

## 4.1 Battle UI Composition

The existing battle Canvas may use a Battle UI Root as its authored composition and configuration-validation point.

It groups references to:

- Battle HUD UI
- Monster status presentation
- Damage-number presentation

The Battle UI Root does not own gameplay state, does not forward routine UI requests, and does not act as a runtime service locator. Gameplay systems retain narrow dependencies on the presentation module they actually use.

The authored Draft Window remains in the battle Canvas hierarchy while closed. Opening a Draft activates that window and creates transient Draft choice items; closing it removes only those created items and returns the window to its inactive state.

---

## 5. Core UI Elements

### 5.1 Player Runtime Display

The first version should include:

| UI Element | Description |
|---|---|
| Current Level Text | Displays current player level |
| Level Progress Bar | Displays progress toward the next level and Draft opportunity |
| HP Bar | Displays current player HP |
| HP Text | Displays current HP and max HP |

These UI elements should listen to Player System runtime events.

Recommended events:

```csharp
OnPlayerProgressChanged
OnPlayerLevelUp
OnPlayerHealthChanged
OnPlayerDead
```

---

### 5.2 Draft Window

The Draft Window is opened by Draft System.

Battle HUD UI System is responsible for:

- Opening the draft UI.
- Displaying draft choices.
- Returning player selection callbacks.
- Closing the draft UI.

Battle HUD UI System should not generate tower draft choices itself.

Recommended flow:

```text
PlayerSystem
    ↓ OnPlayerLevelUp
DraftSystem
    ↓ Generate Choices
BattleHUDUISystem
    ↓ Open Draft Window
DraftWindowUI
    ↓ Player Selects Draft Choice
DraftSystem
```

---

### 5.3 Draft Item Interaction Area

The Draft Item Interaction Area stores Draft items selected from the Draft Window but not yet consumed.

Responsibilities:

- Display draggable Tower Draft and Tower Upgrade Draft items.
- Allow Draft item drag interaction.
- Act as the release target for cancelling the current Draft item drag operation.
- Remove consumed Draft items after successful placement, tower level-up, or upgrade application.
- Display future recycle tower entries.

Unconsumed Tower Upgrade Draft items in this area represent already selected upgrade capacity that Draft System may read when generating future Tower Upgrade Draft choices.

If any currently dragged Draft item is released back inside the Draft Item Interaction Area, the current drag operation should be cancelled. This applies to Tower Draft items, future Tower Upgrade Draft items, and future draggable Draft item types.

Canceling the current drag operation returns the Draft item to the Battle HUD interaction flow and should not trigger scene placement validation, tower level-up validation, or upgrade application validation.

The Draft Item Interaction Area should not validate placement, tower level-up rules, or tower upgrade rules.

The Draft Item Interaction Area should not decide Draft pool generation, pending upgrade reservation, candidate weighting, or sampling rules.

Placement validation belongs to Tower Placement System.

Tower level-up and upgrade validation belong to Tower Upgrade System.

---

### 5.4 Valid Target Highlighting

When dragging a Tower Upgrade Draft item, Battle HUD UI System should present valid target feedback requested by gameplay validation.

Example display behavior:

| Target Type | Display |
|---|---|
| Valid tower | Highlighted |
| Invalid tower | Dimmed or unhighlighted |

This presentation helps reduce interaction complexity.

Battle HUD UI System should not decide TowerFamily, TowerLevel, duplicate upgrade, or max-level rules.

---

### 5.5 Placement Feedback

Battle HUD UI System may display placement feedback during deployment.

Example feedback:

| Feedback | Description |
|---|---|
| Valid Placement Highlight | Placement is valid |
| Invalid Placement Highlight | Placement is invalid |
| Path Blocking Warning | Placement blocks monster path |

The actual validation logic should still belong to Tower Placement System.

---

## 6. Recommended Runtime Structure

Recommended first-version runtime structure:

```text
GameManager
├── PlayerSystem
├── DraftSystem
├── TowerPlacementSystem
├── MonsterSystem
├── MapSystem
└── BattleHUDUISystem
```

BattleHUDUISystem should subscribe to runtime gameplay events.

It should not become a gameplay logic owner.

---

## 7. Runtime Event Flow

### 7.1 Player Level Progress Flow

```text
MonsterSystem
    ↓ Monster Resolved
PlayerSystem
    ↓ OnPlayerProgressChanged
BattleHUDUISystem
    ↓ Update Level Progress UI
```

---

### 7.2 Player Level-Up Flow

```text
PlayerSystem
    ↓ OnPlayerLevelUp
DraftSystem
    ↓ Generate Draft Choices
BattleHUDUISystem
    ↓ Open Draft Window
```

---

### 7.3 Player HP Flow

```text
MonsterSystem
    ↓ Monster Reaches Target Node
PlayerSystem
    ↓ Apply Damage
PlayerSystem
    ↓ OnPlayerHealthChanged
BattleHUDUISystem
    ↓ Update HP UI
```

---

### 7.4 Tower Placement Flow

```text
BattleHUDUISystem
    ↓ Player Drags Tower Draft Item
TowerPlacementSystem
    ↓ Placement Validation
TowerPlacementSystem
    ↓ Successful Placement
BattleHUDUISystem
    ↓ Remove Consumed Draft Item
```

---

## 8. Future Expansion Possibilities

Potential future features:

- Runtime damage number display
- Combo display
- Runtime wave warning UI
- Boss warning UI
- Runtime notification feed
- Buff display
- Debuff display
- Runtime timer UI
- Pause menu integration
- Battle result UI
- Battle failure UI
- Multiplayer player status UI
- Runtime minimap
- Runtime skill bar

These future systems should still follow the same ownership principle:

Gameplay systems own gameplay data.
Battle HUD UI System owns only display and interaction.

---

## 9. Related Systems

### Player System

Provides:

- Level
- ResolvedMonsterCount progress
- HP
- Death state
- Runtime player events

BattleHUDUI listens to Player System events and updates display.

---

### Draft System

Provides:

- Draft choices
- Draft workflow
- Draft selection result handling

BattleHUDUISystem displays the Draft Window.

---

### Tower Placement System

Provides:

- Placement validation
- Runtime deployment logic
- Walkability update
- Path blocking validation

BattleHUDUISystem only provides interaction entry and visual feedback.

---

### Monster System

Provides:

- Runtime monster state
- Monster resolution reports
- Player target arrival notification

BattleHUDUISystem may display monster-related runtime information.

---

## 10. First Version Scope

Included features:

- Current player level display
- Level progress bar
- Player HP display
- Draft Window display
- Draft Item Interaction Area
- Draft item drag interaction entry
- Valid target highlight presentation
- Placement feedback display
- Runtime event-driven UI updates

Excluded features:

- Damage number display
- Runtime notification feed
- Buff UI
- Multiplayer UI
- Minimap
- Skill bar
- Battle replay UI
- Advanced animation systems
