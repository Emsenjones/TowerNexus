# Projectile System

## 1. System Overview

The Projectile System is responsible for managing projectile lifecycle after a projectile has been spawned by the Tower Runtime Combat System.

The system manages:

- Projectile movement
- Projectile collision detection
- Projectile lifetime management
- Impact event triggering
- Impact VFX triggering
- Projectile destruction

The Projectile System does not own tower combat logic, buff execution, elemental stack rules, area damage execution, or monster health.

---

## 2. Responsibility Boundary

### Owns

The Projectile System owns:

- Projectile runtime state
- Projectile movement
- Projectile collision detection
- Projectile lifetime management
- Impact event generation
- Projectile-specific impact VFX triggering
- Direct single-target hit dispatch for projectile-to-monster impacts
- Position Impact and Monster Hit fact generation
- Reviewed projectile-local Behaviour execution, including piercing, Hunting tracking, and Bouncing Shell runtime state
- Projectile destruction

### Does Not Own

The Projectile System does not own:

- Target selection
- Attack cooldowns
- Damage formula calculation
- Area damage execution
- Buff application
- Elemental stack application
- Elemental overload rules
- Monster health
- Tower combat logic
- Gameplay effect execution

These responsibilities belong to other systems.

---

## 3. Core Design Philosophy

The Projectile System should remain independent from tower-specific logic.

Projectile behaviour should be determined by the flight identity and typed runtime data supplied by the spawning combat runtime, rather than by branching on tower type.

Bad:

```text
If Archer Tower
    Arrow Logic

If Cannon Tower
    Cannonball Logic
```

Good:

```text
Receive projectile flight identity
    ↓
Spawn Projectile
    ↓
Execute Matching Projectile Behaviour
```

Projectile movement style is determined by the explicit Direction, Arc, or Tracking flight identity supplied at initialization.

ProjectileConfig should never contain a movement type field.

Movement ownership belongs to Projectile System after the spawning runtime supplies the selected flight identity and required inputs.

The Projectile System should only care about projectile runtime execution.

Projectile is a shared runtime concept for projectile-style Attack Entities.

Examples:

- Archer Tower spawns Arrow projectile Attack Entities.
- Cannon Tower spawns Shell projectile Attack Entities.
- Drone is an Attack Entity which may spawn Projectile Attack Entities using the ProjectileConfig supplied by DroneCombatBehaviour.

Magic Orb and Drone themselves are Attack Entities, but they do not have to use the full projectile impact lifecycle unless their behavior is implemented as projectile-style movement and hit resolution.

---

## 4. Projectile Lifecycle

Standard projectile lifecycle:

```text
Spawn
    ↓
Move
    ↓
Hit
    ↓
Trigger Impact Event
    ↓
Trigger Optional Impact VFX
    ↓
Destroy
```

The lifecycle begins when a projectile is created and ends when the projectile is destroyed.

Impact VFX is presentation-only. It must not change projectile hit detection, damage dispatch, area damage execution, target validity, or projectile destruction rules.

---

## 5. Runtime Components

The Projectile System is centered around a runtime projectile component.

Example:

```text
ProjectileBehaviour
```

ProjectileBehaviour acts as the runtime entry point of the Projectile System.

Typical responsibilities:

- Storing runtime projectile state
- Moving the projectile
- Detecting collisions
- Triggering impact events
- Managing lifetime
- Destroying the projectile

Each projectile owns an independent ProjectileBehaviour instance.

---

## 6. Runtime Projectile State

Example runtime data:

```text
Source Tower
Target Monster
Target Position
Current Position
Lifetime Timer
Projectile Config
Attack Damage
Relevant Refreshable Behaviour Options
Elemental Context
Projectile Hit History When Required
Remaining Bounce Count When Required
Initial-Release Or Bounce-Child Identity When Required
```

Runtime state should never be stored inside configuration assets.

Projectile runtime receives only the resolved typed data relevant to its own execution. Future-facing values such as unresolved damage, Piercing, Hunting conversion, Explosive Shell, pre-chain Bouncing Shell, Blast Rounds, and current TrackingRange may receive approved Live Refresh. It should not receive or interpret the source tower's complete TowerUpgradeState or unrelated Behaviour definitions.

Captured landing positions, launch direction, TrackingRangeOrigin, elapsed lifetime, hit history, bounce history, flight progress, completed results, and an already-started bounce chain are immutable Entity State and are never reset by upgrade refresh.

When a projectile needs a monster-side target or hit reference position, that position should come from the Monster System hit/reference anchor concept.

---

