# Task002 - Monster Resolution And Battle Stop

Status: Ready for review

Depends on: Current Player, Monster, Draft, placement, and combat baseline

## 1. Goal

Replace kill-only EXP behavior with the approved Monster Resolution contract:

- A Monster resolves exactly once when Towers kill it or when it reaches the Target.
- Every accepted resolution adds exactly one Player level-progress point.
- Target arrival additionally reduces Player health by exactly one.
- Player health reaching zero enters defeat once and stops the current battle simulation.

This task provides only the minimal demo battle-stop boundary. It does not implement the broader Game Flow.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_StageSystem.md`
- `Doc/02_PlayerSystem.md`
- `Doc/03_BattleHUDUISystem.md`
- `Doc/05_MonsterSystem.md`
- `Doc/06_DraftSystem.md`
- `Doc/07_TowerPlacementSystem.md`

## 3. Current State

- Player progression and UI still use EXP naming.
- Only Monster death grants EXP; Target arrival only applies authored damage.
- MonsterDefinition stores `expReward` and `damageToPlayer` even though both approved outcomes are fixed at one.
- Player progression can still change after `isDead` becomes true.
- Defeat publishes an event but does not stop spawning, combat, movement, Draft, or placement gameplay.
- Player battle-local state has no explicit fresh-Stage reset transaction.

## 4. Resolution Transaction

Monster System must report one semantic resolution transaction containing whether that Monster reached the Target.

```text
Monster Enters Dead Or Arrived
    -> Guard Exactly Once
    -> Report Monster Resolution
        -> Add One Player Progress
        -> If Target Arrival: Reduce Player Health By One
        -> Resolve Level Thresholds And Defeat
    -> Publish Coherent State Changes
    -> Stop Battle If Defeated
```

The Monster that causes defeat still contributes its one progress point. Defeat takes precedence over opening or leaving an interactive Draft after the transaction completes.

Death presentation delay must not delay the resolution transaction. Repeated death, arrival, unregister, cleanup, disable, or destroy paths must not duplicate progress or health damage.

## 5. Progress Terminology And Data Migration

Rename the Player progression model from EXP to resolved progress across runtime code, configuration, events, warnings, and HUD presentation.

Required semantic API/data names should represent:

- Current Progress
- Required Progress
- Progress Required Per Level
- Monster Resolved
- Progress Changed

Preserve existing serialized Player level requirements and scene/prefab references with explicit serialization migration attributes where fields are renamed.

Remove per-Monster EXP reward and Target damage authoring from MonsterDefinition. Do not keep hidden compatibility fields or variable-value runtime branches after migration; the current rules are exactly one progress and exactly one Target damage.

## 6. Battle-Local Player Lifecycle

Provide one explicit fresh-battle initialization transaction that:

- Restores current health to maximum health
- Clears defeated state
- Resets Player level and current progress to their battle-start values
- Publishes one coherent initial-state refresh for consumers
- Makes the battle active before gameplay begins

Task003 Stage Composition will call this transaction for each selected Stage. Before Task003, the current demo bootstrap may invoke it explicitly for the existing single battle.

Player state from a completed or defeated battle must not leak into the next Stage battle.

## 7. Minimal Battle Stop

Introduce one authoritative battle-active/stopped gate for the current demo runtime. Player System owns defeat; a narrow battle-runtime coordinator may propagate the resulting stop to gameplay consumers.

On defeat:

- Stop future Wave spawning.
- Stop Monster movement and future Monster resolution.
- Stop Tower attack scheduling and clean up or invalidate pending/released gameplay entities without ordinary completion results.
- Reject new Draft generation and close or neutralize any active Draft interaction.
- Cancel placement/upgrade dragging and reject new placement intent.
- Reject further Player progress and health mutation.

The implementation must not rely only on scaled delta time if gameplay components could still schedule zero-cooldown work or accept input while time is paused. A time-scale freeze may be used for the demo presentation, but the gameplay-active gate remains authoritative and must be restored safely when a fresh battle begins.

## 8. HUD Boundary

- Continue displaying current Player level, current progress, required progress, and current health.
- Do not add maximum-health presentation unless separately requested.
- Do not add a defeat screen, result panel, restart button, or Stage transition.
- An active Draft or placement interaction must not remain actionable after battle stop.

## 9. Wave Interval Correction

While updating Monster runtime behavior, correct Spawn Interval so it is applied only between adjacent instances inside the same Spawn Entry.

- No Spawn Interval follows the final instance in an entry.
- The next entry begins immediately unless another authored rule delays it.
- Each Wave retains its own Wave Delay.
- Do not add an implicit extra interval between Waves.

## 10. Out Of Scope

- Start screen or main menu
- Stage-selection UI
- Victory detection or victory presentation
- Defeat presentation
- Restart or next-Stage flow
- Stage unlocks, save data, or persistence
- Scene transitions
- Pause-menu design
- Variable Monster resolution rewards or Target damage
- Multiple Spawn Routes

## 11. Unity Authoring Checklist

- Preserve the existing Player level-requirement asset values during EXP-to-progress migration.
- Update Player, HUD, MonsterManager, and MonsterSpawner prefab/scene references after renamed fields or coordinator wiring.
- Remove obsolete Monster reward/damage fields without requiring manual rebalance of every MonsterDefinition.
- Configure the demo battle-runtime coordinator references required to stop current gameplay.
- Keep defeat UI unassigned because it is outside this task.

Unless explicitly handed over, the user owns final Inspector wiring and Play Mode acceptance for the demo battle root.

## 12. Acceptance Criteria

- One killed Monster adds exactly one Player progress.
- One Monster reaching Target adds exactly one progress and removes exactly one health.
- Neither terminal path can report twice.
- A Target arrival that reaches zero health still contributes its progress, then stops the battle.
- No progression API, configuration, UI text, or warning continues to use EXP terminology.
- MonsterDefinition no longer owns variable progress reward or Target damage.
- Multi-threshold level progression and maximum configured level behavior remain valid.
- Defeat publishes once and no later gameplay changes Player progress or health.
- Spawning, movement, attack scheduling, Draft interaction, and placement interaction stop after defeat.
- Fresh battle initialization restores an active, independent Player state.
- Spawn Interval occurs only between instances in the same entry.

## 13. Validation And Handoff

- Run targeted compilation for the main Unity assembly.
- Search scripts and current authored assets for stale `Exp`, `EXP`, `expReward`, and `damageToPlayer` semantics; explain any intentional historical text outside runtime content.
- Test death resolution, Target resolution, duplicate terminal calls, multi-level progress, maximum level, and post-defeat rejection.
- Test a Target arrival that both crosses a level threshold and reaches zero health; confirm no interactive Draft remains after stop.
- Test two entries and two Waves with observable delays to verify interval boundaries.
- Begin a fresh battle after defeat and confirm health, progress, level, active state, and any demo pause mechanism are restored.
- Run `git diff --check` and review serialized migrations before removing compatibility attributes.
