# Tower Nexus - Draft System

Document Set: System

---

# 1. Purpose And Ownership

Draft System turns an approved Draft opportunity into a set of runtime choices and one selected reward.

It owns:

- Candidate gathering from Stage-specific content pools
- Stage-authored Level-Up Draft category-slot allocation
- Eligibility-aware candidate representation
- Battle-local Held reward identity, single-use consumption and Pending Tower Upgrade reservation
- Candidate weighting and sampling
- Same-round displayed-choice deduplication
- Draft workflow state and result creation
- Explicit Draft workflow phase, Draft-session identity, and stale-callback rejection
- Battle-local free Re-roll budget, acceptance, and remaining balance
- Choice-set identity and atomic replacement within one Draft session
- Battle-simulation pause while the Draft Window is open

It does not own Player progression, Stage composition, UI layout, Tower placement, Tower Upgrade application, Map occupancy, or combat behavior.

---

# 2. Inputs And Outputs

Inputs:

- One Initial Tower Draft opportunity for each fresh Stage battle
- Player level-up opportunity
- Stage Tower Draft Pool
- Stage Tower Upgrade Draft Pool
- Stage Tower Draft Slot Probability
- Stage Initial Free Re-roll Count
- Player Re-roll intent for the current Draft session and choice set
- Current deployed Tower instances
- Tower Upgrade eligibility results
- Unconsumed Tower Upgrade Draft items

Outputs:

- One current displayed Draft choice set, replaceable through accepted Re-rolls
- Remaining free Re-roll balance and semantic Re-roll outcomes for presentation
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
    -> Allocate Requested Choice Categories
    -> Sample Distinct Identities Within Each Category
    -> Transfer Unfilled Slots Across Categories
    -> Randomize Final Display Order
    -> Establish Provisional Opening Session
    -> Present Choices With Session Identity
    -> Acquire Battle-Simulation Pause After Successful Opening
    -> Await One Selection
        -> Optionally Re-roll The Current Choice Set And Continue Awaiting Selection
    -> Claim Selection Commit Before Held-Item Creation
    -> Accept One Selection
    -> Create Held Draft Item
    -> Preserve Initial Completion Evidence When Applicable
    -> Close Draft Presentation
    -> Release Battle-Simulation Pause
    -> Publish Accepted Completion When Required
```

Only one choice from the active set may become a result. Presentation closure, duplicate input, or stale selection must not create additional rewards.

Each Draft session belongs to one fresh Battle generation and one unique attempt within that Battle. Stop, release, retry, session replacement, or disable invalidates the active session. Re-roll replaces its choice set while preserving that session. Completion and technical-failure facts are accepted only from the exact current session; identity must not collide with an earlier Battle.

Every session moves through one explicit workflow phase:

```text
None
    -> Opening
    -> Awaiting Selection
        -> Refreshing Choices
            -> Awaiting Selection With Replaced Choices Or Preserved Old Choices
        -> Committing Selection
            -> Completed
            -> Failed

Opening / Awaiting Selection / Refreshing Choices / Committing Selection
    -> Cancelled By Lifecycle Cleanup
