# Tower Nexus - Project Overview

Document Set: System

---

# 1. Project Identity

Tower Nexus is a tower-defense roguelite in which towers are both combat units and physical parts of the battlefield.

The player builds a temporary strategy through Draft choices while tower placement changes monster movement routes. Each playable Stage combines one authored Map, one Monster Wave configuration, and the Tower and Tower Upgrade content available during that battle.

The project is designed around four connected decisions:

- Which Tower or Tower Upgrade to select
- Where to place or improve a Tower
- How placement changes battlefield topology
- How Tower packages and Elemental effects compose during combat

The guiding design principle is:

> Meaningful decisions create unique battlefield stories.

---

# 2. Core Game Loop

The Demo game loop is:

```text
Enter Main Menu
    -> Start New Run At First Stage
    -> Prepare Current Stage
        -> Establish Fresh Player State From Current Stage Health And Progress Requirements
        -> Establish Current Map, 3D Camera Boundary, Wave, And Draft Content
        -> Reset Camera To Default Active-Map Framing
    -> Show Optional Stage Introduction
    -> Begin Battle
        -> Complete One Initial Tower Draft
        -> Begin Monster Wave Execution
    -> Continue Until Victory Or Player Defeat
    -> Victory
        -> Prepare Next Stage
        -> Final Stage Returns To Main Menu
    -> Defeat
        -> Retry Current Stage
        -> Or Return To Main Menu
```

Within one active Stage, the battle loop is:

```text
Complete Initial Tower Draft
    -> Pause Battle Simulation While The Draft Window Is Open
    -> Create One Held Tower Draft Item
    -> Resume Battle Simulation
    -> Begin First Wave Delay
    -> Spawn Monster Waves
    -> Towers Resolve Combat
    -> Resolved Monsters Advance Player Progress
    -> Player Level-Up Opens Draft
    -> Select Tower Or Tower Upgrade
    -> Place Or Improve Tower
    -> Battlefield And Build Evolve
    -> Continue Until Battle Result
```

Core flow and battle rules:

- A new run begins from the first Stage in the ordered Demo sequence.
- One selected Stage composition is active during one battle.
- Every initial Stage, next Stage, and retry prepares fresh Player state from the selected Stage's positive maximum health and ordered Player progress requirements.
- Every initial Stage, next Stage, and retry binds the selected Map's authored 3D Camera movement boundary and restores the authored default Camera framing without inheriting prior Pan displacement.
- Battle gameplay remains inactive during Stage preparation and optional Stage Introduction.
- Every fresh Stage battle grants exactly one Initial Tower Draft after Battle start permission and before Monster Wave execution begins.
- The Initial Tower Draft creates one held Tower Draft item without changing Player level or progress.
- The first Wave Delay begins only after the Initial Tower Draft selection is accepted; Tower deployment itself may occur during that delay.
- Every Initial or Player level-up Draft pauses battle simulation while its Draft Window is open. Draft presentation and selection remain interactive, and the prior simulation rate is restored before gameplay resumes.
- Monsters enter from the Map's Spawn node and attempt to reach its Target node.
- A Monster that dies or reaches the Target is resolved exactly once.
- Every accepted Monster resolution advances player level progress by one.
- A Monster reaching the Target also reduces player health by one.
- Player level progress and player health are independent state.
- Player level-up opens a Draft choice.
- Placed Towers occupy Grid Nodes and can change effective walkability.
- A legal placement must not violate occupancy or approved route rules.
- Victory requires normal completion of all configured spawning, no alive unresolved Monsters, and a Player who is not defeated.
- Player health reaching zero produces Defeat and takes precedence when the final Monster resolution could otherwise satisfy Victory.
- One Battle claims at most one terminal state: Victory, Defeat, or result-neutral Technical Failure.
- Stop, release, replacement, retry, and preparation rollback are lifecycle cancellation rather than terminal Battle states.
- A result-neutral Technical Failure after Battle start closes gameplay authority, releases the current Stage, and returns Game Flow directly to Main Menu without publishing Victory or Defeat.

Unlocking, persistence, Stage Selection UI, branching progression, and Scene flow remain deferred.

---

# 3. Configuration Model

