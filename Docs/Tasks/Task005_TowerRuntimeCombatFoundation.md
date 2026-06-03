

# Task 005: Tower Runtime Combat Foundation

## 1. Task Overview
- Implement the first-version Tower Runtime Combat foundation.
- Consume `TowerDefinition` and `AttackConfig`.
- Do **not** implement tower upgrades.
- Do **not** implement advanced buffs.

## 2. Related System Documents
- `00_ProjectOverview.md`
- `07_TowerFrameworkSystem.md`
- `08_TowerRuntimeCombatSystem.md` (Primary source of truth)
- `09_ProjectileSystem.md`
- `11_BuffAndEffectSystem.md`

Refer to `08_TowerRuntimeCombatSystem.md` for detailed requirements and architecture.

## 3. Implementation Goal
Support the following:
- Runtime tower combat state
- Enemy detection
- Target selection
- Cooldown management
- Attack execution
- Animator-driven attack release timing
- Projectile creation and initialization
- Direct damage dispatch for non-projectile archetypes

## 4. Core Architecture Rules
- **Tower Runtime Combat owns projectile creation.**
- **Tower animation can drive the exact attack release timing through Animation Events.**
- **Projectile System owns projectile lifecycle.**
- **Buff And Effect System owns AreaDamageEffect execution.**
- **Monster System owns health and death.**

## 5. Required Runtime Component
Implement a recommended runtime component: **TowerCombatBehaviour**

Responsibilities:
- Detect enemies
- Select targets
- Manage cooldowns
- Trigger tower attack animations
- Receive animation event callbacks for attack release
- Execute attack archetypes

## 6. Runtime Combat State
Maintain the following runtime state:
- `CurrentTarget`
- `DetectedEnemies`
- `CooldownTimer`
- `AttackState`
- `CurrentChannelTarget`
- `ChannelTimer`

## 7. Enemy Detection
- Use `attackRange` from `AttackConfig` to detect enemies within range.
- Maintain a runtime collection of detected enemies.

## 8. Target Selection
Support the following selection types:
- **Nearest**
- **HighestHealth**
- **LowestHealth**
- **Random**

Note: `PeriodicArea` archetype does **not** use `TargetSelectionType`.

## 9. Attack Execution

### Animator-Driven Attack Flow
- Tower attacks should be animation-driven when an Animator is configured.
- Each attack-capable tower can have an `Animator` component.
- The Animator should use a Trigger parameter named `Attack`.
- When `TowerCombatBehaviour` decides an attack can start, it should call `animator.SetTrigger("Attack")` instead of immediately releasing the attack payload.
- The attack payload release should happen through an Animation Event on the tower attack animation clip.
- The Animation Event should call a method on `TowerCombatBehaviour`, such as `OnAttackAnimationRelease()`.
- `OnAttackAnimationRelease()` should execute the already-selected attack payload, such as projectile creation, beam damage tick, or periodic area damage, depending on the attack archetype.
- If no Animator is configured, `TowerCombatBehaviour` may fall back to immediate attack execution for prototype safety.
- Cooldown and attack state logic should still be owned by `TowerCombatBehaviour`; the animation only controls the visual timing of the release moment.

### StraightProjectile
- On cooldown expiry and valid target, trigger the tower attack animation.
- On the attack animation release event, create and launch a straight projectile.

### ArcProjectile
- On cooldown expiry and valid target, trigger the tower attack animation.
- On the attack animation release event, create and launch an arc projectile.

### ChannelBeam
- On valid target, begin channel.
- Channel start may trigger the tower attack animation.
- Damage ticks may be executed by timer logic or by animation events, depending on the final animation setup.
- Apply damage every `channelDamageInterval` seconds.
- End channel on target exit or interruption.

### PeriodicArea
- On cooldown expiry, trigger the tower attack animation.
- On the attack animation release event, apply area damage to all detected enemies.
- Repeat this flow every `attackInterval`.
- Does not require target selection.

## 10. Projectile Creation
- Tower Runtime Combat creates and initializes `ProjectileBehaviour` instances.
- Conceptual flow:
  - `TowerCombatBehaviour` decides attack start → `Animator.SetTrigger("Attack")` → Animation Event → `TowerCombatBehaviour.OnAttackAnimationRelease()` → `ProjectileConfig` → `ProjectileBehaviour.Initialize(...)`

## 11. Direct Damage Dispatch
- `ChannelBeam` and `PeriodicArea` archetypes dispatch damage directly to enemies without projectiles.
- Direct damage dispatch should also respect animator-driven release timing when an Animator is configured.
- Monster health and death remain owned by the Monster System.

## 12. Relationship With Other Systems
### Tower Framework System
- Provides tower configuration and runtime context.

### Projectile System
- Manages projectile lifecycle after creation.

### Buff And Effect System
- Responsible for executing effects like `AreaDamageEffect`.

### Monster System
- Owns monster health, damage application, and death.

## 13. Constraints
Do **not** implement the following in this task:
- Tower upgrades
- Buff runtime framework
- Projectile movement
- AreaDamageEffect execution
- Monster death visuals
- Detailed tower animation clip creation
- Advanced animation state machine design

## 14. Expected Output
Deliverables:
- Tower runtime combat foundation with:
  - Enemy detection
  - Target selection
  - Cooldown and attack state management
  - Projectile creation and initialization
  - Animator trigger support for attack animation
  - Animation Event callback support for attack payload release
  - Direct damage dispatch for channel and area archetypes

## 15. Verification Checklist
- [ ] Enemy detection within range
- [ ] Target selection per selection type
- [ ] Cooldown and attack state transitions
- [ ] Animator `Attack` trigger is called when a tower attack starts
- [ ] Animation Event callback releases the attack payload
- [ ] Immediate attack fallback works when no Animator is assigned
- [ ] Projectile creation on attack
- [ ] ChannelBeam damage applied at `channelDamageInterval`
- [ ] PeriodicArea damage applied at `attackInterval`

## 16. Implementation Plan Requirement
Before implementation, **Codex must inspect the project and provide a plan**:
- List existing related scripts
- Specify files to modify/create
- Identify risks
- Define verification strategy

## 17. Notes
- This task consumes outputs from Task001, Task003, and Task004.
- Tower Runtime Combat serves as the orchestration layer between tower data, projectile runtime, effects, and monster damage.
- Tower attack visuals should be driven by tower animation where possible. Runtime combat decides when an attack starts, while Animation Events decide the exact frame when the attack payload is released.