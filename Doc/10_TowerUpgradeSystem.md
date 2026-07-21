# Tower Nexus - Tower Upgrade System

---

# 1. System Overview

The Tower Upgrade System is responsible for defining how towers grow during a battle.

The Tower Upgrade System defines what upgrades exist, how upgrades are categorized, how tower progression is structured, how tower level-up requests are processed, and how upgrades are applied to individual tower instances. It does not own Draft UI presentation or tower placement validation.

This system owns:

- Tower upgrade layers
- Tower upgrade progression
- Tower level progression
- Tower level-up request validation
- Upgrade definitions
- Upgrade application rules
- Required tower level unlock rules
- Per-tower duplicate upgrade rules
- Future upgrade prerequisites and evolution paths

The Tower Upgrade System does not own:

- Draft generation
- Draft UI presentation
- Runtime combat execution
- Projectile movement
- Buff execution
- Placement validation
- Placement or upgrade preview rendering
- TowerVisualController implementation
- Direct tower visual hierarchy manipulation

---

# 2. Core Design Philosophy

Tower upgrades are intended to provide progression across three different dimensions.

Players should experience:

```text
Stat Growth
    ↓
Behaviour Evolution
    ↓
Elemental Strategy
```

This structure allows towers to first become stronger, then become more unique, and finally convert into elemental towers that interact with monster debuff stacks, overloads, and path-segment coverage.

Tower growth has two separate surfaces:

1. Tower Level
2. Tower Upgrades

Tower Level is a light growth layer used for small base stat increases, model or visual replacement, and unlocking higher upgrade categories.

Tower Upgrades are the primary source of build identity and power growth.

---

# 3. Tower Level Progression

Tower levels represent the basic advancement state of an individual tower instance.

Current design target:

```text
Max Tower Level = 3
```

Tower levels provide:

- Small base stat increases
- New tower visuals or models
- Access to higher upgrade categories

Tower levels are not intended to be the primary source of power growth.

Most power growth should come from Tower Upgrades.

## 3.1 Tower Level Config Data

Tower level base stat growth should be configured in TowerDefinition through per-level config data.

Suggested TowerDefinition-owned level data:

| Field | Type | Description |
|---|---|---|
| towerLevelConfigs | List<TowerLevelConfig> | Per-level stat and presentation data for this tower type |

Suggested TowerLevelConfig fields:

| Field | Type | Description |
|---|---|---|
| level | int | Tower level represented by this config entry |
| basicDamage | int | Basic damage value for this tower level |
| towerModelPrefab | GameObject | Optional visual/model replacement for this level |
| displayIcon | Sprite | Optional UI icon for this level |

Tower Level should own basic damage growth. Tower upgrade runtime state should own instance-specific damage bonuses and other upgrade modifiers.

The first-version damage direction is:

```text
FinalDamage = TowerLevelConfig.basicDamage + RuntimeDamageBonus
```

Exact stat fields may evolve with the concrete TowerCombatBehaviour component and TowerRuntimeCombatSystem implementation needs.

## 3.2 Tower Level-Up Request

Tower Draft level-up should be routed as a level-up request to TowerUpgradeSystem.

TowerPlacementSystem only detects placement or target intent.

When a Tower Draft item is dragged onto an existing tower:

```text
Tower Draft Item
    ↓ Preview CenterAnchor Snaps To GridNode
TowerPlacementSystem
    ↓ Detect Target Tower Intent From TowerInstance.OccupiedNodes
TowerUpgradeSystem
    ↓ Validate Tower Level-Up Request
TowerUpgradeSystem
    ↓ Apply Tower Level Data
TowerPlacementSystem
    ↓ Request Target TowerBehaviour Visual Refresh
```

A tower level-up request is valid only when:

- The Draft item's TowerFamily matches the target tower's TowerFamily.
- The target tower has not reached max tower level.
- The run and battle state still allow draft item consumption.

If the request is accepted:

- Consume the Tower Draft item.
- Increase the target tower level by 1.
- Apply the per-level base stat growth from TowerDefinition.
- Request the target tower runtime to replace or update the tower model/visuals for the new level if configured.
- Keep the permanent TowerBaseVisualRoot unchanged.
- Refresh the current active AttackOrigin after model replacement.
- Unlock access to higher upgrade categories.

If the request is rejected, the Tower Draft item should not be consumed.

TowerUpgradeSystem owns level-up validation and level data application only. It should not operate TowerBehaviour or TowerVisualController directly.

Tower model replacement is performed through the tower-owned visual/runtime path after TowerPlacementSystem receives an accepted level-up result and asks the target TowerBehaviour to refresh visuals.

If the replaced model was executing an attack presentation, the new `TowerModelPresentation` takes over the already confirmed pending attack by replaying its attack trigger. The combat runtime preserves confirmation topology and snapshots. If the new presentation cannot accept the trigger, runtime releases from the newly resolved current AttackOrigin immediately. Old-model Animation Events cannot create a duplicate release.

TowerUpgradeSystem should not directly manipulate VisualRoot, TowerPrefabSpawnPoint, renderer materials, or AttackOrigin hierarchy.

Tower Level-Up Preview is owned by the placement drag workflow. It currently means a Tower Draft item dragged onto an existing deployed tower with the same TowerFamily can display the Current Level + 1 ghost model before release. It does not include future Tower Upgrade Draft item effect previews.

