# Tower Nexus - Tower Framework System

Document Set: System

---

# 1. Purpose And Ownership

Tower Framework System defines what a Tower is and how Tower content is authored.

It owns:

- TowerDefinition and TowerLevelConfig schemas
- TowerFamily identity
- Attack archetype identity
- Shared base combat authoring
- Target-selection categories
- Tower runtime-template hierarchy
- Placement anchors and footprint structure
- Tower level-model and Attack Origin contracts
- Tower-local visual ownership

It does not own placement workflow, placed-Tower combat execution, Projectile behavior, Upgrade eligibility, Draft generation, Effect execution, or Buff runtime.

---

# 2. TowerDefinition

TowerDefinition is the reusable identity referenced by Draft, Placement, Upgrade, UI, and combat systems.

| Data | Contract |
|---|---|
| TowerFamily | Compatibility identity used by level-up and Tower Upgrade rules |
| Display Name | Player-facing name |
| Description | Player-facing summary |
| Icon | Player-facing Draft and UI presentation |
| Tower Runtime Template | Reusable Tower base object |
| Tower Level Configurations | Ordered per-level identity and model data |

TowerDefinition does not contain per-instance state, current targets, cooldowns, applied upgrades, active Attack Entities, or placement occupancy.

## 2.1 TowerLevelConfig

Each supported Tower level provides:

| Data | Contract |
|---|---|
| Level | Unique positive level represented by the entry |
| Tower Level Model | Model/presentation template used at that level |
| Display Icon | Optional level-specific presentation |

Tower level data defines progression identity and model replacement. It does not contain combat stats. TowerUpgradeDefinition defines separately acquired stat, Behaviour, and Elemental content.

---

# 3. Tower Runtime Template

The approved authored hierarchy is:

```text
Tower Root
├── VisualRoot
│   ├── TowerBaseVisualRoot
│   └── TowerPrefabSpawnPoint
├── TowerAnchorSet
│   ├── CenterAnchor
│   └── OccupiedAnchors
├── AttackRangePreview
├── TowerSpawnRefreshVfxAnchor
├── TowerUpgradeAppliedVfxAnchor
└── AttackOriginFallback
```

Equivalent engine structures must preserve the same ownership:

- `VisualRoot` contains Tower-local presentation.
- `TowerBaseVisualRoot` is the permanent base and survives level-model replacement.
- `TowerPrefabSpawnPoint` owns the current level-model instance.
- `TowerAnchorSet` defines placement geometry.
- `AttackRangePreview` is presentation only and has authored radius one at scale one.
- Success-feedback anchors provide optional Tower-local presentation positions.
- `AttackOriginFallback` is used only when the active level model is missing its required Attack Origin.

Missing optional presentation cannot invalidate gameplay. Missing required structure is an authoring error and must be reported.

---

# 4. Placement Anchor Contract

## 4.1 Center Anchor

Each Tower has one Center Anchor used as the placement snap reference and footprint origin.

## 4.2 Occupied Anchors

Occupied Anchors define every Grid Node blocked by the placed Tower relative to the Center Anchor.

Requirements:

- Offsets align with the Map grid.
- Every occupied anchor resolves to one unique Grid Node at placement time.
- Different Towers may author different footprint shapes.
- Placement does not hardcode Tower-specific shapes.
- Tower rotation is not supported by the current footprint contract.

Tower Framework owns the authored footprint. Tower Placement System resolves and validates it.

---

# 5. Tower Level Model Contract

Each TowerLevelConfig may reference one Tower Level Model:

```text
Tower Level Model
├── ModelPresentation
├── VisualRoot
└── AttackOrigin
```

The model supplies its correctly positioned Attack Origin and optional model-local attack presentation.

Rules:

