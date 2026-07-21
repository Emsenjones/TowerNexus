# Task012 - Combat Behaviour Structure Migration

Status: Ready for review

Depends on: Task001-Task011

## 1. Goal

Replace the shared `AttackConfig` asset and monolithic archetype-switching `TowerCombatBehaviour` with one abstract combat base and four concrete prefab-authored combat components, while preserving all currently approved Archer, Cannon, Magic, Drone, Behaviour, Elemental, presentation, and cleanup outcomes.

This task changes combat-data ownership and code structure only. It must not implement the Selective Live Refresh behavior reserved for Task013.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/06_TowerPlacementSystem.md`
- `Doc/07_TowerFrameworkSystem.md`
- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/Task/Task001_BehaviourPackageAuthoringExpansion.md` through `Task011_DroneFinalDiveLifecycle.md`

The current System documents are authoritative where their combat-component ownership supersedes historical Task001-Task011 `AttackConfig` wording.

## 3. Current State

- `TowerDefinition` references one `AttackConfig` asset.
- `AttackConfig` stores common fields and unrelated Archer/Cannon, Magic, and Drone fields behind an `AttackArchetype` selector.
- One concrete `TowerCombatBehaviour` contains all four tower release paths and switches by `AttackArchetype`.
- `TowerDeployController` may add a missing `TowerCombatBehaviour` dynamically.
- Placement preview, stat resolution, Projectile, Magic Orb, Drone, and impact-context paths still transport or read `AttackConfig`.
- The four Tower Base Prefabs and their TowerDefinition assets require serialized authoring migration before the old assets can be removed safely.

## 4. Required Component Model

`TowerCombatBehaviour` becomes an abstract base for shared lifecycle and orchestration.

| Component | TowerFamily | Prefab-Authored Fields |
|---|---|---|
| TowerCombatBehaviour | Shared base | `attackRange`, `attackInterval`, `targetSelectionType`, `attackReleaseVfxPrefab` |
| DirectionProjectileCombatBehaviour | Archer | `projectileConfig` |
| ArcProjectileCombatBehaviour | Cannon | `projectileConfig`, `arcHeight` |
| MagicOrbCombatBehaviour | Magic | `magicOrbPrefab`, `magicOrbRotationSpeed`, `magicOrbOrbitRadius`, `magicOrbContactDistance`, `magicOrbMaxHitCount`, `magicOrbMaxLifetime`, `magicOrbSameTargetHitCooldown` |
| DroneCombatBehaviour | Drone | `dronePrefab`, `droneProjectileConfig`, `droneBatteryDuration`, `droneOrbitRadius`, `droneFlightSpeed`, `droneFlightHeight`, `droneBurstCount`, `droneBurstInterval`, `droneBurstCooldown` |

The concrete component type is the tower attack identity. Tower authoring must not keep a second `AttackArchetype` selector that can disagree with the component. `AttackArchetype` may remain as Projectile flight identity for Direction, Arc, and Tracking movement where Projectile System still needs it.

## 5. In Scope

- Make `TowerCombatBehaviour` abstract and retain only genuinely shared initialization, cooldown, target-query, presentation, upgrade subscription, and cleanup coordination.
- Create the four concrete components in the table above.
- Move each archetype's release state, helper methods, validation, serialized fields, and Behaviour-package resolution into its concrete component.
- Remove `AttackConfig` from `TowerDefinition`; validate that `towerPrefab` contains exactly one compatible concrete combat component for its `TowerFamily`.
- Treat a missing or mismatched concrete component as an authoring error. Do not add an untyped combat component at deployment time.
- Update placement preview to read base range from the Tower Base Prefab's component and deployed range from the placed component.
- Refactor `TowerRuntimeStatResolver` to use component-authored base values plus `TowerInstance` level and upgrade state.
- Replace `AttackConfig` parameters or stored references in Projectile, Magic Orb, Drone, and `ProjectileImpactContext` with the smallest typed initialization data each consumer needs.
- Preserve `ProjectileRuntimeOptions`, `MagicOrbRuntimeOptions`, and equivalent typed per-release data where they remain useful.
- Preserve attack-animation forwarding, AttackOrigin/FireAnchor rules, and immediate Arcane Field reconciliation.
- Remove the `AttackConfig` script and four configuration assets only after code and serialized references have migrated successfully.

