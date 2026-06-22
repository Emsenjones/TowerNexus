# Task001 - Tower Framework Schema Refactor

---

# 1. Source Of Truth

Primary system document:

- `Docs/07_TowerFrameworkSystem.md`

Related system documents:

- `Docs/00_ProjectOverview.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`
- `Docs/10_TowerUpgradeSystem.md`

This task implements the Tower Framework schema foundation only. Runtime behavior for Magic Orb, Drone, projectile flight, and upgrades belongs to later tasks.

---

# 2. Goal

Refactor Tower Framework code so the static schema matches the current first-version tower framework:

- Archer Tower
- Cannon Tower
- Magic Tower
- Drone Tower

The framework should define tower identity, tower category, attack archetype, AttackConfig fields, and optional tower level config data without implementing runtime attack behavior.

---

# 3. Current Code Gaps

Current code still reflects the previous tower framework:

- `TowerCategory` contains `Watch` instead of `Drone`.
- `AttackArchetype` contains `StraightProjectile`, `ArcProjectile`, `ChannelBeam`, and `PeriodicArea`.
- `AttackConfig` still owns ChannelBeam and PeriodicArea fields.
- `AttackConfig` does not expose Magic Orb or Drone framework fields.
- `TowerDefinition` does not expose `towerCategory`.
- `TowerDefinition` does not expose `towerLevelConfigs`.
- `TowerLevelConfig` does not exist yet.

---

# 4. Implementation Scope

## 4.1 TowerCategory

Update `Assets/Scripts/TowerFramework/TowerCategory.cs`.

Expected current first-version values:

```text
Archer
Cannon
Magic
Drone
```

Notes:

- Remove `Watch` from the active enum.
- Keep enum ordering stable where practical to reduce Unity asset migration risk.

## 4.2 AttackArchetype

Update `Assets/Scripts/TowerFramework/AttackArchetype.cs`.

Expected current first-version values:

```text
DirectionProjectile
ArcProjectile
MagicOrb
Drone
```

Notes:

- `DirectionProjectile` replaces the old `StraightProjectile` framework meaning.
- `MagicOrb` replaces the old `ChannelBeam` first-version meaning.
- `Drone` replaces the old `PeriodicArea` first-version meaning.
- Preserve enum numeric order where practical so existing serialized values can be migrated predictably.

## 4.3 AttackConfig

Update `Assets/Scripts/TowerFramework/AttackConfig.cs`.

Keep core fields:

- `attackConfigId`
- `attackArchetype`
- `attackRange`
- `attackInterval`
- `targetSelectionType`
- `damage`
- `projectileConfig`
- `arcHeight`
- `attackAnimatorTriggerName`
- `attackingAnimatorBoolName`
- `projectileReleaseVfxPrefab`

Remove or retire active use of old fields:

- `channelDamageInterval`
- `maxChannelDuration`
- `channelBeamVfxPrefab`
- `periodicAreaVfxPrefab`

Add Magic Orb fields:

- `magicOrbRotationSpeed`
- `magicOrbMaxHitCount`
- `magicOrbPrefab`

Add Drone fields:

- `droneBatteryDuration`
- `droneRechargeDuration`
- `droneHoverDistance`
- `dronePrefab`

Update Odin visibility helpers:

- Projectile fields show for `DirectionProjectile`, `ArcProjectile`, and Drone projectile firing support where needed.
- `arcHeight` shows only for `ArcProjectile`.
- Target selection shows for `DirectionProjectile`, `ArcProjectile`, and `Drone`.
- Magic Orb fields show only for `MagicOrb`.
- Drone fields show only for `Drone`.

Update validation:

- Required id must remain non-empty.
- Range and interval must remain non-negative.
- Damage must remain non-negative.
- Projectile config is required for projectile archetypes that directly fire projectiles.
- Magic Orb max hit count must be positive.
- Magic Orb rotation speed must be non-negative.
- Drone battery duration must be positive.
- Drone recharge duration must be non-negative.
- Drone hover distance must be non-negative.

## 4.4 TowerDefinition

Update `Assets/Scripts/TowerFramework/TowerDefinition.cs`.

Add:

- `towerCategory`
- `towerLevelConfigs`

Expected public accessors:

- `TowerCategory TowerCategory`
- `IReadOnlyList<TowerLevelConfig> TowerLevelConfigs`

Validation should include:

- Existing id, prefab, attack config, and anchor validation.
- Optional level config sanity checks if entries exist.

## 4.5 TowerLevelConfig

Add a serializable framework data type under `Assets/Scripts/TowerFramework/`.

Expected fields:

- `level`
- `baseDamageModifier`
- `attackIntervalModifier`
- `rangeModifier`
- `towerModelPrefab`
- `displayIcon`

This type is data only. It must not apply upgrades or mutate tower runtime state.

---

# 5. Out Of Scope

Do not implement:

- Magic Orb orbit runtime.
- Drone launch, hover, return, or recharge runtime.
- Projectile Direction, Arc, or Tracking runtime refactor.
- Tower upgrade definitions.
- Tower upgrade application.
- Draft integration.
- Player progression changes.
- Object pooling.
- Prefab authoring or VFX polish.

---

# 6. Compatibility Notes

This schema refactor will break old enum references in runtime code unless a compatibility pass is included.

Minimum acceptable compatibility:

- Project compiles after enum and field rename work.
- Runtime systems may log clear unsupported-archetype warnings for `MagicOrb` and `Drone` until Phase 2 implements them.
- Existing Archer and Cannon references should be easy to map to `DirectionProjectile` and `ArcProjectile`.

Full runtime behavior is not required in this task.

---

# 7. Acceptance Criteria

- `TowerCategory` reflects Archer, Cannon, Magic, and Drone.
- `AttackArchetype` reflects DirectionProjectile, ArcProjectile, MagicOrb, and Drone.
- `AttackConfig` exposes current first-version schema fields from `Docs/07_TowerFrameworkSystem.md`.
- `AttackConfig` no longer presents ChannelBeam or PeriodicArea as active first-version framework fields.
- `TowerDefinition` exposes tower category and optional tower level config data.
- `TowerLevelConfig` exists as serializable framework data.
- Code compiles or reaches the closest available local validation signal.
- No runtime implementation for Magic Orb, Drone, or upgrades is added in this task.

---

# 8. Suggested Validation

Run available validation after implementation:

```text
rg -n "ChannelBeam|PeriodicArea|StraightProjectile|Watch" Assets/Scripts/TowerFramework Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
git diff --check -- Assets/Scripts/TowerFramework Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
```

If a Unity compile check is available, run it. If not, document why it was not available.