## 7. ProjectileConfig (First Version)

The first version introduces a lightweight ProjectileConfig.

ProjectileConfig is responsible for projectile-specific runtime data and visual behavior.

ProjectileConfig should not duplicate data already owned by Tower Framework, the concrete TowerCombatBehaviour, Tower Runtime Combat, or the spawning Attack Entity.

Examples of data that should remain outside ProjectileConfig:

- TowerLevelConfig.basicDamage
- Tower upgrade runtime damage bonuses
- resolved projectile damage
- attackRange
- attackInterval
- attackArchetype
- arcHeight
- tracking behavior parameters when owned by attack behavior rather than projectile-specific visuals

ProjectileConfig should focus on projectile-specific configuration.

Recommended first-version fields:

| projectilePrefab | GameObject | Projectile prefab reference |
| projectileSpeed | float | Projectile movement speed (Unity units per second) |
| hitDistanceThreshold | float | Direct-hit threshold; for Arc projectiles, the one-time Monster query radius around the completed landing position |
| maxLifetime | float | Maximum projectile lifetime before forced cleanup |
| impactEffectDefinition | EffectDefinition | Optional gameplay effect triggered on impact |
| impactVfxPrefab | GameObject | Optional visual effect prefab spawned when impact occurs |

Notes:

- projectileSpeed controls how quickly the projectile reaches its target.
- hitDistanceThreshold controls direct Monster hit checks for projectile types that contact Monsters directly.
- Arc arrival is determined by completing its normalized travel progress and snapping exactly to the captured target-position snapshot. For an Arc projectile, hitDistanceThreshold is only the radius of the one-time nearest-valid-Monster query centered on that completed landing position.
- A baseline Cannon Shell requires a positive hitDistanceThreshold so its Position Impact can resolve at most one nearby Monster for direct damage. It does not disable that query merely because an optional impact Effect is present.
- maxLifetime prevents projectiles from existing forever if impact does not occur.
- Projectile prefab roots are expected to use local +Y as Up and local +Z as Forward.
- ProjectileBehaviour should rotate the projectile so its local +Z direction points toward the movement direction.
- Imported visual models with different source orientations should be corrected as child objects under a projectile prefab root that follows the project convention.
- This orientation convention should be used consistently across all projectile prefabs to avoid per-projectile rotation fixes.
- impactEffectDefinition is optional.
- impactVfxPrefab is optional and presentation-only.
- impactVfxPrefab should point to a prefab prepared for one-shot impact playback, commonly a GameObject with ParticleSystem components.
- Direct single-target projectile hits may dispatch already-calculated damage directly to MonsterBehaviour.
- Complex combat results should still be represented as Effects.
- AreaDamageEffect backed by EffectDefinition is an example of a valid impact effect.
- The first version supports a single impact effect.
- Future versions may support multiple impact effects.

Example:

```text
Arrow
    ↓
Direct Damage
```

No impact effect required.

```text
Baseline Cannon Shell
    ↓
Position Impact
    ↓
Nearest-valid-Monster query within hitDistanceThreshold
    ↓
Optional direct Monster Hit
```

No impact Effect required. Explosive Shell may add an AreaDamageEffect after the baseline direct result.

Design Principle:

```text
Concrete TowerCombatBehaviour
    Owns prefab-authored base attack configuration

TowerLevelConfig
    Owns per-level basic damage

Tower Upgrade Runtime State
    Owns instance-specific damage bonuses

Tower Runtime Combat / Spawning Attack Entity
    Provides resolved damage context

ProjectileConfig
    Owns projectile runtime data

Gameplay Effect Data
    Owns reusable gameplay impact results
```

This separation prevents duplicate configuration and keeps responsibilities clear.

Impact VFX follows the same separation:

```text
ProjectileConfig
    Owns projectile impact presentation reference

ProjectileBehaviour
    Owns impact-time VFX trigger

Impact VFX Prefab
    Owns visual playback
```

Gameplay Effect data should not be required just to play a visual impact effect. A projectile may have impactVfxPrefab without impactEffectDefinition.

---

## 8. Projectile Flight Behaviors

The first version supports three projectile flight behaviors.

### Direction Flight

Used by arrow-style projectiles and other projectiles that travel in a fixed direction after launch.

Example:

```text
Spawn
    ↓
Move In Launch Direction
    ↓
Hit Monster
```

The selected target may define the initial launch direction, but the projectile is not required to remain locked to that target after launch.

