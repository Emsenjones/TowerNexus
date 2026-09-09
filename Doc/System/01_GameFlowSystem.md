# Tower Nexus - Game Flow System

Document Set: System

---

# 1. Purpose And Ownership

Game Flow System owns the playable flow outside and between individual Stage battles.

It owns:

- The ordered Demo Stage sequence
- The current Stage position within one game run
- Entry into the main menu, Stage preparation, optional Stage introduction, battle, victory, and defeat states
- Validation of legal flow transitions
- Selection of the StageDefinition supplied to Stage System
- Response to one authoritative battle result
- Response to one result-neutral asynchronous Battle runtime failure
- Next-Stage, retry-current-Stage, and return-to-main-menu decisions
- Visibility and interaction intent for Game Flow presentation surfaces

It coordinates Stage and battle runtime owners without absorbing their domain rules. It does not compose Maps, execute Waves, mutate Player health directly, resolve Monsters, generate Drafts, validate Tower placement, or own combat behavior.

The Demo uses one ordered sequence of directly referenced StageDefinitions. Unlocking, persistence, branching routes, and a Stage-selection interface are separate future designs.

Before a terminal Battle notification can cause Stage release, committed
investment facts and terminal observation snapshots are finalized. Observation
and public flow notifications are ordered phases, not subscriber-order assumptions.
A necessary Upgrade refresh failure after commitment uses the same result-neutral
Technical Failure route; accepted investment is neither refunded nor replayed.

---

# 2. Game Flow State Model

The first-version Game Flow contains these states:

| State | Meaning |
|---|---|
| Main Menu | No Stage battle is active; the player may start a new run |
| Stage Preparing | The selected Stage is being validated and prepared while battle gameplay remains inactive |
| Stage Introduction | The selected Stage is ready, but battle gameplay waits for player confirmation |
| Battle | The prepared Stage battle is active |
| Stage Victory | The battle has ended in victory and the player may continue or return to the main menu |
| Stage Defeat | The battle has ended in defeat and the player may retry or return to the main menu |

Valid primary transitions are:

```text
Application Entry
    -> Main Menu

Main Menu
    -> Stage Preparing At First Stage

Stage Preparing
    -> Stage Introduction
    -> Battle

Stage Introduction
    -> Battle

Battle
    -> Stage Victory
    -> Stage Defeat
    -> Main Menu After Result-Neutral Runtime Failure

Stage Victory
    -> Stage Preparing At Next Stage
    -> Main Menu

Stage Defeat
    -> Stage Preparing At Current Stage
    -> Main Menu
```

An interaction intent is accepted only when it is valid for the current state. Repeated or late intents cannot prepare, start, complete, or release the same Stage twice.

---

# 3. Demo Stage Sequence

The Demo Stage sequence is an ordered collection of StageDefinition references.

Sequence rules:

- A new run begins at index zero.
- Advancing after victory selects the next index.
- Retrying after defeat keeps the current index.
- Victory on the final configured Stage returns to the main menu.
- Returning to the main menu ends the current run.
- Starting again from the main menu creates a new run at index zero.
- The number of configured Stages is authored data rather than a hard-coded runtime limit.

The current Demo target is at least six independently authored StageDefinitions. A temporarily shorter sequence may support content production, but an empty sequence, null entry, duplicate entry, or invalid current index is not a playable configuration.

Game Flow owns Stage ordering only. StageDefinition continues to own one Stage's reusable composition, and Stage System continues to own preparation and release of the selected Stage.

---

# 4. Stage Preparation And Start

Every initial Stage, next Stage, and retry enters the same preparation flow:

```text
Select Current StageDefinition
    -> Release Previous Stage Runtime
    -> Validate And Prepare Selected Stage
    -> Establish Fresh Player Battle-Local State
        -> Maximum Health = Selected Stage Player Maximum Health
        -> Progress Requirements = Selected Stage Player Progress Requirements
        -> Current Health = Maximum Health
        -> Reset Player Level And Progress
        -> Clear Player Defeat State
    -> Establish Selected Map, Camera Boundary, Wave, And Draft Content
    -> Restore Default Camera Framing Without Prior Pan Displacement
    -> Mark Stage Ready While Battle Remains Inactive
    -> Show Stage Introduction When Configured
    -> Otherwise Begin Battle
```

Game Flow requests preparation of the selected Stage. Stage System distributes the selected configuration, Player System owns the resulting runtime Player state, and Camera System owns restoration of its authored default framing against the newly active Map. Game Flow does not write Player health, progress, level, defeat state, or Camera pose directly.

