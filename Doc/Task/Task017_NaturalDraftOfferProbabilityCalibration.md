# Task017 - Natural Draft Offer Probability Calibration

Status: Completed on 2026-09-04. The simplified runtime and evidence contract,
Stage probability authoring, Recorder schema 25, and representative Natural-run
acceptance are complete.

Depends on: Accepted Task010-Task015 fixed-speed Stage Build envelopes

Deferred input: Task016 Fast Monster And Wave Substitution. The initial Task017
baseline excludes it. A later accepted substitution reopens only the affected
Stage's Natural runs and smallest relevant combat regression.

## 1. Goal

Give each natural Player level-up Draft an explicit Stage-authored balance
between Tower and Tower Upgrade choices. Eligible Upgrade content must not
dilute Tower availability merely because the player deploys more Towers, while
an Upgrade usable by more current Tower instances retains greater weight inside
the Upgrade category.

Recorder evidence must make each run explainable through displayed choices,
player selection, held-item creation, Pending and consumption, final Build,
placement, and combat outcome. Task017 demonstrates that the natural Draft loop
is functional, reproducible under a controlled seed, and capable of producing
playable coherent paths without guaranteeing a precise Build-completion or
Stage-completion probability.

## 2. Locked Inputs

Task010-Task015 continue to own and lock:

- Progress Requirements, Player Health, Monster Profiles, Waves, and timing;
- exact Stage Tower and Tower Upgrade pools;
- accepted Reference, coherent-alternative, and Anti-pattern Builds;
- accepted placement footprints and combat envelopes;
- Tower Upgrade eligibility and Pending reservation;
- deployment, Tower Level Up, Upgrade application, and consumption rules;
- the separate Tower-only Initial Draft and Fixed Draft eligibility validation.

Task017 must not weaken Stage pressure, change combat values, pad a content pool
with duplicates, or treat Fixed Draft evidence as Natural availability evidence.

## 3. Level-Up Offer Generation

Each StageDefinition authors one `Tower Draft Slot Probability` in the inclusive
range `[0, 1]`. For every configured Level-Up Draft display slot, Draft System
independently requests Tower with that probability and Tower Upgrade otherwise.
The current authored display count is three, so `3/0`, `2/1`, `1/2`, and `0/3`
requests are all legal. The implementation reads the configured slot count and
does not hard-code three.

Draft System builds two categories:

- Tower: one identity per valid Stage TowerDefinition, equal weight, unaffected
  by Pending Tower Drafts;
- Tower Upgrade: one identity per eligible Stage TowerUpgradeDefinition, with
  multiplicity equal to remaining eligible Tower capacity after Pending
  reservation.

Sampling is without display replacement. Upgrade multiplicity affects weighted
selection but never permits the same Upgrade identity to occupy two slots in one
window. An unfilled requested slot transfers to the other category. Fewer than
the configured number of choices is legal only when both categories together
contain fewer distinct eligible identities. The combined result receives a
final seeded shuffle before presentation.

## 4. Initial And Fixed Drafts

The Initial Draft remains Tower-only, does not use the Stage slot probability,
and completes only after a selected held Tower Draft item exists. Fixed Draft
continues to validate every configured result against natural eligibility, but
does not execute category rolls and contributes no Natural probability evidence.

## 5. Initial Stage Hypotheses

These values express the accepted Reference Builds' Tower-versus-Upgrade
resource tendency. They are calibration starting points, not mathematical
guarantees of the selected reward ratio or exact Build accessibility.

| Stage | Player Level-Up Drafts | Remaining Tower / Upgrade Cost | Initial Tower Slot Probability |
|---|---:|---:|---:|
| Stage1 | 4 | 2 / 2 | 0.500 |
| Stage2 | 5 | 3 / 2 | 0.600 |
| Stage3 | 6 | 4 / 2 | 0.667 |
| Stage4 | 7 | 5 / 2 | 0.714 |
| Stage5 | 9 | 6 / 3 | 0.667 |
| Stage6 | 14 | 8 / 6 | 0.571 |

