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
- Upgrade layer unlock rules
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

Tower Level is a light growth layer used for small base stat increases, model or visual replacement, and unlocking higher-level upgrade pools.

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
- Access to higher-level Upgrade Pools

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

Tower Level should own basic damage growth. AttackConfig and Tower Upgrade runtime should own attack-behavior or instance-specific damage multipliers.

The first-version damage direction is:

```text
FinalDamage = RoundToInt(TowerLevelConfig.basicDamage * RuntimeDamageMultiplier)
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
- Unlock access to higher-level upgrade pools.

If the request is rejected, the Tower Draft item should not be consumed.

TowerUpgradeSystem owns level-up validation and level data application only. It should not operate TowerBehaviour or TowerVisualController directly.

Tower model replacement is performed through the tower-owned visual/runtime path after TowerPlacementSystem receives an accepted level-up result and asks the target TowerBehaviour to refresh visuals.

TowerUpgradeSystem should not directly manipulate VisualRoot, TowerPrefabSpawnPoint, renderer materials, or AttackOrigin hierarchy.

Tower Level-Up Preview is owned by the placement drag workflow. It currently means a Tower Draft item dragged onto an existing deployed tower with the same TowerFamily can display the Current Level + 1 ghost model before release. It does not include future Tower Upgrade Draft item effect previews.

---

# 4. Tower Upgrade Draft Application

Tower Upgrade Drafts represent tower enhancement items.

Tower Upgrades are applied to individual tower instances.

Tower Upgrades are not global upgrades.

Each tower may gradually develop its own build identity.

Example:

```text
Archer A
- Multi Shot
- Critical Strike

Archer B
- Poison Arrow
- Long Range
```

## 4.1 Upgrade Eligibility

An upgrade may be applied only when:

- The upgrade TowerFamily matches the target tower's TowerFamily.
- The target tower level satisfies Required Tower Level.
- The target tower does not already have the same upgrade.

Example:

```text
Upgrade: Archer Multi Shot
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

## 4.2 Duplicate Rules

A tower cannot receive the same Upgrade twice.

The duplicate restriction is per tower, not global.

Example:

```text
Archer A already owns Multi Shot.
Archer A cannot receive Multi Shot again.
Archer B may still receive Multi Shot.
```

---

# 5. Upgrade Eligibility Support

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

# 6. Tower Upgrade Layers

Tower upgrades are divided into three conceptual layers.

---

## 6.1 Basic Layer

Basic Layer upgrades represent common numerical improvements.

These upgrades are generally shared across most tower types.

Examples:

- Damage multiplier
- Attack Speed
- Range
- Critical Chance

Purpose:

- Improve tower efficiency
- Provide reliable power growth
- Create a stable progression foundation

---

## 6.2 Behaviour Layer

Behaviour Layer upgrades modify how a tower attacks.

These upgrades are intended to reinforce the identity of a specific tower type.

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
| Archer Tower | Arrow Damage Multiplier, Attack Interval | Pierce Arrow, Scatter Arrow, Split Arrow |
| Cannon Tower | Shell Damage Multiplier, Attack Interval | Bouncing Shell, Burning Shell, Delayed Shell |
| Magic Tower | Orb Rotation Speed, Magic Orb Damage Multiplier | Additional Orb, Unlimited Hits, Consecutive Hit Bonus |
| Drone Tower | Drone Projectile Damage Multiplier, Recharge Time | Dual Drones, Missile Attack |

These upgrade ideas are design references for framework extensibility. They are not part of the current implementation scope unless a later Task Document explicitly adopts them.

Damage upgrade examples should modify runtime damage multipliers rather than overwrite TowerLevelConfig.basicDamage. TowerLevelConfig.basicDamage remains the tower's level-based base stat.

Purpose:

- Differentiate tower types
- Create build diversity
- Change attack patterns rather than only increasing numbers

---

## 6.3 Synergy Layer

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

# 7. Related Systems

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

Battle HUD UI System displays Draft items and valid target highlights.

Battle HUD UI System should not own upgrade validation rules.

---

# 8. Future Expansion

Future versions may expand this system with:

- Upgrade prerequisites
- Upgrade rarity
- Upgrade evolution chains
- Upgrade exclusions
- Tower level unlock requirements
- Global upgrades
- Tower specialization paths
- Advanced buff reactions
