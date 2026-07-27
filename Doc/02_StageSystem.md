# Tower Nexus - Stage System

---

# 1. Purpose And Ownership

Stage System owns composition of one playable Stage from the StageDefinition selected for the current battle.

It owns:

- Validation of the selected Stage composition
- Creation of the selected Map instance
- Establishment of the Active Map
- Distribution of the selected MonsterWaveConfig
- Distribution of the selected Player maximum health
- Distribution of Stage-specific Tower and Tower Upgrade Draft pools
- Establishment of a fresh battle-local Player runtime for the selected Stage
- Establishment of a prepared Stage boundary before battle gameplay begins
- Completion and replacement of Stage-composed runtime content

It coordinates domain owners without absorbing Map, Monster, Draft, Tower, or Game Flow rules.

The demo is expected to contain at least five independently authored StageDefinitions. One selected StageDefinition is active in one battle runtime.

---

# 2. StageDefinition

StageDefinition is one reusable playable-Stage composition.

| Data | Contract |
|---|---|
| Display Name | Optional player-facing Stage name |
| Player Max Health | Positive health cap and full-health start value for this Stage battle |
| Map Template | Authored Map used by this Stage |
| Monster Wave Config | Wave sequence executed in this Stage |
| Tower Draft Pool | TowerDefinitions allowed in this Stage's Drafts |
| Tower Upgrade Draft Pool | TowerUpgradeDefinitions allowed in this Stage's Drafts |
| Show Stage Introduction | Whether the prepared Stage waits for Game Flow introduction confirmation |
| Introduced Towers | TowerDefinitions presented as newly introduced in this Stage |
| Introduced Tower Upgrades | TowerUpgradeDefinitions presented as newly introduced in this Stage |

The asset or resource name is sufficient as authoring identity. A separate StageId is not required while selection uses direct references and no persistence or external lookup contract needs one.

StageDefinition references content owned by other systems. It does not duplicate their rules.

Introduction content explicitly authors presentation for this Stage. Introduced Towers must be members of the same Stage's Tower Draft Pool, and introduced Tower Upgrades must be members of the same Stage's Tower Upgrade Draft Pool. Stage System does not calculate introduction content by comparing adjacent Stages.

---

# 3. Composition Flow

```text
Receive Selected StageDefinition
    -> Validate Composition
    -> Create Selected Map
    -> Establish Active Map
    -> Supply Map To Runtime Consumers
    -> Supply MonsterWaveConfig To Monster System
    -> Supply Player Max Health To Player System
    -> Supply Draft Pools To Draft System
    -> Initialize Fresh Player Battle-Local State At Full Health
    -> Mark Stage Composition Ready
    -> Return Prepared Stage To Game Flow
    -> Game Flow Completes Optional Introduction
    -> Begin Battle Runtime
```

Monster Wave execution, Draft generation, Tower placement, and other battle runtime must not begin before Stage composition is ready. A prepared Stage remains inactive until Game Flow permits battle start after any configured introduction.

Each receiving system gets only the configuration slice it owns. Stage System is not a general service locator.

When replacing or ending a Stage composition, Stage System releases only the runtime objects and references created by that composition. Domain owners remain responsible for their own technical cleanup.

Player level progress, health, and defeat state are independent for each Stage battle. They do not carry from one Stage battle into the next or into a retry. StageDefinition authors the positive maximum-health value for the selected Stage. Player System owns the applied runtime maximum, current health, and their rules; Stage composition only supplies the authored value and establishes a fresh full-health runtime before the prepared Stage may begin.

---

# 4. Ownership Boundaries

| System | Receives From Stage | Continues To Own |
|---|---|---|
| Game Flow System | Prepared Stage readiness and introduction content | Stage ordering, current position, start permission, and result transitions |
| Map System | Selected Map template and active-instance role | Grid state, spatial queries, Map presentation, and validation |
| Monster System | Active Map and MonsterWaveConfig | Wave timing, spawning, pathfinding, movement, and resolution |
| Draft System | Tower and Tower Upgrade pools | Candidate generation, reservation, sampling, and results |
| Player System | Player Max Health and fresh Stage-battle initialization | Applied maximum health, current health, level progress, level-up, and defeat state |
| Tower Upgrade System | No direct runtime mutation | Upgrade schema, eligibility, and application |
| Tower Placement System | Active Map availability | Placement, occupancy commit, and topology requests |

---

# 5. Validation

Stage validation should report at minimum:

- Missing or invalid Map template
- Missing MonsterWaveConfig or invalid Wave content
- Non-positive Player Max Health
- Null or duplicate TowerDefinition references
- Null or duplicate TowerUpgradeDefinition references
- Referenced definitions that fail owner-system validation
- Tower Upgrade content whose TowerFamily cannot be represented by the Stage Tower pool when that relationship is required
- Introduction Tower content outside the Stage Tower Draft Pool
- Introduction Upgrade content outside the Stage Tower Upgrade Draft Pool
- Null or duplicate introduction content
- Introduction requested without any presentable content

Stage validation checks composition coherence. It does not silently repair referenced content or replace owner-system validation.

An introduction requested with no presentable Tower or Upgrade content is a warning. It does not invalidate an otherwise playable Stage, and Game Flow may proceed directly from preparation to Battle.

---

# 6. Approved Scope And Deferred Topics

Current scope includes:

- Five or more authorable StageDefinitions
- One selected Stage per battle runtime
- One Map template and one MonsterWaveConfig per Stage
- One positive Player Max Health value per Stage
- Stage-specific Tower and Tower Upgrade Draft pools
- Optional Stage Introduction content
- Composition validation and pre-battle distribution
- A prepared Stage boundary controlled by Game Flow
- Fresh battle-local Player state for each composed Stage

Game Flow System owns the approved Demo Stage ordering, introduction, victory, defeat, next-Stage, retry, and return-to-main-menu rules.

Deferred Game Flow topics include:

- Unlock rules
- Save data and persistent Stage progress
- Stage Selection UI
- Scene transition strategy

Multiple Spawn Routes and route-specific Wave composition are also deferred to future Map and Monster design.
