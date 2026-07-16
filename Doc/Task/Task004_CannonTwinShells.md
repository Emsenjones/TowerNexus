# Task004 - Cannon Twin Shells

Status: Implementation complete; Unity Play Mode validation pending

Depends on: Task003

## 1. Goal

Implement Twin Shells as a configurable Cannon initial-release coverage upgrade that captures up to the authored maximum number of different target positions before animation and releases exactly the stored number of Shells from one confirmed attack.

## 2. Source Documents

- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`

## 3. In Scope

- Resolve Twin Shells from the applied Behaviour package at attack confirmation.
- Author `twinShellsMaxInitialShellCount` on `CannonTwinShells`, with minimum and default value `2`.
- Select up to the authored maximum number of different valid Monsters using the tower's current `TargetSelectionType` semantics.
- Capture one position per selected Monster before entering animation wait.
- Release one Shell when exactly one valid target exists.
- Release one Shell per captured target position, up to the authored maximum.
- Treat all released Shells as independent initial Attack Entities after release.
- Preserve one attack confirmation, animation, release presentation, and cooldown.
- Mark initial-release identity so later Bouncing Shell children cannot consume Twin Shells again.

## 4. Out of Scope

- Releasing multiple initial Shells at one Monster when fewer distinct targets exist.
- Progression, tiering, replacement, priority, aggregation, or sequencing rules between multiple assets that represent the same package capability.
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
    -> store up to the authored maximum HitAnchor position snapshots
    -> enter WaitingForAnimationRelease
    -> one Animation Event releases one Shell per stored snapshot
    -> one release VFX and one cooldown
```

For `Random`, selection must not choose the same Monster twice when alternatives exist. For Nearest, HighestHealth, and LowestHealth, every additional selection applies the same rule after excluding all previously selected Monsters.

## 6. Ownership Contract

- Tower Runtime owns target selection, snapshot collection, initial release count, animation, VFX, and cooldown.
- Each released Shell owns its independent position flight and impact lifecycle.
- `TowerUpgradeDefinition` owns the authored maximum initial-Shell count.
- Twin Shells modifies initial release only.
- V1 allows at most one applied Behaviour upgrade with a given non-None `TowerBehaviourPackageType` on one tower, preventing package parameter lookup from depending on applied-upgrade order.

## 7. Unity Authoring Checklist

- Create or configure a Cannon Behaviour upgrade asset using `CannonTwinShells`.
- Set `twinShellsMaxInitialShellCount` to an integer of at least `2`; no Effect reference is required.
- Ensure the existing Cannon projectile prefab supports multiple simultaneous instances.

## 8. Acceptance Criteria

- Zero valid targets means no attack and no cooldown.
- One valid target means one stored position and one released Shell.
- Two or more valid targets produce one different stored position per selected Monster, capped by the authored maximum.
- An authored maximum of three or greater releases that many initial Shells when enough distinct valid targets exist.
- Stored positions survive source-Monster invalidation during animation wait.
- Exactly one tower release VFX and one cooldown occur per attack.
- Shells release in the same Animation Event without delay.
- Each Shell resolves its own damage, Elemental, impact, and lifetime state.
- Existing single-Shell Cannon behavior is unchanged when Twin Shells is absent.
- Future bounce children can be distinguished from initial Shells.
- A tower rejects a second Behaviour upgrade with the same non-None package type, even when it is a different Upgrade asset.

## 9. Validation

- Run Play Mode scenarios with zero, one, two, and at least the authored maximum number of Monsters.
- Repeat with each TargetSelectionType.
- Kill one or more selected Monsters during animation wait and verify every stored position still releases.
- Count projectile instances, release VFX instances, and cooldown starts.
- Run `git diff --check` and verify no Explosive/Bouncing gameplay was added.

## 10. Review Note

Task006 will use initial-release identity to ensure each initial Shell owns a bounce chain while bounce children do not multiply through Twin Shells.
