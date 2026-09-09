# Tower Nexus - Stage System

Document Set: System

---

# 1. Purpose And Ownership

Stage System owns composition of one playable Stage from the StageDefinition selected for the current battle.

It owns:

- Validation of the selected Stage composition
- Creation of the selected Map instance
- Establishment of the Active Map
- Distribution of the selected Map's authored 3D Camera movement boundary
- Distribution of the selected MonsterWaveConfig
- Distribution of the selected Player maximum health
- Distribution of the selected Player progress requirements
- Distribution of Stage-specific Tower and Tower Upgrade Draft pools
- Distribution of the Stage-specific Tower Draft Slot Probability
- Distribution of the Stage Upgrade pool as Tower level-eligibility authoring
- Establishment of a fresh battle-local Player runtime for the selected Stage
- Establishment of a prepared Stage boundary before battle gameplay begins
- Completion and replacement of Stage-composed runtime content

It coordinates domain owners without absorbing Map, Monster, Draft, Tower, or Game Flow rules.

The demo is expected to contain at least six independently authored StageDefinitions. One selected StageDefinition is active in one battle runtime.

---

# 2. StageDefinition

StageDefinition is one reusable playable-Stage composition.

| Data | Contract |
|---|---|
| Display Name | Optional player-facing Stage name |
| Player Max Health | Positive health cap and full-health start value for this Stage battle |
| Player Progress Requirements | Ordered positive resolved-Monster requirements for each Player level transition in this Stage |
| Map Template | Authored Map used by this Stage |
| Monster Wave Config | Wave sequence executed in this Stage |
| Tower Draft Pool | TowerDefinitions allowed in this Stage's Drafts |
| Tower Upgrade Draft Pool | TowerUpgradeDefinitions allowed in this Stage's Drafts |
| Tower Draft Slot Probability | Inclusive `[0, 1]` probability that each natural Player level-up Draft display slot initially requests the Tower category |
| Show Stage Introduction | Whether the prepared Stage waits for Game Flow introduction confirmation |
| Introduced Towers | TowerDefinitions presented as newly introduced in this Stage |
| Introduced Tower Upgrades | TowerUpgradeDefinitions presented as newly introduced in this Stage |

The asset or resource name is sufficient as authoring identity. A separate StageId is not required while selection uses direct references and no persistence or external lookup contract needs one.

StageDefinition references content owned by other systems. It does not duplicate their rules.

Each Stage independently authors one Player Progress Requirements sequence. Its first entry is the requirement from Player Level 1 to Level 2, and each following entry governs the next transition. The sequence length therefore defines that Stage's maximum Player level and the number of Player level-up Draft opportunities that can be produced. The separate Initial Tower Draft is not represented in this sequence.

StageDefinition owns the reusable progression authoring. Player System receives a battle-local snapshot during Stage composition and owns runtime progress, threshold consumption, and level-up transitions. There is no shared progression curve that overrides or supplements the selected Stage's sequence.

Because every fresh Stage battle begins with one Initial Tower Draft, the Tower Draft Pool must contain at least one valid TowerDefinition. A Stage with no valid Initial Tower Draft candidate is not playable and cannot proceed to Monster Wave execution.

Each Stage independently authors one Tower Draft Slot Probability. The value
applies to each requested display slot in a natural Player level-up Draft. It
does not affect the Tower-only Initial Draft, guarantee a realized display or
selection ratio, or guarantee a Reference Build. Stage System supplies the
value; Draft System owns category allocation, sampling, and backfill.

The Tower Upgrade Draft Pool also authors each TowerFamily's maximum reachable level for this Stage. The maximum is the highest Required Tower Level among that family's Stage-allowed Upgrades, clamped by the supported and TowerDefinition-configured maximums. A family with no Stage-allowed Upgrade remains at Level 1.

Every level from 2 through the Stage-authored maximum must make at least one Upgrade newly eligible at exactly that level. A gap is invalid composition because it would permit a Level Up with no newly accessible Stage content.

Introduction content explicitly authors presentation for this Stage. Introduced Towers must be members of the same Stage's Tower Draft Pool, and introduced Tower Upgrades must be members of the same Stage's Tower Upgrade Draft Pool. Stage System does not calculate introduction content by comparing adjacent Stages.