---

# 4. Tower Upgrade Definitions And Runtime State

TowerUpgradeDefinition represents one independent tower upgrade option.

Each TowerUpgradeDefinition belongs to one TowerFamily and declares a Required Tower Level.

TowerUpgradeDefinition remains separate from the prefab-authored base combat data on the tower's concrete TowerCombatBehaviour component. TowerUpgradeDefinition represents upgrade content that may be applied to a tower instance during a battle.

TowerUpgradeDefinition may define:

- Upgrade identity and display text
- TowerFamily
- Required tower level
- Upgrade Layer
- Basic Layer stat deltas
- Behaviour Layer package identity and package-specific authoring data
- Elemental Layer element type and elemental apply effect
- Authoring validation metadata

Required Tower Level is a code-facing unlock requirement.

Upgrade Layer is a design-facing category that describes what kind of upgrade the definition represents.

Current content may still align Basic with Lv1, Behaviour with Lv2, and Elemental with Lv3, but the system contract should not permanently derive Upgrade Layer from Required Tower Level. This keeps future content flexible when a later upgrade uses a different unlock level than its design category.

TowerUpgradeDefinition should not contain Draft sampling, display choice count, reroll, or weighting rules. Those rules belong to DraftSystem.

## 4.1 Tower Upgrade Database

Tower Upgrade System owns the configured set of available TowerUpgradeDefinition assets.

The upgrade database is a content lookup source. It may support lookup and filtering by TowerFamily, required tower level, behaviour package, or other authoring metadata.

The upgrade database should not contain gameplay selection logic.

Runtime flow:

```text
Tower Upgrade Database
    ↓ Provides Upgrade Definitions
DraftSystem
    ↓ Selects Tower Upgrade Draft Candidates
Player Selects TowerUpgradeDefinition
    ↓
TowerPlacementSystem
    ↓ Detects Target Tower Intent
TowerUpgradeSystem
    ↓ Validates And Applies Selected Upgrade
Tower Runtime
    ↓ Resolves Stats And Behaviour Packages
```

## 4.2 Per-Tower Upgrade State

Tower upgrades are applied to individual tower instances.

Tower Upgrades are not global upgrades.

Each tower instance tracks its own applied upgrades.

Runtime upgrade state should answer:

- Which TowerUpgradeDefinition entries this tower already owns
- Which Required Tower Level categories are unlocked for this tower level
- Whether this tower already owns an Elemental Layer upgrade
- Which Basic Layer stat deltas affect this tower
- Which Behaviour Layer packages are active on this tower
- Which Behaviour Layer upgrade definition provides the active package parameters
- Which Elemental Layer profile is active on this tower when one has been applied

Applying a TowerUpgradeDefinition records that upgrade on the target tower.

V1 does not impose a quantity limit on upgrades within the same Required Tower Level category for Basic or Behaviour upgrades. A tower may receive multiple different Basic upgrades and multiple different Behaviour upgrades as long as it satisfies the category unlock and duplicate rules. However, one tower may own at most one Behaviour upgrade for each non-None `TowerBehaviourPackageType`; V1 does not define package replacement, priority, aggregation, or sequencing.

The first Elemental Layer rule is exclusive per tower: a tower may own at most one Elemental Layer upgrade unless a future reviewed rule explicitly allows replacement or multi-element towers.

TowerUpgradeState should store applied upgrade facts and expose query support, such as whether a tower already owns an Elemental upgrade. TowerUpgradeSystem should remain the authority that interprets those facts into application rules, including the first-version one-element-per-tower restriction.

Tower level unlocks upgrade categories:

| Tower Level | Unlocked Upgrade Categories |
|---|---|
| Lv1 | Basic |
| Lv2 | Basic, Behaviour |
| Lv3 | Basic, Behaviour, Elemental |

The system contract is that lower tower levels cannot receive upgrades whose Required Tower Level is higher than the tower's current level.

Each tower may gradually develop its own build identity.

Example:

```text
Archer A
- Extended Range
- Scatter Arrow
- Piercing Arrow

Archer B
- Damage Bonus
- Rapid Fire
```

## 4.3 Runtime Change Notification

Recording an upgrade or applying a tower level is an authoritative state change. After the state change succeeds, the tower runtime must be notified so it can:

- Resolve the tower's new current combat values.
- Refresh tower-owned targeting and cooldown state.
- Refresh eligible active Attack Entities according to the timing contract in this document and Tower Runtime Combat System.
- Create an immediate persistent runtime such as Arcane Field when the package contract requires it.
- Reconcile eligible existing release groups when Hunting Arrow or Multi Orbs requires live retrofit.
- Refresh Multi Drones scheduler capacity without directly launching a companion or resetting cooldown.

TowerUpgradeSystem owns validation, state recording, and the notification boundary. The corresponding combat runtime remains responsible for executing the refresh or package behavior. Recording an upgrade must not require destroying and recreating the tower or indiscriminately removing its active Attack Entities.

`TowerInstance.OnUpgradeRecorded` fires only after one upgrade is accepted and recorded. A separate `OnLevelChanged(previousLevel, currentLevel)` notification fires only after an accepted actual level transition; rejected and same-level requests emit nothing. The combat runtime caches the last resolved values, resolves the new state after mutation, replaces its cache before dispatch, and then applies ratios/deltas or exact package commands. This ordering makes multiple accepted changes in one frame compose from the immediately previous resolved state. Every handler rejects inactive sessions and notifications from any source other than its explicitly bound owner.

