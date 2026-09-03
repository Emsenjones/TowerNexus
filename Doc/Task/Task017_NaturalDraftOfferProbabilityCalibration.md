# Task017 - Natural Draft Offer Probability Calibration

Status: In Progress; Draft offer-generation contract approved for documentation
on 2026-09-04. Runtime implementation and natural-offer evidence are pending.

Depends on: Accepted Task010-Task015 fixed-speed Stage Build envelopes

Deferred input: Task016 Fast Monster And Wave Substitution. The initial Task017
baseline intentionally excludes it; later accepted substitutions require the
affected Stage cohorts to be re-run.

## 1. Goal

Calibrate natural Player level-up Draft offers so eligible Tower Upgrade content
cannot dilute Tower Draft availability merely because the player deploys more
Towers, while preserving the approved rule that an Upgrade usable by more
current Tower instances has greater probability inside the Upgrade category.

This Task determines whether more than one coherent Stage-appropriate Build is
reasonably accessible without guaranteeing the exact Reference Build. It
separates offer availability, player choice, Pending and consumption outcomes,
and final combat results.

## 2. Source Documents And Locked Inputs

Source documents:

- `Doc/System/02_StageSystem.md`
- `Doc/System/08_DraftSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/Balance/00_StageDesignBlueprint.md`
- accepted Task010-Task015 Stage calibration contracts and evidence

Locked inputs:

- accepted Task010-Task015 Progress Requirements, Monster Profiles, Waves,
  Player Health, Reference Builds, coherent alternatives, and Anti-patterns;
- exact Stage Tower and Tower Upgrade pools accepted by Task010-Task015;
- Tower Upgrade eligibility and Pending reservation rules;
- same-round unique display, held-item creation, Level Up, placement, Upgrade
  application, and consumption rules;
- Tower Draft dual use for new deployment or same-family Tower Level Up;
- one Tower-only Initial Draft outside Player progression.

Task016 is not a locked input for the initial baseline. If it later changes a
Stage Wave, that Stage's natural-offer cohort and relevant combat regression
must be repeated before the new result supersedes fixed-speed evidence.

Task017 must not weaken a Wave, change Monster HP or MoveSpeed, alter Player
Health, rebalance Tower combat, or reinterpret a Fixed-run Build result to hide
an offer-distribution problem.

## 3. Existing Baseline Contract

Natural mode is the authority for probability evidence. Fixed mode may reproduce
an already accepted Build but contributes no displayed-offer frequency evidence.

The pre-Task017 runtime baseline is:

- each valid Stage TowerDefinition contributes one internal candidate entry;
- each eligible TowerUpgradeDefinition contributes one entry per remaining
  eligible Tower instance after Pending reservations;
- Player level-up Drafts merge Tower and Upgrade entries before sampling;
- displayed choices are unique by reward identity;
- the Initial Draft is Tower-only and reported separately.

The unmodified baseline must be measured before the approved category-allocation
candidate replaces it. Stage pools must not be padded with duplicate content to
simulate weight.

## 4. Approved Level-Up Offer-Generation Candidate

### 4.1 Stage-Local Slot Probability

Each StageDefinition authors one `Tower Draft Slot Probability` in the inclusive
range `[0, 1]`. It applies only to the three requested display slots in each
natural Player level-up Draft.

It is not a whole-window Tower probability, a displayed-share guarantee, a
selected-reward ratio, or a Reference-Build guarantee. The complementary Tower
Upgrade slot probability is `1 - Tower Draft Slot Probability`.

### 4.2 Separate Candidate Categories

Draft System first builds two categories:

- Tower candidates: one identity per valid Stage TowerDefinition, with equal
  identity weight. Pending Tower Drafts do not reduce this category.
- Tower Upgrade candidates: one identity per eligible Stage
  TowerUpgradeDefinition, with multiplicity equal to remaining eligible Tower
  capacity after Pending reservation.

Tower Upgrade multiplicity affects weighted selection inside the Upgrade
category. It never permits the same Upgrade identity to occupy multiple display
slots in one Draft Window.

### 4.3 Independent Slot Requests

For the current three-choice design, Draft System performs three independent
category rolls. Each roll requests Tower with the Stage-authored Tower Draft
Slot Probability and otherwise requests Tower Upgrade.

