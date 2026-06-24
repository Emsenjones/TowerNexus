# Task010 - Drone Orbit Runtime Refactor

---

# 1. Source Of Truth

Primary system documents:

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

Depends on:

- `Docs/Task/Task008_DroneBaseRuntime.md`
- `Docs/Task/Task009_DamageFrameworkRefactor.md`

---

# 2. Goal

Refactor Drone base runtime from target hover behavior to target orbit behavior.

Drone should behave like an autonomous gunship:

```text
Rest
    ↓
Launch
    ↓
Rise to configured flight height
    ↓
Select target
    ↓
Orbit around selected target
    ↓
Return
    ↓
Recharge
```

Drone remains an Attack Entity which may spawn Projectile Attack Entities.

---

# 3. Implementation Scope

## 3.1 AttackConfig Drone Movement Fields

Update Drone movement configuration fields.

Expected schema direction:

- Replace `droneHoverDistance` with `droneOrbitRadius`.
- Replace `droneMoveSpeed` with `droneFlightSpeed`.
- Replace `droneLaunchHeight` with `droneFlightHeight`.
- Preserve Unity serialized data where practical with `FormerlySerializedAs`.

Do not add separate orbit angular speed configuration.

Orbit angular speed should be calculated from:

```text
droneFlightSpeed / droneOrbitRadius
```

## 3.2 Drone Runtime State

Update Drone runtime terminology and behavior from hover to orbit.

Expected direction:

- Replace concept-level `Hovering` with `Orbiting`.
- Keep resting, launching, returning, and recharging behavior.
- Keep Drone `IsFlying` false while resting or recharging.
- Keep Drone `IsFlying` true while launching, orbiting, or returning.

If enum renaming would create avoidable serialized risk, document the compatibility decision in code comments or task notes.

## 3.3 Orbit Movement

Drone orbit behavior:

- Drone rises from AttackOrigin to `droneFlightHeight`.
- Drone selects a valid target inside source tower AttackRange.
- Drone approaches the selected target's orbit path.
- Drone orbits around the selected target using `droneOrbitRadius`.
- Drone maintains configured flight height during active flight.
- Drone model local +Z faces the orbit tangent / flight direction while orbiting.
- When Drone enters Orbiting or retargets, Drone chooses orbit direction based on which tangent direction around the target is closer to the Drone's current local +Z forward direction.
- Orbit direction is runtime state and must not be exposed as an AttackConfig field.
- Drone-fired projectile launch direction and attack release VFX still aim from FireAnchor to the selected monster hit position.
- Drone retargets if the current target becomes invalid or leaves source tower AttackRange.
- Drone returns to AttackOrigin when battery is depleted or no valid targets remain.

Use MonsterBehaviour.HitAnchor when available for target orbit reference.

## 3.4 TowerCombatBehaviour Integration

TowerCombatBehaviour should remain a coordinator.

It may:

- Spawn or own the active Drone.
- Provide MonsterManager, source TowerInstance, AttackConfig, and AttackOrigin.
- Reflect Drone state into tower attack state.

It should not own:

- Drone orbit movement.
- Drone target orbit math.
- Drone battery drain.
- Drone return pathing.

## 3.5 Config Asset Migration

Update Drone Tower AttackConfig asset values to use the new movement fields.

Preserve asset GUIDs and `.meta` files.

---

# 4. Out Of Scope

Do not implement:

- Drone burst fire timing.
- Dual Drones upgrade.
- Missile Attack upgrade.
- Tower Upgrade runtime.
- Draft changes.
- Projectile Tracking runtime.
- New projectile flight behavior.
- Object pooling.
- Final VFX authoring or polish.

Drone may continue using its existing projectile fire behavior until Task011 replaces it with burst fire.

---

# 5. Acceptance Criteria

- Drone movement config uses `droneOrbitRadius`, `droneFlightSpeed`, and `droneFlightHeight`.
- Drone no longer uses hover distance or hover point behavior.
- Drone rises to configured flight height after launch.
- Drone selects targets inside source tower AttackRange.
- Drone orbits around the selected target.
- Drone chooses orbit direction from current forward direction when entering Orbiting or retargeting.
- Drone model local +Z faces the orbit tangent while Orbiting.
- Drone-fired projectile launch direction still targets the selected monster hit position.
- Drone orbit angular speed is derived from flight speed and orbit radius.
- Drone retargets or returns when its target becomes invalid.
- Drone returns to AttackOrigin for recharge when battery is depleted or no valid targets remain.
- AttackOrigin remains the rest, launch, return, and recharge anchor.
- FireAnchor remains on Drone prefab or DroneBehaviour.
- TowerCombatBehaviour remains a coordinator.
- No Drone burst fire implementation is introduced in this task.

---

# 6. Suggested Validation

Run targeted source checks:

```text
rg -n "Hover|hover|DroneHoverDistance|droneHoverDistance|DroneMoveSpeed|droneMoveSpeed|DroneLaunchHeight|droneLaunchHeight" Assets/Scripts Assets/Configs
rg -n "Orbit|orbit|droneOrbitRadius|droneFlightSpeed|droneFlightHeight|IsFlying" Assets/Scripts/TowerRuntimeCombat Assets/Scripts/TowerFramework Assets/Configs
git diff --check -- Assets/Scripts/TowerRuntimeCombat Assets/Scripts/TowerFramework Assets/Configs
```

If Unity play-mode validation is available:

- Place Drone Tower and verify rest pose at AttackOrigin.
- Verify launch and rise to configured flight height.
- Verify target selection inside source tower AttackRange.
- Verify Drone orbits around the selected monster.
- Verify return and recharge behavior.
- Verify Drone does not use hover point behavior.
