# Tower Nexus - Stage Design Blueprint

Document Set: Balance

Status: Campaign learning arc approved; Stage1-Stage5 calibration accepted by
Task010-Task014; the Stage6 Reference Build v0.2 shape remains an approved
baseline whose exact Core families, repeated support family,
UpgradeDefinitions, shared ElementType, and Stage-local calibration values
remain Task015 decisions

---

# 1. Purpose And Authority

This document is the durable design blueprint for the Stage1-Stage6 campaign.

`01_TowerGrowthAndUpgradeIdentity.md` owns the cross-Stage interpretation of Horizontal Expansion, Vertical Investment, Immediate Conversion, and Core-versus-Support growth. This Blueprint applies that shared growth experience to individual Stage lessons and Reference Builds.

It answers:

- What the player should experience in each Stage
- What new content the Stage introduces
- What the intended end-of-Stage Reference Build looks like
- What capability the player must demonstrate
- Which strategically incoherent Anti-pattern should fail

It does not own:

- Exact Map Grid size
- Target Draft count
- Serialized Tower or Tower Upgrade Draft pool asset lists
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

Tower Level is a concentrated BasicDamage and future-eligibility investment under the approved v0.2 growth contract. A Stage may require a Core to reach a level only when its Tower Upgrade pool provides a continuous newly eligible Upgrade path through that level.

The Reference Build is the stable positive control used to derive implementation budgets and perform controlled Stage calibration. It is not intended to be the only legal solution, the globally strongest Build, or the only successful Upgrade order.

The primary Expected Anti-pattern is a build that rejects the Stage lesson and
should fail because it lacks the required capability. Secondary stress fixtures
measure the surrounding Build envelope; they are not automatically required to
fail. The game does not apply hidden penalties for deviating from the Reference
Build.

Concentrated investment is not intrinsically an Anti-pattern. A highly developed Core should fail only when the Stage also requires coverage, role complement, or a second matching Elemental source that the concentrated build does not provide. Conversely, horizontal expansion is a primary Anti-pattern only when additional coverage cannot itself supply the Stage's required capability; otherwise it remains a stress fixture whose margin is measured from evidence.

New Content describes the first campaign introduction of a mechanic or TowerFamily. Tower and Upgrade access is cumulative unless a Stage Note explicitly says otherwise. Each Stage Tower Draft pool contains every TowerFamily introduced by that Stage, and its Tower Upgrade Draft pool contains all currently unlocked UpgradeDefinitions for those represented families. Stage1-Stage4 unlock Basic and Behaviour content; Stage5-Stage6 additionally unlock Elemental content. CombatMathV2 Task010-Task015 each materialize those rules as their exact Stage asset lists and reproducible Fixed calibration fixtures.

Reference Build v0.2 uses these shared rules:

- Every non-core Tower is L1 and has no Basic, Behaviour, or Elemental Upgrade.
- Every Stage1-Stage4 Core Tower is L2 with one Basic and one Behaviour Upgrade.
- Stage1-Stage4 do not use Elemental Upgrades.
- The Stage5 Core Tower is L3 with one Basic, one Behaviour, and one Elemental Upgrade.
- Both Stage6 Core Towers are L3 with one Basic, one Behaviour, and one Elemental Upgrade.
- The two Stage6 Core Towers use the same ElementType and must have overlapping effective coverage.
- Exact Basic, Behaviour, and Elemental identities used by a reproducible Reference Run are selected and recorded by downstream calibration. They are not mandatory player answers.

The listed one-Basic and one-Behaviour Core is the Reference Build used to derive Stage budgets. It is not the maximum legal Upgrade stack on one Tower. Cross-Stage qualitative growth principles belong to `01_TowerGrowthAndUpgradeIdentity.md`; realized-power targets, cumulative single-Tower guardrails, exact Upgrade values, and Stage pressure remain Task outputs.

The intended Core TowerFamily sequence for Stage1-Stage4 is Archer, Cannon, Magic, and Drone. Reference Build intent does not itself force the player's Draft choice. Task010-Task015 use legal Fixed Draft sequences to isolate Build efficacy; natural candidate availability and any justified first-Draft weighting or guarantee remain Task017 design work.

The early campaign teaches the player to establish useful route coverage and
then concentrate enough Draft investment to unlock and apply higher-level
content. A player following that lesson should not need one uniquely named
Upgrade combination to clear. Pure horizontal expansion into many undeveloped
Towers remains a required stress fixture, but evidence may reclassify it as a
low-margin edge Build when coverage itself supplies real compensating value and
the Stage still rejects its primary strategically incoherent Build.

