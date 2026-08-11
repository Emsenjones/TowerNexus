# Tower Nexus - Tower Upgrade System

Document Set: System

---

# 1. Purpose And Ownership

Tower Upgrade System owns battle-local growth of individual Tower instances.

It owns:

- Tower level-up eligibility and application
- Stage-bound per-TowerFamily maximum level resolution
- TowerUpgradeDefinition schema
- Required Tower Level and Upgrade Layer rules
- Per-Tower applied Upgrade state
- Duplicate and package-capacity rules
- Upgrade target eligibility queries
- Accepted state-change publication
- Package authoring validation

It records what a Tower has gained. Tower Runtime Combat, Projectile, Effect, Buff, and Tower visual presentation execute the resulting behavior in their own domains.

It does not own Draft generation, placement intent detection, UI feedback, combat scheduling, Projectile flight, Effect execution, Buff state, or Tower-local rendering.

---

# 2. Growth Model

Tower growth has two separate surfaces:

1. Tower Level: level-model change and Stage-authorized content unlocks.
2. Tower Upgrades: the main source of stat, Behaviour, and Elemental build identity.

The intended progression is:

```text
Stage-Authorized Tower Level
    -> Newly Eligible Upgrade Content
    -> Basic Stat Specialization
    -> Attack Behaviour Evolution
    -> Elemental Strategy
```

The current maximum Tower level is three.

Tower Level v0.1 does not directly change damage, Attack Range, Attack Cycle Duration, or another combat stat. Direct combat growth comes from applied Tower Upgrades.

---

# 3. Tower Level-Up

TowerDefinition owns ordered TowerLevelConfig data. Tower Upgrade System owns applying the next valid level to one Tower instance.

A level-up request is valid only when:

- The Tower Draft TowerFamily matches the target TowerFamily.
- The target is below the active Stage maximum for that TowerFamily.
- The target is below the supported and TowerDefinition-configured maximums.
- The next TowerLevelConfig exists and is valid.
- The battle still allows the held item to be consumed.

The active Stage maximum for one TowerFamily is the highest Required Tower Level among that family's Stage Upgrade pool entries. A family with no Stage-allowed Upgrade remains at Level 1. Every level from 2 through that maximum must have at least one Stage-allowed Upgrade whose Required Tower Level equals the reached level.

Stage composition supplies and validates this authoring. Tower Upgrade System binds the resulting Stage level rules and remains the authority queried by preview and final application.

An accepted request:

1. Advances the Tower by exactly one level.
2. Makes the new level's Stage-allowed Upgrade content eligible.
3. Publishes one accepted level-change result.
4. Allows the Tower visual owner to replace the level model.
5. Consumes the Tower Draft item through the calling interaction flow.

A rejected request changes nothing and does not consume the item.

Base damage resolves as:

```text
Resolved Damage
    = Tower Runtime Template Base Attack Damage
    + Sum Of Applied Damage Bonus Deltas
```

Model replacement, Attack Origin resolution, pending presentation handoff, and Upgrade-driven combat refresh belong to their owning systems.

---

# 4. TowerUpgradeDefinition

Each TowerUpgradeDefinition represents one independently draftable Upgrade.

| Data | Contract |
|---|---|
| Display Identity | Name, description, and player-facing presentation |
| TowerFamily | Tower family allowed to receive it |
| Required Tower Level | Minimum level for eligibility |
| Upgrade Layer | Basic, Behaviour, or Elemental design category |
| Layer Data | Stat deltas, package data, or Elemental profile required by that layer |

Required Tower Level and Upgrade Layer are separate concepts. Current content may align levels one, two, and three with Basic, Behaviour, and Elemental, but later content may use a different unlock level without changing layer identity.

TowerUpgradeDefinition does not own Draft weight, choice count, reroll rules, target highlighting, or runtime entity history.

Each StageDefinition independently chooses which TowerUpgradeDefinitions may enter that Stage's Draft pool.

---

# 5. Per-Tower Upgrade State

Every placed Tower records its own accepted Upgrades. Upgrades are not global.

Per-Tower state must answer:

- Which Upgrade definitions are already applied
- Which Basic deltas contribute to resolved values
- Which Behaviour package types and parameters are active
- Whether an Elemental Layer is active
- Which Elemental type and apply Effect are active

State stores accepted facts. Tower Upgrade System interprets those facts for eligibility; runtime owners interpret them for behavior.

---

# 6. Upgrade Eligibility

A TowerUpgradeDefinition may be applied only when:

- TowerFamily matches.
- Current Tower level satisfies Required Tower Level.
- The same definition is not already applied to that Tower.
- The Upgrade Layer and package obey current capacity rules.
- The battle and target remain valid at final application.

First-version capacity rules:

- Multiple different Basic Upgrades are allowed.
- Multiple different Behaviour Upgrades are allowed.
- One Tower may own at most one Upgrade for each non-empty Behaviour package type.
- One Tower may own at most one Elemental Layer.
- No replacement, priority, aggregation, or sequencing rule exists for package conflicts.

Duplicate and capacity restrictions are per Tower, not global. Another compatible Tower may receive the same definition.

Draft System may ask for eligibility while generating candidates or reserving pending capacity. Tower Upgrade System does not decide how those results are weighted or displayed.

---

# 7. Application Transaction

Applying an Upgrade is one atomic semantic transaction:

```text
Revalidate Target And Definition
    -> Record Upgrade On Target Tower
    -> Publish One Accepted State Change
    -> Return Accepted Result
```

Rejected application records nothing, publishes nothing, plays no success result, and does not consume the held item.

After acceptance:

- The interaction owner consumes the held item and pending reservation.
- Tower visual presentation may show Upgrade success feedback.
- Tower Runtime Combat resolves new values and package state.
- Immediate persistent package runtime, such as Arcane Field, may be reconciled.

Multiple accepted changes in one gameplay step must be observable in accepted order. Runtime refresh behavior is defined by Tower Runtime Combat System; Tower Upgrade System does not mutate released entities directly.

---

# 8. Basic Layer

Basic Layer contains numerical combat modifiers and no reusable gameplay Effects. Most modifiers are additive deltas; a family-specific modifier may instead define a reviewed release-time probability whose result is executed by the owning runtime.

Common deltas:

- Attack Range
- Attack Cycle Duration
- Damage Bonus

TowerFamily-specific deltas may include:

- Magic Orb rotation speed
- Drone burst cooldown
- Drone projectile bonus-damage chance

Same-type deltas add together:

```text
Resolved Value = Authored Base + Sum Of Applied Deltas
```

Values are clamped to their valid gameplay ranges after composition.

Arcane Recovery is the Magic Attack Cycle Duration Basic Upgrade. Magic Orb Maximum Hit Count is not a gameplay stat or Upgrade surface.

Expanded Patrol Radius is the Drone Attack Range Basic Upgrade. Drone Battery Duration remains static entity authoring and is not resolved from Tower Upgrade state.

High-Caliber Rounds grants a finite authored chance in `[0, 1]` for each released Drone projectile to carry one fixed bonus direct-damage result. The chance is the Upgrade's authored tuning parameter; the fixed bonus is one. The release result is immutable for that projectile and does not increase package area damage.

Live propagation rules are owned by Tower Runtime Combat System. Basic Layer does not execute Effects or Buffs.

---

# 9. Behaviour Layer

Each Behaviour Layer Upgrade grants one typed package. Upgrade-level parameters remain on the package definition, while a package that creates a self-contained runtime entity may reference a complete prefab whose root Behaviour owns that entity's local authored parameters. Package identity is not a free-form string.

The first-version package set is:

| TowerFamily | Package | Authored Contract |
|---|---|---|
| Archer | Piercing Arrow | Finite hit capacity |
| Archer | Scatter Arrow | Side-Arrow topology and angle |
| Archer | Explosive Arrow | Direct-hit area Effect that includes every surviving valid target in range |
| Cannon | Explosive Shell | Position Impact area Effect |
| Cannon | Multi Shells | Maximum initial Shell count and positive integer Additional Shell Damage |
| Cannon | Bouncing Shell | Bounce count, local radius, bounce Arc height, local selector, and positive integer Bounce Damage |
| Magic | Multi Orbs | Desired synchronized member count |
| Magic | Arcane Detonation | Normal-completion area Effect |
| Magic | Arcane Field | Complete field runtime prefab and presentation; its root Behaviour owns radius, tick interval, and tick Effect |
| Drone | Multi Drones | Absolute maximum active Drone count |
| Drone | Blast Rounds | Projectile-hit area Effect |
| Drone | Final Dive | Positive arrival threshold and impact Effect |

Package definitions own upgrade-level authoring values and complete runtime-prefab references. A referenced runtime prefab may own its entity-local authoring values through its root Behaviour. Runtime owner documents define trigger timing, target resolution, state transitions, and result order.

## 9.1 Timing When Applied

| Package | Existing Runtime Contract |
|---|---|
| Piercing Arrow | Add capacity delta to eligible active Arrows without clearing history |
| Scatter Arrow | Future release only; never add side Arrows to a pending or released group |
| Explosive Arrow | Future release only; never add an explosion result to an active Arrow |
| Explosive Shell | May affect unresolved airborne Shell impacts |
| Multi Shells | Future confirmation only; never add Shells to a pending or released group |
| Bouncing Shell | May affect an initial Shell only before its first Position Impact; never rewrite an active chain |
| Multi Orbs | Atomically add missing members to the active group while preserving its lifecycle |
| Arcane Detonation | Give an incomplete active group future normal-completion eligibility; never detonate immediately |
| Arcane Field | Reconcile one Tower-owned field immediately |
| Multi Drones | Change scheduler capacity without direct launch, cooldown bypass, or batch fill |
| Blast Rounds | Affect future Drone shots and eligible unresolved airborne Drone projectiles |
| Final Dive | Affect active Drones only before battery-end branch resolution |

