# Tower Nexus - Player System

---

# 1. Purpose And Ownership

Player System is the authority for battle-local player progression and survival state.

It owns:

- Current player level
- Resolved Monster progress toward the next level
- Current and maximum health
- Level-up transitions
- Defeat state
- State-change notifications consumed by Draft and UI systems

It does not own Monster resolution detection, Draft generation, battle UI, Tower placement, Stage flow, or combat.

---

# 2. Runtime State

| State | Contract |
|---|---|
| Current Level | Current battle progression level |
| Resolved Progress | Progress retained toward the next level |
| Current Health | Remaining player survival value |
| Maximum Health | Runtime health cap supplied by the selected Stage and used as its full-health start value |
| Defeated | Terminal player-survival state for the battle |

Player progress and health are independent state resolved through one Monster-resolution report. The report contains whether that Monster reached the Target so Player System can update both values atomically.

---

# 3. Resolved Monster Progression

Monster System reports one resolution when a Monster dies or reaches the Target. The report includes the Target-arrival fact. Every accepted resolution contributes exactly one point of Player progress while the battle remains active.

```text
Receive Monster Resolution
    -> Add One To Resolved Progress
    -> While Next-Level Requirement Is Met
        -> Subtract Requirement
        -> Increase Player Level
    -> If Monster Reached Target
        -> Reduce Current Health By One
    -> Determine Defeat
    -> Publish Coherent Progress, Level, And Health State
    -> If Defeated
        -> Publish Defeat
        -> Do Not Publish Interactive Level-Up Opportunities
    -> Otherwise
        -> Publish Level-Up Opportunities
```

Progress carries across multiple level thresholds. At maximum player level, additional resolution does not create further level-ups.

## 3.1 Player Level Configuration

Player level configuration stores the ordered resolved-progress requirement for each transition:

```text
Level 1 Requirement -> Level 2
Level 2 Requirement -> Level 3
...
```

Requirements must be positive. Missing next-level data means the current level is the configured maximum.

---

# 4. Player Health And Defeat

The Monster-resolution report contains whether the Monster reached the Target. Each accepted Target arrival reduces Player health by exactly one in the same transaction that advances resolved progress.

```text
Receive Monster Resolution With Target Arrival
    -> Add One To Resolved Progress
    -> Resolve Level Thresholds
    -> Reduce Current Health By One
    -> Clamp To Valid Range
    -> Health Reaches Zero
        -> Enter Defeated State Once
```

All state mutation completes before observers receive state-change notifications. A Monster that causes defeat still contributes its progress and may cross a level threshold, but defeat suppresses any interactive Draft opportunity produced by that transaction.

After defeat:

- Further health damage is ignored.
- Defeat is not published again.
- Further Monster resolution does not advance player progress.
- The current battle simulation stops producing new gameplay results.

Stopping the current battle does not imply a defeat screen, Stage transition, restart flow, or persistence rule. Game Flow will later decide what transition follows defeat.

Each newly composed Stage battle starts with fresh Player level progress, defeat state, and the positive maximum health authored by its StageDefinition. Current health starts equal to that maximum. Player state and maximum health from a previous Stage battle are not reused.

---

# 5. State-Change Outputs

Player System exposes semantic notifications for:

- Resolved progress changed
- Player level increased
- Player health changed
- Player entered defeated state

Draft System consumes level-up opportunities. Battle HUD UI System consumes player state changes for presentation. Consumers cannot mutate Player state through those notifications.

---

# 6. Validation

Player configuration validation should report at minimum:

- Rejection of a non-positive Stage-supplied maximum health
- Starting health outside the valid range
- Non-positive level requirements
- Missing or ambiguous maximum-level progression data
- Invalid duplicate Monster resolution or Target-arrival reports

---

# 7. Approved Scope And Deferred Topics

Current scope includes battle-local level, one-point-per-resolution progress, health, one damage per Target arrival, level-up opportunities, defeat state, and stopping the current battle on defeat.

Deferred topics include player attributes, active skills, passive abilities, shields, damage resistance, talents, base meta-upgrades, persistent progression, defeat presentation, restart, and the broader Game Flow response to victory or defeat.