```

The attempt identity is provisional before it is exposed to presentation callbacks. A successful opening acquires the attempt-owned pause before entering Awaiting Selection. Opening failure rolls the provisional session back synchronously and publishes no asynchronous failure fact.

Each displayed choice set also has an identity within its session. Selection and
Re-roll requests are bound to the set that produced them. A replaced set loses
input authority immediately, even if the same reward identity appears in the new
set. Session identity and reward membership alone do not authorize stale input.

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

Each accepted Player level-up opportunity builds separate Tower and eligible
Tower Upgrade candidate categories, applies the active Stage's Tower Draft Slot
Probability to the configured display slots, and then samples distinct reward
identities. It remains independent of the one Initial Tower Draft granted for
that Stage battle.

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
- Stop, release, session replacement, retry, and disable cancel the session and release its pause without publishing completion or technical failure.
- Re-roll keeps the same session-owned pause continuously; it neither resumes battle simulation nor acquires another pause.
- Nested or stale callbacks cannot release another session's pause.

Pausing prevents ordinary later Monster resolution and Player progress while a Draft is open. Same-frame reentrant level-up or selection callbacks are still rejected by session identity and exactly-once guards.

Slow motion while dragging a held Draft item is a separate future behavior. It does not share the Draft Window pause lifetime.

The attempt identity prevents another Draft from releasing the active pause; it cannot arbitrate an unrelated simulation-rate writer. If another runtime time owner is introduced, direct Draft ownership must be replaced by a shared pause or time-control service.

## 3.4 Free Re-roll

Each fresh Stage battle initializes its remaining free Re-roll count from the
Stage-authored non-negative budget. The Initial Tower Draft and all Level-Up
Drafts share that balance. Multiple Re-rolls may be spent in one Draft. Retry
starts a fresh configured budget; neither Stage transitions nor retry carry
unused balance. Presentation opening, hiding, or rebuilding does not reset it.

Re-roll replaces the entire displayed choice set and leaves the player with one
selection from the resulting set. It does not create a new Draft opportunity,
advance its ordinal, create or consume a held reward, change Pending reservations,
advance Player progress, or complete the Initial Draft. Initial Re-rolls remain
Tower-only. Wave timing still waits for the accepted Initial held Tower reward.

An actionable Re-roll requires a live battle, the exact current session and
choice-set identity, Awaiting Selection, an open presentation, the owned pause,
and positive remaining balance. Fixed Draft sequence mode rejects Re-rolls so
its authored steps retain their meaning. A fixed seed in Natural mode still
permits Re-rolls.

Draft gathers distinct eligible identities using the active session's source
rules, current Upgrade eligibility, and Pending reservations. Multiplicity is a
sampling weight, not an additional distinct identity. Zero-probability categories
contribute alternatives only when category backfill can actually reach them. If every eligible identity
is already displayed, Draft returns No Other Candidates without replacing the
set or spending balance. With three configured slots and unchanged eligibility,
this includes pools of one, two, or three distinct identities. This outcome is
normal feedback, not a technical failure. It does not disable an otherwise
actionable button; HUD presents a Toast when the player requests it.

Otherwise, Re-roll uses the existing category probability, identity weights,
deduplication, backfill, and shuffle rules through the Draft-owned random source.
It displays up to the configured choice count. Prior displayed identities are
not excluded. Partial or complete repetition of the previous set is valid and
spends one Re-roll when replacement succeeds; a changed order is not promised
to include a new reward. Re-roll cannot unlock ineligible content or guarantee a
TowerFamily, Upgrade, category composition, or Build.

Replacement follows one protected transaction:

```text
Accept Current Re-roll Intent And Claim Refreshing Choices
    -> Gather Eligible Identities And Check For Other Candidates
    -> Generate And Prepare Replacement Choices And Usable Presentation
    -> Revalidate Battle, Session, Choice-Set Identity, And Budget
    -> Commit New Set And Its Identity Together With Exactly One Budget Decrement
    -> Publish Committed Observation While Input Remains Protected
    -> Expose New Choices And Return To Awaiting Selection
