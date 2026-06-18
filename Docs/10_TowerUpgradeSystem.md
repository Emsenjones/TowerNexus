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
| baseDamageModifier | float | Small base damage adjustment for this level |
| attackIntervalModifier | float | Optional attack interval adjustment for this level |
| rangeModifier | float | Optional attack range adjustment for this level |
| towerModelPrefab | GameObject | Optional visual/model replacement for this level |
| displayIcon | Sprite | Optional UI icon for this level |

Exact stat fields may evolve with AttackConfig and TowerRuntimeCombatSystem implementation needs.

## 3.2 Tower Level-Up Request

Tower Draft level-up should be routed as a level-up request to TowerUpgradeSystem.

TowerPlacementSystem only detects placement or target intent.

When a Tower Draft item is dragged onto an existing tower:

```text
Tower Draft Item
    ↓ Dropped On Existing Tower
TowerPlacementSystem
    ↓ Detect Target Tower Intent
TowerUpgradeSystem
    ↓ Validate Tower Level-Up Request
TowerUpgradeSystem
    ↓ Apply Tower Level-Up
```

A tower level-up request is valid only when:

- The Draft item's TowerType matches the target tower's TowerType.
- The target tower has not reached max tower level.
- The run and battle state still allow draft item consumption.

If the request is accepted:

- Consume the Tower Draft item.
- Increase the target tower level by 1.
- Apply the per-level base stat growth from TowerDefinition.
- Replace or update the tower model/visuals for the new level if configured.
- Unlock access to higher-level upgrade pools.

If the request is rejected, the Tower Draft item should not be consumed.

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

- The upgrade TowerType matches the target tower's TowerType.
- The target tower level satisfies Required Tower Level.
- The target tower does not already have the same upgrade.

Example:

```text
Upgrade: Archer Multi Shot
TowerType: Archer
Required Level: 2

Valid Targets:
- Archer Lv2
- Archer Lv3

Invalid Targets:
- Archer Lv1
- Cannon Towers
- Magic Towers
- Watch Towers
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

# 5. Upgrade Pool Generation

Upgrade pool generation is tower-instance driven.

When Draft System requests Tower Upgrade Draft options, the system should inspect all towers currently present on the battlefield.

For every tower instance:

- Determine TowerType.
- Determine TowerLevel.
- Gather all valid upgrades that tower is eligible for.
- Exclude upgrades already owned by that tower.

The resulting candidate set forms the Upgrade Pool for the current Draft.

## 5.1 Tower-Instance Weighting

Upgrade Draft Pool should be tower-instance weighted.

Example:

```text
Battlefield
- Archer Lv2 x 3
- Cannon Lv1 x 1
```

Because three eligible Archer tower instances exist, Archer upgrades naturally have higher representation in the generated pool.

This creates the desired behavior:

- More invested tower types appear more often in upgrade drafts.
- More high-level towers create more opportunities to discover higher-level upgrades.
- The player can shape future upgrade discovery by choosing which towers to deploy and level.

## 5.2 Same-Round Duplicate Prevention

Displayed Draft options should prevent duplicates within the same Draft round.

The pool may contain weighted duplicate candidates internally, but the final displayed choices should not show the same upgrade definition more than once in a single Draft window.

---

# 6. Tower Upgrade Layers

Tower upgrades are divided into three conceptual layers.

---

## 6.1 Basic Layer

Basic Layer upgrades represent common numerical improvements.

These upgrades are generally shared across most tower types.

Examples:

- Damage
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
| Laser Tower | Damage ramps up over time |
| Magic Tower | Larger aura radius |
| Magic Tower | Faster aura tick rate |

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

---

# Change Log

## 2026-06-18 (Tower Level And Instance Upgrade Sync)

- Added Tower Level as a light growth layer separate from Tower Upgrades.
- Added max tower level target, TowerDefinition-owned per-level config direction, and tower level-up request rules.
- Clarified that Tower Upgrades are applied to individual tower instances and are not global upgrades.
- Added per-tower duplicate upgrade rules.
- Added tower-instance weighted Upgrade Draft Pool generation with same-round duplicate prevention for displayed Draft options.

## 2026-05-29

- Created Tower Upgrade System.
- Moved Basic Layer, Behaviour Layer, and Synergy Layer ownership from Tower Framework System.
- Established tower progression structure based on stat growth, behaviour evolution, and tower synergy.
- Clarified ownership boundaries between Tower Upgrade System and Draft System.