The three requests produce `Requested Tower Slots = X` and
`Requested Tower Upgrade Slots = Y`, where `X + Y = 3`. Natural outcomes may
therefore request `3/0`, `2/1`, `1/2`, or `0/3`. The first version adds no rule
that guarantees both categories in every Level-Up Draft.

### 4.4 Unique Weighted Sampling And Cross-Category Backfill

Draft System samples up to `X` distinct Tower identities and up to `Y` distinct
Tower Upgrade identities. Tower identities are equal-weight. Upgrade identities
are sampled without display replacement using their remaining-capacity
multiplicity as weight.

If one category cannot fill its requested distinct slots, every unfilled slot is
transferred to the other category. The Draft Window shows fewer than three
choices only when both categories together contain fewer than three distinct
eligible identities. It never invents an invalid reward or repeats one identity
to satisfy a requested count.

After sampling and backfill, Draft System randomizes the combined display order
through the same controlled random source so UI position does not expose the
internal category-processing order.

### 4.5 Initial And Fixed Modes

The Initial Draft remains Tower-only and does not consume the Stage slot
probability. Fixed Draft mode continues to validate configured results against
natural eligibility but does not execute category rolls and contributes no
probability evidence.

## 5. Initial Stage Probability Hypotheses

The first candidate value for each Stage is derived from its accepted Reference
Build resource cost:

```text
Reference Tower Draft Cost
    = Tower Deployments
    + Sum Of Tower Level Increases

Remaining Reference Tower Draft Cost
    = Reference Tower Draft Cost - One Initial Tower Draft

Initial Tower Draft Slot Probability
    = Remaining Reference Tower Draft Cost
    / Player Level-Up Draft Count
```

| Stage | Player Level-Up Drafts | Remaining Tower / Upgrade Cost | Initial Tower Slot Probability |
|---|---:|---:|---:|
| Stage1 | 4 | 2 / 2 | 0.500 |
| Stage2 | 5 | 3 / 2 | 0.600 |
| Stage3 | 6 | 4 / 2 | 0.667 |
| Stage4 | 7 | 5 / 2 | 0.714 |
| Stage5 | 9 | 6 / 3 | 0.667 |
| Stage6 | 14 | 8 / 6 | 0.571 |

These are testable initial hypotheses, not accepted final balance values. They
shape requested display-slot categories rather than guaranteeing selection
counts. If accepted coherent alternatives use materially different category
costs, the reviewed coherent-Build range takes precedence over fitting only the
Reference Build.

## 6. Ownership

- StageDefinition owns the reusable Tower Draft Slot Probability authoring.
- Stage System validates and supplies that authored value with the Stage pools.
- Draft System owns category rolls, distinct identity sampling, Upgrade
  multiplicity, cross-category backfill, final order, and Draft observations.
- Tower Upgrade System remains the sole eligibility and application authority.
- Battle HUD UI System stores Pending Draft items and presents supplied choices;
  it does not generate, weight, refill, or reroll them.
- The calibration Recorder observes Draft facts and never changes gameplay
  candidates, weights, random results, selection, or consumption.

## 7. Seed And Observation Contract

Natural probability evidence uses a Draft-owned controlled random source. A
recorded seed, the same Stage and candidate state, and the same controlled
selection policy must reproduce category requests, weighted identities,
backfill, and final display order. Recorder observation does not own or mutate
the random source.

Each Draft attempt records at minimum:

- seed, Stage identity, Draft ordinal, session kind, and generation mode;
- Tower Draft Slot Probability and the three category-roll results;
- requested Tower and Tower Upgrade slot counts;
- each distinct natural candidate identity and its multiplicity;
- available distinct identity count per category;
- realized category counts after backfill;
- backfill direction and reason, including total-identity exhaustion;
- final displayed identities and order;
- selected identity and category;
- held-item creation or Pending registration outcome;
- successful consumption or unresolved Pending outcome.

Final-Build deviation must be classified as availability, controlled-choice,
held-item creation, application or consumption, or combat outcome. Recorder
facts diagnose the chain; they do not guarantee that every random run wins.

## 8. Controlled Choice Policies

Probability runs use named deterministic selection policies so offer
availability is not confused with changing player preference. At minimum:

- Reference-seeking: choose the highest-priority naturally displayed reward on
  the accepted Reference path;
