# Task006 - Archer And Cannon Base Runtime Completion

---

# 1. Source Of Truth

Primary system documents:

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

Depends on:

- `Docs/Task/Task004_ProjectileFlightFoundation.md`
- `Docs/Task/Task005_TowerRuntimeCombatAttackEntityProtocol.md`

---

# 2. Goal

Complete and verify the first two base tower runtimes:

- Archer Tower
- Cannon Tower

This task should make the existing projectile-based towers match the current system documents after the Tower Framework refactor.

---

# 3. Implementation Scope

## 3.1 Archer Tower

Archer Tower behavior:

- Detect valid monsters inside attack range.
- Select one target by `TargetSelectionType`.
- Fire one Direction projectile toward the selected target's monster-side hit/reference anchor.
- Start cooldown immediately after the arrow is fired, not after impact.
- Arrow travels independently after launch.
- Arrow continuously performs distance-based hit detection.
- Arrow may hit any valid monster encountered during flight, not only the originally selected target.
- Arrow applies direct damage and destroys itself on hit.
- Arrow destroys itself when max lifetime is reached.

## 3.2 Cannon Tower

Cannon Tower behavior:

- Detect valid monsters inside attack range.
- Select one target by `TargetSelectionType`.
- Snapshot target position from the selected target's monster-side hit/reference anchor.
- Fire one Arc projectile toward the target position.
- Start cooldown immediately after shell launch, not after explosion.
- Shell travels along arc using `AttackConfig.ArcHeight`.
- Shell triggers impact when it reaches the target position.
- Shell impact may trigger `AreaDamageEffect`.
- Explosion/impact VFX follows projectile impact timing.

## 3.3 Damage And Effects

Keep first-version damage ownership:

- Archer direct hit damage may be dispatched by Projectile System.
- Cannon area damage is represented by `AreaDamageEffect`.
- Buffs are not required for Archer or Cannon base behavior.

---

# 4. Out Of Scope

Do not implement:

- Magic Orb runtime.
- Drone runtime.
- Tracking projectile runtime unless already required by Task004.
- Tower upgrades.
- Draft changes.
- PlayerSystem changes.
- Object pooling.
- Final VFX authoring or particle polish.

---

# 5. Acceptance Criteria

- Archer Tower fires Direction projectiles and damages valid monsters.
- Archer cooldown starts immediately after projectile fire.
- Archer projectile can hit any valid monster encountered during flight.
- Cannon Tower fires Arc projectiles toward target position snapshots.
- Cannon cooldown starts immediately after projectile launch.
- Cannon impact triggers area damage through the existing effect path.
- Existing projectile release VFX and impact VFX hooks remain presentation-only.
- No Magic Orb or Drone runtime is implemented in this task.

---

# 6. Suggested Validation

Run targeted source checks:

```text
rg -n "DirectionProjectile|ArcProjectile|AreaDamageEffect|ProjectileImpactContext" Assets/Scripts
rg -n "MagicOrb|Drone" Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
git diff --check -- Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile Assets/Scripts/BuffAndEffect
```

If Unity play-mode validation is available:

- Place Archer Tower and verify arrow firing, hit, damage, and lifetime cleanup.
- Place Cannon Tower and verify shell arc, impact, area damage, and cleanup.
- Confirm cooldown starts on fire/launch rather than impact.
