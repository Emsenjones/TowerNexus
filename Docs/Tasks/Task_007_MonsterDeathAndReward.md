

# Task 007 - Monster Death And Reward

## 1. Overview

This task introduces monster death handling and player reward flow for TowerNexus.

This task is designed based on:

- ProjectOverview.md
- 03_MonsterSystem.md

This document only defines the implementation scope of the current task.

## Related Tasks

This task is closely related to:

- Task_001_MonsterDefinition
- Task_002_MonsterWaveSpawner
- Task_004_MonsterMovement
- Task_005_DynamicPathRecalculation
- Task_006_PathBlockingValidation

Relationship:

- MonsterDefinition provides runtime values such as maxHealth, expReward, damageToPlayer, deathAnimationName, and deathDelay.
- MonsterWaveSpawner creates runtime monster instances before death handling becomes relevant.
- MonsterMovement must stop movement when monsters die or reach the Target Node.
- DynamicPathRecalculation should no longer include dead monsters.
- PathBlockingValidation should no longer validate paths for dead monsters.

This task focuses only on monster death flow and reward handling.

This task does NOT include:

- Projectile combat implementation
- Buff systems
- Advanced damage systems
- Player level progression implementation
- Advanced combat effects
- Runtime victory conditions

These systems are implemented in other tasks or future phases.

---

# 2. Goal

Implement runtime monster death handling and EXP reward flow.

The system should support:

- Runtime damage receiving
- HP reduction
- Death state handling
- Death animation triggering
- Runtime movement stopping
- EXP reward generation
- Runtime monster cleanup
- Monster removal from MonsterManager

---

# 3. Runtime Architecture

Recommended runtime structure:

| Layer | Responsibility |
|---|---|
| MonsterBehaviour | Handles runtime HP and death flow |
| MonsterManager | Tracks alive monsters and unregisters dead monsters |
| PlayerLevelSystem | Receives EXP rewards |
| MonsterDefinition | Provides HP, EXP reward, and death configuration |
| MonsterMovement | Stops movement during death |

Death handling should remain independent from projectile implementation details.

---

# 4. Runtime Damage Rules

Current runtime rules:

- Monsters can receive damage during movement
- Damage reduces currentHealth
- Damage can trigger death
- Monsters should stop all movement after death
- Dead monsters should no longer participate in runtime repathing
- Dead monsters should no longer participate in path validation

---

# 5. Monster HP Flow

Recommended runtime flow:

```text
Monster receives damage
→ Reduce currentHealth
→ Check currentHealth <= 0
→ Enter death flow
```

Monster currentHealth should initialize from:

```text
MonsterDefinition.maxHealth
```

---

# 6. Required Runtime APIs

Recommended APIs:

```csharp
void TakeDamage(int damage)
```

Used to apply runtime damage.

```csharp
void Die()
```

Used to trigger runtime death flow.

```csharp
bool IsDead()
```

Used by future systems to check monster runtime state.

---

# 7. Death Flow

Recommended runtime flow:

```text
Monster HP <= 0
→ Stop movement
→ Stop runtime path updates
→ Play death animation
→ Reward player EXP
→ Unregister from MonsterManager
→ Wait deathDelay
→ Destroy monster object
```

---

# 8. Movement Stop Rules

When a monster dies:

- Movement must stop immediately
- Current path should no longer update
- Runtime repathing should ignore the monster
- Runtime target arrival checks should stop

Recommended behavior:

```text
Monster dies
→ StopMovement()
→ isMoving = false
```

---

# 9. Animation Rules

Monster death should support death animation playback.

Recommended behavior:

- Use deathAnimationName from MonsterDefinition
- Avoid hardcoded animation state names
- Play death animation immediately after death
- Prevent walk animation from continuing

This task does not implement advanced animation layering.

---

# 10. EXP Reward Flow

When a monster dies:

```text
Monster dies
→ Read expReward from MonsterDefinition
→ Reward PlayerLevelSystem EXP
```

Recommended runtime integration:

```csharp
PlayerLevelSystem.AddExp(expReward)
```

This task only triggers EXP reward flow.

Actual level-up behavior is handled by PlayerLevelSystem.

---

# 11. MonsterManager Integration

Dead monsters should be removed from MonsterManager.

Recommended runtime flow:

```text
Monster dies
→ MonsterManager.UnregisterMonster(monster)
```

This ensures:

- Dynamic path recalculation ignores dead monsters
- Path blocking validation ignores dead monsters
- Runtime monster tracking remains correct

---

# 12. Runtime Cleanup

After death animation finishes:

```text
Wait deathDelay
→ Destroy monster GameObject
```

Recommended runtime rules:

- deathDelay comes from MonsterDefinition
- Destroy timing should remain configurable
- Cleanup should avoid duplicate destruction calls

---

# 13. Target Node Arrival

If a monster reaches the Target Node before dying:

Recommended runtime behavior:

```text
Monster reaches Target Node
→ Stop movement
→ Deal damage to player later
→ Remove monster from runtime systems
→ Destroy monster object
```

This task may prepare a basic structure for target arrival cleanup.

However:

- Actual PlayerSystem damage implementation is not required yet
- Advanced battle result handling is not required yet

---

# 14. Runtime Constraints

Current constraints:

- Damage uses simple integer values
- No elemental damage system
- No armor or resistance system
- No critical strike system
- No DOT effects yet
- No crowd control system yet

Future versions may support:

- Buff systems
- DOT effects
- Resistance systems
- Armor systems
- Advanced combat calculations
- Combat events
- Floating damage numbers
- Hit flash feedback

---

# 15. Out of Scope

The following systems are intentionally excluded from this task:

- Projectile implementation
- Buff systems
- DOT systems
- Advanced combat logic
- Critical strike systems
- Player level-up implementation
- Runtime battle victory conditions
- Loot systems
- Item drops
- Advanced combat visual effects

---

# 16. Acceptance Criteria

This task is considered complete when:

1. Monsters can receive runtime damage.
2. currentHealth decreases correctly.
3. Monsters enter death flow when HP reaches 0.
4. Monster movement stops after death.
5. Death animation can be triggered using MonsterDefinition configuration.
6. EXP reward flow can trigger PlayerLevelSystem.AddExp().
7. Dead monsters unregister from MonsterManager.
8. Dead monsters no longer participate in dynamic repathing.
9. Monster GameObjects are destroyed after deathDelay.
10. No advanced combat systems or projectile systems are implemented in this task.