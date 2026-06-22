# Task005 - Tower Runtime Combat Attack Entity Protocol

---

# 1. Source Of Truth

Primary system document:

- `Docs/08_TowerRuntimeCombatSystem.md`

Related system documents:

- `Docs/07_TowerFrameworkSystem.md`
- `Docs/09_ProjectileSystem.md`

Depends on:

- `Docs/Task/Task004_ProjectileFlightFoundation.md`

---

# 2. Goal

Refactor `TowerCombatBehaviour` into a tower-side coordinator that can spawn or control the required Attack Entity types without implementing the full Magic Orb or Drone runtime yet.

This task introduces only the minimal spawn/control protocol needed by the next tasks.

---

# 3. Hard Constraints

- Do not over-engineer a full generic Attack Entity framework.
- Do not introduce a broad inheritance hierarchy before Magic Orb and Drone requirements are implemented.
- Do not implement Magic Orb orbit/contact runtime in this task.
- Do not implement Drone launch/hover/return/recharge runtime in this task.
- Keep the protocol small and shaped by the immediate needs of Tasks 006-008.

The purpose is to prepare `TowerCombatBehaviour` for Attack Entity ownership without turning it into either:

- a giant switch containing all attack behavior, or
- an abstract framework larger than the current requirements.

---

# 4. Current Design Direction

Every attack-capable tower prefab should use one `TowerCombatBehaviour`.

`TowerCombatBehaviour` owns shared tower-side orchestration:

- Runtime reference validation.
- Enemy detection.
- Target selection.
- Cooldown timing.
- Attack origin.
- Animator trigger / bool control.
- Projectile release timing for projectile attacks.
- Minimal Attack Entity spawn/control hooks.

Attack-specific behavior belongs to Attack Entity runtime components:

- `ProjectileBehaviour`
- future `MagicOrbBehaviour`
- future `DroneBehaviour`

---

# 5. Implementation Scope

## 5.1 TowerCombatBehaviour Shared Flow

Keep or clarify shared flow:

```text
Validate runtime references
    ↓
Update cooldown timer
    ↓
Detect enemies within attackRange
    ↓
Read AttackConfig.AttackArchetype
    ↓
Spawn or control the required Attack Entity
```

## 5.2 Minimal Attack Entity Protocol

Introduce only the minimum protocol needed for the next tasks.

Acceptable examples:

- A private spawn method per AttackArchetype.
- A tiny interface only if it immediately removes real duplication.
- A small shared initialization context only if Magic Orb and Drone both need it.

Avoid:

- A full `IAttackEntity` framework with unused lifecycle methods.
- A base class hierarchy for every future attack type.
- Generic pooling, ownership registries, or event buses.
- Solving upgrade behavior in this task.

## 5.3 Projectile Attack Hook

Keep Archer and Cannon projectile release path working:

- Select target.
- Store pending target and target position.
- Wait for animation release if configured.
- Spawn projectile.
- Initialize `ProjectileBehaviour`.
- Start cooldown immediately after attack release/start as currently designed.

## 5.4 Magic Orb Hook

Prepare a minimal branch for Magic Orb:

- Enough for Task007 to spawn/own `MagicOrbBehaviour`.
- Current behavior may remain an explicit unsupported warning until Task007.

Do not implement orbit, contact detection, hit count, or respawn loop here.

## 5.5 Drone Hook

Prepare a minimal branch for Drone:

- Enough for Task008 to spawn/own `DroneBehaviour`.
- Current behavior may remain an explicit unsupported warning until Task008.

Do not implement launch, movement, hover, battery, return, recharge, or projectile firing here.

---

# 6. Out Of Scope

Do not implement:

- Magic Orb runtime behavior.
- Drone runtime behavior.
- Tracking projectile use by Drone.
- Tower upgrades.
- Draft changes.
- PlayerSystem changes.
- Object pooling.
- Final VFX authoring or polish.

---

# 7. Acceptance Criteria

- `TowerCombatBehaviour` remains the single tower-side combat coordinator.
- Shared detection, targeting, cooldown, attack origin, and animation logic remain centralized.
- Archer and Cannon projectile attack path still works.
- MagicOrb and Drone branches are prepared with minimal hooks or clear unsupported placeholders.
- No full generic Attack Entity framework is introduced.
- No Magic Orb or Drone behavior is implemented beyond the minimal hook needed for later tasks.
- No tower upgrade or Draft behavior is introduced.

---

# 8. Suggested Validation

Run targeted source checks:

```text
rg -n "AttackArchetype|DirectionProjectile|ArcProjectile|MagicOrb|Drone" Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
rg -n "interface IAttack|abstract class.*Attack|BaseAttackEntity|AttackEntityManager" Assets/Scripts
git diff --check -- Assets/Scripts/TowerRuntimeCombat Assets/Scripts/Projectile
```

If Unity play-mode validation is available:

- Confirm Archer and Cannon still attack.
- Confirm Magic and Drone do not silently run old ChannelBeam or PeriodicArea behavior.
