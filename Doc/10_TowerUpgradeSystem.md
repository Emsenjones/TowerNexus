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

Exact stat fields may evolve with AttackConfig and TowerRuntimeCombatSystem implementation needs.

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

TowerUpgradeSystem should not directly manipulate VisualRoot, TowerPrefabSpawnPoint, renderer materials, or AttackOrigin hierarchy.

Tower Level-Up Preview is owned by the placement drag workflow. It currently means a Tower Draft item dragged onto an existing deployed tower with the same TowerFamily can display the Current Level + 1 ghost model before release. It does not include future Tower Upgrade Draft item effect previews.

---

# 4. Tower Upgrade Definitions And Runtime State

TowerUpgradeDefinition represents one independent tower upgrade option.

Each TowerUpgradeDefinition belongs to one TowerFamily and declares a Required Tower Level.

TowerUpgradeDefinition should not be attached to AttackConfig. AttackConfig remains the immutable default combat configuration template. TowerUpgradeDefinition represents upgrade content that may be applied to a tower instance during a battle.

TowerUpgradeDefinition may define:

- Upgrade identity and display text
- TowerFamily
- Required tower level
- Upgrade Layer
- Basic Layer stat deltas
- Behaviour Layer package
- Elemental Layer element type and elemental apply effect
- Generic Effect bindings for Behaviour gameplay
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

V1 does not impose a quantity limit on upgrades within the same Required Tower Level category for Basic or Behaviour upgrades. A tower may receive multiple different Basic upgrades and multiple different Behaviour upgrades as long as it satisfies the category unlock and duplicate rules.

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
- Piercing Arrow + Hunting Arrow: a surviving Arrow reacquires after each hit until its piercing count is exhausted.
- Scatter Arrow + Hunting Arrow: every scattered Arrow resolves its own initial target and then tracks independently. Initial selection prefers different valid Monsters when alternatives exist and permits target reuse when distinct candidates are insufficient.
- Twin Shells + Explosive Shell: every released initial Shell may execute its own explosion.
- Twin Shells + Bouncing Shell: every released initial Shell owns an independent bounce chain.
- Explosive Shell + Bouncing Shell: each valid landing completes its explosion before selecting the next bounce target in the same frame.
- Twin Orbs + Arcane Detonation: every Orb independently detonates on normal completion.
- Twin Drones + Blast Rounds: every Drone fires Blast Rounds.
- Twin Drones + Final Dive: every Drone independently resolves its own battery-end Final Dive.

The first version does not define upgrade-exclusion rules where applying one upgrade prevents another different upgrade from being applied later.

If a future design needs upgrade exclusion, that rule should be added as an explicit reviewed contract instead of being assumed by the current upgrade model.

## 5.4 Authoring Validation

Authoring validation exists to prevent invalid content configuration.

For example:

- Hunting Arrow should not be configured for Cannon.
- Twin Drones should not be configured for Archer.
- Magic Orb-specific stat deltas should not be configured for Cannon.

These cases are content errors, not player-facing gameplay rules.

The editor or validation path should warn designers about invalid combinations. Runtime should fail safely and log clear warnings if invalid content is encountered.

Behaviour Layer package identity should use typed package identifiers rather than free-form strings. The package identity represents which runtime Behaviour package an upgrade grants. It is separate from Basic Layer stat delta types.

Behaviour Layer package parameters belong to the corresponding TowerUpgradeDefinition asset. Runtime systems consume those parameters through the applied upgrade definition on the placed tower instance.

Examples of Behaviour Layer package parameters:

- Archer Piercing Arrow finite piercing hit count
- Archer Scatter Arrow angle offset
- Cannon Explosive Shell area Effect reference
- Cannon Bouncing Shell search radius and maximum bounce count
- Magic Twin Orbs orb count and starting angle offset
- Magic Arcane Detonation area Effect reference
- Magic Arcane Field radius, tick interval, and tick Effect reference
- Drone Twin Drones drone count and takeoff delay
- Drone Blast Rounds area Effect reference
- Drone Final Dive positive `finalDiveHitThreshold` and impact Effect reference

TowerUpgradeSystem should validate and record upgrade ownership only. It should not execute Behaviour Layer gameplay or interpret package parameters beyond content validation.