Reusable gameplay content is data-driven. A concrete engine may represent definitions as data assets and reusable runtime structures as prefabs, templates, or equivalent authored objects.

Current domain definitions include:

- StageDefinition
- MapVisualTheme
- MonsterWaveConfig
- TowerDefinition and TowerLevelConfig
- TowerUpgradeDefinition
- EffectDefinition
- BuffDefinition

Stage composition follows this relationship:

```text
StageDefinition
    + Player Maximum Health
    + Player Progress Requirements
    + Map Template
        + MapVisualTheme
        + Authored 3D Camera Movement Boundary
        + Authored Default Camera Pose
    + MonsterWaveConfig
    + Tower Draft Pool
    + Tower Upgrade Draft Pool
    + Optional Stage Introduction Content
```

Monster Wave configuration follows this relationship:

```text
MonsterWaveConfig
    + Ordered Waves
        + Wave Delay
        + Monster Runtime Template
        + Count
        + Spawn Interval
```

The demo uses one directly referenced runtime template for each unique Monster
type. Separate balance definitions or multiple stat variants sharing one visual
Monster are deferred until the content model requires them.

The demo is expected to contain at least six independently authored StageDefinitions. Each battle composes one selected StageDefinition.

Combat configuration follows this relationship:

```text
TowerDefinition
    + Per-Level Model And BasicDamage Data
    + Tower Runtime Template
        + Range, Cycle, And Targeting Authoring
        + Attack Entity References

TowerUpgradeDefinition
    + Eligibility And Layer Identity
    + Stat Or Behaviour Package Data
    + Optional Elemental Identity
```

The three Upgrade layers remain separate balance authorities. Basic changes
deterministic Tower stats. Behaviour changes attack topology, coverage, and
package gameplay but its added or extended results do not create ordinary
Elemental applications. Elemental authoring supplies the application Effect and
positive stack contribution used only by the baseline primary attack path.
Once ElectricShock or Windcut exists on a Monster, every successful Tower-owned
damage result may offer that shared Buff one Elemental hit-reaction opportunity;
this broader reaction permission never grants Buff application or stack
contribution.

Definitions and runtime templates store reusable authored truth. Per-instance runtime state, consumed history, timers, pending actions, and active entity state must not be written back into reusable authored content.

Tower-owned damage resolves from the source Tower's current Level-authored BasicDamage plus applied Basic Damage Bonus, multiplied by one stable DamageScale at the actual damage boundary. Periodic, Overload, Protection, persistent, and shared Electric/Wind Elemental hit-reaction damage uses independently authored FixedDamage. The Tower that produced a hit is diagnostic context for a shared reaction and never supplies its BasicDamage to that reaction. Already resolved damage is never replayed after a Level or Upgrade change.

Elemental balance therefore treats one Element's shared normal Buff behavior as
an absolute fixed-value package rather than a percentage multiplier on its
carrying Tower. Tower-family stack contribution adjusts only the opportunity to
reach Overload. Matching sources with overlapping effective coverage create the
conditional cooperation ceiling through more reliable Overload; Stage pressure
later tests the value of that build without becoming Elemental runtime
authority.

---

# 4. System Ownership

## 4.1 Game Flow System

Owns the ordered Demo Stage sequence, the current position in one game run, legal transitions among main menu, Stage preparation, optional introduction, battle, victory, and defeat, and the resulting next-Stage, retry, or return-to-main-menu decision.

It selects the StageDefinition supplied to Stage System and consumes one authoritative Battle result. It does not compose Stage content, mutate Player state, execute Waves, resolve Monsters, generate Drafts, or own combat behavior.

## 4.2 Stage System

Owns composition of the StageDefinition selected for the current battle. It establishes the active Map, supplies its authored 3D Camera movement boundary, requests fresh default Camera framing, and supplies the selected Player, Wave, and Draft content before battle runtime begins.

It establishes a prepared Stage with fresh Player and Camera framing state, then waits for Game Flow start permission. It does not own Stage ordering, result transitions, Camera movement, Map behavior, Wave execution, or Draft generation.

## 4.3 Player System

Owns battle-local player level, level progress, health, level-up notification, defeat state, and the runtime snapshot and consumption of the selected Stage's Player progress requirements. StageDefinition owns the reusable authoring of those requirements.

