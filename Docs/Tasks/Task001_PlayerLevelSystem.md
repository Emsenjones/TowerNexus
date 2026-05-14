# Task 001 - Player Level System

---

# 1. Task Overview

This task implements the first part of the Tower Deployment System: the Player Level System.

The Player Level System is responsible for:

- Tracking player current level
- Tracking player current experience
- Defining required experience for each level
- Adding experience during runtime
- Triggering level-up when enough experience is accumulated
- Providing events for future systems such as Tower Draft UI

This task does not implement tower draft, tower selection, tower placement, or map interaction.

---

# 2. Related System Document

Reference document:

```text
Docs/02_TowerDeploymentSystem.md
```

Relevant sections:

- `# 3. Player Level System`
- `# 4. Tower Draft System`
- `# 11. First Version Scope`

---

# 3. Goals

The goal of this task is to create a reusable player level system that can be used by future Tower Deployment tasks.

After this task is completed:

- Player EXP can be added through code
- Player level can increase when enough EXP is accumulated
- Required EXP per level can be configured in Unity
- Other systems can listen to level-up events
- The system can be tested in Play Mode through public methods or inspector debug buttons

---

# 4. Implementation Scope

This task includes:

- `PlayerLevelConfig`
- `PlayerLevelSystem`
- EXP add logic
- Level-up detection
- Level-up event
- Basic runtime debug support

This task excludes:

- Tower Draft UI
- Tower Pool
- Tower Definition
- Tower Placement
- Map System changes
- Pathfinding validation
- Monster reward integration
- Save/load system

---

# 5. Suggested Runtime Responsibilities

This task should provide the following runtime responsibilities:

- Player level configuration
- Runtime player EXP tracking
- Runtime level-up handling
- Level-up event dispatching
- Debug EXP testing support

Codex should first inspect the existing project structure before implementation.

The implementation may:

- extend existing scripts
- reuse existing progression systems
- create new scripts if necessary

Avoid creating duplicate systems if equivalent runtime responsibilities already exist.

Recommended script names:

```text
PlayerLevelConfig
PlayerLevelSystem
```

These names are recommendations only.
If a different architecture fits the existing project better, explain the reasoning before implementation.

---

# 6. PlayerLevelConfig

## 6.1 Script Type

`PlayerLevelConfig` should be implemented as a `ScriptableObject`.

Recommended asset menu path:

```csharp
[CreateAssetMenu(
    fileName = "PlayerLevelConfig",
    menuName = "Tower Nexus/Tower Deployment/Player Level Config"
)]
```

---

## 6.2 Data Fields

| Field | Type | Description |
|---|---|---|
| expRequiredPerLevel | List<int> | Required EXP for each level-up step |

Example:

| Index | Meaning | Value |
|---|---|---|
| 0 | Level 1 → Level 2 | 10 |
| 1 | Level 2 → Level 3 | 20 |
| 2 | Level 3 → Level 4 | 40 |

---

## 6.3 Required Runtime Capability

The implementation must provide a way to:

- query required EXP for a given level
- prevent invalid level access
- prevent leveling beyond configured data

Recommended API:

```csharp
public int GetRequiredExpForLevel(int currentLevel)
```

Equivalent implementations are acceptable if they better match the existing project architecture.

---

# 7. PlayerLevelSystem

## 7.1 Script Type

`PlayerLevelSystem` should be implemented as a `MonoBehaviour`.

It should reference `PlayerLevelConfig`.

---

## 7.2 Runtime Fields

| Field | Type | Description |
|---|---|---|
| levelConfig | PlayerLevelConfig | EXP requirement configuration |
| currentLevel | int | Current player level |
| currentExp | int | Current accumulated EXP for current level |

Recommended initial values:

```csharp
currentLevel = 1;
currentExp = 0;
```

---

## 7.3 Public Properties

Expose read-only public properties:

```csharp
public int CurrentLevel { get; }
public int CurrentExp { get; }
public int RequiredExp { get; }
```

`RequiredExp` should return the required EXP for current level from `PlayerLevelConfig`.

---

## 7.4 Events

Implement the following events:

```csharp
public event Action<int> OnLevelChanged;
public event Action<int, int> OnExpChanged;
public event Action<int> OnLevelUp;
```