Subscription is the last initialization/recovery step, after common and subtype baselines exist and subtype recovery has completed. Technical disable or invalidation resets the scheduler and removes active/pending runtime, so upgrades recorded while inactive are represented by the new baseline on recovery rather than replayed as deltas against removed state.

Notification routing remains narrow:

- Level change refreshes level-derived common combat values.
- Basic Layer refreshes only the common or family-specific stat fields actually present in that definition. A multi-delta definition still produces one atomic family refresh transaction.
- Behaviour Layer dispatches only the matching package reconciliation.
- Elemental Layer updates authoritative tower state but sends no mutable entity-refresh command; later eligible attack boundaries already read the current Elemental profile.

---

# 5. Tower Upgrade Target Validation And Application

Tower Upgrade Drafts represent tower enhancement items.

TowerUpgradeSystem is the eligibility authority for Tower Upgrade Draft targets. Drag or placement systems may ask TowerUpgradeSystem whether a deployed tower can receive the selected TowerUpgradeDefinition, then use that result for valid-target feedback.

TowerUpgradeSystem should return eligibility or application results only. It should not directly play VFX, control tower highlight state, mutate renderer materials, or operate tower visual hierarchy.

After an upgrade application succeeds, the caller may request upgrade-applied visual feedback through the target tower's visual ownership path.

## 5.1 Upgrade Eligibility

An upgrade may be applied only when:

- The upgrade TowerFamily matches the target tower's TowerFamily.
- The target tower level satisfies Required Tower Level.
- The target tower does not already have the same upgrade.
- Elemental Layer upgrades are not applied to a tower that already owns an Elemental Layer upgrade.

Example:

```text
Upgrade: Archer Scatter Arrow
TowerFamily: Archer
Required Level: 2

Valid Targets:
- Archer Lv2
- Archer Lv3

Invalid Targets:
- Archer Lv1
- Cannon Towers
- Magic Towers
- Drone Towers
```

## 5.2 Duplicate Rules

A tower cannot receive the same Upgrade twice.

A tower also cannot receive a second Behaviour upgrade whose non-None `TowerBehaviourPackageType` is already active on that tower, even when it is represented by a different Upgrade asset. Multiple assets may author the same package type globally, but they cannot coexist on one tower in V1.

The duplicate restriction is per tower, not global.

Example:

```text
Archer A already owns Multi Shot.
Archer A cannot receive Multi Shot again.
Archer B may still receive Multi Shot.
```

## 5.3 Upgrade Composition

TowerUpgradeDefinition entries are independent by default.

If a tower owns both Piercing Arrow and Scatter Arrow, the intended result is that the scattered arrows can also pierce.

Behaviour upgrades are composable by default in v1.

Reviewed composition results include:

- Piercing Arrow + Scatter Arrow: every scattered Arrow may pierce.
- Piercing Arrow + Hunting Arrow: the Arrow tracks only its locked initial target; after that hit or a tracking-validity failure, a surviving Piercing Arrow continues ordinary Direction hits until its piercing count is exhausted.
- Scatter Arrow + Hunting Arrow: Tower runtime assigns distinct initial targets without replacement to fixed Center, Left, and Right slots at confirmation. Unassigned or invalid secondary slots preserve their corresponding confirmation-time Scatter directions. Tracking never reacquires.
- Multi Shells + Explosive Shell: every released initial Shell may execute its own explosion.
- Multi Shells + Bouncing Shell: every released initial Shell owns an independent bounce chain.
- Explosive Shell + Bouncing Shell: each valid landing completes its explosion before selecting the next bounce target in the same frame.
- Multi Orbs + Arcane Detonation: the synchronized group completes once, and every active member independently detonates at its own current position before the group disappears.
- Multi Drones + Blast Rounds: every active Drone fires Blast Rounds.
- Multi Drones + Final Dive: every active Drone independently resolves its own battery-end Final Dive.

The first version does not define upgrade-exclusion rules where applying one upgrade prevents another different upgrade from being applied later.

If a future design needs upgrade exclusion, that rule should be added as an explicit reviewed contract instead of being assumed by the current upgrade model.

## 5.4 Authoring Validation

Authoring validation exists to prevent invalid content configuration.

For example:

- Hunting Arrow should not be configured for Cannon.
- Multi Drones should not be configured for Archer.
- Magic Orb-specific stat deltas should not be configured for Cannon.

These cases are content errors, not player-facing gameplay rules.

The editor or validation path should warn designers about invalid combinations. Runtime should fail safely and log clear warnings if invalid content is encountered.

Behaviour Layer package identity should use typed package identifiers rather than free-form strings. The package identity represents which runtime Behaviour package an upgrade grants. It is separate from Basic Layer stat delta types.

Behaviour Layer package parameters belong to the corresponding TowerUpgradeDefinition asset. Runtime systems consume those parameters through the applied upgrade definition on the placed tower instance.

Examples of Behaviour Layer package parameters:

- Archer Piercing Arrow finite piercing hit count
- Archer Scatter Arrow angle offset
- Cannon Explosive Shell area Effect reference
- Cannon Bouncing Shell search radius, maximum bounce count, bounce-child arc height, and local target-selection type
- Magic Multi Orbs count; runtime derives an even `360 / count` starting-angle step
- Magic Arcane Detonation area Effect reference
- Magic Arcane Field radius, tick interval, tick Effect reference, and VFX prefab whose root contains MagicArcaneFieldBehaviour
- Drone Multi Drones absolute override maximum active count
- Drone Blast Rounds area Effect reference
- Drone Final Dive positive `finalDiveHitThreshold` and impact Effect reference

TowerUpgradeSystem should validate and record upgrade ownership only. It should not execute Behaviour Layer gameplay or interpret package parameters beyond content validation.

Behaviour Layer uses reviewed typed packages. Each package owns its required authoring values and Effect references on its TowerUpgradeDefinition. The corresponding runtime implementation owns fixed trigger timing, target rules, execution order, and Elemental opportunities. Designers do not independently bind arbitrary trigger types to Behaviour upgrades.

Elemental Layer identity should use a typed ElementType rather than a free-form string. Each Elemental Layer upgrade also declares one Elemental apply Effect. Runtime systems ask the tower upgrade state whether a tower owns an Elemental upgrade, then provide that apply Effect at the real attack boundary for Effect System execution.

Elemental Layer content also does not expose a designer-selected trigger type in v1. Its runtime producer decides whether the real event is a hit, contact, impact, or area resolution and supplies the direct Elemental apply Effect at that boundary. Basic Layer remains a pure numerical layer and exposes no gameplay Effect authoring.

---

# 6. Upgrade Eligibility Support

TowerUpgradeSystem provides upgrade definitions and eligibility rules used by DraftSystem when DraftSystem builds Tower Upgrade Draft pools.

TowerUpgradeSystem owns:

- TowerFamily matching rules
- Required Tower Level checks
- Per-tower duplicate upgrade checks
- Upgrade definition lookup
- Upgrade application validation

TowerUpgradeSystem does not own:

- Draft pool generation timing
- Tower-instance weighting
- Pending Tower Upgrade Draft reservation during Draft pool generation
- Draft choice count
- Same-round duplicate prevention for displayed Draft options
- Reroll, rarity, or future Draft presentation rules

Those Draft option generation rules belong to DraftSystem.

When requested by DraftSystem, TowerUpgradeSystem may expose helper queries such as:

```text
GetEligibleUpgradesForTower(towerInstance)
CanApplyUpgrade(towerInstance, upgradeDefinition)
```

These helpers should answer eligibility questions only. They should not decide how DraftSystem samples, weights, or displays the final Draft choices.

---

# 7. Tower Upgrade Layers

Tower upgrades are divided into three conceptual layers.

These layers are design categories. Required Tower Level controls when an upgrade is unlocked, while Upgrade Layer controls what kind of upgrade it is.

The first content set may map Lv1 to Basic, Lv2 to Behaviour, and Lv3 to Elemental, but system logic should keep those concepts separate so future content can evolve without rewriting the upgrade model.

---

## 7.1 Basic Layer

Basic Layer upgrades represent numerical improvements.

Each tower family owns its own Basic Layer upgrade definitions. Many tower families may still share common concepts such as range, attack interval, and damage bonus.

Common Basic Layer stat deltas:

- Attack range delta
- Attack interval delta
- Damage bonus delta

Tower-family-specific Basic Layer stat deltas may include examples such as:

- Magic Orb rotation speed delta
- Magic Orb max hit count delta
- Drone battery duration delta
- Drone burst cooldown delta

Basic Layer stat deltas use same-type addition:

```text
FinalAttackRange = BaseAttackRange + Sum(AttackRangeDeltas)
FinalAttackInterval = Clamp(BaseAttackInterval + Sum(AttackIntervalDeltas))
FinalDamage = TowerLevelConfig.basicDamage + Sum(DamageBonusDeltas)
FinalMagicOrbMaxHitCount = Clamp(BaseMagicOrbMaxHitCount + Sum(MagicOrbMaxHitCountDeltas))
```

AttackInterval improvements may use negative deltas.

Runtime stat resolution should clamp final values so invalid or extreme content cannot break combat behavior.

Successful Basic Layer and tower-level changes use the following first-version runtime propagation rules:

- Attack Range refreshes immediately for tower target acquisition and for active Hunting Arrow and Drone range checks. Existing TrackingRangeOrigin values, Drone release-time range origins, and captured Cannon target positions remain unchanged.
- Attack Interval refreshes immediately. For a positive remaining cooldown and positive old resolved interval, multiply remaining time by `newInterval / oldInterval`. A ready cooldown remains zero; a non-positive old interval resolves remaining time to zero. A new zero interval completes cooldown, but the next normal scheduler Update remains responsible for any release.
- Damage refreshes immediately for unresolved hits from active Arrows, Shells, Magic Orbs, Drones, and their eligible attack extensions. Damage that has already resolved is never replayed.
- Magic Orb rotation speed refreshes immediately for active Orbs.
- Magic Orb max hit count adds the resolved delta once to every active member's independent remaining hit count without clearing contact history or resetting consumed hits.
- Drone battery duration refreshes `Launching` and `Orbiting` Drones by delta: add the resolved duration change to remaining battery without resetting the Drone to full battery. `FinalDiving` has already resolved the battery-end branch and ignores the change.
- Drone burst cooldown refreshes future cadence. Only an active inter-burst cooldown after the last shot preserves its completion ratio; timers between shots, burst interval, and remaining shots in the current burst do not change. Zero-value handling follows Attack Interval and firing resumes only through the next normal Drone Update.