## 6. Seed And Recorder Contract

Natural Draft generation uses a Draft-owned deterministic random source so
unrelated combat or presentation randomness cannot change offers. A recorded
seed, the same Stage and Draft-relevant gameplay history, and the same player
actions reproduce the offer sequence. Recorder observes but never controls the
random source.

Each run records Stage identity, configured display count, slot probability,
Draft seed, fixed-seed mode, generation-contract version, and random-algorithm
version.

Each Draft attempt records at minimum:

- attempt identity, ordinal, session kind, progression node, and Player state;
- distinct natural candidates and Upgrade multiplicity;
- requested category sequence and requested Tower/Upgrade counts when applicable;
- available distinct identity counts, realized counts, and backfill facts;
- final displayed identities and order;
- selected identity and whether held-item creation committed;
- consumption reconciliation through the existing investment-commit identity.

At battle terminal observation the Recorder freezes the current Pending Draft
snapshot before deferred report generation. Final reconciliation classifies a
committed held item as consumed, still Pending, or missing its investment commit.

## 7. Natural Run Interpretation

Before testing a Stage, the tester records a short manual choice policy: reward
priority, Tower deployment-or-Level-Up use, Tower or Upgrade target, accepted
placement, consumption timing, and ordered fallback when the preferred identity
is absent. This is a reproducible testing instruction, not an automated policy
framework.

A failed run is classified as one of:

- Natural offer availability: reasonable choices could not form an accepted
  path despite coherent player decisions;
- Player choice or Build allocation: useful displayed choices were rejected and
  the final Build became fragmented or otherwise incoherent;
- held-item or consumption outcome: a selected reward did not become the
  expected investment;
- placement or coverage: the Build was coherent but its deployment did not
  provide accepted strategic coverage;
- unexpected combat outcome: Draft, Build, consumption, and placement were
  coherent but combat departed from the accepted Stage envelope.

One failed run is a bad-luck or diagnostic sample. A Stage probability changes
only after repeated representative runs show the same availability pattern.

## 8. Implementation And Authoring

- Add validated Stage-local Tower Draft Slot Probability authoring.
- Supply and clear it with the active Stage Draft pools across preparation,
  release, retry, and replacement.
- Add a Draft-owned seed with an Editor fixed-seed input.
- Separate Tower and Upgrade candidate categories.
- Preserve Upgrade eligibility, multiplicity, and Pending reservation.
- Implement independent slot requests, distinct sampling, cross-category
  backfill, and final seeded shuffle.
- Preserve Initial and Fixed Draft behavior.
- Extend Recorder schema 25 and integrity for requested versus realized choices,
  selection, terminal Pending snapshot, and investment reconciliation.
- Author the Section 5 values in Stage1-Stage6; author a valid explicit value in
  the Default Stage; retain three choices in the production Draft configuration.

## 9. Validation Order

1. Build runtime and Editor assemblies.
2. Validate every authored Stage and the complete Stage binding lifecycle.
3. Prove fixed-seed replay under the same Draft-relevant gameplay history.
4. Verify requested category composition separately from realized composition.
5. Exercise legal category mixes, both backfill directions, total-identity
   exhaustion, identity uniqueness, Upgrade multiplicity, and Pending reservation.
6. Revalidate Initial Tower-only and Fixed eligibility behavior.
7. Exercise every Stage through representative exploratory Natural campaigns,
   including coherent choices and deliberate Anti-pattern choices where useful.
   Add runs when evidence is inconsistent or a configuration correction makes an
   earlier run diagnostic-only.
8. Inspect every report and classify any failure before changing probability.
9. Rerun the smallest Fixed Draft and consumption regressions.

Different seeds may legally produce the same choices. The selected seed set must
cover multiple legal outcomes; pairwise difference is not an invariant.

