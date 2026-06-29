

# 05 Draft System

## 1. Overview

The Draft System is responsible for generating runtime player choices during battle progression.

The first version of the Draft System focuses on tower-related drafting.

Current supported draft types:

- Tower Draft
- Tower Upgrade Draft

Additional draft types may be added in future versions.

When the player levels up during battle, the Draft System generates multiple draft choices and allows the player to select one result.

The selected result is then forwarded to other gameplay systems.

The Draft System should not directly handle:

- Runtime tower deployment
- Placement validation
- GridNode occupancy
- Runtime walkability updates
- Player ResolvedMonsterCount calculation
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
| Player System | Level, ResolvedMonsterCount progress, HP, battle failure |
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
    ↓ Monster Resolved
PlayerSystem
    ↓ Advance ResolvedMonsterCount
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

If the selected result is a Tower Draft and the player deploys it onto the map:

```text
DraftSystem
    ↓ Process Tower Draft
BattleHUDUISystem
    ↓ Create Draggable Tower Draft Item
TowerPlacementSystem
    ↓ Place New Tower
```

If the selected Tower Draft is dragged onto an existing tower with the same TowerFamily:

```text
BattleHUDUISystem
    ↓ Drag Tower Draft Item
TowerPlacementSystem
    ↓ Detect Existing Same-TowerFamily Tower Target
TowerUpgradeSystem
    ↓ Process Tower Level-Up Request
```

If the selected result is a Tower Upgrade Draft:

```text
DraftSystem
    ↓ Process Tower Upgrade Draft
BattleHUDUISystem
    ↓ Drag Upgrade Draft Item
TowerPlacementSystem
    ↓ Detect Target Tower Intent
TowerUpgradeSystem
    ↓ Apply Upgrade To Target Tower
```

---

## 5. Current Draft Types

### 5.1 Tower Draft

A Tower Draft represents a tower card or tower item.

The player may use a Tower Draft in two ways:

1. Deploy it onto a valid deployment tile.
2. Drag it onto an existing tower with the same TowerFamily to request a tower level-up.

Deploying a Tower Draft consumes the draft item and creates a new tower.

Using a Tower Draft as a tower level-up request consumes the draft item only if TowerUpgradeSystem accepts the request.

TowerPlacementSystem only detects placement or target intent. TowerUpgradeSystem owns tower level-up rules and execution.

---

### 5.2 Tower Upgrade Draft

A Tower Upgrade Draft gives the player an upgrade item for an eligible existing tower.

Tower Upgrade Drafts may affect:

- Basic Layer upgrades
- Behaviour Layer upgrades
- Synergy Layer upgrades

Tower level determines which upgrade layers may appear for a tower:

| Tower Level | Eligible Tower Upgrade Draft Layers |
|---|---|
| Lv1 | Basic |
| Lv2 | Basic, Behaviour |
| Lv3 | Basic, Behaviour, Synergy |

Draft System only generates the choice.

Tower Upgrade System is responsible for validating the target tower and applying the selected upgrade.

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

Tower Upgrade Draft choices are generated from the player's current tower instance state.

Draft System owns Tower Upgrade Draft pool generation.

TowerUpgradeSystem provides upgrade definitions and eligibility checks, but Draft System owns how those eligible candidates are gathered, weighted, sampled, deduplicated for display, and presented as Draft choices.

The upgrade pool should be constructed using:

- Current tower instances on the battlefield
- Each tower instance's TowerFamily
- Each tower instance's TowerLevel
- Unlocked upgrade layers for that tower level
- Remaining upgrade slots for each unlocked layer
- Upgrades already applied to that tower
- Upgrade definitions provided by Tower Upgrade System

For every tower instance, Draft System should request or evaluate eligible upgrades using TowerUpgradeSystem rules:

- Determine TowerFamily.
- Determine TowerLevel.
- Determine unlocked upgrade layers and remaining slots.
- Gather all valid upgrades that tower is eligible for.
- Exclude upgrades already owned by that tower.

The resulting candidate set forms the Tower Upgrade Draft Pool for the current Draft.

Tower Upgrade Draft Pool generation is tower-instance weighted.

