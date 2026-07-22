# Tower Nexus - Stage System

---

# 1. Purpose And Ownership

Stage System owns composition of one playable Stage from the StageDefinition selected for the current battle.

It owns:

- Validation of the selected Stage composition
- Creation of the selected Map instance
- Establishment of the Active Map
- Distribution of the selected MonsterWaveConfig
- Distribution of Stage-specific Tower and Tower Upgrade Draft pools
- Establishment of a fresh battle-local Player runtime for the selected Stage
- Completion and replacement of Stage-composed runtime content

It coordinates domain owners without absorbing Map, Monster, Draft, Tower, or Game Flow rules.

The demo is expected to contain at least five independently authored StageDefinitions. One selected StageDefinition is active in one battle runtime.

---

# 2. StageDefinition

StageDefinition is one reusable playable-Stage composition.

| Data | Contract |
|---|---|
| Display Name | Optional player-facing Stage name |
| Map Template | Authored Map used by this Stage |
| Monster Wave Config | Wave sequence executed in this Stage |
| Tower Draft Pool | TowerDefinitions allowed in this Stage's Drafts |
| Tower Upgrade Draft Pool | TowerUpgradeDefinitions allowed in this Stage's Drafts |

The asset or resource name is sufficient as authoring identity. A separate StageId is not required while selection uses direct references and no persistence or external lookup contract needs one.

StageDefinition references content owned by other systems. It does not duplicate their rules.

---

# 3. Composition Flow

```text
Receive Selected StageDefinition
    -> Validate Composition
    -> Create Selected Map
    -> Establish Active Map
    -> Supply Map To Runtime Consumers
    -> Supply MonsterWaveConfig To Monster System
    -> Supply Draft Pools To Draft System
    -> Reset Player Battle-Local State
    -> Mark Stage Composition Ready
    -> Begin Battle Runtime
```

Monster Wave execution, Draft generation, Tower placement, and other battle runtime must not begin before Stage composition is ready.

Each receiving system gets only the configuration slice it owns. Stage System is not a general service locator.

When replacing or ending a Stage composition, Stage System releases only the runtime objects and references created by that composition. Domain owners remain responsible for their own technical cleanup.

Player level progress, health, and defeat state are independent for each Stage battle. They do not carry from one Stage battle into the next. Player System owns those values and their rules; Stage composition only establishes a fresh battle-local runtime before battle begins.

---

# 4. Ownership Boundaries

| System | Receives From Stage | Continues To Own |
|---|---|---|
| Map System | Selected Map template and active-instance role | Grid state, spatial queries, Map presentation, and validation |
| Monster System | Active Map and MonsterWaveConfig | Wave timing, spawning, pathfinding, movement, and resolution |
| Draft System | Tower and Tower Upgrade pools | Candidate generation, reservation, sampling, and results |
| Player System | Fresh Stage-battle initialization | Level progress, health, level-up, and defeat state |
| Tower Upgrade System | No direct runtime mutation | Upgrade schema, eligibility, and application |
| Tower Placement System | Active Map availability | Placement, occupancy commit, and topology requests |

---

# 5. Validation

Stage validation should report at minimum:

- Missing or invalid Map template
- Missing MonsterWaveConfig or invalid Wave content
- Null or duplicate TowerDefinition references
- Null or duplicate TowerUpgradeDefinition references
- Referenced definitions that fail owner-system validation
- Tower Upgrade content whose TowerFamily cannot be represented by the Stage Tower pool when that relationship is required

Stage validation checks composition coherence. It does not silently repair referenced content or replace owner-system validation.

---

# 6. Approved Scope And Deferred Topics

Current scope includes:

- Five or more authorable StageDefinitions
- One selected Stage per battle runtime
- One Map template and one MonsterWaveConfig per Stage
- Stage-specific Tower and Tower Upgrade Draft pools
- Composition validation and pre-battle distribution
- Fresh battle-local Player state for each composed Stage

Deferred to future Game Flow design:

- Stage ordering and selection entry points
- Unlock rules
- Victory and failure transitions
- Automatic next-Stage flow
- Save data and persistent Stage progress
- Stage Selection UI
- Scene transition strategy

Multiple Spawn Routes and route-specific Wave composition are also deferred to future Map and Monster design.
