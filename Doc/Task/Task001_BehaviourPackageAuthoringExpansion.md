# Task001 - Behaviour Package Authoring Expansion

Status: Ready for implementation

Depends on: None

## 1. Goal

Expand the typed Behaviour Layer authoring model from the four already implemented packages to the final twelve approved packages without implementing their gameplay runtime.

This task establishes stable identity, package-specific data, TowerFamily compatibility, and validation for Tasks002-011.

## 2. Source Documents

- `Doc/07_TowerFrameworkSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`

## 3. Current State

The live enum and `TowerUpgradeDefinition` support:

- Archer Piercing Arrow
- Archer Scatter Arrow
- Magic Twin Orbs
- Drone Twin Drones

Their serialized enum values and existing authored assets must remain valid.

## 4. In Scope

- Add the eight missing typed Behaviour package identities.
- Preserve all existing serialized enum values.
- Add package-specific serialized fields, getters, inspector visibility, validation, and TowerFamily compatibility.
- Keep existing Behaviour authoring and runtime reads working.

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

## 5. Out of Scope

- Projectile, Magic Orb, Arcane Field, or Drone gameplay implementation.
- Changing existing package enum values.
- Creating upgrade assets, Effect assets, ProjectileConfig assets, or prefabs.
- Adding speculative generic parameter containers.
- Modifying Basic or Elemental Layer eligibility rules.

## 6. Ownership Contract

- `TowerUpgradeDefinition` owns package authoring data.
- `TowerBehaviourPackageType` owns typed Behaviour identity.
- `TowerUpgradeSystem` validates and records ownership only.
- Runtime systems read the applied definition and consume only parameters relevant to the entity they create.
- Effect gameplay remains owned by Effect System; persistent Buff state remains owned by Buff System.

## 7. Unity Authoring Checklist

After code implementation, the user must be able to author eight Behaviour upgrade assets with the correct TowerFamily and package type.

Required references and values must be visible only for their matching package. No asset needs to be created by this task, but validation must clearly identify missing Effects, nonpositive radii or intervals, invalid bounce counts, and TowerFamily mismatches.

## 8. Acceptance Criteria

- Existing enum values remain unchanged: Archer `100/101`, Magic `200`, Drone `300`.
- All eight new identities use unique explicit values.
- All twelve packages validate against exactly one compatible TowerFamily.
- Package-specific fields appear only for the selected package.
- Required Effect references are validated.
- `bounceSearchRadius`, `maxBounceCount`, Arcane Field radius/tick interval, and `finalDiveHitThreshold` reject nonpositive authored values.
- Hunting Arrow and Twin Shells introduce no redundant V1 numeric fields.
- Existing Piercing Arrow, Scatter Arrow, Twin Orbs, and Twin Drones assets remain readable and valid.
- No gameplay behavior changes in this task.

## 9. Validation

- Inspect serialized enum values before and after the change.
- Run `TowerUpgradeDefinition.IsValid()` against one valid and one invalid definition for every new package type.
- Confirm inspector visibility and warning text.
- Run `git diff --check` and verify scope is limited to authoring/identity code and this Task contract.

## 10. Review Note

Task002 may rely on the new identities and getters. It must not fold all package data into one universal Attack Entity options structure.