Elemental Layer identity should use a typed ElementType rather than a free-form string. Each Elemental Layer upgrade also declares one Elemental apply Effect. Runtime systems ask the tower upgrade state whether a tower owns an Elemental upgrade, then provide that apply Effect at the real attack boundary for Effect System execution.

Effect bindings remain available for generic trigger-driven Behaviour content. They describe which gameplay trigger may execute which Effect definition. Elemental Layer content does not expose a designer-selected TriggerType in v1: its runtime producer decides whether the real event is a hit, contact, impact, or area resolution. Basic Layer upgrades should not define Effect bindings in the first version because Basic Layer remains a pure numerical layer.

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

Purpose:

- Improve tower efficiency
- Provide reliable power growth
- Create a stable progression foundation

Basic Layer upgrades should not define gameplay trigger bindings such as OnHit or OnImpact in the first version. If an upgrade needs trigger-driven gameplay, it belongs in Behaviour Layer or Elemental Layer.

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
| Cannon Tower | Explosive Shell, Twin Shells, Bouncing Shell | Area impact, multi-target release, local chaining |
| Magic Tower | Twin Orbs, Arcane Detonation, Arcane Field | Entity count, normal-completion explosion, persistent tower field |
| Drone Tower | Twin Drones, Blast Rounds, Final Dive | Entity count, projectile explosion, Drone lifecycle attack |

Runtime composition is resolved from the source tower's complete applied Behaviour package set. Each released Attack Entity receives only the immutable resolved options relevant to its own execution. A Projectile, Magic Orb, or Drone does not need to own or interpret the complete TowerUpgradeState or unrelated Behaviour definitions.

Behaviour Layer package identity should be typed. Behaviour parameters live on their corresponding TowerUpgradeDefinition and are consumed by the runtime module that owns the behavior. Reusable target resolution and Effect execution should remain in Effect System, while persistent Buff state remains in Buff System.

### Archer Behaviour Upgrades

Piercing Arrow grants finite per-projectile hit count and hit-history behavior. Every newly resolved Monster Hit consumes one hit, may dispatch direct damage, and provides one explicit Elemental application opportunity. Reaching the maximum hit count ends the Arrow.

Scatter Arrow releases multiple independent Arrow projectiles from one attack. Each Arrow owns its own movement, hit detection, piercing state, hit history, lifetime, damage result, and Elemental opportunities. Buff apply cooldown and Protection decide whether simultaneous attempts against the same Monster produce more than one successful application.

Hunting Arrow changes Arrow flight into tracking behavior. It tracks one target inside the source tower's resolved AttackRange, reacquires when that target dies, becomes invalid, leaves range, or is hit by a surviving Piercing Arrow, excludes the current Arrow's hit history, and selects the nearest candidate relative to the Arrow. With no candidate it continues along its current direction and may reacquire later until lifetime expires. When combined with Scatter Arrow, every released Arrow resolves its own initial target; selection prefers different valid Monsters when alternatives exist and permits reuse when there are fewer valid Monsters than Arrows. Each Arrow then owns independent tracking, hit history, remaining piercing count, lifetime, and Elemental opportunities. Tracking movement itself does not periodically apply Elemental Buffs; actual Monster Hits use the Arrow attack boundary.

### Cannon Behaviour Upgrades

The baseline Cannon Shell captures a target position, produces Position Impact on arrival, and searches for at most one nearby direct target within ProjectileConfig.hitDistanceThreshold.

Explosive Shell adds an area Effect at Position Impact. It does not replace the baseline direct Monster Hit. A direct target may therefore receive direct damage plus explosion damage and two independent Elemental application attempts. The explosion executes even when no direct Monster Hit is resolved.

Twin Shells modifies initial release count:

```text
Two or more valid Monsters
    -> capture two different target-position snapshots
    -> release two initial Shells

Exactly one valid Monster
    -> capture one target-position snapshot
    -> release one initial Shell
```

The attack uses one confirmation, one presentation sequence, and one cooldown. Confirmed target positions are not retargeted or canceled during the animation wait. Each released Shell owns independent direct, explosion, bounce, lifetime, and Elemental results. Bounce children never consume Twin Shells again.