It does not own Draft generation, UI presentation, Monster lifecycle, or Stage flow.

## 4.4 Battle HUD UI System

Owns presentation and interaction for battle information, Draft choices, held Draft items, drag feedback, and placement feedback.

It observes or forwards domain intent but does not own player state, Draft rules, Draft-driven simulation pause, placement validation, combat outcomes, or Game Flow presentation.

## 4.5 Map System

Owns the authored grid battlefield, Grid Node state, effective walkability, spatial queries, Map presentation generation, one authored 3D Camera movement boundary, one authored default Camera pose, runtime Tile topology refresh, and Map validation.

It does not own Stage selection, Camera movement, pathfinding algorithms, Tower placement rules, Monster behavior, or battle flow.

## 4.6 Camera System

Owns battle-local framing of the Active Map, direct-manipulation pan input, enforcement of the Map-authored 3D Camera movement boundary, and framing reset when the Active Map changes.

It consumes Map framing data, one authored 3D movement boundary, and battle-interaction availability without owning Map state, UI interaction, Tower placement, Monster behavior, or Game Flow transitions.

## 4.7 Monster System

Owns Wave execution, Monster spawning, normal spawning-completion reporting, health, movement, pathfinding, runtime state, death, Target arrival, exactly-once resolution reporting, post-resolution alive-Monster state, and Monster-local presentation state.

It consumes Map data and Stage-selected Wave configuration without owning them. It supplies battle-completion facts without deciding Victory or Defeat.

## 4.8 Draft System

Owns Draft candidate gathering, eligibility-aware weighting, pending reservation, sampling, displayed-choice deduplication, explicit Draft workflow phase and session identity, Draft-driven battle-simulation pause, and Draft result creation.

It consumes the Stage-specific Tower and Tower Upgrade pools and forwards the selected result to the appropriate gameplay owner.

## 4.9 Tower Placement System

Owns drag placement intent, Grid alignment, placement preview, placement validation, Tower target intent, occupancy commit, and the resulting Map topology update request.

It does not own Tower combat, Tower Upgrade eligibility, Map data, or pathfinding execution.

## 4.10 Tower Framework System

Owns shared Tower identity, per-level BasicDamage and model data, authored non-damage base combat data, attack archetype identity, targeting categories, Tower template structure, anchors, and visual ownership contracts.

It defines what a Tower is, not how a placed Tower executes combat.

## 4.11 Tower Runtime Combat System

Owns combat orchestration for placed Towers: target acquisition, attack timing, confirmation and release boundaries, Attack Entity release, active entity ownership, the baseline-primary-only Elemental opportunity boundary, technical cleanup, and approved runtime refresh coordination.

It decides when attacks are released. Released Attack Entities own their domain behavior.

## 4.12 Projectile System

Owns projectile-style Attack Entities after release: movement, hit detection, lifetime, impact facts, projectile-specific results, presentation hooks, and completion.

It does not own Tower targeting, Tower cooldowns, reusable Effect execution, Buff lifecycle, or Monster health state.

## 4.13 Tower Upgrade System

Owns Stage-bound Tower level eligibility, Tower growth and upgrade definitions, layer capacity, duplicate rules, accepted state changes, and upgrade application.

It records what a Tower has gained. Runtime owners execute the resulting combat or presentation behavior.

## 4.14 Effect System

Owns reusable one-shot gameplay resolution such as damage actions, target queries, Buff application requests, and the reviewed WindVortex entity.

It does not own persistent Buff state, projectile flight, or source-system scheduling.

## 4.15 Buff System

Owns persistent Monster-attached state: duration, refresh, stacking, periodic timing, lifecycle bindings, Elemental overload, Protection, status presentation data, and persistent Buff presentation.

It may request one-shot Effects but does not duplicate Effect execution.

---

# 5. Runtime Relationship

