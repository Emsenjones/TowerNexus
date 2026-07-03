# 1. Project Introduction

Tower Nexus is a draft-driven tower defense roguelike focused on terrain manipulation, tower drafting, and runtime battlefield reshaping.

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
Monsters Are Resolved
    ↓
ResolvedMonsterCount Advances Level Progress
    ↓
Player Level Up Triggers Draft
    ↓
Repeat Until Victory Or Failure
```

Key runtime rules:

- Towers can attack monsters.
- Towers occupy grid nodes.
- Tower placement can change monster paths.
- Path-blocking validation is part of the intended placement rule set and prevents fully blocking all valid monster routes when enabled.
- Alive monsters recalculate paths when battlefield walkability changes.
- Monsters are resolved when they are killed or when they reach the destination.
- ResolvedMonsterCount advances player level progress.
- Player health and player level progress are separate.
- Player level-up triggers draft selection.

---

# 3. Configuration Strategy

Tower Nexus uses Unity ScriptableObject assets as the primary configuration solution.

Current first-version configuration assets include:

- TowerDefinition
- AttackConfig
- TowerLevelConfig
- MonsterDefinition
- ProjectileConfig
- EffectConfig
- TowerUpgradeDefinition

Presentation-oriented prefab references may live in the configuration asset that owns the runtime event.

Examples:

- AttackConfig owns tower attack presentation hooks such as attack release VFX.
- The tower visual ownership path owns tower-side success feedback hooks such as model spawn or upgrade-applied VFX.
- ProjectileConfig owns projectile-specific presentation hooks such as optional impact VFX.
- Gameplay Effect data owns reusable gameplay effect rules and should not be required for purely visual projectile impact feedback.

Current combat configuration dependency flow:

```text
TowerDefinition
    ↓
AttackConfig
    ↓
ProjectileConfig
```

ProjectileConfig may reference gameplay effect data for projectile impact results that need reusable Effect execution.

TowerUpgradeDefinition owns runtime upgrade content such as Basic stat deltas, Behaviour packages, future Elemental profiles, and future Effect bindings. Upgrade content should not be mixed into TowerDefinition or AttackConfig.

Current first-version tower lineup:

- Archer Tower
- Cannon Tower
- Magic Tower
- Drone Tower

Archer Tower and Cannon Tower keep their existing functional direction.

Magic Tower is redesigned around a persistent orbiting Magic Orb rather than a channel beam.

Drone Tower is redesigned around an autonomous Drone that orbits selected target monsters and fires projectile bursts.

Watch Tower is removed from the current first-version tower lineup and replaced by Drone Tower.

Future versions may additionally introduce:

- BuffConfig
- EffectDefinition
- BuffDefinition
- EffectZoneDefinition
- PlayerLevelConfig
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
- ResolvedMonsterCount / level progress
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

- Tower Draft
- Tower Upgrade Draft

Responsible for:

- Listening to player level-up events
- Generating draft choices
- Processing draft selection
- Creating draft results
- Routing Tower Draft deployment or tower-level-up intent to the appropriate system

Tower Upgrade Draft choices are generated from eligible tower instance state, including TowerFamily, tower level, Required Tower Level eligibility, remaining upgrade slots, and upgrades already applied to each tower.

Future Elemental Layer upgrade choices should only enter the Tower Upgrade Draft pool when at least one deployed tower can legally receive that Elemental upgrade. A typical first rule is that the battlefield must contain a tower that satisfies the required tower level and does not already own an Elemental upgrade.

---

## 5.5 Tower Placement System

Owns tower placement and runtime battlefield topology modification.

Responsible for:

- Drag and snap placement
- Placement preview
- Tower Draft level-up preview request flow
- Attack range preview request flow during Draft item drag
- Current Drag Operation cancellation flow
- Placement validation
- Occupied node detection
- Walkability updates
- Path-blocking validation integration with Monster System pathfinding

Does not own tower combat, projectile behavior, effects, buffs, or upgrade logic.

---

## 5.6 Tower Framework System

Defines shared tower data and configuration references.

Responsible for:

- TowerDefinition
- AttackConfig
- TowerLevelConfig
- Attack archetypes
- Attack Entity concepts
- Target selection types
- Attack presentation configuration
- Tower model presentation contract
- Tower prefab structure
- Tower visual structure
- Tower visual VFX anchors
- TowerVisualController ownership direction
- TowerAnchorSet
- Center Anchor
- Occupied Anchors

Runtime placement, combat execution, projectile behavior, effect execution, and upgrade behavior are owned by their respective systems.

Tower-side visual feedback is presentation-only. Deploy success and tower level-up model refresh may share a model spawn or refresh feedback category, while applying a TowerUpgradeDefinition may use a separate upgrade-applied feedback category. Gameplay systems decide whether the underlying action succeeds; tower-owned visual presentation handles the local feedback playback.

---

## 5.7 Tower Runtime Combat System

Consumes TowerDefinition and AttackConfig data and converts them into runtime combat behavior.

Responsible for:

- Runtime tower combat state
- Enemy detection
- Target selection
- Attack cooldown management
- Attack Entity spawning or control
- Magic Orb lifecycle orchestration
- Drone launch, target orbit, burst fire, return, and recharge orchestration
- Attack execution
- Attack presentation request timing
- Attack visual effect hook triggering
- Current active AttackOrigin consumption
- Projectile creation and initialization
- Damage dispatch coordination

Tower Runtime Combat decides when an attack happens.

Attack Entities decide how the attack behaves.

Tower level data provides basic damage. Runtime upgrade state provides damage bonuses. Runtime combat and Attack Entity logic use resolved damage values before dispatching damage.

Tower Runtime Combat consumes the current active AttackOrigin and tower model presentation entry resolved by the tower visual/runtime layer. It does not own tower model replacement, model presentation resolution, or AttackOrigin fallback resolution.

Projectile lifecycle execution belongs to Projectile System when the Attack Entity is a projectile.

Attack Entities may emit gameplay trigger context when they hit, contact, or impact a monster or position. Runtime Combat and Attack Entities should not own Buff, Effect, elemental stack, or overload rules.

---

## 5.8 Projectile System

Manages projectile lifecycle after a projectile has been created and initialized by Tower Runtime Combat System.

Responsible for:

- Projectile movement
- Projectile hit detection
- Projectile lifetime management
- Impact event generation
- Projectile-specific impact visual effect triggering
- Simple single-target projectile damage dispatch
- Projectile destruction

Does not own tower targeting, attack cooldowns, area damage resolution, buff application, or monster health logic.

Projectile impact VFX is presentation-only and should not affect hit detection, damage dispatch, area damage execution, or projectile lifetime rules.

Projectile is a shared runtime concept for projectile-style Attack Entities.

Current first-version projectile flight behaviors:

- Direction
- Arc
- Tracking

Future projectile-style attacks should extend this shared framework whenever practical.

Projectile prefab roots follow the shared runtime orientation convention: local +Y Up and local +Z Forward.

---

## 5.9 Buff And Effect System

Handles reusable gameplay effects, future buff runtime, future Elemental debuff stacking, EffectZone execution, and complex combat results beyond simple direct damage.

Framework direction:

- Trigger context consumption from Attack Entities, projectiles, zones, and buffs
- Radius-based target resolution
- Effect action execution
- Buff application and lifecycle when buff runtime is in scope
- Elemental stack, overload, and same-element stack immunity when Elemental Layer is in scope
- EffectZone duration, tick, targeting, and movement when zone gameplay is in scope

Direct base attack damage does not need to migrate into Buff And Effect System immediately. The current direct damage path may remain simple while Buff And Effect System executes additional effects, buff ticks, zone ticks, overload damage, and other complex results.

Projectile impact VFX is not owned by the Buff And Effect System. It is configured through ProjectileConfig and triggered by the Projectile System when impact occurs.

Purely visual impact feedback should not require gameplay Effect data.

---

## 5.10 Monster System

Owns monster spawning, pathfinding, movement, runtime state, death flow, arrival flow, and monster resolution reporting.

Responsible for:

- Monster wave spawning
- MonsterDefinition-driven configuration
- Runtime pathfinding
- Dynamic path recalculation
- Movement toward target
- Health and damage processing
- Death handling
- Monster resolution reporting
- Player damage reporting when monsters reach the target
- Monster health bar runtime presentation
- Monster hit feedback presentation

---

# 6. Current Runtime Architecture Direction

```text
PlayerSystem
    ↓ OnPlayerLevelUp
