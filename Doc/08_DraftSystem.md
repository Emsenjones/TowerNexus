# Tower Nexus - Draft System

---

# 1. Purpose And Ownership

Draft System turns an approved Draft opportunity into a set of runtime choices and one selected reward.

It owns:

- Candidate gathering from Stage-specific content pools
- Eligibility-aware candidate representation
- Pending Tower Upgrade reservation
- Candidate weighting and sampling
- Same-round displayed-choice deduplication
- Draft workflow state and result creation
- Explicit Draft workflow phase, Draft-session identity, and stale-callback rejection
- Battle-simulation pause while the Draft Window is open

It does not own Player progression, Stage composition, UI layout, Tower placement, Tower Upgrade application, Map occupancy, or combat behavior.

---

# 2. Inputs And Outputs

Inputs:

- One Initial Tower Draft opportunity for each fresh Stage battle
- Player level-up opportunity
- Stage Tower Draft Pool
- Stage Tower Upgrade Draft Pool
- Current deployed Tower instances
- Tower Upgrade eligibility results
- Unconsumed Tower Upgrade Draft items

Outputs:

- One displayed Draft choice set
- One selected Tower Draft or Tower Upgrade Draft result
- Accepted completion of the Initial Tower Draft after its held Tower Draft item exists
- One owned battle-simulation pause for the active Draft session

The first version displays up to three distinct choices. It displays one or two choices when the active Stage contains fewer than three distinct eligible identities.

---

# 3. Draft Workflow

```text
Approved Draft Opportunity
    -> Resolve Initial Or Level-Up Source
    -> Build Candidates Allowed For That Source
    -> Merge Candidate Entries
    -> Sample Distinct Display Choices
    -> Establish Provisional Opening Session
    -> Present Choices With Session Identity
    -> Acquire Battle-Simulation Pause After Successful Opening
    -> Await One Selection
    -> Claim Selection Commit Before Held-Item Creation
    -> Accept One Selection
    -> Create Held Draft Item
    -> Preserve Initial Completion Evidence When Applicable
    -> Close Draft Presentation
    -> Release Battle-Simulation Pause
    -> Publish Accepted Completion When Required
```

Only one choice from the active set may become a result. Presentation closure, duplicate input, or stale selection must not create additional rewards.

Each Draft session belongs to one fresh Battle generation and one unique attempt within that Battle. Stop, release, retry, replacement, or disable invalidates the active session. Completion and technical-failure facts are accepted only from the exact current session; identity must not collide with an earlier Battle.

Every session moves through one explicit workflow phase:

```text
None
    -> Opening
    -> Awaiting Selection
    -> Committing Selection
        -> Completed
        -> Failed

Opening / Awaiting Selection / Committing Selection
    -> Cancelled By Lifecycle Cleanup
```

The attempt identity is provisional before it is exposed to presentation callbacks. A successful opening acquires the attempt-owned pause before entering Awaiting Selection. Opening failure rolls the provisional session back synchronously and publishes no asynchronous failure fact.

## 3.1 Initial Tower Draft

Every fresh Stage battle, including a retry, grants exactly one Initial Tower Draft after Game Flow permits Battle entry and before Monster Wave execution begins.

The Initial Tower Draft:

- Builds candidates only from the active Stage Tower Draft Pool
- Does not include Tower Upgrade Draft candidates
- Does not change Player level or progress
- Produces one held Tower Draft item after an accepted selection
- Authorizes the first Monster Wave Delay only after that held item exists

The selected Tower does not have to be deployed before Wave timing begins. Deployment follows the normal held-item interaction and Tower Placement rules during the first Wave Delay.

A stopped, released, or failed Stage does not count as completing its Initial Tower Draft. A retry creates a fresh Initial Tower Draft opportunity with fresh Stage runtime state.

## 3.2 Player Level-Up Draft

Each accepted Player level-up opportunity uses the normal combined Tower and eligible Tower Upgrade candidate process. It remains independent of the one Initial Tower Draft granted for that Stage battle.

