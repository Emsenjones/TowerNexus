# Tower Nexus - Tower Growth And Upgrade Identity

Document Set: Balance

Status: Growth strategy and Tower Level v0.1 baseline approved; per-Tower Upgrade values remain Task003 calibration outputs

---

# 1. Purpose And Authority

This document is the durable cross-Stage balance contract for how Draft choices grow a Tower build during one battle.

It answers:

- Which strategic investment a Tower or Tower Upgrade Draft represents
- How current Monster pressure changes the value of that investment
- Why Tower Level is worth buying even without direct combat-stat growth
- How Core and Support Towers divide limited Draft investment
- How Basic, Behaviour, and Elemental value depends on battlefield conditions
- Which growth Anti-patterns later Stage calibration should expose

It does not own runtime eligibility, candidate sampling, exact Upgrade values, exact Stage pools, Player Progress Requirements, or Monster Wave values. Those rules and authored values belong to their owning System documents, Task contracts, and Unity assets.

---

# 2. Draft Investment Model

Every accepted Draft choice spends one limited growth opportunity on one of three strategic investments.

| Investment | Primary Value | Strategic Horizon |
|---|---|---|
| Horizontal Expansion | Deploy another Tower, reshape the Monster route, add coverage, and create another future Upgrade receiver | Current spatial pressure plus future build capacity |
| Vertical Investment | Raise one Tower's level so higher Required Tower Level content becomes eligible | Future Draft and specialization potential |
| Immediate Conversion | Apply one eligible Basic, Behaviour, or Elemental Upgrade to convert the held reward into combat capability | Current pressure plus Upgrade-specific synergy |

These are player strategy types, not required Draft-window categories. One displayed Draft set may contain any valid combination produced by the current candidate pool.

A Tower Draft remains a flexible resource: it may deploy its TowerDefinition or level an existing Tower of the same TowerFamily. A Tower Upgrade Draft applies one specific Upgrade to one eligible placed Tower.

---

# 3. Pressure And Investment Horizon

The intended recurring decision is whether to buy present safety or accept present risk for a stronger future build.

When pressure is low, the route is already serviceable, and the player has identified a likely Core Tower, Level Up should be an attractive investment in future eligibility. A Basic Upgrade that strongly supports the intended final build may still be the better choice.

When pressure is high, direct Upgrade application or spatial expansion may be necessary before further vertical investment is safe.

Pressure is interpreted from the complete battle state, including:

- Whether the alive-Monster backlog is growing or shrinking
- How close the leading Monsters are to the Target
- Remaining Player Health
- Which early, middle, and late route zones have effective Tower coverage
- How much time remains before the next pressure increase

Kill rate compared with spawn rate is useful evidence, but it is not the complete pressure model because route length and remaining exposure provide spatial buffer.

The intended strategy is contextual. Low pressure should often favor Level investment, not make it automatically optimal.

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

# 5. Tower Level v0.1 Experience

Tower Level v0.1 is a technology tier, not a direct combat-stat tier.

An accepted Level Up:

- Advances the Tower by one level
- Changes the level-model presentation
- Makes Stage-allowed Upgrades at the new Required Tower Level eligible
- Does not directly change base damage, Attack Range, Attack Cycle Duration, or another combat stat

The deterministic reward is access to new content. Rogue-like sampling does not guarantee that a newly eligible Upgrade appears in the next or any later Draft.

To prevent structurally empty investment, the active Stage Upgrade pool determines the maximum reachable level for each TowerFamily. Every transition from Level 1 to that maximum must make at least one Stage-allowed Upgrade newly eligible at the reached level.

For example, a Stage that allows one TowerFamily to reach Level 3 must contain at least one Upgrade for that family at Required Tower Level 2 and at least one at Required Tower Level 3.

This is the approved v0.1 baseline. A future direct Level combat bonus requires named balance evidence and a contract revision rather than silent value authoring.

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

# 7. Elemental Learning Arc

Stage5 establishes one complete Elemental Core. Its normal Elemental Effect and stacking improve combat output without requiring Overload for the intended clear.

Stage6 establishes cooperation between two Towers with the same ElementType and overlapping effective coverage. TowerFamily may differ. Their shared application opportunity should make Overload reliable enough to answer Stage6 pressure.

The second matching source extends a complete Elemental strategy; it does not repair an otherwise valueless first Elemental Upgrade.

---

# 8. Expected Anti-patterns

Later calibration should expose these strategically incoherent outcomes:

- Expanding into many undeveloped Level 1 Towers when a Stage requires one mature Core
- Spending a Level Up on a TowerFamily whose Stage pool cannot unlock new content
- Distributing Upgrade investment across low-exposure Support positions while the intended Core remains undeveloped
- Treating a higher Layer name as automatically stronger without reading position, route, Monster composition, or current pressure
- Offering a theoretical build path whose remaining Draft opportunity and sampling probability make it practically unreachable

The game applies no hidden punishment for these choices. They fail only when the resulting build lacks the capability required by the Stage.

---

# 9. Calibration Evidence

Task003 and later Stage calibration should compare:

- Current pressure before the choice
- Immediate pressure change after deployment or Upgrade application
- The Tower level and Draft index at which new content becomes eligible
- The Draft index at which each Core becomes operational
- Remaining Draft opportunities after each Level investment
- Probability and observed timing of seeing newly eligible content
- Route-zone coverage and downstream Tower idle time
- Monster backlog trend, leading-Monster Target distance, leaks, and remaining Player Health
- Whether the owning TowerFamily identity remains readable

The approved result comes from fixed-condition Play Mode evidence, not from nominal damage gain alone.