Event meanings:

| Event | Parameters | Description |
|---|---|---|
| OnLevelChanged | newLevel | Triggered when current level changes |
| OnExpChanged | currentExp, requiredExp | Triggered when EXP changes |
| OnLevelUp | newLevel | Triggered when player levels up |

---

# 8. EXP Add Logic

Implement:

```csharp
public void AddExp(int amount)
```

Expected behavior:

1. If `amount <= 0`, do nothing.
2. Add amount to `currentExp`.
3. Check whether currentExp reaches required EXP.
4. If currentExp >= requiredExp:
    - Subtract required EXP from currentExp
    - Increase currentLevel by 1
    - Trigger level-up events
5. Support multiple level-ups from one large EXP gain.
6. Stop leveling up if no more required EXP is configured.

Example:

```text
Current Level: 1
Current EXP: 0
Required EXP: 10
Add EXP: 25

Result:
Level 2 reached, remaining EXP 15
Level 3 reached, remaining EXP depends on Level 2 requirement
```

---

# 9. Level-Up Logic

Recommended internal handling:

- repeatedly checks whether current EXP reaches level-up requirement
- supports multiple level-ups
- dispatches level-up events in correct order

A dedicated internal method such as:

```csharp
private void TryLevelUp()
```

is recommended but not mandatory.
# 13. Acceptance Criteria

# 13. Implementation Planning Requirement

Before implementation, Codex should:

1. Inspect the current project structure
2. Identify existing progression or player-related systems
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

# 14. Acceptance Criteria

---

# 10. Debug Support

Add one public debug method:

```csharp
public void DebugAddExp(int amount)
```

This method should call:

```csharp
AddExp(amount);
```

This is only for Play Mode testing and future test scene support.

Optional inspector debug fields may be added if useful, but do not create custom editor tools in this task.

---

# 11. Validation Rules

The implementation must handle:

- Missing `PlayerLevelConfig`
- Empty EXP requirement list
- Invalid current level
- EXP added with zero or negative value
- EXP gain large enough to trigger multiple level-ups
- EXP gain after reaching max configured level

If config is missing or invalid:

- Do not crash
- Log a warning
- Prevent level-up

---

# 12. Integration Notes

This task only prepares level-up events.

Future tasks will use `OnLevelUp` to open the Tower Draft UI.

Do not implement the draft UI in this task.

Do not create tower-related scripts in this task.

---

# 13. Acceptance Criteria

This task is complete when:

- `PlayerLevelConfig` can be created as a ScriptableObject asset
- Required EXP per level can be configured in Unity Inspector
- `PlayerLevelSystem` can reference the config
- `AddExp(int amount)` correctly increases EXP
- Player levels up when EXP requirement is reached
- Multiple level-ups from one EXP gain are supported
- Level-up stops when config data ends
- Events are triggered correctly
- Unity Console has no compile errors
- No Tower Draft UI, tower placement, or map logic is implemented

---

# 15. Testing Checklist

Use a temporary scene object with `PlayerLevelSystem`.

Test the following cases:

1. Add EXP below requirement
    - Level should not change
    - EXP should increase

2. Add EXP equal to requirement
    - Level should increase by 1
    - EXP should reset or carry remaining value correctly

3. Add EXP greater than requirement
    - Level should increase
    - Remaining EXP should be preserved

4. Add large EXP amount
    - Multiple level-ups should occur if configured

5. Add EXP after max level
    - No crash
    - No invalid level-up

6. Missing config
    - No crash
    - Warning is logged

7. Empty config
    - No crash
    - Warning is logged

---

# 16. Out of Scope

Do not implement:

- Tower Draft UI
- Tower Pool
- TowerDefinition
- Tower Anchor System
- Tower Placement
- Map walkability update
- Pathfinding validation
- Monster EXP reward binding
- Save/load
- Custom editor windows

---

# Change Log

## 2026-05-13

- Initial Player Level System task document created.
- Defined PlayerLevelConfig ScriptableObject.
- Defined PlayerLevelSystem runtime behavior.
- Defined EXP add and level-up logic.
- Defined level-up event requirements.
- Defined first-version testing checklist.
- Refactored task structure to support architecture-driven AI workflow.