## 6. Out of Scope

- Refreshing already active Attack Entities after a level or upgrade change.
- Active-entity registration, `ReleaseGroupId`, companion retrofit, or cooldown-ratio refresh; Task013 owns them.
- Changing Behaviour triggers, targeting, hit order, Elemental opportunities, Effects, Buffs, lifetimes, or cleanup outcomes.
- Reinitializing towers or destroying their active Attack Entities when an upgrade is applied.
- A generic Attack Entity hierarchy, runtime-options framework, dependency-injection layer, or new configuration abstraction.
- New content, balance tuning, VFX redesign, or model hierarchy changes.
- Removing Projectile System's Direction/Arc/Tracking flight identity merely because tower authoring no longer uses `AttackArchetype`.

## 7. Migration Sequence

1. Introduce the abstract base and four concrete components without changing released-entity behavior.
2. Move common and archetype-specific authoring reads from `AttackConfig` to those components.
3. Convert stat resolution and Attack Entity initialization to typed data.
4. Update TowerDefinition validation, deployment, model presentation, and placement preview consumers.
5. Configure each Tower Base Prefab and copy its current values from the corresponding `AttackConfig` asset.
6. Clear obsolete TowerDefinition references and verify all four towers.
7. Remove the unused `AttackConfig` script/assets and stale paths only after serialized migration is confirmed.

Do not introduce a runtime fallback that silently reads both authoring models. A temporary editor migration aid is acceptable only if narrowly scoped, reviewed, and removed before completion.

## 8. Unity Authoring Checklist

- Archer Tower Base Prefab: configure `DirectionProjectileCombatBehaviour` and copy common plus projectile values.
- Cannon Tower Base Prefab: configure `ArcProjectileCombatBehaviour` and copy common, projectile, and arc-height values.
- Magic Tower Base Prefab: configure `MagicOrbCombatBehaviour` and copy all Magic Orb values.
- Drone Tower Base Prefab: configure `DroneCombatBehaviour` and copy all Drone values.
- Confirm every TowerDefinition references the expected prefab and passes family/component validation.
- Confirm deployment no longer adds a combat component dynamically.
- Confirm new-tower and deployed-tower range previews use the correct component values.
- Save all prefabs and TowerDefinition assets before removing the old configuration assets.

Unless explicitly requested, Codex owns the script migration and documentation; the user owns final Inspector wiring, serialized value confirmation, and Play Mode acceptance.

## 9. Acceptance Criteria

- `TowerCombatBehaviour` is abstract and has exactly four production concrete tower-combat subtypes.
- Each subtype exposes only common fields plus fields relevant to its archetype.
- TowerDefinition contains no `AttackConfig` reference and rejects a missing, duplicate, or TowerFamily-incompatible component.
- Deployment never adds a missing base combat component at runtime.
- Placement preview resolves range from the prefab-authored component.
- Projectile, Magic Orb, Drone, stat resolver, and impact context no longer store or require `AttackConfig`.
- Tower authoring has no duplicate attack-archetype selector; Projectile flight identity remains where required.
- All attack confirmation, animation release, cooldown start, anchors, target selection, damage, Behaviour, Elemental, and cleanup outcomes remain unchanged.
- Existing active Attack Entities keep pre-Task013 snapshot behavior; this task does not claim Live Refresh.
- No runtime fallback or dead compatibility path consumes old configuration assets.
- After Unity authoring is saved, `AttackConfig` scripts/assets can be removed without missing-reference warnings.

## 10. Validation And Handoff

- Run targeted compilation for the main Unity assembly.
- Run `rg "AttackConfig" Assets/Scripts` and explain any intentional remaining match.
- Search for dynamic `AddComponent<TowerCombatBehaviour>` and tower-level `AttackArchetype` switching.
- In Unity, validate one baseline release for Archer, Cannon, Magic, and Drone.
- Confirm attack VFX, animation-event release, range preview, and cleanup for every family.
- Separate missing-script, missing-reference, and authoring-validation output from runtime defects.
- Run `git diff --check` and review final changed-file scope.

## 11. Review Note

Task012 is a behavior-preserving ownership migration. If implementation requires changing what an upgrade does to an already active entity, stop and move that change to Task013.
