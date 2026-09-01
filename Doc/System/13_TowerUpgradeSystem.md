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

1. Tower Level: Level-authored BasicDamage growth, level-model change, and Stage-authorized content unlocks.
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

Tower Level v0.2 directly changes BasicDamage. Attack Range, Attack Cycle Duration, and other non-damage combat stats change only through their owning authoring or accepted Tower Upgrades.

`Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md` owns the durable player-facing
interpretation of horizontal deployment, Level investment, Basic, Behaviour,
one-source Elemental value, and matching Elemental cooperation. This System
document owns eligibility, authoring, and application mechanics. Task contracts
and authored assets own exact accepted numerical values and fixture evidence.

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

Before mutation, an accepted request prepares the exact held-Draft ownership,
target and next configuration, runtime readiness, and complete next combat
baseline. No gameplay state changes when preparation fails.

The non-failing semantic commit:

1. Advances the Tower by exactly one level and replaces its Level-authored BasicDamage.
2. Applies the prepared combat baseline through pure cache assignment.
3. Removes the exact held Tower Draft from semantic ownership and marks its view consumed.

After commit, the systems publish exception-isolated level-change and diagnostic
notifications. Those notifications do not refresh gameplay state. The Tower
visual owner then performs best-effort level-model replacement, Attack Origin
handoff, VFX, and consumed-view cleanup. A subscriber or presentation failure
cannot roll back the accepted Level, combat baseline, eligibility, or Draft
consumption and cannot leave the consumed Draft interactive.

A rejected request changes nothing and does not consume the item.

BasicDamage resolves as:

```text
Current Resolved BasicDamage
    = Current TowerLevelConfig.BasicDamage
    + Sum Of Applied Damage Bonus Deltas
```

Damage Bonus remains whole-number authoring. Its accumulated value is not
rounded independently before the final Tower-owned damage formula.

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

Basic Layer contains deterministic numerical combat modifiers and no reusable gameplay Effects. Modifiers are additive deltas.

Common deltas:

- Attack Range
- Attack Cycle Duration
- Damage Bonus

TowerFamily-specific deltas may include:

- Magic Orb rotation speed
- Drone burst cooldown

Same-type deltas add together:

```text
Resolved Value = Authored Base + Sum Of Applied Deltas
```

Values are clamped to their valid gameplay ranges after composition.

Arcane Recovery is the Magic Attack Cycle Duration Basic Upgrade. Magic Orb Maximum Hit Count is not a gameplay stat or Upgrade surface.

Expanded Patrol is the Drone Attack Range Basic Upgrade. Its larger resolved
range applies immediately and may expose a candidate to a Holding Drone through
the Tower Runtime Combat live-refresh contract. It does not move the frozen
Drone range origin or Holding center. Drone Battery Duration remains static
entity authoring and is not resolved from Tower Upgrade state.

High-Caliber Rounds grants an authored deterministic Damage Bonus through the shared Basic stat contract. Because Damage Bonus is part of Current Resolved BasicDamage, it increases Drone direct and Tower-owned Behaviour damage. It never increases shared-state Buff-lifecycle, Overload, or Elemental hit-reaction FixedDamage.

Live propagation rules are owned by Tower Runtime Combat System. Basic Layer does not execute Effects or Buffs.

---

# 9. Behaviour Layer

Each Behaviour Layer Upgrade grants one typed package. Upgrade-level parameters remain on the package definition, while a package that creates a self-contained runtime entity may reference a complete prefab whose root Behaviour owns that entity's local authored parameters. Package identity is not a free-form string.

The first-version package set is:

| TowerFamily | Package | Authored Contract |
|---|---|---|
| Archer | Piercing Arrow | Finite hit capacity |
| Archer | Scatter Arrow | Side-Arrow angle plus additional entity template, count, and DamageScale |
| Archer | Explosive Arrow | Direct-hit area Effect that includes every surviving valid target in range |
| Cannon | Explosive Shell | Position Impact area Effect |
| Cannon | Multi Shells | Additional entity template, count, and DamageScale |
| Cannon | Bouncing Shell | Bounce count, local radius, bounce Arc height, local selector, and positive Bounce DamageScale |
| Magic | Multi Orbs | Additional entity template, count, and DamageScale |
| Magic | Arcane Detonation | Normal-completion area Effect |
| Magic | Arcane Field | Complete field runtime prefab and presentation; its root Behaviour owns radius, tick interval, and tick Effect |
| Drone | Multi Drones | Additional entity template, count, and DamageScale |
| Drone | Blast Rounds | Projectile-hit area Effect |
| Drone | Final Dive | Positive arrival threshold and impact Effect |

Package definitions own upgrade-level authoring values and complete runtime-prefab references. Scatter Arrow, Multi Shells, Multi Orbs, and Multi Drones reuse one additional Attack Entity authoring shape, but each owning Tower runtime remains responsible for release topology and lifecycle. A referenced runtime prefab may own its entity-local authoring values through its root Behaviour. Runtime owner documents define trigger timing, target resolution, state transitions, and result order.

## 9.1 Timing When Applied

