# Task004 - Cannon Twin Shells

Status: Ready for implementation

Depends on: Task003

## 1. Goal

Implement Twin Shells as a Cannon initial-release coverage upgrade that captures up to two different target positions before animation and releases exactly the stored number of Shells from one confirmed attack.

## 2. Source Documents

- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`

## 3. In Scope

- Resolve Twin Shells from the applied Behaviour package at attack confirmation.
- Select up to two different valid Monsters using the tower's current `TargetSelectionType` semantics.
- Capture one position per selected Monster before entering animation wait.
- Release one Shell when exactly one valid target exists.
- Release two Shells when at least two valid targets exist.
- Treat both Shells as independent initial Attack Entities after release.
- Preserve one attack confirmation, animation, release presentation, and cooldown.
- Mark initial-release identity so later Bouncing Shell children cannot consume Twin Shells again.

## 4. Out of Scope

- Releasing two Shells at one Monster when only one target exists.
- Retargeting during animation wait.
- Canceling a stored position because its source Monster became invalid.
- Release delays, spawn offsets, or multiple animation events.
- Explosive Shell or Bouncing Shell implementation.
- A generic multi-shot framework for unrelated towers.

## 5. Runtime Contract

```text
Twin Shells active
    -> collect valid Cannon targets
    -> apply TargetSelectionType while excluding already selected Monsters
    -> store up to two HitAnchor position snapshots
    -> enter WaitingForAnimationRelease
    -> one Animation Event releases one Shell per stored snapshot
    -> one release VFX and one cooldown
```

For `Random`, selection must not choose the same Monster twice when alternatives exist. For Nearest, HighestHealth, and LowestHealth, the second selection applies the same rule after excluding the first Monster.

## 6. Ownership Contract

- Tower Runtime owns target selection, snapshot collection, initial release count, animation, VFX, and cooldown.
- Each released Shell owns its independent position flight and impact lifecycle.
- Twin Shells modifies initial release only.

## 7. Unity Authoring Checklist

- Create or configure a Cannon Behaviour upgrade asset using `CannonTwinShells`.
- No numeric parameter or Effect reference is required in V1.
- Ensure the existing Cannon projectile prefab supports multiple simultaneous instances.

## 8. Acceptance Criteria

- Zero valid targets means no attack and no cooldown.
- One valid target means one stored position and one released Shell.
- Two or more valid targets means two different stored positions and two released Shells.
- Stored positions survive source-Monster invalidation during animation wait.
- Exactly one tower release VFX and one cooldown occur per attack.
- Shells release in the same Animation Event without delay.
- Each Shell resolves its own damage, Elemental, impact, and lifetime state.
- Existing single-Shell Cannon behavior is unchanged when Twin Shells is absent.
- Future bounce children can be distinguished from initial Shells.

## 9. Validation

- Run Play Mode scenarios with zero, one, two, and three or more Monsters.
- Repeat with each TargetSelectionType.
- Kill one or both selected Monsters during animation wait and verify stored-position release.
- Count projectile instances, release VFX instances, and cooldown starts.
- Run `git diff --check` and verify no Explosive/Bouncing gameplay was added.

## 10. Review Note

Task006 will use initial-release identity to ensure each initial Shell owns a bounce chain while bounce children do not multiply through Twin Shells.
