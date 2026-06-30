# Task003 - Runtime Stat Resolver

## Status

Implemented.

## Goal

Make Basic Layer upgrades affect actual runtime combat values.

After this task, Tower Runtime Combat should consume resolved runtime stats instead of raw AttackConfig values wherever an upgraded value is expected.

## Source Documents

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/07_TowerFrameworkSystem.md`
- `Docs/09_ProjectileSystem.md`

## Scope

This task should introduce runtime stat resolution from:

```text
AttackConfig base value
+ TowerInstance upgrade state additive deltas
= Final runtime stat
```

Resolved stats should include:

- AttackRange
- AttackInterval
- AttackDamage resolved from DamageBonus
- MagicOrbRotationSpeed
- MagicOrbMaxHitCount
- DroneBatteryDuration
- DroneBurstCooldown

The first-version damage direction is:

```text
FinalDamage = TowerLevelConfig.BasicDamage + DamageBonus
```

AttackConfig must not apply an additional damage multiplier in this model.

## Required Contracts

- Basic stat deltas are additive in v1.
- Runtime stat resolution clamps final values before combat uses them.
- TowerRuntimeCombatSystem consumes resolved runtime stats.
- Projectile, Magic Orb, and Drone damage paths consume resolved damage values.
- ProjectileSystem should receive or consume resolved damage context; it should not own the formula for combining tower level damage and upgrade state.
- AttackConfig does not own final damage calculation and should not define a runtime damage multiplier.

## Required Clamps

- AttackRange must not become negative.
- AttackInterval must not be lower than the configured minimum allowed interval.
- DamageBonus must not make final damage negative.
- MagicOrbMaxHitCount must be at least 1.
- DroneBatteryDuration must remain positive.
- DroneBurstCooldown must not be lower than the configured minimum allowed cooldown.

## System Document Sync Check

During this task, keep `Docs/09_ProjectileSystem.md` aligned with the resolved damage model. ProjectileSystem should consume resolved damage context and should not own the formula that combines tower level damage and upgrade state.

## Out Of Scope

- DraftSystem Tower Upgrade Draft generation.
- BattleHUD Tower Upgrade Draft UI.
- Behaviour package logic.
- Synergy gameplay.
- Generic Buff or Effect System changes.

## Acceptance Criteria

- Applying DamageBonus +10 to an Archer increases Archer projectile damage by 10.
- Applying AttackInterval -0.2 makes the tower attack faster.
- AttackInterval never goes below the configured minimum.
- Applying AttackRange delta changes enemy detection range.
- Applying MagicOrbMaxHitCount delta changes Magic Orb hit capacity and never resolves below 1.
- Applying DroneBurstCooldown delta changes Drone burst cooldown and never resolves below the configured minimum.
- Runtime combat no longer reads raw AttackConfig values where upgraded values are expected.
- Existing non-upgraded towers preserve their previous runtime behaviour.
- Existing non-upgraded towers resolve damage as TowerLevelConfig.BasicDamage.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
