# Tower Nexus - Draft System

---

# 1. Purpose And Ownership

Draft System turns Player level-up opportunities into a set of runtime choices and one selected reward.

It owns:

- Candidate gathering from Stage-specific content pools
- Eligibility-aware candidate representation
- Pending Tower Upgrade reservation
- Candidate weighting and sampling
- Same-round displayed-choice deduplication
- Draft workflow state and result creation

It does not own Player progression, Stage composition, UI layout, Tower placement, Tower Upgrade application, Map occupancy, or combat behavior.

---

# 2. Inputs And Outputs

Inputs:

- Player level-up opportunity
- Stage Tower Draft Pool
- Stage Tower Upgrade Draft Pool
- Current deployed Tower instances
- Tower Upgrade eligibility results
- Unconsumed Tower Upgrade Draft items

Outputs:

- One displayed Draft choice set
- One selected Tower Draft or Tower Upgrade Draft result

The first version displays three distinct choices when at least three distinct eligible identities are available.

---

# 3. Draft Workflow

```text
Player Level-Up Opportunity
    -> Build Tower Draft Candidates
    -> Build Tower Upgrade Draft Candidates
    -> Merge Candidate Entries
    -> Sample Distinct Display Choices
    -> Present Choices
    -> Accept One Selection
    -> Create Held Draft Item
```

Only one choice from the active set may become a result. Presentation closure, duplicate input, or stale selection must not create additional rewards.

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

Tower Draft and Tower Upgrade candidate entries are merged before sampling.

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

---

# 9. Validation

Draft validation should report at minimum:

- Missing active Stage pools
- Null or duplicate entries inside a Stage pool
- Definitions that fail owner-system validation
- Non-positive configured displayed choice count
- Pending reservation that cannot identify its reward or exclusive capacity
- A selected identity not present in the active displayed set

Validation does not silently add content to a Stage or alter Tower Upgrade rules.

---

# 10. Approved Scope And Deferred Topics

Current scope includes:

- Stage-specific Tower and Tower Upgrade pools
- Player level-up Draft opportunities
- Three-choice display when enough identities exist
- Equal weight per internal candidate entry
- Tower-instance-weighted Upgrade candidates
- Pending Upgrade reservation
- Same-round displayed deduplication
- Held Tower and Tower Upgrade results

Deferred topics include rarity, reroll, ban or pick, global rewards, curses, persistent progression rewards, multiplayer Drafts, and Stage-completion rewards.
