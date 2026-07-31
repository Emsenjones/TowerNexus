# Tower Nexus - Stage Design Blueprint

Document Set: Balance

Status: Campaign learning arc and Stage1-Stage6 Reference Build v0.1 shapes approved; exact repeated support families, UpgradeDefinitions, and ElementTypes remain downstream decisions

---

# 1. Purpose And Authority

This document is the durable design blueprint for the Stage1-Stage6 campaign.

It answers:

- What the player should experience in each Stage
- What new content the Stage introduces
- What the intended end-of-Stage Reference Build looks like
- What capability the player must demonstrate
- Which strategically incoherent Anti-pattern should fail

It does not own:

- Exact Map Grid size
- Target Draft count
- Exact Tower or Tower Upgrade Draft pools
- Player Progress Requirements
- Monster counts, Wave timing, or Stage difficulty values
- Candidate-generation algorithms

Those values are derived by the relevant Task from this Blueprint and the owning System contracts, then authored in Unity assets.

Decision status:

| Status | Meaning |
|---|---|
| Approved | Accepted as durable Stage design intent |
| Baseline v0.1 | Approved as the first testable hypothesis and expected to be revised from evidence |
| TBD | Required design or implementation detail that has not yet been selected |

---

# 2. Shared Interpretation

Each Draft asks the player to allocate limited growth between:

1. Horizontal expansion through additional Towers and coverage.
2. Vertical growth through Tower levels.
3. Specialization through Basic, Behaviour, and later Elemental Upgrades.

The Reference Build is the stable standard solution used to derive implementation budgets and perform controlled Stage calibration. It is not intended to be the only legal solution.

The Expected Anti-pattern is a build that rejects the Stage lesson and should fail because it lacks the required capability. The game does not apply hidden penalties for deviating from the Reference Build.

New Content describes the first campaign introduction of a mechanic or TowerFamily. Previously introduced content remains generally available unless a Stage Note explicitly says otherwise. Exact Draft pool composition is authored later by Task006.

Reference Build v0.1 uses these shared rules:

- Every non-core Tower is L1 and has no Basic, Behaviour, or Elemental Upgrade.
- Every Stage1-Stage4 Core Tower is L3 with one Basic and one Behaviour Upgrade.
- Stage1-Stage4 do not use Elemental Upgrades.
- The Stage5 Core Tower is L3 with one Basic, one Behaviour, and one Elemental Upgrade.
- Both Stage6 Core Towers are L3 with one Basic, one Behaviour, and one Elemental Upgrade.
- The two Stage6 Core Towers use the same ElementType and must have overlapping effective coverage.
- Exact Basic, Behaviour, and Elemental definitions are selected by downstream calibration.

The intended Core TowerFamily sequence for Stage1-Stage4 is Archer, Cannon, Magic, and Drone. Reference Build intent does not itself force the player's Draft choice. Candidate availability and any first-Draft guarantee remain Task006 design work.

---

# 3. Campaign Learning Arc

| Stage | Primary Experience | New Content |
|---|---|---|
| Stage1 | Concentrate limited Drafts into one developed core while maintaining a second coverage point | Archer |
| Stage2 | Combine Towers with different range and cadence roles | Cannon |
| Stage3 | Use route-adjacent persistent contact while covering multiple useful route zones | Magic |
| Stage4 | Answer expanded spatial pressure through pursuit and larger placement commitments | Drone |
| Stage5 | Create the first Elemental core and understand Elemental application and stacking | Elemental Layer |
| Stage6 | Coordinate two Towers with the same ElementType in one overlapping fire zone to trigger Overload | Elemental cooperation |

Campaign spatial scale should generally increase from Stage1 through Stage4. Stage5 and Stage6 reuse the Stage4 spatial scale so their additional pressure comes from Elemental specialization and cooperation rather than from larger boards. Exact Grid sizes are Task001 implementation inputs and may be revised when Reference Build placement, attack-range separation, and route validation require it.

---

# 4. Stage Design Contracts

## 4.1 Stage1

Status: Reference Build Baseline v0.1

| Field | Design Target |
|---|---|
| Primary Experience | Learn that concentrated growth is required; pure horizontal expansion is insufficient |
| New Content | Archer |
| Required Capability | One developed L3 Archer core plus a second coverage point |
| Expected Anti-pattern | Over-expand with undeveloped L1 Archers instead of creating the required L3 core |
| Notes | Exact Basic and Behaviour definitions remain downstream calibration choices |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Core | 1 | Archer | L3, one Basic, one Behaviour, no Elemental |
| Support | 1 | Archer | L1, no Upgrades |

## 4.2 Stage2

Status: Reference Build Baseline v0.1