## 10. Acceptance

- Stage probability binds and clears without stale cross-Stage state.
- Fixed seed replays category requests, weighted identities, backfill, and order.
- Display identities are valid and unique; a short window occurs only under
  total distinct-identity exhaustion.
- Upgrade weight remains equal to remaining eligible Tower capacity.
- Initial and Fixed Draft contracts remain unchanged.
- Recorder reconciles requested and realized categories, displayed and selected
  identities, held-item creation, Pending, consumption, final Build, and result.
- Representative Natural records demonstrate that the implemented Draft
  contract functions across Stage1-Stage6, exposes meaningful player choices,
  and can produce accepted Reference or coherent-alternative paths. Every seed
  is not required to expose or complete either path.
- Failures can be attributed without changing accepted Task010-Task015 combat or
  Stage-pressure values.

The final evidence may claim demonstrated functional stability and observable
Natural accessibility across representative runs. It must not claim a
statistically proven Build-completion probability, guaranteed Reference Build,
or stable Stage completion for every coherent player policy.

## 11. Completion Evidence And Decision

The accepted Phase A evidence contains 44 schema-25 Natural records across all
six Stages:

| Stage | Runs | Victory / Defeat | Accepted interpretation |
|---|---:|---:|---|
| Stage1 | 8 | 6 / 2 | Reference and alternative play are viable with reasonable placement; incoherent play can still fail. |
| Stage2 | 8 | 3 / 5 | Early access to a Level 2 Tower is an important player decision and timing risk rather than a Draft runtime defect. |
| Stage3 | 6 | 4 / 2 | Tower-only Anti-pattern play fails; observed category variation remains legal under independent slot rolls. |
| Stage4 | 8 | 5 / 3 | Reference and coherent-alternative play pass with reasonable placement; some Anti-pattern runs are near the survival boundary. This cohort used six health; the subsequently accepted asset value is five. |
| Stage5 | 12 | 3 / 9 | The first four runs used the temporary five-health fixture and remain diagnostic only. At the restored six-health fixture, coherent Elemental Magic paths can pass, while alternative Elemental-family paths are substantially narrower. |
| Stage6 | 2 | 0 / 2 | Both records completed valid Draft observation; exact same-source Elemental construction remains narrow under the available Draft budget and identity randomness. |

All retained records report schema 25 and pass the Draft selection, Draft count,
generation-trace, consumption-reconciliation, and investment-commit integrity
checks applicable to their terminal state. The records expose displayed choices,
selection, consumption, final investment, placement, leaks, and battle outcome;
the observed defeats can therefore be investigated without inferring Draft
behavior from the final Build alone.

The Stage5 and Stage6 results do not justify weakening their accepted combat
pressure or Reference Build definitions inside Task017. They establish a known
v0.1 experience limitation: narrow coherent paths can require favorable offers
and informed adaptation. This is acceptable for the first playable demo because
the rogue-like variability and complete Stage flow remain available even when a
run ends in Defeat. Stage-authored reroll opportunities, pity, or other
accessibility assistance remain separate future experience work.

Task017 is therefore accepted as complete for v0.1 on functional correctness,
cross-Stage integration, authoring, observability, and representative Play Mode
stability. It is not accepted as proof that every Stage reliably offers or
completes its Reference or coherent-alternative Build under every seed.

## 12. Out Of Scope

- Automated large-cohort simulation or statistical probability guarantees
- Synthetic Build-state evaluators or automatic choice-policy frameworks
- A permanent legacy-versus-category generation switch
- Category pity, guaranteed mixed windows, adaptive Build-aware probability,
  rerolls, bans, rarity, curses, persistent progression, or global rewards
- Guaranteed TowerFamily, Upgrade layer, ElementType, identity, or exact
  Reference sequence
- Task016 Fast-Monster substitution in the initial evidence
