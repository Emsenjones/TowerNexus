

# 05 Draft System

## 1. Overview

The Draft System is responsible for generating runtime player choices during battle progression.

The first version of the Draft System focuses on tower-related drafting.

Current supported draft types:

- New Tower Draft
- Tower Upgrade Draft

Additional draft types may be added in future versions.

When the player levels up during battle, the Draft System generates multiple draft choices and allows the player to select one result.

The selected result is then forwarded to other gameplay systems.

The Draft System should not directly handle:

- Runtime tower deployment
- Placement validation
- GridNode occupancy
- Runtime walkability updates
- Player EXP calculation
- Player HP calculation
- Runtime battle UI layout

The Draft System owns only:

- Draft generation
- Draft choice rules
- Draft result handling
- Draft workflow state

---

## 2. Design Goals

1. Separate runtime drafting logic from deployment logic.
2. Support future draft content expansion.
3. Support event-driven runtime progression.
4. Allow multiple future draft reward types.
5. Keep gameplay ownership boundaries clean.

---

## 3. Runtime Ownership Boundary

| System | Owns |
|---|---|
| Player System | Level, EXP, HP, battle failure |
| Draft System | Draft generation and draft result workflow |
| Battle HUD UI System | Draft window display and player interaction |
| Tower Placement System | Tower deployment and placement validation |
| Tower Upgrade System | Tower upgrade progression and upgrade application |
| Map System | GridNode data and walkability |
| Monster System | Monster runtime behavior |

The Draft System should not directly own player progression, deployment validation, or runtime battlefield state.

---

## 4. Current First-Version Draft Flow

Current gameplay flow:

```text
MonsterSystem
    ↓ Monster Dies
PlayerSystem
    ↓ Gain EXP
PlayerSystem
    ↓ OnPlayerLevelUp
DraftSystem
    ↓ Generate Draft Choices
BattleHUDUISystem
    ↓ Open Draft Window
Player Selects Draft Choice
    ↓
DraftSystem
    ↓ Process Selection Result
```

If the selected result is a New Tower Draft:

```text
DraftSystem
    ↓ Process New Tower Draft
BattleHUDUISystem
    ↓ Add Pending Tower Entry
TowerPlacementSystem
```

If the selected result is a Tower Upgrade Draft:

```text
DraftSystem
    ↓ Process Tower Upgrade Draft
TowerUpgradeSystem
```

---

## 5. Current Draft Types

### 5.1 New Tower Draft

A New Tower Draft gives the player a new deployable tower.

The selected tower becomes a pending deployable tower entry handled later by Battle HUD UI System and Tower Placement System.

---

### 5.2 Tower Upgrade Draft

A Tower Upgrade Draft gives the player an upgrade option for an existing tower type.

Tower Upgrade Drafts may affect:

- Basic Layer upgrades
- Behaviour Layer upgrades
- Synergy Layer upgrades

Draft System only generates the choice.

Tower Upgrade System is responsible for applying the selected upgrade.

---

## 6. Draft Choice Generation

### 6.1 New Tower Pool

The first version uses a Tower Definition Database or Tower Pool.

The Draft System may randomly generate draft choices from:

- All available towers
- Weighted tower pools
- Future rarity groups
- Future unlock conditions

The Draft System owns the draft generation rules.

Tower Placement System should not decide draft generation.

---

### 6.2 Tower Upgrade Pool

Tower Upgrade Draft choices are generated from the player's current tower progression state.

The upgrade pool may be constructed using:

- Current tower types owned by the player
- Current tower levels
- Available upgrade layers
- Upgrade definitions provided by Tower Upgrade System

Detailed upgrade pool construction rules belong to Tower Upgrade System and may evolve in future versions.

---

### 6.2 Current First-Version Rules

Recommended first-version rules:

| Rule | Description |
|---|---|
| Choice Count | 3 choices |
| Duplicate Prevention | Optional |
| Random Weight | Equal weight initially |
| Refresh System | Not included |
| Rarity System | Not included |
| Ban/Pick System | Not included |