Only the first level-up callback may establish a Draft session. While ordinary gameplay pause prevents later Monster resolution, one debug or future batch-progression transaction may still publish multiple callbacks synchronously. Later callbacks in that transaction are rejected as unsupported overlap: they cannot replace the active Draft, create a reward, close its presentation, or release its pause. Preserving one Draft reward for every callback in a multi-level batch requires a future Draft queue.

## 3.3 Draft Simulation Pause

Every successfully opened Initial or Player level-up Draft Window pauses battle simulation until that Draft session commits a held item or terminates through failure or lifecycle cancellation.

Pause rules are:

- Draft System owns acquisition and release for the exact active Draft session.
- The simulation rate that existed before the Draft opened is restored rather than replaced with an assumed default.
- While the Draft-owned pause is active, Draft System is the only permitted writer of the simulation rate; another pause or slow-motion owner must not change it before release.
- Draft presentation and selection remain interactive while battle simulation is paused.
- Successful selection first enters Committing Selection, then commits the held item, closes presentation, releases pause, and only then publishes completion.
- Synchronous opening failure acquires no pause and returns failure directly.
- Asynchronous technical failure releases pause before reporting failure.
- Stop, release, replacement, retry, and disable cancel the session and release its pause without publishing completion or technical failure.
- Nested or stale callbacks cannot release another session's pause.

Pausing prevents ordinary later Monster resolution and Player progress while a Draft is open. Same-frame reentrant level-up or selection callbacks are still rejected by session identity and exactly-once guards.

Slow motion while dragging a held Draft item is a separate future behavior. It does not share the Draft Window pause lifetime.

The attempt identity prevents another Draft from releasing the active pause; it cannot arbitrate an unrelated simulation-rate writer. If another runtime time owner is introduced, direct Draft ownership must be replaced by a shared pause or time-control service.

---

# 4. Draft Result Types

## 4.1 Tower Draft

A Tower Draft references one TowerDefinition and may be consumed in either of two ways:

1. Place a new Tower on a valid deployment area.
2. Target an existing Tower of the same TowerFamily to request a Tower level increase.

Placement or level-up failure does not consume the item. Tower Placement System identifies the intent; Tower Upgrade System owns level-up eligibility and application.

Pending Tower Draft items do not reserve future Tower Draft candidate capacity because each item remains a flexible placement-or-level-up resource.

## 4.2 Tower Upgrade Draft

A Tower Upgrade Draft references one TowerUpgradeDefinition and may be applied to one eligible existing Tower.

Tower Upgrade System owns required level, TowerFamily, layer capacity, duplicate, and application rules. Draft System consumes those eligibility results without reimplementing them.

An unconsumed Tower Upgrade Draft reserves the corresponding remaining battlefield capacity so later Drafts do not over-offer an upgrade that can no longer be applied.

---

# 5. Tower Draft Candidate Pool

Every valid TowerDefinition in the active Stage Tower Draft Pool contributes one candidate identity.

TowerDefinitions outside the current Stage pool do not participate. Pending held Tower Draft items do not reduce this pool.

All first-version candidate entries have equal base weight unless an approved rule explicitly changes weighting.

The Initial Tower Draft samples only these Tower Draft candidates. Later Player level-up Drafts may merge them with eligible Tower Upgrade candidates.

---

# 6. Tower Upgrade Candidate Pool

For each Stage-allowed TowerUpgradeDefinition, Draft System asks how many current Tower instances can legally receive it.

Eligibility considers owner-system rules such as:

- Matching TowerFamily
- Required Tower Level
- Upgrade layer capacity
- Existing applied upgrades
- Elemental exclusivity

Draft System then subtracts pending reserved capacity held in the Draft Item Interaction Area:

```text
Remaining Eligible Capacity
    = Eligible Tower Count
    - Pending Reserved Capacity
```

The definition contributes one internal candidate entry per remaining eligible capacity. A non-positive result contributes no candidate.

Pending reservation must cover:

- The same TowerUpgradeDefinition
- Any approved exclusive capacity shared by multiple definitions, such as one Elemental Layer slot per Tower

This produces Tower-instance-weighted discovery: content usable by more current Towers has more internal representation, while already-held rewards reduce over-offering.