These rules distinguish a live resolved value from immutable entity history. Hit history, consumed hits, elapsed lifetime, captured positions, and already completed Effects are not reconstructed when a numerical upgrade changes.

Purpose:

- Improve tower efficiency
- Provide reliable power growth
- Create a stable progression foundation

Basic Layer upgrades should not define gameplay Effect execution in the first version. If an upgrade needs trigger-driven gameplay, it belongs in a reviewed Behaviour package or the Elemental Layer.

---

## 7.2 Behaviour Layer

Behaviour Layer upgrades modify how a tower attacks.

These upgrades are intended to reinforce the identity of a specific tower type.

Each Behaviour Layer TowerUpgradeDefinition grants one behaviour package.

TowerUpgradeSystem applies the upgrade and records that the tower owns the behaviour package. Tower Runtime Combat and the corresponding runtime modules execute the behavior.

TowerUpgradeSystem should not become a behaviour manager.

The final first-version Behaviour content set is:

| Tower | Behaviour Upgrades | Identity |
|---|---|---|
| Archer Tower | Piercing Arrow, Scatter Arrow, Hunting Arrow | Penetration, projectile count, tracking |
| Cannon Tower | Explosive Shell, Multi Shells, Bouncing Shell | Area impact, multi-target release, local chaining |
| Magic Tower | Multi Orbs, Arcane Detonation, Arcane Field | Synchronized group membership, normal-completion explosion, persistent tower field |
| Drone Tower | Multi Drones, Blast Rounds, Final Dive | Maximum active capacity, projectile explosion, Drone lifecycle attack |

Runtime composition is resolved from the source tower's complete applied Behaviour package set. Each Attack Entity receives typed runtime data relevant to its own execution. Fields marked Live Refresh may be updated through the owning tower runtime; Release Snapshot values and immutable entity history remain local to that entity. A Projectile, Magic Orb, or Drone does not need to own or interpret the complete TowerUpgradeState or unrelated Behaviour definitions.

Behaviour timing is package-specific:

| Package | Active-entity behavior when the upgrade is applied |
|---|---|
| Piercing Arrow | Refresh existing Arrows immediately; add the package hit-count delta without clearing hit history |
| Scatter Arrow | Release Snapshot; do not create missing side Arrows for an existing release |
| Hunting Arrow | Live retrofit eligible existing Arrow release groups through virtual confirmation |
| Explosive Shell | Refresh airborne Shells before Position Impact |
| Multi Shells | Release Snapshot; do not create additional Shells for an existing release |
| Bouncing Shell | Refresh only an initial Shell that has not reached its first Position Impact; an active bounce chain remains unchanged |
| Multi Orbs | Add missing synchronized mirror members to the one active Orb group; they join its existing shared lifecycle |
| Arcane Detonation | An active incomplete Orb group gains Detonation eligibility; no immediate explosion, and every member detonates only on the group's later normal completion |
| Arcane Field | Create the tower-owned field immediately |
| Multi Drones | Replace the scheduler's maximum active Drone capacity; do not directly launch a companion, reset cooldown, or batch-fill capacity |
| Blast Rounds | Refresh active Drones and already airborne unresolved Drone projectiles |
| Final Dive | Refresh active Drones before their battery-end resolution |

Live retrofit uses an owner-local stable `ReleaseGroupId` plus package-specific slot or membership identity for Archer and Magic groups so repeated refreshes are idempotent. Archer groups preserve Center/Left/Right slots, use Center for a single Arrow, and consume Hunting reconciliation exactly once; a Tracking Arrow that later falls back to Direction never reacquires. Multi Orbs adds only missing mirror members and preserves the group's elapsed lifetime, original members' remaining hit counts, member histories, and already-resolved results. A new mirror starts with the current resolved per-member maximum but cannot postpone an original member's exhaustion. Missing Orb candidates are created and validated before group membership is committed; a failed candidate attempt leaves the original group unchanged and no partial mirrors. Multi Drones uses registered active count rather than companion release-group identity.

Scatter Arrow and Multi Shells topology is fixed when the attack enters `WaitingForAnimationRelease`. An upgrade during that wait never adds side Arrows, changes the number of initial Shells, or recaptures Cannon target positions. Pending attacks are not mutated by entity refresh APIs. At the Animation Event, Archer initializes from current Damage, Piercing, and Hunting values; Cannon initializes from current Damage, Explosive, and pre-impact Bouncing values. Both preserve confirmation topology and snapshots.

Behaviour Layer package identity should be typed. Behaviour parameters live on their corresponding TowerUpgradeDefinition and are consumed by the runtime module that owns the behavior. Reusable target resolution and Effect execution should remain in Effect System, while persistent Buff state remains in Buff System.

### Archer Behaviour Upgrades

Piercing Arrow grants finite per-projectile hit count and hit-history behavior. Every newly resolved Monster Hit consumes one hit, may dispatch direct damage, and provides one explicit Elemental application opportunity. Reaching the maximum hit count ends the Arrow. Applying Piercing Arrow refreshes active Arrows by adding the package hit-count delta to their remaining count; it does not clear previous hits or reset remaining count from the new maximum.

