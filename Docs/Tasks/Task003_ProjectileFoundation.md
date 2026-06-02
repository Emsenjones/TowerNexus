

# Task 003: Projectile Foundation

## 1. Task Overview

This task implements the first-version Projectile System foundation.

The goal is to create a lightweight projectile configuration and runtime projectile behavior layer that can be consumed by the future Tower Runtime Combat System.

This task should not implement full tower combat logic.

This task should not implement enemy detection, target selection, tower cooldown logic, tower attack execution, or tower upgrade behavior.

The output of this task should provide clean projectile data and runtime behavior that later systems can use.

---

## 2. Related System Documents

Please review the following system documents before implementation:

- Docs/00_ProjectOverview.md
- Docs/07_TowerFrameworkSystem.md
- Docs/08_TowerRuntimeCombatSystem.md
- Docs/09_ProjectileSystem.md
- Docs/11_BuffAndEffectSystem.md

The main source of truth for this task is:

```text
Docs/09_ProjectileSystem.md
```

---

## 3. Implementation Goal

Create the first-version projectile foundation.

The implementation should support:

- ProjectileConfig ScriptableObject
- ProjectileBehaviour runtime component
- Projectile initialization API
- Straight projectile movement
- Arc projectile movement
- Monster collision hit detection
- Position arrival hit detection
- Direct single-target damage dispatch
- Optional impact effect trigger hook
- Projectile lifetime / cleanup

This task should prepare the projectile layer for later integration with Tower Runtime Combat System.

---

## 4. Core Architecture Rules

Projectile System should not decide when a tower attacks.

Projectile System should not select targets.

Projectile System should not own attack cooldowns.

Projectile System should not own AttackArchetype.

Attack movement style is determined by:

```text
AttackConfig.attackArchetype
```

ProjectileConfig should not introduce a duplicate ProjectileMovementType enum.

The first-version ownership should be:

```text
AttackConfig
    Owns attack behavior and damage

ProjectileConfig
    Owns projectile prefab, speed, and optional impact effect reference

ProjectileBehaviour
    Owns runtime projectile movement, hit detection, impact event triggering, and cleanup
```

---

## 5. Required Data Definitions

### 5.1 ProjectileConfig

Create a ScriptableObject that represents projectile-specific configuration.

Recommended name:

```text
ProjectileConfig
```

Recommended fields:

| Field | Type | Description |
|---|---|---|
| projectileConfigId | string | Unique projectile configuration identifier |
| projectilePrefab | GameObject | Projectile prefab reference |
| projectileSpeed | float | Projectile movement speed in Unity units per second |
| impactEffectConfig | EffectConfig | Optional effect triggered on impact |

Notes:

- projectileSpeed controls how quickly the projectile reaches its target.
- impactEffectConfig is optional.
- Direct single-target damage does not require an Effect.
- AreaDamageEffect is an example of a valid impact effect.
- The first version supports a single impact effect.
- Future versions may support multiple impact effects.

Do not add damage to ProjectileConfig.

Damage is owned by AttackConfig.

Do not add attack range, attack interval, attack archetype, or arc height to ProjectileConfig.

Those fields are owned by AttackConfig.

---

## 6. Required Runtime Component

### 6.1 ProjectileBehaviour

Create or update a runtime projectile component.

Recommended name:

```text
ProjectileBehaviour
```

ProjectileBehaviour is responsible for projectile runtime execution after the projectile has been created and initialized.

Typical responsibilities:

- Store runtime projectile state
- Move the projectile
- Detect collision or arrival
- Dispatch direct damage for simple single-target hits
- Trigger optional impact effect for complex results
- Destroy or recycle the projectile after completion

ProjectileBehaviour should not decide when to spawn.

Projectile creation and initialization belong to Tower Runtime Combat System.

---

## 7. Projectile Runtime State

ProjectileBehaviour should be initialized with the data it needs to run.

Recommended runtime state:

```text
Source Tower
Target Monster
Target Position
Current Position
Lifetime Timer
ProjectileConfig
AttackConfig
Attack Damage
```

Attack Damage should come from AttackConfig.damage.

ProjectileConfig should not own damage.

---

## 8. Projectile Initialization API

Create a clear initialization method that future Tower Runtime Combat code can call.

The exact C# signature may depend on the current project structure.

Recommended concept:

```text
Initialize(
    sourceTower,
    projectileConfig,
    attackConfig,
    targetMonster,
    targetPosition
)
```

The initialization API should support both:

- Monster target projectiles
- Target position projectiles

Example usage:

```text
StraightProjectile
    targetMonster required
    targetPosition optional or derived from targetMonster

ArcProjectile
    targetPosition required
    targetMonster optional
```

Do not implement TowerRuntimeCombatSystem in this task.

Only provide an API that TowerRuntimeCombatSystem can call later.

---

## 9. Movement Behavior

The first version supports two movement behaviors.

Movement behavior is selected by AttackConfig.attackArchetype.

Do not create a separate ProjectileMovementType enum in this task.

---

### 9.1 Straight Projectile Movement

Used by StraightProjectile attacks.

Runtime behavior:

```text
Initialize With Target Monster
    ↓
Move Directly Toward Target Monster
    ↓
Hit Monster
    ↓
Dispatch Direct Damage
    ↓
Destroy Projectile
```

If the target dies or becomes invalid before impact, Codex should choose a safe behavior and explain it in the implementation plan.

Recommended simple behavior:

```text
Destroy projectile when target becomes invalid.
```

---

### 9.2 Arc Projectile Movement

Used by ArcProjectile attacks.

Runtime behavior:

```text
Initialize With Target Position
    ↓
Move Along Arc
    ↓
Reach Target Position
    ↓
Trigger Impact Effect If Assigned
    ↓
Destroy Projectile
```

Arc height is provided by:

```text
AttackConfig.arcHeight
```

Arc Projectile should not directly resolve area damage.

If an impactEffectConfig is assigned, ProjectileBehaviour should trigger the effect and provide:

- Impact position
- Attack damage
- Source tower / source context if available

Area damage resolution belongs to Buff And Effect System.

---

## 10. Hit Detection

The first version should support two hit conditions.

### 10.1 Monster Collision

Used by StraightProjectile.

Example:

```text
Arrow Projectile
    ↓
Collides With Monster
    ↓
Dispatch Direct Damage
```

Direct single-target damage does not require an Effect.

---

### 10.2 Position Arrival

Used by ArcProjectile.

Example:

```text
Cannonball Projectile
    ↓
Reaches Target Position
    ↓
Trigger AreaDamageEffect
```

Position arrival should be based on a clear distance threshold or movement completion rule.

Codex should describe the chosen approach in its implementation plan.

---

## 11. Impact Effect Hook

ProjectileBehaviour should provide an extension point for impact effects.

The first version only needs to support one optional impact effect.

Example:

```text
Projectile Impact
    ↓
impactEffectConfig exists?
    ↓
Trigger Effect
```

If no impactEffectConfig is assigned:

```text
Projectile Impact
    ↓
No Effect Triggered
```

The impact effect trigger should not require Buff runtime implementation.

AreaDamageEffect support will be implemented in the Buff And Effect Foundation task.

---

## 12. Relationship With Other Systems

### Tower Runtime Combat System

Future responsibility:

- Create projectile instance
- Initialize ProjectileBehaviour
- Provide AttackConfig
- Provide ProjectileConfig
- Provide target monster or target position

This task should not implement Tower Runtime Combat System.

---

### Tower Framework System

Provides:

- AttackConfig
- AttackArchetype
- Attack damage
- Arc height

---

### Buff And Effect System

Responsible for:

- AreaDamageEffect execution
- Area damage resolution
- Future buff support

ProjectileBehaviour may trigger an effect, but it should not resolve area damage directly.

---

### Monster System

Responsible for:

- Receiving direct damage
- Updating health
- Handling death

ProjectileBehaviour may call an existing MonsterBehaviour damage API if one already exists.

If no damage API exists yet, Codex should identify the missing dependency and propose the minimal placeholder or defer direct damage integration to the combat integration task.

---

## 13. Constraints

Do not implement:

- TowerCombatBehaviour
- Tower target detection
- Tower target selection
- Tower attack cooldowns
- Tower attack execution loop
- Full Buff runtime
- AreaDamageEffect resolution
- Monster death visual effects
- Tower upgrade logic

Do not create duplicate concepts:

- Do not create ProjectileMovementType if AttackArchetype already determines movement style.
- Do not add damage to ProjectileConfig.
- Do not add arcHeight to ProjectileConfig.
- Do not add explosionRadius to ProjectileConfig.

---

## 14. Expected Files To Review

Before implementation, inspect the current project structure.

Likely areas to inspect:

```text
Assets/Scripts
Assets/Scripts/TowerFramework
Assets/Scripts/Projectile
Assets/Scripts/Monster
Assets/Configs
Assets/Prefabs
Assets/Art/Prefab
```

The actual project structure is the source of truth.

Avoid creating duplicate classes if equivalent classes already exist.

---

## 15. Expected Output

After implementation, the project should have:

- ProjectileConfig ScriptableObject
- ProjectileBehaviour runtime component
- A clear projectile initialization API
- Straight projectile movement support
- Arc projectile movement support
- Direct damage dispatch path for monster collision
- Optional impact effect trigger path
- Clean ownership boundaries with TowerRuntimeCombatSystem and BuffAndEffectSystem

---

## 16. Verification Checklist

Codex should explain how to verify the implementation.

Minimum verification:

- Unity compiles without errors
- ProjectileConfig assets can be created from the editor
- ProjectileConfig can reference a projectile prefab
- ProjectileConfig can optionally reference an EffectConfig if available
- ProjectileBehaviour can be attached to a projectile prefab
- ProjectileBehaviour exposes or supports a clear initialization API
- Straight movement can move toward a target monster
- Arc movement can move toward a target position using AttackConfig.arcHeight
- Direct hit damage path does not require an Effect
- Area damage is not resolved inside ProjectileBehaviour
- No TowerRuntimeCombatSystem logic was added in this task

---

## 17. Implementation Plan Requirement

Before writing code, Codex must inspect the current project and provide an implementation plan.

The implementation plan must include:

1. Existing scripts/classes related to projectile, tower attack config, monster damage, or effects
2. Whether ProjectileConfig already exists or must be created
3. Whether ProjectileBehaviour already exists or must be created
4. How ProjectileBehaviour will be initialized by future TowerRuntimeCombatSystem
5. How StraightProjectile movement will be implemented
6. How ArcProjectile movement will be implemented using AttackConfig.arcHeight
7. How direct damage will be dispatched, or what dependency is missing
8. How optional impact effect triggering will be represented
9. Files expected to be modified or created
10. Risks, assumptions, or possible conflicts
11. How the result will be verified in Unity

Do not implement until the plan is reviewed and approved.

---

## 18. Notes

This task builds the projectile foundation only.

Projectile creation is owned by Tower Runtime Combat System.

Projectile lifecycle execution is owned by Projectile System.

Complex impact results are owned by Buff And Effect System.

This task should prepare the projectile layer without pulling runtime tower combat responsibilities into the projectile implementation.