Example:

```text
Battlefield
- Archer Lv2 x 3
- Cannon Lv1 x 1
```

Because three eligible Archer tower instances exist, Archer upgrade definitions naturally receive higher representation in the generated pool.

This creates the desired behavior:

- More invested tower types appear more often in upgrade drafts.
- More high-level towers create more opportunities to discover higher-level upgrades.
- The player can shape future upgrade discovery by choosing which towers to deploy and level.

Draft System should still prevent duplicate upgrade options from appearing in the same displayed Draft round.

The pool may contain weighted duplicate candidates internally, but the final displayed choices should not show the same upgrade definition more than once in a single Draft window.

Detailed upgrade eligibility rules belong to Tower Upgrade System and may evolve in future versions.

---

### 6.3 Current First-Version Rules

Recommended first-version rules:

| Rule | Description |
|---|---|
| Choice Count | 3 choices |
| Duplicate Prevention | Prevent duplicate displayed options within the same Draft round |
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
Tower Draft Result

Tower Upgrade Draft Result
```

Future versions may support:

| Draft Type | Example |
|---|---|
| Tower Draft | New tower or tower level-up request |
| Tower Buff Draft | Buff specific tower |
| Global Buff Draft | Global gameplay modifier |
| Temporary Buff Draft | Temporary battle buff |
| Utility Draft | Currency or support reward |
| Curse Draft | Tradeoff modifier |

The Draft System should remain generic enough to support multiple future reward types.

---

## 9. Draft Result Flow

### 9.1 Tower Draft Result Flow

```text
Player selects tower
    ↓
DraftSystem validates selection
    ↓
DraftSystem creates draft result
    ↓
BattleHUDUISystem creates draggable Tower Draft item
    ↓
TowerPlacementSystem detects deployment or same-TowerFamily tower target intent
```

If the Tower Draft item is dropped on a valid deployment tile, TowerPlacementSystem places the new tower.

If the Tower Draft item is dropped on an existing tower with matching TowerFamily, TowerPlacementSystem forwards a tower level-up request to TowerUpgradeSystem.

---

### 9.2 Tower Upgrade Draft Result Flow

```text
Player selects tower upgrade
    ↓
DraftSystem validates selection
    ↓
DraftSystem creates draft result
    ↓
BattleHUDUISystem creates draggable Tower Upgrade Draft item
    ↓
TowerPlacementSystem detects target tower intent
    ↓
TowerUpgradeSystem validates and applies upgrade
```

### 9.3 Ownership Rules

Draft System owns:

- Draft workflow
- Draft choices
- Draft result generation

Battle HUD UI System owns:

- Draft Window UI
- Draft interaction UI
- Draggable draft item display
- Valid target highlight presentation

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

Player System owns ResolvedMonsterCount accumulation and level-up events.

Draft System may subscribe to:

```text
OnPlayerLevelUp
```

---

### Battle HUD UI System

Battle HUD UI System displays:

- Draft Window
- Draft choices
- Draggable draft items

Draft System should not directly manage runtime UI layout.

---

### Tower Upgrade System

Tower Upgrade System owns:

- Tower upgrade progression
- Upgrade application
- Upgrade layer definitions

Draft System may generate Tower Upgrade Draft results. TowerUpgradeSystem owns target validation, duplicate checks per tower, and final application.

---

### Tower Placement System

Tower Placement System only detects whether a dragged Draft item is targeting a deployment tile or an existing tower.

Tower Placement System should not generate draft choices, decide tower level-up rules, or apply tower upgrades.

---

### Monster System

Monster System may indirectly trigger Draft System progression through monster resolution reports sent to Player System.

Monster System should not directly interact with Draft System.

---

## 12. First Version Scope

Included features:

- Player level-up draft trigger
- 3-choice draft window
- Tower Draft
- Tower Upgrade Draft
- Random tower selection
- Draggable draft item creation
- Same-round duplicate prevention for displayed upgrade options
- Event-driven draft flow

Excluded features:

- Global Buff Draft
- Relic Draft
- Reroll system
- Weighted rarity
- Advanced synergy drafting
- Multiplayer drafting
- Persistent progression drafting
