

# Task 004: Buff And Effect Foundation

## 1. Task Overview

This task implements the first-version Buff And Effect System foundation.

The goal is to create a lightweight effect framework that can support projectile impact effects and area damage resolution.

The first version only needs to support AreaDamageEffect.

This task should not implement a complete Buff system.

This task should not implement buff duration, buff stacking, buff ticking, buff removal, buff UI, or status effect gameplay.

---

## 2. Related System Documents

Please review the following system documents before implementation:

- Docs/00_ProjectOverview.md
- Docs/09_ProjectileSystem.md
- Docs/11_BuffAndEffectSystem.md

The main source of truth for this task is:

```text
Docs/11_BuffAndEffectSystem.md
```

---

## 3. Implementation Goal

Create the first-version Buff And Effect foundation.

The implementation should support:

- EffectConfig ScriptableObject
- AreaDamageEffect configuration
- AreaDamageEffect runtime execution
- Area damage target collection
- Area damage dispatch
- Projectile impact integration hook

The implementation should provide a clean extension point for future effects.

---

## 4. Core Architecture Rules

The first version only supports:

```text
AreaDamageEffect
```

The first version does not support:

```text
PoisonEffect
SlowEffect
BurnEffect
Buff Duration
Buff Stack
Buff Tick
Buff Removal
```

Do not build a full buff framework in this task.

Keep the implementation lightweight.

---

## 5. Required Data Definitions

### 5.1 EffectConfig

Create a ScriptableObject representing effect configuration.

Recommended fields:

| Field | Type | Description |
|---|---|---|
| effectId | string | Unique effect identifier |
| radius | float | Area damage radius |

Notes:

- EffectConfig should not contain damage.
- EffectConfig should not contain attack interval.
- EffectConfig should not contain attack range.

Those values belong to AttackConfig.

---

## 6. AreaDamageEffect Runtime

Create a runtime execution path for AreaDamageEffect.

Expected behavior:

```text
Projectile Impact
    ↓
Trigger AreaDamageEffect
    ↓
Find Monsters In Radius
    ↓
Dispatch Attack Damage
```

AreaDamageEffect owns:

- Area query
- Target collection
- Area damage dispatch

AreaDamageEffect does not own:

- Damage calculation
- Monster health logic
- Monster death handling

---

## 7. Runtime Inputs

AreaDamageEffect should receive runtime context.

Recommended inputs:

```text
Impact Position
Attack Damage
Source Tower
EffectConfig
```

Attack Damage should come from:

```text
AttackConfig.damage
```

EffectConfig should not own damage.

---

## 8. Monster Query

AreaDamageEffect must be able to find valid monsters within a radius.

Expected flow:

```text
Impact Position
    ↓
Radius Query
    ↓
Collect Valid Monsters
    ↓
Dispatch Damage
```

Codex should inspect the existing Monster system and choose the safest integration method.

---

## 9. Damage Dispatch

Expected flow:

```text
AreaDamageEffect
    ↓
Damage Request
    ↓
Monster System
```

Monster health calculation belongs to Monster System.

AreaDamageEffect should only dispatch damage.

---

## 10. Projectile Integration

ProjectileBehaviour may trigger an optional effect.

Expected flow:

```text
Projectile Impact
    ↓
impactEffectConfig exists?
    ↓
Execute AreaDamageEffect
```

If no effect is assigned:

```text
Projectile Impact
    ↓
No Effect Triggered
```

This task should support being called by ProjectileBehaviour.

---

## 11. Relationship With Other Systems

### Projectile System

Provides:

- Impact Position
- Attack Damage
- EffectConfig

May trigger AreaDamageEffect.

---

### Tower Runtime Combat System

Provides:

- AttackConfig.damage
- Source combat context

Should not resolve area damage directly.

---

### Monster System

Responsible for:

- Health
- Damage processing
- Death handling

AreaDamageEffect should only dispatch damage requests.

---

## 12. Constraints

Do not implement:

- Buff duration
- Buff stacking
- Buff ticking
- Buff removal
- Buff UI
- Poison effect
- Slow effect
- Burn effect
- Crowd control effects
- Tower combat logic
- Projectile movement

Keep the scope strictly limited to AreaDamageEffect.

---

## 13. Expected Files To Review

Before implementation, inspect the current project structure.

Likely areas:

```text
Assets/Scripts/Projectile
Assets/Scripts/Monster
Assets/Scripts/TowerFramework
Assets/Scripts/BuffAndEffect
Assets/Configs
```

The actual project structure is the source of truth.

---

## 14. Expected Output

After implementation, the project should have:

- EffectConfig ScriptableObject
- AreaDamageEffect runtime execution path
- Area monster query support
- Area damage dispatch support
- Projectile impact integration hook
- Clean ownership boundaries

---

## 15. Verification Checklist

Minimum verification:

- Unity compiles without errors
- EffectConfig assets can be created from the editor
- EffectConfig exposes radius configuration
- Projectile impact can trigger AreaDamageEffect
- AreaDamageEffect can find monsters in radius
- AreaDamageEffect can dispatch damage
- Monster health remains owned by Monster System
- No buff runtime framework was introduced

---

## 16. Implementation Plan Requirement

Before writing code, Codex must inspect the current project and provide an implementation plan.

The implementation plan must include:

1. Existing scripts/classes related to effects, projectile impacts, monster damage, and runtime combat
2. Whether EffectConfig already exists or must be created
3. How AreaDamageEffect will be represented
4. How monsters will be collected within radius
5. How damage will be dispatched
6. How ProjectileBehaviour will trigger AreaDamageEffect
7. Files expected to be modified or created
8. Risks, assumptions, or possible conflicts
9. How the result will be verified in Unity

Do not implement until the plan is reviewed and approved.

---

## 17. Notes

This task builds the first-version effect foundation only.

The first version supports:

```text
AreaDamageEffect
```

Future versions may introduce additional effects and buff runtime systems.

Those features are intentionally out of scope for this task.