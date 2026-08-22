# Tower Nexus - Tower Growth And Upgrade Identity

Document Set: Balance

Status: CombatMathV2 growth strategy approved for implementation review; exact Level, Upgrade, Effect, Buff, and Stage values remain Task-owned

---

# 1. Purpose And Authority

This document is the durable cross-Stage balance contract for how Draft choices grow a Tower build during one battle.

It answers:

- Which strategic investment a Tower or Tower Upgrade Draft represents
- How current Monster pressure changes the value of that investment
- Why Tower Level provides direct BasicDamage growth as well as content access
- How Core and Support Towers divide limited Draft investment
- How Basic, Behaviour, and Elemental value depends on battlefield conditions
- How immediate and delayed growth paths remain competitive without becoming universally optimal

It does not own runtime eligibility, candidate sampling, numerical power targets, accepted Upgrade values, test fixtures, route matrices, exact Stage pools, Player Progress Requirements, Monster Wave values, or raw Play Mode evidence. Those rules, values, and evidence belong to their owning System documents, Task contracts, and authored game content. This document remains the qualitative cross-Stage growth contract.

---

# 2. Draft Investment Model

Every accepted Draft choice spends one limited growth opportunity on one of three strategic investments.

| Investment | Primary Value | Strategic Horizon |
|---|---|---|
| Horizontal Expansion | Deploy another Tower, reshape the Monster route, add coverage, and create another future Upgrade receiver | Current spatial pressure plus future build capacity |
| Vertical Investment | Raise one Tower's level, increase its BasicDamage, and make higher Required Tower Level content eligible | Immediate concentrated damage plus future specialization potential |
| Immediate Conversion | Apply one eligible Basic, Behaviour, or Elemental Upgrade to convert the held reward into combat capability | Current pressure plus Upgrade-specific synergy |

These are player strategy types, not required Draft-window categories. One displayed Draft set may contain any valid combination produced by the current candidate pool.

A Tower Draft remains a flexible resource: it may deploy its TowerDefinition or level an existing Tower of the same TowerFamily. A Tower Upgrade Draft applies one specific Upgrade to one eligible placed Tower.

---

# 3. Pressure And Investment Horizon

The intended recurring decision is whether to buy present safety or accept present risk for a stronger future build.

When pressure is low, the route is already serviceable, and the player has identified a likely Core Tower, Level Up should be an attractive concentrated-damage and future-eligibility investment. A Basic Upgrade that strongly supports the intended final build may still be the better immediate specialization choice.

When pressure is high, direct Upgrade application or spatial expansion may be necessary before further vertical investment is safe.

Pressure is interpreted from the complete battle state, including:

- Whether the alive-Monster backlog is growing or shrinking
- How close the leading Monsters are to the Target
- Remaining Player Health
- Which early, middle, and late route zones have effective Tower coverage
- How much time remains before the next pressure increase

Kill rate compared with spawn rate is useful evidence, but it is not the complete pressure model because route length and remaining exposure provide spatial buffer.

The intended strategy is contextual. Low pressure should often favor Level investment, not make it automatically optimal.

At equal Tower-Draft investment and before spatial value is counted, increasing an existing Tower's level must provide more marginal damage capacity than deploying one additional Level 1 Tower of the same family. Deployment remains competitive through route shaping, coverage, parallel targeting, and the creation of another Upgrade receiver. Level Up does not need to outperform every Upgrade Draft universally.

---

# 4. Core And Support Build

The intended end-of-Stage structure is concentrated growth rather than uniform maximum-level investment.

- Support Towers establish route topology, coverage, finishing, or role complement.
- One or two Towers in key positions become primary Cores.
- Core Towers receive most Level and Upgrade investment.
- Support Towers may remain Level 1 without Upgrades when their spatial or role value is already sufficient.
- Deploying a new Tower creates useful spatial value but also competes with vertical Core investment for limited Draft opportunities.