Scatter Arrow releases multiple independent Arrow projectiles from one attack. Each Arrow owns its own movement, hit detection, piercing state, hit history, lifetime, damage result, and Elemental opportunities. Buff apply cooldown and Protection decide whether simultaneous attempts against the same Monster produce more than one successful application. Scatter Arrow is a Release Snapshot package and never supplements an already released Arrow group with side Arrows.

Hunting Arrow changes one assigned Arrow flight into locked-target tracking. Tower runtime captures the authoritative main target position and fixed Center, Left, and Right target slots at confirmation without target replacement or reuse. TrackingRangeOrigin is immutable per release, while the resolved TrackingRange refreshes when Attack Range changes. An assigned Arrow tracks only its locked target while both remain inside that range. Hitting the target, target invalidation, loss of MonsterManager registration, or either range failure permanently transitions the Arrow to ordinary Direction flight with no reacquisition. When combined with Scatter Arrow, unassigned or invalid secondary slots preserve their confirmation-time Scatter directions. Each Arrow owns independent hit history, remaining piercing count, lifetime, and Elemental opportunities; surviving Piercing remains active after Hunting ends. Tracking movement itself does not periodically apply Elemental Buffs; actual Monster Hits use the Arrow attack boundary.

When Hunting Arrow is applied while Arrows are active, each eligible release group performs one virtual confirmation using its stable Center, Left, and Right slots and a fresh target collection. Existing Arrows receive distinct valid targets without reuse; a slot with no valid target remains Direction flight. The group permanently consumes this reconciliation attempt. The retrofit does not restart movement, lifetime, hit history, explicit remaining Piercing capacity, or already-resolved hits, and a later one-way tracking-to-direction fallback never becomes eligible for reacquisition.

### Cannon Behaviour Upgrades

The baseline Cannon Shell captures a target position, produces Position Impact on arrival, and searches for at most one nearby direct target within ProjectileConfig.hitDistanceThreshold.

Explosive Shell adds an area Effect at Position Impact. It does not replace the baseline direct Monster Hit. A direct target may therefore receive direct damage plus explosion damage and two independent Elemental application attempts. The explosion executes even when no direct Monster Hit is resolved.

Multi Shells modifies initial release count:

```text
Multiple valid Monsters
    -> capture different target-position snapshots up to the authored maximum
    -> release one initial Shell per captured snapshot

Exactly one valid Monster
    -> capture one target-position snapshot
    -> release one initial Shell
```

The package owns `multiShellsMaxInitialShellCount`, with a minimum and default of `2`. The attack uses one confirmation, one presentation sequence, one cooldown, and captured target positions. Confirmed target positions are not retargeted or canceled during the animation wait. Multi Shells is Release Snapshot and never creates additional Shells for an existing release group. Each released Shell owns independent direct, explosion, bounce, lifetime, and Elemental results. Bounce children never consume Multi Shells again.

Bouncing Shell adds a finite local Position Impact chain. After every landing it completes all immediate results in the same frame: any direct damage and direct Elemental attempt, any Explosive Shell actions, explosion-target Elemental attempts, and synchronous Buff, overload, death, or target-state consequences. Only then does it search within the authored `bounceSearchRadius` around the impact position. It excludes direct Monsters already resolved by the chain, applies the package-owned `bounceTargetSelectionType` to the surviving local candidates, captures the selected target's current position, and creates one bounce child in the same frame. The selector supports Nearest, HighestHealth, LowestHealth, and Random without widening eligibility beyond the local radius. The package-owned `maxBounceCount` limits the chain, and `bounceArcHeight` controls bounce-child flight while the initial Shell continues to use the ArcProjectileCombatBehaviour's authored initial arc height. It does not use the source tower's full AttackRange or the component's tower target-selection type. Direct Monster Hit and positive direct damage are not required; no remaining bounce count or no candidate ends the chain.

Applying Bouncing Shell may refresh an initial airborne Shell only before its first Position Impact. The first Position Impact fixes the chain contract: remaining bounce count, resolved-target history, bounce arc height, search radius, and local selector remain unchanged for that chain. Damage and Explosive Shell eligibility may still refresh for unresolved impacts according to their own contracts.

### Magic Behaviour Upgrades

Magic Tower owns at most one active Magic Orb release group. A successful release starts cooldown, but a later release requires both cooldown readiness and completion of the active group. If cooldown becomes ready first it waits at zero; if the group completes first the tower waits for the remaining cooldown. Only a successful new group release restarts cooldown.

Multi Orbs owns the desired synchronized group-member count, with a minimum and default of `2`. Runtime chooses one shared orbit phase and distributes members evenly using fixed `360 / count` offsets. The group owns one orbit center, elapsed lifetime, resolved group values, and completion reason. Each member keeps its own remaining hit count, offset, world position, per-target contact cooldown history, contact damage result, and Elemental opportunity. Every member starts with the same resolved maximum; a successful contact consumes only that member's remaining count, and exhaustion of any member completes the group.

When Multi Orbs is applied to an active single-Orb group, runtime adds `desiredCount - currentCount` missing mirror members around the group's current phase. Added members join the existing shared lifetime and start with the current resolved per-member maximum. They do not reset the original member's remaining count or replay earlier results; therefore they cannot delay completion caused by the original member. Membership reconciliation is idempotent.