---

## 7. Draft Window

The Draft Window belongs to Battle HUD UI System.

Draft System should only provide:

- Draft data
- Draft choice content
- Draft selection callbacks

Battle HUD UI System is responsible for:

- Opening the Draft Window
- Displaying draft entries
- Handling player button interaction
- Closing the Draft Window

---

## 8. Draft Result Types

The first version supports:

```text
New Tower Draft Result

Tower Upgrade Draft Result
```

Future versions may support:

| Draft Type | Example |
|---|---|
| Tower Draft | New tower |
| Tower Buff Draft | Buff specific tower |
| Global Buff Draft | Global gameplay modifier |
| Temporary Buff Draft | Temporary battle buff |
| Utility Draft | Currency or support reward |
| Curse Draft | Tradeoff modifier |

The Draft System should remain generic enough to support multiple future reward types.

---

## 9. Draft Result Flow

### 9.1 New Tower Draft Result Flow

```text
Player selects tower
    ↓
DraftSystem validates selection
    ↓
DraftSystem creates draft result
    ↓
BattleHUDUISystem adds pending tower entry
    ↓
TowerPlacementSystem handles deployment later
```

---

### 9.2 Tower Upgrade Draft Result Flow

```text
Player selects tower upgrade
    ↓
DraftSystem validates selection
    ↓
DraftSystem creates draft result
    ↓
TowerUpgradeSystem applies upgrade
```

### 9.3 Ownership Rules

Draft System owns:

- Draft workflow
- Draft choices
- Draft result generation

Battle HUD UI System owns:

- Draft Window UI
- Draft interaction UI
- Pending tower entry display

Tower Placement System owns:

- Tower dragging
- Placement validation
- Grid occupancy
- Runtime deployment

---

## 10. Future Expansion Possibilities

Potential future systems:

- Weighted rarity system
- Synergy draft system
- Draft reroll system
- Limited draft pools
- Wave reward drafts
- Boss reward drafts
- Buff draft system
- Curse system
- Multiplayer shared drafts
- Persistent roguelike progression

The Draft System should remain event-driven and reward-type agnostic.

---

## 11. Related Systems

### Player System

Player System owns EXP accumulation and level-up events.

Draft System may subscribe to:

```text
OnPlayerLevelUp
```

---

### Battle HUD UI System

Battle HUD UI System displays:

- Draft Window
- Draft choices
- Pending tower entries

Draft System should not directly manage runtime UI layout.

---

### Tower Upgrade System

Tower Upgrade System owns:

- Tower upgrade progression
- Upgrade application
- Upgrade layer definitions

Draft System may generate Tower Upgrade Draft results and forward them to Tower Upgrade System.

---

### Tower Placement System

Tower Placement System only handles deployment after a tower has already become a pending deployable entry.

Tower Placement System should not generate draft choices.

---

### Monster System

Monster System may indirectly trigger Draft System progression through EXP rewards sent to Player System.

Monster System should not directly interact with Draft System.

---

## 12. First Version Scope

Included features:

- Player level-up draft trigger
- 3-choice draft window
- New Tower Draft
- Tower Upgrade Draft
- Random tower selection
- Pending tower entry creation
- Event-driven draft flow

Excluded features:

- Global Buff Draft
- Relic Draft
- Reroll system
- Weighted rarity
- Advanced synergy drafting
- Multiplayer drafting
- Persistent progression drafting

---

# Change Log

## 2026-05-24

- Created independent Draft System documentation.
- Moved tower draft ownership out of Tower Deploy System.
- Clarified ownership boundaries between Draft System, Battle HUD UI System, Player System, and Tower Deploy System.
- Added future support direction for buff drafting and generic reward drafting.
- Expanded Draft System from tower-only drafting toward a generic drafting architecture supporting New Tower Draft and Tower Upgrade Draft.