| Package | Existing Runtime Contract |
|---|---|
| Piercing Arrow | Add capacity delta to eligible active Arrows without clearing history |
| Scatter Arrow | Future Windup only; never add side Arrows to a pending or released group |
| Explosive Arrow | Future release only; never add an explosion result to an active Arrow |
| Explosive Shell | May affect unresolved airborne Shell impacts |
| Multi Shells | Future Windup only; never add Shells to a pending or released group |
| Bouncing Shell | May affect an initial Shell only before its first Position Impact; never rewrite an active chain |
| Multi Orbs | Atomically add missing members to the active group while preserving its lifecycle |
| Arcane Detonation | Give an incomplete active group future normal-completion eligibility; never detonate immediately |
| Arcane Field | Reconcile one Tower-owned field immediately |
| Multi Drones | Change scheduler capacity without direct launch, cooldown bypass, or batch fill |
| Blast Rounds | Affect future Drone shots and eligible unresolved airborne Drone projectiles |
| Final Dive | Affect active engaged or Holding Drones only before battery-end branch resolution; Holding without a valid current target does not gain an impact target |

These timing identities are part of the Upgrade contract. Detailed algorithms remain with Tower Runtime Combat or Projectile System.

---

# 10. Behaviour Composition

Different package types compose by default. The current reviewed combinations include:

- Piercing + Scatter: every Arrow owns independent Piercing state.
- Piercing + Explosive: every new unique Monster Hit may produce one explosion.
- Scatter + Explosive: every independently hitting Arrow may produce one explosion.
- Multi Shells + Explosive: every initial Shell may explode.
- Multi Shells + Bouncing: every initial Shell owns an independent chain; every bounce child uses the Bouncing Shell package's stable DamageScale against current resolved BasicDamage.
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
- One positive authored stack contribution, shown only for Elemental content

One Tower may own at most one Elemental Layer.

Elemental state is read at each explicitly authorized unresolved attack boundary. Applying an Elemental Upgrade does not replay earlier hits or change movement, targets, histories, timers, or completed results of active entities.

An Elemental opportunity:

- Requires the baseline primary attack boundary authorized by Tower Runtime
  Combat.
- Is not inferred from generic Projectile, Effect, or positive-damage identity.
- Is independent from damage amount or damage-operation success while the Monster remains valid.
- Does not automatically propagate through reaction damage, Buff ticks, WindVortex ticks, or overload damage.
- Is never granted by a Behaviour-added or Behaviour-extended result.

Tower-specific Elemental Upgrade definitions may share one Elemental BuffDefinition. The concrete Upgrade supplies the positive stack-unit request for each explicitly authorized application; this value is authored content and is not inferred from TowerFamily, attack speed, BasicDamage, source count, placement, or Buff state. Basic and Behaviour Upgrades do not participate in this authoring surface.

After application, Buff duration, Maximum Stacks, source cooldown, Protection, overload, UI, persistent presentation, and Electric/Wind normal hit-reaction values no longer vary by source Tower. `ElementalStackContribution` controls only the explicit stack units requested by an authorized baseline-primary application and therefore the opportunity to reach Overload. It never scales or repeats normal Elemental hit reactions.

Normal Elemental value is authored once by the shared Buff and Effect content,
not as a Tower-family output multiplier. Cross-family calibration may revise an
Elemental Upgrade's positive stack contribution when one family's reviewed
application cadence produces an Overload-frequency outlier, but contribution
must not be used to scale periodic, control, or hit-reaction potency. Matching
coverage is expected to increase Overload reliability while isolated matching
sources retain their ordinary shared-Buff value.

The first complete content pass contains four Elemental types for each of four TowerFamilies, producing sixteen Elemental TowerUpgradeDefinitions.

## 11.1 Behaviour And Elemental Separation

Behaviour packages may add members, continuation, area results, persistent ticks,
or completion results, but those additions do not multiply ordinary Elemental
applications. The authoritative per-family primary boundary belongs to Tower
Runtime Combat System. This System owns only the Elemental Upgrade's apply Effect
and positive contribution authoring; it does not derive opportunity eligibility
from Behaviour package presence.

Buff System remains the authority for whether a dispatched request applies,
refreshes, stacks, overloads, or is blocked.

---

# 12. Validation

Tower Upgrade validation should report or reject at minimum:

- Missing or invalid TowerFamily
- Required Tower Level outside supported progression
- Stage level progression with no newly eligible Upgrade at an intermediate level
- Level-up request without bound Stage level rules
- Missing or incompatible layer data
- Damage Bonus delta that is non-finite or not a whole number
- Package identity incompatible with TowerFamily
- Missing required package Effect or parameter
- Non-positive count, radius, interval, threshold, or capacity where invalid
- Missing or incompatible additional Attack Entity root component, or non-positive additional count, DamageScale, or Bounce DamageScale
- Duplicate package identity on one Tower
- Second Elemental Layer on one Tower
- Elemental definition without a valid Elemental apply Effect

Invalid content is an authoring error. Runtime does not silently reinterpret it as another package or layer.

---

# 13. Approved Scope And Deferred Topics

Current scope includes three Tower levels, Stage-derived per-TowerFamily level caps, Basic/Behaviour/Elemental layers, the twelve reviewed Behaviour packages, one package of each type per Tower, one Elemental Layer per Tower, per-Tower duplicate rules, and Stage-specific Upgrade pool eligibility support. Removed Hunting Arrow, Magic Orb Maximum Hit Count, and Drone Battery Duration Upgrade identities have no compatibility aliases or fallback interpretation.

Deferred topics include prerequisites, rarity, evolution chains, Upgrade replacement, multi-element Towers, global Upgrades, specialization paths, and persistent progression.