Arcane Detonation gives an active incomplete group future normal-completion Detonation eligibility; applying it never explodes the group immediately. Exhaustion of any member's hit count or shared lifetime expiry completes the group normally. Every active member then executes one area Effect at its own current world position before the complete group disappears. Every valid Monster resolved by each Detonation receives one explicit Elemental application opportunity. Initialization failure, combat reinitialization, component/GameObject disable, scene teardown, and owner-reference invalidation force-clean the complete group without Detonation.

Arcane Field creates one tower-owned field immediately when the upgrade is applied. Its TowerUpgradeDefinition owns field radius, tick interval, tick Effect reference, and the Magic Arcane Field VFX prefab whose root contains MagicArcaneFieldBehaviour. The instantiated prefab becomes the concrete field runtime instance. It is parented to and follows the tower, and its local X/Z scale is set to the applied field radius while its authored local Y scale is preserved. It has no independent first-version duration, does not duplicate when other upgrades are applied, and ends during combat reinitialization, component/GameObject disable, owner destruction, or scene teardown. Each tick resolves every valid Monster inside the field and provides one 100% Elemental application attempt per target. V1 has no per-target Elemental chance parameter.

### Drone Behaviour Upgrades

DroneCombatBehaviour owns `defaultMaximumDroneCount`, with a minimum and default of `1`. Its scheduler may launch one Drone only when cooldown is ready, a valid target exists, and registered active Drone count is below the currently resolved maximum. Every successful launch restarts cooldown. A ready tower at capacity waits at zero; when capacity becomes available it returns to the normal one-at-a-time target/release path.

Multi Drones is a Behaviour Layer package that owns `overrideMaximumDroneCount`, an absolute value with a minimum of `2`. While absent, runtime uses the combat component's default. Applying Multi Drones changes capacity immediately but does not launch a companion directly, reset or bypass cooldown, skip target validation, or fill every open slot as a batch. If cooldown is already ready, the normal scheduler may launch at most one Drone. Each active Drone owns independent movement, target, orbit, burst timing, battery, projectile attacks, and optional Final Dive lifecycle.

Blast Rounds adds an area Effect after a Drone projectile's primary direct hit. It is additive rather than replacing direct damage. The primary target may receive direct damage plus explosion damage and two independent Elemental application attempts. Every other valid explosion target receives its own explosion opportunity.

Final Dive adds a battery-end Drone state. Launching does not consume battery. When battery naturally depletes while Orbiting, an invalid current target produces VFX-only aerial despawn. A valid target is locked and the Drone enters FinalDiving, stops firing, pursues the target's current hit position without returning to Orbiting or selecting another Monster, and refreshes a last-valid-position snapshot. If the target becomes invalid during the dive, the Drone continues toward that last valid position. Entering the package-owned positive `finalDiveHitThreshold` around the current destination produces Position Impact.

At Position Impact, Final Dive searches for the nearest valid Monster within `finalDiveHitThreshold` around the actual impact position. A resolved Monster receives the Drone's current refreshed direct damage and one direct Elemental application opportunity. No resolved Monster means no direct damage or direct opportunity. After that optional direct result, the authored Final Dive explosion always executes. Every valid explosion target resolves its own damage and Elemental opportunity; the direct target may therefore receive both results. The Drone despawns after all synchronous impact results complete.

### Elemental Opportunity Audit

Behaviour Layer creates explicit Elemental application opportunities; it does not guarantee successful stacks. Damage amount and DealDamage success do not globally gate an otherwise eligible attempt.

| Upgrade | Elemental Application Opportunity |
|---|---|
| Piercing Arrow | Once for each new Monster Hit resolved by the Arrow |
| Scatter Arrow | Independently for every released Arrow's resolved Monster Hits |
| Hunting Arrow | No periodic application from tracking; actual Monster Hits follow Arrow rules |
| Explosive Shell | Direct target and every explosion target resolve independent attempts; the center may receive both |
| Multi Shells | Every released initial Shell resolves independently |
| Bouncing Shell | Every bounce child resolves its own direct and inherited explosion opportunities |
| Multi Orbs | Every Orb contact resolves independently |
| Arcane Detonation | Once for every valid Monster resolved by a normal-completion Detonation |
| Arcane Field | Once per valid Monster per field tick at 100% eligibility in V1 |
| Multi Drones | Every active Drone's projectile hits resolve independently |
| Blast Rounds | Primary direct target and every explosion target resolve independent attempts |
| Final Dive | The optional nearest direct target and every explosion target resolve independent attempts; the direct target may receive both |

BuffApplyCooldown, Protection, and Buff runtime decide whether each attempt applies, refreshes, stacks, or is blocked. Ordinary child Effects, Buff lifecycle Effects, periodic Elemental damage, reactions, zones, and overload results do not inherit eligibility unless a future reviewed Behaviour explicitly grants it.

TowerUpgradeSystem remains responsible for upgrade ownership, validation, and application only. It does not execute piercing, scatter release, target tracking, Shell impacts, bounce chains, Magic Orb lifecycle, Arcane Field ticks, Drone attacks, or Final Dive.

Damage upgrade examples should modify runtime damage bonuses rather than overwrite TowerLevelConfig.basicDamage. TowerLevelConfig.basicDamage remains the tower's level-based base stat.

Behaviour packages should not duplicate shared area-query, delayed-damage, repeated-damage, Buff, or Effect execution logic inside individual tower runtimes when that logic belongs to Effect System or Buff System. Tower runtimes may request or trigger those Effects, but reusable Effect resolution remains in Effect System and persistent Buff rules remain in Buff System.