Stage calibration should pursue the ideal ordering of stable Reference clear,
coherent-alternative clear with an accepted margin, and Anti-pattern Defeat. A
Stage may accept a documented secondary exception when it is reproducible,
materially worse than the coherent envelope, does not erase the primary Stage
lesson, and is not manufactured or rejected solely through Player Health.
Fixed-sequence results establish conditional Build efficacy; Task017 separately
measures whether natural Draft offers make that Build realistically accessible.

Player Health is calibrated after the Build envelope is visible. It provides a
reviewed Build-maturation and execution margin for coherent Builds; it must not
be used by itself to manufacture a power gap that the Monster Wave and Build
packages do not otherwise express.

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

Campaign spatial scale should generally increase from Stage1 through Stage4. Stage5 and Stage6 reuse the Stage4 spatial scale so their additional pressure comes from Elemental specialization and cooperation rather than from larger boards. The accepted Map Prefabs remain foundation inputs. CombatMathV2 Stage calibration may propose a Map revision only when reproducible placement, attack-range separation, or route evidence identifies a concrete spatial defect.

---

# 4. Stage Design Contracts

## 4.1 Stage1

Status: Accepted Stage1 Calibration v1 (Task010, 2026-08-27)

| Field | Design Target |
|---|---|
| Primary Experience | Learn that a developed Core plus a second coverage point provides the reliable low-leak path |
| New Content | Archer |
| Required Capability | Reliable completion uses one developed L2 Archer Core plus a second coverage point |
| Expected Anti-pattern | Primary: concentrate every legal investment into one Archer while omitting the required second coverage point; secondary stress: over-expand into five undeveloped L1 Archers |
| Notes | Task010 accepted stable zero-leak Reference clears, coherent alternatives with a small leak margin, a decisive one-Core Defeat, and repeatable five-L1 completion as a rare low-margin edge. Exact results remain in Task010. All unlocked Archer Basic and Behaviour definitions enter the Stage pool; the Reference pair is not a mandatory player answer, and Task017 owns natural offer probability. |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Core | 1 | Archer | L2, one Basic, one Behaviour, no Elemental |
| Support | 1 | Archer | L1, no Upgrades |

## 4.2 Stage2

Status: Accepted Stage2 Calibration v1 (Task011, 2026-08-28)

| Field | Design Target |
|---|---|
| Primary Experience | Combine Archer and Cannon range and cadence roles |
| New Content | Cannon |
| Required Capability | Develop a Cannon damage package while maintaining complementary Archer route coverage against the HP400 late-wave body |
| Expected Anti-pattern | Primary: spend all Drafts on undeveloped horizontal spread, or split Levels into incomplete range-only packages; secondary edge: reject Cannon and rely on a coherent Archer-only package |
| Notes | Task011 accepts Player Health `6`, Progress `[3,3,4,4,4]`, `32` fixed-speed Monsters, final Draft node `18`, and `14` later resolutions. Reference and coherent alternatives clear; Horizontal Sprawl and Fragmented Investment fail on the sixth leak. Archer-only clears with one Health and remains a narrow edge rather than the primary Anti-pattern. Exact results remain in Task011; Task017 owns natural offer probability. |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Core | 1 | Cannon | L2, one Basic, one Behaviour, no Elemental |
| Support | 2 | Archer | L1, no Upgrades |

## 4.3 Stage3

Status: Accepted Stage3 Calibration v1 (Task012, 2026-08-28)

| Field | Design Target |
|---|---|
| Primary Experience | Use Magic contact behavior while covering more than one useful route zone |
| New Content | Magic |
| Required Capability | Persistent route-adjacent contact plus sufficient wider coverage |
| Expected Anti-pattern | Concentrate all power in one local area while leaving the route under-covered |
| Notes | Player Health 6; 40 Monsters; Progress `[3,3,4,4,4,4]`; coherent Builds with sensible multi-zone placement are accepted at `0-3` leaks |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Core | 1 | Magic | L2, Faster Orbit, Arcane Field, no Elemental |
| Support | 3 | Archer ×2 and Cannon ×1 | L1, no Upgrades |

## 4.4 Stage4

Status: Accepted Stage4 Calibration v1 (Task013, 2026-08-29)

