# Task013 - Selective Live Refresh

Status: Ready for review

Depends on: Task012

## 1. Goal

Implement event-driven Selective Live Refresh so approved tower level and upgrade changes affect the future unresolved behavior of already active owned Attack Entities, while preserving Release Snapshot decisions and immutable Entity State.

This task must not rebuild the tower, destroy all active entities, poll the full TowerUpgradeState every frame, or replay completed results.

## 2. Source Documents

- `Doc/07_TowerFrameworkSystem.md`
- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/Task/Task012_CombatBehaviourStructureMigration.md`
- `Doc/Task/Task001_BehaviourPackageAuthoringExpansion.md` through `Task011_DroneFinalDiveLifecycle.md`

The current System documents are authoritative for Live Refresh timing. Historical immutable-release wording remains applicable only to fields now classified as Release Snapshot or immutable Entity State.

## 3. Timing Model

| Category | Meaning |
|---|---|
| Static Authoring | Prefab or ScriptableObject data that does not change during a battle |
| Release Snapshot | A creation decision used only by releases confirmed after the change |
| Live Refresh | Approved future unresolved behavior on an already active entity is updated after the change |
| Entity State | Consumed history, progress, timers, captured positions, and completed results refresh never overwrites |

Refresh is pushed from a successful state-change notification. Attack Entities must not inspect complete upgrade state every frame.

## 4. Runtime Foundation

- Keep `TowerInstance.OnUpgradeRecorded` as the accepted-upgrade notification boundary.
- Add a successful level-change notification carrying enough previous/current information to compute resolved deltas and ratios.
- Subscribe and unsubscribe the owning concrete `TowerCombatBehaviour` symmetrically across initialization, enable, disable, reuse, and invalidation.
- Re-resolve old and new relevant values once per accepted change.
- Register active owned Projectiles, Magic Orbs, Drones, and relevant Drone-fired Projectiles with their source combat runtime.
- Unregister entities idempotently when they impact, complete, disable, despawn, or are cleaned up.
- Assign an owner-local `ReleaseGroupId` when one attack confirmation creates a release group.
- Preserve stable Archer `Center`, `Left`, and `Right` slot identity inside each Arrow group.
- Store per-group Hunting, Orb-companion, and Drone-companion retrofit guards.
- Push only typed refresh data or package-specific commands to the relevant entity type.
- Refresh iteration must tolerate an entity completing or unregistering during the notification.

## 5. Common Stat Refresh

| Change | Approved Active-Runtime Result |
|---|---|
| Attack Range | Refresh tower acquisition, active Hunting `TrackingRange`, and Drone range checks; preserve TrackingRangeOrigin and Cannon target positions |
| Attack Interval | Scale remaining tower cooldown by `newInterval / oldInterval`; do not restart it or grant a free attack |
| Damage | Refresh unresolved damage for active Arrows, Shells, Orbs, Drones, Final Dive, and Drone projectiles; never replay a resolved result |
| Magic Orb Rotation Speed | Refresh movement without changing orbit center, current angle, lifetime, or contact history |
| Magic Orb Max Hit Count | Add the resolved maximum delta to remaining hits; do not reset remaining count or history |
| Drone Battery Duration | Add the resolved duration delta to remaining battery; do not refill to the new maximum or restart state |
| Drone Burst Cooldown | Refresh future cadence and preserve the completion ratio of a running burst cooldown |

Use actual old and new resolved values after clamping when computing ratios or deltas.

## 6. Behaviour Package Refresh

### Archer

- Piercing Arrow is Live Refresh. Active Arrows gain the resolved piercing-capacity delta while preserving consumed hits and hit history.
- Scatter Arrow is Release Snapshot. Never add side Arrows to an existing group.
- Hunting Arrow is Live Refresh. Each eligible active group performs one virtual confirmation using stable slots and current tower targeting rules.
- Assign distinct valid targets where possible. A slot without a valid assignment remains Direction flight; do not reuse another slot's target.
- Tracking conversion preserves current position, direction history, lifetime, Piercing state, and completed hits.
- Existing invalidation, range failure, and one-way Tracking-to-Direction fallback remain unchanged; tracking never reacquires.

### Cannon

- Multi Shells is Release Snapshot. Never add Shells to an existing group.
- Explosive Shell is Live Refresh for an airborne Shell before Position Impact completes.
- Bouncing Shell may refresh an initial airborne Shell only before its first Position Impact.
- First Position Impact fixes remaining bounce count, resolved-target history, bounce arc, search radius, and local selector for that chain.
- Later damage or Explosive refresh may affect unresolved impacts under their own contracts; later Bouncing configuration cannot rewrite the active chain.

### Magic

- Multi Orbs adds exactly one companion to every eligible active pre-upgrade single-Orb group.
- Create the companion at the same orbit center opposite the original Orb's current angle.
- Use current resolved values and independent remaining hits, lifetime, contact cooldown history, and Elemental opportunities. Do not reset the original Orb.
- Arcane Detonation refreshes active incomplete Orbs and triggers only on later normal completion, never technical cleanup.
- Arcane Field keeps its existing immediate, tower-owned, exactly-once reconciliation and must not duplicate during other refreshes.

### Drone

- Twin Drones schedules exactly one companion launch for every eligible active pre-upgrade single-Drone group.
- Launch from the tower's current AttackOrigin after the authored delay and select a target at actual takeoff.
- Use current resolved values with a full independent battery and burst state. Do not reset the original Drone or tower cooldown.
- Blast Rounds refreshes active Drones and already-airborne unresolved Drone projectiles.
- Final Dive refreshes active Drones before battery-end resolution.
- Preserve Drone state, target history, orbit progress, consumed battery, remaining burst shots, and completed results.

### Elemental

- Elemental Layer remains a live lookup at the real attack boundary.
- Later eligible unresolved hit, contact, Position Impact, area, or lifecycle results use the tower's current Elemental profile.
- Earlier events are not replayed; ordinary child Effects, Buff ticks, reactions, overloads, and zones gain no new eligibility.

## 7. Cleanup And Idempotency

- Registration and unregistration tolerate repeated disable, completion, cleanup, and owner-destruction calls.
- Tower destruction/removal invalidates pending companion launches and stops refresh dispatch.
- A scheduled companion never launches from a missing or stale AttackOrigin.
- Inactive pooled entities are not registered to a former owner.
- Re-enable/reinitialize does not duplicate subscriptions, registrations, Arcane Fields, or retrofit guards.
- Repeated reconciliation creates no duplicate companion, Effect, VFX, field, or result.

## 8. Out of Scope

- Task012's `AttackConfig` removal and combat subtype split.
- Recreating the tower or clearing all owned Attack Entities after every upgrade.
- Per-frame upgrade polling or continuous direct combat-component reads from Attack Entities.
- Retroactive Scatter Arrows or Multi Shells.
- Resetting lifetimes, consumed hits, histories, captured positions, orbit progress, battery, state-machine progress, or completed Effects.
- A reflection-based refresh system, generic event bus, modifier framework, or new Attack Entity hierarchy.
- Balance changes, new content, presentation changes, or broad pooling redesign.
- Full composition regression; Task014 owns it after Tasks012-013 pass focused validation.

## 9. Focused Validation Scenarios

- Apply damage level changes while one active entity of every family is waiting to resolve.
- Change Attack Interval halfway through cooldown and verify proportional remaining time.
- Change Attack Range while Hunting Arrow and Drone are active.
- Apply Piercing and Hunting to active Arrow groups; apply Scatter and confirm no retrofit.
- Apply Explosive and pre-impact Bouncing to Shells; apply Multi Shells and confirm no retrofit; change Bouncing after chain start and confirm stability.
- Apply Multi Orbs to multiple active single-Orb groups and verify one opposite-angle companion per group.
- Apply Orb hit-count delta and Arcane Detonation without resetting Orb history.
- Apply Twin Drones to multiple active groups and verify delayed tower-origin launches, independent batteries, and unchanged tower cooldown.
- Apply Blast Rounds while a Drone projectile is airborne and Final Dive while a Drone is active.
- Apply an Elemental upgrade while each entity type is active and verify only later eligible events use it.
- Repeat reconciliation and cleanup to verify no duplicate or stale runtime state.

## 10. Unity Authoring Checklist

- Use observable values for range, interval, damage, Piercing count, Orb hit count/rotation, Drone battery, and burst cooldown.
- Keep valid upgrade assets for every Behaviour package used by focused scenarios.
- Prepare controlled Monster layouts for Hunting slots, Cannon pre/post-impact timing, delayed Drone launch, and range changes.
- Save assets before Play Mode so serialized state matches the Inspector.
- Record user-owned configuration failures separately from script defects.

Codex does not need to rebalance or rewrite user-owned assets unless explicitly requested.

## 11. Acceptance Criteria

- Successful level and upgrade changes notify the owning combat runtime exactly once.
- Active-entity registration, cleanup, and dispatch are source-owned, symmetric, and idempotent.
- Attack Entities receive only relevant typed refresh data and never poll complete upgrade state.
- Every common stat follows its approved rule and preserves Entity State.
- Scatter Arrow and Multi Shells remain Release Snapshot.
- Hunting Arrow, Multi Orbs, and Twin Drones retrofit every eligible group exactly once.
- Explosive, pre-chain Bouncing, Arcane Detonation, Blast Rounds, Final Dive, and Elemental follow approved active boundaries.
- Cooldown and capacity changes use resolved ratios or deltas rather than resetting state.
- No refresh replays completed damage, Effects, Elemental attempts, VFX, or lifecycle results.
- Owner invalidation and pooling leave no stale registrations or delayed companion work.
- Baseline outcomes from Task001-Task012 remain intact.

## 12. Validation And Handoff

- Add focused automated coverage only where existing seams make it proportionate.
- Run targeted compilation for the main Unity assembly.
- Run focused Unity Play Mode scenarios and capture Console warnings/errors.
- Distinguish configuration failures, Task012 migration failures, and Task013 refresh defects.
- Search for direct Attack Entity reads of complete upgrade state and stale full-snapshot assumptions.
- Run `git diff --check` and review final changed-file scope.
- Hand off to Task014 only after focused timing boundaries are confirmed.

## 13. Review Note

Task013 owns upgrade propagation semantics, not the component refactor or final combination certification. Omit helpers that exist only for hypothetical future packages.