---

# 7. Combined Sampling And Display

For a Player level-up Draft, Tower Draft and Tower Upgrade candidate entries are merged before sampling. The Initial Tower Draft samples only Tower Draft entries.

Internal duplicate entries provide weight. Displayed choices remain unique by reward identity:

- Tower Draft identity is its TowerDefinition.
- Tower Upgrade Draft identity is its TowerUpgradeDefinition.

The same identity must not appear twice in one displayed Draft set. Removing a sampled duplicate must not accidentally remove its internal weight before the selection process has completed.

If no Tower Upgrade candidates exist, available Tower Draft candidates may still form the displayed choices. If fewer distinct eligible identities exist than the configured choice count, the Draft shows only the available distinct identities rather than inventing invalid choices.

---

# 8. Selection And Consumption Boundary

Selecting a Draft choice creates a held Draft item; it does not immediately place a Tower or apply an Upgrade.

```text
Select Tower Draft
    -> Create Held Tower Draft Item
    -> Later Placement Or Same-Family Tower Intent
    -> Accepted Gameplay Result Consumes Item
```

```text
Select Tower Upgrade Draft
    -> Create Held Upgrade Item And Pending Reservation
    -> Later Tower Target Intent
    -> Accepted Upgrade Consumes Item
```

Cancelling or rejecting a drag preserves the held item and any reservation it represents. Successful consumption removes both.

Held-item creation is atomic. A selected result is committed only after one complete held item has been created, validated, initialized, made interactive, and registered in the pending-item collection. Failure leaves no partial registration and does not publish accepted Draft completion.

The active session must atomically move from Awaiting Selection to Committing Selection before held-item creation begins. Object construction, activation, initialization, or nested presentation callbacks therefore cannot enter a second held-item transaction. Failure establishes Failed.

For an Initial Draft, successful held-item commit next preserves an attempt-scoped completed record containing the exact completed token and committed held item, then establishes Completed. Active selection authority may then be invalidated without erasing the evidence required by Battle coordination. Presentation closes, pause releases, and completion publishes only after the record exists. The completed Initial record survives completion publication and is cleared only by fresh Battle reset or Stage cleanup.

---

# 9. Validation

Draft validation should report at minimum:

- Missing active Stage pools
- No valid TowerDefinition available for the required Initial Tower Draft
- Null or duplicate entries inside a Stage pool
- Definitions that fail owner-system validation
- Non-positive configured displayed choice count
- Pending reservation that cannot identify its reward or exclusive capacity
- A selected identity not present in the active displayed set
- A stale or mismatched Battle-generation or Draft-attempt identity
- A duplicate Initial Tower Draft opportunity for one Stage battle
- Held-item creation entered without first claiming the Committing Selection phase
- Initial Draft completion reported before its held Tower Draft item exists
- Active Draft invalidation erasing the completed Initial Draft record before Battle coordination validates it
- Completed Initial Draft record retained across fresh Battle reset or Stage cleanup
- More than one active Draft session or Draft-owned simulation pause
- Another simulation-rate owner writing while the Draft-owned pause is active
- Draft pause not released on selection, failure, stop, release, retry, replacement, or disable
- Draft presentation unable to remain interactive while battle simulation is paused

Validation does not silently add content to a Stage or alter Tower Upgrade rules.

---

# 10. Approved Scope And Deferred Topics

Current scope includes:

- Stage-specific Tower and Tower Upgrade pools
- One Initial Tower Draft for each fresh Stage battle
- Player level-up Draft opportunities
- Three-choice display when enough identities exist
- Equal weight per internal candidate entry
- Tower-instance-weighted Upgrade candidates
- Pending Upgrade reservation
- Same-round displayed deduplication
- Held Tower and Tower Upgrade results
- Draft-owned battle-simulation pause while the Draft Window is open
- Battle-generation and attempt identity guards

Deferred topics include Draft queueing for multi-level batch progression, slow motion while dragging a held Draft item, rarity, reroll, ban or pick, global rewards, curses, persistent progression rewards, multiplayer Drafts, and Stage-completion rewards.