```text
Game Flow
    -> Select Current StageDefinition
        -> Stage Composition
            -> Fresh Player State
                -> Stage Maximum Health
                -> Stage Progress Requirements
            -> Active Map
                -> Authored 3D Camera Movement Boundary
                -> Default Camera Framing Reset
            -> Monster Wave Configuration
            -> Stage Draft Pools
            -> Prepared Stage
        -> Optional Stage Introduction
        -> Begin Battle
            -> Initial Tower Draft
                -> Pause Battle Simulation
                    -> Held Tower Draft Item
                        -> Resume Battle Simulation
                            -> Authorize Monster Wave Execution

Battle Completion Facts
    -> Authoritative Victory Or Defeat
        -> Stop Battle Runtime Output
        -> Game Flow Transition
            -> Next Stage
            -> Retry Current Stage
            -> Main Menu

Monster Resolution
    -> Player Progress
        -> Level-Up Draft Request
            -> Pause Battle Simulation
                -> Draft Choice
                    -> Resume Battle Simulation
                        -> Placement Or Tower Upgrade Intent

Result-Neutral Battle Runtime Failure
    -> Close Battle Gameplay Authority
        -> Release Current Stage
            -> Main Menu

Active Map And Authored 3D Camera Movement Boundary
    -> Default Camera Framing
        -> Camera Framing And Bounds
            -> Eligible Battlefield Pan Gesture
                -> Camera Translation

Tower Placement
    -> Runtime Occupancy
        -> Effective Walkability
            -> Tile Topology Refresh
            -> Monster Path Recalculation

Tower Runtime Combat
    -> Attack Entity
        -> Projectile / Magic Orb / Drone Behavior
            -> Effect Request
                -> Buff Request Or One-Shot Result
                    -> Monster State
```

The arrows describe information or intent flow, not object ownership. Each receiving system remains responsible for validating and executing its own domain rules.

---

# 6. Documentation Contract

The complete `Doc/` folder is the long-term design source of truth. `Doc/README.md` defines the document sets and their authority. Another team should be able to reproduce approximately the same game and system behavior in a different programming language or game engine by following these documents.

System Documents under `Doc/System/` describe:

- Purpose and ownership
- Stable data and authoring contracts
- Gameplay and runtime invariants
- Inputs, outputs, and system boundaries
- Validation rules
- Approved scope and explicitly deferred topics

They do not store task status, concrete method names, engine lifecycle callbacks, subscription order, third-party library choices, Inspector tooling, change history, or unapproved future brainstorming.

Engine-specific structures remain only when they are intentional content-authoring contracts. Stable formulas, hierarchy semantics, data schemas, and behavior flows are valid System Document content because they are required to reproduce the design.

Balance sources under `Doc/Balance/` describe cross-Stage growth intent plus the campaign learning arc, new content, Reference Builds, required capabilities, and expected Anti-patterns. Exact Map size, Draft budgets, placement pressure, progression, Draft pools, Upgrade values, and Wave values are downstream implementation results derived by Task Documents and authored in their owning Unity assets.

Task Documents under `Doc/Task/` are implementation contracts for a bounded development slice. They may contain code-level decisions and derived working values. They may be retired after durable intent or contract changes are synchronized into the owning Blueprint or System Document and accepted executable values are authored in their owning Unity assets.

---

# 7. System Document Index

| Area | Source Of Truth |
|---|---|
| Game flow, Stage sequencing, and result transitions | `01_GameFlowSystem.md` |
| Stage composition and configuration distribution | `02_StageSystem.md` |
| Player progression and survival | `03_PlayerSystem.md` |
| Battle UI presentation and interaction | `04_BattleHUDUISystem.md` |
| Grid, walkability, and Map presentation | `05_MapSystem.md` |
| Active-Map framing and player-controlled Camera pan | `06_CameraSystem.md` |
| Monster waves, runtime, pathfinding, and resolution | `07_MonsterSystem.md` |
| Draft generation and result ownership | `08_DraftSystem.md` |
| Tower placement and runtime topology changes | `09_TowerPlacementSystem.md` |
| Tower definitions, authoring, and shared structure | `10_TowerFrameworkSystem.md` |
| Placed-Tower combat orchestration | `11_TowerRuntimeCombatSystem.md` |
| Projectile lifecycle and impact behavior | `12_ProjectileSystem.md` |
| Tower growth, upgrade eligibility, and application | `13_TowerUpgradeSystem.md` |
| Reusable one-shot gameplay resolution | `14_EffectSystem.md` |
| Persistent Buff and Elemental runtime state | `15_BuffSystem.md` |