```

Selection and further Re-roll requests are rejected during refresh. Preparation
does not expose new interactive choices or discard the old set. No Other
Candidates or preparation failure preserves the old choices, choice-set identity,
and balance, and returns a still-live session to Awaiting Selection. Cancellation
by battle or session termination instead discards preparation and never restores
an outgoing window or spends its budget. Repeated or stale requests cannot
commit the same replacement twice. After the last free Re-roll succeeds, the
new choices remain selectable with the exhausted Re-roll control shown.

A generated batch that fails presentation preparation still advances the random
stream; it does not spend balance or replace the current set. Rejected requests
and No Other Candidates do not draw random values. Reproducibility therefore
requires the same seed, eligibility, and complete request/failure history.

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

Tower candidate identities have equal weight inside the Tower category.

The Initial Tower Draft samples only these Tower Draft candidates. Later Player
level-up Drafts allocate Tower slots separately from Tower Upgrade slots before
sampling identities.

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

The definition contributes one candidate identity whose sampling multiplicity
equals its remaining eligible capacity. A non-positive result contributes no
candidate.

Pending reservation must cover:

- The same TowerUpgradeDefinition
- Any approved exclusive capacity shared by multiple definitions, such as one Elemental Layer slot per Tower

This produces Tower-instance-weighted discovery inside the Tower Upgrade
category: content usable by more current Towers has greater identity weight,
while already-held rewards reduce over-offering. Multiplicity never permits one
identity to occupy multiple display slots in the same Draft Window.

---

# 7. Category Allocation, Sampling, And Display

For a natural Player level-up Draft, each configured display slot independently
requests Tower with the active Stage's Tower Draft Slot Probability and requests
Tower Upgrade otherwise. With three configured slots, the requested category
composition may be `3/0`, `2/1`, `1/2`, or `0/3`. The first version does not
guarantee that both categories appear in every Level-Up Draft.

The probability controls requested display-slot categories. It is not a
whole-window category probability, a realized display-share guarantee, a
selected-reward ratio, or a guarantee of any Build path. The Initial Tower Draft
does not use this probability and samples only Tower identities.

Within the requested Tower slots, Draft System samples equal-weight Tower
identities. Within the requested Tower Upgrade slots, it samples identities
without display replacement using remaining eligible capacity as weight.
Displayed choices remain unique by reward identity:

- Tower Draft identity is its TowerDefinition.
- Tower Upgrade Draft identity is its TowerUpgradeDefinition.

The same identity must not appear twice in one displayed Draft set. Once an
identity is displayed, all of its multiplicity is excluded from later slots in
that Draft without changing its probability before selection.

If one category cannot fill its requested number of distinct slots, each
unfilled slot transfers to the other category. The Draft shows fewer choices
than the configured count only when both categories together contain fewer
distinct eligible identities than that count. It never repeats an identity or
invents invalid content to fill a slot.

After category sampling and backfill, Draft System randomizes the combined
display order through the same owned random source. UI position therefore does
not expose the internal category-processing order.

Natural sampling is reproducible under controlled seed input. The same seed,
Stage configuration, candidate state, and selection and Re-roll request history
produce the same category requests, weighted identity results, backfill, and
display order.
Observation records the seed and results but does not control the random source.

Raising a Tower level may make additional Stage-allowed Upgrade identities eligible for later candidate generation. It does not guarantee that any newly eligible identity appears in the next or a later Draft. Sampling risk remains part of the battle, while Stage authoring and Tower Upgrade System prevent a level transition that unlocks no possible Stage content.

---

# 8. Calibration Observation

Draft System exposes the initial displayed set and every committed Re-roll set
under the same Draft attempt without allowing observation to influence gameplay.
Each set observation identifies:

- Stage, seed, Draft ordinal, session kind, and generation mode;
- choice-set identity, initial opening versus Re-roll, and Re-roll ordinal;
- configured free budget and remaining balance before and after replacement;
- Tower Draft Slot Probability and requested category results;
- distinct candidates and their multiplicities by category;
- realized category counts after backfill and any exhaustion reason;
- final displayed identity order.

The final selection identifies its exact displayed set, identity, and category,
followed by held-item creation, Pending registration, and later consumption
outcome. Superseded sets remain offer evidence; they are not unconsumed rewards
or additional Draft attempts. Rejected and failed Re-roll requests can be
distinguished from accepted replacements and do not count as displayed sets or
budget consumption. Terminal evidence preserves remaining balance and the
committed set history before Stage cleanup.

Observation distinguishes Draft opportunity count, displayed-set count,
successful Re-roll count, and reward commit count. Probability analysis separates
Initial from Level-Up sets and initial offers from player-requested replacement
offers; selective Re-roll use is not an unbiased sample of original offers.
Fixed Draft fixtures remain without Re-rolls. Candidate-generation and report
contract versions identify the interpretation of recorded histories.

Calibration uses these facts to distinguish availability, player choice,
held-item creation, application or consumption, and final combat outcome. A
recorded fact cannot generate, weight, select, apply, or consume a reward.

---

# 9. Selection And Consumption Boundary

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

Held-item creation is atomic. A selected result is committed only after its model entry and usable presentation have been prepared and Battle/session authority has been revalidated. Preparation does not enable input. Draft then registers the entry and its view binding together; input becomes available only after selection commit finishes. Failure leaves no partial registration and does not publish accepted Draft completion.

The active session must atomically move from Awaiting Selection to Committing Selection before held-item creation begins. Object construction, activation, initialization, or nested presentation callbacks therefore cannot enter a second held-item transaction. Failure establishes Failed.

For an Initial Draft, successful held-item commit next preserves an attempt-scoped completed record containing the exact completed token and committed model entry, then establishes Completed. Active selection authority may then be invalidated without erasing the evidence required by Battle coordination. Presentation closes, pause releases, and completion publishes only after the record exists. The completed Initial record survives completion publication and is cleared only by fresh Battle reset or Stage cleanup.

---

# 10. Validation

Draft validation should report at minimum:

- Missing active Stage pools
- No valid TowerDefinition available for the required Initial Tower Draft
- Null or duplicate entries inside a Stage pool
- Definitions that fail owner-system validation
- Non-positive configured displayed choice count
- Stage Tower Draft Slot Probability outside the inclusive `[0, 1]` range
- Negative configured or remaining free Re-roll count
- Budget reset by presentation refresh, written back into StageDefinition, or carried between battles
- Re-roll accepted outside the current Awaiting Selection authority or in Fixed Draft sequence mode
- No Other Candidates calculated from raw weights or Stage lists instead of eligible distinct identities
- Balance spent or choices lost after a rejected or failed replacement
- Multiple decrements for one committed replacement
- Replaced-set input accepted, including an identity also present in the new set
- Refresh exposing partial choices or allowing overlapping selection or refresh commits
- Re-roll advancing Player progress, Draft ordinal, held rewards, or Initial completion
- Re-roll releasing or reacquiring the active Draft pause
- Replacement observations counted as extra Draft opportunities or rewards
- Requested category counts that do not sum to the configured choice count
- Upgrade multiplicity that disagrees with eligible capacity after reservation
- Duplicate displayed identity produced from candidate multiplicity
- An unfilled display slot while another distinct eligible identity is available
- Non-reproducible category, identity, backfill, or ordering results under the
  same controlled seed and candidate history
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

# 11. Approved Scope And Deferred Topics

Current scope includes:

- Stage-specific Tower and Tower Upgrade pools
- Stage-specific Tower Draft Slot Probability for Player level-up choices
- Stage-authored free Re-roll budget shared by Initial and Level-Up Drafts
- Atomic whole-set replacement, choice-set identity, and balance presentation
- No Other Candidates feedback without spending and cross-set repetition allowed
- Per-set Re-roll observation within one Draft opportunity
- One Initial Tower Draft for each fresh Stage battle
- Player level-up Draft opportunities
- Three-choice display when enough identities exist
- Independent category request per Level-Up Draft display slot
- Equal-weight Tower identity sampling inside the Tower category
- Tower-instance-weighted Upgrade identity sampling inside the Upgrade category
- Cross-category backfill when one category cannot fill its requested slots
- Seed-controlled final display ordering
- Pending Upgrade reservation
- Rogue-like sampling without guaranteed post-Level-Up offers
- Same-round displayed deduplication
- Held Tower and Tower Upgrade results
- Draft-owned battle-simulation pause while the Draft Window is open
- Battle-generation and attempt identity guards

Deferred topics include a guaranteed mixed-category Level-Up window, category
pity or streak protection, Build-responsive probability changes, guaranteed
TowerFamily or Upgrade identity, guaranteed new identities across Re-rolls,
paid Re-rolls, extra budget grants within a battle, per-card locking or selective
Re-roll, Draft queueing for multi-level batch progression, slow motion while
dragging a held Draft item, rarity, ban
or pick, global rewards, curses, persistent progression rewards, multiplayer
Drafts, and Stage-completion rewards.

### Pending ownership contract

Draft owns Battle-local Held reward identities, source Draft tokens and consumption.
HUD presents those entries; hiding or rebuilding presentation preserves ownership,
order and reservations. Only the exact current owner/entry may be consumed once.
Stop closes mutations while retaining terminal Pending evidence; Stage release
invalidates entries. Initial selection and Debug batches prepare usable presentation
before atomic registration. Failed preparation grants nothing. Deploy, Level Up and
Upgrade commit gameplay and exact consumption together; immutable investment evidence
precedes external notifications that may end the Stage. Rebuild cancels active drag
and rejects input from replaced views. Draft remains the pause/session authority.


### Placement submission and membership

Draft queries the stable submission owner for ordered read-only deployed Tower membership. Queries do not rebuild or prune members. Pending ownership remains in Draft and is bound directly to submission; view hierarchy is not the consumption authority.


## Re-roll Commit And Terminal Observation Boundary

Preparation uses an independent candidate set and one eligibility snapshot. The
current set always represents committed content. Final validation and model/view
ownership transfer contain no external notifications or presentation lifecycle work.

A committed replacement remains a successful Re-roll even if presentation is later
cancelled or fails. Record model commitment separately from presentation outcome.
A presentation failure belonging to the still-current Draft terminates that battle
as a technical failure for both Initial and Level-Up; an expired operation cannot
terminate a new battle. Precommit failures alone preserve the previous set/budget.

The operation remains protected through final request notifications and cleanup.
Battle termination immediately revokes gameplay, captures terminal budget/Pending,
and waits only for already-started observations to drain before finalizing evidence.
Each observed request has a unique identity, independently registered start and one
final result. Committed replacements and requests reconcile one-to-one. Natural
presentation identities and weights must match the recorded eligibility snapshot.


Recursive requests made while a Re-roll operation guard is held are rejected before
observation admission, preventing diagnostic callbacks from recursively producing
request events. Every admitted observed request has one independent start and final
result. Candidate history is owned solely by the attempt's ordered choice sets;
opportunity, selection and consumption data are separate from generation data.