Monster spawning, Draft generation, Tower placement, and combat remain inactive throughout Stage Preparing and Stage Introduction. Player confirmation of a configured introduction permits the already prepared Stage to enter Battle; it does not recompose the Stage.

Preparation failure cannot enter Battle or expose an interactive result state. Partial Stage content is released, and the flow returns to a safe non-battle state.

---

# 5. Stage Introduction Content

StageDefinition may provide optional Stage Introduction content:

| Data | Contract |
|---|---|
| Show On Stage Start | Whether the prepared Stage waits for introduction confirmation |
| Introduced Towers | TowerDefinitions presented as newly introduced in this Stage |
| Introduced Tower Upgrades | TowerUpgradeDefinitions presented as newly introduced in this Stage |

Introduced Towers must belong to the same Stage's Tower Draft Pool. Introduced Tower Upgrades must belong to the same Stage's Tower Upgrade Draft Pool. The lists explicitly author the intended presentation; Game Flow does not calculate a difference against the preceding Stage.

Each introduced item presents the referenced definition's player-facing name, description, icon, and category treatment. Tower and Tower Upgrade items share one ordered presentation collection while remaining visually distinguishable. If the Stage does not require an introduction, preparation proceeds directly to Battle.

Introduction content is presentation metadata. It does not change Draft weighting, eligibility, availability, or the underlying Tower and Tower Upgrade definitions.

Requesting an introduction while both introduced-content lists are empty produces an authoring warning and skips the empty introduction rather than blocking an otherwise valid Stage.

---

# 6. Battle Result Contract

One active Battle owns one authoritative terminal claim:

```text
None
    -> Victory
    OR Defeat
    OR Technical Failure
```

Victory and Defeat are semantic results. Technical Failure is terminal but result-neutral. Every terminal path must first claim this shared state; competing independent result and failure flags cannot authorize separate terminal transactions. Stop, Stage release, replacement, retry, and preparation rollback are lifecycle cancellation and do not claim a terminal state.

## 6.1 Victory

Victory requires all of these facts:

- The selected Monster Wave configuration has normally completed all configured spawning.
- No alive unresolved Monster remains.
- Player System has not entered defeat.

An empty battlefield before all configured spawning is complete is not victory. Technical cancellation, cleanup, or preparation rollback is not victory.

## 6.2 Defeat

Defeat occurs when Player System enters its terminal defeated state after current health reaches zero.

Monster removal and Player resolution form one ordered transaction. When the final Monster reaches the Target and causes health to reach zero, Player defeat is resolved before victory eligibility is evaluated. Defeat therefore takes precedence over victory for that resolution.

## 6.3 Result Publication

The battle-active authority closes before result presentation begins:

```text
Claim Victory Or Defeat From The Shared Terminal State
    -> Close Battle Gameplay Authority
    -> Stop New Runtime Output
    -> Perform Required Technical Cleanup
    -> Publish The Semantic Result Once
    -> Enter The Matching Game Flow Result State
```

A technical stop remains distinct from a semantic result. Releasing a Stage, returning to the main menu, disabling runtime owners, or rolling back failed preparation must not manufacture Victory or Defeat.

## 6.4 Result-Neutral Runtime Failure

A technical failure discovered before Battle start succeeds follows the existing failed-transition rollback and never enters Battle.

A technical failure discovered after Battle start succeeds follows this contract:

```text
Claim Technical Failure From The Shared Terminal State
    -> Close Battle Gameplay Authority
    -> Stop New Runtime Output
    -> Perform Required Technical Cleanup
    -> Publish One Result-Neutral Failure
    -> Game Flow Verifies The Current Battle Source
    -> Release The Current Stage
    -> Clear The Current Run
    -> Enter Main Menu
```

The failure publication is the final operation performed by the Battle coordinator. A Game Flow subscriber may immediately release the Stage and invalidate its runtime objects; the coordinator must return without reading or mutating coordinator or Stage state after publication.

The failure does not publish Victory or Defeat and does not enter a result presentation state. A synchronous and asynchronous report of the same underlying failure must converge on one cleanup transaction.

---

# 7. Result Transitions

Stage Victory offers one forward action:

- If another Stage exists, advance to that Stage and enter Stage Preparing.
- If the completed Stage is the final Stage, return to the main menu.

Stage Defeat offers:

- Retry the current Stage through the complete Stage Preparing flow.
- Return to the main menu.

Next-Stage and retry transitions both release the previous Stage runtime before preparing the selected Stage. They never reuse prior Player state or applied Player configuration, Map state, Camera boundary or Pan displacement, Monsters, Towers, pending Drafts, Wave execution state, or battle-active authority.

