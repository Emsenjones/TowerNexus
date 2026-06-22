# Task007 - Magic Orb Base Runtime

---

# 1. Source Of Truth

Primary system documents:

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`

Related system documents:

- `Docs/11_BuffAndEffectSystem.md`

Depends on:

- `Docs/Task/Task005_TowerRuntimeCombatAttackEntityProtocol.md`
- `Docs/Task/Task006_ArcherAndCannonBaseRuntimeCompletion.md`

---

# 2. Goal

Implement Magic Tower base runtime using an orbiting Magic Orb Attack Entity.

Magic Orb is an Attack Entity, not a Projectile Attack Entity by default.

---

# 3. Implementation Scope

## 3.1 MagicOrbBehaviour

Add a dedicated runtime component for Magic Orb behavior.

Expected ownership:

- Orbit around the source tower.
- Contact distance detection against valid monsters.
- Damage dispatch on successful contact.
- Maximum hit count.
- Same-target hit cooldown tracking.
- Local orbit radius configuration.
- Despawn or deactivate when hit count reaches zero.

Magic Orb behavior should consume runtime initialization context from `TowerCombatBehaviour` and `AttackConfig`.

MagicOrbBehaviour should own `orbitRadius` in the first version.

`AttackConfig.sameTargetHitCooldown` should define how soon the same Magic Orb may hit the same monster again.

## 3.2 TowerCombatBehaviour Integration

Magic Tower integration:

- `TowerCombatBehaviour` reads `AttackArchetype.MagicOrb`.
- It spawns or owns the active Magic Orb.
- It starts cooldown after the active Magic Orb ends.
- It respawns a new Magic Orb after cooldown if battle conditions still allow it.

Keep `TowerCombatBehaviour` as coordinator only.

Do not put orbit/contact/hit-count logic directly into `TowerCombatBehaviour`.

## 3.3 Runtime Rules

Magic Orb rules:

- The tower owns one active Magic Orb in the base version.
- The orb rotates around the tower.
- The orb checks distance to monsters while orbiting.
- Contact deals damage.
- Hit count decreases after each successful hit.
- The same monster cannot be hit again by the same orb until sameTargetHitCooldown has elapsed.
- When hit count reaches zero, the orb disappears.
- After cooldown, the tower generates a new orb.

## 3.4 Presentation

Magic Orb visual references may come from `AttackConfig.MagicOrbPrefab`.

Presentation rules:

- Magic Orb VFX is presentation-only.
- VFX must not own damage, target search, orbit hit rules, or hit validation.

---

# 4. Out Of Scope

Do not implement:

- Magic Orb upgrades.
- Additional Orb upgrade.
- Unlimited Hits upgrade.
- Consecutive Hit Bonus upgrade.
- Drone runtime.
- Projectile Tracking runtime for Drone.
- Draft changes.
- PlayerSystem changes.
- Object pooling.

---

# 5. Acceptance Criteria

- Magic Tower spawns or owns one active Magic Orb.
- Magic Orb orbits around the tower.
- Magic Orb orbit radius is configured on MagicOrbBehaviour.
- Magic Orb applies contact damage to valid monsters.
- Magic Orb consumes hit count on successful hits.
- Magic Orb prevents repeated hits against the same monster until sameTargetHitCooldown has elapsed.
- Magic Orb ends when hit count reaches zero.
- Magic Tower cooldown/respawn loop works after orb end.
- Magic Orb does not use `ProjectileBehaviour` or Projectile System lifecycle.
- No Magic Orb upgrade behavior is implemented.

---

# 6. Suggested Validation

Run targeted source checks:

```text
rg -n "MagicOrb|MagicOrbBehaviour|ProjectileBehaviour" Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
rg -n "Additional Orb|Unlimited Hits|Consecutive" Assets/Scripts
git diff --check -- Assets/Scripts/TowerRuntimeCombat Assets/Scripts/TowerFramework
```

If Unity play-mode validation is available:

- Place Magic Tower and verify orb spawn.
- Verify orb orbit and contact damage.
- Verify hit count depletion and respawn after cooldown.
- Confirm no ProjectileBehaviour is attached to the Magic Orb unless intentionally documented.
