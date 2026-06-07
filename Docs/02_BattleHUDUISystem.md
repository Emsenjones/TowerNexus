# 02 Battle HUD UI System

## 1. Overview

The Battle HUD UI System is responsible for displaying runtime battle UI during gameplay.

This system acts as the primary runtime gameplay interface between the player and the battle systems.

Battle HUD UI System is responsible for displaying gameplay state, draft interaction, pending tower deployment interaction, and player battle information.

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
- Displaying player EXP progress.
- Displaying player HP.
- Displaying runtime battle state.
- Opening and closing the Tower Draft Window.
- Displaying pending deployment towers.
- Providing tower drag interaction entry.
- Displaying placement validation feedback.
- Displaying future runtime battle notifications.
- Displaying future battle failure UI.

The Battle HUD UI System is NOT responsible for:

- Player EXP calculation.
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
| Player System | Level, EXP, HP, death state |
| Draft System | Draft generation and draft result workflow |
| Tower Placement System | Placement validation and placement |
| Monster System | Monster runtime state |
| Battle HUD UI System | Runtime UI display and interaction |

---

## 5. Core UI Elements

### 5.1 Player Runtime Display

The first version should include:

| UI Element | Description |
|---|---|
| Current Level Text | Displays current player level |
| EXP Progress Bar | Displays current EXP progress |
| HP Bar | Displays current player HP |
| HP Text | Displays current HP and max HP |

These UI elements should listen to Player System runtime events.

Recommended events:

```csharp
OnPlayerExpChanged
OnPlayerLevelUp
OnPlayerHealthChanged
OnPlayerDead
```

---

### 5.2 Tower Draft Window

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
    ↓ Player Selects Tower
DraftSystem
```

---

### 5.3 Pending Tower Deployment Area

The Pending Tower Deployment Area stores towers selected from the draft window but not yet deployed onto the battlefield.

Responsibilities:

- Display pending tower entries.
- Allow tower drag interaction.
- Remove deployed tower entries.
- Display future recycle tower entries.

The Pending Tower Deployment Area should not validate placement.

Placement validation belongs to Tower Placement System.

---

### 5.4 Placement Feedback

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

### 7.1 Player EXP Flow

```text
MonsterSystem
    ↓ Reward EXP
PlayerSystem
    ↓ OnPlayerExpChanged
BattleHUDUISystem
    ↓ Update EXP UI
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
    ↓ Player Drags Pending Tower
TowerPlacementSystem
    ↓ Placement Validation
TowerPlacementSystem
    ↓ Successful Placement
BattleHUDUISystem
    ↓ Remove Pending Tower Entry
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
- EXP
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
- EXP rewards
- Player target arrival notification

BattleHUDUISystem may display monster-related runtime information.

---

## 10. First Version Scope

Included features:

- Current player level display
- EXP progress bar
- Player HP display
- Draft Window display
- Pending Tower Deployment Area
- Pending tower drag interaction entry
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

---

# Change Log

## 2026-05-24 (Naming Sync)

- Updated references from Tower Draft System to Draft System.
- Updated references from Tower Deploy System to Tower Placement System.
- Updated runtime interaction references from BattleHUDUI to BattleHUDUISystem.
- Renamed Tower Deployment Flow to Tower Placement Flow.
- Renamed Tower Draft Window references to Draft Window.

## 2026-05-24

- Created independent Battle HUD UI System document.
- Moved Battle HUD responsibilities out of Tower Deploy System.
- Clarified ownership boundaries between UI systems and gameplay systems.
- Added Player HP display responsibilities.
- Added runtime event flow definitions.