Hunting Arrow changes this flight behavior into reviewed locked-target tracking. For attacks released after Hunting is active, Tower runtime captures the authoritative main target position and fixed Center, Left, and Right target slots at confirmation, then supplies TrackingRangeOrigin and current resolved TrackingRange. Each assigned Hunting projectile owns one locked target, hit history, remaining piercing count, and lifetime:

- The locked target must remain gameplay-targetable, registered with MonsterManager, and inside the current refreshed TrackingRange around the immutable TrackingRangeOrigin.
- The Arrow itself must remain inside the same TrackingRange while tracking.
- Tracking checks only the locked target and uses overshoot-safe movement toward its current HitAnchor.
- Hitting the locked target, target invalidation, either range failure, or losing MonsterManager registration permanently transitions the Arrow to Direction flight.
- A surviving Piercing Arrow continues ordinary Direction hits after that transition; source range no longer affects Piercing or projectile collision.
- Tracking never reacquires and never becomes active again after Direction fallback.
- Tracking movement itself does not periodically apply Elemental Buffs; actual resolved Monster Hits use the Arrow attack boundary.

Hunting Arrow is also Live Refresh for still-active Archer Arrows. This released-entity path is distinct from an Archer attack waiting for its Animation Event. Applying the package treats the upgrade moment as a virtual confirmation: the owner takes a snapshot containing only Projectiles with Archer release identity, groups them by owner-local ReleaseGroupId, sorts each existing group by Center/Left/Right slot, collects fresh valid targets using current tower targeting rules, and assigns without target reuse. A partially released Scatter group reconciles only its actual surviving/initialized slots. A single-Arrow group uses the Center slot. Only successfully assigned Arrows convert from their current position into Tracking flight; an Arrow without a valid assignment keeps Direction flight.

Each group consumes a permanent Hunting reconciliation guard after this one virtual confirmation. A converted Arrow also retains a permanent consumed marker, so its later one-way Tracking-to-Direction fallback can never reacquire because of an Attack Range, level, or unrelated upgrade notification. Retrofit never resets direction history, explicit remaining Piercing capacity, Monster hit history, elapsed lifetime, or completed hits.

Piercing Live Refresh adds the resolved capacity delta to each eligible Arrow's explicit remaining hit capacity. It preserves the Arrow's existing Monster hit set and does not reconstruct remaining capacity from the new maximum, so previously consumed hits are never restored.

For a pending Archer attack, confirmation captures candidate identity and fallback direction even when Hunting is absent. If Hunting becomes active before release, the Animation Event only revalidates the captured candidate as registered, gameplay-targetable, and inside current resolved AttackRange. It does not freely retarget. TrackingRangeOrigin is captured from the actual release origin. Pending attacks are not registered Attack Entities and do not receive Projectile refresh methods.

---

### Arc Flight

Used by cannonball-style projectiles.

Example:

```text
Spawn
    ↓
Travel Along Arc
    ↓
Reach Target Position
    ↓
Produce Position Impact
    ↓
Search Nearest Valid Monster Around Impact Position
```

Arc height is provided by ArcProjectileCombatBehaviour.arcHeight for an initial Shell. A bounce child uses the Bouncing Shell package's authored bounceArcHeight.

Arc flight consumes an explicitly present immutable target-position snapshot; `Vector3.zero` remains a valid destination and is not a missing-position sentinel. Later invalidation of the Monster that supplied the snapshot does not cancel or redirect the projectile. A nonpositive Arc hitDistanceThreshold is rejected before projectile instantiation and defensively rejected again during projectile initialization.

Arc flight reaches normalized progress one, snaps exactly to its immutable target-position snapshot, and then produces Position Impact. ProjectileConfig.hitDistanceThreshold is only the radius of the one-time nearest-Monster query centered on that completed landing position. The query uses Monster hit/reference anchors, includes the exact threshold boundary, and selects at most one nearest gameplay-targetable Monster while preserving Monster Manager order for equal distances. Finding a Monster produces Monster Hit and permits baseline direct-hit dispatch. Finding no Monster leaves Position Impact valid, permits presentation and reviewed Position Impact Effects, and then ends the projectile without a Monster-targeted direct result.

An Arc impact payload may retain the optional direct Monster together with the actual impact position. Its Position Impact Effect context keeps `TargetMonster` empty and supplies the actual impact position explicitly, so later area Effects remain centered on the landing rather than shifting to that Monster's hit/reference anchor. The direct Monster's Elemental opportunity is dispatched separately.

---

### Tracking Flight

Used by projectile-style attacks that update their travel direction toward a target or target reference over time.

Example:

```text
Spawn
    ↓
Track Target Or Target Reference
    ↓
Hit Monster Or Expire
```

