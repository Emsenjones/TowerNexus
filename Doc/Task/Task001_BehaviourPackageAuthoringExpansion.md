# Task001 - Behaviour Package Authoring Expansion And Trigger Cleanup

Status: Implementation complete; Unity Inspector validation pending

Depends on: None

## 1. Goal

Expand the typed Behaviour Layer authoring model from the four already implemented packages to the final twelve approved packages without implementing their gameplay runtime, and remove the unused generic Behaviour Effect-binding/trigger path before adding the new package-specific Effect references.

This task establishes stable identity, package-specific data, TowerFamily compatibility, and validation for Tasks002-011. It also leaves Effect execution with one reviewed model: package runtimes own fixed trigger timing and consume package-specific authoring data, while Effect System receives only the execution context data it actually uses.

## 2. Source Documents

- `Doc/07_TowerFrameworkSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. Current State

The live enum and `TowerUpgradeDefinition` support:

- Archer Piercing Arrow
- Archer Scatter Arrow
- Magic Twin Orbs
- Drone Twin Drones

Their serialized enum values and existing authored assets must remain valid.

`TowerUpgradeDefinition` also exposes generic Behaviour `EffectBindings`, but no runtime calls `EffectBindingExecutor`. Once that unused binding path is removed, no remaining runtime branch reads `EffectTriggerType`; current producers only transport it through `EffectTriggerContext`.

## 4. In Scope

- Add the eight missing typed Behaviour package identities.
- Preserve all existing serialized enum values.
- Add package-specific serialized fields, getters, inspector visibility, validation, and TowerFamily compatibility.
- Keep existing Behaviour authoring and runtime reads working.
- Remove the unused generic Upgrade `EffectBindings` authoring and executor path after confirming every serialized list is empty.
- Remove unused `EffectTriggerType` transport from `EffectTriggerContext` and its existing call sites.
- Keep `BuffEventType` and `BuffEventBinding` as the dedicated Buff lifecycle authoring identity.

Recommended append-only identity values:

```text
ArcherHuntingArrow = 102

MagicArcaneDetonation = 201
MagicArcaneField = 202

DroneBlastRounds = 301
DroneFinalDive = 302

CannonExplosiveShell = 400
CannonTwinShells = 401
CannonBouncingShell = 402
```

Package-specific authoring:

| Package | Authoring Data |
|---|---|
| Archer Hunting Arrow | No V1 parameter |
| Cannon Explosive Shell | Area `EffectDefinition` |
| Cannon Twin Shells | No V1 parameter; initial release count is fixed by contract |
| Cannon Bouncing Shell | Positive `bounceSearchRadius`, positive `maxBounceCount` |
| Magic Arcane Detonation | Area `EffectDefinition` |
| Magic Arcane Field | Positive radius, positive tick interval, tick `EffectDefinition` |
| Drone Blast Rounds | Area `EffectDefinition` |
| Drone Final Dive | Positive `finalDiveHitThreshold`, impact explosion `EffectDefinition` |

`maxBounceCount` means the maximum number of bounce children after one initial Shell. It does not include the initial Shell.

Explosive Shell, Arcane Detonation, Blast Rounds, and Final Dive require valid area Effects with positive radius. Arcane Field instead owns its positive area radius and tick interval directly; its tick Effect must be valid and single-target with radius zero.

## 5. Out of Scope

- Projectile, Magic Orb, Arcane Field, or Drone gameplay implementation.
- Changing existing package enum values.
- Creating upgrade assets, Effect assets, ProjectileConfig assets, or prefabs.
- Adding speculative generic parameter containers.
- Changing Basic or Elemental Layer gameplay eligibility rules.
- Adding a replacement generic trigger, binding, event, or sequencing framework.
- Adding Hit, Impact, or Zone Tick values to `BuffEventType`.

## 6. Ownership Contract

- `TowerUpgradeDefinition` owns package authoring data.
- `TowerBehaviourPackageType` owns typed Behaviour identity.
- `TowerUpgradeSystem` validates and records ownership only.
- Runtime systems read the applied definition and consume only parameters relevant to the entity they create.
- Package runtimes own their reviewed trigger timing and execution order; designers do not independently bind arbitrary trigger types to Behaviour upgrades.
- `EffectTriggerContext` owns source, target, position, damage, and eligibility data only.
- `BuffEventType` remains the authoring and runtime identity for Buff lifecycle bindings.
- Effect gameplay remains owned by Effect System; persistent Buff state remains owned by Buff System.

## 7. Unity Authoring Checklist

After code implementation, the user must be able to author eight Behaviour upgrade assets with the correct TowerFamily and package type.

Required references and values must be visible only for their matching package. No asset needs to be created by this task, but validation must clearly identify missing Effects, nonpositive radii or intervals, invalid bounce counts, and TowerFamily mismatches.

Before deleting `effectBindings`, inspect every serialized `TowerUpgradeDefinition` asset. If any list is non-empty, stop this task and report the asset for explicit migration. When every list is empty, do not bulk reserialize assets merely to remove the stale empty YAML key.

## 8. Acceptance Criteria

- Existing enum values remain unchanged: Archer `100/101`, Magic `200`, Drone `300`.
- All eight new identities use unique explicit values.
- All twelve packages validate against exactly one compatible TowerFamily.
- Package-specific fields appear only for the selected package.
- Package-specific fields remain hidden after an asset is switched from Behaviour to Basic or Elemental Layer.
- Required Effect references are validated.
- Area Effects reject nonpositive Effect radius; Arcane Field rejects a non-single-target tick Effect.
- `bounceSearchRadius`, `maxBounceCount`, Arcane Field radius/tick interval, and `finalDiveHitThreshold` reject nonpositive authored values.
- Hunting Arrow and Twin Shells introduce no redundant V1 numeric fields.
- Existing Piercing Arrow, Scatter Arrow, Twin Orbs, and Twin Drones assets remain readable and valid.
- Generic Upgrade `EffectBindings`, `EffectBindingExecutor`, and `EffectTriggerType` are removed.
- `EffectTriggerContext`, `EffectExecutor`, `BuffEventType`, and `BuffEventBinding` remain active.
- Buff lifecycle execution, Projectile impact, Magic Orb contact, Wind Vortex ticks, and Elemental application retain their existing results.
- No gameplay behavior changes in this task.

## 9. Validation

- Inspect serialized enum values before and after the change.
- Audit every serialized `effectBindings` list before deleting the authoring path.
- Run family validation for all new package types, Effect-shape validation only for Effect-backed packages, and positive-value validation only for packages that own those values.
- Confirm inspector visibility and warning text.
- Confirm no removed binding/trigger symbol remains and no replacement generic trigger system was added.
- Compile the mechanically updated Effect context call sites and run focused gameplay regression checks.
- Run `git diff --check` and verify no upgrade assets, Effect assets, ProjectileConfig assets, or prefabs changed.

## 10. Review Note

Task002 may rely on the new identities and getters. It must not fold all package data into one universal Attack Entity options structure.
