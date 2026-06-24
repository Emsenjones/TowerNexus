# Task009 - Damage Framework Refactor

---

# 1. Source Of Truth

Primary system documents:

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`
- `Docs/10_TowerUpgradeSystem.md`

Depends on:

- `Docs/Task/Task004_ProjectileFlightFoundation.md`
- `Docs/Task/Task006_ArcherAndCannonBaseRuntimeCompletion.md`
- `Docs/Task/Task007_MagicOrbBaseRuntime.md`
- `Docs/Task/Task008_DroneBaseRuntime.md`

---

# 2. Goal

Refactor first-version damage data and runtime damage dispatch so tower level growth and attack-behavior damage multipliers are separate.

First-version formula:

```text
FinalDamage = RoundToInt(TowerLevelConfig.basicDamage * RuntimeDamageMultiplier)
```

Tower level data owns `basicDamage`.

Attack configuration owns the default attack-behavior `damageMultiplier`.

Future Tower Upgrade runtime may add instance-specific multiplier modifiers without overwriting level-based `basicDamage`.

---

# 3. Implementation Scope

## 3.1 TowerLevelConfig Basic Damage

Update `TowerLevelConfig` so per-level tower base damage is represented directly.

Expected direction:

- Add `basicDamage`.
- Use `int` for first-version damage values.
- Keep existing non-damage fields unless this task requires a local rename.
- Do not introduce full Tower Upgrade runtime application in this task.

If replacing an existing serialized field, preserve Unity serialized data where practical.

## 3.2 AttackConfig Damage Multiplier

Update `AttackConfig` damage configuration.

Expected direction:

- Replace single `damage` usage with `damageMultiplier`.
- Keep `damageMultiplier` as the default attack-behavior multiplier.
- Preserve Unity serialized data where practical with `FormerlySerializedAs`.
- Keep `attackInterval` for projectile-style attacks and Magic Orb respawn cooldown.
- Do not use `attackInterval` as Drone projectile fire timing in later Drone tasks.

## 3.3 Runtime Damage Calculation

Add a small, explicit damage calculation path.

Expected behavior:

- Resolve the source tower's current level `basicDamage`.
- Resolve the active runtime damage multiplier.
- Calculate final damage with `Mathf.RoundToInt`.
- Clamp or validate negative values so runtime damage cannot become negative.

Acceptable first-version implementation:

- A local helper on `AttackConfig`, `TowerCombatBehaviour`, or a small shared utility if the existing code shape justifies it.
- A direct helper that accepts `basicDamage` and multiplier.

Do not add a broad stat system, modifier stack, or upgrade pipeline in this task.

## 3.4 Projectile Damage Dispatch

Update projectile-style damage paths to use calculated final damage.

Affected behavior:

- Direction Projectile direct hit damage.
- Arc Projectile impact context damage.
- Drone-fired projectile damage, if it currently uses the same projectile damage path.

Projectile System should use calculated damage context from Tower Runtime Combat or the spawning Attack Entity. Projectile System should not own the formula.

## 3.5 Magic Orb Damage Dispatch

Update Magic Orb contact damage to use calculated final damage.

Magic Orb remains an Attack Entity and does not become a Projectile Attack Entity.

## 3.6 Config Asset Migration

Update first-version config assets to match the new schema.

Expected asset updates:

- Tower level config entries provide `basicDamage`.
- AttackConfig assets provide `damageMultiplier`.
- Existing Archer, Cannon, Magic, and Drone first-version damage output should remain intentionally balanced after migration.

Preserve asset GUIDs and `.meta` files.

---

# 4. Out Of Scope

Do not implement:

- Tower Upgrade runtime modifier application.
- Draft changes.
- PlayerSystem changes.
- Drone orbit runtime.
- Drone burst fire runtime.
- New damage types.
- Armor, resistance, critical hits, or elemental damage.
- A generic stat/modifier framework.
- Object pooling.

---

# 5. Acceptance Criteria

- `TowerLevelConfig` exposes per-level `basicDamage`.
- `AttackConfig` exposes default `damageMultiplier`.
- Runtime damage uses `RoundToInt(TowerLevelConfig.basicDamage * RuntimeDamageMultiplier)`.
- Projectile direct hit damage uses calculated final damage.
- Projectile impact context uses calculated final damage.
- Magic Orb contact damage uses calculated final damage.
- Drone-fired projectiles use the same calculated damage direction when they spawn projectiles.
- Projectile System does not own the damage formula.
- Tower Upgrade System remains out of implementation scope except for data compatibility.
- Existing config assets are migrated without GUID churn.

---

# 6. Suggested Validation

Run targeted source checks:

```text
rg -n "Damage|damage|basicDamage|damageMultiplier|baseDamageModifier" Assets/Scripts Assets/Configs
rg -n "TakeDamage|ProjectileImpactContext" Assets/Scripts/Projectile Assets/Scripts/TowerRuntimeCombat
git diff --check -- Assets/Scripts Assets/Configs
```

If Unity play-mode validation is available:

- Place Archer Tower and verify direct projectile damage.
- Place Cannon Tower and verify impact damage context / area damage behavior.
- Place Magic Tower and verify Magic Orb contact damage.
- Place Drone Tower and verify Drone-fired projectile damage still resolves through Projectile System.
