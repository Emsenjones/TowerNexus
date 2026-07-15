# Task012 - Behaviour Composition And Regression

Status: Ready for implementation

Depends on: Task001-Task011

## 1. Goal

Validate the natural Behaviour combinations produced by Tasks001-011, fix only concrete contract violations, and deliver a repeatable Unity Play Mode scenario matrix without introducing a new composition or test framework.

## 2. Source Documents

- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`
- `Doc/Task/Task001_BehaviourPackageAuthoringExpansion.md` through `Task011_DroneFinalDiveLifecycle.md`

## 3. In Scope

- Verify all twelve Behaviour package identities and authoring validation.
- Verify the four existing upgrades were not reimplemented or regressed.
- Exercise every approved cross-package composition.
- Verify target invalidation, cleanup, same-frame ordering, and immutable runtime option isolation.
- Verify Elemental opportunities remain independent from damage and are finalized by Buff runtime.
- Fix only defects that violate an approved Task001-011 contract.
- Record each manual scenario as Setup, Action, and Expected Result.

## 4. Out of Scope

- New Behaviour content.
- A generic composition handler.
- Universal Attack Entity or runtime-options frameworks.
- Large automated test infrastructure when none already exists.
- Deferred implementation from earlier Tasks.
- Balance tuning beyond values required to make scenarios observable.

## 5. Required Scenario Matrix

### Archer

- Piercing Arrow only.
- Scatter Arrow only.
- Hunting Arrow only.
- Piercing + Scatter.
- Piercing + Hunting.
- Scatter + Hunting with sufficient distinct targets.
- Scatter + Hunting with target reuse.
- Piercing + Scatter + Hunting with independent histories and lifetimes.

### Cannon

- Baseline position snapshot and local direct result.
- Twin Shells with one and multiple targets.
- Explosive Shell with and without a direct target.
- Bouncing Shell with local candidates and exhausted history.
- Twin + Explosive.
- Twin + Bouncing.
- Explosive + Bouncing with post-explosion selection.
- All three upgrades with independent initial chains.

### Magic

- Twin Orbs baseline regression.
- Arcane Detonation for each normal and forced end reason.
- Twin Orbs + Arcane Detonation independent completion.
- Arcane Field immediate activation, exactly-once ownership, tick cadence, and cleanup.
- Arcane Field remaining singular after later upgrades.

### Drone

- Twin Drones baseline regression.
- Blast Rounds direct + explosion.
- Final Dive VFX-only aerial despawn paths.
- Final Dive dynamic target pursuit and last-valid fallback.
- Final Dive direct + explosion against the same surviving Monster.
- Final Dive direct kill followed by explosion target re-resolution.
- Twin Drones + Blast Rounds + Final Dive with independent entity state.

### Elemental And Cleanup

- Zero direct damage with a valid application opportunity.
- Zero explosion/tick damage with valid explicit opportunities.
- Direct + explosion attempts against one Monster.
- BuffApplyCooldown and Protection blocking later attempts.
- No recursive application from Buff ticks, reactions, overloads, zones, or ordinary child Effects.
- Target death, tower removal, battle cleanup, reset, and owner invalidation.

## 6. Unity Authoring Checklist

- Prepare one valid upgrade asset for every final Behaviour package.
- Assign every required Effect reference, positive radius/interval/threshold/count, and relevant prefab reference.
- Clear the old Cannon baseline explosion gameplay Effect while preserving impact feedback.
- Configure Drone aerial-despawn VFX presentation.
- Prepare controlled Monster layouts for distance, range, death, and target-count cases.
- Record any user-owned configuration still required before running a scenario.

Codex does not need to create or rebalance user-owned assets unless explicitly requested, but validation cannot claim a scenario passed until its required Unity content is configured.

## 7. Acceptance Criteria

- Every scenario has an explicit Setup, Action, and Expected Result.
- All twelve packages can be authored and applied to the correct TowerFamily.
- Existing Piercing, Scatter, Twin Orbs, and Twin Drones behavior remains intact.
- Combinations emerge from independent package implementation rather than a new combination framework.
- Released entities receive only relevant immutable runtime options.
- Cannon bounce selection observes the complete post-landing synchronous state.
- Arcane Field activates immediately and remains exactly once.
- Final Dive direct and explosion results follow the approved ordering and eligibility rules.
- Elemental attempts are not gated by positive damage.
- BuffApplyCooldown, Protection, and Buff state decide final application outcomes.
- All cleanup paths stop future behavior and avoid duplicate Effects or VFX.
- Any fixes remain within the ownership boundary of the violated Task contract.

## 8. Validation And Handoff

- Prefer existing automated coverage where available.
- Otherwise run the documented Unity Play Mode matrix.
- Capture Unity Console warnings/errors and distinguish configuration failures from runtime defects.
- Run `git diff --check`, targeted stale-rule searches, and a final changed-file scope review.
- Report unconfigured user-owned assets separately from implementation failures.
- Do not mark the Behaviour Layer complete while a required scenario is untested or contract-incorrect.

## 9. Review Note

Task012 is a regression and contract-validation task. It is not permission to redesign Tasks001-011 or absorb unfinished features into a final catch-all implementation.
