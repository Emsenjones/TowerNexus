# Task 003 - Battle HUD and Tower Draft UI

---

# 1. Task Overview

This task implements the first version of the Battle HUD and Tower Draft UI for the Tower Deploy System.

The system is responsible for:

- Displaying current player level
- Displaying EXP progress
- Displaying the Tower Pending Deployment Area
- Opening the Tower Draft Window when level-up occurs
- Displaying 3 draft tower choices
- Allowing the player to select one tower
- Adding the selected tower into the Tower Pending Deployment Area

This task focuses only on UI logic and tower entry flow.

This task does not implement tower dragging, placement validation, map interaction, or tower deployment.

---

# 2. Related System Document

Reference document:

```text
Docs/02_TowerDeploySystem.md
```

Relevant sections:

- `# 4. Battle HUD System`
- `# 5. Tower Draft System`
- `# 7. Tower Pending Deployment Area`
- `# 12. Runtime State Management`
- `# 13. First Version Scope`

---

# 3. Goals

The goal of this task is to create the first playable UI flow for tower drafting.

After this task is completed:

- Player level and EXP are visible during gameplay
- The Tower Draft Window can open
- The Tower Draft Window displays 3 tower choices
- The player can select one tower
- The selected tower enters the Tower Pending Deployment Area
- Pending towers remain stored in the Battle HUD
- The system is ready for future drag placement tasks

---

# 4. Implementation Scope

This task includes:

- Battle HUD UI
- Level display UI
- EXP progress display UI
- Tower Pending Deployment Area UI
- Tower Draft Window UI
- Tower Draft Item UI
- Draft selection flow
- Pending tower entry creation
- UI open/close logic
- Basic runtime UI testing support

This task excludes:

- Tower dragging
- Tower placement preview
- Grid snapping
- Placement validation
- Map interaction
- Walkability update
- Pathfinding validation
- Tower combat logic
- Tower recycle system
- Save/load system

---

# 5. Suggested Runtime Responsibilities

This task should provide the following runtime responsibilities:

- Battle HUD runtime UI management
- Player level and EXP display handling
- Tower Pending Deployment Area UI management
- Tower Draft Window UI management
- Draft item generation and cleanup
- Draft selection flow handling
- Pending tower entry creation and removal
- Runtime-safe UI validation handling

Codex should first inspect the existing project structure before implementation.

The implementation may:

- extend existing UI scripts
- reuse existing HUD/runtime systems
- create new scripts if necessary

Avoid creating duplicate UI managers or window systems if equivalent responsibilities already exist.

Recommended script names:

```text
BattleHUDUI
TowerDraftUI
TowerDraftItemUI
PendingTowerItemUI
```

These names are recommendations only.
If a different architecture fits the existing project better, explain the reasoning before implementation.

---

# 6. Battle HUD UI

## 6.1 Overview

The Battle HUD is the always-visible gameplay UI shown during battle.

The first version should include:

| UI Element | Description |
|---|---|
| Current Level Text | Displays current player level |
| EXP Progress Bar | Displays EXP progress toward next level |
| Tower Pending Deployment Area | Displays selected but undeployed towers |

---

## 6.2 Required References

`BattleHUDUI` should contain references for:

| Field | Type | Description |
|---|---|---|
| levelText | TMP_Text | Displays current player level |
| expSlider | Slider | Displays current EXP progress |
| pendingTowerContainer | Transform | Parent object for pending tower UI entries |
| pendingTowerItemPrefab | GameObject | UI prefab used for pending tower entries |

---

## 6.3 Required Runtime Capability

The implementation must provide a way to:

- update displayed player level
- update EXP progress display
- add pending tower UI entries
- remove pending tower UI entries
- expose runtime-safe HUD update behavior

Recommended API:

```csharp
public void UpdateLevel(int level)
public void UpdateExp(float currentExp, float requiredExp)
public void AddPendingTower(TowerDefinition towerDefinition)
public void RemovePendingTower(PendingTowerItemUI item)
```

Equivalent implementations are acceptable if they better match the existing project architecture.

---

# 7. Tower Draft Window

## 7.1 Overview

The Tower Draft Window is opened when the player levels up.

The window temporarily blocks normal gameplay interaction.

The player must choose one tower before gameplay continues.

---

## 7.2 UI Structure

Recommended hierarchy:

```text
TowerDraftWindow
├── WindowTitle
├── DraftItemContainer
│   ├── TowerDraftItemUI
│   ├── TowerDraftItemUI
│   └── TowerDraftItemUI
└── BackgroundBlocker
```

---

## 7.3 Required References

`TowerDraftUI` should contain references for:

| Field | Type | Description |
|---|---|---|
| rootObject | GameObject | Root object used for show/hide |
| draftItemContainer | Transform | Parent object for draft items |
| towerDraftItemPrefab | GameObject | Draft item UI prefab |

---

## 7.4 Required Runtime Capability

The implementation must provide a way to:

- open the draft UI with runtime tower data
- clear previous draft entries before regeneration
- close and hide the draft UI safely
- block gameplay interaction while the draft UI is open