## 2.1 Stage Calibration Interpretation

A Stage calibration Reference Build is a reproducible positive control, not a
unique solution or a runtime rule. Each Stage calibration task authors its own
reviewed success margin together with its Wave, Player Health, Progress, Draft
budget, legal Build fixtures, and placement assumptions.

A coherent alternative is legal, internally purposeful, spends its available
Draft decisions without an obvious dead allocation, and uses strategically
reasonable placement. If it clears inside that Stage's reviewed margin, it is
an accepted solution even when its composition, Upgrade package, damage
distribution, or leak count differs from the Reference. Accepted combat values
must not be weakened merely to make a non-Reference coherent solution fail.

Negative fixtures remain intentional and explainable. A Stage may reject local
coverage concentration, incomplete package investment, extreme horizontal
expansion without vertical growth, or another named Anti-pattern when Recorder
evidence ties the failure to the tested decision rather than a hidden family
penalty or invalid runtime state.

These are authoring and acceptance rules, not runtime classification logic.
Stage System does not decide whether a live player Build is coherent, compare it
with the Reference, or enforce a leak margin. Fixed Draft fixtures prove combat
efficacy under controlled choices; natural offer probability and player-choice
interpretation remain separately owned calibration work.

---

# 3. Composition Flow

```text
Receive Selected StageDefinition
    -> Validate Stable Runtime References And Composition
    -> Withdraw Outgoing Camera Binding
    -> Release Outgoing Battle Runtime And Map
    -> Create Selected Map
    -> Resolve And Validate Its Authored Boundary And Default Pose
    -> Stage Exact Map, Boundary, And Default-Pose Identity In Camera System
    -> Supply Map To Runtime Consumers
    -> Supply MonsterWaveConfig To Monster System
    -> Supply Player Max Health To Player System
    -> Supply Player Progress Requirements To Player System
    -> Supply Draft Pools And Tower Draft Slot Probability To Draft System
    -> Supply Stage Upgrade Pool To Tower Upgrade System
    -> Initialize Fresh Player Battle-Local State At Full Health, Level 1, And Zero Progress
    -> Confirm No Deferred Release
    -> Commit Exact Staged Camera Identity And Default Pose
    -> Commit Candidate Stage
    -> Mark Stage Composition Ready
    -> Return Prepared Stage To Game Flow
    -> Game Flow Completes Optional Introduction
    -> Begin Battle Runtime
```

Monster Wave execution, Draft generation, Tower placement, and other battle runtime must not begin before Stage composition is ready. A prepared Stage remains inactive until Game Flow permits battle start after any configured introduction.

Each receiving system gets only the configuration slice it owns. Stage System is not a general service locator.

Stage System obtains the boundary and default pose from the validated candidate Map and supplies that explicit Map-boundary-pose identity to Camera System. Camera System does not search for the Active Map or pull authoring state from Stage System.

When replacing or ending a Stage composition, Stage System first withdraws the outgoing Camera binding, then releases Battle runtime, then disables or destroys the outgoing Map. Candidate and active Camera identities are tracked separately. Cleanup matches the exact Map, boundary, and default-pose identity, clears Camera state before Battle cleanup and Map destruction, and cannot clear a newer binding through a late or repeated call.

Every successfully composed initial Stage, next Stage, and retry establishes fresh Camera framing. Camera System restores its authored default pose relative to the new Active Map and discards all pan displacement from the previous battle. Stage System requests this reset as part of composition but does not calculate or store the Camera pose.

Domain owners remain responsible for their own technical cleanup.

Player level progress, health, and defeat state are independent for each Stage battle. They do not carry from one Stage battle into the next or into a retry. StageDefinition authors the positive maximum-health value and ordered Player progress requirements for the selected Stage. Player System owns their applied runtime snapshots, current state, threshold consumption, and transition rules; Stage composition supplies the authored values and establishes a fresh full-health, Level 1 runtime before the prepared Stage may begin. A retry reloads the same Stage-authored requirements into new runtime state, while a next Stage supplies its own sequence.

---

# 4. Ownership Boundaries