- Deployment uses the Draft result's resolved Tower level.
- Level change replaces only the current Tower Level Model.
- The permanent Tower base remains unchanged.
- Runtime combat consumes the active model's Attack Origin.
- If it is missing, the Tower reports an authoring warning and uses AttackOriginFallback.
- Missing model presentation does not block combat; presentation-gated attacks use their approved fallback release rule.
- Released Attack Entities retain the positions and references captured at release and do not depend on a later model replacement.

Model presentation may receive attack-presentation requests and report the authored release moment. It does not select targets, manage cooldowns, create gameplay results, or apply upgrades.

---

# 6. Tower Visual Ownership

One Tower-local visual owner, currently represented by TowerVisualController under TowerBehaviour, owns rendering operations for that Tower.

It owns:

- Level-model creation and replacement
- Resolution of the active model presentation and Attack Origin
- Complete-preview tint and transparency
- Attack-range presentation
- Valid-target highlighting
- Placement, level-model refresh, and Upgrade success presentation

Gameplay systems decide when a result or feedback state is valid and request the presentation. They do not directly alter Tower visual internals, model children, authored appearance resources, or feedback instances.

The visual owner never decides placement validity, Tower level, Upgrade eligibility, attack range values, attack timing, or Draft item consumption.

---

# 7. Attack Archetypes And Attack Entities

A Tower orchestrates attacks. An Attack Entity executes released attack behavior.

Tower-side responsibilities:

- Detect valid Monsters
- Select targets when required
- Manage attack readiness
- Confirm an attack
- Release Attack Entities

Attack Entity responsibilities:

- Movement, orbit, or tracking
- Hit or contact detection
- Lifetime and consumed history
- Damage or gameplay-trigger dispatch at reviewed boundaries
- Completion

The first version contains four TowerFamilies and base archetypes:

| TowerFamily | Base Archetype | Attack Entity |
|---|---|---|
| Archer | Direction Projectile | Arrow |
| Cannon | Arc Projectile | Shell |
| Magic | Orbiting Group | Magic Orb group |
| Drone | Autonomous Entity | Drone, which may release projectiles |

Each Tower runtime template authors exactly one compatible base archetype identity. A second selector that can disagree with that identity is invalid authoring.

Projectile movement identity is separate from Tower attack archetype: Direction, Arc, and Tracking describe projectile flight only.

---

# 8. Base Combat Authoring

Base combat authoring lives with the Tower runtime template that consumes it, not in TowerDefinition and not in TowerUpgradeDefinition.

Common authored data:

| Data | Contract |
|---|---|
| Base Attack Damage | Base damage before applied Basic Damage Bonus deltas |
| Attack Range | Base acquisition and release range before Upgrade changes |
| Attack Cycle Duration | Minimum duration from one successful Attack Entity release until the archetype may release again |
| Target Selection | Selection category used by archetypes that select one Monster |
| Release Presentation | Optional presentation played at the approved release boundary |

Archetype-specific authored data:

| Archetype | Additional Base Data |
|---|---|
| Direction Projectile | Projectile entity template |
| Arc Projectile | Projectile entity template and initial arc height |
| Magic Orb | Magic Orb entity template |
| Drone | Drone entity template and base maximum active Drone count |

Projectile entity authoring owns base movement speed, hit distance threshold, safety lifetime, optional impact Effect, optional impact presentation, and entity presentation.

Magic Orb entity authoring owns base orbit, contact distance, per-member hit count, lifetime, same-target contact cooldown, and entity presentation.

Drone entity authoring owns its projectile entity template, movement, orbit, battery, burst timing, internal Fire Anchor, and entity presentation.

At release, Tower Runtime Combat combines base combat authoring and applied Tower Upgrade state into only the runtime data relevant to the released entity. Tower level selects presentation and Upgrade eligibility but does not alter combat values in the v0.1 growth model. Runtime history is never stored in authored data.

---

# 9. Target Selection

First-version target selection categories are:

| Category | Rule |
|---|---|
| Nearest | Valid Monster with the shortest distance to the selection origin |
| Highest Health | Valid Monster with the greatest current health |
| Lowest Health | Valid Monster with the least current health |
| Random | One random valid Monster |

