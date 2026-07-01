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
Tower Synergy
```

This structure allows towers to first become stronger, then become more unique, and finally interact with other towers through buff and reaction systems.

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
- Basic Layer stat deltas
- Behaviour Layer package
- Future Synergy Layer data
- Authoring validation metadata

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
- Which Basic Layer stat deltas affect this tower
- Which Behaviour Layer packages are active on this tower

Applying a TowerUpgradeDefinition records that upgrade on the target tower.

V1 does not impose a quantity limit on upgrades within the same Required Tower Level category. A tower may receive multiple different Basic upgrades, multiple different Behaviour upgrades, or multiple different future Synergy upgrades as long as it satisfies the category unlock and duplicate rules.

Tower level unlocks upgrade categories:

| Tower Level | Unlocked Upgrade Categories |
|---|---|
| Lv1 | Basic |
| Lv2 | Basic, Behaviour |
| Lv3 | Basic, Behaviour, Synergy |

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

These layers are design categories derived from Required Tower Level in v1. They are not separate TowerUpgradeDefinition data fields.

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

---

## 7.2 Behaviour Layer

Behaviour Layer upgrades modify how a tower attacks.

These upgrades are intended to reinforce the identity of a specific tower type.

Each Behaviour Layer TowerUpgradeDefinition grants one behaviour package.

TowerUpgradeSystem applies the upgrade and records that the tower owns the behaviour package. Tower Runtime Combat and the corresponding runtime modules execute the behavior.

TowerUpgradeSystem should not become a behaviour manager.

Examples:

| Tower | Example |
|---|---|
| Archer Tower | Multi-shot |
| Archer Tower | Pierce |
| Cannon Tower | Larger explosion radius |
| Cannon Tower | Secondary explosion |
| Magic Tower | Additional Orb |
| Magic Tower | Unlimited Hits |
| Magic Tower | Consecutive Hit Bonus |
| Drone Tower | Dual Drones |
| Drone Tower | Missile Attack |

Current design-reference upgrade ideas:

| Tower | Lv1 Ideas | Lv2 Ideas |
|---|---|---|
| Archer Tower | Damage Bonus, Attack Interval | Piercing Arrow, Scatter Arrow, Hunting Arrow |
| Cannon Tower | Damage Bonus, Attack Interval | Bouncing Shell, Burning Shell, Timed Shell |
| Magic Tower | Orb Rotation Speed, Damage Bonus | Twin Orbs, Orb Splash, Resonance Orb |
| Drone Tower | Damage Bonus, Drone Burst Cooldown | Twin Drones, Missile Drone, Final Dive |

These upgrade ideas are design references for framework extensibility. They are not part of the current implementation scope unless a later Task Document explicitly adopts them.

Damage upgrade examples should modify runtime damage bonuses rather than overwrite TowerLevelConfig.basicDamage. TowerLevelConfig.basicDamage remains the tower's level-based base stat.

Burning Shell should not require a general Buff And Effect System in the first upgrade implementation. If implemented before the Buff And Effect System phase, it should remain a Cannon-local behaviour that creates local area damage over time, with a future migration path to the Buff And Effect System.

Purpose:

- Differentiate tower types
- Create build diversity
- Change attack patterns rather than only increasing numbers

---

## 7.3 Synergy Layer

Synergy Layer upgrades introduce buff and interaction mechanics.

Examples:

- Burning
- Frost
- Poison
- Freeze Reaction
- Flame Burst Reaction

Synergy Layer upgrades are intended to create interactions between towers.

Examples:

```text
Frost + Frost
    ↓
Freeze
```

```text
Burning + Burning
    ↓
Flame Burst
```

Purpose:

- Encourage tower combinations
- Create emergent gameplay
- Support future buff reaction systems

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
