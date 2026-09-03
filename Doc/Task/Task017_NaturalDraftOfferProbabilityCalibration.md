# Task017 - Natural Draft Offer Probability Calibration

Status: Ready

Depends on: Accepted Task010-Task015 fixed-speed Stage Build envelopes

Deferred input: Task016 Fast Monster And Wave Substitution. The initial Task017
baseline intentionally excludes it; later accepted substitutions require the
affected Stage cohorts to be re-run.

## 1. Goal

Calibrate the natural Draft offer mix between Tower Draft items and Tower
Upgrade Draft items, including the relative availability of eligible identities,
after combat values and all six fixed-speed Stage configurations are stable.

This Task determines whether a coherent Stage-appropriate Build is reasonably
available through natural offers without guaranteeing the exact Reference Build.
It separates offer availability from player choice. Task010-Task015 intentionally
do not decide whether a final-Build deviation happened because the player chose a
different reward or because natural sampling never offered a viable path.

## 2. Locked Inputs

- accepted Task010-Task015 Progress Requirements, Monster Profiles, Waves,
  Player Health, Reference Builds, coherent alternatives, and Anti-patterns;
- exact Stage Tower and Tower Upgrade pools accepted by Task010-Task015;
- normal Draft eligibility, Pending reservation, unique-display, Level Up,
  placement, Upgrade application, and consumption rules.

Task016 is not a locked input for the initial baseline. If it later changes a
Stage Wave, that Stage's natural-offer cohort and the relevant combat regression
must be repeated before the new result supersedes the fixed-speed evidence.

Task017 must not weaken a Wave, change Monster HP or MoveSpeed, alter Player
Health, rebalance Tower combat, or reinterpret a Fixed-run Build result to hide
an offer-distribution problem.

## 3. Baseline Sampling Contract

Natural mode is the authority for probability evidence. Fixed mode may reproduce
an already accepted Build but contributes no displayed-offer frequency evidence.

The first baseline uses the current Draft contract:

- each valid Stage TowerDefinition contributes one internal candidate entry;
- each eligible TowerUpgradeDefinition contributes one entry per remaining
  eligible Tower instance after Pending reservations;
- Player level-up Drafts merge Tower and Upgrade entries;
- displayed choices are unique by reward identity;
- the Initial Draft is Tower-only and is reported separately from combined
  Player level-up Drafts.

An explicit category or identity weighting rule may be introduced only when the
baseline evidence demonstrates a reviewed accessibility or dilution defect.
Stage pools must not be padded with fake duplicate content merely to simulate
weight.

## 4. Controlled Choice Policies

Probability runs use named, deterministic selection policies so offer
availability is not confused with changing player preference. At minimum:

- Reference-seeking: choose the highest-priority naturally displayed reward on
  the accepted Reference path;
- Alternative-seeking: follow one accepted coherent alternative path;
- capability-seeking fallback: when the planned identity is absent, choose a
  displayed reward that preserves the Stage's required capability when one
  exists.

Every run records whether the next desired reward was naturally eligible,
displayed, selected, successfully consumed, or never offered. A reward that was
displayed but rejected is a choice outcome; a reward that remained eligible but
was never displayed is an availability outcome.

## 5. Required Evidence

For every Stage, use reproducible seeded cohorts large enough for the selected
acceptance bounds and report:

- Tower-versus-Upgrade displayed-choice share overall and at each Player level-
  up Draft step;
- Basic, Behaviour, and Elemental Upgrade displayed-choice share when eligible;
- per-family and per-identity eligibility-to-display rate;
- natural candidate multiplicity at each Draft step;
- no-useful-offer and path-dead-end rate;
- completion rate for the Reference-seeking and coherent-alternative policies;
- frequency and cause of horizontal dilution, premature over-concentration, and
  missing-capability outcomes;
- confidence interval or an equivalent uncertainty statement for every accepted
  probability threshold.

The exact number of seeds, category-share bands, and coherent-Build accessibility
thresholds are Task outputs. They must be selected before changing weights and
then reused for the post-change comparison.

## 6. Tuning Order

1. Validate Recorder integrity, seed identity, Stage identity, natural mode, and
   the complete candidate/display/selection/consumption chain.
2. Measure the unmodified candidate and multiplicity rules.
3. Identify whether any failure comes from Stage pool composition, runtime
   eligibility, candidate multiplicity, unique-display sampling, or the named
   selection policy.
4. Apply the smallest owning change. Prefer an explicit and explainable weighting
   contract when weighting is the defect.
5. Repeat the same seeded cohorts and selection policies.
6. Re-run only the smallest Fixed Stage regression needed to prove that a Draft-
   system change did not alter eligibility or consumption semantics. Fixed runs
   do not replace the natural probability evidence.

## 7. Acceptance

- Natural Draft offers meet the reviewed minimum accessibility thresholds for
  the Stage's required capability and for more than one coherent Build where the
  accepted Stage envelope supports alternatives.
- The exact Reference sequence is not silently guaranteed and is not treated as
  the only valid player answer.
- Tower offers remain meaningful without persistently diluting eligible Upgrade
  progression; Upgrade offers do not eliminate useful horizontal expansion.
- Stage5 Elemental specialization and Stage6 matching-Element cooperation remain
  reasonably reachable under the reviewed natural policies.
- Every reported final-Build deviation is classified as availability, player-
  choice-policy, application/consumption failure, or combat outcome rather than
  being inferred from the final snapshot alone.
- Any new weighting rule is deterministic under seed control, documented in the
  Draft System contract, and covered by retry and next-Stage reset regressions.
- No accepted Task010-Task015 combat or Stage pressure value is changed to
  compensate for Draft probability.