Bouncing Shell adds a finite local bounce chain. After a landing resolves a valid direct Monster Hit, it completes every immediate result of that landing in the same frame: direct damage, the direct Elemental attempt, any Explosive Shell actions, explosion-target Elemental attempts, and synchronous Buff, overload, death, or target-state consequences. Only then does it search within the authored `bounceSearchRadius` around the impact position. It excludes the chain hit history, chooses the nearest surviving valid Monster relative to that impact position, captures the target's current position, and creates one bounce child in the same frame. The package-owned `maxBounceCount` limits the chain. It does not use the source tower's full AttackRange or TargetSelectionType. No direct Monster Hit, no remaining bounce count, or no candidate ends the chain.

### Magic Behaviour Upgrades

Twin Orbs releases two independent Magic Orb Attack Entities. Each Orb owns its own orbit angle, contact cooldowns, hit count, lifetime, damage results, and Elemental opportunities.

Arcane Detonation executes one area Effect at the Orb's current world position only when the Orb ends through HitCountExhausted or LifetimeExpired. Forced cleanup, battle end, owner invalidation, and reset do not trigger it. Twin Orbs detonate independently. Every valid Monster resolved by a Detonation receives one explicit Elemental application opportunity.

Arcane Field creates one tower-owned field immediately when the upgrade is applied. The Behaviour package owns field radius and tick interval; its referenced EffectDefinition owns reusable damage and execution feedback. The field follows the tower, has no independent first-version duration, does not duplicate when other upgrades are applied, and ends with tower destruction, removal, or battle cleanup. Each tick resolves every valid Monster inside the field and provides one 100% Elemental application attempt per target. V1 has no per-target Elemental chance parameter.

### Drone Behaviour Upgrades

Twin Drones releases two independent Drone Attack Entities. Each Drone owns its own movement, target, orbit, burst timing, battery, projectile attacks, and optional Final Dive lifecycle.

Blast Rounds adds an area Effect after a Drone projectile's primary direct hit. It is additive rather than replacing direct damage. The primary target may receive direct damage plus explosion damage and two independent Elemental application attempts. Every other valid explosion target receives its own explosion opportunity.

Final Dive adds a battery-end Drone state. Launching does not consume battery. When battery naturally depletes while Orbiting, an invalid current target produces VFX-only aerial despawn. A valid target is locked and the Drone enters FinalDiving, stops firing, pursues the target's current hit position without returning to Orbiting or selecting another Monster, and refreshes a last-valid-position snapshot. If the target becomes invalid during the dive, the Drone continues toward that last valid position. Entering the package-owned positive `finalDiveHitThreshold` around the current destination produces Position Impact.

At Position Impact, Final Dive searches for the nearest valid Monster within `finalDiveHitThreshold` around the actual impact position. A resolved Monster receives direct damage from the Drone's release-time resolved attack damage and one direct Elemental application opportunity. No resolved Monster means no direct damage or direct opportunity. After that optional direct result, the authored Final Dive explosion always executes. Every valid explosion target resolves its own damage and Elemental opportunity; the direct target may therefore receive both results. The Drone despawns after all synchronous impact results complete.

### Elemental Opportunity Audit

Behaviour Layer creates explicit Elemental application opportunities; it does not guarantee successful stacks. Damage amount and DealDamage success do not globally gate an otherwise eligible attempt.

| Upgrade | Elemental Application Opportunity |
|---|---|
| Piercing Arrow | Once for each new Monster Hit resolved by the Arrow |
| Scatter Arrow | Independently for every released Arrow's resolved Monster Hits |
| Hunting Arrow | No periodic application from tracking; actual Monster Hits follow Arrow rules |
| Explosive Shell | Direct target and every explosion target resolve independent attempts; the center may receive both |
| Twin Shells | Every released initial Shell resolves independently |
| Bouncing Shell | Every bounce child resolves its own direct and inherited explosion opportunities |
| Twin Orbs | Every Orb contact resolves independently |
| Arcane Detonation | Once for every valid Monster resolved by a normal-completion Detonation |
| Arcane Field | Once per valid Monster per field tick at 100% eligibility in V1 |
| Twin Drones | Every Drone's projectile hits resolve independently |
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

Tower Framework System owns TowerDefinition and the per-level TowerDefinition config data consumed by TowerUpgradeSystem.

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
