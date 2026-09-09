# Tower Nexus - Tower Growth And Upgrade Identity

Document Set: Balance

Status: CombatMathV2 Tower growth and Upgrade value hierarchy accepted; exact
Level, Upgrade, Effect, Buff, and Stage values are authored in their owning assets

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
- Which durable value hierarchy distinguishes Level, Basic, Behaviour, one-source Elemental, and matching Elemental investment

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

Equivalent wording: each Tower Draft spent on Level Up adds more nominal same-family damage capacity than the same Draft spent on another undeveloped Level 1 Tower. Exact margins and integer values are derived through calibration and authored in Tower Level assets. This is not a guarantee that one higher-level Tower always outperforms multiple Towers in a real Map, because horizontal expansion owns legitimate spatial value.

---

# 6. Upgrade Layer Interpretation

Upgrade Layer names do not establish an unconditional strength order.

| Layer | Intended Value Shape |
|---|---|
| Basic | Reliable numerical specialization with low setup dependency |
| Behaviour | Visible attack-strategy change whose gain depends on targets, route geometry, and coverage |
| Elemental | Stable normal Elemental value plus higher conditional value from matching applications and Overload |

A Behaviour Upgrade may outperform a Basic Upgrade in its intended multi-target or persistent-contact condition and underperform it elsewhere. An Elemental Upgrade must provide value before Overload, while matching Elemental sources and overlapping effective coverage raise its reliable ceiling.

Basic and Behaviour value is normally reviewed as a multiplier on the owning
Tower's controlled output because those layers change that Tower's stats or
attack topology. Elemental value uses a different balance model. Its normal
Buff behavior and Overload content are shared fixed-value packages, so their
primary calibration is absolute Elemental damage or control value in a fixed
combat window, not percentage uplift over the carrying Tower's output.

The same Element must remain within a bounded normal-value range when carried
by different TowerFamilies. Natural cadence, contact, target-density, and route
differences may create variation, but no unexplained family-and-Element pairing
should become a dominant spike or a negligible choice. Tower-family stack
contribution exists to normalize progress toward Overload; it does not turn
normal Elemental value back into a Tower damage multiplier.

Matching Elemental cooperation is a separate conditional reward. Two matching
sources with overlapping effective coverage should share stacks and reach
Overload more reliably than the same sources spread across isolated coverage.
That readable cooperation dividend is the intended high Elemental ceiling.
Later Stage balance may demand or reward the completed build through Monster,
Wave, route, and pressure authoring, but Stage values do not redefine the
Elemental package itself.

Required Tower Level and Upgrade Layer remain separate. A Basic or Behaviour Upgrade may require Level 3 when that placement in the growth path supports the Stage lesson.

---

# 7. Accepted CombatMathV2 Value Hierarchy

The current calibration establishes a player-facing hierarchy without forcing
all investments into one universal multiplier.

| Investment | Durable value expectation |
|---|---|
| New Tower | Coverage, parallel targeting, route shaping, and one additional future Upgrade receiver |
| Tower Level | Modest positive concentrated-output advantage plus access to newly eligible content |
| Basic Upgrade | Dependable incremental specialization, approximately `1.2x` in ordinary controlled conditions |
| Behaviour Upgrade | Visible condition-dependent attack-topology gain, approximately `1.5x` in its reviewed condition |
| First Elemental Upgrade | Useful source-independent normal package value, usually stronger than a Basic absolute gain but not guaranteed to exceed Behaviour |
| Second matching Elemental Upgrade | Highest conditional cooperation reward through reliable Overload in overlapping effective coverage |

The accepted naked Level 1 roster remains in one broad output band while route
diagnostics preserve family identity. Equal-Draft Level comparisons give the
concentrated Tower a positive but deliberately modest immediate advantage:
deployment remains competitive because the extra Tower owns spatial value and
future build capacity. The strongest reason to Level is therefore the
combination of immediate BasicDamage growth and later Upgrade access, not an
attempt to make horizontal deployment numerically obsolete.

Basic and Behaviour percentages are same-Tower controlled comparisons.
Elemental uses absolute package value instead. One Elemental
Reference Unit is defined as the rounded median absolute gain of an accepted Behaviour
Upgrade in its controlled fixture. That unit is a design comparison aid, not a
runtime stat or global conversion formula.

One Elemental source must provide normal value without requiring Overload.
Single-source Overload may occur occasionally but is not part of the guaranteed
base return. A second matching source in overlapping coverage earns the premium
ceiling: the accepted cooperation dividend is approximately `1.4-1.6` Behaviour
reference units and is primarily attributable to Overload for directly
damaging Elements. A high-frequency or area-damage companion may amplify
Electric/Wind normal reactions, but it does not replace the stack contribution
and Overload reward of the second matching Elemental Upgrade.

This hierarchy intentionally does not mean:

- every higher Required Tower Level item must beat every lower-level item in
  every geometry;
- every single Elemental Upgrade must individually outperform Behaviour;
- every TowerFamily must convert one shared Element into identical total damage;
- Overload must be frequent from one source;
- one late-Stage matching Build must become the only globally viable strategy.

It means that present safety, horizontal coverage, vertical investment,
immediate specialization, and delayed cooperation remain distinct strategic
reasons to spend a Draft. Stage pressure decides which reason matters now; it
does not redefine the value authority of the underlying Tower package.

## 7.1 Comparison Discipline

Use the smallest comparison that answers the design question:

- compare naked families under one shared fixture to review base identity;
- compare equal Draft counts to review deployment versus Level investment;
- compare one Upgrade with the same-family, same-Level naked control for Basic
  and Behaviour;
- compare shared FixedBuff or control value in one fixed window for Elemental
  normal value;
- compare one-source overlap, two-source overlap, and two-source isolated
  layouts to separate matching-Upgrade and coverage dividends;
- use Stage completion only after the package itself has passed its owning
  calibration contract.

Calibration separately establishes the naked Level 1 baseline, Level curve,
Basic and Behaviour value, and Elemental normal and cooperation value. Exact
accepted parameters are authored in their owning assets. This Balance document
owns the durable interpretation used when later Stage work decides whether to
deploy, Level, specialize, or complete a matching Build.

---

# 8. Growth Curve Principles

Growth should remain readable and reasonably smooth across the limited Draft investments available during one battle.

- Every accepted Upgrade should create meaningful value without being required to repair an unusable Base Tower.
- Immediate lower-level Upgrades should offer dependable current specialization, while Level investment provides concentrated BasicDamage growth plus access to future specialization.
- Higher Required Tower Level content should create new strategic capability, synergy, or scenario strength rather than acting as an unconditional numerical tier above lower-level content.
- No immediate or delayed investment path should become the universally correct choice across pressure states, Tower positions, and intended build roles.
- Large power discontinuities should come from readable build completion or cooperation, not from an isolated unexplained parameter spike.

Tower-owned direct and Behaviour damage derives from the producing Tower's current Level `BasicDamage`. Periodic, Overload, Protection, persistent-area, and shared Electric/Wind Elemental hit-reaction damage uses independently authored fixed values. Elemental stack contribution changes only progress toward Overload. This distinction lets Level growth strengthen the Tower's own damage package without making an already-active shared Buff inherit source-family damage or later Tower growth.

New calibration tasks derive candidate targets and acceptance fixtures from this document and the owning System contracts. Unity assets remain the executable source for authored parameters. Historical calibration evidence is accessible through the [CombatMathV2 closeout index](../History/CombatMathV2_Closeout.md); it does not override current design intent.