A valid result-neutral runtime failure transitions directly from the active Battle to Main Menu after releasing the failed Stage. No retry, victory, defeat, or technical-error window is implied.

The final-Stage check is derived from the current position in the configured sequence. It is not stored as a separate flag on StageDefinition.

---

# 8. Game Flow Presentation

Game Flow presentation contains four distinct player-facing capabilities:

| Presentation | Interaction Contract |
|---|---|
| Main Menu | A full-screen start interaction begins a new run |
| Stage Introduction | Presents configured Stage additions and confirms entry into Battle |
| Stage Victory | Presents victory and requests next Stage or main menu according to sequence position |
| Stage Defeat | Presents defeat and requests retry or main menu |

Victory and Defeat are distinct presentation modes because their available
actions differ. They may share one result surface as long as its title,
description, and available actions are configured from the current Game Flow
state before interaction is enabled. Visual styling, animation, concrete
hierarchy, and loading strategy are presentation implementation concerns.

Only the presentation valid for the current Game Flow state accepts interaction. Modal Game Flow presentation prevents input from reaching inactive battle interaction surfaces beneath it.

Battle HUD UI remains a separate battle-local presentation system. Sharing one visual canvas or screen does not transfer Game Flow ownership to Battle HUD UI.

---

# 9. Ownership Boundaries

| System | Supplies To Game Flow | Continues To Own |
|---|---|---|
| Stage System | Preparation success or failure and selected Stage readiness | Composition validation, Active Map and Camera-boundary handoff, configuration distribution, and Stage release |
| Camera System | No Game Flow decision | Default Active-Map framing, Pan state, and movement-boundary enforcement |
| Player System | Authoritative defeat state | Runtime level, progress, health, and defeat mutation |
| Monster System | Normal spawning completion and post-resolution alive-Monster state | Wave execution, Monster lifecycle, and exactly-once resolution |
| Battle runtime coordination | One claimed terminal fact: Victory, Defeat, or result-neutral Technical Failure, after gameplay authority closes | Shared terminal claim, Battle gates, and technical runtime cleanup |
| Battle HUD UI System | No Game Flow decision | Battle-local information, Draft interaction, and placement feedback |

Game Flow consumes semantic outcomes and readiness facts. It does not infer them from presentation state, object destruction timing, or visible UI contents.

---

# 10. Validation

Game Flow validation should report at minimum:

- Empty Stage sequence
- Null or duplicate StageDefinition references
- Current Stage position outside the configured sequence
- Invalid Stage preparation entering Battle
- Player state not reset to the selected Stage's positive maximum health
- Player state not initialized from the selected Stage's Player progress requirements
- Camera boundary or Pan displacement retained by an initial Stage, next Stage, or retry
- Introduction Tower content outside the selected Stage Tower Draft Pool
- Introduction Upgrade content outside the selected Stage Tower Upgrade Draft Pool
- Null or duplicate introduction content
- Introduction requested without any presentable content, reported as a warning
- Battle gameplay active during Stage Introduction
- Victory before normal spawning completion
- Victory while an unresolved Monster remains
- More than one terminal state claimed for one Battle
- Lifecycle cancellation incorrectly claiming a terminal state
- A technical runtime failure publishing Victory or Defeat
- Battle coordinator state accessed after publishing a result-neutral runtime failure
- Duplicate cleanup or transition from synchronous and asynchronous reports of one technical failure
- A result-neutral runtime failure retained in Battle or routed through a result window
- Duplicate next, retry, confirmation, or return transitions
- Previous Stage runtime state retained by next Stage or retry

Validation reports invalid authoring or transition state without silently selecting another Stage, repairing content, or bypassing the required preparation flow.

---

# 11. Approved Scope And Deferred Topics

Current scope includes:

- One ordered Demo Stage sequence
- New-run entry from a main menu
- Stage preparation with fresh Player state and default Active-Map Camera framing
- Optional Stage Introduction content
- Battle start gating
- Authoritative Victory and Defeat transitions
- Result-neutral asynchronous Battle failure return to Main Menu
- Next Stage, retry, and return-to-main-menu flow
- Distinct main-menu, introduction, victory, and defeat presentation capabilities

Deferred topics include:

- Stage unlocking and prerequisites
- Stage Selection UI
- Save data and persistent Stage progress
- Chapters, branching paths, and alternate Stage sequences
- Scene-transition strategy
- Asynchronous or streamed loading
- Pause and resume flow
- Meta progression

These topics require separate design review before expanding the current Game Flow ownership boundary.
