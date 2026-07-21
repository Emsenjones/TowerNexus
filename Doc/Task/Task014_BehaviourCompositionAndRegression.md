# Task014 - Behaviour Composition And Regression

Status: Deferred until Task012 and Task013 implementation is complete

Depends on: Task001-Task013

## 1. Goal

After Task012 and Task013 are complete, validate the natural Behaviour combinations produced by Tasks001-Task011, the approved Release Snapshot versus Live Refresh timing contract, and the concrete TowerCombatBehaviour subtype boundaries. Fix only concrete contract violations and deliver a repeatable Unity Play Mode scenario matrix without introducing a new composition or test framework.

## 2. Source Documents

- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`
- `Doc/Task/Task001_BehaviourPackageAuthoringExpansion.md` through `Task011_DroneFinalDiveLifecycle.md`
- `Doc/Task/Task012_CombatBehaviourStructureMigration.md`
- `Doc/Task/Task013_SelectiveLiveRefresh.md`

The current System documents are authoritative for combat-data ownership and Release Snapshot versus Live Refresh timing. Task001-Task011 remain authoritative for their package gameplay outcomes, but their historical AttackConfig or immutable-release wording is superseded where the current System documents explicitly replace it.

## 3. In Scope

- Verify all twelve Behaviour package identities and authoring validation.
- Verify the four existing upgrades were not reimplemented or regressed.
- Exercise every approved cross-package composition.
- Verify target invalidation, cleanup, same-frame ordering, typed runtime-data isolation, and immutable entity history.
- Verify Basic Layer and tower-level changes refresh eligible active entities without replaying resolved results.
- Verify Release Snapshot packages do not retrofit existing release groups.
- Verify Live Refresh packages update or supplement eligible existing release groups exactly once.
- Verify every live companion or Hunting retrofit is idempotent through stable release-group and slot identity.
- Verify Elemental opportunities remain independent from damage and are finalized by Buff runtime.
- Fix only defects that violate an approved Task001-Task013 contract.
- Record each manual scenario as Setup, Action, and Expected Result.

## 4. Out of Scope

- New Behaviour content.
- A generic composition handler.
- Universal Attack Entity or runtime-options frameworks.
- Large automated test infrastructure when none already exists.
- Implementing or completing Task012 combat-component migration or Task013 Selective Live Refresh inside Task014.
- Reinitializing the full tower or destroying all active Attack Entities when an upgrade is applied.
- Deferred implementation from earlier Tasks or the prerequisite migration.
- Balance tuning beyond values required to make scenarios observable.

## 5. Required Scenario Matrix

### Archer

- Piercing Arrow only.
- Scatter Arrow only.
- Hunting Arrow only.
- Apply Piercing Arrow while an Arrow is active; add remaining hits by delta without clearing history.
- Apply Scatter Arrow while an Arrow is active; do not supplement that release with side Arrows.
- Apply Hunting Arrow while one or more Arrow release groups are active; perform one virtual confirmation with fixed slots and preserve Direction flight for slots without valid targets.
- Change Attack Range while a Hunting Arrow is active; refresh TrackingRange while preserving TrackingRangeOrigin and one-way tracking fallback.
- Piercing + Scatter.
- Piercing + Hunting.
- Scatter + Hunting with sufficient distinct targets.
- Scatter + Hunting with insufficient distinct targets and no target reuse.
- Piercing + Scatter + Hunting with independent histories and lifetimes.

### Cannon

- Baseline position snapshot and local direct result.
- Multi Shells with one and multiple targets.
- Apply Multi Shells while a Shell is airborne; do not supplement the existing release.
- Explosive Shell with and without a direct target.
- Apply Explosive Shell while a Shell is airborne; execute the explosion at its unresolved Position Impact.
- Bouncing Shell with local candidates and exhausted history.
- Apply Bouncing Shell before an initial Shell's first Position Impact; enable the chain.
- Apply or change Bouncing Shell after a chain starts; preserve that chain's remaining count, history, arc, radius, and selector.
- Multi + Explosive.
- Multi + Bouncing.
- Explosive + Bouncing with post-explosion selection.
- All three upgrades with independent initial chains.
- Change Attack Range after Shell release; preserve its captured target position.

### Magic

- Multi Orbs baseline regression.
- Apply Multi Orbs while pre-upgrade single-Orb groups are active; create exactly one opposite-angle companion per eligible group without resetting the original Orb.
- Apply max-hit-count delta while an Orb is active; change remaining hits by delta without clearing contact history.
- Apply rotation-speed or damage changes while an Orb is active; refresh unresolved behavior.
- Arcane Detonation for each normal and forced end reason.
- Apply Arcane Detonation while an Orb is active; enable only future normal-completion resolution.
- Multi Orbs + Arcane Detonation independent completion.
- Arcane Field immediate activation, exactly-once ownership, tick cadence, and cleanup.
- Arcane Field remaining singular after later upgrades.

### Drone

- Twin Drones baseline regression.
- Apply Twin Drones while pre-upgrade single-Drone groups are active; launch exactly one companion per eligible group from the tower after the authored delay without resetting the original Drone or tower cooldown.
- Change Attack Range, battery duration, burst cooldown, or damage while a Drone is active; refresh the corresponding unresolved state and preserve elapsed history.
- Blast Rounds direct + explosion.
- Apply Blast Rounds while a Drone projectile is airborne; execute the explosion after its unresolved direct hit.
- Final Dive VFX-only aerial despawn paths.
- Apply Final Dive while a Drone is active; enable its future battery-end lifecycle.
- Final Dive dynamic target pursuit and last-valid fallback.
- Final Dive direct + explosion against the same surviving Monster.
- Final Dive direct kill followed by explosion target re-resolution.
- Twin Drones + Blast Rounds + Final Dive with independent entity state.

### Elemental And Cleanup

- Apply a damage level-up while each Attack Entity type is active; unresolved results use current damage and resolved results are not replayed.
- Change Attack Interval during tower cooldown; preserve the same remaining completion ratio.
- Apply an Elemental upgrade while each Attack Entity type is active; later eligible unresolved events use the current Elemental profile without replaying earlier events.
- Reapply runtime refresh reconciliation; no duplicate companion, field, Effect, or VFX is created.
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
- TowerDefinition validates the expected concrete TowerCombatBehaviour subtype without a separate AttackConfig asset.
- Attack Entities receive only relevant typed runtime data; live fields refresh only at approved boundaries and immutable entity history remains intact.
- Scatter Arrow and Multi Shells do not retrofit existing release groups.
- Hunting Arrow, Multi Orbs, and Twin Drones retrofit every eligible existing release group exactly once.
- Active Arrow, Shell, Orb, Drone, and Drone-projectile upgrades follow the approved Live Refresh rules.
- Current tower cooldown preserves its completion ratio when Attack Interval changes.
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

Task014 is a regression and contract-validation task. It begins only after Task012 and Task013 are complete. It is not permission to finish either prerequisite, redesign Tasks001-Task013, or absorb unfinished features into a final catch-all implementation.
