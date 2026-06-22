# Task004 - Projectile Flight Foundation

---

# 1. Source Of Truth

Primary system document:

- `Docs/09_ProjectileSystem.md`

Related system documents:

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

Depends on:

- `Docs/Task/Task001_TowerFrameworkSchemaRefactor.md`
- `Docs/Task/Task002_FourTowerConfigAssetMigration.md`
- `Docs/Task/Task003_TowerFrameworkIntegrationValidation.md`

---

# 2. Goal

Clarify and refactor Projectile System flight structure so projectile-style Attack Entities have explicit first-version flight behavior support:

- Direction Flight
- Arc Flight
- Tracking Flight

This task preserves existing Archer and Cannon behavior.

---

# 3. Hard Constraints

- Do not refactor `TowerCombatBehaviour` in this task.
- Do not implement Magic Orb runtime.
- Do not implement Drone runtime.
- Do not treat Magic Orb or Drone themselves as projectiles.
- Preserve existing Archer Direction projectile behavior.
- Preserve existing Cannon Arc projectile behavior.

Projectile System work in this task should remain local to projectile runtime structure and projectile config consumption.

---

# 4. Current Code Context

Current relevant files:

- `Assets/Scripts/Projectile/ProjectileBehaviour.cs`
- `Assets/Scripts/Projectile/ProjectileConfig.cs`
- `Assets/Scripts/Projectile/ProjectileImpactContext.cs`
- `Assets/Scripts/BuffAndEffect/AreaDamageEffectExecutor.cs`
- `Assets/Scripts/TowerFramework/AttackConfig.cs`
- `Assets/Scripts/TowerFramework/AttackArchetype.cs`

Current expected behavior:

- `DirectionProjectile` supports Archer-style fixed-direction flight and distance-based monster hit detection.
- `ArcProjectile` supports Cannon-style travel to a target position and impact event generation.
- `MagicOrb` and `Drone` are Attack Entities, not projectile flight behaviors.
- Drone-fired bullets or missiles may use Projectile System later.

---

# 5. Implementation Scope

## 5.1 Projectile Flight Structure

Refactor `ProjectileBehaviour` so flight behavior is explicit and easy to extend.

Acceptable approaches:

- Private methods grouped by flight behavior.
- Small internal helper methods.
- A lightweight enum-independent branch using `AttackConfig.AttackArchetype`.

Do not add a broad generic projectile framework unless the current code needs it.

## 5.2 Direction Flight

Direction Flight must preserve Archer behavior:

- Tower selects an initial target.
- Projectile launch direction is derived from the selected target's hit/reference anchor.
- After launch, the projectile travels independently in that direction.
- Projectile may hit any valid monster within hit distance threshold.
- Projectile applies direct damage and then destroys itself.
- Projectile expires at `ProjectileConfig.MaxLifetime`.

## 5.3 Arc Flight

Arc Flight must preserve Cannon behavior:

- Tower provides a target position snapshot.
- Projectile travels along an arc toward that target position.
- `AttackConfig.ArcHeight` controls arc height.
- Projectile triggers impact on arrival.
- Area damage behavior remains delegated through impact effect execution.

## 5.4 Tracking Flight

Add Tracking Flight support only at the minimum level needed for Projectile System structure.

Acceptable first implementation:

- A clear placeholder branch that logs unsupported Tracking when invoked.
- Or a minimal tracking movement implementation if the required target data already exists.

Do not create Drone runtime just to test Tracking.

Do not add new tower behavior in this task.

## 5.5 Projectile And Attack Entity Boundary

Update comments or local code naming if needed so the boundary is clear:

- Projectile is a projectile-style Attack Entity.
- Magic Orb is not a projectile by default.
- Drone is not a projectile by default.
- Drone-fired bullets or missiles may be projectiles.

---

# 6. Out Of Scope

Do not implement:

- `TowerCombatBehaviour` orchestration refactor.
- Magic Orb spawn, orbit, contact, hit count, or respawn behavior.
- Drone launch, hover, return, recharge, or projectile firing behavior.
- Tower upgrade structure.
- Upgrade effects.
- Draft changes.
- PlayerSystem changes.
- Object pooling.

---

# 7. Acceptance Criteria

- Archer Direction projectile behavior remains playable and equivalent to current behavior.
- Cannon Arc projectile behavior remains playable and equivalent to current behavior.
- Projectile System has clear Direction, Arc, and Tracking flight branches or structure.
- Magic Orb and Drone are not implemented as projectile lifecycles.
- `TowerCombatBehaviour` is not refactored by this task.
- Projectile impact VFX and AreaDamageEffect integration continue to work for existing projectile impact paths.
- No tower upgrade or Draft logic is introduced.

---

# 8. Suggested Validation

Run targeted source checks:

```text
rg -n "DirectionProjectile|ArcProjectile|Tracking|MagicOrb|Drone" Assets/Scripts/Projectile Assets/Scripts/TowerFramework
rg -n "TowerCombatBehaviour" Assets/Scripts/Projectile
git diff --check -- Assets/Scripts/Projectile Assets/Scripts/TowerFramework
```

If Unity play-mode validation is available:

- Place Archer Tower and confirm arrow firing/hit behavior.
- Place Cannon Tower and confirm shell arc/impact behavior.
- Confirm no Magic Orb or Drone runtime behavior is expected from this task.