Purpose:

- Differentiate tower types
- Create build diversity
- Change attack patterns rather than only increasing numbers

---

## 7.3 Elemental Layer

Elemental Layer upgrades convert a tower into an elemental tower for one element.

Examples:

- Burning
- Cold
- ElectricShock
- Windcut

Elemental Layer upgrades are intended to make path segments smarter and more dangerous through same-element tower coverage.

Each tower may receive one Elemental Layer upgrade in the first version.

Elemental Layer is Live Refresh at the real attack boundary. If an Elemental upgrade is applied while an Arrow, Shell, Magic Orb, Drone, or Drone-fired projectile is already active, its later eligible unresolved hit, contact, Position Impact, area resolution, or lifecycle result uses the tower's current Elemental profile. Earlier resolved events are not replayed, and immutable movement, target, history, and timer state is not changed.

Tower-owned primary attacks and reviewed Behaviour attack extensions may apply Elemental debuff stacks through Effect System and Buff System when their runtime context explicitly allows Elemental application. Multiple towers whose active Elemental upgrades share the same ElementType stack the same Elemental debuff on the same monster and can eventually trigger overload.

Elemental opportunity is independent from Damage amount and DealDamage success. A valid resolved attack target may receive an application attempt even when the associated damage value is zero or damage execution is unsuccessful. The opportunity still needs an explicitly authorized attack boundary and a valid target; technical type alone does not grant eligibility.

Reaction-generated damage, buff tick damage, EffectZone tick damage, and overload damage should not apply elemental stacks by default. Elemental stacking should remain tied to explicitly eligible tower-owned attack events unless a future reviewed upgrade explicitly expands that rule.

Examples:

```text
Cold tower hits
    ↓
Apply or refresh Cold stack
    ↓
Multiple Cold towers reach max stacks
    ↓
Frozen overload
```

```text
Fire tower hits
    ↓
Apply or refresh Burning stack
    ↓
Multiple Fire towers reach max stacks
    ↓
FlameBurst overload
```

Elemental Layer content is split between tower-specific upgrade authoring and shared Elemental Buff data:

- Each Elemental TowerUpgradeDefinition declares its TowerFamily, ElementType, and one Elemental apply effect.
- Tower runtime and the owning Attack Entity decide when that effect is executed and which valid targets receive it according to the reviewed primary-attack or Behaviour contract.
- One shared BuffDefinition owns the persistent Elemental Buff data for each ElementType, including periodic, stack, and overload Effect references, Buff apply cooldown, Protection duration, and first-version Buff visual references.
- After a Buff is applied, its tick damage, overload, status presentation, and persistent Buff VFX no longer vary by the tower that applied it.

The first complete Elemental Layer content pass contains four ElementTypes for each of the four TowerFamilies: 16 Elemental TowerUpgradeDefinition assets. The four tower-family assets for one element reuse that element's shared Buff data wherever their actual attack timing and target scope allow it.

Buff apply cooldown prevents the same elemental debuff from stacking too quickly on the same monster, regardless of which tower attempts the application. When this cooldown blocks an application, the first-version rule is that no stack is added, duration is not refreshed, and stack effects such as Electric extra damage or the Windcut secondary attack do not trigger.

First application of an elemental debuff should apply the debuff only. If the monster already has that elemental debuff and an eligible tower-owned attack event successfully adds one stack, the StackApplied Buff event binding may execute. A pure refresh should not trigger stack effects. After a successful stack increase, the system checks whether max stacks have been reached; if yes, overload executes and the Buff enters Protection phase when configured.

Archer and Drone projectile hits, Magic Orb contact, Cannon direct arrival, Behaviour explosions, Arcane Field ticks, and Final Dive each provide different reviewed attack timing. Their explicit opportunity boundaries are defined in the Behaviour audit above. Position Impact Effects may resolve area targets without a direct Monster Hit, while Monster-targeted results require valid resolved Monsters.

Purpose:

- Encourage tower combinations
- Encourage same-element path-segment coverage
- Support elemental debuff stacking and overload rhythm
- Keep recursive elemental reactions controlled

---

# 8. Related Systems

## Draft System

Draft System owns draft generation workflow and displayed choice count.

Tower Upgrade System provides upgrade eligibility and application rules.

## Tower Placement System

Tower Placement System detects whether a dragged Draft item targets a deployment tile or an existing tower.

Tower Placement System should forward tower level-up or upgrade target intent to TowerUpgradeSystem.

Tower Placement System should not decide tower level-up rules or apply tower upgrades.

## Tower Framework System

Tower Framework System owns TowerDefinition, its per-level config data, and the prefab-authored concrete TowerCombatBehaviour contract. TowerUpgradeSystem owns upgrade definitions, including the Arcane Field package identity, parameters, and package-specific VFX prefab reference.

## Battle HUD UI System

Battle HUD UI System displays Draft items and owns the drag interaction entry points.

Battle HUD UI System should not own upgrade validation rules or tower-local highlight presentation.

---

# 9. Future Expansion

Future versions may expand this system with:

- Upgrade prerequisites
- Upgrade rarity
- Upgrade evolution chains
- Tower level unlock requirements
- Global upgrades
- Tower specialization paths
- Advanced buff reactions
