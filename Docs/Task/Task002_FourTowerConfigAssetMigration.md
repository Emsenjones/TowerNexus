# Task002 - Four Tower Config Asset Migration

---

# 1. Source Of Truth

Primary system document:

- `Docs/07_TowerFrameworkSystem.md`

Depends on:

- `Docs/Task/Task001_TowerFrameworkSchemaRefactor.md`

This task migrates ScriptableObject assets to the new Tower Framework schema. It should happen after the schema fields exist in code.

---

# 2. Goal

Migrate current tower configuration assets so the project has four current first-version towers:

- Archer Tower
- Cannon Tower
- Magic Tower
- Drone Tower

Archer and Cannon keep their functional direction.

Magic is migrated from the previous Mage/ChannelBeam direction to Magic Orb framework data.

Watch is removed from the current first-version lineup and replaced by Drone framework data.

---

# 3. Current Asset Gaps

Current assets still include:

- `Assets/Configs/TowerDefinitionConfig/Config_TowerDefinition_MageTower.asset`
- `Assets/Configs/TowerDefinitionConfig/Config_TowerDefinition_WatchTower.asset`
- `Assets/Configs/AttackConfig/Config_AttackConfig_MageTower.asset`
- `Assets/Configs/AttackConfig/Config_AttackConfig_WatchTower.asset`

Current AttackConfig values still map to the old enum:

- Archer uses old `StraightProjectile`.
- Cannon uses old `ArcProjectile`.
- Mage uses old `ChannelBeam`.
- Watch uses old `PeriodicArea`.

---

# 4. Implementation Scope

## 4.1 TowerDefinition Assets

Update TowerDefinition assets so each current tower has:

- stable `towerId`
- correct `displayName`
- useful `description`
- assigned `towerCategory`
- assigned `towerPrefab`
- assigned `attackConfig`
- optional empty or starter `towerLevelConfigs`

Expected current tower definitions:

```text
Config_TowerDefinition_ArcherTower
Config_TowerDefinition_CannonTower
Config_TowerDefinition_MagicTower
Config_TowerDefinition_DroneTower
```

Asset migration guidance:

- Prefer renaming/migrating Mage to Magic if existing references should be preserved.
- Prefer renaming/migrating Watch to Drone if existing references should be preserved.
- Avoid deleting and recreating assets unless there is no meaningful reference to preserve.

## 4.2 AttackConfig Assets

Update AttackConfig assets so each current tower maps to the new framework schema:

| Tower | AttackArchetype | Notes |
|---|---|---|
| Archer | DirectionProjectile | Keeps arrow projectile config and projectile release VFX if assigned |
| Cannon | ArcProjectile | Keeps shell projectile config, arc height, and projectile release VFX if assigned |
| Magic | MagicOrb | Uses Magic Orb framework fields; old ChannelBeam fields should not be active |
| Drone | Drone | Uses Drone framework fields and may reference projectile config for Drone-fired projectiles |

Expected current attack configs:

```text
Config_AttackConfig_ArcherTower
Config_AttackConfig_CannonTower
Config_AttackConfig_MagicTower
Config_AttackConfig_DroneTower
```

## 4.3 TowerDefinitionDatabase

Update any scene or asset references that feed `TowerDefinitionDatabase`.

Expected behavior:

- Draft pool source contains Archer, Cannon, Magic, and Drone.
- Watch should not appear as a current draftable tower.
- Magic should be named Magic Tower, not Mage Tower, unless a future naming decision explicitly keeps Mage.

## 4.4 Placeholder Prefab References

Phase 1 may use placeholder prefab references when a final Magic Orb or Drone prefab does not exist yet.

Rules:

- Do not author final VFX or final presentation assets in this task.
- Do not build runtime Magic Orb or Drone behavior in this task.
- If required prefab references are unavailable, leave them empty only when `AttackConfig.IsValid()` permits it for Phase 1.
- Record missing prefab references in the task implementation notes.

---

# 5. Out Of Scope

Do not implement:

- Magic Orb orbit runtime.
- Drone movement or battery runtime.
- Drone projectile fire runtime.
- Projectile Tracking runtime.
- Tower upgrades.
- Draft UI changes beyond keeping current tower definitions available.
- Player progression changes.

---

# 6. Acceptance Criteria

- Current tower asset set represents Archer, Cannon, Magic, and Drone.
- Watch Tower is not present as a current first-version draftable tower.
- Mage naming is migrated to Magic naming unless intentionally preserved and documented.
- Archer and Cannon assets keep their existing functional config direction.
- Magic AttackConfig uses Magic Orb framework fields.
- Drone AttackConfig uses Drone framework fields and can point to projectile config for Drone-fired projectile behavior.
- TowerDefinition assets include `towerCategory`.
- TowerDefinitionDatabase source references the current four towers.
- No runtime behavior is implemented in this task.

---

# 7. Suggested Validation

Run targeted searches:

```text
rg -n "WatchTower|Watch Tower|MageTower|Mage Tower|ChannelBeam|PeriodicArea" Assets/Configs Assets/Scripts/TowerFramework
rg -n "DroneTower|Drone Tower|MagicTower|Magic Tower|MagicOrb|DirectionProjectile" Assets/Configs Assets/Scripts/TowerFramework
git diff --check -- Assets/Configs Assets/Scripts/TowerFramework
```

If Unity reports missing serialized fields after schema migration, fix the asset YAML or re-save assets in Unity and document the action.
