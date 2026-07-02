# Task005 - Behaviour Package Authoring Parameters

## Objective

Move Phase 1 Behaviour Layer tuning parameters from tower runtime components into their corresponding TowerUpgradeDefinition assets.

After this task, designers should configure Phase 1 Behaviour upgrade parameters on the upgrade item asset, not on TowerCombatBehaviour.

This task should also replace the temporary string-based Behaviour package identity with a typed enum identity.

## System References

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

## Prerequisites

- Task001 Phase 1 Behaviour Runtime Activation is complete.
- Task002 Phase 1 Archer Piercing And Scatter is complete.
- Task003 Phase 1 Magic Twin Orbs is complete.
- Task004 Phase 1 Drone Twin Drones is complete.
- The Phase 1 Behaviour Layer TowerUpgradeDefinition assets exist.

## Scope

Task005 should migrate Phase 1 Behaviour package identity and parameters into TowerUpgradeDefinition authoring data.

The temporary TowerCombatBehaviour tuning fields to remove are:

- piercingArrowMaxHitCount
- scatterArrowAngleOffset
- twinOrbsCount
- twinOrbsStartingAngleOffset
- twinDronesCount
- twinDronesTakeOffDelay

The string-based behaviourPackageId should be replaced by a typed Behaviour package enum.

## Behaviour Package Identity

Add a typed Behaviour package identity enum:

```csharp
public enum TowerBehaviourPackageType
{
    None = 0,

    ArcherPiercingArrow = 100,
    ArcherScatterArrow = 101,

    MagicTwinOrbs = 200,

    DroneTwinDrones = 300,
}
```

TowerUpgradeDefinition should store:

```csharp
[SerializeField] private TowerBehaviourPackageType behaviourPackageType;
```

instead of:

```csharp
[SerializeField] private string behaviourPackageId;
```

Use this enum only for Behaviour Layer package identity.

Basic Layer stat deltas should continue to use TowerUpgradeBasicStatType.

## Runtime Query Path

Update runtime package lookup to use TowerBehaviourPackageType:

```csharp
bool HasBehaviourPackage(TowerBehaviourPackageType packageType)
```

Add a runtime lookup for the applied upgrade definition:

```csharp
bool TryGetBehaviourPackageUpgrade(
    TowerBehaviourPackageType packageType,
    out TowerUpgradeDefinition upgradeDefinition)
```

The source of truth remains the placed tower instance's applied TowerUpgradeDefinition list.

The lookup should:

- skip null applied upgrades
- ignore TowerBehaviourPackageType.None
- return the matching applied upgrade definition
- not read DraftSystem
- not read pending draft UI state
- not bypass TowerUpgradeSystem application rules

The old TowerBehaviourPackageIds string constants should be removed or no longer used after migration.

## Behaviour Layer Parameters

TowerUpgradeDefinition should expose package-specific Behaviour Layer parameters only for matching Behaviour package types.

Suggested Phase 1 parameters:

| Behaviour Package Type | Parameters |
|---|---|
| ArcherPiercingArrow | piercing max hit count |
| ArcherScatterArrow | scatter angle offset |
| MagicTwinOrbs | orb count, starting angle offset |
| DroneTwinDrones | drone count, takeoff delay |

Odin visibility guards should use behaviourPackageType checks rather than string comparisons.

Example:

```csharp
private bool IsArcherPiercingArrow()
{
    return behaviourPackageType == TowerBehaviourPackageType.ArcherPiercingArrow;
}
```

## Runtime Consumption

TowerCombatBehaviour should read Behaviour package parameters from the applied TowerUpgradeDefinition for the matching TowerBehaviourPackageType.

Expected parameter usage:

- Archer Piercing Arrow uses the configured piercing max hit count.
- Archer Scatter Arrow uses the configured scatter angle offset.
- Magic Twin Orbs uses the configured orb count and starting angle offset.
- Drone Twin Drones uses the configured drone count and takeoff delay.

TowerCombatBehaviour should keep safe fallback defaults if expected Behaviour package data is missing or invalid at runtime, and it should log a clear warning.

The fallback values should preserve the current Task002-Task004 default behavior:

- piercing max hit count = 3
- scatter angle offset = 15
- twin orbs count = 2
- twin orbs starting angle offset = 180
- twin drones count = 2
- twin drones takeoff delay = 0.6

## Asset Updates

Update the four Phase 1 Behaviour Layer TowerUpgradeDefinition assets:

- Archer Piercing Arrow
  - behaviourPackageType = ArcherPiercingArrow
  - piercing max hit count = 3
- Archer Scatter Arrow
  - behaviourPackageType = ArcherScatterArrow
  - scatter angle offset = 15
- Magic Twin Orbs
  - behaviourPackageType = MagicTwinOrbs
  - orb count = 2
  - starting angle offset = 180
- Drone Twin Drones
  - behaviourPackageType = DroneTwinDrones
  - drone count = 2
  - takeoff delay = 0.6

Do not create new Behaviour upgrade assets in this task.

## Requirements

- Designers can configure Phase 1 Behaviour package parameters on the corresponding TowerUpgradeDefinition assets.
- TowerCombatBehaviour no longer exposes Phase 1 Behaviour package tuning fields.
- Runtime package checks use TowerBehaviourPackageType.
- Behaviour package parameter lookup uses the applied upgrade definition on the placed tower instance.
- Basic Layer stat delta logic remains separate from Behaviour package identity.
- TowerUpgradeSystem remains the upgrade eligibility, application, and recording authority only.
- DraftSystem remains unrelated to runtime Behaviour execution.
- TowerRuntimeStatResolver remains unchanged.

## Out Of Scope

- Introducing TowerBehaviourPackageDefinition ScriptableObject assets.
- Replacing Behaviour package enum identity with ScriptableObject references.
- Adding new Behaviour packages.
- Adding upgrade exclusion rules.
- Changing DraftSystem sampling rules.
- Changing Basic Layer stat resolution.
- Changing Projectile, Magic Orb, or Drone gameplay behavior beyond reading equivalent parameters from the applied upgrade definition.
- Implementing Missile Drone, Final Dive, Orb Splash, Resonance Orb, Hunting Arrow, Ricochet, Chain, or Buff And Effect behavior.

## Acceptance Criteria

- TowerUpgradeDefinition uses TowerBehaviourPackageType instead of string behaviourPackageId.
- TowerBehaviourPackageIds string constants are removed or no longer used.
- TowerInstance and TowerUpgradeState can query active Behaviour packages by TowerBehaviourPackageType.
- TowerInstance and TowerUpgradeState can return the applied TowerUpgradeDefinition for a Behaviour package type.
- Archer Piercing still limits projectile piercing by the configured max hit count.
- Archer Scatter still uses the configured angle offset.
- Magic Twin Orbs still releases the configured orb count using the configured starting angle offset.
- Drone Twin Drones still releases the configured drone count using the configured takeoff delay.
- Current Task002-Task004 behavior remains equivalent after default asset values are configured.
- TowerUpgradeSystem, DraftSystem, and TowerRuntimeStatResolver remain unchanged.
- Missing or invalid Behaviour parameter data fails safely with clear diagnostics.