Tracking flight supports Hunting Arrow and may support future missiles, homing shots, and Drone-fired projectile variants.

Tracking projectiles still belong to Projectile System after they are created and initialized. Tower Runtime Combat or the spawning Attack Entity provides source, target, flight identity, and typed runtime data.

---

### Projectile Orientation Convention

Projectile visual orientation should follow a single project-wide convention:

```text
Projectile Head
      ↓
Local +Z Axis

Projectile Up
      ↓
Local +Y Axis
```

When a projectile is moving, ProjectileBehaviour should align the projectile root's local +Z axis with the current travel direction.

The projectile prefab root should use local +Y as Up and local +Z as Forward. If an imported model faces another axis, rotate the visual child under the prefab root so the root remains consistent.

This prevents projectile prefabs from appearing sideways, backwards, or requiring special-case rotation logic.

---

Future versions may support additional projectile patterns:

- Chain
- Split
- Boomerang

---

## 9. Hit Detection

Different projectile types may use different hit conditions.

Projectile hit detection may consume the monster hit/reference anchor exposed by the Monster System when evaluating monster-side hit positions.

### Monster Collision

Example:

```text
Arrow
    ↓
Collides With Monster
    ↓
Trigger Impact Event
```

Monster collision and distance-based hit checks should resolve against the same monster-side hit/reference concept so projectile behavior remains consistent across monster prefab layouts.

---

### Position Arrival

Example:

```text
Cannonball
    ↓
Reach Target Position
    ↓
Trigger Impact Event
```

For target-position projectile behavior, the target position is provided by the Tower Runtime Combat System. When the target is a monster, that position should represent the monster-side hit/reference position captured at attack confirmation.

Reaching that position produces Position Impact regardless of whether a valid Monster remains nearby. A separate impact-position query may also resolve Monster Hit. One landing may therefore produce both facts or Position Impact only.

---

The Projectile System only determines when a hit occurs.

The Projectile System does not determine complex gameplay results beyond its simple direct-hit exception.

---

## 10. Impact Event Triggering

When a projectile hits a valid target or arrives at a valid position, it generates the corresponding runtime facts and impact context.

Example:

```text
Projectile Hit
    ↓
Impact Event
    ↓
Effect System
```

The Projectile System may directly dispatch single-target damage when a projectile successfully hits a monster.

This exception exists to keep simple projectile attacks lightweight.

For projectile types that use direct hit damage and also author a Position Impact Effect, direct hit damage executes first. The optional Effect then executes as an additional gameplay result using the generated trigger context. Explosive Shell deliberately uses this additive rule, so a direct target may also be included in the explosion.

Projectile System should use the final resolved damage value provided by Tower Runtime Combat or the spawning Attack Entity. It should not own the formula that combines TowerLevelConfig.basicDamage and tower upgrade runtime damage bonuses, and it should not apply damage multipliers.

Examples:

Arrow
    ↓
Hit Monster
    ↓
MonsterBehaviour.TakeDamage(...)

Cannonball
    ↓
Reach Target Position
    ↓
Position Impact
    ↓
Optional Monster Hit
    ↓
Optional Behaviour Position Impact Effect

The baseline Cannon Shell does not require an impact gameplay Effect. It performs one nearest-Monster query around the impact position and may dispatch one direct hit. Explosive Shell adds a Position Impact area Effect without replacing that direct result.

The Projectile System should not directly apply buffs.

For complex impact behavior such as area damage, Buff application, chained Effects, Elemental stack rules, overload rules, or future special mechanics, the Projectile System should generate impact or hit trigger context and delegate execution to Effect System.

Elemental application eligibility is supplied explicitly by the reviewed tower-owned attack or Behaviour extension. A valid opportunity is not globally gated by positive damage or DealDamage success. Direct damage still resolves first; if it kills or removes the Monster, the target is no longer gameplay-targetable at the following Elemental boundary and receives no Buff request. Projectile System does not own the resulting Buff cooldown, Protection, stack, or overload outcome.

Projectile impact VFX remains presentation-only and belongs to ProjectileConfig and Projectile System impact playback. It should not be routed through Effect System or Buff System.

### Bouncing Shell

Bouncing Shell is a reviewed Shell runtime extension, not a generic SpawnProjectile Effect action or a generic ricochet framework.

After a Shell reaches its Position Impact, with or without a direct Monster Hit:

```text
Resolve optional direct Monster Hit
    -> execute Explosive Shell when active
    -> complete explosion damage, death, and target-state updates in the same frame
    -> search around the current impact position using the authored bounceSearchRadius
    -> exclude Monsters already hit by the current bounce chain
    -> apply the Bouncing Shell package's authored selection type to the surviving local candidates
    -> capture that Monster's current hit/reference position
    -> create one bounce child in the same frame
```

No remaining bounce count or no surviving local candidate ends the bounce chain. Direct Monster Hit and positive direct damage are not required. Bounce selection does not use the source tower's full AttackRange or the source combat component's normal TargetSelectionType. The package-owned selector supports Nearest, HighestHealth, LowestHealth, and Random among candidates that already passed local radius and history filtering. It has no Coroutine, next-frame wait, or release delay.

An initial airborne Shell may receive Live Refresh for unresolved damage, Explosive Shell, and Bouncing Shell before its first Position Impact. Once that Position Impact begins a bounce chain, the chain's remaining count, hit history, bounce arc height, and target-selection type become immutable Entity State. Later refresh does not extend or rewrite an active chain.

The bounce child receives relevant typed runtime data: source context, current unresolved damage, Projectile configuration, Explosive Shell state, remaining bounce count, chain hit history, bounce arc height, bounce target-selection type, and bounce-child identity. The initial Shell uses ArcProjectileCombatBehaviour.arcHeight; only bounce children use the package-authored bounce arc height. A child does not receive the complete TowerUpgradeState, does not re-resolve unrelated Cannon Behaviour packages, and does not consume Multi Shells again. Elemental application retains the live sourceTower lookup path.

---

## 11. Relationship With Other Systems

### Tower Runtime Combat System

Responsible for:

- Creating projectiles
- Initializing projectile runtime state
- Providing target information
- Providing flight identity and relevant typed runtime data
- Providing calculated damage context when projectile damage is resolved through Projectile System
- Resolving source Behaviour composition and providing only relevant refreshable options plus immutable Entity State

---

### Attack Entities

Responsible for:

- Spawning projectile Attack Entities when their behavior requires it
- Providing projectile source context
- Providing target or launch direction context
- Providing calculated damage context for projectile Attack Entities they spawn

Example:

```text
Drone Attack Entity
    ↓ Orbit selected target and run burst fire timing
    ↓ Spawn Projectile Attack Entity from droneProjectileConfig
    ↓
Projectile System handles flight, hit detection, impact event, and destruction
```

Drone orbit movement, battery lifetime, battery-end destruction, and burst timing are owned by the Drone Attack Entity, not by Projectile System.

---

### Effect System And Buff System

Responsible for:

- Gameplay effect execution
- Area damage effects
- Buff application effects
- Elemental stack and overload rules

Not responsible for:

- Projectile movement
- Projectile hit detection
- Projectile-specific impact VFX playback

---

### Monster System

Responsible for:

- Health
- Damage processing
- Death handling
- Monster-side hit/reference anchors

---

## 12. First Version Scope

The first version supports:

- Direction Flight
- Arc Flight
- Tracking Flight
- Monster Collision Hit Detection
- Position Arrival Hit Detection
- Impact Event Triggering
- Projectile-level piercing state when granted by an Archer Behaviour package
- Finite piercing hit count for projectile-level piercing
- Hunting Arrow locked-target tracking and one-way Direction fallback
- Reviewed Bouncing Shell local bounce behavior

The first version intentionally excludes these projectile patterns:

- Chain Projectiles
- Split Projectiles
- Generic ricochet frameworks beyond the reviewed Bouncing Shell behavior

Those excluded projectile patterns may be added in future versions.

Piercing is introduced through tower upgrade behaviour packages. Projectile System may execute projectile-level piercing state, but it should not decide why a projectile has piercing.

Projectile-level piercing should have a finite hit count. Projectile System owns tracking which monsters a single piercing projectile has already hit, prevents repeated damage to the same monster from the same projectile, and ends the projectile when its configured piercing hit count is reached.

Projectile lifetime remains a safety cleanup boundary for piercing projectiles. Lifetime should not be the primary balancing rule for how many enemies one piercing projectile may damage.

---

## 13. Summary

Projectile creation is owned by the Tower Runtime Combat System. The Projectile System begins responsibility after a projectile has been initialized.

The Projectile System manages projectile lifecycle after a projectile has been spawned by the Tower Runtime Combat System.

The system is responsible for:

- Moving projectiles
- Detecting hits
- Triggering impact events
- Destroying projectiles

The system should remain independent from tower-specific logic. Simple projectile-to-monster hits may dispatch direct single-target damage, while complex combat results should be delegated to Effect System and, when persistent state is needed, Buff System.