| Field | Design Target |
|---|---|
| Primary Experience | Use Drone pursuit to answer expanded spatial pressure |
| New Content | Drone |
| Required Capability | Close the early map shortcut with Support coverage, then retain useful coverage across the expanded route |
| Expected Anti-pattern | Horizontal L1 expansion or a local fixed core that lacks late cross-zone pursuit |
| Notes | Player Health 6; 56 Monsters; Progress `[3,3,4,4,4,8,8]`; Orc HP520 elite capstone; accepted Builds clear at `0-4` leaks |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Core | 1 | Drone | L2, Expanded Patrol, Double Drones, no Elemental |
| Support | 4 | Archer ×2, Cannon ×1, Magic ×1 | L1, no Upgrades |

## 4.5 Stage5

Status: Accepted Stage5 Calibration V9 (Task014, 2026-09-03)

| Field | Design Target |
|---|---|
| Primary Experience | Build the first Elemental core and understand application and stacking |
| New Content | Elemental Layer |
| Required Capability | One Magic Tower reaches L3 with a coherent Basic, Behaviour, and Elemental specialization |
| Expected Anti-pattern | Average investment across all Towers without reaching an Elemental core |
| Notes | Player Health 6; 76 Monsters; Progress `[3,3,4,4,4,8,8,10,10]`; Skeleton Lv6 HP1100; Orc Lv7 HP1700; final Draft node 54 with 22 later resolutions; the Fire Reference and Cold coherent alternative clear, while fragmented four-family investment fails on the sixth leak |

Reference Build:

| Role | Count | TowerFamily | Intended Final State |
|---|---:|---|---|
| Elemental Core | 1 | Magic | L3, Arcane Charge, Twin Orbs, and Blazing Orbs in the reproducible Reference |
| Support | 4 | Cannon, Archer, Drone, and Magic | L1, no Upgrades |

Faster Orbit, Arcane Field, and Frostbound Orbs form the accepted coherent Cold
alternative on the same Magic Core and placement. Both positive fixtures show
meaningful single-source Elemental application and stacking plus visible
non-Elemental Support damage. Dual-source Elemental cooperation is not required
for the Stage5 clear and remains the Stage6 lesson.

## 4.6 Stage6

Status: Reference Build Baseline v0.2

| Field | Design Target |
|---|---|
| Primary Experience | Coordinate two matching Elemental Towers to trigger Overload |
| New Content | Elemental cooperation |
| Required Capability | Two Towers with the same ElementType and overlapping effective coverage |
| Expected Anti-pattern | Stop at one Elemental Core, use unmatched Elements, or separate matching Towers so they do not share targets |
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
    -> Stage Progress requirements and Monster-resolution budget
    -> Fixed-speed Monster Profile and MonsterWaveConfig calibration
    -> Fast-Monster Wave substitutions
    -> Natural Draft offer-probability calibration
```

Task ownership:

| Task | Derived Responsibility |
|---|---|
| CombatMathV2 Task001 | Damage formula, Tower Level BasicDamage authority, DamageScale/FixedDamage migration, and diagnostics |
| CombatMathV2 Task002-Task004 | Level 1 Tower, Tower Level, and non-Elemental Upgrade baselines |
| CombatMathV2 Task005-Task007 | Elemental contribution-damage authority, Drone Burst elemental opportunity, and Elemental/Buff baselines |
| CombatMathV2 Task010-Task015 | Stage-specific Draft totals, exact pools, continuous Level reachability, legal Fixed Build fixtures, Progress requirements, fixed-speed Monster Profile reuse or introduction, Wave tables, Player Health, measurable Reference Runs, Anti-pattern checks, and accepted local calibration |
| CombatMathV2 Task016 | Fast-Monster identity plus reviewed substitutions into accepted Stage Waves |
| CombatMathV2 Task017 | Natural Tower-versus-Upgrade Draft offer ratios, Build accessibility, and availability-versus-choice evidence |

Task calculations are reviewable implementation inputs, not new Stage intent. Accepted executable values live in their owning Unity assets. If a derived result cannot realize an approved Stage design, the Task proposes a Blueprint revision explicitly rather than silently changing the intended experience.

Each CombatMathV2 Task010-Task015 must prove that its Reference, alternative, and Anti-pattern runs are constructible under that Stage's authored pools and Draft opportunities. A Stage Task must not manufacture an impossible Build or add a hidden penalty in order to demonstrate its lesson.

Task010-Task015 use Fixed Draft sequences to isolate Build efficacy from natural
offer probability. They do not infer whether a different final Build resulted
from player preference or from unavailable offers. Task017 asks that question
only after the six Stage configurations and Task016 substitutions are accepted.
