# 1. Project Introduction

Tower Nexus is a strategy tower defense game focused on terrain manipulation, tower drafting, and runtime battlefield reshaping.

Unlike traditional tower defense games where towers only act as combat units, towers in Tower Nexus also function as physical obstacles that affect monster pathfinding and battlefield topology.

The core experience is built around meaningful placement decisions, path manipulation, random build creation, and gradual tower growth during each run.

---

# 2. Core Gameplay Loop

The core gameplay loop is:

```text
Load Map
    ↓
Spawn Monster Waves
    ↓
Monsters Pathfind Toward Target
    ↓
Player Drafts Towers / Upgrades
    ↓
Player Places Towers
    ↓
Towers Occupy Nodes And Attack Monsters
    ↓
Monster Death Grants EXP
    ↓
Player Level Up Triggers Draft
    ↓
Repeat Until Victory Or Failure
```

Key runtime rules:

- Towers can attack monsters.
- Towers occupy grid nodes.
- Tower placement can change monster paths.
- Path-blocking validation prevents fully blocking all valid monster routes.
- Alive monsters recalculate paths when battlefield walkability changes.
- Monster death grants EXP.
- Player level-up triggers draft selection.

---

# 3. Configuration Strategy

Tower Nexus uses Unity ScriptableObject assets as the primary configuration solution.

Current first-version configuration assets include:

- TowerDefinition
- AttackConfig
- MonsterDefinition
- ProjectileConfig
- EffectConfig

Current combat configuration dependency flow:

```text
TowerDefinition
    ↓
AttackConfig
    ↓
ProjectileConfig
    ↓
EffectConfig
```

Future versions may additionally introduce:

- BuffConfig
- LevelConfig
- StageConfig

Odin Inspector may be used to improve configuration editing workflows, validation, and editor usability.

The first version does not use Excel export tools, CSV import pipelines, JSON generation workflows, or external data table workflows.

---

# 4. Core Design Philosophy

The core design philosophy of Tower Nexus is:

> Meaningful decisions create unique battlefield stories.

The game emphasizes:

- Terrain control
- Strategic tower placement
- Dynamic path manipulation
- Draft-based build creation
- Runtime adaptation
- Spatial planning

---

# 5. Core Systems Overview

## 5.1 Map System

Owns the grid-based battlefield foundation.

Responsible for:

- Grid nodes
- Walkability state
- Runtime topology updates
- Node queries
- Map visual refresh

Does not own tower placement rules, monster AI, combat logic, or draft logic.

---

## 5.2 Player System

Owns player runtime progression and survival state.

Responsible for:

- Player level
- EXP
- HP
- Level-up events
- Player death / battle failure

Other systems may react to player events, but Player System does not own tower placement, draft generation, monster movement, or UI implementation.

---

## 5.3 Battle HUD UI System

Owns runtime battle UI presentation.

Responsible for:

- Player battle info display
- Draft window presentation
- Pending tower deployment area
- Placement feedback presentation

Does not own player state, draft generation, placement validation, or combat logic.

---

## 5.4 Draft System

Owns runtime draft generation and draft result processing.

First-version draft categories:

- New Tower Draft
- Tower Upgrade Draft

Responsible for:

- Listening to player level-up events
- Generating draft choices
- Processing draft selection
- Creating pending tower entries

---

## 5.5 Tower Placement System

Owns tower placement and runtime battlefield topology modification.

Responsible for:

- Drag and snap placement
- Placement preview
- Placement validation
- Occupied node detection
- Walkability updates
- Path-blocking validation

Does not own tower combat, projectile behavior, effects, buffs, or upgrade logic.

---

## 5.6 Tower Framework System

Defines shared tower data and configuration references.

Responsible for:

- TowerDefinition
- AttackConfig
- Attack archetypes
- Target selection types
- Attack presentation configuration
- Animator presentation parameter definitions
- Tower prefab structure
- TowerAnchorSet
- Center Anchor
- Occupied Anchors

Runtime placement, combat execution, projectile behavior, effect execution, and upgrade behavior are owned by their respective systems.

---

## 5.7 Tower Runtime Combat System

Consumes TowerDefinition and AttackConfig data and converts them into runtime combat behavior.

Responsible for:

- Runtime tower combat state
- Enemy detection
- Target selection
- Attack cooldown management
- Channel attack state management
- Periodic area attack state management
- Attack execution
- Attack animation state control
- Attack visual effect hook triggering
- Projectile creation and initialization
- Damage dispatch coordination

Projectile lifecycle execution belongs to Projectile System.

---

## 5.8 Projectile System

Manages projectile lifecycle after a projectile has been created and initialized by Tower Runtime Combat System.

Responsible for:

- Projectile movement
- Projectile collision detection
- Projectile lifetime management
- Impact event generation
- Simple single-target projectile damage dispatch
- Projectile destruction

Does not own tower targeting, attack cooldowns, area damage resolution, buff application, or monster health logic.

---

## 5.9 Buff And Effect System

Handles projectile impact effects and future buff-based combat behaviors.

First version responsibility:

- EffectConfig
- AreaDamageEffectExecutor
- Area damage resolution for cannon-style projectile impacts

Direct projectile hit damage is not treated as an Effect in the first version.

Buff runtime behavior is reserved for future versions.

---

## 5.10 Monster System

Owns monster spawning, pathfinding, movement, runtime state, and death flow.

Responsible for:

- Monster wave spawning
- MonsterDefinition-driven configuration
- Runtime pathfinding
- Dynamic path recalculation
- Movement toward target
- Health and damage processing
- Death handling
- EXP reward generation
- Player damage reporting when monsters reach the target

---

# 6. Current Runtime Architecture Direction

```text
PlayerSystem
    ↓ OnPlayerLevelUp
DraftSystem
    ↓ Draft Result
BattleHUDUISystem
    ↓ Pending Tower Entry
TowerPlacementSystem
    ↓ Place Tower
TowerRuntimeCombatSystem
    ↓ Attack Execution / Projectile Creation
ProjectileSystem
    ↓ Direct Hit Damage / Impact Context
BuffAndEffectSystem
    ↓ AreaDamageEffect
MonsterSystem

TowerPlacementSystem
    ↓ Modify Walkability
MapSystem

MonsterSystem
    ↓ Pathfinding Queries
MapSystem
```

This separation is intended to:

- Improve maintainability
- Reduce system coupling
- Clarify responsibility ownership
- Simplify future feature expansion
- Improve AI-assisted development workflows

---

# 7. Current Development Focus

Current combat foundation goal:

```text
Deployable towers can attack monsters using four first-version attack archetypes.
```

First-version tower attack archetypes:

- StraightProjectile
- ArcProjectile
- ChannelBeam
- PeriodicArea

Immediate implementation order:

```text
Tower Framework Data Definitions
    ↓
Tower Prefab Structure Calibration
    ↓
Projectile Foundation
    ↓
Buff And Effect Foundation
    ↓
Tower Runtime Combat Foundation
    ↓
Combat Integration Verification
```

Current implementation status:

```text
✓ Projectile Foundation
✓ Buff And Effect Foundation
✓ Tower Runtime Combat Foundation
□ Combat Integration Verification
```

Tower Upgrade System is planned but not part of the current combat foundation implementation pass.

---