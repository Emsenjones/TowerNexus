

# Task 001: Tower Framework Data Definitions

## 1. Task Overview

This task implements the data definition layer of the Tower Framework System.

The goal is to create configurable ScriptableObject-based data structures for towers and tower attack configurations.

This task should not implement runtime tower combat logic.

This task should not implement projectile movement, buff logic, damage processing, target detection, cooldown behavior, or tower upgrade behavior.

The output of this task should provide clean data assets that later systems can consume.

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
Docs/07_TowerFrameworkSystem.md
```

---

## 3. Implementation Goal

Create the first-version tower framework data definitions.

The implementation should support:

- TowerDefinition ScriptableObject
- AttackConfig ScriptableObject
- TowerCategory enum
- AttackArchetype enum
- TargetSelectionType enum
- Basic configurable fields required by the first version
- Inspector-friendly editing workflow
- Odin Inspector usage where helpful

The implementation should make it easy to create and configure different tower assets in the Unity editor.

---

## 4. Required Data Definitions

### 4.1 TowerDefinition

Create or update a ScriptableObject that represents a tower definition.

Recommended name:

```text
TowerDefinition
```

Recommended responsibility:

```text
Defines what a tower is.
```

Recommended fields:

| Field | Type | Description |
|---|---|---|
| towerId | string | Unique tower identifier |
| displayName | string | Display name used for editor or UI |
| description | string | Short tower description |
| towerCategory | TowerCategory | Tower category/type |
| towerPrefab | GameObject | Runtime tower prefab reference |
| attackConfig | AttackConfig | Attack configuration reference |
| icon | Sprite | Optional tower icon for UI |

If the existing project already has similar fields, prefer updating or aligning the existing structure instead of creating duplicate fields.

---

### 4.2 AttackConfig

Create a ScriptableObject that represents static tower attack configuration.

Recommended name:

```text
AttackConfig
```

Recommended responsibility:

```text
Defines combat configuration data consumed by Tower Runtime Combat System.
```

Recommended fields:

| Field | Type | Description |
|---|---|---|
| attackConfigId | string | Unique attack configuration identifier |
| attackArchetype | AttackArchetype | Attack behavior type |
| attackRange | float | Maximum attack range |
| attackInterval | float | Time between attacks |
| targetSelectionType | TargetSelectionType | Target selection rule |
| damage | float | Base damage value |
| projectileConfigId | string | Projectile configuration reference for future Projectile System |
| effectConfigId | string | Effect configuration reference for future Buff And Effect System |
| explosionRadius | float | Area damage radius for cannon-style attacks |
| damagePerSecond | float | Continuous damage value for channel attacks |
| maxChannelDuration | float | Maximum channel duration |
| areaTickInterval | float | Periodic area damage interval |

Not every attack archetype uses every field.

Unused fields should be hidden in the Inspector whenever practical.

Use Odin Inspector conditional display features to show only fields relevant to the selected AttackArchetype.

The goal is to keep AttackConfig assets clean, readable, and easy to configure.

Use Inspector grouping, labels, validation, or conditional display where helpful.

Odin Inspector may be used to improve editor readability.

---

### 4.3 TowerCategory

Create or update an enum for tower category.

Recommended name:

```text
TowerCategory
```

Recommended values:

```text
Archer
Cannon
Magic
Watch
```

This enum describes the tower category for identification and editor organization.

Runtime combat behavior should still be driven by AttackConfig and AttackArchetype.

---

### 4.4 AttackArchetype

Create or update an enum for attack archetype.

Recommended name:

```text
AttackArchetype
```

Recommended values:

```text
StraightProjectile
ArcProjectile
ChannelBeam
PeriodicArea
```

This enum determines which runtime attack execution path the Tower Runtime Combat System will use.

---

### 4.5 TargetSelectionType

Create or update an enum for target selection.

Recommended name:

```text
TargetSelectionType
```

Recommended values:

```text
Nearest
HighestHealth
LowestHealth
Random
```

The Tower Framework System only defines these options.

The Tower Runtime Combat System will consume this enum and implement the actual selection logic.

---

## 5. Odin Inspector Guidance

Odin Inspector is available in the project and may be used when useful.

Recommended Odin usage:

- Group AttackConfig fields by attack archetype
- Show projectile-related fields only for projectile archetypes if practical
- Show channel-related fields only for ChannelBeam if practical
- Show area-related fields only for ArcProjectile or PeriodicArea if practical
- Add required-field validation for key references
- Add editor-only helper buttons only if they are safe and simple

Do not over-engineer the editor UI.

The first version should prioritize clear data structure over complex editor tooling.

---

## 6. Constraints

Do not implement:

- TowerCombatBehaviour
- Target detection
- Target selection runtime logic
- Attack cooldowns
- Projectile movement
- ProjectileBehaviour
- Effect runtime execution
- Buff runtime execution
- Monster damage or death logic
- Tower upgrade logic

This task is data-definition only.

---

## 7. Expected Files To Review

Before implementation, inspect the current project structure and identify existing related files.

Likely areas to inspect:

```text
Assets/Scripts
```

The actual project structure may differ.

Use the current project structure as the source of truth.

Avoid creating duplicate classes if equivalent classes already exist.

---

## 8. Expected Output

After implementation, the project should have configurable ScriptableObject assets or ScriptableObject types for:

- TowerDefinition
- AttackConfig

The project should also have enums or equivalent type definitions for:

- TowerCategory
- AttackArchetype
- TargetSelectionType

The data should be usable by later systems:

```text
TowerRuntimeCombatSystem
ProjectileSystem
BuffAndEffectSystem
TowerUpgradeSystem
```

---

## 9. Verification Checklist

Codex should explain how to verify the implementation.

Minimum verification:

- Unity compiles without errors
- TowerDefinition assets can be created from the editor
- AttackConfig assets can be created from the editor
- TowerDefinition can reference AttackConfig
- TowerDefinition can reference a tower prefab
- TargetSelectionType exposes Nearest, HighestHealth, LowestHealth, and Random
- AttackArchetype exposes StraightProjectile, ArcProjectile, ChannelBeam, and PeriodicArea
- Inspector layout is readable
- No runtime combat logic was added in this task

---

## 10. Implementation Plan Requirement

Before writing code, Codex must inspect the current project and provide an implementation plan.

The implementation plan must include:

1. Existing scripts/classes related to tower definitions or configs
2. Whether each required data structure already exists or must be created
3. Files expected to be modified or created
4. Any naming conflicts or duplicate concepts found
5. How Odin Inspector will or will not be used
6. How the implementation will avoid adding runtime combat logic
7. How the result will be verified in Unity

Do not implement until the plan is reviewed and approved.

---

## 11. Notes

This task prepares the data layer for later tower combat implementation.

The main purpose is to create a clean, configurable foundation.

Later tasks will consume these definitions to implement:

- Runtime tower combat
- Projectile behavior
- Area damage effects
- Buff behavior
- Tower upgrades