Which Tower becomes a Core depends on position, route exposure, TowerFamily identity, offered Upgrades, and current pressure.

---

# 5. Tower Level v0.2 Experience

Tower Level v0.2 is both a direct BasicDamage tier and a technology tier.

An accepted Level Up:

- Advances the Tower by one level
- Changes the level-model presentation
- Replaces the Tower's current Level-authored BasicDamage
- Makes Stage-allowed Upgrades at the new Required Tower Level eligible
- Does not directly change Attack Range, Attack Cycle Duration, or another non-damage combat stat

The deterministic reward is immediate BasicDamage growth plus access to new content. Rogue-like sampling does not guarantee that a newly eligible Upgrade appears in the next or any later Draft.

To prevent structurally empty investment, the active Stage Upgrade pool determines the maximum reachable level for each TowerFamily. Every transition from Level 1 to that maximum must make at least one Stage-allowed Upgrade newly eligible at the reached level.

For example, a Stage that allows one TowerFamily to reach Level 3 must contain at least one Upgrade for that family at Required Tower Level 2 and at least one at Required Tower Level 3.

The accepted Level curve must satisfy the pure-damage investment guardrail before Stage geometry is considered:

```text
BasicDamage(Level 2) - BasicDamage(Level 1)
    > BasicDamage(Level 1)

BasicDamage(Level 3) - BasicDamage(Level 2)
    > BasicDamage(Level 1)
```

Equivalent wording: each Tower Draft spent on Level Up adds more nominal same-family damage capacity than the same Draft spent on another undeveloped Level 1 Tower. Exact margins and integer values belong to Task003. This is not a guarantee that one higher-level Tower always outperforms multiple Towers in a real Map, because horizontal expansion owns legitimate spatial value.

---

# 6. Upgrade Layer Interpretation

Upgrade Layer names do not establish an unconditional strength order.

| Layer | Intended Value Shape |
|---|---|
| Basic | Reliable numerical specialization with low setup dependency |
| Behaviour | Visible attack-strategy change whose gain depends on targets, route geometry, and coverage |
| Elemental | Stable normal Elemental value plus higher conditional value from matching applications and Overload |

A Behaviour Upgrade may outperform a Basic Upgrade in its intended multi-target or persistent-contact condition and underperform it elsewhere. An Elemental Upgrade must provide value before Overload, while matching Elemental sources and overlapping effective coverage raise its reliable ceiling.

Required Tower Level and Upgrade Layer remain separate. A Basic or Behaviour Upgrade may require Level 3 when that placement in the growth path supports the Stage lesson.

---

# 7. Growth Curve Principles

Growth should remain readable and reasonably smooth across the limited Draft investments available during one battle.

- Every accepted Upgrade should create meaningful value without being required to repair an unusable Base Tower.
- Immediate lower-level Upgrades should offer dependable current specialization, while Level investment provides concentrated BasicDamage growth plus access to future specialization.
- Higher Required Tower Level content should create new strategic capability, synergy, or scenario strength rather than acting as an unconditional numerical tier above lower-level content.
- No immediate or delayed investment path should become the universally correct choice across pressure states, Tower positions, and intended build roles.
- Large power discontinuities should come from readable build completion or cooperation, not from an isolated unexplained parameter spike.

Tower-owned direct, Behaviour, and approved immediate `StackApplied` contribution damage derives from the contributing Tower's current Level `BasicDamage`. Periodic, Overload, Protection, persistent-area, and other shared-state Buff/Elemental-reaction damage uses independently authored fixed values. This distinction lets Level growth strengthen the Tower's own contribution package without making an already-active shared Buff retroactively inherit later Tower growth.

CombatMathV2 Task003, Task004, Task005, and Task007 own candidate targets, fixed-condition evidence, accepted Level/Upgrade values, and revision decisions. Unity assets remain the executable source for authored parameters. This document retains only the design intent used to judge those outputs.