These timing identities are part of the Upgrade contract. Detailed algorithms remain with Tower Runtime Combat or Projectile System.

---

# 10. Behaviour Composition

Different package types compose by default. The current reviewed combinations include:

- Piercing + Scatter: every Arrow owns independent Piercing state.
- Piercing + Explosive: every new unique Monster Hit may produce one explosion.
- Scatter + Explosive: every independently hitting Arrow may produce one explosion.
- Multi Shells + Explosive: every initial Shell may explode.
- Multi Shells + Bouncing: every initial Shell owns an independent chain; every bounce child uses the Bouncing Shell package's fixed Bounce Damage.
- Explosive + Bouncing: every landing completes explosion results before selecting the next bounce target.
- Multi Orbs + Arcane Detonation: every active synchronized member detonates at normal group completion.
- Multi Drones + Blast Rounds: every active Drone may fire Blast Rounds.
- Multi Drones + Final Dive: every active Drone resolves its own battery-end branch.

The first version defines no general mutual-exclusion rules between different package types. Future exclusions require explicit reviewed content rather than implicit incompatibility.

---

# 11. Elemental Layer

Each Elemental TowerUpgradeDefinition declares:

- TowerFamily
- Required Tower Level
- Elemental type
- One Elemental apply Effect

One Tower may own at most one Elemental Layer.

Elemental state is read at each explicitly authorized unresolved attack boundary. Applying an Elemental Upgrade does not replay earlier hits or change movement, targets, histories, timers, or completed results of active entities.

An Elemental opportunity:

- Requires an explicitly reviewed primary attack or Behaviour boundary.
- Is not inferred from generic Projectile, Effect, or positive-damage identity.
- Is independent from damage amount or damage-operation success while the Monster remains valid.
- Does not automatically propagate through reaction damage, Buff ticks, WindVortex ticks, or overload damage.

Tower-specific Elemental Upgrade definitions may share one Elemental BuffDefinition. After application, Buff duration, stacking, cooldown, Protection, overload, UI, and persistent presentation no longer vary by source Tower.

The first complete content pass contains four Elemental types for each of four TowerFamilies, producing sixteen Elemental TowerUpgradeDefinitions.

## 11.1 Behaviour Elemental Opportunities

| Package | Opportunity Boundary |
|---|---|
| Piercing Arrow | Each new Arrow Monster Hit |
| Scatter Arrow | Each independent Arrow Monster Hit |
| Explosive Arrow | Direct result and every separately resolved explosion target independently; a surviving direct target may receive both |
| Explosive Shell | Direct target and every explosion target independently |
| Multi Shells | Each initial Shell independently |
| Bouncing Shell | Each bounce child's direct and inherited explosion results |
| Multi Orbs | Each Orb contact |
| Arcane Detonation | Every Monster resolved by each normal-completion detonation |
| Arcane Field | Every valid Monster on each field tick at full first-version eligibility |
| Multi Drones | Each Drone projectile's reviewed attack results |
| Blast Rounds | Direct target and every explosion target independently |
| Final Dive | Optional direct target and every explosion target independently |

Buff System remains the authority for whether each attempt applies, refreshes, stacks, overloads, or is blocked.

---

# 12. Validation

Tower Upgrade validation should report or reject at minimum:

- Missing or invalid TowerFamily
- Required Tower Level outside supported progression
- Stage level progression with no newly eligible Upgrade at an intermediate level
- Level-up request without bound Stage level rules
- Missing or incompatible layer data
- Package identity incompatible with TowerFamily
- Missing required package Effect or parameter
- Non-positive count, radius, interval, threshold, or capacity where invalid
- Probability outside its valid range or non-positive Cannon Additional Shell Damage or Bounce Damage
- Duplicate package identity on one Tower
- Second Elemental Layer on one Tower
- Elemental definition without a valid Elemental apply Effect

Invalid content is an authoring error. Runtime does not silently reinterpret it as another package or layer.

---

# 13. Approved Scope And Deferred Topics

Current scope includes three Tower levels, Stage-derived per-TowerFamily level caps, Basic/Behaviour/Elemental layers, the twelve reviewed Behaviour packages, one package of each type per Tower, one Elemental Layer per Tower, per-Tower duplicate rules, and Stage-specific Upgrade pool eligibility support. Removed Hunting Arrow, Magic Orb Maximum Hit Count, and Drone Battery Duration Upgrade identities have no compatibility aliases or fallback interpretation.

Deferred topics include prerequisites, rarity, evolution chains, Upgrade replacement, multi-element Towers, global Upgrades, specialization paths, and persistent progression.
