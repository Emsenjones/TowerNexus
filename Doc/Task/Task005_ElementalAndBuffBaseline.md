# Task005 - Elemental And Buff Baseline

Status: Planned

Depends on: Accepted Task004 Non-Elemental Upgrade Baseline

Blocks: Task006-Task014

## 1. Goal

Calibrate Elemental application, shared Buff runtime, FixedBuff damage,
stacking, Overload, Protection, and matching-source cooperation on top of the
accepted Tower and non-Elemental baselines.

## 2. Required Screens

- every TowerFamily and ElementType application boundary;
- one source applying each Elemental Buff;
- two matching source Towers contributing to one shared Buff;
- source-scoped apply cooldown;
- StackApplied, Overload, Protection, expiry, and re-entry;
- Burning periodic and Overload FixedDamage;
- Electric StackApplied and LightningStrike FixedDamage;
- Wind StackApplied and WindVortex FixedDamage;
- Cold slow and Frozen movement lock;
- Behaviour plus Elemental opportunity combinations;
- no recursive Elemental application from reaction damage.

## 3. Interpretation

- A single Elemental Upgrade provides useful normal application value.
- Matching sources and Overload provide the higher conditional ceiling.
- FixedBuff damage is independent from later source-Tower Level or Damage Bonus.
- Elemental is not required to outperform Behaviour in every geometry or target
  state.

## 4. Acceptance

- All FixedBuff values are positive, intentional, and Recorder-visible.
- All stack, cooldown, lifecycle, and Protection transitions are deterministic.
- Matching-source cooperation is stronger than isolated application without
  becoming an automatic result independent of coverage.
- Elemental opportunity counts match reviewed attack boundaries.
- No Buff or reaction damage reads current Tower BasicDamage.

