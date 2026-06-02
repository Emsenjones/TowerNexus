

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
- Projectile creation and initialization
- Direct damage dispatch for non-projectile archetypes

## 4. Core Architecture Rules
- **Tower Runtime Combat owns projectile creation.**
- **Projectile System owns projectile lifecycle.**
- **Buff And Effect System owns AreaDamageEffect execution.**
- **Monster System owns health and death.**

## 5. Required Runtime Component
Implement a recommended runtime component: **TowerCombatBehaviour**

Responsibilities:
- Detect enemies
- Select targets
- Manage cooldowns
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
### StraightProjectile
- On cooldown expiry and valid target, create and launch a straight projectile.

### ArcProjectile
- On cooldown expiry and valid target, create and launch an arc projectile.

### ChannelBeam
- On valid target, begin channel.
- Apply damage every `channelDamageInterval` seconds.
- End channel on target exit or interruption.

### PeriodicArea
- On cooldown expiry, apply area damage to all detected enemies every `attackInterval`.
- Does not require target selection.

## 10. Projectile Creation
- Tower Runtime Combat creates and initializes `ProjectileBehaviour` instances.
- Conceptual flow:
  - `TowerCombatBehaviour` → `ProjectileConfig` → `ProjectileBehaviour.Initialize(...)`

## 11. Direct Damage Dispatch
- `ChannelBeam` and `PeriodicArea` archetypes dispatch damage directly to enemies without projectiles.
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

## 14. Expected Output
Deliverables:
- Tower runtime combat foundation with:
  - Enemy detection
  - Target selection
  - Cooldown and attack state management
  - Projectile creation and initialization
  - Direct damage dispatch for channel and area archetypes

## 15. Verification Checklist
- [ ] Enemy detection within range
- [ ] Target selection per selection type
- [ ] Cooldown and attack state transitions
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