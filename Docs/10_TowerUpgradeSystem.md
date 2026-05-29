# Tower Nexus - Tower Upgrade System

---

# 1. System Overview

The Tower Upgrade System is responsible for defining how towers grow during a battle.

The Tower Upgrade System defines what upgrades exist, how upgrades are categorized, how tower progression is structured, and how upgrades are applied to towers. It does not decide which upgrades are presented to the player during drafting.

This system owns:

- Tower upgrade layers
- Tower upgrade progression
- Tower level progression
- Upgrade definitions
- Upgrade application rules
- Upgrade layer unlock rules
- Future upgrade prerequisites and evolution paths

The Tower Upgrade System does not own:

- Draft generation
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

---

# 3. Tower Upgrade Layers

Tower upgrades are divided into three conceptual layers.

---

## 3.1 Basic Layer

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

## 3.2 Behaviour Layer

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

## 3.3 Synergy Layer

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

# 4. Future Expansion

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

## 2026-05-29

- Created Tower Upgrade System.
- Moved Basic Layer, Behaviour Layer, and Synergy Layer ownership from Tower Framework System.
- Established tower progression structure based on stat growth, behaviour evolution, and tower synergy.
- Clarified ownership boundaries between Tower Upgrade System and Draft System.