| System | Receives From Stage | Continues To Own |
|---|---|---|
| Game Flow System | Prepared Stage readiness and introduction content | Stage ordering, current position, start permission, and result transitions |
| Map System | Selected Map template and active-instance role | Grid state, spatial queries, Map presentation, and validation |
| Monster System | Active Map and MonsterWaveConfig | Wave timing, spawning, pathfinding, movement, and resolution |
| Draft System | Tower and Tower Upgrade pools plus Tower Draft Slot Probability | Category allocation, candidate generation, reservation, sampling, backfill, and results |
| Player System | Player Max Health, Player Progress Requirements, and fresh Stage-battle initialization | Applied runtime snapshots, current health, level progress, level-up, and defeat state |
| Tower Upgrade System | Stage Upgrade pool as level-eligibility authoring | Stage-bound level cap, Upgrade schema, eligibility, and application |
| Tower Placement System | Active Map availability | Placement, occupancy commit, and topology requests |
| Camera System | Exact Active Map, authored 3D boundary, and authored default pose | Initial framing, pan input, and enforcement of Camera movement bounds |

---

# 5. Validation

Stage validation should report at minimum:

- Missing or invalid Map template
- Missing, ambiguous, or invalid 3D Camera movement boundary in the selected Map template
- Missing, ambiguous, invalid, or externally owned default Camera pose
- Missing MonsterWaveConfig or invalid Wave content
- Non-positive Player Max Health
- Missing or empty Player Progress Requirements
- Non-positive Player progress requirement
- Null or duplicate TowerDefinition references
- No valid TowerDefinition available for the required Initial Tower Draft
- Null or duplicate TowerUpgradeDefinition references
- Tower Draft Slot Probability outside the inclusive `[0, 1]` range
- Referenced definitions that fail owner-system validation
- Tower Upgrade content whose TowerFamily cannot be represented by the Stage Tower pool when that relationship is required
- Required Tower Level outside the supported or represented TowerDefinition progression
- Missing newly eligible Upgrade content at any level from Level 2 through a TowerFamily's Stage maximum
- Introduction Tower content outside the Stage Tower Draft Pool
- Introduction Upgrade content outside the Stage Tower Upgrade Draft Pool
- Null or duplicate introduction content
- Introduction requested without any presentable content

Stage validation checks composition coherence. It does not silently repair referenced content or replace owner-system validation.

An introduction requested with no presentable Tower or Upgrade content is a warning. It does not invalidate an otherwise playable Stage, and Game Flow may proceed directly from preparation to Battle.

---

# 6. Approved Scope And Deferred Topics

Current scope includes:

- Six or more authorable StageDefinitions
- One selected Stage per battle runtime
- One Map template and one MonsterWaveConfig per Stage
- One valid authored 3D Camera movement boundary per Map template
- One positive Player Max Health value per Stage
- One non-empty ordered Player Progress Requirements sequence per Stage
- Stage-specific Tower and Tower Upgrade Draft pools
- One Stage-specific Tower Draft Slot Probability for natural Player level-up Drafts
- Stage-derived per-TowerFamily level caps with continuous unlock paths
- At least one valid TowerDefinition for the Initial Tower Draft
- Optional Stage Introduction content
- Composition validation and pre-battle distribution
- A prepared Stage boundary controlled by Game Flow
- Fresh battle-local Player state for each composed Stage
- Fresh default Camera framing for every initial Stage, next Stage, and retry

Game Flow System owns the approved Demo Stage ordering, introduction, victory, defeat, next-Stage, retry, and return-to-main-menu rules.

Deferred Game Flow topics include:

- Unlock rules
- Save data and persistent Stage progress
- Stage Selection UI
- Scene transition strategy

Multiple Spawn Routes and route-specific Wave composition are also deferred to future Map and Monster design.

## Battle Dependency Lifetime

Each prepared Battle receives a distinct combat authority identity. Activation opens
that identity once; terminal acceptance, Stop, Release (including deferred Release)
and preparation failure permanently revoke it before evidence callbacks. Evidence
may still read committed state before physical cleanup. A retry never reactivates
an outgoing identity, even when it reuses the same runtime services.
