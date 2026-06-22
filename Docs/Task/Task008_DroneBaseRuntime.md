# Task008 - Drone Base Runtime

---

# 1. Source Of Truth

Primary system documents:

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

Depends on:

- `Docs/Task/Task005_TowerRuntimeCombatAttackEntityProtocol.md`
- `Docs/Task/Task006_ArcherAndCannonBaseRuntimeCompletion.md`
- `Docs/Task/Task007_MagicOrbBaseRuntime.md`

---

# 2. Goal

Implement Drone Tower base runtime using an autonomous Drone Attack Entity.

Drone itself is an Attack Entity, not a Projectile Attack Entity.

Drone may spawn Projectile Attack Entities such as bullets or missiles.

---

# 3. Implementation Scope

## 3.1 DroneBehaviour

Add a dedicated runtime component for Drone behavior.

Expected ownership:

- Resting on or near the tower while inactive.
- Using AttackOrigin as rest, launch, return, and recharge target when available.
- Launch movement.
- Local movement speed configuration.
- Target selection or target consumption from `TowerCombatBehaviour`.
- Movement near the target.
- Hovering while facing the target.
- Relocation to maintain hover distance.
- Battery consumption.
- Return to tower.
- Recharge timing.
- Projectile firing while in valid hover attack state.
- Spawning Drone-fired projectiles from Drone FireAnchor when available.
- Playing Drone-fired projectile release VFX from Drone FireAnchor when configured.

Drone behavior should consume runtime initialization context from `TowerCombatBehaviour` and `AttackConfig`.

For Drone Tower, AttackOrigin acts as the Drone parking, launch, return, and recharge anchor in the first version.

DroneBehaviour should own `moveSpeed` in the first version.

Drone Tower should reuse `AttackConfig.AttackRange` as the tower detect and launch range in the first version.

FireAnchor should come from the Drone prefab or `DroneBehaviour`, because it moves with the Drone.

Do not store FireAnchor in `AttackConfig`.

## 3.2 TowerCombatBehaviour Integration

Drone Tower integration:

- `TowerCombatBehaviour` reads `AttackArchetype.Drone`.
- It spawns or owns the active Drone.
- It supplies MonsterManager, source TowerInstance, AttackConfig, AttackOrigin, and target context as needed.
- It supplies AttackOrigin or a fallback rest transform when available.
- It remains a coordinator and should not own detailed Drone movement or battery behavior.

## 3.3 Drone Runtime Loop

Expected base loop:

```text
Drone rests on tower
    ↓
Enemy detected
    ↓
Launch
    ↓
Move near target
    ↓
Hover and face target
    ↓
Fire projectile attack entities at attack interval
    ↓
Consume battery while active
    ↓
Return when battery is depleted or no monsters remain
    ↓
Recharge
    ↓
Launch again if monsters exist
```

## 3.4 Drone-Fired Projectiles

Drone-fired bullets should use Projectile System.

Rules:

- Drone itself does not use `ProjectileBehaviour`.
- Drone-fired bullet/projectile may use `ProjectileBehaviour`.
- Drone-fired projectile spawn position should use Drone FireAnchor when available.
- Drone-fired projectile release VFX should use Drone FireAnchor when configured.
- `AttackConfig.ProjectileReleaseVfxPrefab` may be reused for Drone-fired projectile release VFX.
- Direction projectile behavior is acceptable for the base version.
- Tracking projectile behavior may be used only if Task004 implemented it sufficiently.

---

# 4. Out Of Scope

Do not implement:

- Dual Drones upgrade.
- Missile Attack upgrade unless used as a simple placeholder projectile and explicitly scoped.
- Drone upgrade effects.
- Tower upgrade structure.
- Draft changes.
- PlayerSystem changes.
- Object pooling.
- Final VFX authoring or polish.

---

# 5. Acceptance Criteria

- Drone Tower spawns or owns a Drone Attack Entity.
- Drone rests at AttackOrigin when inactive, with a documented fallback if AttackOrigin is missing.
- Drone launches when enemies enter range.
- Drone moves near a target and hovers while facing it.
- Drone movement speed is configured on DroneBehaviour.
- Drone relocates when target movement breaks hover distance.
- Drone fires projectile attack entities from Drone FireAnchor while hovering.
- Drone-fired projectile release VFX plays from Drone FireAnchor when configured.
- Drone battery drains while active.
- Drone returns to AttackOrigin when battery is depleted or no enemies remain.
- Drone recharges and can launch again.
- Drone itself does not use Projectile System lifecycle.
- Drone-fired projectiles use Projectile System lifecycle.
- AttackOrigin and FireAnchor are prefab/runtime references, not AttackConfig fields.
- No Drone upgrade behavior is implemented.

---

# 6. Suggested Validation

Run targeted source checks:

```text
rg -n "Drone|DroneBehaviour|ProjectileBehaviour|DirectionProjectile|Tracking" Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
rg -n "Dual Drones|Missile Attack|Upgrade" Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
git diff --check -- Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
```

If Unity play-mode validation is available:

- Place Drone Tower and verify rest, launch, hover, fire, return, recharge, launch again.
- Confirm Drone-fired projectile uses ProjectileBehaviour.
- Confirm Drone object itself does not use ProjectileBehaviour unless explicitly documented as a temporary implementation compromise.
