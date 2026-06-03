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
Support the following first-version runtime combat foundation:
- Runtime tower combat state
- Enemy detection and target selection
- Cooldown and attack state management
- Attack execution for all first-version AttackArchetypes
- Projectile creation and initialization for projectile-based attacks
- Direct damage dispatch for non-projectile archetypes
- Animator-driven attack presentation based on AttackConfig fields
- Placeholder runtime hooks for future attack visual effects

## 4. Core Architecture Rules
- **Tower Runtime Combat owns projectile creation.**
- **Tower animation can drive the exact attack release timing through Animation Events.**
- **Projectile System owns projectile lifecycle.**
- **Buff And Effect System owns AreaDamageEffect execution.**
- **Monster System owns health and death.**

## 5. Required Runtime Component
Implement a recommended runtime component: **TowerCombatBehaviour**

Responsibilities:
- Maintain tower combat runtime state
- Detect enemies in attack range
- Select targets according to AttackConfig
- Manage cooldown and attack state transitions
- Execute the configured AttackArchetype
- Coordinate with Projectile System, Buff And Effect System, and Monster System
- Drive attack animation parameters using AttackConfig
- Reserve extension points for future attack visual effects

## 6. Runtime Combat State
Maintain the following runtime state:
- `CurrentTarget`
- `DetectedEnemies`
- `CooldownTimer`
- `AttackState`
- `IsAttacking`
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

TowerCombatBehaviour must support the first-version AttackArchetypes defined by the Tower Framework System:

- StraightProjectile
- ArcProjectile
- ChannelBeam
- PeriodicArea

Detailed execution rules, animator parameter behavior, damage dispatch ownership, and visual-effect hook expectations should follow:

- `07_TowerFrameworkSystem.md`
- `08_TowerRuntimeCombatSystem.md`
- `09_ProjectileSystem.md`
- `11_BuffAndEffectSystem.md`

Task005 should not duplicate full system-level rules. Codex must inspect the related system documents and propose the concrete implementation plan before coding.

## 10. Projectile Creation

- Tower Runtime Combat creates and initializes `ProjectileBehaviour` instances for projectile-based attacks.
- Concrete initialization details should follow the current Projectile System implementation from Task003.
- Animator-driven projectile release timing should follow `08_TowerRuntimeCombatSystem.md`.

## 11. Direct Damage Dispatch

- `ChannelBeam` and `PeriodicArea` dispatch damage without spawning projectiles.
- Damage dispatch ownership must follow `08_TowerRuntimeCombatSystem.md`.
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
- Beam visual implementation
- Area field visual implementation
- Final attack VFX assets and polish

## 14. Expected Output
Deliverables:
- Tower runtime combat foundation with:
  - Enemy detection
  - Target selection
  - Cooldown and attack state management
  - First-version AttackArchetype execution
  - Projectile creation and initialization coordination
  - Direct damage dispatch for non-projectile archetypes
  - AttackConfig-driven animator parameter support
  - Placeholder runtime hooks for future attack visual effects

## 15. Verification Checklist
- [ ] Enemy detection works within AttackConfig attack range
- [ ] Target selection works for supported TargetSelectionTypes
- [ ] Cooldown and attack state transitions work
- [ ] Projectile-based attacks create and initialize ProjectileBehaviour correctly
- [ ] ChannelBeam damage tick works according to AttackConfig
- [ ] PeriodicArea damage tick works according to AttackConfig
- [ ] Animator parameters are driven from AttackConfig when Animator is configured
- [ ] Logic-only fallback works when no Animator is assigned
- [ ] Task005 does not implement tower upgrades, advanced buffs, projectile movement, final VFX, or monster death logic

## 16. Implementation Plan Requirement
Before implementation, **Codex must inspect the project and provide a plan**:
- List existing related scripts
- Specify files to modify/create
- Identify risks
- Define verification strategy

## 17. Notes
- This task consumes outputs from Task001, Task003, and Task004.
- `08_TowerRuntimeCombatSystem.md` is the primary source of truth for runtime combat architecture.
- This task document defines scope, not full implementation details.
- Codex must use the related system documents as the reference when preparing the implementation plan.