Archer, Cannon, and Drone use a target-selection category. Baseline Magic Orb behavior does not select one release target because contacts are resolved while orbiting.

Package-local selection, such as Bouncing Shell, may reuse the same categories after applying its own local eligibility filter. That does not delegate the package decision back to ordinary Tower target selection.

---

# 10. Base Tower Behavior

This section defines identity and gameplay direction. Runtime execution belongs to Tower Runtime Combat and Projectile System.

## 10.1 Archer

- Short range, high attack speed, low damage per Arrow
- Selected Monster defines initial launch direction
- Released Arrow travels independently
- Direction flight may hit the nearest valid Monster within its hit threshold
- Attack Cycle begins on successful Arrow release

## 10.2 Cannon

- Long range and low attack speed
- Confirmation captures a target-position snapshot
- Shell travels to that immutable position
- Arrival always produces Position Impact
- A local arrival query may additionally produce one Monster Hit and direct damage
- Area explosion is Upgrade content, not baseline Cannon behavior
- Attack Cycle begins on successful Shell release

## 10.3 Magic

- Owns at most one active synchronized Magic Orb group
- Successful group activation starts one Attack Cycle while the group remains active
- Normal group completion clears exact ownership without restarting the Cycle
- A new group requires Attack Cycle readiness and no active group
- Members orbit one release-time center
- Each member owns independent remaining hits and contact history
- The group owns shared phase, lifetime, and completion
- Exhaustion of any member or shared lifetime completes the whole group
- Technical cleanup removes the group and clears scheduler state

## 10.4 Drone

- Releases autonomous Drones one at a time while below current capacity
- A Drone launches only with Attack Cycle readiness and a valid target
- Each Drone owns movement, target, orbit, battery, projectile bursts, and completion
- Loss of target attempts an in-range retarget; no replacement target ends ordinary Drone work
- Drone may enter package-defined Final Dive behavior at battery end

Detailed release scheduling, Live Refresh, entity state machines, and package composition belong to their runtime owner documents.

---

# 11. Entity Orientation And Anchors

Reusable Attack Entity templates follow authored orientation conventions so runtime does not require model-specific offsets.

- Drone root local +Z is active-flight forward.
- Drone internal Fire Anchor is the origin for Drone-fired projectiles and release presentation.
- Imported visual orientation differences are corrected inside the entity's visual hierarchy.
- Tower Attack Origin releases the Drone; the moving Drone does not continue depending on Tower child references.

Projectile orientation is defined by Projectile System.

---

# 12. Effect And Buff Boundary

- Simple direct attack damage may use the lightweight direct-damage path.
- Reusable one-shot area or package results use Effect System.
- Persistent unit-attached state uses Buff System.
- Position Impact and Monster Hit remain separate facts.
- Elemental application is granted only by an explicitly reviewed attack boundary.
- Presentation never decides hit, damage, Effect, Buff, or Elemental eligibility.

---

# 13. Validation

Tower authoring validation should report at minimum:

- Missing Tower runtime template
- Missing or mismatched TowerFamily archetype identity
- Missing or duplicate Tower levels
- Missing required level model
- Invalid base combat damage
- Missing Center Anchor or invalid Occupied Anchors
- Missing required Attack Entity configuration
- Non-positive or invalid base timing, range, lifetime, or capacity values
- Missing model Attack Origin, using fallback as a reported authoring error
- Invalid entity orientation or required internal anchor where detectable

Validation reports the source content and does not silently replace the authored archetype or footprint.

---

# 14. Approved Scope And Deferred Topics

Current scope includes four TowerFamilies, four base archetypes, three projectile flight identities, runtime-template base combat data, level-model data, anchor-defined footprints, level-model replacement, Tower-local presentation ownership, and first-version target selection.

Deferred Tower identities include support, trap, summon, resource, laser, boomerang, missile, and other archetypes that require reviewed behavior rather than expansion of one generic Tower type.