DraftSystem
    ↓ Draft Result
BattleHUDUISystem
    ↓ Drag / Selection Intent
TowerPlacementSystem
    ↓ Placement Or Target Intent
TowerUpgradeSystem
    ↓ Tower Level / Upgrade Application
TowerRuntimeCombatSystem
    ↓ Resolved Stats / Behaviour Execution
ProjectileSystem
    ↓ Hit Detection / Impact Event
BuffAndEffectSystem
    ↓ Effect / Buff / Elemental Resolution
MonsterSystem

TowerPlacementSystem
    ↓ Modify Walkability
MapSystem

MonsterSystem
    ↓ Monster Resolved
PlayerSystem

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

ProjectOverview is intended to provide general project context and system relationship references.

Detailed behavior, current scope, and task-level implementation notes should live in the dedicated System Documents and Task Documents.

Use the System Documents as the source of truth for each area:

| Area | System Document |
|---|---|
| Runtime player progression and survival | `01_PlayerSystem.md` |
| Battle UI presentation | `02_BattleHUDUISystem.md` |
| Grid, walkability, and map visuals | `03_MapSystem.md` |
| Monster spawning, movement, pathfinding, health, and feedback | `04_MonsterSystem.md` |
| Runtime draft generation and draft results | `05_DraftSystem.md` |
| Tower placement and battlefield topology updates | `06_TowerPlacementSystem.md` |
| Tower data, tower structure, attack configuration, and target selection types | `07_TowerFrameworkSystem.md` |
| Runtime tower combat behavior | `08_TowerRuntimeCombatSystem.md` |
| Projectile lifecycle and impact handling | `09_ProjectileSystem.md` |
| Tower growth and upgrade concepts | `10_TowerUpgradeSystem.md` |
| Effects, buffs, Elemental rules, and EffectZone behavior | `11_BuffAndEffectSystem.md` |

Task Documents under `Docs/Task/` are temporary implementation references.

After a Task implementation is completed, its Task Document may be removed while the corresponding System Document remains as the long-term reference.

---