| Field | Design Target |
|---|---|
| Primary Experience | Combine Archer and Cannon range and cadence roles |
| New Content | Cannon |
| Required Capability | At least one coherent Archer and Cannon role-complement relationship |
| Expected Anti-pattern | Spread growth across undeveloped Towers with no role complement |
| Notes | Three Towers use Archer and Cannon; exact repeated support family remains TBD |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Core | 1 | Cannon | L3, one Basic, one Behaviour, no Elemental |
| Support | 2 | Archer is required; the second support is Archer or Cannon, TBD | L1, no Upgrades |

## 4.3 Stage3

Status: Reference Build Baseline v0.1

| Field | Design Target |
|---|---|
| Primary Experience | Use Magic contact behavior while covering more than one useful route zone |
| New Content | Magic |
| Required Capability | Persistent route-adjacent contact plus sufficient wider coverage |
| Expected Anti-pattern | Concentrate all power in one local area while leaving the route under-covered |
| Notes | Four Towers use Archer, Cannon, and Magic; exact repeated support family remains TBD |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Core | 1 | Magic | L3, one Basic, one Behaviour, no Elemental |
| Support | 3 | Archer and Cannon are required; the third support family is TBD | L1, no Upgrades |

## 4.4 Stage4

Status: Reference Build Baseline v0.1

| Field | Design Target |
|---|---|
| Primary Experience | Use Drone pursuit to answer expanded spatial pressure |
| New Content | Drone |
| Required Capability | Coverage that remains useful across an expanded route |
| Expected Anti-pattern | Rely only on local fixed firepower that cannot cover the expanded route |
| Notes | Five Towers use all four TowerFamilies; exact repeated support family remains TBD |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Core | 1 | Drone | L3, one Basic, one Behaviour, no Elemental |
| Support | 4 | Archer, Cannon, and Magic are required; the fourth support family is TBD | L1, no Upgrades |

## 4.5 Stage5

Status: Reference Build Baseline v0.1

| Field | Design Target |
|---|---|
| Primary Experience | Build the first Elemental core and understand application and stacking |
| New Content | Elemental Layer |
| Required Capability | One Tower reaches a coherent Elemental specialization |
| Expected Anti-pattern | Average investment across all Towers without reaching an Elemental core |
| Notes | Five Towers use all four TowerFamilies; exact Core family, repeated family, and ElementType remain TBD |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Elemental Core | 1 | TBD | L3, one Basic, one Behaviour, one Elemental |
| Support | 4 | All four TowerFamilies represented | L1, no Upgrades |

Dual-source Elemental cooperation is not required for the intended Stage5 clear.

## 4.6 Stage6

Status: Reference Build Baseline v0.1

| Field | Design Target |
|---|---|
| Primary Experience | Coordinate two matching Elemental Towers to trigger Overload |
| New Content | Elemental cooperation |
| Required Capability | Two Towers with the same ElementType and overlapping effective coverage |
| Expected Anti-pattern | Use unmatched Elements or separate matching Towers so they do not share targets |
| Notes | Five Towers use all four TowerFamilies; exact Core families, repeated family, and shared ElementType remain TBD |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Matching Elemental Core | 2 | TBD; matching TowerFamily is not required | L3, one Basic, one Behaviour, one matching Elemental each |
| Support | 3 | Complete the four-family Reference roster together with the two Cores | L1, no Upgrades |

Different TowerFamilies with the same ElementType may contribute to the same shared Elemental Buff on a Monster. Because Overload is required, the authored Draft structure must preserve at least one achievable matching-Element build path.

---

# 5. Downstream Derivation Contract

When a Reference Build changes, its dependent outputs must be recalculated rather than copied back into this Blueprint.

```text
Stage Design Blueprint
    -> Reference Build cost and placement demand
    -> Map implementation size and capacity
    -> Stage Draft opportunities and Draft pools
    -> Shared Progress curve and Monster-resolution budgets
    -> MonsterWaveConfig calibration
```

Task ownership:

| Task | Derived Responsibility |
|---|---|
| Task001 | Candidate Grid size, Reference placement demand, attack-range separation, route zones, and Map Prefabs |
| Task002-Task005 | Stage-independent Tower, Upgrade, Elemental, Buff, Effect, and Monster baselines |
| Task006 | Draft totals, exact Draft pools, candidate availability, one shared Progress curve, and StageDefinition skeletons |
| Task007-Task012 | Stage-specific Wave tables, measurable Reference Runs, Anti-pattern checks, and accepted local calibration |

Task calculations are reviewable implementation inputs, not new Stage intent. Accepted executable values live in their owning Unity assets. If a derived result cannot realize an approved Stage design, the Task proposes a Blueprint revision explicitly rather than silently changing the intended experience.