- Alternative-seeking: follow one accepted coherent alternative path;
- capability-seeking fallback: when the planned identity is absent, choose a
  displayed reward that preserves the Stage's required capability when one
  exists.

A displayed but rejected reward is a choice outcome. A reward that remained
eligible but was never displayed is an availability outcome.

## 9. Required Evidence

For every Stage, use reproducible seeded cohorts large enough for the selected
acceptance bounds and report:

- requested and realized Tower-versus-Upgrade choice share overall and at every
  Player level-up Draft step;
- category absence and `3/0`, `2/1`, `1/2`, and `0/3` request frequencies;
- cross-category backfill and fewer-than-three-choice rates with causes;
- Basic, Behaviour, and Elemental Upgrade displayed-choice share when eligible;
- per-family and per-identity eligibility-to-display rate;
- natural candidate multiplicity at each Draft step;
- no-useful-offer and path-dead-end rate;
- completion rate for Reference-seeking and coherent-alternative policies;
- frequency and cause of horizontal dilution, premature over-concentration, and
  missing-capability outcomes;
- confidence interval or equivalent uncertainty for every accepted threshold.

Seed count, category-share bands, and coherent-Build accessibility thresholds
must be selected before changing runtime weights and reused for comparison.

## 10. Implementation And Authoring Checklist

- Add validated Stage-local Tower Draft Slot Probability authoring without
  changing accepted Stage pools, Progress, Waves, Player Health, or combat.
- Supply the probability to Draft System with the active Stage pools and clear
  all Stage-bound Draft configuration on release, retry, or replacement.
- Separate Tower and Upgrade category construction for natural Level-Up Drafts.
- Preserve eligible-instance Upgrade multiplicity and Pending reservation.
- Implement three independent category rolls, unique weighted sampling,
  cross-category backfill, and final seeded display shuffle.
- Preserve Initial Draft and Fixed Draft behavior.
- Add controlled seed input and complete per-attempt Recorder observations.
- Add deterministic controlled-choice cohort execution for accepted Reference
  and coherent-alternative policies.
- Author the Section 5 values only as initial Task017 candidates until evidence
  accepts or revises them.

## 11. Execution Order

1. Validate the existing Recorder candidate, display, selection, Pending,
   consumption, Stage, and generation-mode chain.
2. Add controlled Draft seed and missing baseline observations without changing
   the current merged-pool algorithm.
3. Choose cohort size and accessibility thresholds, then measure the unmodified
   merged-pool baseline.
4. Implement the approved category-allocation candidate and initial Stage
   probability hypotheses.
5. Repeat the exact same seeds and controlled choice policies.
6. Adjust only the smallest owning Stage probability or sampling rule justified
   by evidence. Do not pad content pools.
7. Re-run the smallest Fixed Stage regression needed to prove eligibility,
   Pending, and consumption semantics remain unchanged.

## 12. Acceptance

- The controlled seed and observation chain is internally consistent and
  reproducible for retry and next-Stage boundaries.
- Requested and realized category counts, identity multiplicity, backfill,
  display, selection, Pending, consumption, and combat outcome reconcile.
- Valid identities from the other category fill requested-category shortages;
  fewer than three choices occurs only under total distinct-identity exhaustion.
- Upgrade identity probability remains weighted by remaining eligible Tower
  capacity without same-round duplicate display.
- Natural offers meet reviewed accessibility thresholds for each Stage's
  required capability and for more than one coherent Build where supported.
- The exact Reference sequence is not silently guaranteed or treated as the
  only valid player answer.
- Tower offers remain meaningful without persistently diluting eligible Upgrade
  progression; Upgrade offers do not eliminate useful Tower investment.
- Stage5 Elemental specialization and Stage6 coherent Elemental paths remain
  reasonably reachable under reviewed policies.
- No accepted Task010-Task015 combat or Stage-pressure value changes to
  compensate for Draft probability.

## 13. Out Of Scope

- Guaranteed mixed-category Level-Up windows
- Category pity or protection against random streaks
- Runtime probability adaptation based on the player's current Build
- Guaranteed TowerFamily, Upgrade layer, ElementType, identity, or Reference path
- Reroll, ban, rarity, curses, persistent progression, or global rewards
- Task016 Fast-Monster identity and Wave substitutions in the initial cohort
