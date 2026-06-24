# Task011 - Drone Burst Fire Runtime

---

# 1. Source Of Truth

Primary system documents:

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

Depends on:

- `Docs/Task/Task009_DamageFrameworkRefactor.md`
- `Docs/Task/Task010_DroneOrbitRuntimeRefactor.md`

---

# 2. Goal

Refactor Drone projectile firing from single-shot attackInterval timing to Drone-specific burst fire timing.

Drone should not use `AttackConfig.attackInterval`.

Drone projectile fire timing should use:

- `droneBurstCount`
- `droneBurstInterval`
- `droneBurstCooldown`

This implementation should remain Drone-specific for the first version.

---

# 3. Implementation Scope

## 3.1 AttackConfig Drone Burst Fields

Add Drone burst fire configuration.

Expected fields:

| Field | Type | Description |
|---|---|---|
| droneBurstCount | int | Number of projectiles fired in one burst |
| droneBurstInterval | float | Time between projectiles within one burst |
| droneBurstCooldown | float | Cooldown between bursts |

Validation direction:

- `droneBurstCount` must be greater than zero for Drone.
- `droneBurstInterval` must not be negative.
- `droneBurstCooldown` must not be negative.

## 3.2 DroneBehaviour Burst Runtime

Implement fixed Drone-specific burst timing inside `DroneBehaviour`.

Expected behavior:

```text
Ready to fire
    ↓
Start burst
    ↓
Fire one projectile
    ↓
Wait droneBurstInterval
    ↓
Repeat until droneBurstCount projectiles fired
    ↓
Wait droneBurstCooldown
    ↓
Start next burst if still active and target is valid
```

Rules:

- Burst state belongs to DroneBehaviour.
- Burst timing only runs while Drone is active and has a valid target.
- Burst timing should reset when Drone launches, returns, recharges, or retargets if needed.
- Drone should stop firing when battery is depleted.
- Drone should stop firing when no valid target remains.

## 3.3 Drone-Fired Projectile Spawn

Drone-fired projectiles continue to use Projectile System.

Rules:

- Projectile data comes from `AttackConfig.droneProjectileConfig`.
- Projectile spawn anchor uses Drone FireAnchor when available.
- Projectile release VFX uses Drone FireAnchor when configured.
- Projectile flight and hit detection remain owned by Projectile System.
- Damage should use the calculated damage direction from Task009.

## 3.4 Attack Interval Removal From Drone

Remove Drone runtime dependency on `AttackConfig.attackInterval`.

`attackInterval` remains valid for:

- Direction Projectile attacks.
- Arc Projectile attacks.
- Tracking Projectile attacks when implemented.
- Magic Orb respawn cooldown after the active orb ends.

`attackInterval` must not control Drone projectile fire timing.

## 3.5 Config Asset Migration

Update Drone Tower AttackConfig asset values to include burst fire fields.

Preserve asset GUIDs and `.meta` files.

---

# 4. Out Of Scope

Do not implement:

- Generic Burst Projectile framework.
- Burst behavior for Archer or Cannon.
- Dual Drones upgrade.
- Missile Attack upgrade.
- Tower Upgrade runtime.
- Draft changes.
- Projectile Tracking runtime.
- Object pooling.
- Final VFX authoring or polish.

---

# 5. Acceptance Criteria

- `AttackConfig` exposes Drone burst fields.
- DroneBehaviour fires projectiles in bursts.
- Drone burst count controls projectiles per burst.
- Drone burst interval controls time between projectiles within a burst.
- Drone burst cooldown controls time between bursts.
- Drone no longer reads `AttackConfig.attackInterval` for projectile fire timing.
- Drone stops burst firing when target becomes invalid or battery is depleted.
- Drone-fired projectiles still use Projectile System lifecycle.
- Drone-fired projectiles spawn from FireAnchor when available.
- Drone-fired projectile release VFX plays from FireAnchor when configured.
- No generic burst projectile framework is introduced.

---

# 6. Suggested Validation

Run targeted source checks:

```text
rg -n "AttackInterval|attackInterval|droneBurstCount|droneBurstInterval|droneBurstCooldown|FireProjectile" Assets/Scripts/TowerRuntimeCombat Assets/Scripts/TowerFramework Assets/Configs
rg -n "Burst|burst" Assets/Scripts
git diff --check -- Assets/Scripts/TowerRuntimeCombat Assets/Scripts/TowerFramework Assets/Configs
```

If Unity play-mode validation is available:

- Place Drone Tower and verify burst count.
- Verify interval between projectiles inside one burst.
- Verify cooldown between bursts.
- Verify Drone stops firing during return and recharge.
- Verify Direction/Arc/Magic Orb timing still uses their intended cooldown rules.