Recommended API:

```csharp
public void OpenDraft(List<TowerDefinition> towerDefinitions)
public void CloseDraft()
```

Equivalent implementations are acceptable if they better match the existing project architecture.

---

# 8. Tower Draft Item UI

## 8.1 Overview

Each Tower Draft Item represents one selectable tower option.

Each item displays:

- Tower icon
- Tower name
- Tower description

---

## 8.2 Required References

`TowerDraftItemUI` should contain references for:

| Field | Type | Description |
|---|---|---|
| iconImage | Image | Displays tower icon |
| nameText | TMP_Text | Displays tower name |
| descriptionText | TMP_Text | Displays tower description |
| button | Button | Used for tower selection |

---

## 8.3 Required Runtime Capability

The implementation must provide a way to:

- initialize draft item UI using tower data
- bind selection callback behavior
- return selected tower data to parent systems

Recommended API:

```csharp
public void Initialize(
    TowerDefinition towerDefinition,
    Action<TowerDefinition> onSelected
)
```

Equivalent implementations are acceptable if they better match the existing project architecture.

---

# 9. Pending Tower Item UI

## 9.1 Overview

Pending Tower Items represent towers stored in the Tower Pending Deployment Area.

The first version only needs visual display.

Future versions will support:

- Dragging
- Deployment
- Redeployment
- Recycle flow

---

## 9.2 Required References

`PendingTowerItemUI` should contain references for:

| Field | Type | Description |
|---|---|---|
| iconImage | Image | Displays tower icon |
| nameText | TMP_Text | Displays tower name |

---

## 9.3 Required Runtime Capability

The implementation must provide a way to:

- initialize pending tower UI entries
- display tower icon and name safely
- retain runtime tower data for future systems

Recommended API:

```csharp
public void Initialize(TowerDefinition towerDefinition)
```

Equivalent implementations are acceptable if they better match the existing project architecture.

# 14. Implementation Planning Requirement

Before implementation, Codex should:

1. Inspect the current project structure
2. Identify existing UI, HUD, runtime, or draft-related systems
3. Decide whether to:
   - extend existing scripts
   - create new scripts
   - refactor small existing structures
4. Explain the implementation plan before writing code

The implementation plan should include:

- scripts expected to change
- new scripts expected to be created
- responsibilities of each modified script
- reasoning for any newly created runtime systems

Do not start implementation before presenting the plan.

---

# 10. Draft Selection Flow

The first version draft workflow is:

1. Player levels up
2. Tower Draft Window opens
3. System generates 3 tower choices
4. Player selects one tower
5. Selected tower is added to Tower Pending Deployment Area
6. Tower Draft Window closes

The player does not immediately enter tower placement mode.

---

# 11. Runtime Interaction Rules

The following rules must be enforced:

- Only one Tower Draft Window may exist at a time
- Draft UI blocks gameplay interaction while open
- Selecting one tower immediately closes the draft window
- Selected towers remain in pending deployment area
- Pending tower entries persist until future deployment systems remove them

---

# 12. Validation Rules

The implementation must handle:

- Null TowerDefinition
- Missing icon
- Missing tower name
- Empty draft list
- Draft list smaller than 3 entries
- Duplicate draft entries
- Missing prefab references
- Missing UI references

If data is invalid:

- Do not crash
- Log warnings where appropriate
- Prevent invalid UI generation

---

# 13. Integration Notes

This task should integrate with:

- `PlayerLevelSystem`
- `TowerDefinition`
- Future Tower Placement System

However:

- Do not implement drag placement
- Do not implement map interaction
- Do not implement placement validation
- Do not implement walkability updates

This task only prepares the UI flow.

---

# 14. Testing Checklist

Use temporary runtime buttons or test methods to validate:

1. Open draft window manually
    - Window appears correctly

2. Generate 3 draft items
    - All items display correctly

3. Select one tower
    - Draft window closes
    - Pending tower entry appears

4. Add multiple pending towers
    - Pending area updates correctly

5. Update player level
    - Level UI updates correctly

6. Update EXP
    - EXP progress updates correctly

7. Missing icon
    - No crash
    - UI still functions

8. Missing description
    - No crash
    - Empty text allowed

9. Duplicate draft entries
    - System still functions

10. Empty draft list
    - Warning logged
    - No invalid UI generated

---

# 17. Out of Scope

Do not implement:

- Tower dragging
- Placement preview
- Grid snapping
- Placement validation
- Map interaction
- Walkability update
- Pathfinding validation
- Tower recycle system
- Tower combat logic
- Tower upgrade system
- Save/load system
- Multiplayer synchronization

---

# Change Log

## 2026-05-13

- Initial Battle HUD and Tower Draft UI task document created.
- Defined Battle HUD structure.
- Defined Tower Draft Window structure.
- Defined Tower Draft Item UI workflow.
- Defined Tower Pending Deployment Area workflow.
- Defined first-version draft selection flow.
- Reserved drag placement for future tasks.
- Refactored task structure to support architecture-driven AI workflow.